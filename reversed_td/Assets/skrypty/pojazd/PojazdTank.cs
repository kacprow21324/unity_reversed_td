using UnityEngine;

public class PojazdTank : pojazd
{
    protected override void Start()
    {
        if (DecreeManager.Instance != null)
        {
            maxHp   = DecreeManager.Instance.FinalHP("Tank", maxHp);
            pancerz = DecreeManager.Instance.FinalArmor("Tank", pancerz);
        }
        maTaunt = true;
        base.Start();
        if (DecreeManager.Instance != null)
            _agent.speed = DecreeManager.Instance.FinalSpeed("Tank", _agent.speed);
    }
}
