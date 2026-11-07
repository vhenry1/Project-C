using Unity.Netcode;
using UnityEngine;

/// <summary>
/// The team's shared money, scrap and upgrade levels. The host owns every change.
/// Clients request purchases/crafting through RPCs and the host validates them (anti-cheat basics).
/// </summary>
public class TeamWallet : NetworkBehaviour
{
    private const int FencePerimeterColumns = 8;
    private const int FencePerimeterRows = 4;
    private const float FencePerimeterLeft = -8f;
    private const float FencePerimeterBottom = -4f;
    private const float FencePerimeterWidth = 20f;
    private const float FencePerimeterHeight = 10f;
    private const float TurretPerimeterInset = 1f;

    public static TeamWallet Instance { get; private set; }

    public int startingMoney = 20;

    public NetworkVariable<int> Money = new NetworkVariable<int>(0);
    public NetworkVariable<int> Scrap = new NetworkVariable<int>(0);
    public NetworkVariable<int> SwordLevel = new NetworkVariable<int>(0);
    public NetworkVariable<int> GrowthLevel = new NetworkVariable<int>(0);
    public NetworkVariable<int> YieldLevel = new NetworkVariable<int>(0);

    public float GrowthMultiplier { get { return Mathf.Pow(0.85f, GrowthLevel.Value); } }
    public int YieldBonus { get { return YieldLevel.Value * 1; } }

    public override void OnNetworkSpawn()
    {
        Instance = this;
        if (IsServer) Money.Value = startingMoney;
    }

    public override void OnNetworkDespawn()
    {
        if (Instance == this) Instance = null;
    }

    // ---- Host-side helpers (used by crops, monsters, turrets) ----

    public void Add(int money, int scrap)
    {
        if (!IsServer) return;
        Money.Value += money;
        Scrap.Value += scrap;
    }

    public bool TrySpend(int money, int scrap)
    {
        if (!IsServer || Money.Value < money || Scrap.Value < scrap) return false;
        Money.Value -= money;
        Scrap.Value -= scrap;
        return true;
    }

    // ---- Requests from any player ----

    [ServerRpc(RequireOwnership = false)]
    public void BuyServerRpc(int itemId, ServerRpcParams rpcParams = default)
    {
        bool ok = TryBuy(itemId);
        FeedbackClientRpc(ok, TargetOnly(rpcParams.Receive.SenderClientId));
    }

    private bool TryBuy(int itemId)
    {
        if (itemId < 0 || itemId >= Catalog.Shop.Length) return false;
        ShopItem item = Catalog.Shop[itemId];

        int level = 0;
        if (itemId == Catalog.SwordUpgrade) level = SwordLevel.Value;
        else if (itemId == Catalog.SeedUpgrade) level = GrowthLevel.Value;
        else if (itemId == Catalog.FertilizerUpgrade) level = YieldLevel.Value;

        if (item.MaxLevel > 0 && level >= item.MaxLevel) return false;
        if (!TrySpend(Catalog.ShopPrice(item, level), 0)) return false;

        switch (itemId)
        {
            case Catalog.SwordUpgrade: SwordLevel.Value++; break;
            case Catalog.SeedUpgrade: GrowthLevel.Value++; break;
            case Catalog.FertilizerUpgrade: YieldLevel.Value++; break;
            case Catalog.Bandages:
                foreach (NetworkClient c in NetworkManager.ConnectedClientsList)
                {
                    if (c.PlayerObject == null) continue;
                    Health h = c.PlayerObject.GetComponent<Health>();
                    if (h != null) h.Heal(h.Max.Value);
                }
                break;
            case Catalog.ScrapBundle: Scrap.Value += 3; break;
        }
        return true;
    }

    // facing: which way the player is looking (+1 right, -1 left); the item is placed in front of them.
    [ServerRpc(RequireOwnership = false)]
    public void CraftServerRpc(int itemId, float facing, ServerRpcParams rpcParams = default)
    {
        bool ok = TryCraft(itemId, facing, rpcParams.Receive.SenderClientId);
        FeedbackClientRpc(ok, TargetOnly(rpcParams.Receive.SenderClientId));
    }

