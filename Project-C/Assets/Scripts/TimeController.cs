using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Networked day/night clock. Only the host advances time; everyone else reads the phase,
/// the night count and the phase end time. Replaces your original TimeController.
/// Lives on the FarmManagers prefab (spawned by ProjectCRuntime when the Farm loads).
/// </summary>
public class TimeController : NetworkBehaviour
{
    public static TimeController Instance { get; private set; }

    public float DayDuration = 95f;
    public float NightDuration = 70f;

    public NetworkVariable<bool> IsNight = new NetworkVariable<bool>(false);
    public NetworkVariable<int> NightsSurvived = new NetworkVariable<int>(0);
    public NetworkVariable<double> PhaseEndTime = new NetworkVariable<double>(0d);

    [HideInInspector] public bool Running = true;

    public float PhaseTimeRemaining
    {
        get { return Mathf.Max(0f, (float)(PhaseEndTime.Value - NetworkManager.ServerTime.Time)); }
    }

    public override void OnNetworkSpawn()
    {
        Instance = this;
        if (!IsServer) return;

        DayDuration = GameSettings.DayLength;
        NightDuration = GameSettings.NightLength;
        IsNight.Value = false;
        NightsSurvived.Value = 0;
        PhaseEndTime.Value = NetworkManager.ServerTime.Time + DayDuration;
    }

    public override void OnNetworkDespawn()
    {
        if (Instance == this) Instance = null;
    }

    private void Update()
    {
        if (!IsServer || !Running) return;

        double now = NetworkManager.ServerTime.Time;
        if (now < PhaseEndTime.Value) return;

        if (IsNight.Value)
        {
            NightsSurvived.Value++;
            IsNight.Value = false;
            PhaseEndTime.Value = now + DayDuration;
        }
        else
        {
            IsNight.Value = true;
            PhaseEndTime.Value = now + NightDuration;
        }
    }
}
