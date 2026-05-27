# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**Reversed Tower Defense** — Unity 3D gra, w której gracze sterują pojazdami atakującymi wieże (odwrócona mechanika klasycznego TD). Obsługuje tryb singleplayer (do 10 rund) i multiplayer 1v1 (Mirror networking, lokalny LAN).

Unity project root: `reversed_td/`  
Skrypty gry: `reversed_td/Assets/skrypty/`  
Sceny: `reversed_td/Assets/mapy/`

## Uruchamianie i budowanie

Projekt otwiera się w Unity Editor (zalecane Unity 2022.x lub 2023.x LTS). Nie ma CLI ani testów jednostkowych — testy odbywają się wyłącznie przez Play Mode w edytorze.

**Sceny w Build Settings (kolejność):**
- Index 0: `Assets/mapy/menu.unity` — główne menu
- SP: `teren1.unity`, `teren2.unity`, `teren3.unity`
- MP: `mapy/multi/mapy/Teren1_Multiplayer/Teren1_Multiplayer.unity` itd.

`SceneManager.LoadScene(0)` zawsze wraca do menu.

**Debug klawiszologia (działa tylko w SP, w fazie ataku):**
- `P` — +100 złota
- `O` — wygraj bieżącą rundę
- `I` — wygraj grę
- `U` — przegraj grę

## Architektura gry

### Fazy rozgrywki

**Singleplayer:** Planning → Attack → (wszystkie pojazdy zniszczone lub uciekły) → Decree → Planning+1. Wygrana = runda 10 (`GameManager.VICTORY_ROUND = 10`). Porażka = żaden pojazd nie uciekł w danej rundzie (`_escapedThisRound == 0`).

**Multiplayer:** Serwer wysyła seed → obaj generują mapy → Planning (60s lub wczesny start gdy obaj gotowi) → faza ataku jednocześnie (duchy mirrowane) → szybszy gracz czeka na overlay → serwer rozlicza rundy → Decree phase → następna runda. Każdy ma 3 HP; utrata HP gdy żaden pojazd nie uciekł.

### Menedżery (singletony)

| Klasa | Rola |
|---|---|
| `GameManager` | Koniec gry (victory/defeat/draw), nawigacja między scenami, czyszczenie singleton-ów |
| `GameplayUIManager` | Centrum UI: złoto, kolejka pojazdów, fazy planowania/ataku, dekrety, powiązania z VehicleSpawner |
| `NetworkMatchManager` | Mirror NetworkBehaviour — seed map, timer, HP graczy, synchronizacja rund i ghost-ów |
| `TowerSpawner` | Generuje wieże na obiektach `TowerPlate` z seeda. W MP: `mapRoot` izoluje planszę gracza |
| `VehicleSpawner` | Spawning kolejki pojazdów gracza + ghost-ów przeciwnika |
| `DecreeManager` | Między-rundowe ulepszenia (21 dekretów w puli); AplikujDecree modyfikuje statystyki run-time |
| `TacticalAbilities` | 3 aktywne zdolności: Nalot / Tarcza / Boost; mapaware w MP przez `IsOnMyMap()` |
| `GameStatistics` | Zbiera statystyki do ekranu końcowego |

### Pojazdy (`skrypty/pojazd/`)

Klasa bazowa `pojazd` (NavMeshAgent). Pojazdy mają `isGhost = true` gdy są wizualnymi replikami przeciwnika — duchy NIE wyzwalają `OnVehicleRemoved()` przy śmierci.

| Prefab | Klasa | Cecha specjalna |
|---|---|---|
| Wóz Podstawowy | `PojazdPodstawowy` | Bez specjalnych efektów |
| Wóz Tank | `PojazdTank` | `maTaunt = true` — wieże preferują go jako cel |
| Wóz Dalekosiężny | `PojazdArtyleria` | Strzela do wież w ruchu (`PociskArtyleryjski`, AoE) |
| Wóz Lustrzany | `PojazdLustro` | Odbija `DamageType.Basic` i laser; kontruje `WiezaSonar` |
| Wóz Zasadzka | `PojazdKamikaze` | `IsTargetable = false` gdy brak sonarów; eksploduje blisko wieży |