    private bool TryCraft(int itemId, float facing, ulong sender)
    {
        if (itemId < 0 || itemId >= Catalog.Craft.Length) return false;
        CraftItem item = Catalog.Craft[itemId];

        NetworkClient client;
        if (!NetworkManager.ConnectedClients.TryGetValue(sender, out client) || client.PlayerObject == null) return false;
        Health h = client.PlayerObject.GetComponent<Health>();
        if (h != null && h.IsDead) return false;

        if (CountInWorld(item.PrefabName) >= item.MaxInWorld) return false;
        GameObject prefab = Resources.Load<GameObject>(item.PrefabName);
        if (prefab == null) return false;

        Vector3 p;
        Quaternion rotation = Quaternion.identity;
        if (item.PrefabName == "Fence")
        {
            if (!TryGetFencePerimeterPosition(out p, out rotation)) return false;
        }
        else if (item.PrefabName == "Turret")
        {
            if (!TryGetTurretPerimeterPosition(out p)) return false;
        }
        else if (item.PrefabName == "Scarecrow")
        {
            p = Vector3.one;
        }
        else
        {
            p = client.PlayerObject.transform.position + new Vector3(Mathf.Sign(facing) * 1.5f, -0.3f, 0f);
            p = new Vector3(Mathf.Round(p.x * 2f) / 2f, Mathf.Round(p.y * 2f) / 2f, 0f);
        }

        if (!TrySpend(item.Money, item.Scrap)) return false;

        GameObject go = Instantiate(prefab, p, rotation);
        go.GetComponent<NetworkObject>().Spawn(true);
        return true;
    }

    private static bool TryGetTurretPerimeterPosition(out Vector3 position)
    {
        float left = FencePerimeterLeft + TurretPerimeterInset;
        float right = FencePerimeterLeft + FencePerimeterWidth - TurretPerimeterInset;
        float bottom = FencePerimeterBottom + TurretPerimeterInset;
        float top = FencePerimeterBottom + FencePerimeterHeight - TurretPerimeterInset;
        Vector3[] corners =
        {
            new Vector3(left, bottom, 0f),
            new Vector3(right, bottom, 0f),
            new Vector3(right, top, 0f),
            new Vector3(left, top, 0f)
        };
        foreach (Vector3 candidate in corners)
        {
            if (!IsTurretPositionOccupied(candidate))
            {
                position = candidate;
                return true;
            }
        }

        position = Vector3.zero;
        return false;
    }

    private static bool IsTurretPositionOccupied(Vector3 candidate)
    {
        foreach (Turret turret in FindObjectsByType<Turret>(FindObjectsSortMode.None))
        {
            if (turret != null && Vector2.Distance(turret.transform.position, candidate) < 1f)
                return true;
        }

        return false;
    }

    private bool TryGetFencePerimeterPosition(out Vector3 position, out Quaternion rotation)
    {
        float stepX = FencePerimeterWidth / FencePerimeterColumns;
        float stepY = FencePerimeterHeight / FencePerimeterRows;

        for (int column = 0; column < FencePerimeterColumns; column++)
        {
            float x = FencePerimeterLeft + (column + 0.5f) * stepX;
            Vector3 bottom = new Vector3(x, FencePerimeterBottom, 0f);
            if (TryGetUnoccupiedFencePosition(bottom, out position))
            {
                rotation = Quaternion.identity;
                return true;
            }

            Vector3 top = new Vector3(x, FencePerimeterBottom + FencePerimeterHeight, 0f);
            if (TryGetUnoccupiedFencePosition(top, out position))
            {
                rotation = Quaternion.identity;
                return true;
            }
        }

        for (int row = 0; row < FencePerimeterRows; row++)
        {
            float y = FencePerimeterBottom + (row + 0.5f) * stepY;
            Vector3 left = new Vector3(FencePerimeterLeft, y, 0f);
            if (TryGetUnoccupiedFencePosition(left, out position))
            {
                rotation = Quaternion.Euler(0f, 0f, 90f);
                return true;
            }

            Vector3 right = new Vector3(FencePerimeterLeft + FencePerimeterWidth, y, 0f);
            if (TryGetUnoccupiedFencePosition(right, out position))
            {
                rotation = Quaternion.Euler(0f, 0f, 90f);
                return true;
            }
        }

        position = Vector3.zero;
        rotation = Quaternion.identity;
        return false;
    }

    private static bool TryGetUnoccupiedFencePosition(Vector3 candidate, out Vector3 position)
    {
        foreach (Fence fence in FindObjectsByType<Fence>(FindObjectsSortMode.None))
        {
            if (fence != null && Vector2.Distance(fence.transform.position, candidate) < 0.5f)
            {
                position = Vector3.zero;
                return false;
            }
        }

        position = candidate;
        return true;
    }

    public int CountInWorld(string prefabName)
    {
        if (prefabName == "Fence") return FindObjectsByType<Fence>(FindObjectsSortMode.None).Length;
        if (prefabName == "Turret") return FindObjectsByType<Turret>(FindObjectsSortMode.None).Length;
        if (prefabName == "Scarecrow") return FindObjectsByType<Scarecrow>(FindObjectsSortMode.None).Length;
        return 0;
    }

    private static ClientRpcParams TargetOnly(ulong clientId)
    {
        return new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = new[] { clientId } } };
    }

    [ClientRpc]
    private void FeedbackClientRpc(bool ok, ClientRpcParams rpcParams = default)
    {
        AudioManager.Play(ok ? "buy" : "deny", 0.6f);
    }
}
