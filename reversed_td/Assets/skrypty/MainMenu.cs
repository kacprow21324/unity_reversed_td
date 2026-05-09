using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuLogic : MonoBehaviour
{
    [Header("G��wne Panele")]
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
        Debug.Log($"Laduje mape: {sceneName}");
        // Przenies NetworkManager z DontDestroyOnLoad do biezacej sceny,
        // zeby zostal zniszczony razem z menu przy zaladowaniu SP sceny.
        if (Mirror.NetworkManager.singleton != null)
            SceneManager.MoveGameObjectToScene(
                Mirror.NetworkManager.singleton.gameObject,
                SceneManager.GetActiveScene());
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