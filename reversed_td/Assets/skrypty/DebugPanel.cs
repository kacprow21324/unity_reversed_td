using UnityEngine;

public class DebugPanel : MonoBehaviour
{
    public void DebugWinRound()
    {
        GameplayUIManager.Instance?.DebugWinCurrentRound();
    }

    public void DebugWinGame()
    {
        GameManager.Instance?.TriggerVictory();
    }

    public void DebugLoseGame()
    {
        GameManager.Instance?.TriggerDefeat();
    }
}
