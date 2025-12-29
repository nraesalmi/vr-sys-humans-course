using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;

public class EnemyController : MonoBehaviour
{
    PhotonView photonView;

    [Header("Enemy Stats")]
    [SerializeField] public int minBaseDamage = 1000;
    [SerializeField] public int maxBaseDamage = 10000;

    public int baseHealth = 100;
    public int rewardMoney = 10;

    [Header("Health Bar Settings")]
    [SerializeField] private Slider healthSlider;
    [SerializeField] private Canvas healthBarCanvas;
    [SerializeField] private Vector3 healthBarOffset = new Vector3(0, 2f, 0);
    
    [Header("Fixed Spot to Face")]
    [SerializeField] private Transform fixedSpotTransform; // Assign an empty GameObject here
    [SerializeField] private bool alwaysFaceUp = true; // Keep health bar upright
    
    [Header("Current Stats")]
    private int currentHealth;
    private float healthMultiplier = 1f;
    private float speedMultiplier = 1f;
    public int rolledBaseDamage;


    
    [Header("References")]
    private EnemySpawner spawner;
    private BaseHealth targetBase;
    private PatrolFollowingPredeterminedOrder movement;
    
    void Start()
    {
        photonView = GetComponent<PhotonView>();

        movement = GetComponent<PatrolFollowingPredeterminedOrder>();
        if (movement == null)
        {
            Debug.LogWarning("Enemy missing PatrolFollowingPredeterminedOrder component!");
        }
        
        // Initialize health bar position above enemy
        if (healthBarCanvas != null)
        {
            healthBarCanvas.transform.localPosition = healthBarOffset;
            // Set initial rotation to face the fixed spot
            UpdateHealthBarRotation();
        }
    }

    void Update()
    {
        if (photonView.IsMine) return;

        // Smoothly interpolate health for networked enemies
        healthSlider.value = Mathf.Lerp(healthSlider.value, GetHealthPercentage(), Time.deltaTime * 10f);
    }

    
    private void UpdateHealthBarRotation()
    {
        if (healthBarCanvas == null) return;
        
        // Get the target position from the transform if assigned, otherwise use default
        Vector3 targetPosition = GetFixedSpotPosition();
        
        if (alwaysFaceUp)
        {
            // Method 1: Keep health bar upright while facing spot horizontally
            // This is best for readability
            Vector3 lookAtPosition = new Vector3(targetPosition.x, 
                                                healthBarCanvas.transform.position.y, 
                                                targetPosition.z);
            healthBarCanvas.transform.LookAt(lookAtPosition);
        }
        else
        {
            // Method 2: Face spot exactly (might tilt if spot is above/below)
            healthBarCanvas.transform.LookAt(targetPosition);
        }
        
        // Optional: If you want the health bar text/numbers to be readable from the front
        // Rotate 180 degrees so the front faces the spot
        // healthBarCanvas.transform.Rotate(0, 180, 0);
    }
    
    private Vector3 GetFixedSpotPosition()
    {
        // Return the transform position if assigned, otherwise return a default
        if (fixedSpotTransform != null)
        {
            return fixedSpotTransform.position;
        }
        else
        {
            // Default fallback position
            return new Vector3(-6.86f, 3.42f, -31.1f);
        }
    }
    
    // Static method to change the spot for ALL enemies
    public static void SetGlobalFixedSpot(Transform newSpotTransform)
    {
        // Use the non-deprecated method
        EnemyController[] allEnemies = FindObjectsByType<EnemyController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (EnemyController enemy in allEnemies)
        {
            enemy.fixedSpotTransform = newSpotTransform;
        }
    }
    
    // Alternative: Set global fixed spot by position
    public static void SetGlobalFixedSpot(Vector3 newPosition)
    {
        // Create or find a global fixed spot GameObject
        GameObject fixedSpotObj = GameObject.Find("GlobalHealthBarTarget");
        if (fixedSpotObj == null)
        {
            fixedSpotObj = new GameObject("GlobalHealthBarTarget");
            // Optionally make it persistent
            // DontDestroyOnLoad(fixedSpotObj);
        }
        fixedSpotObj.transform.position = newPosition;
        
        // Set this transform on all enemies
        SetGlobalFixedSpot(fixedSpotObj.transform);
    }
    
    // Individual enemy spot change
    public void SetIndividualFixedSpot(Transform newSpotTransform)
    {
        fixedSpotTransform = newSpotTransform;
    }
    
    // Individual enemy spot change by position
    public void SetIndividualFixedSpot(Vector3 newPosition)
    {
        // Create a temporary GameObject for this enemy
        GameObject tempSpot = new GameObject("TempHealthBarTarget_" + gameObject.name);
        tempSpot.transform.position = newPosition;
        // Parent it to the enemy or keep it in world space
        fixedSpotTransform = tempSpot.transform;
    }
    
