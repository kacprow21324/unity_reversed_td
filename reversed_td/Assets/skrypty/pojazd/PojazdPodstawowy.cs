using UnityEngine;

public class PojazdPodstawowy : pojazd
{
    protected override void Start()
    {
        maxHp   = DecreeManager.Instance != null
            ? DecreeManager.Instance.FinalHP("Podstawowy", DecreeManager.BASE_POD_HP)
            : DecreeManager.BASE_POD_HP;
        pancerz = DecreeManager.Instance != null
            ? DecreeManager.Instance.FinalArmor("Podstawowy", DecreeManager.BASE_POD_ARM)
            : DecreeManager.BASE_POD_ARM;
        base.Start();
        _agent.speed = DecreeManager.Instance != null
            ? DecreeManager.Instance.FinalSpeed("Podstawowy", DecreeManager.BASE_POD_SPD)
            : DecreeManager.BASE_POD_SPD;
    }
}
