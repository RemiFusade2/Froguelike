using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using Steamworks;
using System;

/// <summary>
/// UIManager deals with navigation in the menus, as well as in-game UI.
/// </summary>
public class UIManager : MonoBehaviour
{
    public static UIManager instance;

    [Header("Settings")]
    public VerboseLevel logsVerboseLevel = VerboseLevel.NONE;

    #region Title Screen / Start Game

    [Header("Title")]
    public GameObject titleScreen;
    public TextMeshProUGUI titleScreenCurrencyText;
    public TextMeshProUGUI titleScreenWelcomeMessageText;
    public TextMeshProUGUI titleScreenSaveLocationText;
    [Space]
    public Button startButton;
    public GameObject shopButton;
    public GameObject achievementsButton;

    [Header("Shop")]
    public GameObject shopScreen;

    [Header("Achievements")]
    public GameObject achievementsScreen;

    [Header("Character Selection")]
    public GameObject characterSelectionScreen;

    #endregion

    #region In Game

    [Header("Chapter selection")]
    public GameObject chapterSelectionScreen;

    [Header("Chapter Start")]
    public GameObject chapterStartScreen;

    [Header("In game UI")]
    public GameObject inGameUIPanel;

    [Header("Level UP Panel")]
    public GameObject levelUpPanel;
    public Animator levelUpPanelAnimator;

    [Header("Pause")]
    public GameObject pausePanel;
    public Animator pausePanelAnimator;

    #endregion

    #region End Game

    [Header("Game over Screen")]
    public GameObject gameOverPanel;
    public GameObject gameOverRespawnButton;
    public GameObject gameOverGiveUpButton;

    [Header("Score Screen")]
    public GameObject scoreScreen;

    #endregion

    [Header("Confirmation panels")]
    public GameObject backToTitleScreenConfirmationPanel;
    public GameObject clearSaveFileConfirmationPanel;

    [Header("Settings Screen")]
    public GameObject settingsScreen;

    [Header("Demo stuff")]
    public List<GameObject> demoPanelsList;
    public GameObject demoLimitationSticker;
    public TextMeshProUGUI demoLimitationText;

    const string demoRunLimitationStr = "Available Runs: DEMO_RUNCOUNT_LIMIT";
    const string demoTimeLimitationStr = "Remaining: DEMO_TIME_LIMIT";

    [Header("Demo Disclaimer Screen")]
    public GameObject demoDisclaimerScreen;
    public TextMeshProUGUI demoDisclaimerText;

