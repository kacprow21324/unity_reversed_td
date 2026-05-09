using System.Collections;
using Mirror;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// Top bar UI: [MójNick][Serca] | [Timer] | [Serca][NickPrzeciwnika]
/// Referencje ustawiane przez Tools/Generuj Match HP UI lub tworzone dynamicznie w Start().
public class MatchHPUI : MonoBehaviour
{
    public static MatchHPUI Instance { get; private set; }

    [Header("Elementy paska (wired przez builder)")]
    public TextMeshProUGUI myNameText;
    public TextMeshProUGUI myHeartsText;
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI enemyHeartsText;
    public TextMeshProUGUI enemyNameText;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        if (NetworkManager.singleton == null || !NetworkManager.singleton.isNetworkActive)
        {
            gameObject.SetActive(false);
            return;
        }

        // Fallback: builder nie był uruchomiony — generuj UI w runtime
        if (myNameText == null)
            BuildUI();

        StartCoroutine(InitialRefresh());
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public static void EnsureExists()
    {
        if (Instance == null)
            new GameObject("MatchHPUI").AddComponent<MatchHPUI>();
    }

    // ── Inicjalizacja ─────────────────────────────────────────────────────

    IEnumerator InitialRefresh()
    {
        while (NetworkClient.localPlayer == null)
            yield return null;

        // Czekaj aż obaj gracze załadują scenę i ich nick SyncVar dotrze
        NetworkPlayer[] players;
        do
        {
            yield return null;
            players = FindObjectsByType<NetworkPlayer>(FindObjectsSortMode.None);
        }
        while (players.Length < 2);

        yield return null; // jeden dodatkowy frame na synchronizację SyncVarów
        RefreshHP();
        if (NetworkMatchManager.Instance != null)
            UpdateTimer(NetworkMatchManager.Instance.preparationTime);
    }

    // ── Dynamiczne tworzenie UI (fallback bez buildera) ───────────────────

