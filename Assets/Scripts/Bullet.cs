using UnityEngine;

public class Bullet : MonoBehaviour
{
    [Header("Bullet Settings")]
    [SerializeField] private int damage = 10;
    [SerializeField] private float speed = 20f;
    [SerializeField] private float lifetime = 5f; // Destroy bullet after this time if it doesn't hit anything
    [SerializeField] private bool destroyOnImpact = true;
    
    [Header("Visual Effects")]
    [SerializeField] private GameObject hitEffectPrefab;
    [SerializeField] private float hitEffectLifetime = 2f;
    
    [Header("References")]
    private Rigidbody rb;
    private Collider bulletCollider;
    
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        bulletCollider = GetComponent<Collider>();
        
        // Auto-destroy after lifetime to prevent infinite bullets
        Destroy(gameObject, lifetime);
    }
    
    private void Start()
    {
        // If using Rigidbody for movement
        if (rb != null)
        {
            rb.linearVelocity = transform.forward * speed;
        }
    }
    
    private void Update()
    {
        // Alternative movement if not using Rigidbody
        if (rb == null)
        {
            transform.position += transform.forward * speed * Time.deltaTime;
        }
    }
    
    // Method to initialize bullet with specific damage/speed
    public void Initialize(int bulletDamage, float bulletSpeed, bool useRigidbody = true)
    {
        damage = bulletDamage;
        speed = bulletSpeed;
        
        if (useRigidbody && rb != null)
        {
            rb.linearVelocity = transform.forward * speed;
        }
    }
    
    private void OnTriggerEnter(Collider other)
    {
        // Don't collide with the shooter (if needed)
        // if (other.CompareTag("Player") || other.CompareTag("Turret")) return;
        
        // Check if we hit an enemy
        if (other.CompareTag("Enemy"))
        {
            EnemyController enemy = other.GetComponent<EnemyController>();
            if (enemy != null)
            {
                // Damage the enemy
                enemy.TakeDamage(damage);
                
                // Spawn hit effect
                SpawnHitEffect();
                
                // Destroy bullet
                if (destroyOnImpact)
                {
                    Destroy(gameObject);
                }
                else
                {
                    // Disable bullet but keep it for effects
                    DisableBullet();
                }
            }
        }
        // Optional: Destroy on hitting environment
        else if (other.CompareTag("Environment") || other.CompareTag("Obstacle"))
        {
            SpawnHitEffect();
            Destroy(gameObject);
        }
    }
    
    private void OnCollisionEnter(Collision collision)
    {
        // Alternative using collision instead of trigger
        // Check if we hit an enemy
        if (collision.gameObject.CompareTag("Enemy"))
        {
            EnemyController enemy = collision.gameObject.GetComponent<EnemyController>();
            if (enemy != null)
            {
                enemy.TakeDamage(damage);
                SpawnHitEffect();
                
                if (destroyOnImpact)
                {
                    Destroy(gameObject);
                }
                else
                {
                    DisableBullet();
                }
            }
        }
    }
    
    private void SpawnHitEffect()
    {
        if (hitEffectPrefab != null)
        {
            GameObject effect = Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);
            Destroy(effect, hitEffectLifetime);
        }
    }
    
    private void DisableBullet()
    {
        // Disable visuals/collisions but keep the GameObject for effects
        if (rb != null) rb.linearVelocity = Vector3.zero;
        if (bulletCollider != null) bulletCollider.enabled = false;
        
        // Disable renderer
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null) renderer.enabled = false;
        
        // Disable trail renderer if present
        TrailRenderer trail = GetComponent<TrailRenderer>();
        if (trail != null) trail.enabled = false;
        
        // Destroy after a short delay
        Destroy(gameObject, 0.5f);
    }
    
    // For tower/weapon to set target
    public void SetTarget(Vector3 targetPosition)
    {
        // Face the target
        transform.LookAt(targetPosition);
        
        // Recalculate velocity if using Rigidbody
        if (rb != null)
        {
            rb.linearVelocity = transform.forward * speed;
        }
    }
    
    // For homing bullets (optional)
    public void SetHomingTarget(Transform target, float turnSpeed = 5f)
    {
        if (target == null) return;
        
        // Calculate direction to target
        Vector3 direction = (target.position - transform.position).normalized;
        
        // Rotate towards target
        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
        
        // Update velocity
        if (rb != null)
        {
            rb.linearVelocity = transform.forward * speed;
        }
    }
}