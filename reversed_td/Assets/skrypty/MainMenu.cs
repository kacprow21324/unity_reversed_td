using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuLogic : MonoBehaviour
{
    [Header("G³ówne Panele")]
    public GameObject mainMenuPanel;
    public GameObject mapSelectionPanel;
    public GameObject settingsPanel;
    public GameObject multiPanel;
    public GameObject tutorialPanel;

    private void Start()
    {
        ShowOnlyOnePanel(mainMenuPanel);
    }

    public void OnClickSinglePlayer() => ShowOnlyOnePanel(mapSelectionPanel);
    public void OnClickMultiplayer() => ShowOnlyOnePanel(multiPanel);
    public void OnClickSettings() => ShowOnlyOnePanel(settingsPanel);
    public void OnClickTutorial() => ShowOnlyOnePanel(tutorialPanel);
    public void OnClickBack() => ShowOnlyOnePanel(mainMenuPanel);

    public void OnClickShutDown()
    {
        Debug.Log("Zamykanie gry...");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void OnClickMap(string sceneName)
    {
        Debug.Log($"£adujê mapê: {sceneName}");
        SceneManager.LoadScene(sceneName);
    }

    private void ShowOnlyOnePanel(GameObject panelToShow)
    {
        if (mainMenuPanel) mainMenuPanel.SetActive(false);
        if (mapSelectionPanel) mapSelectionPanel.SetActive(false);
        if (settingsPanel) settingsPanel.SetActive(false);
        if (multiPanel) multiPanel.SetActive(false);
        if (tutorialPanel) tutorialPanel.SetActive(false);

        if (panelToShow) panelToShow.SetActive(true);
    }
}