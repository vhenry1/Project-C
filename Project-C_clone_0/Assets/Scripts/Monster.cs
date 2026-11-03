using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Night enemy. Runs on the host only: chases the nearest living player, otherwise tramples crops.
/// Chews through fences that block the way. Drops money and scrap when it dies.
/// Needs: NetworkObject, NetworkTransform, Rigidbody2D (gravity 0), Collider2D, Health.
/// </summary>
[RequireComponent(typeof(Health))]
public class Monster : NetworkBehaviour
{
    public static readonly List<Monster> All = new List<Monster>();

    public float speed = 2.2f;
    public int contactDamage = 1;
    public float attackInterval = 1f;
    public float aggroRange = 9f;
    public int moneyReward = 5;
    public int scrapReward = 1;

    private Health health;
    private Rigidbody2D rb;
    private float nextHit;
    private float nextFenceHit;

    private void Awake()
    {
        health = GetComponent<Health>();
        rb = GetComponent<Rigidbody2D>();
    }

    public override void OnNetworkSpawn()
    {
        All.Add(this);
        if (!IsServer)
        {
            // Clients just display the position the host sends.
            if (rb != null) rb.simulated = false;
            return;
        }
        health.Died += OnDied;
    }

    public override void OnNetworkDespawn()
    {
        All.Remove(this);
        if (IsServer && health != null) health.Died -= OnDied;
    }

    public void Hurt(int amount)
    {
        if (!IsServer) return;
        health.TakeDamage(amount);
    }

    private void FixedUpdate()
    {
        if (!IsServer || health.IsDead || rb == null) return;

        Vector2 pos = rb.position;
        Health targetPlayer = NearestPlayer(pos, aggroRange);
        CropPlot targetCrop = targetPlayer == null ? NearestCrop(pos) : null;

        Vector2 targetPos;
        if (targetPlayer != null) targetPos = targetPlayer.transform.position;
        else if (targetCrop != null) targetPos = targetCrop.transform.position;
        else { rb.linearVelocity = Vector2.zero; return; }

        Vector2 to = targetPos - pos;
        float dist = to.magnitude;
        rb.linearVelocity = dist > 0.7f ? to.normalized * speed : Vector2.zero;

        if (dist <= 1.0f && Time.time >= nextHit)
        {
            nextHit = Time.time + attackInterval;
            if (targetPlayer != null) targetPlayer.TakeDamage(contactDamage);
            else if (targetCrop != null) targetCrop.Trample();
        }
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (!IsServer || Time.time < nextFenceHit) return;
        Fence fence;
        if (collision.collider.TryGetComponent(out fence))
        {
            nextFenceHit = Time.time + 1f;
            fence.GetComponent<Health>().TakeDamage(contactDamage * 2);
        }
    }

    private Health NearestPlayer(Vector2 from, float range)
    {
        Health best = null;
        float bestD = range * range;
        foreach (NetworkClient c in NetworkManager.ConnectedClientsList)
        {
            if (c.PlayerObject == null) continue;
            Health h = c.PlayerObject.GetComponent<Health>();
            if (h == null || h.IsDead) continue;
            float d = ((Vector2)h.transform.position - from).sqrMagnitude;
            if (d < bestD) { bestD = d; best = h; }
        }
        return best;
    }

    private CropPlot NearestCrop(Vector2 from)
    {
        CropPlot best = null;
        float bestD = float.MaxValue;
        foreach (CropPlot c in CropPlot.All)
        {
            if (c == null || c.Stage <= 0) continue;
            float d = ((Vector2)c.transform.position - from).sqrMagnitude;
            if (d < bestD) { bestD = d; best = c; }
        }
        return best;
    }

    private void OnDied(Health h)
    {
        if (TeamWallet.Instance != null) TeamWallet.Instance.Add(moneyReward, scrapReward);
        DeathFxClientRpc(transform.position);
        NetworkObject.Despawn(true);
    }

    [ClientRpc]
    private void DeathFxClientRpc(Vector3 pos)
    {
        FX.Burst(pos, new Color(0.75f, 0.3f, 0.75f), 16, 0.14f, 3.5f);
    }
}
