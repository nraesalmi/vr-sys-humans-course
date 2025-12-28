using UnityEngine;

public class RevolverCylinderReload : MonoBehaviour
{
    public Transform cylinder;

    // Z positions
    public float zClosed = 0.0005047199f;
    public float zOpen = -0.0668f;

    // Roll thresholds
    public float openThreshold = -15f;   // open cylinder
    public float closeThreshold = 15f;   // close cylinder
    public float dropThreshold = -50f;   // drop item

    // Animation speed
    public float moveSpeed = 10f;

    // Item to drop from cylinder
    public GameObject itemPrefab;
    public Transform dropPoint; // where the item should appear (optional)

    Vector3 cylinderInitialLocalPos;

    enum CylinderState
    {
        Closed,
        Open
    }

    CylinderState state = CylinderState.Closed;

    // Prevent multiple drops
    bool hasDropped = false;

    void Start()
    {
        if (!cylinder)
            return;

        cylinderInitialLocalPos = cylinder.localPosition;
    }

    void LateUpdate()
    {
        if (!cylinder)
            return;

        // --- Use world rotation to get actual roll ---
        float roll = transform.eulerAngles.x;
        if (roll > 180f) roll -= 360f;

        float pitch = transform.eulerAngles.z;
        if (pitch > 180f) pitch -= 360f; // normalize to -180..180

        // --- Cylinder state transitions ---
        if (state == CylinderState.Closed && roll <= openThreshold)
        {
            state = CylinderState.Open;
            hasDropped = false; // reset drop flag when cylinder opens
            Debug.Log($"Cylinder opened");
        }
        else if (state == CylinderState.Open && roll >= closeThreshold)
        {
            state = CylinderState.Closed;
            Debug.Log($"Cylinder closed");
        }

        // --- Drop item ---
        if (!hasDropped && state == CylinderState.Open && pitch <= dropThreshold)
        {
            DropItem();
            hasDropped = true;
        }

        // --- Animate cylinder ---
        Vector3 targetPos = cylinderInitialLocalPos;
        targetPos.z = (state == CylinderState.Open) ? zOpen : zClosed;

        cylinder.localPosition = Vector3.Lerp(
            cylinder.localPosition,
            targetPos,
            Time.deltaTime * moveSpeed
        );
    }

    void DropItem()
    {
        if (!itemPrefab) return;

        int bulletCount = 5;
        float spreadRadius = 0.02f; // 5 cm spread around the drop point

        for (int i = 0; i < bulletCount; i++)
        {
            // Random small offset so bullets are close but not overlapping
            Vector3 offset = new Vector3(
                Random.Range(-spreadRadius, spreadRadius),
                Random.Range(-spreadRadius, spreadRadius),
                Random.Range(-spreadRadius, spreadRadius)
            );

            Vector3 spawnPos = (dropPoint ? dropPoint.position : cylinder.position) + offset;
            Quaternion spawnRot = dropPoint ? dropPoint.rotation : cylinder.rotation;

            GameObject bullet = Instantiate(itemPrefab, spawnPos, spawnRot);

            // Ensure bullet has Rigidbody for physics
            Rigidbody rb = bullet.GetComponent<Rigidbody>();

            // Optionally add small force so bullets spread naturally
            rb.AddForce(transform.forward * Random.Range(0.5f, 1.5f), ForceMode.Impulse);
            rb.AddTorque(Random.insideUnitSphere * 2f, ForceMode.Impulse);
        }

        Debug.Log($"{bulletCount} bullets dropped");
    }



}
