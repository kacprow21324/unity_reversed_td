using UnityEngine;
using UnityEngine.AI; 

[RequireComponent(typeof(NavMeshAgent))] 
public class pojazd : MonoBehaviour
{
    [Header("Ustawienia Zdrowia")]
    public float maxHp = 100f;
    private float aktualneHp;

    private NavMeshAgent _agent; 

    void Start()
    {
 
        aktualneHp = maxHp;

        _agent = GetComponent<NavMeshAgent>();
        FinishLine meta = FindObjectOfType<FinishLine>();

        if (meta != null)
        {
            _agent.SetDestination(meta.transform.position);
        }
        else
        {
            Debug.LogError("Pojazd: Nie znaleziono obiektu ze skryptem FinishLine na mapie!");
        }
    }


    public void OdejmijHp(float obrazenia)
    {
        aktualneHp -= obrazenia;
        if (aktualneHp <= 0f)
        {
            Smierc();
        }
    }

    void Smierc()
    {
        Destroy(gameObject);
    }

    public float PobierzAktualneHP()
    {
        return aktualneHp;
    }
}