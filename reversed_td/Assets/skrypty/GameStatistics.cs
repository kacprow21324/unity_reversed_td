using System.Collections.Generic;
using UnityEngine;

public class GameStatistics : MonoBehaviour
{
    public static GameStatistics Instance;

    public Dictionary<string, int> deployedUnits   = new Dictionary<string, int>();
    public Dictionary<string, int> destroyedTowers = new Dictionary<string, int>();

    [Header("Podglad statystyk")]
    public int   totalGoldSpent;
    public int   wavesSurvived;
    public int   totalDeployedUnits;
    public int   totalDestroyedTowers;
    public int   airstrikeUsed;
    public int   shieldUsed;
    public int   boostUsed;
    public float gameStartTime;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
        gameStartTime = Time.realtimeSinceStartup;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void RegisterDeployedUnit(string unitName)
    {
        if (!deployedUnits.ContainsKey(unitName))
            deployedUnits[unitName] = 0;
        deployedUnits[unitName]++;
        totalDeployedUnits++;
    }

    public void RegisterDestroyedTower(string towerName)
    {
        if (!destroyedTowers.ContainsKey(towerName))
            destroyedTowers[towerName] = 0;
        destroyedTowers[towerName]++;
        totalDestroyedTowers++;
    }

    public void RegisterAbility(string abilityType)
    {
        switch (abilityType)
        {
            case "airstrike": airstrikeUsed++; break;
            case "shield":    shieldUsed++;    break;
            case "boost":     boostUsed++;     break;
        }
    }

    public string GetFavoriteUnit()
    {
        if (deployedUnits.Count == 0) return "Brak";
        string favorite = null;
        int maxCount = 0;
        foreach (var kv in deployedUnits)
        {
            if (kv.Value > maxCount)
            {
                maxCount  = kv.Value;
                favorite  = kv.Key;
            }
        }
        return favorite ?? "Brak";
    }

    public float GetGameDuration() => Time.realtimeSinceStartup - gameStartTime;
}
