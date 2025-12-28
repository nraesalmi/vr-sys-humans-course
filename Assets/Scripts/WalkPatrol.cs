using System.Collections.Generic;
using UnityEngine;

public class PatrolFollowingPredeterminedOrder : MonoBehaviour
{
    [Header("Movement")]
    public float MovementSpeed = 2f;       // units/sec
    public float TurningSpeed = 6f;        // how fast to rotate (larger = faster)
    public float ArrivalDistance = 0.5f;   // considered reached when within this distance

    [Header("Waypoints (assign in order)")]
    public List<Transform> Waypoints = new List<Transform>();
    public bool IsLoop = false;
    public bool AutoStart = true;

    int nextWp = 0;
    bool isMoving = false;

    void Start()
    {
        if (Waypoints == null || Waypoints.Count == 0)
        {
            Debug.LogWarning($"{name}: No waypoints assigned.");
            return;
        }

        nextWp = 0;
        isMoving = AutoStart;
    }

    void FixedUpdate()
    {
        if (!isMoving || Waypoints == null || Waypoints.Count == 0) return;

        Transform target = Waypoints[nextWp];
        if (target == null)
        {
            AdvanceToNext();
            return;
        }

        // Compute horizontal direction to target (ignore Y)
        Vector3 toTarget = target.position - transform.position;
        toTarget.y = 0f;

        // If target is exactly on same position horizontally, consider reached
        if (toTarget.sqrMagnitude < 0.0001f)
        {
            AdvanceToNext();
            return;
        }

        // Smooth yaw rotation toward target
        Quaternion targetRot = Quaternion.LookRotation(toTarget);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Mathf.Clamp01(Time.deltaTime * TurningSpeed));

        // Move directly toward the target position (prevents orbiting).
        // Using MoveTowards ensures we actually reduce distance every frame.
        Vector3 newPos = Vector3.MoveTowards(transform.position, 
                                             new Vector3(target.position.x, transform.position.y, target.position.z),
                                             MovementSpeed * Time.deltaTime);
        transform.position = newPos;

        // Check arrival
        float sqrDist = (new Vector3(target.position.x, transform.position.y, target.position.z) - transform.position).sqrMagnitude;
        if (sqrDist <= ArrivalDistance * ArrivalDistance)
        {
            AdvanceToNext();
        }
    }

    void AdvanceToNext()
    {
        if (nextWp >= Waypoints.Count - 1)
        {
            if (IsLoop) nextWp = 0;
            else
            {
                isMoving = false;
                Debug.Log($"{name}: Reached final waypoint.");
            }
        }
        else nextWp++;
    }

    // Public controls
    public void StartPatrol() { if (Waypoints.Count > 0) isMoving = true; }
    public void StopPatrol() { isMoving = false; }
    public void ResetToFirstWaypoint(bool startMoving = false) { nextWp = 0; isMoving = startMoving; }

    void OnDrawGizmosSelected()
    {
        if (Waypoints == null) return;
        Gizmos.color = Color.yellow;
        for (int i = 0; i < Waypoints.Count; i++)
        {
            if (Waypoints[i] == null) continue;
            Gizmos.DrawWireSphere(Waypoints[i].position, 0.2f);
            if (i < Waypoints.Count - 1 && Waypoints[i + 1] != null)
                Gizmos.DrawLine(Waypoints[i].position, Waypoints[i + 1].position);
            else if (IsLoop && i == Waypoints.Count - 1 && Waypoints[0] != null)
                Gizmos.DrawLine(Waypoints[i].position, Waypoints[0].position);
        }
    }
}
