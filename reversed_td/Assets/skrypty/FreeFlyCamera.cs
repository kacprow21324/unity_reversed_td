using UnityEngine;

/// <summary>
/// FreeFlyCamera - swobodna kamera do eksploracji terenu w Unity
/// 
/// STEROWANIE:
///   WASD / strzałki  - ruch poziomy (przód/tył/lewo/prawo)
///   Q / E            - ruch w dół / w górę
///   Prawy przycisk myszy (przytrzymaj) - obracanie kamery
///   Scroll myszy     - przyśpieszenie ruchu (zoom prędkości)
///   Shift            - tryb turbo (szybki ruch)
///   Alt              - tryb precyzyjny (wolny ruch)
///   F                - fokus na punkt (0,0,0) / reset pozycji
///   R                - reset całkowity (pozycja + rotacja)
/// </summary>
public class FreeFlyCamera : MonoBehaviour
{
    [Header("Prędkość ruchu")]
    [Tooltip("Podstawowa prędkość lotu")]
    public float moveSpeed = 20f;

    [Tooltip("Mnożnik prędkości przy Shift (turbo)")]
    public float turboMultiplier = 4f;

    [Tooltip("Mnożnik prędkości przy Alt (precyzja)")]
    public float slowMultiplier = 0.2f;

    [Tooltip("Płynność przyspieszania ruchu")]
    public float acceleration = 10f;

    [Header("Obrót kamery")]
    [Tooltip("Czułość myszy - oś pozioma")]
    public float mouseSensitivityX = 3f;

    [Tooltip("Czułość myszy - oś pionowa")]
    public float mouseSensitivityY = 3f;

    [Tooltip("Odwróć oś Y myszy")]
    public bool invertY = false;

    [Tooltip("Wygładzanie obrotu")]
    public float rotationSmoothing = 10f;

    [Header("Scroll - zmiana prędkości")]
    [Tooltip("Scroll myszy zmienia prędkość lotu")]
    public bool scrollChangesSpeed = true;

    [Tooltip("Jak bardzo scroll wpływa na prędkość")]
    public float scrollSpeedMultiplier = 2f;

    [Tooltip("Minimalna prędkość lotu")]
    public float minSpeed = 1f;

    [Tooltip("Maksymalna prędkość lotu")]
    public float maxSpeed = 200f;

    [Header("Opcje")]
    [Tooltip("Ukryj kursor podczas lotu")]
    public bool hideCursorOnFly = true;

    [Tooltip("Startowa pozycja kamery")]
    public Vector3 startPosition = new Vector3(0f, 30f, -50f);

    [Tooltip("Startowa rotacja kamery (Euler)")]
    public Vector3 startRotation = new Vector3(30f, 0f, 0f);

    // Stan wewnętrzny
    private Vector3 _currentVelocity = Vector3.zero;
    private float _yaw;
    private float _pitch;
    private float _targetYaw;
    private float _targetPitch;
    private bool _isFlying = false;

    // Kursor
    private CursorLockMode _previousLockState;
    private bool _previousCursorVisible;

    void Start()
    {
        // Ustaw startową pozycję i rotację
        transform.position = startPosition;
        transform.eulerAngles = startRotation;

        _yaw = startRotation.y;
        _pitch = startRotation.x;
        _targetYaw = _yaw;
        _targetPitch = _pitch;
    }

    void Update()
    {
        HandleMouseLook();
        HandleMovement();
        HandleScrollSpeed();
        HandleSpecialKeys();
    }