    const string disclaimerTextIntroStr = "This Demo is meant to show a glimpse of what the full game will be. However, it has limits:";
    const string disclaimerTextNoSaveStr = "- your progress will not be saved";
    const string disclaimerTextRunLimitStr = "- you will only be able to play DEMO_RUNCOUNT_LIMIT runs";
    const string disclaimerTextTimeLimitStr = "- you will only be able to play for DEMO_TIME_LIMIT minutes";
    const string disclaimerTextNoLimitStr = "- the content is only a fraction of the full game";

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
    }

    private void Start()
    {
        UpdateDemoPanels();
    }

    private void Update()
    {
        if (GameManager.instance.demoBuild && GameManager.instance.demoLimitationType == DemoLimitationType.TIMER && titleScreen.activeInHierarchy)
        {
            UpdateDemoLimitationSticker();
        }
    }

    public void UpdateDemoPanels()
    {
        foreach (GameObject demoPanel in demoPanelsList)
        {
            demoPanel.SetActive(GameManager.instance.demoBuild);
        }
    }

    private void HideAllScreens()
    {
        EventSystem.current.SetSelectedGameObject(null);
        titleScreen.SetActive(false);
        characterSelectionScreen.SetActive(false);
        chapterSelectionScreen.SetActive(false);
        chapterStartScreen.SetActive(false);
        scoreScreen.SetActive(false);
        inGameUIPanel.SetActive(false);
        gameOverPanel.SetActive(false);
        shopScreen.SetActive(false);
        achievementsScreen.SetActive(false);
        settingsScreen.SetActive(false);
    }

    public void UpdateDemoLimitationSticker()
    {
        if (GameManager.instance.demoBuild && GameManager.instance.demoLimitationType != DemoLimitationType.NONE)
        {
            demoLimitationSticker.SetActive(true);
            switch(GameManager.instance.demoLimitationType)
            {
                case DemoLimitationType.NUMBER_OF_RUNS:
                    int remainingRuns = GameManager.instance.demoRunCountLimit - GameManager.instance.gameData.attempts;
                    demoLimitationText.text = demoRunLimitationStr.Replace("DEMO_RUNCOUNT_LIMIT", remainingRuns.ToString());
                    startButton.interactable = (remainingRuns > 0); // disable start button if there are no more runs
                    break;
                case DemoLimitationType.TIMER:
                    float remainingTimeFloat = GameManager.instance.demoTimeLimit - Time.unscaledTime;
                    remainingTimeFloat = Mathf.Clamp(remainingTimeFloat, 0, float.MaxValue);
                    TimeSpan remainingTime = TimeSpan.FromSeconds(remainingTimeFloat);
                    demoLimitationText.text = demoTimeLimitationStr.Replace("DEMO_TIME_LIMIT", remainingTime.ToString(@"mm\:ss"));
                    startButton.interactable = (remainingTimeFloat > 0); // disable start button if there's no more time
                    break;
                default:
                    break;
            }
        }
        else
        {
            demoLimitationSticker.SetActive(false);
        }
    }

    public void ShowTitleScreen()
    {
        MusicManager.instance.PlayTitleMusic();
        HideAllScreens();
        UpdateTitleScreenCurrencyText(GameManager.instance.gameData.availableCurrency);

        if (demoLimitationSticker != null)
        {
            UpdateDemoLimitationSticker();
        }

        shopButton.SetActive(ShopManager.instance.IsShopUnlocked());
        achievementsButton.SetActive(AchievementManager.instance.IsAchievementsListUnlocked());

        titleScreen.SetActive(true);

        titleScreenSaveLocationText.text = Application.persistentDataPath;
        if (SteamManager.Initialized)
        {
            string steamName = SteamFriends.GetPersonaName();
            titleScreenWelcomeMessageText.text = $"Welcome {steamName}! You are connected to Steam.";
        }

        if (logsVerboseLevel == VerboseLevel.MAXIMAL)
        {
            Debug.Log("UI - Display Title screen");
        }
    }

    public void UpdateCurrencyDisplay()
    {
        long currencyValue = GameManager.instance.gameData.availableCurrency;

        UpdateTitleScreenCurrencyText(currencyValue);
        ShopManager.instance.DisplayShop(false);
    }

    private void UpdateTitleScreenCurrencyText(long currencyValue)
    {
        titleScreenCurrencyText.text = Tools.FormatCurrency(currencyValue, DataManager.instance.currencySymbol);
    }

    public void ShowCharacterSelectionScreen(bool thenGoToChapterSelection)
    {
        HideAllScreens();

        bool isThereCharacterSelection = CharacterManager.instance.UpdateCharacterSelectionScreen();

        titleScreen.SetActive(true);
        characterSelectionScreen.SetActive(isThereCharacterSelection);

        if (thenGoToChapterSelection)
        {
            if (!isThereCharacterSelection)
            {
                // Start Run (show chapter selection)
                CharacterManager.instance.StartRun();
            }
        }

        if (logsVerboseLevel == VerboseLevel.MAXIMAL)
        {
            Debug.Log("UI - Display Character selection screen");
        }
    }

    public void ShowChapterSelectionScreen(bool forceTitleScreen = false)
    {
        HideAllScreens();
        if (forceTitleScreen)
        {
            titleScreen.SetActive(true);
        }
        chapterSelectionScreen.SetActive(true);

        if (logsVerboseLevel == VerboseLevel.MAXIMAL)
        {
            Debug.Log("UI - Display Chapter selection screen");
        }
    }

    public void ShowChapterStart()
    {
        HideAllScreens();
        chapterStartScreen.SetActive(true);
        SoundManager.instance.PlayLongPageSound();

        if (logsVerboseLevel == VerboseLevel.MAXIMAL)
        {
            Debug.Log("UI - Display Chapter start screen");
        }
    }

    public void ShowScoreScreen()
    {
        HideAllScreens();

        // Display score screen
        inGameUIPanel.SetActive(true);
        scoreScreen.SetActive(true);
        SoundManager.instance.PlayLongPageSound();

        if (logsVerboseLevel == VerboseLevel.MAXIMAL)
        {
            Debug.Log("UI - Display Score screen");
        }
    }

    public void ShowGameUI()
    {
        HideAllScreens();
        HidePauseScreen();
        inGameUIPanel.SetActive(true);
    }

    public void ShowGameOver(bool respawnAvailable)
    {
        HideAllScreens();
        inGameUIPanel.SetActive(true);
        gameOverPanel.SetActive(true);
        gameOverRespawnButton.SetActive(respawnAvailable);
        gameOverGiveUpButton.SetActive(!respawnAvailable);
        SoundManager.instance.PlayDeathSound();

        if (logsVerboseLevel == VerboseLevel.MAXIMAL)
        {
            Debug.Log("UI - Display Game over screen");
        }
    }

    public void ShowPauseScreen()
    {
        // Tell the pause screen to update its information.
        pausePanel.GetComponent<PauseScreen>().UpdatePauseScreen();

        // MusicManager.instance.PauseMusic(); // I took this away because I think teh music should still be playing (Johanna).
        // Show the pause screen.
        pausePanel.SetActive(true);
        pausePanelAnimator.SetBool("Visible", true);

        if (logsVerboseLevel == VerboseLevel.MAXIMAL)
        {
            Debug.Log("UI - Display Pause screen");
        }
    }

    public void HidePauseScreen()
    {
        // MusicManager.instance.UnpauseMusic();
        if (pausePanel.activeInHierarchy)
        {
            pausePanelAnimator.SetBool("Visible", false);
        }

        if (logsVerboseLevel == VerboseLevel.MAXIMAL)
        {
            Debug.Log("UI - Hide Pause screen");
        }
    }

    public void ShowShop()
    {
        HideAllScreens();
        ShopManager.instance.DisplayShop(true);
        titleScreen.SetActive(true);
        shopScreen.SetActive(true);

        if (logsVerboseLevel == VerboseLevel.MAXIMAL)
        {
            Debug.Log("UI - Display Shop screen");
        }
    }

    public void ShowAchievements()
    {
        HideAllScreens();
        AchievementManager.instance.DisplayAchievementsScreen();
        titleScreen.SetActive(true);
        achievementsScreen.SetActive(true);

        if (logsVerboseLevel == VerboseLevel.MAXIMAL)
        {
            Debug.Log("UI - Display Achievements screen");
        }
    }

    #region Confirmation Panels

    public void ShowBackToTitleScreenConfirmationPanel(bool active)
    {
        backToTitleScreenConfirmationPanel.SetActive(active);

        if (logsVerboseLevel == VerboseLevel.MAXIMAL)
        {
            Debug.Log($"UI - Display Go back to Title confirmation screen: {active}");
        }
    }

    public void ShowClearSaveFileConfirmationPanel(bool active)
    {
        clearSaveFileConfirmationPanel.SetActive(active);

        if (logsVerboseLevel == VerboseLevel.MAXIMAL)
        {
            Debug.Log($"UI - Display Clear save file confirmation screen: {active}");
        }
    }

    #endregion

    private string GetDemoDisclaimerString(DemoLimitationType limitationType, bool gameIsSaved, int numberOfRuns = 0, float timer = 0)
    {
        string disclaimerString = disclaimerTextIntroStr + "\n";
        if (!gameIsSaved)
        {
            disclaimerString += disclaimerTextNoSaveStr + "\n";
        }
        switch (limitationType)
        {
            case DemoLimitationType.NONE:
                disclaimerString += disclaimerTextNoLimitStr;
                break;
            case DemoLimitationType.NUMBER_OF_RUNS:
                disclaimerString += disclaimerTextRunLimitStr.Replace("DEMO_RUNCOUNT_LIMIT", numberOfRuns.ToString());
                break;
            case DemoLimitationType.TIMER:
                disclaimerString += disclaimerTextTimeLimitStr.Replace("DEMO_TIME_LIMIT", Mathf.FloorToInt(timer / 60).ToString());
                break;
        }

        return disclaimerString;
    }

    public void ShowDemoDisclaimerScreen(bool active, DemoLimitationType limitationType, bool gameIsSaved, int numberOfRuns = 0, float timer = 0)
    {
        if (active && demoDisclaimerText != null)
        {
            string disclaimerStr = GetDemoDisclaimerString(limitationType, gameIsSaved, numberOfRuns, timer);
            demoDisclaimerText.text = disclaimerStr;
        }

        if (demoDisclaimerScreen != null)
        {
            demoDisclaimerScreen.SetActive(active);
        }

        if (logsVerboseLevel == VerboseLevel.MAXIMAL)
        {
            Debug.Log($"UI - Display Demo disclaimer screen: {active}");
        }
    }

    public void HideDemoDisclaimerScreen()
    {
        demoDisclaimerScreen.SetActive(false);
        if (logsVerboseLevel == VerboseLevel.MAXIMAL)
        {
            Debug.Log("UI - Hide Demo disclaimer screen");
        }
    }

    public void ShowSettingsScreen()
    {
        settingsScreen.SetActive(true);

        if (logsVerboseLevel == VerboseLevel.MAXIMAL)
        {
            Debug.Log("UI - Display Settings screen");
        }
    }

    public void HideSettingsScreen()
    {
        settingsScreen.SetActive(false);

        if (logsVerboseLevel == VerboseLevel.MAXIMAL)
        {
            Debug.Log("UI - Hide Settings screen");
        }
    }

}
