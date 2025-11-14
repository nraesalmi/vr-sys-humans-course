using UnityEngine;

public class gravity_toggle : Interactive
{
    public float launchSpeed = 40f;
    public float sidewaysSpeed = 2f;

    public new void Interact()
    {
        Rigidbody rb = GetComponent<Rigidbody>();

        rb.useGravity = true;

        rb.linearVelocity = new Vector3(rb.linearVelocity.x + sidewaysSpeed, launchSpeed, rb.linearVelocity.z + sidewaysSpeed);

    }
}
