using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LobbyHostStarter : MonoBehaviour
{
    private void Start()
    {
        NetworkManager networkManager = NetworkManager.Singleton;
        if (networkManager == null)
        {
            Debug.LogError("Cannot start the lobby host: no NetworkManager exists in the Lobby scene.");
            return;
        }

        if (networkManager.IsListening)
        {
            return;
        }

        if (!networkManager.StartHost())
        {
            Debug.LogError("Failed to start the lobby host. Check the NetworkManager and transport configuration.");
        }
    }

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

        Unity.Netcode.SceneEventProgressStatus status =
            networkManager.SceneManager.LoadScene("Farm", LoadSceneMode.Single);
        if (status != Unity.Netcode.SceneEventProgressStatus.Started)
        {
            Debug.LogError($"Failed to start the networked Farm scene load: {status}.");
        }
    }
}