Wszystkie pojazdy czytają `DecreeManager` w `Start()` aby nałożyć aktywne buffy (HP%, Speed flat, Armor%).

### Wieże (`skrypty/tower/`)

Klasa bazowa `WiezaBaza` (HP display auto-tworzone TextMeshPro, nagroda złota, tarcza, boost ataku). Wieże **nie mają NetworkIdentity** — generowane deterministycznie z seeda na każdym kliencie oddzielnie.

| Klasa | Mechanika |
|---|---|
| `WiezaPodstawowa` | Śledzenie jednego celu; priorytet dla tauntów |
| `WiezaArmatnia` | `PociskArmatni` — AoE przy trafieniu |
| `WiezaKolcowa` | Area of effect kolce (OnTrigger) |
| `WiezaPlazmowa` | Laser; `ReflektujLaser()` przed `OdejmijHp` — Lustro może odesłać |
| `WiezaSonar` | Brak obrażeń; debuff mnożnik 1.5×; max 2 na mapie; `ActiveRadarsCount` globalne |

### Multiplayer (Mirror)

**Kluczowa zasada:** Wieże nie mają `NetworkIdentity`. Efekty (nalot, tarcza) synchronizowane przez `ClientRpc` w `NetworkMatchManager` — identyczny kod wykonywany na obu klientach daje identyczny wynik.

**Ghost system:** Gracz przesyła `CmdSubmitQueueAndReady(int[] vehicleIndices)` → serwer przechowuje kolejki obu graczy → `RpcSpawnEnemyVehicles` → u przeciwnika `VehicleSpawner.StartSpawningGhosts()` z `isGhost = true`.

**Synchronizacja fali:**
1. Gracz kończy falę → `CmdReportWaveFinished(escaped)`
2. Serwer czeka na obu → szybszy dostaje `RpcShowWaitingOverlay` (TargetRpc)
3. Gdy obaj zgłoszą → serwer oblicza HP → `RpcStartDecreePhase(seed, round)`

**PlayerIndex:** Host = 1 (`connectionId == 0`), Client = 2. Tylko Host (index 1) może wysłać `CmdTryStartMatch`.

**Złoto między scenami:** `LobbySettings.StartGold` (statyczna klasa) przenosi wartość przez `RpcSyncStartGold` tuż przed `ServerChangeScene`.

### Dekrety

`DecreeManager` buduje pulę 21 dekretów w `BuildPool()`. `SetQueueFilter(int[])` wyklucza bezsensowne dekrety (np. Pancerz Artylerii, która ma pancerz = 0). W MP każdy klient losuje dekrety niezależnie (`System.Random` z `TickCount + instanceID`).

### Ekonomia (`GameConfig` ScriptableObject)

- Złoto startowe (SP: z `GameConfig.startingGold`, MP: z `LobbySettings.StartGold`)
- Nagroda za rundę: `goldPerWin + goldPerRoundMultiplier * nrRundy`
- Nagroda za ucieczkę pojazdu: `goldPerEscapedVehicle`
- 5 slotów pojazdów skonfigurowanych w `GameConfig.vehicles[]`

### Kamery

- SP: `NowaKamera` (orbit/freelook)
- MP: `MultiplayerCameraSwitcher` — toggle między własną planszą a planszą przeciwnika

## Ważne konwencje

- **Tagi Unity:** Pojazdy muszą mieć tag `"POJAZD"` (sprawdzany przez `FinishLine`). Płyty pod wieże muszą mieć tag `"TowerPlate"` (szukane przez `TowerSpawner`).
- **Warstwy:** `warstwaWroga` (LayerMask) w wieżach musi pokrywać warstwę pojazdów.
- Sceny MP rozpoznawane po `SceneManager.GetActiveScene().name.Contains("Multiplayer")`.
- `Time.timeScale = 0f` w fazie planowania, `1f` w fazie ataku.
- `WiezaSonar.ActiveRadarsCount` to statyczna zmienna globalna — resetuje się gdy wieże są niszczone/dezaktywowane przez `OnDisable`/`OnDestroy`.
