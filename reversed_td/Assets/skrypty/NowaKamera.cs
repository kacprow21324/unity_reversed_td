using UnityEngine;
using UnityEngine.EventSystems;

public enum CameraMode { Orbit, FreeFly }

/// STEROWANIE:
///   Prawy przycisk myszy  – Orbit: obrót poziomy + nachylenie góra/dół
///                         – FreeFly: FPS look (pitch + yaw)
///   Scroll myszy          – Orbit: zoom dystansu | FreeFly: zmiana wysokości Y
///   Shift                 – turbo  |  Alt – precyzja
///   Q                     – przełącz w tryb FreeFly
///   E                     – wróć do trybu Orbit (płynny powrót do pivota)
///   WSAD                  – lot w trybie FreeFly (płaszczyzna XZ)
///   C (przytrzymaj)       – Optifine Zoom (FOV → zoomedFOV)
///   F                     – reset nachylenia (Orbit)
///   R                     – pełny reset (Orbit)
[RequireComponent(typeof(Camera))]
public class AIEnhancedRTSCamera : MonoBehaviour
{
    // ── Tryb kamery ───────────────────────────────────────────────────────

    [Header("Tryb Kamery")]
    public CameraMode mode = CameraMode.Orbit;

    // ── Orbit ─────────────────────────────────────────────────────────────

    [Header("Orbit – Cel i Orientacja")]
    [Tooltip("Punkt środkowy mapy, wokół którego kamera się obraca. Puste = (0,0,0).")]
    public Transform pivotPoint;

    [Tooltip("Początkowe nachylenie kamery (stopnie od poziomu)")]
    [Range(5f, 89f)]
    public float startTiltAngle = 50f;

    [Tooltip("Minimalne nachylenie (prawie poziomo)")]
    [Range(5f, 45f)]
    public float minTiltAngle = 5f;

    [Tooltip("Maksymalne nachylenie (prawie pionowo – widok z góry)")]
    [Range(45f, 89f)]
    public float maxTiltAngle = 89f;

    [Header("Orbit – Zoom")]
    public float zoomSpeed       = 30f;
    public float minZoomDistance = 10f;
    public float maxZoomDistance = 100f;
    public float startZoomDistance = 40f;
    public float zoomSmoothing   = 8f;

    [Header("Orbit – Obrót i Nachylenie")]
    public float rotationSpeed   = 120f;
    public float tiltSpeed       = 80f;
    public float rotationSmoothing = 15f;

    // ── FreeFly ───────────────────────────────────────────────────────────

    [Header("FreeFly – Lot")]
    public float freeFlySpeed           = 20f;
    public float freeFlyTurboMultiplier = 3f;

    [Header("FreeFly – Czułość myszki (look)")]
    public float lookSensitivity = 2f;

    [Header("FreeFly – Granice mapy (4 Cube'y)")]
    [Tooltip("4 obiekty wyznaczające rogi mapy. Bounding box liczony automatycznie. Puste = Start() szuka Cube(1)..Cube(4).")]
    public Transform[] boundCubes = new Transform[4];

    [Header("FreeFly – Limity wysokości")]
    public float limitMinY = 10f;
    public float limitMaxY = 80f;

    [Header("FreeFly – Scroll → zmiana wysokości")]
    public float flyScrollSpeed = 15f;

    // ── Zoom Optifine ─────────────────────────────────────────────────────

    [Header("Zoom Optifine (klawisz C)")]
    public float zoomedFOV   = 20f;
    public float fovSmoothing = 10f;

    // ── Wspólne mnożniki ──────────────────────────────────────────────────

    [Header("Mnożniki prędkości (Shift / Alt)")]
    public float turboMultiplier = 2.5f;
    public float slowMultiplier  = 0.3f;

    // ── Stan prywatny ─────────────────────────────────────────────────────

    // Bounds
    private float _minX, _maxX, _minZ, _maxZ;
    private bool  _boundsReady;

    // Orbit
    private float _currentZoom;
    private float _targetZoom;
    private float _currentRotationY;
    private float _targetRotationY;
    private float _currentTilt;
    private float _targetTilt;
    private Vector3 _fallbackPivot;

    // Orbit → powrót
    private bool       _returningToOrbit;
    private Vector3    _orbitReturnPos;
    private Quaternion _orbitReturnRot;

    // FreeFly look
    private float _flyYaw;
    private float _flyPitch;

    // FOV
    private Camera _cam;
    private float  _defaultFOV;

    // ─────────────────────────────────────────────────────────────────────

    void Start()
    {
        _cam       = GetComponent<Camera>();
        _defaultFOV = _cam.fieldOfView;

        _targetZoom       = startZoomDistance;
        _currentZoom      = startZoomDistance;
        _targetRotationY  = transform.eulerAngles.y;
        _currentRotationY = _targetRotationY;
        _targetTilt       = startTiltAngle;
        _currentTilt      = startTiltAngle;

        _fallbackPivot = (pivotPoint != null) ? pivotPoint.position : Vector3.zero;

        mode = CameraMode.Orbit;

        AutoFindBoundCubes();
        RecalcBounds();
    }

