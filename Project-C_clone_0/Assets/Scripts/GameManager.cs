using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Win/lose rules. The team loses when every player is down at the same time.
/// Reaching the MVP goal (5 nights) lets players finish the run or keep playing.
/// </summary>
public class GameManager : NetworkBehaviour
{
    public static GameManager Instance { get; private set; }
    public static bool IsOver { get { return Instance != null && Instance.Over.Value; } }

    public NetworkVariable<bool> Over = new NetworkVariable<bool>(false);
    public NetworkVariable<int> FinalNights = new NetworkVariable<int>(0);
    public NetworkVariable<bool> GoalReached = new NetworkVariable<bool>(false);

    public override void OnNetworkSpawn()
    {
        Instance = this;
        if (IsServer)
        {
            Over.Value = false;
            GoalReached.Value = false;
        }
    }

    public override void OnNetworkDespawn()
    {
        if (Instance == this) Instance = null;
    }

    private void Update()
    {
        if (!IsServer || Over.Value) return;
        TimeController time = TimeController.Instance;
        if (time == null) return;

        if (!GoalReached.Value && time.NightsSurvived.Value >= GameSettings.GoalNights)
            GoalReached.Value = true;

        bool anyPlayer = false;
        bool anyAlive = false;
        foreach (NetworkClient c in NetworkManager.ConnectedClientsList)
        {
            if (c.PlayerObject == null) continue;
            Health h = c.PlayerObject.GetComponent<Health>();
            if (h == null) continue;
            anyPlayer = true;
            if (!h.IsDead) anyAlive = true;
        }

        if (anyPlayer && !anyAlive) EndGame(time);
    }

    private void EndGame(TimeController time)
    {
        Over.Value = true;
        FinalNights.Value = time.NightsSurvived.Value;
        time.Running = false;
        foreach (Monster m in Monster.All.ToArray())
        {
            if (m != null && m.NetworkObject.IsSpawned) m.NetworkObject.Despawn(true);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void FinishRunServerRpc()
    {
        if (Over.Value || !GoalReached.Value) return;

        TimeController time = TimeController.Instance;
        if (time != null) EndGame(time);
    }

    // Called by MonsterSpawner when the sun comes up.
    public void OnDawn()
    {
        if (!IsServer) return;
        foreach (NetworkClient c in NetworkManager.ConnectedClientsList)
        {
            if (c.PlayerObject == null) continue;
            Health h = c.PlayerObject.GetComponent<Health>();
            if (h == null) continue;
            
        }
        DawnClientRpc();
    }

    [ClientRpc]
    private void DawnClientRpc()
    {
        if (FarmUI.Instance != null) FarmUI.Instance.Toast("Dawn breaks. Everyone recovers a little.");
    }

}