    void BuildUI()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null) return;

        var bar = new GameObject("MatchHPBar");
        bar.transform.SetParent(canvas.transform, false);
        bar.transform.SetAsFirstSibling();

        var barRT       = bar.AddComponent<RectTransform>();
        barRT.anchorMin = new Vector2(0f, 1f);
        barRT.anchorMax = new Vector2(1f, 1f);
        barRT.pivot     = new Vector2(0.5f, 1f);
        barRT.offsetMin = Vector2.zero;
        barRT.offsetMax = Vector2.zero;
        barRT.sizeDelta = new Vector2(0f, 48f);

        var bg   = bar.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.72f);

        var hlg = bar.AddComponent<HorizontalLayoutGroup>();
        hlg.childForceExpandWidth  = true;
        hlg.childForceExpandHeight = true;
        hlg.childControlWidth      = true;
        hlg.childControlHeight     = true;
        hlg.padding                = new RectOffset(12, 12, 6, 6);
        hlg.spacing                = 8f;

        // Lewa sekcja
        var leftGO  = new GameObject("Left");
        leftGO.transform.SetParent(bar.transform, false);
        var leftHLG = leftGO.AddComponent<HorizontalLayoutGroup>();
        leftHLG.childAlignment        = TextAnchor.MiddleLeft;
        leftHLG.childForceExpandWidth  = false;
        leftHLG.childForceExpandHeight = true;
        leftHLG.childControlWidth      = true;
        leftHLG.childControlHeight     = true;
        leftHLG.spacing                = 10f;
        leftGO.AddComponent<LayoutElement>().flexibleWidth = 1f;

        myNameText   = MakeLabel(leftGO.transform, "MyName",   "Gracz",  16f, Color.white,                  TextAlignmentOptions.Left);
        myHeartsText = MakeLabel(leftGO.transform, "MyHearts", "♥♥♥",   20f, new Color(0.9f, 0.15f, 0.15f), TextAlignmentOptions.Left);

        // Środkowa sekcja
        var centerGO = new GameObject("Center");
        centerGO.transform.SetParent(bar.transform, false);
        centerGO.AddComponent<LayoutElement>().minWidth = 80f;
        timerText            = MakeLabel(centerGO.transform, "Timer", "60", 24f, new Color(1f, 0.88f, 0.15f), TextAlignmentOptions.Center);
        timerText.fontStyle  = FontStyles.Bold;

        // Prawa sekcja
        var rightGO  = new GameObject("Right");
        rightGO.transform.SetParent(bar.transform, false);
        var rightHLG = rightGO.AddComponent<HorizontalLayoutGroup>();
        rightHLG.childAlignment        = TextAnchor.MiddleRight;
        rightHLG.childForceExpandWidth  = false;
        rightHLG.childForceExpandHeight = true;
        rightHLG.childControlWidth      = true;
        rightHLG.childControlHeight     = true;
        rightHLG.spacing                = 10f;
        rightGO.AddComponent<LayoutElement>().flexibleWidth = 1f;

        enemyHeartsText = MakeLabel(rightGO.transform, "EnemyHearts", "♥♥♥",    20f, new Color(0.9f, 0.15f, 0.15f), TextAlignmentOptions.Right);
        enemyNameText   = MakeLabel(rightGO.transform, "EnemyName",   "Gracz 2", 16f, Color.white,                  TextAlignmentOptions.Right);
    }

    static TextMeshProUGUI MakeLabel(Transform parent, string goName, string text,
        float fontSize, Color color, TextAlignmentOptions align)
    {
        var go  = new GameObject(goName);
        go.transform.SetParent(parent, false);
        go.AddComponent<LayoutElement>().flexibleWidth = 1f;

        var tmp           = go.AddComponent<TextMeshProUGUI>();
        tmp.text          = text;
        tmp.fontSize      = fontSize;
        tmp.color         = color;
        tmp.fontStyle     = FontStyles.Bold;
        tmp.alignment     = align;
        tmp.raycastTarget = false;
        return tmp;
    }

    // ── Publiczne API ─────────────────────────────────────────────────────

    public void RefreshHP()
    {
        var nmm = NetworkMatchManager.Instance;
        if (nmm == null) return;

        var localPlayer = NetworkClient.localPlayer?.GetComponent<NetworkPlayer>();
        if (localPlayer == null) return;

        int myIdx   = localPlayer.playerIndex;
        int myHP    = myIdx == 1 ? nmm.player1HP : nmm.player2HP;
        int enemyHP = myIdx == 1 ? nmm.player2HP : nmm.player1HP;

        string myName    = localPlayer.playerNickname;
        string enemyName = "Gracz " + (myIdx == 1 ? 2 : 1);

        foreach (var p in FindObjectsByType<NetworkPlayer>(FindObjectsSortMode.None))
        {
            if (p != localPlayer) { enemyName = p.playerNickname; break; }
        }

        if (myNameText      != null) myNameText.text      = myName;
        if (myHeartsText    != null) myHeartsText.text    = HeartsString(myHP);
        if (enemyNameText   != null) enemyNameText.text   = enemyName;
        if (enemyHeartsText != null) enemyHeartsText.text = HeartsString(enemyHP);
    }

    public void UpdateTimer(float time)
    {
        if (timerText == null) return;
        int secs      = Mathf.CeilToInt(Mathf.Max(0f, time));
        timerText.text  = secs > 0 ? secs.ToString() : "-";
        timerText.color = secs <= 10 && secs > 0
            ? new Color(1f, 0.3f, 0.2f)
            : new Color(1f, 0.88f, 0.15f);
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    static string HeartsString(int hp)
    {
        hp = Mathf.Clamp(hp, 0, 3);
        string filled = hp > 0 ? $"<color=#E03030>{new string('♥', hp)}</color>"    : "";
        string empty  = hp < 3 ? $"<color=#444444>{new string('♥', 3 - hp)}</color>" : "";
        return filled + empty;
    }
}