    // ── Auto-wyszukiwanie Cube'ów ─────────────────────────────────────────

    void AutoFindBoundCubes()
    {
        string[] names = { "Cube(1)", "Cube(2)", "Cube(3)", "Cube(4)" };
        bool anyMissing = false;
        for (int i = 0; i < boundCubes.Length; i++)
            if (boundCubes[i] == null) { anyMissing = true; break; }

        if (!anyMissing) return;

        if (boundCubes.Length < 4) System.Array.Resize(ref boundCubes, 4);

        for (int i = 0; i < 4; i++)
        {
            if (boundCubes[i] != null) continue;
            GameObject found = GameObject.Find(names[i]);
            if (found != null)
                boundCubes[i] = found.transform;
            else
                Debug.LogWarning($"[Kamera] Nie znaleziono obiektu '{names[i]}' na scenie.");
        }
    }

    void RecalcBounds()
    {
        _boundsReady = false;
        float minX = float.MaxValue, maxX = float.MinValue;
        float minZ = float.MaxValue, maxZ = float.MinValue;

        foreach (var t in boundCubes)
        {
            if (t == null) continue;
            minX = Mathf.Min(minX, t.position.x);
            maxX = Mathf.Max(maxX, t.position.x);
            minZ = Mathf.Min(minZ, t.position.z);
            maxZ = Mathf.Max(maxZ, t.position.z);
            _boundsReady = true;
        }

        _minX = minX; _maxX = maxX;
        _minZ = minZ; _maxZ = maxZ;
    }

    // ── LateUpdate ────────────────────────────────────────────────────────

    void LateUpdate()
    {
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

        HandleModeSwitch();

        if (mode == CameraMode.Orbit)
        {
            HandleOrbitRotation();
            HandleOrbit();
        }
        else
        {
            HandleFreeFlyLook();
            HandleFreeFly();
        }

        HandleOptifineZoom();
    }

    // ── Przełączanie trybów ───────────────────────────────────────────────

    void HandleModeSwitch()
    {
        if (Input.GetKeyDown(KeyCode.Q) && mode == CameraMode.Orbit)
        {
            mode = CameraMode.FreeFly;
            _returningToOrbit = false;

            // Inicjalizuj kąty FreeFly z bieżącej orientacji kamery
            Vector3 euler = transform.eulerAngles;
            _flyYaw   = euler.y;
            _flyPitch = euler.x > 180f ? euler.x - 360f : euler.x;
        }

        if (Input.GetKeyDown(KeyCode.E) && mode == CameraMode.FreeFly)
        {
            mode = CameraMode.Orbit;
            _returningToOrbit = true;

            Vector3 pivot = PivotPos();
            Quaternion rot = Quaternion.Euler(_currentTilt, _currentRotationY, 0f);
            _orbitReturnPos = pivot + rot * new Vector3(0f, 0f, -_currentZoom);
            _orbitReturnRot = Quaternion.LookRotation(pivot - _orbitReturnPos);
        }

        if (_returningToOrbit)
        {
            transform.position = Vector3.Lerp(transform.position, _orbitReturnPos, Time.unscaledDeltaTime * 6f);
            transform.rotation = Quaternion.Slerp(transform.rotation, _orbitReturnRot, Time.unscaledDeltaTime * 6f);

            if (Vector3.Distance(transform.position, _orbitReturnPos) < 0.05f)
                _returningToOrbit = false;
        }
    }

    // ── Orbit: rotacja (prawy przycisk myszy) ─────────────────────────────
    // X myszy → obrót poziomy (yaw)
    // Y myszy → nachylenie góra/dół (tilt)

    void HandleOrbitRotation()
    {
        if (!Input.GetMouseButton(1)) return;

        float mouseX  = Input.GetAxis("Mouse X");
        float mouseY  = Input.GetAxis("Mouse Y");
        float mult    = GetSpeedMultiplier();
        float dt      = Time.unscaledDeltaTime;

        _targetRotationY += mouseX * rotationSpeed * mult * dt;

        // Przeciągnięcie w górę → mniejszy kąt (widok z boku),
        // przeciągnięcie w dół → większy kąt (widok z góry).
        _targetTilt -= mouseY * tiltSpeed * mult * dt;
        _targetTilt  = Mathf.Clamp(_targetTilt, minTiltAngle, maxTiltAngle);

        _currentRotationY = Mathf.LerpAngle(_currentRotationY, _targetRotationY,
                                             dt * rotationSmoothing);
        _currentTilt      = Mathf.Lerp(_currentTilt, _targetTilt,
                                        dt * rotationSmoothing);
    }

