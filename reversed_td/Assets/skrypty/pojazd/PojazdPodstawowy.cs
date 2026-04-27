using UnityEngine;

public class PojazdPodstawowy : pojazd
{
    protected override void Start()
    {
        if (DecreeManager.Instance != null)
        {
            maxHp   = DecreeManager.Instance.FinalHP("Podstawowy", maxHp);
            pancerz = DecreeManager.Instance.FinalArmor("Podstawowy", pancerz);
        }
        base.Start();
        if (DecreeManager.Instance != null)
            _agent.speed = DecreeManager.Instance.FinalSpeed("Podstawowy", _agent.speed);
    }
}
