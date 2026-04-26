using UnityEngine;

public class PojazdTank : pojazd
{
    [Header("Parametry Tauntera")]
    public float promienTauntu = 20f;

    protected override void Start()
    {
        maxHp   = DecreeManager.Instance != null
            ? DecreeManager.Instance.FinalHP("Tank", DecreeManager.BASE_TNK_HP)
            : DecreeManager.BASE_TNK_HP;
        pancerz = DecreeManager.Instance != null
            ? DecreeManager.Instance.FinalArmor("Tank", DecreeManager.BASE_TNK_ARM)
            : DecreeManager.BASE_TNK_ARM;
        maTaunt = true;
        base.Start();
        _agent.speed = DecreeManager.Instance != null
            ? DecreeManager.Instance.FinalSpeed("Tank", DecreeManager.BASE_TNK_SPD)
            : DecreeManager.BASE_TNK_SPD;
    }
}
