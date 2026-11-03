using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// Sword attack (Space or left click). The owner asks, the host decides who gets hit.
/// Also tints the player while downed and handles being teleported to the farm spawn.
/// </summary>
public class PlayerCombat : NetworkBehaviour
{
    public int baseDamage = 3;
    public float attackReach = 1.0f;
    public float attackRadius = 1.3f;
    public float cooldown = 0.45f;

    private SpriteRenderer sr;
    private Health health;
    private float nextLocalAttack;
    private float nextServerAttack;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        health = GetComponent<Health>();
    }

    private void Update()
    {
        // Downed players look faded on every screen.
        if (sr != null && health != null && IsSpawned)
        {
            sr.color = health.IsDead ? new Color(1f, 1f, 1f, 0.4f) : Color.white;
        }

        if (!IsOwner || health == null || health.IsDead || FarmUI.Blocking) return;
        if (SceneManager.GetActiveScene().name != "Farm") return;

        Keyboard kb = Keyboard.current;
        Mouse mouse = Mouse.current;
        bool pressed = (kb != null && kb.spaceKey.wasPressedThisFrame) ||
                       (mouse != null && mouse.leftButton.wasPressedThisFrame);

        if (pressed && Time.time >= nextLocalAttack)
        {
            nextLocalAttack = Time.time + cooldown;
            Vector2 dir = (sr != null && sr.flipX) ? Vector2.left : Vector2.right;
            AttackServerRpc(dir);
        }
    }

    [ServerRpc]
    private void AttackServerRpc(Vector2 dir)
    {
        if (health == null || health.IsDead) return;
        if (Time.time < nextServerAttack) return;
        nextServerAttack = Time.time + cooldown * 0.8f;

        int bonus = TeamWallet.Instance != null ? TeamWallet.Instance.SwordLevel.Value * 2 : 0;
        int damage = baseDamage + bonus;

        Vector2 center = (Vector2)transform.position + dir.normalized * attackReach;
        Collider2D[] hits = Physics2D.OverlapCircleAll(center, attackRadius);
        foreach (Collider2D hit in hits)
        {
            Monster monster;
            if (hit.TryGetComponent(out monster))
            {
                monster.Hurt(damage);
            }
        }

        SwingClientRpc(center);
    }

    [ClientRpc]
    private void SwingClientRpc(Vector2 center)
    {
        FX.Burst(center, new Color(1f, 1f, 0.8f), 5, 0.1f, 2f);
        AudioManager.Play("attack", 0.4f);
    }

    [ClientRpc]
    public void TeleportClientRpc(Vector3 position, ClientRpcParams rpcParams = default)
    {
        if (IsOwner) transform.position = position;
    }
}
