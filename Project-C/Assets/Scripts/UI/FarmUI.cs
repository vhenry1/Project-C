using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Everything on screen in the Farm: HUD (health bar, money, scrap, night, timer, prompts),
/// pause menu, shop menu, crafting menu and the game over / name entry menu.
/// Built automatically by ProjectCRuntime when the Farm scene loads.
/// </summary>
public class FarmUI : MonoBehaviour
{
    public static FarmUI Instance { get; private set; }

    // True while any menu is open, so the player stops walking and attacking.
    public static bool Blocking { get; private set; }

    private Canvas canvas;
    private GameObject pause, shop, craft, gameOver;
    private RectTransform shopContent, craftContent;
    private TMP_Text moneyText, scrapText, nightText, timerText, promptText, toastText, goalText, shopMoney, craftMoney, overTitle, overHint;
    private Image healthFill, nightTint;
    private TMP_Text healthText;
    private TMP_InputField nameInput;
    private Button submitButton, lobbyButton;
    private GameObject nameRow;

    private float toastUntil;
    private bool lastNight, phaseKnown;
    private string shopSig = "", craftSig = "";
    private bool scoreSaved;

    private void Awake()
    {
        Instance = this;
        Blocking = false;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        Blocking = false;
    }

    private void Start()
    {
        UIKit.HideOriginalCanvases();
        canvas = UIKit.MakeCanvas("ProjectC_UI");
        BuildHud();
        BuildPause();
        BuildShop();
        BuildCraft();
        BuildGameOver();
    }

    // ------------------------------------------------------------ HUD

