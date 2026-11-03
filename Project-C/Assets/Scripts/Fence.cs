using Unity.Netcode;
using UnityEngine;

/// <summary>Crafted barrier. Monsters must chew through it; it turns red as it takes damage.</summary>
[RequireComponent(typeof(Health))]
public class Fence : NetworkBehaviour
{
    private Health health;
    private SpriteRenderer sr;

    public override void OnNetworkSpawn()
    {
        health = GetComponent<Health>();
        sr = GetComponent<SpriteRenderer>();
        if (IsServer) health.Died += OnDied;
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer && health != null) health.Died -= OnDied;
    }

    private void Update()
    {
        if (sr != null && health != null && IsSpawned)
        {
            sr.color = Color.Lerp(new Color(1f, 0.4f, 0.4f), Color.white, health.Fraction);
        }
    }

    private void OnDied(Health h)
    {
        NetworkObject.Despawn(true);
    }
}
