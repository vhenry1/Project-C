using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Starts itself before the first scene and stays alive. It:
///  - builds the right menus for each scene (Menu / Lobby / Farm) so no scene wiring is needed,
///  - spawns the host-side managers (clock, wallet, spawner, rules) when the Farm finishes loading,
///  - moves players to the farm start, and handles "return to lobby" and "quit to menu".
/// </summary>
public class ProjectCRuntime : MonoBehaviour
{
    public static ProjectCRuntime Instance { get; private set; }

    private bool subscribed;
    private GameObject managers;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Boot()
    {
        if (Instance != null) return;
        GameObject go = new GameObject("ProjectCRuntime");
        DontDestroyOnLoad(go);
        go.AddComponent<AudioManager>();
        go.AddComponent<SessionManager>();
        ProjectCRuntime rt = go.AddComponent<ProjectCRuntime>();
        rt.OnSceneReady(SceneManager.GetActiveScene());
    }

    private void Awake()
    {
        Instance = this;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        OnSceneReady(scene);
    }

    private void OnSceneReady(Scene scene)
    {
        switch (scene.name)
        {
            case "Menu":
                new GameObject("MenuSceneUI").AddComponent<MenuSceneUI>();
                break;
            case "Lobby":
                new GameObject("LobbyUI").AddComponent<LobbyUI>();
                break;
            case "Farm":
                new GameObject("FarmUI").AddComponent<FarmUI>();
                Camera cam = Camera.main;
                if (cam != null && cam.GetComponent<CameraFollow2D>() == null) cam.gameObject.AddComponent<CameraFollow2D>();
                break;
        }
    }

    private void Update()
    {
        // Listen for "Farm finished loading" on the host. Re-subscribe if the network was restarted.
        NetworkManager nm = NetworkManager.Singleton;
        if (nm == null || !nm.IsListening) { subscribed = false; return; }

        if (nm.IsServer && !subscribed && nm.SceneManager != null)
        {
            nm.SceneManager.OnLoadEventCompleted -= OnLoadCompleted;
            nm.SceneManager.OnLoadEventCompleted += OnLoadCompleted;
            subscribed = true;
        }
    }

    private void OnLoadCompleted(string sceneName, LoadSceneMode mode, List<ulong> completed, List<ulong> timedOut)
    {
        if (sceneName != "Farm") return;
        SpawnManagers();
        PlacePlayers();
    }

    private void SpawnManagers()
    {
        GameObject prefab = Resources.Load<GameObject>("FarmManagers");
        if (prefab == null)
        {
            Debug.LogError("No 'FarmManagers' prefab in a Resources folder. Run Project C > Run Full Setup.");
            return;
        }
        managers = Instantiate(prefab);
        managers.GetComponent<NetworkObject>().Spawn(true);
    }

    private void PlacePlayers()
    {
        NetworkManager nm = NetworkManager.Singleton;
        GameObject spawn = GameObject.Find("PlayerSpawn");
        Vector3 origin = spawn != null ? spawn.transform.position : Vector3.zero;

        int count = nm.ConnectedClientsList.Count;
        int i = 0;
        foreach (NetworkClient c in nm.ConnectedClientsList)
        {
            if (c.PlayerObject == null) continue;
            PlayerCombat pc = c.PlayerObject.GetComponent<PlayerCombat>();
            if (pc == null) continue;

            Vector3 pos = origin + new Vector3((i - (count - 1) / 2f) * 1.5f, 0f, 0f);
            pc.TeleportClientRpc(pos, new ClientRpcParams
            {
                Send = new ClientRpcSendParams { TargetClientIds = new[] { c.ClientId } }
            });
            i++;
        }
    }

    // Host only: clean up the Farm and go back to the lobby for another run.
    public void ReturnToLobby()
    {
        NetworkManager nm = NetworkManager.Singleton;
        if (nm == null || !nm.IsServer) return;

        DespawnAll<Monster>();
        DespawnAll<Fence>();
        DespawnAll<Turret>();
        DespawnAll<Scarecrow>();

        foreach (NetworkClient c in nm.ConnectedClientsList)
        {
            if (c.PlayerObject == null) continue;
            Health h = c.PlayerObject.GetComponent<Health>();
            if (h != null) h.FullHeal();
        }

        if (managers != null)
        {
            NetworkObject no = managers.GetComponent<NetworkObject>();
            if (no != null && no.IsSpawned) no.Despawn(true);
            managers = null;
        }

        nm.SceneManager.LoadScene("Lobby", LoadSceneMode.Single);
    }

    private static void DespawnAll<T>() where T : NetworkBehaviour
    {
        foreach (T item in FindObjectsByType<T>(FindObjectsSortMode.None))
        {
            if (item != null && item.NetworkObject.IsSpawned) item.NetworkObject.Despawn(true);
        }
    }

    public void QuitToMenu()
    {
        StartCoroutine(QuitRoutine());
    }

    private IEnumerator QuitRoutine()
    {
        if (SessionManager.Instance != null)
        {
            var leave = SessionManager.Instance.LeaveAsync();
            while (!leave.IsCompleted) yield return null;
        }

        NetworkManager nm = NetworkManager.Singleton;
        if (nm != null)
        {
            if (nm.IsListening) nm.Shutdown();
            while (nm != null && nm.ShutdownInProgress) yield return null;
            if (nm != null) Destroy(nm.gameObject); // the Lobby scene brings a fresh one next time
        }

        managers = null;
        SceneManager.LoadScene("Menu");
    }
}
