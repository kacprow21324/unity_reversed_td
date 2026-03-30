using UnityEngine;
using TMPro;

public class WyswietlaczHP : MonoBehaviour
{
    public TMP_Text tekstHP;
    private pojazd skryptPojazdu;
    private Transform kamera;

    void Start()
    {
        skryptPojazdu = GetComponentInParent<pojazd>();
        kamera = Camera.main.transform;
    }

    void Update()
    {
        if (skryptPojazdu != null && tekstHP != null)
        {
            tekstHP.text = skryptPojazdu.PobierzAktualneHP().ToString();
        }

        if (kamera != null)
        {
            transform.rotation = kamera.rotation;
        }
    }
}