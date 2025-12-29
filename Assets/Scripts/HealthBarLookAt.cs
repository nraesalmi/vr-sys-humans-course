using UnityEngine;

public class HealthBarLookAtFixedPoint : MonoBehaviour
{
    // Global world position to face
    private static readonly Vector3 FIXED_WORLD_POSITION =
        new Vector3(-6.86f, 3.42f, -31.1f);

    [Tooltip("Keep the health bar upright (no tilting up/down)")]
    public bool keepUpright = true;

    void LateUpdate()
    {
        Vector3 targetPosition = FIXED_WORLD_POSITION;

        Vector3 direction = targetPosition - transform.position;

        if (keepUpright)
        {
            direction.y = 0f;
        }

        if (direction.sqrMagnitude < 0.0001f)
            return;

        // Face the target in world space
        transform.rotation = Quaternion.LookRotation(direction);

        // Flip so the front of the canvas faces the target
        transform.Rotate(0f, 180f, 0f, Space.Self);
    }
}
