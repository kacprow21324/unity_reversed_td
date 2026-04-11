using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// AIEnhancedRTSCamera - P³ynna kamera orbitalna do gier strategicznych/TD.
/// 
/// STEROWANIE:
///   Prawy przycisk myszy (przytrzymaj) - obracanie kamery wokó³ œrodka mapy
///   Scroll myszy        - p³ynne przybli¿anie / oddalanie (Zoom)
///   Shift               - tryb turbo (szybszy zoom/obrót)
///   Alt                 - tryb precyzyjny (wolniejszy zoom/obrót)
///   F                   - reset k¹ta nachylenia do startowego
///   R                   - reset ca³kowity (zoom + obrót + pochylenie)
/// </summary>
[RequireComponent(typeof(Camera))]
public class AIEnhancedRTSCamera : MonoBehaviour
{
    [Header("--- Cel i Orientacja ---")]
    [Tooltip("Punkt na œrodku mapy, wokó³ którego kamera siê krêci. Jeœli puste, u¿yje (0,0,0)")]
    public Transform pivotPoint;

    [Tooltip("Pocz¹tkowe nachylenie kamery pod ukosem w dó³ (w stopniach)")]
    [Range(10f, 85f)]
    public float startTiltAngle = 50f;

    [Header("--- Przybli¿anie (Zoom) ---")]
    [Tooltip("Podstawowa prêdkoœæ przybli¿ania rolk¹ myszy")]
    public float zoomSpeed = 30f;

    [Tooltip("Minimalna odleg³oœæ od œrodka mapy")]
    public float minZoomDistance = 10f;

    [Tooltip("Maksymalna odleg³oœæ od œrodka mapy")]
    public float maxZoomDistance = 100f;

    [Tooltip("Startowa odleg³oœæ od œrodka mapy")]
    public float startZoomDistance = 40f;

    [Tooltip("Wyg³adzanie przybli¿ania (im mniejsze, tym wolniejsze)")]
    public float zoomSmoothing = 8f;

    [Header("--- Obrót orbitalny ---")]
    [Tooltip("Prêdkoœæ obrotu wokó³ mapy trzymaj¹c prawy przycisk myszy")]
    public float rotationSpeed = 120f;

    [Tooltip("Wyg³adzanie obrotu (im mniejsze, tym wolniejsze)")]
    public float rotationSmoothing = 15f;

    [Header("Mno¿niki prêdkoœci (Shift/Alt)")]
    [Tooltip("Mno¿nik prêdkoœci przy Shift (turbo)")]
    public float turboMultiplier = 2.5f;

    [Tooltip("Mno¿nik prêdkoœci przy Alt (precyzja)")]
    public float slowMultiplier = 0.3f;

    private float _currentZoom;
    private float _targetZoom;
    private float _currentRotationY;
    private float _targetRotationY;
    private float _currentTargetTilt;
    private Vector3 _fallbackPivot;

    void Start()
    {
        _targetZoom = startZoomDistance;
        _currentZoom = startZoomDistance;
        _targetRotationY = transform.eulerAngles.y;
        _currentRotationY = _targetRotationY;
        _currentTargetTilt = startTiltAngle;

        if (pivotPoint == null)
        {
            Debug.LogWarning("[Kamera RTS] Nie przypisano punktu centralnego (Pivot). U¿ywam (0,0,0).");
            _fallbackPivot = Vector3.zero;
        }
    }

    void LateUpdate()
    {
        // Ignorowanie klikniêæ na interfejs UI
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

        HandleCalculations();
        HandleSpecialKeys();
        ApplyTransform();
    }

    void HandleCalculations()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.01f)
        {
            float speedMultiplier = GetSpeedMultiplier();
            _targetZoom -= scroll * zoomSpeed * speedMultiplier;
            _targetZoom = Mathf.Clamp(_targetZoom, minZoomDistance, maxZoomDistance);
        }

        _currentZoom = Mathf.Lerp(_currentZoom, _targetZoom, Time.deltaTime * zoomSmoothing);

        if (Input.GetMouseButton(1))
        {
            float mouseX = Input.GetAxis("Mouse X");
            float speedMultiplier = GetSpeedMultiplier();
            _targetRotationY += mouseX * rotationSpeed * speedMultiplier * Time.deltaTime;
        }

        _currentRotationY = Mathf.LerpAngle(_currentRotationY, _targetRotationY, Time.deltaTime * rotationSmoothing);
    }

    float GetSpeedMultiplier()
    {
        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) return turboMultiplier;
        if (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt)) return slowMultiplier;
        return 1f;
    }

    void ApplyTransform()
    {
        Vector3 currentPivotPos = (pivotPoint != null) ? pivotPoint.position : _fallbackPivot;
        Quaternion rotation = Quaternion.Euler(_currentTargetTilt, _currentRotationY, 0f);
        Vector3 negativeDistance = new Vector3(0.0f, 0.0f, -_currentZoom);

        transform.position = currentPivotPos + rotation * negativeDistance;
        transform.LookAt(currentPivotPos);
    }

    void HandleSpecialKeys()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            _targetZoom = startZoomDistance;
            _currentTargetTilt = startTiltAngle;
        }
        if (Input.GetKeyDown(KeyCode.F))
        {
            _currentTargetTilt = startTiltAngle;
        }
    }

    void OnDrawGizmosSelected()
    {
        Vector3 currentPivotPos = (pivotPoint != null) ? pivotPoint.position : _fallbackPivot;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(currentPivotPos, 2f);
        Gizmos.DrawLine(transform.position, currentPivotPos);
    }
}