using Photon.Pun;
using UnityEngine;

public class HeadAvatarController : MonoBehaviourPun
{
    public Material localMaterial;
    public Material remoteMaterial;

    Transform cam;

    void Awake()
    {
        // Use child renderer in case MeshRenderer is not on the root
        var r = GetComponentInChildren<Renderer>(true);
        if (r && localMaterial && remoteMaterial)
            r.material = photonView.IsMine ? localMaterial : remoteMaterial;

        // Optional: hide your own head so you don't see inside geometry
        // if (photonView.IsMine && r) r.enabled = false;

        // Only the owner drives this
        if (!photonView.IsMine) enabled = false;
    }

    void Start()
    {
        cam = Camera.main ? Camera.main.transform : null;
    }

    void LateUpdate()
    {
        if (!photonView.IsMine) return;
        if (!cam)
        {
            cam = Camera.main ? Camera.main.transform : null;
            if (!cam) return;
        }

        // Copy *rotation only*. Position stays at the spawn (set by the rig in NetBootstrap)
        transform.rotation = cam.rotation;
    }
}
