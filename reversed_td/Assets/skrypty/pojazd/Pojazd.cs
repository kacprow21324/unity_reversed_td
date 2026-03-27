using UnityEngine;

public class pojazd : MonoBehaviour
{
    [Header("Ustawienia Zdrowia")]
    public float maxHp = 100f;
    private float aktualneHp;

    void Start()
    {
        aktualneHp = maxHp;
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
}