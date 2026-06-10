using UnityEngine;
using UnityEngine.AI;

public class WheelRotation : MonoBehaviour
{
    [Header("Koła do obrotu (przypisz Transform każdego koła)")]
    public Transform[] wheels;

    [Header("Parametry")]
    [Tooltip("Stopnie obrotu na 1 m/s prędkości pojazdu, na sekundę")]
    public float speedMultiplier = 30f;
    public Vector3 rotationAxis = Vector3.right;

    private NavMeshAgent _agent;

    void Start()
    {
        _agent = GetComponentInParent<NavMeshAgent>();
    }

    void Update()
    {
        if (wheels == null || wheels.Length == 0) return;

        float speed = _agent != null ? _agent.velocity.magnitude : 0f;
        float angle = speed * speedMultiplier * Time.deltaTime;

        foreach (var wheel in wheels)
            if (wheel != null)
                wheel.Rotate(rotationAxis, angle, Space.Self);
    }
}
