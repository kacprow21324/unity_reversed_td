using UnityEngine;

[System.Serializable]
public class VehicleConfig
{
    public string vehicleName;
    public int cost;
    public Sprite icon; // Tu w przysz�o�ci wrzucisz grafiki w kwadracie
}

[CreateAssetMenu(fileName = "UstawieniaEkonomii", menuName = "Tower Defense/Ustawienia Ekonomii")]
public class GameConfig : ScriptableObject
{
    [Header("Ustawienia Złota")]
    public int startingGold = 200;

    [Tooltip("Bazowa nagroda za wygranie rundy (część stała wzoru)")]
    public int goldPerWin = 100;

    [Tooltip("Mnożnik rundy we wzorze: goldPerWin + goldPerRoundMultiplier * nrRundy")]
    public int goldPerRoundMultiplier = 20;

    [Tooltip("Ile złota dostaje gracz, gdy jego jednostka dotrze do końca trasy")]
    public int goldPerEscapedVehicle = 10;

    [Header("Lista Pojazd�w (ustaw 5)")]
    public VehicleConfig[] vehicles = new VehicleConfig[5];
}