using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Persistent farm scarecrow; monsters that approach it flee for ten seconds.
/// </summary>
public class Scarecrow : NetworkBehaviour
{
    public const float EncounterRadius = 3f;
    public const float FearDuration = 10f;

    public override void OnNetworkSpawn()
    {
        if (IsServer) Active = this;
    }

    public override void OnNetworkDespawn()
    {
        if (Active == this) Active = null;
    }

    public static Scarecrow Active { get; private set; }
}
