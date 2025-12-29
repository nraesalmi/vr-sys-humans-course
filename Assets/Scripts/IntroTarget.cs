using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class IntroTarget : MonoBehaviourPunCallbacks, IPunObservable
{
    [Header("Target Settings")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private GameObject destroyEffectPrefab;
    [SerializeField] private AudioClip hitSound;
    [SerializeField] private AudioClip destroySound;
    [SerializeField] private Renderer targetRenderer;
    [SerializeField] private Color activeColor = Color.red;
    [SerializeField] private Color damagedColor = Color.yellow;
    
    [Header("Multiplayer")]
    [SerializeField] private bool useMasterClientControl = true;
    
    private int currentHealth;
    private bool isDestroyed = false;
    private AudioSource audioSource;
    private GameIntroManager introManager;
    
    // For RPC calls
    private Vector3 networkPosition;
    private Quaternion networkRotation;
    
    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
            
        introManager = FindAnyObjectByType<GameIntroManager>(FindObjectsInactive.Include);
        
        if (PhotonNetwork.IsConnected)
        {
            if (useMasterClientControl && !PhotonNetwork.IsMasterClient)
            {
                // Non-master clients can only observe
                GetComponent<Collider>().enabled = false;
            }
        }
    }
    
    private void Start()
    {
        ResetTarget();
        
        // Register with intro manager
        if (introManager != null && photonView.IsMine)
        {
            introManager.RegisterTarget(this);
        }
    }
    
    public void ResetTarget()
    {
        if (PhotonNetwork.IsConnected && !photonView.IsMine) return;
        
        currentHealth = maxHealth;
        isDestroyed = false;
        
        if (targetRenderer != null)
        {
            targetRenderer.material.color = activeColor;
            targetRenderer.enabled = true;
        }
        
        GetComponent<Collider>().enabled = true;
        
        // Sync via RPC if networked
        if (PhotonNetwork.IsConnected)
        {
            photonView.RPC("RPC_ResetTarget", RpcTarget.All);
        }
    }
    
    [PunRPC]
    private void RPC_ResetTarget()
    {
        currentHealth = maxHealth;
        isDestroyed = false;
        
        if (targetRenderer != null)
        {
            targetRenderer.material.color = activeColor;
            targetRenderer.enabled = true;
        }
        
        GetComponent<Collider>().enabled = true;
    }
    
    public void TakeDamage(int damage)
    {
        // Only allow damage from master client or local in singleplayer
        if (isDestroyed) return;
        
        if (PhotonNetwork.IsConnected)
        {
            if (useMasterClientControl && !PhotonNetwork.IsMasterClient)
            {
                // Request master client to apply damage
                photonView.RPC("RPC_RequestDamage", RpcTarget.MasterClient, damage, PhotonNetwork.LocalPlayer.ActorNumber);
                return;
            }
            
            photonView.RPC("RPC_TakeDamage", RpcTarget.All, damage);
        }
        else
        {
            ApplyDamage(damage);
        }
    }
    
    [PunRPC]
    private void RPC_RequestDamage(int damage, int requestorActorNumber)
    {
        // Master client applies damage
        if (PhotonNetwork.IsMasterClient)
        {
            photonView.RPC("RPC_TakeDamage", RpcTarget.All, damage);
        }
    }
    
    [PunRPC]
    private void RPC_TakeDamage(int damage)
    {
        if (isDestroyed) return;
        
        ApplyDamage(damage);
    }
    
    private void ApplyDamage(int damage)
    {
        currentHealth -= damage;
        
        // Visual feedback
        if (targetRenderer != null)
        {
            float healthPercentage = (float)currentHealth / maxHealth;
            targetRenderer.material.color = Color.Lerp(damagedColor, activeColor, healthPercentage);
            
            // Brief flash effect
            StartCoroutine(FlashEffect());
        }
        
        // Play hit sound
        if (hitSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(hitSound);
        }
        
        // Check if destroyed
        if (currentHealth <= 0 && !isDestroyed)
        {
            DestroyTarget();
        }
    }
    
    private System.Collections.IEnumerator FlashEffect()
    {
        if (targetRenderer == null) yield break;
        
        Color originalColor = targetRenderer.material.color;
        targetRenderer.material.color = Color.white;
        
        yield return new WaitForSeconds(0.1f);
        
        if (!isDestroyed && targetRenderer != null)
        {
            float healthPercentage = (float)currentHealth / maxHealth;
            targetRenderer.material.color = Color.Lerp(damagedColor, activeColor, healthPercentage);
        }
    }
    
    private void DestroyTarget()
    {
        isDestroyed = true;
        
        // Play destroy effect
        if (destroyEffectPrefab != null)
        {
            GameObject effect = Instantiate(destroyEffectPrefab, transform.position, Quaternion.identity);
            Destroy(effect, 3f);
        }
        
        // Play destroy sound
        if (destroySound != null && audioSource != null)
        {
            audioSource.PlayOneShot(destroySound);
        }
        
        // Disable visuals and collider
        if (targetRenderer != null)
            targetRenderer.enabled = false;
        
        GetComponent<Collider>().enabled = false;
        
        // Notify intro manager
        if (introManager != null)
        {
            introManager.OnTargetDestroyed(this);
        }
        
        // Sync destruction
        if (PhotonNetwork.IsConnected)
        {
            photonView.RPC("RPC_DestroyTarget", RpcTarget.Others);
        }
    }
    
    [PunRPC]
    private void RPC_DestroyTarget()
    {
        isDestroyed = true;
        
        if (targetRenderer != null)
            targetRenderer.enabled = false;
        
        GetComponent<Collider>().enabled = false;
    }
    
    public bool IsDestroyed()
    {
        return isDestroyed;
    }
    
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(currentHealth);
            stream.SendNext(isDestroyed);
            stream.SendNext(transform.position);
            stream.SendNext(transform.rotation);
        }
        else
        {
            currentHealth = (int)stream.ReceiveNext();
            isDestroyed = (bool)stream.ReceiveNext();
            networkPosition = (Vector3)stream.ReceiveNext();
            networkRotation = (Quaternion)stream.ReceiveNext();
        }
    }
    
    private void Update()
    {
        if (!PhotonNetwork.IsConnected) return;
        
        // Smooth interpolation for non-local objects
        if (!photonView.IsMine)
        {
            transform.position = Vector3.Lerp(transform.position, networkPosition, Time.deltaTime * 10);
            transform.rotation = Quaternion.Lerp(transform.rotation, networkRotation, Time.deltaTime * 10);
        }
    }
    
    // For direct bullet hits (if bullet script calls this)
    public void OnBulletHit(int damage)
    {
        TakeDamage(damage);
    }
}