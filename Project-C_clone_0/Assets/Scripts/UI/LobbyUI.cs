using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Lobby scene: host online (room code), join with a code, or play solo/local,
/// then pick game mode options (difficulty, max players) before the host starts the farm.
/// This is the game mode select menu.
/// </summary>
public class LobbyUI : MonoBehaviour
{
    private GameObject choose, room;
    private TMP_Text status, codeText, playersText, hintText;
    private TMP_InputField codeInput;
    private Button startButton, diffButton, maxButton;
    private bool busy;
    private LobbyHostStarter starter;

    private void Start()
    {
        UIKit.HideOriginalCanvases();
        starter = FindFirstObjectByType<LobbyHostStarter>();
        if (starter == null) starter = gameObject.AddComponent<LobbyHostStarter>();
        Canvas canvas = UIKit.MakeCanvas("ProjectC_UI");
        AudioManager.Music(false);

        RectTransform c = UIKit.MakePanel(canvas.transform, "Lobby", new Vector2(520, 520), out choose);
        UIKit.MakeButton(c, "Host Online Game", UIKit.Green, HostOnline, new Vector2(440, 64));
        UIKit.MakeButton(c, "Play Solo / Same PC", UIKit.Gold, HostLocal, new Vector2(440, 64));
        codeInput = MakeInput(c);
        UIKit.MakeButton(c, "Join With Room Code", UIKit.Sky, JoinOnline, new Vector2(440, 64));
        UIKit.MakeButton(c, "Join Local Host (127.0.0.1)", UIKit.Sky, JoinLocal, new Vector2(440, 50));
        UIKit.MakeButton(c, "Back", UIKit.Peach, () => SceneManager.LoadScene("Menu"), new Vector2(240, 50));
        status = UIKit.Label(c, "", 22, UIKit.Ink, TextAlignmentOptions.Center, FontStyles.Italic);
        status.rectTransform.sizeDelta = new Vector2(520, 50);

        RectTransform r = UIKit.MakePanel(canvas.transform, "Game Room", new Vector2(520, 520), out room);
        codeText = UIKit.Label(r, "", 34, UIKit.Ink);
        codeText.rectTransform.sizeDelta = new Vector2(520, 50);
        playersText = UIKit.Label(r, "", 26, UIKit.Ink, TextAlignmentOptions.Center, FontStyles.Normal);
        playersText.rectTransform.sizeDelta = new Vector2(520, 36);
        diffButton = UIKit.MakeButton(r, "", UIKit.Gold, CycleDifficulty, new Vector2(440, 60));
        maxButton = UIKit.MakeButton(r, "", UIKit.Gold, CycleMaxPlayers, new Vector2(440, 60));
        startButton = UIKit.MakeButton(r, "Start Farm", UIKit.Green, () => starter.startFarm(), new Vector2(440, 70));
        UIKit.MakeButton(r, "Leave", UIKit.Peach, Leave, new Vector2(240, 50));
        hintText = UIKit.Label(r, "", 22, UIKit.Ink, TextAlignmentOptions.Center, FontStyles.Italic);
        hintText.rectTransform.sizeDelta = new Vector2(520, 36);
        room.SetActive(false);

        RefreshModeButtons();

        NetworkManager nm = NetworkManager.Singleton;
        if (nm != null && nm.IsListening) EnterRoom();
    }

    private TMP_InputField MakeInput(Transform parent)
    {
        Image bg = UIKit.Box(parent, "CodeInput", Color.white);
        bg.rectTransform.sizeDelta = new Vector2(440, 56);
        LayoutElement le = bg.gameObject.AddComponent<LayoutElement>();
        le.preferredHeight = 56; le.preferredWidth = 440;

        RectTransform area = UIKit.Rect("TextArea", bg.transform);
        UIKit.Anchor(area, Vector2.zero, Vector2.one, new Vector2(14, 6), new Vector2(-14, -6));
        area.gameObject.AddComponent<RectMask2D>();

        TMP_Text text = UIKit.Label(area, "", 28, UIKit.Ink, TextAlignmentOptions.Center);
        UIKit.Fill(text.rectTransform);
        TMP_Text ph = UIKit.Label(area, "Enter room code", 26, new Color(0.6f, 0.5f, 0.45f), TextAlignmentOptions.Center, FontStyles.Italic);
        UIKit.Fill(ph.rectTransform);

        TMP_InputField input = bg.gameObject.AddComponent<TMP_InputField>();
        input.textViewport = area;
        input.textComponent = text;
        input.placeholder = ph;
        input.characterLimit = 8;
        input.onValidateInput += (s, i, ch) => char.ToUpperInvariant(ch);
        return input;
    }

