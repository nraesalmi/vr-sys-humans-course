using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

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

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip shootClip;
    public AudioClip reloadClip;

    public AudioClip emptyClickClip; 

    [Header("Input")]
    public bool enableTouchShooting = true;
    public bool enableMouseShooting = true;

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
    }

    void Update()
    {
        if (enableTouchShooting)
        {
            HandleTouchInput();
        }

        if (enableMouseShooting && Mouse.current != null &&
            Mouse.current.leftButton.wasPressedThisFrame)
        {
            TryShoot();
        }
    }

    void HandleTouchInput()
    {
        if (Touch.activeTouches.Count == 0) return;

        foreach (var touch in Touch.activeTouches)
        {
            if (touch.phase == UnityEngine.InputSystem.TouchPhase.Began)
            {
                TryShoot();
                break;
            }
        }
    }

    void TryShoot()
    {
        if (state != CylinderState.Closed)
            return;

        Shoot();
    }

    // =====================
    // SHOOTING
    // =====================
    void Shoot()
    {
        if (currentAmmo <= 0)
        {
            Debug.Log("Click! Revolver empty.");
            PlaySound(emptyClickClip);
            return;
        }

        if (!firedBulletPrefab || !firePoint)
            return;

        Instantiate(
            firedBulletPrefab,
            firePoint.position,
            firePoint.rotation
        );

        PlaySound(shootClip);

        currentAmmo--;
        Debug.Log($"Shot fired. Ammo left: {currentAmmo}");

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
        PlaySound(reloadClip);

        Debug.Log("Reload complete. Ammo refilled.");
    }

    void PlaySound(AudioClip clip)
    {
        if (audioSource && clip)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    void OnDestroy()
    {
        if (EnhancedTouchSupport.enabled && enableTouchShooting)
        {
            EnhancedTouchSupport.Disable();
        }
    }
}
