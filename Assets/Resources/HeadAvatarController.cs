using Photon.Pun;
using UnityEngine;

public class HeadAvatarController : MonoBehaviourPun
{
    public Material localMaterial;
    public Material remoteMaterial;

    public Transform playerRoot;

    Transform cam;
    Quaternion spineBindRotation;

    void Awake()
    {
        var r = GetComponentInChildren<Renderer>(true);
        if (r && localMaterial && remoteMaterial)
            r.material = photonView.IsMine ? localMaterial : remoteMaterial;

        if (!photonView.IsMine)
            enabled = false;
    }

    void Start()
    {
        cam = Camera.main ? Camera.main.transform : null;
        spineBindRotation = transform.localRotation;
    }

    void LateUpdate()
    {
        if (!photonView.IsMine || !cam || !playerRoot)
            return;

        Vector3 camEuler = cam.rotation.eulerAngles;

        float yaw = camEuler.y;
        playerRoot.rotation = Quaternion.Euler(0f, yaw, 0f);

        float pitch = camEuler.x;
        if (pitch > 180f) pitch -= 360f;
        pitch = Mathf.Clamp(pitch, -60f, 60f);

        float roll = camEuler.z;
        if (roll > 180f) roll -= 360f;
        roll = Mathf.Clamp(roll, -30f, 30f);

        Quaternion pitchRot =
            Quaternion.AngleAxis(pitch, Vector3.right);

        Quaternion rollRot =
            Quaternion.AngleAxis(roll, Vector3.forward);

        transform.localRotation =
            spineBindRotation * pitchRot * rollRot;
    }
}