    private void BuildHud()
    {
        nightTint = UIKit.Box(canvas.transform, "NightTint", new Color(0.05f, 0.05f, 0.25f, 0f), false);
        UIKit.Fill(nightTint.rectTransform);
        nightTint.raycastTarget = false;

        Image top = UIKit.Box(canvas.transform, "TopBar", new Color(UIKit.Panel.r, UIKit.Panel.g, UIKit.Panel.b, 0.88f));
        UIKit.Anchor(top.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(16, -76), new Vector2(-16, -12));
        top.raycastTarget = false;

        Image back = UIKit.Box(top.transform, "HealthBack", new Color(0.35f, 0.2f, 0.2f));
        UIKit.Anchor(back.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(16, -16), new Vector2(276, 16));
        healthFill = UIKit.Box(back.transform, "HealthFill", UIKit.Rose);
        UIKit.Fill(healthFill.rectTransform);
        healthFill.type = Image.Type.Filled;
        healthFill.sprite = null; // plain filled bar
        healthFill.fillMethod = Image.FillMethod.Horizontal;
        healthText = UIKit.Label(back.transform, "", 20, Color.white);
        UIKit.Fill(healthText.rectTransform);

        moneyText = HudText(top.transform, 0.30f, "Money 0");
        scrapText = HudText(top.transform, 0.45f, "Scrap 0");
        nightText = HudText(top.transform, 0.62f, "Day 1");
        timerText = HudText(top.transform, 0.80f, "0:00");

        promptText = UIKit.Label(canvas.transform, "", 30, Color.white);
        UIKit.Anchor(promptText.rectTransform, new Vector2(0.2f, 0), new Vector2(0.8f, 0), new Vector2(0, 50), new Vector2(0, 100));
        AddShadow(promptText);

        toastText = UIKit.Label(canvas.transform, "", 30, UIKit.Gold);
        UIKit.Anchor(toastText.rectTransform, new Vector2(0.1f, 1), new Vector2(0.9f, 1), new Vector2(0, -150), new Vector2(0, -90));
        AddShadow(toastText);

        goalText = UIKit.Label(canvas.transform, "WASD move  |  Shift run  |  Space attack  |  E interact  |  Esc pause", 18, new Color(1, 1, 1, 0.8f), TextAlignmentOptions.Center, FontStyles.Normal);
        UIKit.Anchor(goalText.rectTransform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 8), new Vector2(0, 36));
        AddShadow(goalText);
    }

    private TMP_Text HudText(Transform parent, float x, string text)
    {
        TMP_Text t = UIKit.Label(parent, text, 28, UIKit.Ink);
        t.rectTransform.anchorMin = new Vector2(x, 0); t.rectTransform.anchorMax = new Vector2(x + 0.16f, 1);
        t.rectTransform.offsetMin = t.rectTransform.offsetMax = Vector2.zero;
        return t;
    }

    private static void AddShadow(TMP_Text t)
    {
        t.fontMaterial.EnableKeyword("UNDERLAY_ON");
        t.fontMaterial.SetColor("_UnderlayColor", new Color(0, 0, 0, 0.8f));
        t.fontMaterial.SetFloat("_UnderlayOffsetX", 0.6f);
        t.fontMaterial.SetFloat("_UnderlayOffsetY", -0.6f);
    }

    public void Toast(string message, float seconds = 3f)
    {
        if (toastText == null) return;
        toastText.text = message;
        toastUntil = Time.time + seconds;
    }

    // ------------------------------------------------------------ Menus

    private void BuildPause()
    {
        RectTransform c = UIKit.MakePanel(canvas.transform, "Paused", new Vector2(480, 440), out pause);
        UIKit.MakeButton(c, "Resume", UIKit.Green, () => SetOpen(pause, false), new Vector2(340, 64));
        UIKit.MakeButton(c, "Quit To Menu", UIKit.Peach, () => ProjectCRuntime.Instance.QuitToMenu(), new Vector2(340, 64));
        TMP_Text note = UIKit.Label(c, "The farm keeps running while paused online.", 20, UIKit.Ink, TextAlignmentOptions.Center, FontStyles.Italic);
        note.rectTransform.sizeDelta = new Vector2(400, 50);
        pause.SetActive(false);
    }

    private void BuildShop()
    {
        shopContent = UIKit.MakePanel(canvas.transform, "Shop", new Vector2(700, 560), out shop);
        shopContent.GetComponent<VerticalLayoutGroup>().spacing = 8;
        shop.SetActive(false);
    }

    private void BuildCraft()
    {
        craftContent = UIKit.MakePanel(canvas.transform, "Crafting Bench", new Vector2(760, 440), out craft);
        craft.SetActive(false);
    }

    private void BuildGameOver()
    {
        RectTransform c = UIKit.MakePanel(canvas.transform, "Game Over", new Vector2(640, 560), out gameOver);
        overTitle = UIKit.Label(c, "", 34, UIKit.Ink);
        overTitle.rectTransform.sizeDelta = new Vector2(560, 90);

        nameRow = new GameObject("NameRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        nameRow.transform.SetParent(c, false);
        ((RectTransform)nameRow.transform).sizeDelta = new Vector2(560, 56);
        HorizontalLayoutGroup h = nameRow.GetComponent<HorizontalLayoutGroup>();
        h.spacing = 10; h.childControlWidth = false; h.childControlHeight = false; h.childAlignment = TextAnchor.MiddleCenter;

        Image bg = UIKit.Box(nameRow.transform, "NameInput", Color.white);
        bg.rectTransform.sizeDelta = new Vector2(300, 56);
        RectTransform area = UIKit.Rect("TextArea", bg.transform);
        UIKit.Anchor(area, Vector2.zero, Vector2.one, new Vector2(12, 6), new Vector2(-12, -6));
        area.gameObject.AddComponent<RectMask2D>();
        TMP_Text text = UIKit.Label(area, "", 28, UIKit.Ink, TextAlignmentOptions.Center);
        UIKit.Fill(text.rectTransform);
        TMP_Text ph = UIKit.Label(area, "Your name", 26, new Color(0.6f, 0.5f, 0.45f), TextAlignmentOptions.Center, FontStyles.Italic);
        UIKit.Fill(ph.rectTransform);
        nameInput = bg.gameObject.AddComponent<TMP_InputField>();
        nameInput.textViewport = area; nameInput.textComponent = text; nameInput.placeholder = ph;
        nameInput.characterLimit = 12;

        submitButton = UIKit.MakeButton(nameRow.transform, "Save Score", UIKit.Gold, SubmitScore, new Vector2(240, 56));

        overHint = UIKit.Label(c, "", 22, UIKit.Ink, TextAlignmentOptions.Center, FontStyles.Italic);
        overHint.rectTransform.sizeDelta = new Vector2(540, 40);

        lobbyButton = UIKit.MakeButton(c, "Back To Lobby (host)", UIKit.Green, () => ProjectCRuntime.Instance.ReturnToLobby(), new Vector2(380, 60));
        UIKit.MakeButton(c, "Quit To Menu", UIKit.Peach, () => ProjectCRuntime.Instance.QuitToMenu(), new Vector2(380, 60));
        gameOver.SetActive(false);
    }

    private async void SubmitScore()
    {
        if (scoreSaved) return;
        string n = string.IsNullOrWhiteSpace(nameInput.text) ? "Farmer" : nameInput.text.Trim();
        int nights = GameManager.Instance != null ? GameManager.Instance.FinalNights.Value : 0;
        scoreSaved = true;
        submitButton.interactable = false;
        HighScores.AddLocal(n, nights);
        overHint.text = "Saved to this computer.";

        if (SessionManager.Instance != null && await SessionManager.Instance.InitAsync())
        {
            await HighScores.SubmitGlobalAsync(n, nights);
            if (this != null && overHint != null) overHint.text = "Saved locally and to the global board.";
        }
    }

    public void OpenShop() { if (!Blocking) { SetOpen(shop, true); AudioManager.Play("click", 0.4f); shopSig = ""; } }
    public void OpenCrafting() { if (!Blocking) { SetOpen(craft, true); AudioManager.Play("click", 0.4f); craftSig = ""; } }

    private void SetOpen(GameObject panel, bool open)
    {
        panel.SetActive(open);
        Blocking = pause.activeSelf || shop.activeSelf || craft.activeSelf || gameOver.activeSelf;
        if (open) PlayerInteractor.CurrentPrompt = "";
    }

    // ------------------------------------------------------------ Update

    private void Update()
    {
        if (canvas == null) return;

        UpdateHud();
        UpdateMenus();

        Keyboard kb = Keyboard.current;
        if (kb != null && kb.escapeKey.wasPressedThisFrame && !gameOver.activeSelf)
        {
            if (shop.activeSelf) SetOpen(shop, false);
            else if (craft.activeSelf) SetOpen(craft, false);
            else SetOpen(pause, !pause.activeSelf);
        }

        bool over = GameManager.IsOver;
        if (over && !gameOver.activeSelf)
        {
            pause.SetActive(false); shop.SetActive(false); craft.SetActive(false);
            SetOpen(gameOver, true);
            int nights = GameManager.Instance.FinalNights.Value;
            overTitle.text = "The farm has fallen.\nYou survived " + nights + (nights == 1 ? " night." : " nights.");
            scoreSaved = false;
            submitButton.interactable = true;
            overHint.text = nights >= GameSettings.GoalNights ? "You reached the goal! Add your name to the board." : "Add your name to the high score board.";
            AudioManager.Play("down", 0.7f);
        }
        if (gameOver.activeSelf)
        {
            NetworkManager nm = NetworkManager.Singleton;
            lobbyButton.gameObject.SetActive(nm != null && nm.IsServer);
        }
    }

    private void UpdateHud()
    {
        TeamWallet w = TeamWallet.Instance;
        if (w != null)
        {
            moneyText.text = "Money " + w.Money.Value;
            scrapText.text = "Scrap " + w.Scrap.Value;
        }

        TimeController t = TimeController.Instance;
        if (t != null)
        {
            bool night = t.IsNight.Value;
            int rem = Mathf.CeilToInt(t.PhaseTimeRemaining);
            nightText.text = night ? "Night " + (t.NightsSurvived.Value + 1) : "Day " + (t.NightsSurvived.Value + 1);
            timerText.text = (rem / 60) + ":" + (rem % 60).ToString("00");
            nightTint.color = new Color(0.05f, 0.05f, 0.25f, Mathf.MoveTowards(nightTint.color.a, night ? 0.42f : 0f, Time.deltaTime * 0.5f));

            if (!phaseKnown || night != lastNight)
            {
                if (phaseKnown) { AudioManager.Play(night ? "nightfall" : "dawn", 0.6f); Toast(night ? "Night falls. Defend the farm!" : "Morning! Time to farm and shop."); }
                AudioManager.Music(night);
                lastNight = night; phaseKnown = true;
            }
        }

        NetworkManager nm = NetworkManager.Singleton;
        if (nm != null && nm.LocalClient != null && nm.LocalClient.PlayerObject != null)
        {
            Health h = nm.LocalClient.PlayerObject.GetComponent<Health>();
            if (h != null)
            {
                healthFill.fillAmount = h.Fraction;
                healthText.text = h.IsDead ? "DOWN - wait for a revive or dawn" : h.Current.Value + " / " + h.Max.Value;
            }
        }

        promptText.text = Blocking ? "" : PlayerInteractor.CurrentPrompt;
        toastText.gameObject.SetActive(Time.time < toastUntil);
    }

    private void UpdateMenus()
    {
        TeamWallet w = TeamWallet.Instance;
        if (w == null) return;

        if (shop.activeSelf)
        {
            string sig = w.Money.Value + "|" + w.SwordLevel.Value + "|" + w.GrowthLevel.Value + "|" + w.YieldLevel.Value;
            if (sig != shopSig) { shopSig = sig; RebuildShop(w); }
        }
        if (craft.activeSelf)
        {
            string sig = w.Money.Value + "|" + w.Scrap.Value;
            if (sig != craftSig) { craftSig = sig; RebuildCraft(w); }
        }
    }

    private void RebuildShop(TeamWallet w)
    {
        UIKit.Clear(shopContent);
        TMP_Text money = UIKit.Label(shopContent, "Team money: " + w.Money.Value, 24, UIKit.Ink);
        money.rectTransform.sizeDelta = new Vector2(480, 32);

        foreach (ShopItem item in Catalog.Shop)
        {
            int level = item.Id == Catalog.SwordUpgrade ? w.SwordLevel.Value
                      : item.Id == Catalog.SeedUpgrade ? w.GrowthLevel.Value
                      : item.Id == Catalog.FertilizerUpgrade ? w.YieldLevel.Value : 0;
            bool maxed = item.MaxLevel > 0 && level >= item.MaxLevel;
            int price = Catalog.ShopPrice(item, level);
            string label = item.Name + (item.MaxLevel > 0 ? " (Lv " + level + "/" + item.MaxLevel + ")" : "") + " - " +
                           (maxed ? "MAX" : "$" + price) + "\n<size=18>" + item.Desc + "</size>";
            Button b = UIKit.MakeButton(shopContent, label, UIKit.Green, null, new Vector2(480, 60));
            int id = item.Id;
            b.onClick.AddListener(() => TeamWallet.Instance.BuyServerRpc(id));
            b.interactable = !maxed && w.Money.Value >= price;
        }
        UIKit.MakeButton(shopContent, "Close", UIKit.Peach, () => SetOpen(shop, false), new Vector2(220, 48));
    }

    private void RebuildCraft(TeamWallet w)
    {
        UIKit.Clear(craftContent);
        TMP_Text money = UIKit.Label(craftContent, "Money: " + w.Money.Value + "    Scrap: " + w.Scrap.Value, 28, UIKit.Ink);
        money.rectTransform.sizeDelta = new Vector2(640, 36);

        foreach (CraftItem item in Catalog.Craft)
        {
            string cost = (item.Money > 0 ? "$" + item.Money + " + " : "") + item.Scrap + " scrap";
            string label = "Build " + item.Name + " - " + cost + "\n<size=18>" + item.Desc + "</size>";
            Button b = UIKit.MakeButton(craftContent, label, UIKit.Sky, null, new Vector2(640, 72));
            int id = item.Id;
            b.onClick.AddListener(() =>
            {
                NetworkManager nm = NetworkManager.Singleton;
                SpriteRenderer sr = nm.LocalClient.PlayerObject.GetComponent<SpriteRenderer>();
                TeamWallet.Instance.CraftServerRpc(id, sr != null && sr.flipX ? -1f : 1f);
            });
            b.interactable = w.Money.Value >= item.Money && w.Scrap.Value >= item.Scrap;
        }
        UIKit.MakeButton(craftContent, "Close", UIKit.Peach, () => SetOpen(craft, false), new Vector2(240, 56));
    }
}
