using Rewired.ComponentControls.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TextCore.Text;


[System.Serializable]
public class GameSaveData : SaveData
{
    public int deathCount;
    public int bestScore;
    public int cumulatedScore;

    public int attempts;
    public int wins;

    public long availableCurrency;
    public long totalSpentCurrency;

    public int compassLevel;

    public bool isFullGame;

    public string versionNumber;

    public GameSaveData()
    {
        Reset();
    }

    public override void Reset()
    {
        base.Reset();

        deathCount = 0;
        bestScore = 0;
        cumulatedScore = 0;
        attempts = 0;
        wins = 0;
        availableCurrency = 0;
        totalSpentCurrency = 0;
        compassLevel = 0;
    }

    public override string ToString()
    {
        string result = "[GameSaveData]\n";
        result += "deathCount = " + deathCount.ToString() + "\n";
        result += "bestScore = " + bestScore.ToString() + "\n";
        result += "cumulatedScore = " + cumulatedScore.ToString() + "\n";
        result += "attempts = " + attempts.ToString() + "\n";
        result += "wins = " + wins.ToString() + "\n";
        result += "availableCurrency = " + availableCurrency.ToString() + "\n";
        result += "totalSpentCurrency = " + totalSpentCurrency.ToString() + "\n";
        result += "compassLevel = " + compassLevel.ToString();
        return result;
    }
}


/// <summary>
/// GameManager is a high-level class that will coordinate all calls to other Managers.
/// It also contains some settings that apply to the whole game.
/// </summary>
public class GameManager : MonoBehaviour
{
    // Singleton
    public static GameManager instance;

    [Header("Settings - Log")]
    public VerboseLevel logsVerboseLevel = VerboseLevel.NONE;

    [Header("References")]
    public FrogCharacterController player;
    public MainCamera gameCamera;

    [Header("Prefabs")]
    public GameObject destroyParticleEffectPrefab;
    public float destroyParticleEffectTimespan = 1.0f;

    [Header("Runtime - meta data")]
    public GameSaveData gameData;

    [Header("Runtime - one run data")]
    public bool hasGameStarted;
    public bool isGameRunning;

    private const string totalBugEatenSteamStatName = "total_bugs_eaten";

