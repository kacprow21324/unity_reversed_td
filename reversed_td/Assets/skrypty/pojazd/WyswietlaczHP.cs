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
        kamera = Camera.main?.transform;

        transform.localPosition = new Vector3(0f, 3f, 0f);

        if (tekstHP != null)
        {
            tekstHP.fontSize = 8f;
            tekstHP.fontStyle = FontStyles.Bold;
            tekstHP.alignment = TextAlignmentOptions.Center;
        }
    }

    void Update()
    {
        if (skryptPojazdu != null && tekstHP != null)
            tekstHP.text = Mathf.CeilToInt(skryptPojazdu.PobierzAktualneHP()).ToString();

        if (kamera != null)
            transform.rotation = kamera.rotation;
    }
}
