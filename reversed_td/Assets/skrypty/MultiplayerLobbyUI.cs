using Mirror;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MultiplayerLobbyUI : MonoBehaviour
{
    [Header("Panele")]
    public GameObject multiplayerPanel;
    public GameObject waitingPanel;

    [Header("Pole IP")]
    public TMP_InputField ipInputField;

    [Header("Etykiety statusu")]
    public TextMeshProUGUI statusLabel;

    public void HostGame()
    {
        if (NetworkManager.singleton == null)
        {
            Debug.LogError("[MultiplayerLobbyUI] Brak NetworkManager w scenie!");
            return;
        }

        NetworkManager.singleton.StartHost();
        if (multiplayerPanel) multiplayerPanel.SetActive(false);
        SetStatus("Hosting... czekam na gracza.");
        ShowWaitingPanel();
    }

    public void JoinGame(string ipAddress)
    {
        if (NetworkManager.singleton == null)
        {
            Debug.LogError("[MultiplayerLobbyUI] Brak NetworkManager w scenie!");
            return;
        }

        string ip = string.IsNullOrWhiteSpace(ipAddress) ? "localhost" : ipAddress.Trim();
        NetworkManager.singleton.networkAddress = ip;
        NetworkManager.singleton.StartClient();
        if (multiplayerPanel) multiplayerPanel.SetActive(false);
        SetStatus($"Łączenie z {ip}...");
        ShowWaitingPanel();
    }

    // Podpięte pod przycisk "Dołącz" — pobiera IP z pola tekstowego
    public void JoinGameFromInput()
    {
        string ip = ipInputField != null ? ipInputField.text : "localhost";
        JoinGame(ip);
    }

    public void Disconnect()
    {
        if (NetworkServer.active && NetworkClient.isConnected)
            NetworkManager.singleton.StopHost();
        else if (NetworkClient.isConnected)
            NetworkManager.singleton.StopClient();

        ShowMultiplayerPanel();
        SetStatus("");
    }

    void ShowWaitingPanel()
    {
        if (multiplayerPanel) multiplayerPanel.SetActive(false);
        if (waitingPanel)     waitingPanel.SetActive(true);
    }

    void ShowMultiplayerPanel()
    {
        if (multiplayerPanel) multiplayerPanel.SetActive(true);
        if (waitingPanel)     waitingPanel.SetActive(false);
    }

    void SetStatus(string message)
    {
        if (statusLabel) statusLabel.text = message;
    }
}
