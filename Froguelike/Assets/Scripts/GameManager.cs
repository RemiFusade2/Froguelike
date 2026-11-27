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

    public string currentSetModifiers;
    public string currentSetHatName;
    public string currentSetCharacterID;

    public string lastPlayedModifiers;
    public string lastPlayedHatName;
    public string lastPlayedCharacterID;
    public string lastPlayedChapterID;

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
        currentSetModifiers = "";
        currentSetHatName = "";
        currentSetCharacterID = "";
        lastPlayedModifiers = "";
        lastPlayedHatName = "";
        lastPlayedCharacterID = "";
        lastPlayedChapterID = "";
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
        result += "compassLevel = " + compassLevel.ToString() + "\n";
        result += "currentSetModifiers" + currentSetModifiers + "\n";
        result += "currentSetHatName" + currentSetHatName + "\n";
        result += "currentSetCharacterID" + currentSetCharacterID + "\n";
        result += "lastPlayedModifiers" + lastPlayedModifiers + "\n";
        result += "lastPlayedHatName" + lastPlayedHatName + "\n";
        result += "lastPlayedCharacterID" + lastPlayedCharacterID + "\n";
        result += "lastPlayedChapterID" + lastPlayedChapterID;
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
    private const string totalDeathCountSteamStatName = "total_deaths";
    private const string totalChaptersStartedSteamStatName = "total_chapters_started";

    /*
    // Used for saving and loading quick start info
    private const string savedLastSelectedCharacter = "Froguelike last selected character";
    private const string savedLastSelectedGameMode = "Froguelike last selected game mode";
    private const string savedLastSelectedStartingChapter = "Froguelike last selected starting chapter";

    private const string savedLastSelectedCharacterDemo = "Froguelike Demo last selected character";
    private const string savedLastSelectedGameModeDemo = "Froguelike Demo last selected game mode";
    private const string savedLastSelectedStartingChapterDemo = "Froguelike Demo last selected starting chapter";*/

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

        // Clear hats.
        RunManager.instance.player.ClearHats();
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

        // Update the stats on Steam (it's gonna show progress towards the achievements too)
        SetSteamStatIfPossible(totalBugEatenSteamStatName, gameData.cumulatedScore);
        SetSteamStatIfPossible(totalDeathCountSteamStatName, gameData.deathCount);
        SetSteamStatIfPossible(totalChaptersStartedSteamStatName, ChapterManager.instance.GetStartedChaptersCount());
        AchievementManager.instance.SteamStoreStats();
    }

    public void OpenCharacterSelection()
    {
        UIManager.instance.ShowCharacterSelectionScreen(true);
    }

    /*
    // Keys for quick run.
    #region Keys
    private string GetSavedLastSelectedCharacter() // OLD
    {
        return BuildManager.instance.demoBuild ? savedLastSelectedCharacterDemo : savedLastSelectedCharacter;
    }

    private string GetSavedLastSelectedGameMode() // OLD
    {
        return BuildManager.instance.demoBuild ? savedLastSelectedGameModeDemo : savedLastSelectedGameMode;
    }

    
    private string GetSavedLastSelectedStartingChapter() // OLD
    {
        return BuildManager.instance.demoBuild ? savedLastSelectedStartingChapterDemo : savedLastSelectedStartingChapter;
    }
    #endregion Keys

    */
    private bool CheckIfLastPlayedIsAvailable()
    {
        bool isAvailable = false;
        if ((gameData.lastPlayedChapterID == "Endless" && RunModifierManager.instance.IsGameModeUnlocked(GameMode.TIME_ENDLESS)) || (ChapterManager.instance.chaptersData.chaptersList.Find(x => x.chapterID == gameData.lastPlayedChapterID).unlocked && ChapterManager.instance.chaptersData.chaptersList.Find(x => x.chapterID == gameData.lastPlayedChapterID).attemptCountByCharacters.Count > 0))
        {
            if (CharacterManager.instance.charactersData.charactersList.Find(x => x.characterID == gameData.lastPlayedCharacterID).unlocked)
            {
                if (RunModifierManager.instance.IsGameModeUnlocked(Enum.Parse<GameMode>(gameData.lastPlayedModifiers)))
                {
                    if (gameData.lastPlayedHatName == "NONE" || CollectiblesManager.instance.GetFoundHats().Contains(DataManager.instance.GetTypeOfHatFromString(gameData.lastPlayedHatName)))
                    {
                        isAvailable = true;
                    }
                }
            }
        }

        if (isAvailable)
        {
            return true;
        }
        else
        {
            ClearLastPlayedCharacterGameModeStartingChapterAndHat();
            return false;
        }
    }

    public void ClearCurrentSetModifiersAndHatButton()
    {
        gameData.currentSetModifiers = "NONE";
        gameData.currentSetHatName = "NONE";
    }

    public void ClearLastPlayedCharacterGameModeStartingChapterAndHat()
    {
        gameData.lastPlayedModifiers = "";
        gameData.lastPlayedHatName = "";
        gameData.lastPlayedCharacterID = "";
        gameData.lastPlayedChapterID = "";
    }

    public void ClearCurrentSetCharacterGameModeAndHat()
    {
        gameData.currentSetModifiers = "";
        gameData.currentSetHatName = "";
        gameData.currentSetCharacterID = "";
    }

    public void SaveLastPlayedCharacterGameModeStartingChapterAndHat(string characterID, string gameModes, string chapterID, string hatName)
    {
        gameData.lastPlayedModifiers = gameModes;
        gameData.lastPlayedHatName = hatName;
        gameData.lastPlayedCharacterID = characterID;
        gameData.lastPlayedChapterID = chapterID;

        // This means that the current ones are also these options.
        gameData.currentSetModifiers = gameModes;
        gameData.currentSetHatName = hatName;
        gameData.currentSetCharacterID = characterID;

        SaveDataManager.instance.isSaveDataDirty = true;
    }

    public void SaveCurrentSetCharacter(string characterID)
    {
        gameData.currentSetCharacterID = characterID;
        SaveDataManager.instance.isSaveDataDirty = true;
    }

    public void SaveCurrentSetGameMode(string gameModes)
    {
        gameData.currentSetModifiers = gameModes;
        SaveDataManager.instance.isSaveDataDirty = true;
    }

    public void SaveCurrentSetHat(string hatName)
    {
        gameData.currentSetHatName = hatName;
        SaveDataManager.instance.isSaveDataDirty = true;
    }

    public string GetCurrentSetCharacterID()
    {
        if (gameData.currentSetCharacterID == "" || gameData.currentSetCharacterID == null)
        {
            return "CLASSIC_FROG";
        }
        else
        {
            return gameData.currentSetCharacterID;
        }
    }

    public string GetCurrentSetGameModes()
    {
        if (gameData.currentSetModifiers == "" || gameData.currentSetModifiers == null)
        {
            return "NONE";
        }
        else
        {
            return gameData.currentSetModifiers;
        }
    }

    public string GetCurrentSetHatName()
    {
        if (gameData.currentSetHatName == "" || gameData.currentSetHatName == null)
        {
            return "NONE";
        }
        else
        {
            return gameData.currentSetHatName;
        }
    }

    // Checks if the quick start/try again buttons should be shown.
    public bool AreThereQuickStartOptions()
    {
        if (gameData.lastPlayedModifiers == "" || gameData.lastPlayedHatName == "" || gameData.lastPlayedCharacterID == "" || gameData.lastPlayedChapterID == "")
        {
            return false;
        }
        else if (gameData.lastPlayedModifiers == null || gameData.lastPlayedHatName == null || gameData.lastPlayedCharacterID == null || gameData.lastPlayedChapterID == null)
        {
            return false;
        }
        else if (!CheckIfLastPlayedIsAvailable())
        {
            return false;
        }
        else
        {
            return true;
        }
    }

    public void QuickStartNewRun()
    {
        // Set up character, game modes and chapter.
        CharacterManager.instance.currentSelectedCharacter = CharacterManager.instance.GetPlayableCharacter(GetRetryRunInfoCharacterID());
        RunModifierManager.instance.selectedGameModes = Enum.Parse<GameMode>(GetRetryRunInfoGameModes());
        Chapter startChapter = ChapterManager.instance.GetChapterFromID(GetRetryRunInfoChapterID());

        // Set and add the hat.
        RunModifierManager.instance.selectedHatName = GetRetryRunInfoHatName();
        RunManager.instance.player.ClearHats();
        RunManager.instance.player.AddHat(DataManager.instance.GetTypeOfHatFromString(GetRetryRunInfoHatName()));

        // Start the run.
        RunManager.instance.StartNewRun(CharacterManager.instance.currentSelectedCharacter, RunModifierManager.instance.selectedGameModes, startChapter);
    }

    #region Get info helpers

    public string GetRetryRunInfoCharacterID()
    {
        string characterID;
        if (gameData.lastPlayedCharacterID != "")
        {
            characterID = gameData.lastPlayedCharacterID;
        }
        else // Default.
        {
            characterID = "CLASSIC_FROG";
        }

        return characterID;
    }

    public string GetRetryRunInfoGameModes()
    {
        string gameModes;
        if (gameData.lastPlayedModifiers != "")
        {
            gameModes = gameData.lastPlayedModifiers;
        }
        else // Default.
        {
            gameModes = "NONE";
        }

        return gameModes;
    }

    public string GetRetryRunInfoChapterID()
    {
        string chapterID;
        if (gameData.lastPlayedCharacterID != "")
        {
            chapterID = gameData.lastPlayedChapterID;
        }
        else // Default.
        {
            chapterID = "[CH_COLLECT_HEALTH]"; // "First hops" is the fallback.
        }

        return chapterID;
    }

    public string GetRetryRunInfoHatName()
    {
        string hatName;
        if (gameData.lastPlayedHatName != "")
        {
            hatName = gameData.lastPlayedHatName;
        }
        else // Default.
        {
            hatName = "NONE";
        }

        return hatName;
    }

    #endregion Get info helpers

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
            case RewardFeatureType.CHAPTER_COLLECTION:
                ChapterManager.instance.UnlockChapterCollection();
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
                //CharacterManager.instance.IncrementCharacterStats("TOAD", new List<StatValue>() { new StatValue(CharacterStat.ARMOR, 2) });
                break;
            case RewardFeatureType.KERMIT_BUFF:
                CharacterManager.instance.SetCharacterStoryCompleted("SWIMMING_FROG");
                break;
            case RewardFeatureType.FROG_BUFF:
                CharacterManager.instance.SetCharacterStoryCompleted("CLASSIC_FROG");
                break;
                // TODO add all game modes here.
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
        ChapterManager.instance.chapterChoiceIsVisible = false;
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

    #endregion Pause

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
            if (ShopManager.instance.IsPermanentUpgradePopUpVisible())
            {
                // Permanent upgrade pop up is active
                if (ShopManager.instance.IsPermanentUpgradePopUpCloseButtonVisible())
                {
                    // Pop up can be closed, so we close it
                    ShopManager.instance.ClosePermanentUpgradePopUp(true);
                }
            }
            else
            {
                // Shop is open, we hide shop
                UIManager.instance.HideShop();
            }
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

        // Clear Quick start options
        // RemoveSelectedCharacterGameModeAndStartingChapter();

        // Clear game mode.
        RunModifierManager.instance.ResetModifiers();

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
        SetSteamStatIfPossible(totalDeathCountSteamStatName, gameData.deathCount);
        SetSteamStatIfPossible(totalChaptersStartedSteamStatName, ChapterManager.instance.GetStartedChaptersCount());
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

    #endregion Steam
}