    private const string savedLastSelectedCharacter = "Froguelike last selected character";
    private const string savedLastSelectedGameMode = "Froguelike last selected game mode";
    private const string savedLastSelectedStartingChapter = "Froguelike last selected starting chapter";

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(this);
        }
        else
        {
            Destroy(this.gameObject);
        }
        hasGameStarted = false;
        isGameRunning = false;
    }

    // Start is called before the first frame update
    void Start()
    {
        Time.timeScale = 0;

        // Initialize managers and data
        InitializeStuff();

        // Attempt loading the save file, show error message if needed
        TryLoadSaveFile();

        // Show title screen
        BackToTitleScreen();

        // Show disclaimers on top if needed
        UIManager.instance.TryShowDisclaimerScreen();

        // Make sure that the right screen is interactive and others behind it are inactive
        UIManager.instance.ResetTitleScreenInteractability();
    }

    private void Update()
    {
        if (isGameRunning)
        {
            RunManager.instance.UpdateTime(Time.deltaTime);
        }
    }


    public void TryLoadSaveFile()
    {
        if (SaveDataManager.instance.DoesSaveFileExist())
        {
            // Load save file
            bool fileLoaded = SaveDataManager.instance.Load();

            // Create a copy of the existing save file if it doesn't already exist
            SaveDataManager.instance.CreateBackupIfNeeded(somethingWentWrong: !fileLoaded);

            if (fileLoaded)
            {
                // Loading went well!
                SaveDataManager.instance.RunSavingCoroutine();

                UIManager.instance.HideSaveFileCorruptedPopUp();

                // Show title screen
                BackToTitleScreen();

                if (logsVerboseLevel == VerboseLevel.MAXIMAL)
                {
                    Debug.Log("Game - Loading worked and started saving coroutine");
                }
            }
            else
            {
                // Loading failed!

                // Show save file bugged pop up
                UIManager.instance.ShowSaveFileCorruptedPopUp();

                if (logsVerboseLevel == VerboseLevel.MAXIMAL)
                {
                    Debug.Log("Game - Loading failed and showing error pop up");
                }
            }
        }
        else
        {
            // No save file exists, let's create one and start the game
            SaveDataManager.instance.CreateEmptySaveFile();
            SaveDataManager.instance.isSaveDataDirty = true;
            SaveDataManager.instance.RunSavingCoroutine();

            UIManager.instance.HideSaveFileCorruptedPopUp();

            // Show title screen
            BackToTitleScreen();

            if (logsVerboseLevel == VerboseLevel.MAXIMAL)
            {
                Debug.Log("Game - Created empty save file and started saving coroutine");
            }
        }
    }

    public void RegisterScore(int score)
    {
        if (score > gameData.bestScore)
        {
            gameData.bestScore = score;
        }
        gameData.cumulatedScore += score;

        // Update the stat on Steam (it's gonna show progress towards the achievement too)
        SetSteamStatIfPossible(totalBugEatenSteamStatName, gameData.cumulatedScore);
        AchievementManager.instance.SteamStoreStats();
    }

    public void OpenCharacterSelection()
    {
        UIManager.instance.ShowCharacterSelectionScreen(true);
    }

    public void SaveSelectedCharacterGameModeAndStartingChapter(string characterID, string gameMode, string chapterID)
    {
        PlayerPrefs.SetString(savedLastSelectedCharacter, characterID);
        PlayerPrefs.SetString(savedLastSelectedGameMode, gameMode);
        PlayerPrefs.SetString(savedLastSelectedStartingChapter, chapterID);
    }

    public void QuickStartNewRun()
    {
        // Get last selected character, game mode, and start chapter
        string lastSelectedCharacterID = PlayerPrefs.GetString(savedLastSelectedCharacter, "CLASSIC_FROG");
        string lastSelectedGameMode = PlayerPrefs.GetString(savedLastSelectedGameMode, "NONE");
        string lastSelectedChapter = PlayerPrefs.GetString(savedLastSelectedStartingChapter, "[CH_COLLECT_HEALTH]"); // First hops
        
        CharacterManager.instance.currentSelectedCharacter = CharacterManager.instance.GetPlayableCharacter(lastSelectedCharacterID);                
        CharacterManager.instance.selectedGameModes = Enum.Parse<GameMode>(lastSelectedGameMode);                
        Chapter startChapter = ChapterManager.instance.GetChapterFromID(lastSelectedChapter);

        RunManager.instance.StartNewRun(CharacterManager.instance.currentSelectedCharacter, CharacterManager.instance.selectedGameModes, startChapter);
    }

    public void UnlockFeature(RewardFeatureType featureKey)
    {
        switch (featureKey)
        {
            case RewardFeatureType.ACHIEVEMENTS_LIST:
                AchievementManager.instance.UnlockAchievementsList();
                break;
            case RewardFeatureType.CHARACTER_SELECTION:
                // not really needed to do anything, character selection will be unlocked automatically when another character is unlocked
                break;
            case RewardFeatureType.SHOP:
                ShopManager.instance.UnlockShop();
                break;
            case RewardFeatureType.CHAPTER_SELECTION_5:
                ChapterManager.instance.SetChapterCountInSelection(5);
                break;
            case RewardFeatureType.GHOST_BUFF:
                CharacterManager.instance.SetCharacterStoryCompleted("GHOST");
                //CharacterManager.instance.IncrementCharacterStats("GHOST", new List<StatValue>() { new StatValue(CharacterStat.MAX_HEALTH, 50) });
                break;
            case RewardFeatureType.RIBBIT_BUFF:
                CharacterManager.instance.SetCharacterStoryCompleted("POISONOUS_FROG");
                //CharacterManager.instance.IncrementCharacterStats("POISONOUS_FROG", new List<StatValue>() { new StatValue(CharacterStat.ATK_DAMAGE_BOOST, 0.3) });
                break;
            case RewardFeatureType.STANLEY_BUFF:
                CharacterManager.instance.SetCharacterStoryCompleted("STANLEY");
                //CharacterManager.instance.IncrementCharacterStats("STANLEY", new List<StatValue>() { new StatValue(CharacterStat.SWIM_SPEED_BOOST, 0.9) });
                break;
            case RewardFeatureType.TOAD_BUFF:
                CharacterManager.instance.SetCharacterStoryCompleted("TOAD");
                //CharacterManager.instance.IncrementCharacterStats("TOAD", new List<StatValue>() { new StatValue(CharacterStat.ARMOR, 2) }); // TODO decide on amount.
                break;
        }
    }

    #region Chapters

    public void RegisterANewAttempt()
    {
        gameData.attempts++;
        SaveDataManager.instance.isSaveDataDirty = true;
    }

    public void StartRunWithCharacter(PlayableCharacter character, GameMode gameModes)
    {
        // Start a new Run
        RunManager.instance.StartNewRun(character, gameModes);
    }

    #endregion

    public void SpawnDestroyParticleEffect(Vector2 position)
    {
        GameObject particleEffectGo = Instantiate(destroyParticleEffectPrefab, position, Quaternion.identity);
        Destroy(particleEffectGo, destroyParticleEffectTimespan);
    }

    public void SetTimeScale(float timeScale)
    {
        Time.timeScale = timeScale;
    }

    public void TriggerGameOver()
    {
        SetTimeScale(0);
        isGameRunning = false;
        UIManager.instance.ShowGameOver(player.revivals);
        gameData.deathCount++;
        SaveDataManager.instance.isSaveDataDirty = true;
    }

    public void ConfirmReroll()
    {
        if (isGameRunning)
        {
            // Reroll level up items (isGameRunning = true means we're in the middle of a run and that reroll is for a level up)
            RunManager.instance.ConfirmRerollLevelUpItemSelection();
        }
        else
        {
            // Reroll chapters (isGameRunning = false means the game stops and we're likely in between chapters, or on the title screen)
            ChapterManager.instance.ConfirmRerollChapterSelection();
        }
    }

    /// <summary>
    /// From Pause Menu you can abandon the current run and display the score
    /// </summary>
    public void GiveUp()
    {
        Pause(false);

        if (logsVerboseLevel == VerboseLevel.MAXIMAL)
        {
            Debug.Log("Game - Give Up current run");
        }

        RunManager.instance.EndRun();
    }

    public void BackToTitleScreen()
    {
        if (EnemiesManager.instance != null)
        {
            EnemiesManager.instance.ClearAllEnemies();
        }
        UIManager.instance.ShowTitleScreen();
        hasGameStarted = false;
    }

    public void RegisterWin()
    {
        gameData.wins++;
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    public void InitializeStuff()
    {
        // Setup the shop manager
        ShopManager.instance.ResetShop(true);

        // Setup the character manager
        CharacterManager.instance.ResetCharacters(true);

        // Setup the enemies manager
        EnemiesManager.instance.ResetEnemies();

        // Setup the chapters manager
        ChapterManager.instance.ResetChapters(true);

        // Setup the run items manager
        RunItemManager.instance.ResetRunItems();

        // Setup the achievement manager
        AchievementManager.instance.ResetAchievements();
    }

    #region Pause

    public bool gameIsPaused;

    public void TogglePause()
    {
        Pause(!gameIsPaused);
    }

    public void Pause(bool pause)
    {
        if (!UIManager.instance.IsSettingsScreenVisible() && !UIManager.instance.IsBackToTitleScreenConfirmationPanelActive() && !RunManager.instance.fixedCollectibleFoundPanelIsVisible)
        {
            gameIsPaused = pause;
            if (!RunManager.instance.levelUpChoiceIsVisible && !ChapterManager.instance.chapterChoiceIsVisible)
            {
                SetTimeScale(pause ? 0 : 1);
            }
            if (pause)
            {
                UIManager.instance.ShowPauseScreen();
            }
            else
            {
                UIManager.instance.HidePauseScreen();
            }
        }
    }

    #endregion

    public void UICancel()
    {
        if (UIManager.instance.IsClearSaveFileConfirmationPanelActive())
        {
            // Confirmation for clearing save file is visible, we cancel it
            UIManager.instance.ShowClearSaveFileConfirmationPanel(false);
        }
        else if (UIManager.instance.IsBackToTitleScreenConfirmationPanelActive())
        {
            // Confirmation for ending run is visible, we cancel it
            UIManager.instance.ShowBackToTitleScreenConfirmationPanel(false);
        }
        else if (UIManager.instance.IsRerollWarningConfirmationPanelActive())
        {
            // Reroll warning is visible, we cancel it
            UIManager.instance.ShowRerollWarningConfirmationPanel(false);
        }
        else if (UIManager.instance.IsSettingsScreenVisible())
        {
            // Settings are open, we hide settings
            UIManager.instance.HideSettingsScreen();
        }
        else if (UIManager.instance.IsShopScreenVisible())
        {
            // Shop is open, we hide shop
            UIManager.instance.HideShop();
        }
        else if (UIManager.instance.IsAchievementScreenVisible())
        {
            // Quests are open, we hide quests
            UIManager.instance.HideAchievements();
        }
        else if (UIManager.instance.IsCreditsScreenVisible())
        {
            // Credits are open, we hide credits
            UIManager.instance.HideCreditsScreen();
        }
        else if (UIManager.instance.IsCharacterSelectionScreenVisible())
        {
            // Character selection is open, we go back to title
            UIManager.instance.ShowTitleScreen();
        }
        else if (UIManager.instance.IsChapterSelectionScreenVisible(out bool isTitleScreenVisibleToo) && isTitleScreenVisibleToo)
        {
            // Chapter selection is open, we go back to character selection if it's chapter 1 selection (otherwise we do nothing)
            UIManager.instance.ShowCharacterSelectionScreen(false);
        }
        else if (isGameRunning && RunManager.instance.fixedCollectibleFoundPanelIsVisible)
        {
            // An item has been found, we don't want it
            RunManager.instance.CollectFixedCollectible(false);
        }
        else if (isGameRunning && gameIsPaused)
        {
            // Game is running, pause screen is visible but not setting screen
            // Unpause the game!
            Pause(false);
        }
    }

    public void ClearSaveFile()
    {
        if (logsVerboseLevel == VerboseLevel.MAXIMAL)
        {
            Debug.Log("Game - Clear save file");
        }

        // Clear save file and create a new one
        bool fileErased = SaveDataManager.instance.EraseSaveFile(true);
        SaveDataManager.instance.CreateEmptySaveFile();

        InitializeStuff();

        // Just in case it was visible
        UIManager.instance.HideSaveFileCorruptedPopUp();

        BackToTitleScreen();
    }

    public void ChangeAvailableCurrency(long currencyChange)
    {
        gameData.availableCurrency += currencyChange;
        SaveDataManager.instance.isSaveDataDirty = true;
    }

    public void SetGameData(GameSaveData saveData)
    {
        gameData = saveData;
        gameData.isFullGame = !BuildManager.instance.demoBuild;
        gameData.versionNumber = BuildManager.instance.versionNumber;
        UIManager.instance.UpdateCurrencyDisplay();
        SetSteamStatIfPossible(totalBugEatenSteamStatName, gameData.cumulatedScore);
        AchievementManager.instance.SteamStoreStats();
    }

    public int GetCompassLevel()
    {
        return gameData.compassLevel;
    }

    #region Steam

    private void SetSteamStatIfPossible(string statKey, int statValue)
    {
        string log = $"Game Manager - Set Steam Stat {statKey} to value {statValue}. ";
        if (SteamManager.Initialized && !BuildManager.instance.demoBuild)
        {
            if (Steamworks.SteamUserStats.SetStat(statKey, statValue))
            {
                log += "Success!";
                Steamworks.SteamUserStats.StoreStats();
            }
            else
            {
                log += "Failed!";
            }
        }
        else
        {
            log += "Abandoned. Steam manager has not been initialized, or this is the demo build.";
        }
        if (logsVerboseLevel == VerboseLevel.MAXIMAL)
        {
            Debug.Log(log);
        }
    }

    #endregion
}
