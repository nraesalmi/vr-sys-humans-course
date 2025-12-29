using UnityEngine;
using UnityEngine.Events;

public class BaseHealth : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private int currentHealth;
    
    [Header("UI References")]
    [Tooltip("Optional health bar slider")]
    public UnityEngine.UI.Slider healthSlider;
    
    [Tooltip("Optional health display text")]
    public UnityEngine.UI.Text healthText;
    
    [Tooltip("Optional health percentage text")]
    public UnityEngine.UI.Text healthPercentText;
    
    [Tooltip("Optional image for health bar fill (for color changing)")]
    public UnityEngine.UI.Image healthFillImage;
    
    [Header("Health Bar Colors")]
    [Tooltip("Color when health is high")]
    public Color highHealthColor = Color.green;
    
    [Tooltip("Color when health is medium")]
    public Color mediumHealthColor = Color.yellow;
    
    [Tooltip("Color when health is low")]
    public Color lowHealthColor = Color.red;
    
    [Header("Text Format Options")]
    [Tooltip("Format for health text")]
    public string healthFormat = "{0}/{1}";
    
    [Tooltip("Format for health percentage text")]
    public string percentFormat = "{0}%";
    
    [Header("Health Thresholds (0-1)")]
    [Range(0, 1)]
    public float highHealthThreshold = 0.7f;
    
    [Range(0, 1)]
    public float mediumHealthThreshold = 0.3f;
    
    [Header("Events")]
    public UnityEvent<int> onHealthChanged; // Passes current health
    public UnityEvent<int> onDamageTaken; // Passes damage amount
    public UnityEvent<int> onHealed; // Passes heal amount
    public UnityEvent onBaseDestroyed;
    
    [Header("Debug")]
    [SerializeField] private bool showDebug = true;
    
    public int MaxHealth => maxHealth;
    public int CurrentHealth => currentHealth;
    public float HealthPercentage => (float)currentHealth / maxHealth;
    
    void Start()
    {
        InitializeHealth();
    }
    
    void InitializeHealth()
    {
        currentHealth = maxHealth;
        UpdateUI();
        
        if (showDebug)
            Debug.Log($"Base health initialized: {currentHealth}/{maxHealth}", this);
    }
    
    public void TakeDamage(int damage)
    {
        if (damage <= 0 || currentHealth <= 0) return;
        
        int previousHealth = currentHealth;
        currentHealth -= damage;
        currentHealth = Mathf.Max(0, currentHealth);
        
        int actualDamage = previousHealth - currentHealth;
        
        if (showDebug)
            Debug.Log($"Base took {actualDamage} damage. Health: {currentHealth}/{maxHealth}", this);
        
        // Trigger events
        onDamageTaken?.Invoke(actualDamage);
        onHealthChanged?.Invoke(currentHealth);
        
        // Update UI
        UpdateUI();
        
        // Check if base is destroyed
        if (currentHealth <= 0)
        {
            BaseDestroyed();
        }
    }
    
    public void Heal(int amount)
    {
        if (amount <= 0 || currentHealth >= maxHealth) return;
        
        int previousHealth = currentHealth;
        currentHealth += amount;
        currentHealth = Mathf.Min(currentHealth, maxHealth);
        
        int actualHeal = currentHealth - previousHealth;
        
        if (showDebug)
            Debug.Log($"Base healed for {actualHeal}. Health: {currentHealth}/{maxHealth}", this);
        
        // Trigger events
        onHealed?.Invoke(actualHeal);
        onHealthChanged?.Invoke(currentHealth);
        
        UpdateUI();
    }
    
    public void SetHealth(int newHealth)
    {
        int previousHealth = currentHealth;
        currentHealth = Mathf.Clamp(newHealth, 0, maxHealth);
        
        int difference = currentHealth - previousHealth;
        
        if (difference < 0)
        {
            onDamageTaken?.Invoke(Mathf.Abs(difference));
        }
        else if (difference > 0)
        {
            onHealed?.Invoke(difference);
        }
        
        onHealthChanged?.Invoke(currentHealth);
        UpdateUI();
        
        if (currentHealth <= 0 && previousHealth > 0)
        {
            BaseDestroyed();
        }
    }
    
    public void SetMaxHealth(int newMaxHealth, bool healToNewMax = false)
    {
        int oldMaxHealth = maxHealth;
        maxHealth = Mathf.Max(1, newMaxHealth); // Ensure at least 1 max health
        
        if (healToNewMax)
        {
            currentHealth = maxHealth;
        }
        else
        {
            // Maintain the same health percentage
            float healthPercent = (float)currentHealth / oldMaxHealth;
            currentHealth = Mathf.RoundToInt(healthPercent * maxHealth);
        }
        
        UpdateUI();
    }
    
    public void ResetHealth()
    {
        currentHealth = maxHealth;
        onHealthChanged?.Invoke(currentHealth);
        UpdateUI();
    }
    
    void UpdateUI()
    {
        UpdateSlider();
        UpdateText();
        UpdateHealthBarColor();
    }
    
    void UpdateSlider()
    {
        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHealth;
            healthSlider.value = currentHealth;
        }
    }
    
    void UpdateText()
    {
        if (healthText != null)
        {
            healthText.text = string.Format(healthFormat, currentHealth, maxHealth);
        }
        
        if (healthPercentText != null)
        {
            int percent = Mathf.RoundToInt(HealthPercentage * 100);
            healthPercentText.text = string.Format(percentFormat, percent);
        }
    }
    
    void UpdateHealthBarColor()
    {
        if (healthFillImage != null)
        {
            float healthPercent = HealthPercentage;
            
            if (healthPercent > highHealthThreshold)
            {
                healthFillImage.color = highHealthColor;
            }
            else if (healthPercent > mediumHealthThreshold)
            {
                healthFillImage.color = mediumHealthColor;
            }
            else
            {
                healthFillImage.color = lowHealthColor;
            }
        }
    }
    
    void BaseDestroyed()
    {
        if (showDebug)
            Debug.Log("Base destroyed! Game Over!", this);
        
        // Trigger event
        onBaseDestroyed?.Invoke();
        
        // Stop all enemy movement
        var enemies = FindObjectsByType<PatrolFollowingPredeterminedOrder>(FindObjectsSortMode.None);
        foreach (var enemy in enemies)
        {
            enemy.StopPatrol();
        }
        
        // Stop spawning
        var spawner = FindFirstObjectByType<EnemySpawner>();
        if (spawner != null)
        {
            spawner.StopAllCoroutines();
        }
    }
    
    // For external access to check health status
    public bool IsAlive => currentHealth > 0;
    public bool IsFullHealth => currentHealth == maxHealth;
    
    // For UI updates when max health changes externally
    public void RefreshUI()
    {
        UpdateUI();
    }
    
    #region Editor Utilities
    #if UNITY_EDITOR
    [UnityEditor.MenuItem("Tools/Health/Reset All Health Components")]
    static void ResetAllHealthComponents()
    {
        var healthComponents = FindObjectsByType<BaseHealth>(FindObjectsSortMode.None);
        foreach (var health in healthComponents)
        {
            health.ResetHealth();
        }
    }
    #endif
    #endregion
    
    void OnDrawGizmos()
    {
        // Visualize health percentage with color
        float healthPercent = (float)currentHealth / maxHealth;

        Gizmos.color = Color.Lerp(Color.red, Color.green, healthPercent);
        Gizmos.DrawWireCube(transform.position, Vector3.one * 2);

        Gizmos.color = new Color(
            Gizmos.color.r,
            Gizmos.color.g,
            Gizmos.color.b,
            0.3f
        );
        Gizmos.DrawCube(transform.position, Vector3.one * 2);

        // Draw health text above the base - Editor only
        #if UNITY_EDITOR
        UnityEditor.Handles.Label(
            transform.position + Vector3.up * 1.5f,
            $"{currentHealth}/{maxHealth}"
        );
        #endif
    }
}