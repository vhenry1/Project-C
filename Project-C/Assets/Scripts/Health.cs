using System;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Shared health for players, monsters and farm defenses. The host changes it;
/// every client sees the same value through NetworkVariables.
/// </summary>
public class Health : NetworkBehaviour
{
    public int maxHealth = 20;

    public NetworkVariable<int> Current = new NetworkVariable<int>(20);
    public NetworkVariable<int> Max = new NetworkVariable<int>(20);

    public event Action<Health> Died;     // host only
    public event Action<Health> Revived;  // host only

    public bool IsDead { get { return Current.Value <= 0; } }
    public float Fraction { get { return Max.Value > 0 ? (float)Current.Value / Max.Value : 0f; } }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            Max.Value = maxHealth;
            Current.Value = maxHealth;
        }
    }

    public void TakeDamage(int amount)
    {
        if (!IsServer || IsDead || amount <= 0) return;
        Current.Value = Mathf.Max(0, Current.Value - amount);
        bool lethal = Current.Value == 0;
        DamagedClientRpc(lethal);
        if (lethal && Died != null) Died(this);
    }

    public void Heal(int amount)
    {
        if (!IsServer || IsDead) return;
        Current.Value = Mathf.Min(Max.Value, Current.Value + amount);
    }

    public void Revive(int hp)
    {
        if (!IsServer || !IsDead) return;
        Current.Value = Mathf.Clamp(hp, 1, Max.Value);
        if (Revived != null) Revived(this);
    }

    public void FullHeal()
    {
        if (!IsServer) return;
        bool wasDead = IsDead;
        Current.Value = Max.Value;
        if (wasDead && Revived != null) Revived(this);
    }

    [ClientRpc]
    private void DamagedClientRpc(bool lethal)
    {
        FX.Burst(transform.position + Vector3.up * 0.3f, new Color(1f, 0.35f, 0.35f), lethal ? 14 : 6);
        bool isPlayer = GetComponent<PlayerCombat>() != null;
        if (lethal) AudioManager.Play(isPlayer ? "down" : "hit");
        else AudioManager.Play(isPlayer ? "hurt" : "hit", 0.5f);
    }
}
