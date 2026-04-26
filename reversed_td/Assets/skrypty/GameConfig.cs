using UnityEngine;

[System.Serializable]
public class VehicleConfig
{
    public string vehicleName;
    public int cost;
    public Sprite icon; // Tu w przysz³oœci wrzucisz grafiki w kwadracie
}

[CreateAssetMenu(fileName = "UstawieniaEkonomii", menuName = "Tower Defense/Ustawienia Ekonomii")]
public class GameConfig : ScriptableObject
{
    [Header("Ustawienia Z³ota")]
    public int startingGold = 200;
    public int goldPerWin = 100;

    [Tooltip("Ile z³ota dostaje gracz, gdy jego jednostka dotrze do koñca trasy")]
    public int goldPerEscapedVehicle = 10;

    [Header("Lista Pojazdów (ustaw 5)")]
    public VehicleConfig[] vehicles = new VehicleConfig[5];
}