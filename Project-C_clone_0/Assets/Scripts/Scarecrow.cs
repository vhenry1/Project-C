using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Persistent farm scarecrow; monsters that approach it flee for ten seconds.
/// </summary>
[RequireComponent(typeof(Health))]
public class Scarecrow : NetworkBehaviour
{
    public const float EncounterRadius = 3f;
    public const float FearDuration = 10f;

    private Health health;
    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        health = GetComponent<Health>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public override void OnNetworkSpawn()
    {
        health.Died += OnDied;
        if (IsServer) Active = this;
    }

    public override void OnNetworkDespawn()
    {
        if (health != null) health.Died -= OnDied;
        if (Active == this) Active = null;
    }

    private void Update()
    {
        if (IsSpawned && health != null && spriteRenderer != null)
            spriteRenderer.color = Color.Lerp(new Color(1f, 0.4f, 0.4f), Color.white, health.Fraction);
    }

    private void OnDied(Health h)
    {
        NetworkObject.Despawn(true);
    }

    public static Scarecrow Active { get; private set; }
}
