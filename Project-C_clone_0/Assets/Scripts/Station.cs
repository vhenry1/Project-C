using UnityEngine;

public enum StationType { Shop, Crafting }

/// <summary>
/// Put on the shop stall and crafting bench in the Farm scene (with a trigger Collider2D).
/// Press E nearby to open the matching menu. Opening a menu is local; buying is validated by the host.
/// </summary>
public class Station : MonoBehaviour
{
    public StationType type = StationType.Shop;

    public string Prompt
    {
        get { return type == StationType.Shop ? "E: Open Shop" : "E: Open Crafting Bench"; }
    }

    public void Open()
    {
        if (FarmUI.Instance == null) return;
        if (type == StationType.Shop) FarmUI.Instance.OpenShop();
        else FarmUI.Instance.OpenCrafting();
    }
}