    private async void HostOnline()
    {
        if (busy) return;
        busy = true; status.text = "Connecting to online services...";
        SessionManager sm = SessionManager.Instance;
        if (!await sm.InitAsync() || !await sm.HostAsync(GameSettings.MaxPlayers))
        {
            status.text = "Could not host online. Try 'Play Solo / Same PC' (" + Short(sm.LastError) + ")";
            busy = false; return;
        }
        busy = false;
        EnterRoom();
    }

    private async void JoinOnline()
    {
        if (busy) return;
        if (string.IsNullOrWhiteSpace(codeInput.text)) { status.text = "Type a room code first."; return; }
        busy = true; status.text = "Joining...";
        SessionManager sm = SessionManager.Instance;
        if (!await sm.InitAsync() || !await sm.JoinAsync(codeInput.text))
        {
            status.text = "Could not join (" + Short(sm.LastError) + ")";
            busy = false; return;
        }
        busy = false;
        EnterRoom();
    }

    // Solo / local: a plain Netcode host on this PC. A second window can join 127.0.0.1.
    private void HostLocal()
    {
        NetworkManager nm = NetworkManager.Singleton;
        if (nm == null) { status.text = "No NetworkManager in this scene."; return; }
        if (!nm.IsListening)
        {
            UnityTransport utp = nm.GetComponent<UnityTransport>();
            if (utp == null) { status.text = "No Unity Transport in this scene."; return; }
            utp.SetConnectionData("127.0.0.1", 7777);
            if (!nm.StartHost()) { status.text = "Could not start a local host."; return; }
        }
        EnterRoom();
    }

    private void JoinLocal()
    {
        NetworkManager nm = NetworkManager.Singleton;
        if (nm == null) { status.text = "No NetworkManager in this scene."; return; }
        UnityTransport utp = nm.GetComponent<UnityTransport>();
        if (utp == null) { status.text = "No Unity Transport in this scene."; return; }
        utp.SetConnectionData("127.0.0.1", 7777);
        if (!nm.StartClient()) { status.text = "Could not start the client."; return; }
        EnterRoom();
    }

    private void EnterRoom()
    {
        choose.SetActive(false);
        room.SetActive(true);
    }

    private async void Leave()
    {
        if (SessionManager.Instance != null) await SessionManager.Instance.LeaveAsync();
        NetworkManager nm = NetworkManager.Singleton;
        if (nm != null && nm.IsListening) nm.Shutdown();
        if (this == null) return;
        room.SetActive(false);
        choose.SetActive(true);
        status.text = "";
    }

    private void CycleDifficulty()
    {
        GameSettings.Current = (Difficulty)(((int)GameSettings.Current + 1) % 3);
        RefreshModeButtons();
    }

    private void CycleMaxPlayers()
    {
        GameSettings.MaxPlayers = GameSettings.MaxPlayers >= 4 ? 1 : GameSettings.MaxPlayers + 1;
        RefreshModeButtons();
    }

    private void RefreshModeButtons()
    {
        diffButton.GetComponentInChildren<TMP_Text>().text = "Difficulty: " + GameSettings.Current;
        maxButton.GetComponentInChildren<TMP_Text>().text = "Max players: " + GameSettings.MaxPlayers;
    }

    private static string Short(string s) { return string.IsNullOrEmpty(s) ? "unknown error" : (s.Length > 60 ? s.Substring(0, 60) + "..." : s); }

    private bool HasSession() { return SessionManager.Instance != null && SessionManager.Instance.Session != null; }

    private void Update()
    {
        NetworkManager nm = NetworkManager.Singleton;
        if (room == null || !room.activeSelf || nm == null) return;

        bool host = nm.IsServer;
        int players = nm.IsListening ? nm.ConnectedClientsIds.Count : 0;
        string code = SessionManager.Instance != null ? SessionManager.Instance.JoinCode : "";

        codeText.text = !string.IsNullOrEmpty(code) ? "Room code: " + code : (host ? "Local game" : "Connected");
        playersText.text = host ? "Players here: " + players : "Waiting for the host to start...";
        startButton.gameObject.SetActive(host);
        diffButton.interactable = host;
        maxButton.interactable = host && !HasSession();
        hintText.text = host && HasSession() ? "Share the room code with friends." : "";
    }
}
