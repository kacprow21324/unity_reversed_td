using UnityEngine;

// ABASIC – standardowy wóz. Trochę szybszy niż Tank, normalny pancerz i HP.
public class PojazdPodstawowy : pojazd
{
    protected override void Start()
    {
        maxHp = 120f;
        pancerz = 10f;
        base.Start();
        _agent.speed = 4.5f;
    }
}
