using System.Collections.Generic;
using UnityEngine;

public class GameStatistics : MonoBehaviour
{
    public static GameStatistics Instance;

    public Dictionary<string, int> deployedUnits   = new Dictionary<string, int>();
    public Dictionary<string, int> destroyedTowers = new Dictionary<string, int>();

    [Header("Podglad statystyk")]
    public int totalGoldSpent;
    public int wavesSurvived;
    public int totalDeployedUnits;
    public int totalDestroyedTowers;

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
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

    public void ResetStats()
    {
        deployedUnits.Clear();
        destroyedTowers.Clear();
        totalGoldSpent       = 0;
        wavesSurvived        = 0;
        totalDeployedUnits   = 0;
        totalDestroyedTowers = 0;
    }
}
