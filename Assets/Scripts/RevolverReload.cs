using UnityEngine;
using UnityEngine.InputSystem;  // Add this for new Input System
using UnityEngine.InputSystem.EnhancedTouch;  // For touch input
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;  // New Input System Touch

public class RevolverCylinderReload : MonoBehaviour
{
    [Header("Cylinder")]
    public Transform cylinder;

    public float zClosed = 0.0005047199f;
    public float zOpen = -0.0668f;

    public float openThreshold = -15f;
    public float closeThreshold = 15f;
    public float dropThreshold = -50f;

    public float moveSpeed = 10f;

    [Header("Ammo")]
    public int maxAmmo = 5;
    public int currentAmmo = 5;

    [Header("Bullet Prefabs")]
    public GameObject droppedBulletPrefab;
    public GameObject firedBulletPrefab;

    [Header("Points")]
    public Transform dropPoint;
    public Transform firePoint;

    [Header("Shooting")]
    public float shootCooldown = 0.5f;
    public float recoilForce = 1f;
    public float recoilDuration = 0.1f;
    private float lastShootTime = 0f;
    private bool isRecoiling = false;
    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private float recoilTimer = 0f;

    [Header("Input")]
    public bool enableTouchShooting = true;
    public bool enableMouseShooting = true;  // For testing in editor

    Vector3 cylinderInitialLocalPos;

    enum CylinderState
    {
        Closed,
        Open
    }

    CylinderState state = CylinderState.Closed;
    bool hasDropped = false;

    void Start()
    {
        if (!cylinder) return;
        cylinderInitialLocalPos = cylinder.localPosition;
        currentAmmo = maxAmmo;
        
        // Store original position/rotation for recoil
        originalPosition = transform.localPosition;
        originalRotation = transform.localRotation;

        // Enable Enhanced Touch support if using touch input
        if (enableTouchShooting && !EnhancedTouchSupport.enabled)
        {
            EnhancedTouchSupport.Enable();
        }
    }

    void LateUpdate()
    {
        if (!cylinder) return;

        float roll = transform.eulerAngles.x;
        if (roll > 180f) roll -= 360f;

        float pitch = transform.eulerAngles.z;
        if (pitch > 180f) pitch -= 360f;

        if (state == CylinderState.Closed && roll <= openThreshold)
        {
            state = CylinderState.Open;
            hasDropped = false;
        }
        else if (state == CylinderState.Open && roll >= closeThreshold)
        {
            state = CylinderState.Closed;
        }

        if (!hasDropped && state == CylinderState.Open && pitch <= dropThreshold)
        {
            DropAndReload();
            hasDropped = true;
        }

        Vector3 targetPos = cylinderInitialLocalPos;
        targetPos.z = (state == CylinderState.Open) ? zOpen : zClosed;

        cylinder.localPosition = Vector3.Lerp(
            cylinder.localPosition,
            targetPos,
            Time.deltaTime * moveSpeed
        );

        // Handle recoil animation
        UpdateRecoil();
    }
    

    void Update()
    {
        // Check for Android touch input using new Input System
        if (enableTouchShooting)
        {
            HandleTouchInput();
        }
        
        // Optional: Also support mouse input for testing in Unity Editor
        if (enableMouseShooting && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            TryShoot();
        }

        // Alternative: Using the new Input System's Touchscreen directly
        // This is more efficient than EnhancedTouch for simple tap detection
        if (enableTouchShooting && Touchscreen.current != null)
        {
            foreach (var touch in Touchscreen.current.touches)
            {
                if (touch.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Began)
                {
                    TryShoot();
                    break;
                }
            }
        }
    }

    void HandleTouchInput()
    {
        // Using EnhancedTouch API from new Input System
        if (Touch.activeTouches.Count > 0)
        {
            // Loop through all active touches
            foreach (var touch in Touch.activeTouches)
            {
                // Only respond to new touches
                if (touch.phase == UnityEngine.InputSystem.TouchPhase.Began)
                {
                    TryShoot();
                    break; // Only shoot once per frame
                }
            }
        }
    }

    void TryShoot()
    {
        // Check cooldown
        if (Time.time - lastShootTime < shootCooldown)
            return;
            
        // Only shoot if cylinder is closed
        if (state != CylinderState.Closed)
            return;
            
        Shoot();
    }

    // =====================
    // SHOOTING
    // =====================
    public void Shoot()
    {
        if (currentAmmo <= 0)
        {
            Debug.Log("Click! Revolver empty.");
            // Optional: Play empty click sound here
            return;
        }

        if (!firedBulletPrefab || !firePoint)
            return;

        // Create the bullet
        Instantiate(
            firedBulletPrefab,
            firePoint.position,
            firePoint.rotation
        );

        // Apply recoil
        StartRecoil();

        currentAmmo--;
        lastShootTime = Time.time;
        Debug.Log($"Shot fired. Ammo left: {currentAmmo}");
        
        // Optional: Add shooting effects
        // - Muzzle flash
        // - Sound effect
        // - Screen shake
    }

    void StartRecoil()
    {
        isRecoiling = true;
        recoilTimer = 0f;
    }

    void UpdateRecoil()
    {
        if (isRecoiling)
        {
            recoilTimer += Time.deltaTime;
            float progress = recoilTimer / recoilDuration;
            
            if (progress <= 1f)
            {
                // Backward movement
                float recoilAmount = Mathf.Sin(progress * Mathf.PI) * recoilForce;
                transform.localPosition = originalPosition - transform.forward * recoilAmount;
                
                // Slight upward kick
                transform.localRotation = originalRotation * Quaternion.Euler(-recoilAmount * 30f, 0f, 0f);
            }
            else
            {
                // Reset position
                isRecoiling = false;
                transform.localPosition = originalPosition;
                transform.localRotation = originalRotation;
            }
        }
    }

    // =====================
    // RELOAD
    // =====================
    void DropAndReload()
    {
        if (!droppedBulletPrefab) return;

        int bulletsToDrop = currentAmmo;
        float spreadRadius = 0.02f;

        for (int i = 0; i < bulletsToDrop; i++)
        {
            Vector3 offset = Random.insideUnitSphere * spreadRadius;
            Vector3 spawnPos = (dropPoint ? dropPoint.position : cylinder.position) + offset;
            Quaternion spawnRot = dropPoint ? dropPoint.rotation : cylinder.rotation;

            GameObject bullet = Instantiate(droppedBulletPrefab, spawnPos, spawnRot);

            Rigidbody rb = bullet.GetComponent<Rigidbody>();
            if (rb)
            {
                rb.AddForce(transform.forward * Random.Range(0.5f, 1.5f), ForceMode.Impulse);
                rb.AddTorque(Random.insideUnitSphere * 2f, ForceMode.Impulse);
            }
        }

        currentAmmo = maxAmmo;
        Debug.Log("Reload complete. Ammo refilled.");
    }

    // Optional: Public method to allow UI button to trigger shooting
    public void ShootButtonPressed()
    {
        TryShoot();
    }

    void OnDestroy()
    {
        // Clean up EnhancedTouch if we enabled it
        if (EnhancedTouchSupport.enabled && enableTouchShooting)
        {
            EnhancedTouchSupport.Disable();
        }
    }
}