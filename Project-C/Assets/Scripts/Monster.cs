using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Night enemy. Runs on the host only: chases the nearest living player, otherwise tramples crops.
/// Attacks nearby players and defenses, chews through fences, and tramples crops.
/// Drops money and scrap when it dies.
/// Needs: NetworkObject, NetworkTransform, Rigidbody2D (gravity 0), Collider2D, Health.
/// </summary>
[RequireComponent(typeof(Health))]
public class Monster : NetworkBehaviour
{
    public static readonly List<Monster> All = new List<Monster>();

    public float speed = 2.2f;
    public int contactDamage = 2;
    public float attackInterval = 1f;
    public float aggroRange = 9f;
    public int moneyReward = 5;
    public int scrapReward = 1;
    [SerializeField] private Sprite[] animationSprites;
    [SerializeField] private float animationFrameDuration = 0.08f;

    private Health health;
    private SpriteRenderer spriteRenderer;
    private Rigidbody2D rb;
    private Collider2D[] colliders;
    private readonly List<Collider2D> playerColliderBuffer = new List<Collider2D>();
    private float nextHit;
    private float nextFenceHit;
    private float animationStartTime;
    private float scaredUntil;
    private Scarecrow scaredScarecrow;

    private void Awake()
    {
        health = GetComponent<Health>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        colliders = GetComponentsInChildren<Collider2D>(true);
    }

    public override void OnNetworkSpawn()
    {
        All.Add(this);
        animationStartTime = Time.time;
        health.Current.OnValueChanged += OnHealthChanged;
        if (health.IsDead) StopPhysics();
        if (!IsServer)
        {
            // Clients just display the position the host sends.
            if (rb != null) rb.simulated = false;
            return;
        }
        health.Died += OnDied;
        IgnorePlayerCollisions();
    }

    private void Update()
    {
        if (!IsSpawned || spriteRenderer == null || animationSprites == null ||
            animationSprites.Length == 0 || animationFrameDuration <= 0f) return;

        int frame = Mathf.FloorToInt((Time.time - animationStartTime) / animationFrameDuration) % animationSprites.Length;
        if (animationSprites[frame] != null && spriteRenderer.sprite != animationSprites[frame])
        {
            spriteRenderer.sprite = animationSprites[frame];
        }
    }

    public override void OnNetworkDespawn()
    {
        All.Remove(this);
        if (health != null) health.Current.OnValueChanged -= OnHealthChanged;
        if (IsServer && health != null) health.Died -= OnDied;
        StopPhysics();
    }

    public void Hurt(int amount)
    {
        if (!IsServer) return;
        health.TakeDamage(amount);
    }

    private void FixedUpdate()
    {
        if (!IsServer || health.IsDead || rb == null) return;

        IgnorePlayerCollisions();

        Vector2 pos = rb.position;
        Scarecrow scarecrow = Scarecrow.Active;
        if (scarecrow != null && scarecrow.IsSpawned)
        {
            Vector2 away = pos - (Vector2)scarecrow.transform.position;
            if (scaredScarecrow != scarecrow &&
                away.sqrMagnitude <= Scarecrow.EncounterRadius * Scarecrow.EncounterRadius)
            {
                scaredScarecrow = scarecrow;
                scaredUntil = Time.time + Scarecrow.FearDuration;
            }

            if (scaredScarecrow == scarecrow && Time.time < scaredUntil)
            {
                if (away.sqrMagnitude < 0.0001f) away = Vector2.right;
                rb.linearVelocity = away.normalized * speed;
                return;
            }
        }

        Health target = NearestCombatTarget(pos, aggroRange);
        CropPlot targetCrop = target == null ? NearestCrop(pos) : null;

        Vector2 targetPos;
        if (target != null) targetPos = target.transform.position;
        else if (targetCrop != null) targetPos = targetCrop.transform.position;
        else { rb.linearVelocity = Vector2.zero; return; }

        Vector2 to = targetPos - pos;
        float dist = to.magnitude;
        bool isDefense = target != null &&
            (target.GetComponent<Turret>() != null || target.GetComponent<Scarecrow>() != null);
        rb.linearVelocity = dist > (isDefense ? 0.9f : 0.7f) ? to.normalized * speed : Vector2.zero;

        if (dist <= (isDefense ? 1.5f : 1.0f) && Time.time >= nextHit)
        {
            nextHit = Time.time + attackInterval;
            if (target != null) target.TakeDamage(isDefense ? contactDamage * 2 : contactDamage);
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

    private void IgnorePlayerCollisions()
    {
        foreach (NetworkClient client in NetworkManager.ConnectedClientsList)
        {
            if (client.PlayerObject == null) continue;

            playerColliderBuffer.Clear();
            client.PlayerObject.GetComponentsInChildren(playerColliderBuffer);
            foreach (Collider2D monsterCollider in colliders)
            {
                if (monsterCollider == null || !monsterCollider.enabled) continue;

                foreach (Collider2D playerCollider in playerColliderBuffer)
                {
                    if (playerCollider != null && playerCollider.enabled &&
                        !Physics2D.GetIgnoreCollision(monsterCollider, playerCollider))
                    {
                        Physics2D.IgnoreCollision(monsterCollider, playerCollider, true);
                    }
                }
            }
        }
    }

    private Health NearestCombatTarget(Vector2 from, float range)
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

        foreach (Turret turret in Turret.All)
        {
            if (turret == null || !turret.IsSpawned) continue;
            Health h = turret.GetComponent<Health>();
            if (h == null || h.IsDead) continue;
            float d = ((Vector2)turret.transform.position - from).sqrMagnitude;
            if (d < bestD) { bestD = d; best = h; }
        }

        Scarecrow scarecrow = Scarecrow.Active;
        if (scarecrow != null && scarecrow.IsSpawned)
        {
            Health h = scarecrow.GetComponent<Health>();
            if (h != null && !h.IsDead)
            {
                float d = ((Vector2)scarecrow.transform.position - from).sqrMagnitude;
                if (d < bestD) best = h;
            }
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
        StopPhysics();
        DeathFxClientRpc(transform.position);
        NetworkObject.Despawn(true);
    }

    private void OnHealthChanged(int previous, int current)
    {
        if (current <= 0) StopPhysics();
    }

    private void StopPhysics()
    {
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.simulated = false;
        }

        if (colliders == null) return;
        foreach (Collider2D monsterCollider in colliders)
        {
            if (monsterCollider != null) monsterCollider.enabled = false;
        }
    }

    [ClientRpc]
    private void DeathFxClientRpc(Vector3 pos)
    {
        FX.Burst(pos, new Color(0.75f, 0.3f, 0.75f), 16, 0.14f, 3.5f);
    }
}
