using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class NetBootstrap : MonoBehaviourPunCallbacks
{
    [SerializeField] string gameVersion = "0.1";

    // Drag HeadAvatar (cube-only) here
    [SerializeField] GameObject headAvatarPrefab;

    void Start()
    {
        Debug.Log("NetBootstrap Start: connecting to Photon...");
        PhotonNetwork.AutomaticallySyncScene = true;
        PhotonNetwork.GameVersion = gameVersion;
        PhotonNetwork.ConnectUsingSettings();
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("Connected to Master Server!");
        Debug.Log("Attempting to join or create room 'Room1'...");
        PhotonNetwork.JoinOrCreateRoom("Room1", new RoomOptions { MaxPlayers = 3 }, TypedLobby.Default);
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("Joined Room: " + PhotonNetwork.CurrentRoom.Name);
        Debug.Log("Current Player Count: " + PhotonNetwork.CurrentRoom.PlayerCount);

        if (!headAvatarPrefab)
        {
            Debug.LogError("No HeadAvatar prefab assigned in NetBootstrap.");
            return;
        }

        int i = PhotonNetwork.CurrentRoom.PlayerCount - 1;
        Vector3[] spawns = {
            new Vector3(-0.5f, 1.6f, 0f),
            new Vector3( 0.5f, 1.6f, 0f),
            new Vector3(-0.5f, 1.6f, 0.6f),
            new Vector3( 0.5f, 1.6f, 0.6f),
        };
        Vector3 pos = spawns[Mathf.Clamp(i, 0, spawns.Length - 1)];

        Debug.Log("Spawning HeadAvatar at position: " + pos);
        GameObject myHead = PhotonNetwork.Instantiate(headAvatarPrefab.name, pos, Quaternion.identity);

        // Snap local rig to spawn
        var pv = myHead.GetComponent<PhotonView>();
        if (pv != null && pv.IsMine)
        {
            var cam = Camera.main;
            if (cam != null)
            {
                Transform rig = cam.transform.parent ? cam.transform.parent : cam.transform;
                rig.SetPositionAndRotation(pos, Quaternion.identity);
                rig.rotation = Quaternion.LookRotation(Vector3.forward, Vector3.up);
                Debug.Log("Local rig moved to spawn position.");
            }
        }
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.LogWarning("Disconnected from Photon: " + cause);
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        Debug.LogError($"Failed to join room: {message} (Code {returnCode})");
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        Debug.LogWarning($"Failed to join random room: {message} (Code {returnCode})");
    }
}
