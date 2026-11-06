using Unity.Netcode;
using UnityEngine;

/// <summary>Crafted defense. Host-only logic: shoots the nearest monster in range.</summary>
public class Turret : NetworkBehaviour
{
    public float range = 0.25f;
    public float interval = 10f;
    public int damage = 1;

    private float next;

    private void Update()
    {
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

    [ClientRpc]
    private void ShotClientRpc(Vector3 from, Vector3 to)
    {
        FX.Line(from, to, new Color(1f, 0.9f, 0.4f));
        AudioManager.Play("shoot", 0.35f);
    }
}
