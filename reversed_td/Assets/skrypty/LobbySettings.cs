// Przenosi ustawienia lobby (złoto startowe) do sceny gry.
// Statyczna klasa przeżywa zmianę sceny.
public static class LobbySettings
{
    // -1 = użyj wartości z GameConfig (tryb SP lub lobby bez ustawień)
    public static int StartGold = -1;

    public static void Reset() => StartGold = -1;
}
