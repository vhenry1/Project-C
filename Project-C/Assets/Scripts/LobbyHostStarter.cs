using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Replaces your original LobbyHostStarter. It no longer starts a host automatically,
/// because LobbyUI now lets the player choose: host online (room code), solo/LAN host, or join.
/// startFarm() is still here and is called by the lobby's Start Farm button (host only).
/// </summary>
public class LobbyHostStarter : MonoBehaviour
{
    public void startFarm()
    {
        NetworkManager networkManager = NetworkManager.Singleton;
        if (networkManager == null || !networkManager.IsListening)
        {
            Debug.LogError("Cannot load Farm: the network host is not running.");
            return;
        }

        if (!networkManager.IsServer)
        {
            Debug.LogWarning("Only the network host can load Farm.");
            return;
        }

        SceneEventProgressStatus status =
            networkManager.SceneManager.LoadScene("Farm", LoadSceneMode.Single);
        if (status != SceneEventProgressStatus.Started)
        {
            Debug.LogError("Failed to start the networked Farm scene load: " + status);
        }
    }
}