    public void Initialize(EnemySpawner enemySpawner, BaseHealth healthBase, float hpMultiplier = 1f, float spdMultiplier = 1f)
    {
        spawner = enemySpawner; // assign locally for master
        targetBase = healthBase;
        healthMultiplier = hpMultiplier;
        speedMultiplier = spdMultiplier;

        if (photonView.IsMine)
        {
            // Master sets stats
            currentHealth = Mathf.RoundToInt(baseHealth * healthMultiplier);
            rolledBaseDamage = Random.Range(minBaseDamage, maxBaseDamage + 1);

            // Initialize on all clients
            photonView.RPC("InitializeRPC", RpcTarget.AllBuffered,
                            healthMultiplier, speedMultiplier, rolledBaseDamage);
        }
    }


    [PunRPC]
    public void InitializeRPC(float hpMultiplier, float spdMultiplier, int damage)
    {
        healthMultiplier = hpMultiplier;
        speedMultiplier = spdMultiplier;
        rolledBaseDamage = damage;
        currentHealth = Mathf.RoundToInt(baseHealth * healthMultiplier);

        // Assign spawner for remote clients
        if (spawner == null)
        {
            spawner = FindObjectOfType<EnemySpawner>();
        }

        if (movement != null && speedMultiplier != 1f)
            movement.MovementSpeed *= speedMultiplier;

        UpdateHealthBar();
    }



    [PunRPC]
    void SetStatsRPC(int health, int damage, float hpMult, float spdMult)
    {
        healthMultiplier = hpMult;
        speedMultiplier = spdMult;
        currentHealth = health;
        rolledBaseDamage = damage;

        if (movement != null)
            movement.MovementSpeed *= speedMultiplier;

        UpdateHealthBar();
    }

    public void OnPhotonInstantiate(PhotonMessageInfo info)
    {
        if (!photonView.IsMine && spawner != null)
        {
            spawner.RegisterEnemy(this.gameObject);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!photonView.IsMine) return; // Only Master Client handles triggers

        if (other.CompareTag("Base"))
        {
            if (spawner != null)
            {
                spawner.OnEnemyReachedBase(gameObject); // Master applies damage & unregisters
            }

            // Destroy the enemy across the network
            PhotonNetwork.Destroy(gameObject);
        }
    }

    
    // Clean up any temporary GameObject we created
    void OnDestroy()
    {
        // If we created a temporary GameObject for this enemy's fixed spot, destroy it
        if (fixedSpotTransform != null && fixedSpotTransform.name.StartsWith("TempHealthBarTarget_"))
        {
            Destroy(fixedSpotTransform.gameObject);
        }
    }
    
    // Call this when enemy is killed by towers
    public void Die(bool giveReward = true)
    {
        if (giveReward)
        {
            // Example: GameManager.Instance.AddMoney(rewardMoney);
        }

        // Only Master Client unregisters the enemy and syncs removal
        if (photonView.IsMine && spawner != null)
        {
            spawner.UnregisterEnemy(gameObject);
        }

        // Destroy the enemy
        if (photonView != null && photonView.IsMine)
        {
            PhotonNetwork.Destroy(gameObject);
        }
        else
        {
            Destroy(gameObject); // fallback for remote clients
        }
    }



    
    [PunRPC]
    public void TakeDamageRPC(int damage)
    {
        currentHealth -= damage;
        UpdateHealthBar();
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void TakeDamage(int damage)
    {
        if (photonView == null || !photonView.IsMine) return;

        // Ask all clients to apply damage
        photonView.RPC("TakeDamageRPC", RpcTarget.AllBuffered, damage);
    }


    private void UpdateHealthBar()
    {
        if (healthSlider != null)
        {
            healthSlider.value = GetHealthPercentage();
            
            // Optional: Hide health bar when at full health
            // if (GetHealthPercentage() >= 1f)
            // {
            //     healthBarCanvas.gameObject.SetActive(false);
            // }
            // else if (!healthBarCanvas.gameObject.activeSelf)
            // {
            //     healthBarCanvas.gameObject.SetActive(true);
            // }
        }
    }
    
    // Getter for current health (useful for health bars)
    public int GetCurrentHealth()
    {
        return currentHealth;
    }
    
    public int GetMaxHealth()
    {
        return Mathf.RoundToInt(baseHealth * healthMultiplier);
    }
    
    // For health bar display
    public float GetHealthPercentage()
    {
        return (float)currentHealth / GetMaxHealth();
    }
    
    // OnValidate to see changes in editor
    void OnValidate()
    {
        // This helps visualize the spot in the editor
        if (healthBarCanvas != null)
        {
            UpdateHealthBarRotation();
        }
    }
}