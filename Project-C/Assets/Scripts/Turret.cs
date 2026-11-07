using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>Crafted defense. Host-only logic: shoots the nearest monster in range.</summary>
[RequireComponent(typeof(Health))]
public class Turret : NetworkBehaviour
{
    public static readonly List<Turret> All = new List<Turret>();

    public float range = 0.25f;
    public float interval = 10f;
    public int damage = 1;

    private Health health;
    private SpriteRenderer spriteRenderer;
    private float next;

    private void Awake()
    {
        health = GetComponent<Health>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public override void OnNetworkSpawn()
    {
        All.Add(this);
        health.Died += OnDied;
    }

    public override void OnNetworkDespawn()
    {
        All.Remove(this);
        if (health != null) health.Died -= OnDied;
    }

    private void Update()
    {
        if (IsSpawned && health != null && spriteRenderer != null)
            spriteRenderer.color = Color.Lerp(new Color(1f, 0.4f, 0.4f), Color.white, health.Fraction);

        if (!IsServer || Time.time < next) return;

        Monster best = null;
        float bestD = range * range;
        foreach (Monster m in Monster.All)
        {
            if (m == null || !m.IsSpawned) continue;
            float d = ((Vector2)m.transform.position - (Vector2)transform.position).sqrMagnitude;
            if (d < bestD) { bestD = d; best = m; }
        }
        if (best == null) return;

        next = Time.time + interval;
        int bonus = TeamWallet.Instance != null ? TeamWallet.Instance.SwordLevel.Value : 0;
        best.Hurt(damage + bonus);
        ShotClientRpc(transform.position + Vector3.up * 0.4f, best.transform.position);
    }

    private void OnDied(Health h)
    {
        NetworkObject.Despawn(true);
    }

    [ClientRpc]
    private void ShotClientRpc(Vector3 from, Vector3 to)
    {
        FX.Line(from, to, new Color(1f, 0.9f, 0.4f));
        AudioManager.Play("shoot", 0.35f);
    }
}