    /// <summary>
    /// Obsługa obrotu kamery prawym przyciskiem myszy
    /// </summary>
    void HandleMouseLook()
    {
        bool rightMouseHeld = Input.GetMouseButton(1);

        // Aktywacja trybu lotu
        if (Input.GetMouseButtonDown(1))
        {
            _isFlying = true;

            if (hideCursorOnFly)
            {
                _previousLockState = Cursor.lockState;
                _previousCursorVisible = Cursor.visible;
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        // Dezaktywacja
        if (Input.GetMouseButtonUp(1))
        {
            _isFlying = false;

            if (hideCursorOnFly)
            {
                Cursor.lockState = _previousLockState;
                Cursor.visible = _previousCursorVisible;
            }
        }

        // Obrót tylko gdy prawy przycisk wciśnięty
        if (rightMouseHeld)
        {
            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");

            _targetYaw   += mouseX * mouseSensitivityX;
            _targetPitch -= mouseY * mouseSensitivityY * (invertY ? -1f : 1f);

            // Ogranicz kąt pionowy żeby nie "przewrócić" kamery
            _targetPitch = Mathf.Clamp(_targetPitch, -89f, 89f);
        }

        // Wygładzony obrót
        _yaw   = Mathf.LerpAngle(_yaw,   _targetYaw,   Time.deltaTime * rotationSmoothing);
        _pitch = Mathf.LerpAngle(_pitch, _targetPitch, Time.deltaTime * rotationSmoothing);

        transform.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
    }

    /// <summary>
    /// Obsługa ruchu WASD + Q/E
    /// </summary>
    void HandleMovement()
    {
        // Kierunki
        float horizontal = Input.GetAxis("Horizontal"); // A/D lub strzałki
        float vertical   = Input.GetAxis("Vertical");   // W/S lub strzałki
        float upDown     = 0f;

        if (Input.GetKey(KeyCode.E)) upDown =  1f;
        if (Input.GetKey(KeyCode.Q)) upDown = -1f;

        Vector3 direction = new Vector3(horizontal, upDown, vertical);

        // Mnożnik prędkości
        float speed = moveSpeed;
        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
            speed *= turboMultiplier;
        else if (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt))
            speed *= slowMultiplier;

        // Docelowa prędkość w lokalnym układzie kamery
        Vector3 targetVelocity = transform.TransformDirection(direction.normalized) * speed;

        // Wygładzenie przyspieszenia
        _currentVelocity = Vector3.Lerp(_currentVelocity, targetVelocity, Time.deltaTime * acceleration);

        transform.position += _currentVelocity * Time.deltaTime;
    }

    /// <summary>
    /// Scroll zmienia prędkość bazową
    /// </summary>
    void HandleScrollSpeed()
    {
        if (!scrollChangesSpeed) return;

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.01f)
        {
            moveSpeed += scroll * scrollSpeedMultiplier * moveSpeed;
            moveSpeed  = Mathf.Clamp(moveSpeed, minSpeed, maxSpeed);
        }
    }

    /// <summary>
    /// Klawisze specjalne: F = fokus, R = reset
    /// </summary>
    void HandleSpecialKeys()
    {
        // R - pełny reset
        if (Input.GetKeyDown(KeyCode.R))
        {
            transform.position = startPosition;
            transform.eulerAngles = startRotation;

            _yaw        = startRotation.y;
            _pitch      = startRotation.x;
            _targetYaw  = _yaw;
            _targetPitch = _pitch;

            _currentVelocity = Vector3.zero;
            moveSpeed = 20f;

            Debug.Log("[FreeFlyCamera] Reset do pozycji startowej.");
        }

        // F - spójrz na środek mapy (0,0,0)
        if (Input.GetKeyDown(KeyCode.F))
        {
            Vector3 dir = (Vector3.zero - transform.position).normalized;
            Quaternion targetRot = Quaternion.LookRotation(dir);

            _targetYaw   = targetRot.eulerAngles.y;
            _targetPitch = targetRot.eulerAngles.x;
            if (_targetPitch > 180f) _targetPitch -= 360f;

            Debug.Log("[FreeFlyCamera] Fokus na punkt (0,0,0).");
        }
    }

    /// <summary>
    /// Rysuje pomocnicze Gizmo w edytorze
    /// </summary>
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 1f);
        Gizmos.DrawRay(transform.position, transform.forward * 5f);
    }
}
