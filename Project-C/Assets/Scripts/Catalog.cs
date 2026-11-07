/// <summary>
/// All shop and crafting items in one place so prices are easy to tune.
/// Ids are sent over the network, so keep them stable.
/// </summary>
public struct ShopItem
{
    public int Id;
    public string Name;
    public string Desc;
    public int BaseMoney;   // price = BaseMoney * (level + 1) for upgrades
    public int MaxLevel;    // 0 = repeatable purchase
}

public struct CraftItem
{
    public int Id;
    public string Name;
    public string Desc;
    public int Money;
    public int Scrap;
    public string PrefabName; // loaded from Resources
    public int MaxInWorld;
}

public static class Catalog
{
    public const int SwordUpgrade = 0;
    public const int SeedUpgrade = 1;
    public const int FertilizerUpgrade = 2;
    public const int Bandages = 3;
    public const int ScrapBundle = 4;

    public static readonly ShopItem[] Shop =
    {
        new ShopItem { Id = SwordUpgrade,      Name = "Sharper Sword", Desc = "+2 damage for everyone",   BaseMoney = 50, MaxLevel = 5 },
        new ShopItem { Id = SeedUpgrade,       Name = "Better Seeds",  Desc = "Crops grow 15% faster",    BaseMoney = 35, MaxLevel = 5 },
        new ShopItem { Id = FertilizerUpgrade, Name = "Fertilizer",    Desc = "+1 money per harvest",     BaseMoney = 40, MaxLevel = 10 },
        new ShopItem { Id = Bandages,          Name = "Bandages",      Desc = "Heal all living players",  BaseMoney = 25, MaxLevel = 0 },
        new ShopItem { Id = ScrapBundle,       Name = "Scrap Bundle",  Desc = "Get 3 scrap for crafting", BaseMoney = 25, MaxLevel = 0 },
    };

    public static readonly CraftItem[] Craft =
    {
        new CraftItem { Id = 0, Name = "Fence",  Desc = "Blocks monsters until they chew through it", Money = 0,  Scrap = 2, PrefabName = "Fence",  MaxInWorld = 50 },
        new CraftItem { Id = 1, Name = "Turret", Desc = "Auto-shoots nearby monsters",                Money = 25, Scrap = 6, PrefabName = "Turret", MaxInWorld = 4 },
        new CraftItem { Id = 2, Name = "Scarecrow", Desc = "Scares each nearby monster away for 10 seconds", Money = 15, Scrap = 4, PrefabName = "Scarecrow", MaxInWorld = 1 },
    };

    public static int ShopPrice(ShopItem item, int level)
    {
        return item.MaxLevel > 0 ? item.BaseMoney * (level + 1) : item.BaseMoney;
    }
}
