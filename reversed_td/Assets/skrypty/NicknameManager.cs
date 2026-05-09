using System.IO;
using UnityEngine;

public class NicknameManager : MonoBehaviour
{
    public static NicknameManager Instance { get; private set; }

    public static string LocalNickname { get; private set; } = "Gracz";

    const string FILE_NAME = "player_settings.txt";
    const int MAX_NICK_LEN = 24;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        LoadFromFile();
    }

    public void SaveNickname(string nick)
    {
        nick = SanitizeNick(nick);
        LocalNickname = nick;
        File.WriteAllText(Path.Combine(Application.persistentDataPath, FILE_NAME), nick);
    }

    static string SanitizeNick(string nick)
    {
        if (string.IsNullOrWhiteSpace(nick)) return "Gracz";
        nick = nick.Trim();
        return nick.Length > MAX_NICK_LEN ? nick.Substring(0, MAX_NICK_LEN) : nick;
    }

    void LoadFromFile()
    {
        string path = Path.Combine(Application.persistentDataPath, FILE_NAME);
        if (!File.Exists(path)) return;
        string nick = SanitizeNick(File.ReadAllText(path));
        if (!string.IsNullOrEmpty(nick)) LocalNickname = nick;
    }

    // Ensures NicknameManager exists in the scene (called from other scripts)
    public static void EnsureExists()
    {
        if (Instance == null)
            new GameObject("NicknameManager").AddComponent<NicknameManager>();
    }
}
