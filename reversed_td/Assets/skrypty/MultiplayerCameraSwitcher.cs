using Mirror;
using System.Collections;
using UnityEngine;

public class MultiplayerCameraSwitcher : MonoBehaviour
{
    [Header("Kamery graczy")]
    public GameObject cameraPlayer1;
    public GameObject cameraPlayer2;

    private Camera        _myCam;
    private AudioListener _myListener;
    private Camera        _enemyCam;
    private AudioListener _enemyListener;

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
    }

    void Update()
    {
        if (_myCam == null || _enemyCam == null) return;
        if (!Input.GetKeyDown(KeyCode.Tab)) return;

        bool showingMine = _myCam.enabled;
        SetCameraActive(_myCam,    _myListener,    !showingMine);
        SetCameraActive(_enemyCam, _enemyListener,  showingMine);
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
