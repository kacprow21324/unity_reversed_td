using Mirror;
using System.Collections;
using TMPro;
using UnityEngine;

public class MultiplayerCameraSwitcher : MonoBehaviour
{
    public static MultiplayerCameraSwitcher Instance { get; private set; }

    [Header("Kamery graczy")]
    public GameObject cameraPlayer1;
    public GameObject cameraPlayer2;

    [Header("Etykieta planszy (opcjonalna — tworzona automatycznie jeśli pusta)")]
    public TextMeshProUGUI boardLabel;

    private Camera        _myCam;
    private AudioListener _myListener;
    private Camera        _enemyCam;
    private AudioListener _enemyListener;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        // Wyłącz oba AudioListenery natychmiast — przed coroutine, która czeka
        // na localPlayer. Zapobiega błędowi "2 AudioListeners in the scene"
        // w klatkach, gdy sieć jeszcze nie przypisała tożsamości gracza.
        SilenceAllListeners();

        StartCoroutine(SetupCameras());
    }

    IEnumerator SetupCameras()
    {
        while (NetworkClient.localPlayer == null)
            yield return null;

        NetworkPlayer player = NetworkClient.localPlayer.GetComponent<NetworkPlayer>();
        if (player == null)
        {
            Debug.LogError("[MultiplayerCameraSwitcher] Brak komponentu NetworkPlayer na localPlayer.");
            yield break;
        }

        GameObject myCameraGO    = player.playerIndex == 1 ? cameraPlayer1 : cameraPlayer2;
        GameObject enemyCameraGO = player.playerIndex == 1 ? cameraPlayer2 : cameraPlayer1;

        _myCam         = myCameraGO.GetComponent<Camera>();
        _myListener    = myCameraGO.GetComponent<AudioListener>();
        _enemyCam      = enemyCameraGO.GetComponent<Camera>();
        _enemyListener = enemyCameraGO.GetComponent<AudioListener>();

        SetCameraActive(_myCam,    _myListener,    true);
        SetCameraActive(_enemyCam, _enemyListener, false);

        Debug.Log($"[MultiplayerCameraSwitcher] Gracz {player.playerIndex} — aktywna kamera: {myCameraGO.name}");

        BuildBoardLabelIfMissing();
        UpdateBoardLabel(true);
    }

    void Update()
    {
        if (_myCam == null || _enemyCam == null) return;
        if (!Input.GetKeyDown(KeyCode.Tab)) return;

        bool showingMine = _myCam.enabled;
        SetCameraActive(_myCam,    _myListener,    !showingMine);
        SetCameraActive(_enemyCam, _enemyListener,  showingMine);
        UpdateBoardLabel(!showingMine);
    }

    void UpdateBoardLabel(bool showingMine)
    {
        if (boardLabel == null) return;

        string myNick    = "Gracz";
        string enemyNick = "Gracz 2";

        var localPlayer = NetworkClient.localPlayer?.GetComponent<NetworkPlayer>();
        if (localPlayer != null)
        {
            myNick = localPlayer.playerNickname;
            foreach (var np in FindObjectsByType<NetworkPlayer>(FindObjectsSortMode.None))
                if (np != localPlayer) { enemyNick = np.playerNickname; break; }
        }

        boardLabel.text = showingMine
            ? $"<color=#0088FF>Plansza: {myNick}</color>"
            : $"<color=#FF3333>Plansza: {enemyNick}</color>";
    }

    void BuildBoardLabelIfMissing()
    {
        if (boardLabel != null) return;
        var canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null) return;

        var go = new GameObject("BoardLabel");
        go.transform.SetParent(canvas.transform, false);

        var rt              = go.AddComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0.5f, 1f);
        rt.anchorMax        = new Vector2(0.5f, 1f);
        rt.pivot            = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, -52f); // poniżej paska MatchHPUI (48px)
        rt.sizeDelta        = new Vector2(320f, 26f);

        boardLabel           = go.AddComponent<TextMeshProUGUI>();
        boardLabel.fontSize  = 15f;
        boardLabel.fontStyle = FontStyles.Bold;
        boardLabel.alignment = TextAlignmentOptions.Center;
        boardLabel.raycastTarget = false;
    }

    public void ResetCameraToOwnBoard()
    {
        if (_myCam == null || _enemyCam == null) return;
        SetCameraActive(_myCam,    _myListener,    true);
        SetCameraActive(_enemyCam, _enemyListener, false);
        UpdateBoardLabel(true);
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    void SilenceAllListeners()
    {
        DisableListener(cameraPlayer1);
        DisableListener(cameraPlayer2);
    }

    static void DisableListener(GameObject go)
    {
        if (go == null) return;
        var l = go.GetComponent<AudioListener>();
        if (l != null) l.enabled = false;
    }

    static void SetCameraActive(Camera cam, AudioListener listener, bool active)
    {
        if (cam      != null) cam.enabled      = active;
        if (listener != null) listener.enabled = active;
    }
}
