using UnityEngine;
using System.Collections;

// ATANK – Czołg. Ogromna pula HP, bardzo wolny. Naciąga ogień wież na siebie
// dzięki fladze maTaunt, którą respektują wszystkie wieże atakujące.
public class PojazdTank : pojazd
{
    [Header("Parametry Tauntera")]
    public float promienTauntu = 20f;

    protected override void Start()
    {
        maxHp = 500f;
        pancerz = 30f;
        maTaunt = true;
        base.Start();
        _agent.speed = 1.5f;
    }
}
