using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// Press E near something: revive a downed teammate, open the shop/bench, or plant/harvest a crop.
/// Add to the Player prefab (the setup menu does this for you).
/// </summary>
public class PlayerInteractor : NetworkBehaviour
{
    public static string CurrentPrompt = "";

    public float radius = 1.6f;

    private Health health;
    private PlayerCombat self;

    private void Awake()
    {
        health = GetComponent<Health>();
        self = GetComponent<PlayerCombat>();
    }

    private void Update()
    {
        if (!IsOwner) return;

        if (SceneManager.GetActiveScene().name != "Farm" || health == null || health.IsDead || FarmUI.Blocking)
        {
            CurrentPrompt = "";
            return;
        }

        PlayerCombat downed = null; float downedD = float.MaxValue;
        Station station = null;     float stationD = float.MaxValue;
        CropPlot crop = null;       float cropD = float.MaxValue;

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, radius);
        foreach (Collider2D hit in hits)
        {
            float d = ((Vector2)hit.transform.position - (Vector2)transform.position).sqrMagnitude;

            Station s;
            CropPlot c;
            PlayerCombat pc;
            if (hit.TryGetComponent(out s))
            {
                if (d < stationD) { stationD = d; station = s; }
            }
            else if (hit.TryGetComponent(out c))
            {
                if (d < cropD) { cropD = d; crop = c; }
            }
            else if (hit.TryGetComponent(out pc) && pc != self)
            {
                Health ph = pc.GetComponent<Health>();
                if (ph != null && ph.IsDead && d < downedD) { downedD = d; downed = pc; }
            }
        }

        string prompt = "";
        if (downed != null) prompt = "E: Revive teammate";
        else if (station != null) prompt = station.Prompt;
        else if (crop != null) prompt = crop.Prompt;
        CurrentPrompt = prompt;

        Keyboard kb = Keyboard.current;
        if (kb == null || !kb.eKey.wasPressedThisFrame) return;

        if (downed != null)
        {
            ReviveServerRpc(downed.NetworkObject);
        }
        else if (station != null)
        {
            station.Open();
        }
        else if (crop != null)
        {
            crop.InteractServerRpc();
        }
    }

    [ServerRpc]
    private void ReviveServerRpc(NetworkObjectReference target)
    {
        NetworkObject targetObject;
        if (!target.TryGet(out targetObject)) return;

        Health targetHealth = targetObject.GetComponent<Health>();
        if (health == null || health.IsDead || targetHealth == null || !targetHealth.IsDead) return;
        if (Vector2.Distance(transform.position, targetObject.transform.position) > 3f) return;

        targetHealth.Revive(Mathf.Max(1, targetHealth.Max.Value / 2));
        ReviveFxClientRpc(targetObject.transform.position);
    }

    [ClientRpc]
    private void ReviveFxClientRpc(Vector3 pos)
    {
        FX.Burst(pos, new Color(0.5f, 1f, 0.6f), 16, 0.12f, 3f);
        AudioManager.Play("dawn", 0.5f);
    }
}
