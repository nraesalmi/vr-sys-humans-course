using UnityEngine;
using Photon.Pun;

public class gravity_toggle : Interactive
{
    public float launchSpeed = 40f;
    public float sidewaysSpeed = 2f;
    PhotonView pv;
    Rigidbody rb;

    void Awake()
    {
        pv = GetComponent<PhotonView>();
        rb = GetComponent<Rigidbody>();
    }


    public new void Interact()
    {
        if (pv == null) return;

        // Ask Photon to run EnableGravity() on ALL clients
        pv.RPC("EnableGravity", RpcTarget.AllBuffered);
    }

    [PunRPC]
    void EnableGravity()
    {
        if (rb == null) rb = GetComponent<Rigidbody>();
        if (rb != null)
            rb.useGravity = true;

            rb.linearVelocity = new Vector3(rb.linearVelocity.x + sidewaysSpeed, launchSpeed, rb.linearVelocity.z + sidewaysSpeed);

    }

}
