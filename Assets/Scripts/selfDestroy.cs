using UnityEngine;

public class BulletPhysics : MonoBehaviour
{
    public float destroyTime = 10f;

    void Start()
    {
        Destroy(gameObject, destroyTime);
    }
}
