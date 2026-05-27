using UnityEngine;

/// Ciągły obrót anteny/głowicy sonara.
/// Antena zatrzymuje się automatycznie gdy sonar jest zniszczony (SetActive false).
///
/// Jak użyć:
///   Dodaj do dziecka "Antena" w prefabie WiezaSonar.
///   Ustaw predkoscObrotu i os (domyślnie Y = poziomo).
public class SonarAntennaRotation : MonoBehaviour
{
    [Header("Obrót Anteny")]
    public float predkoscObrotu = 180f;  // stopni / sekundę
    public Vector3 os = Vector3.up;      // oś obrotu (Space.Self)

    void Update()
    {
        transform.Rotate(os, predkoscObrotu * Time.deltaTime, Space.Self);
    }
}