    // ── Orbit: zoom i pozycjonowanie ──────────────────────────────────────

    void HandleOrbit()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            _targetZoom = startZoomDistance;
            _targetTilt = startTiltAngle;
        }
        if (Input.GetKeyDown(KeyCode.F))
            _targetTilt = startTiltAngle;

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.01f)
        {
            _targetZoom -= scroll * zoomSpeed * GetSpeedMultiplier();
            _targetZoom  = Mathf.Clamp(_targetZoom, minZoomDistance, maxZoomDistance);
        }

        _currentZoom = Mathf.Lerp(_currentZoom, _targetZoom, Time.unscaledDeltaTime * zoomSmoothing);

        if (!_returningToOrbit)
            ApplyOrbitTransform();
    }

    void ApplyOrbitTransform()
    {
        Vector3 pivot    = PivotPos();
        Quaternion rot   = Quaternion.Euler(_currentTilt, _currentRotationY, 0f);
        transform.position = pivot + rot * new Vector3(0f, 0f, -_currentZoom);
        transform.LookAt(pivot);
    }

    // ── FreeFly: FPS look (prawy przycisk myszy) ──────────────────────────
    // X myszy → yaw  |  Y myszy → pitch

    void HandleFreeFlyLook()
    {
        if (!Input.GetMouseButton(1)) return;

        _flyYaw   += Input.GetAxis("Mouse X") * lookSensitivity;
        _flyPitch -= Input.GetAxis("Mouse Y") * lookSensitivity;
        _flyPitch  = Mathf.Clamp(_flyPitch, -89f, 89f);

        transform.rotation = Quaternion.Euler(_flyPitch, _flyYaw, 0f);
    }

    // ── FreeFly: ruch WSAD + scroll ───────────────────────────────────────

    void HandleFreeFly()
    {
        float mult  = (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                      ? freeFlyTurboMultiplier : 1f;
        float speed = freeFlySpeed * mult * Time.unscaledDeltaTime;

        // Ruch w płaszczyźnie XZ względem orientacji kamery (bez zmiany Y)
        Vector3 forward = transform.forward; forward.y = 0f; forward.Normalize();
        Vector3 right   = transform.right;   right.y   = 0f; right.Normalize();

        Vector3 move = Vector3.zero;
        if (Input.GetKey(KeyCode.W)) move += forward;
        if (Input.GetKey(KeyCode.S)) move -= forward;
        if (Input.GetKey(KeyCode.D)) move += right;
        if (Input.GetKey(KeyCode.A)) move -= right;

        transform.position += move * speed;

        // Scroll → zmiana wysokości Y
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.01f)
        {
            Vector3 p = transform.position;
            p.y += scroll * flyScrollSpeed * GetSpeedMultiplier();
            transform.position = p;
        }

        ClampToBounds();
    }

    void ClampToBounds()
    {
        Vector3 p = transform.position;

        if (_boundsReady)
        {
            p.x = Mathf.Clamp(p.x, _minX, _maxX);
            p.z = Mathf.Clamp(p.z, _minZ, _maxZ);
        }

        p.y = Mathf.Clamp(p.y, limitMinY, limitMaxY);
        transform.position = p;
    }

    // ── Optifine Zoom (klawisz C) ─────────────────────────────────────────

    void HandleOptifineZoom()
    {
        float target = Input.GetKey(KeyCode.C) ? zoomedFOV : _defaultFOV;
        _cam.fieldOfView = Mathf.Lerp(_cam.fieldOfView, target, Time.unscaledDeltaTime * fovSmoothing);
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    Vector3 PivotPos() => pivotPoint != null ? pivotPoint.position : _fallbackPivot;

    float GetSpeedMultiplier()
    {
        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) return turboMultiplier;
        if (Input.GetKey(KeyCode.LeftAlt)   || Input.GetKey(KeyCode.RightAlt))   return slowMultiplier;
        return 1f;
    }

    // ── Gizmos ────────────────────────────────────────────────────────────

    void OnDrawGizmosSelected()
    {
        Vector3 pivot = (pivotPoint != null) ? pivotPoint.position : Vector3.zero;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(pivot, 2f);
        Gizmos.DrawLine(transform.position, pivot);

        Gizmos.color = Color.cyan;
        foreach (var t in boundCubes)
            if (t != null) Gizmos.DrawWireSphere(t.position, 1.5f);

        if (_boundsReady)
        {
            Gizmos.color = new Color(0f, 1f, 1f, 0.3f);
            Vector3 center = new Vector3((_minX + _maxX) * 0.5f, 0f, (_minZ + _maxZ) * 0.5f);
            Vector3 size   = new Vector3(_maxX - _minX, 1f, _maxZ - _minZ);
            Gizmos.DrawWireCube(center, size);
        }
    }
}
