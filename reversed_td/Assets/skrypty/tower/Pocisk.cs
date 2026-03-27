using UnityEngine;
using UnityEngine;

public class Pocisk : MonoBehaviour
{
    [Header("Ustawienia Pocisku")]
    public float predkosc = 20f;
    public float obrazenia = 20f;
    public float czasZycia = 5f; 

    private Transform cel;

    public void UstawCel(Transform nowyCel)
    {
        cel = nowyCel;
    }

    void Start()
    {
        Destroy(gameObject, czasZycia);
    }

    void Update()
    {
        if (cel == null)
        {
            Destroy(gameObject);
            return;
        }

  
        Vector3 kierunek = cel.position - transform.position;
        float dystansWTejRamce = predkosc * Time.deltaTime;
        transform.Translate(kierunek.normalized * dystansWTejRamce, Space.World);
        transform.LookAt(cel);
    }

    void OnTriggerEnter(Collider other)
    {
     
        if (other.gameObject.layer == LayerMask.NameToLayer("Wrog"))
        {
           
            pojazd p = other.GetComponent<pojazd>();

            if (p != null)
            {
                p.OdejmijHp(obrazenia);
            }
            Destroy(gameObject);
        }
    }
}