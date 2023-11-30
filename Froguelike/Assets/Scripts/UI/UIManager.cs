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
    public GameObject menuButtonsGroup;
    public GameObject selectedButtonTitleScreen;
    public TextMeshProUGUI titleScreenCurrencyText;
    public TextMeshProUGUI titleScreenWelcomeMessageText;
    public TextMeshProUGUI titleScreenSaveLocationText;
    [Space]
    public Button startButton;
    public GameObject shopButton;
    public GameObject achievementsButton;
    public GameObject settingsButton;

    [Header("Shop")]
    public GameObject shopScreen;
    private GameObject selectedButtonShopScreen;

    [Header("Achievements")]
    public GameObject achievementsScreen;
    public GameObject selectedButtonAchievementsScreen;

    [Header("Character Selection")]
    public GameObject characterSelectionScreen;
    public GameObject characterSelectionGridLayoutGroup;

    [Header("Settings Screen")]
    public GameObject settingsScreen;
    public GameObject selectedButtonSettingsScreen;

    [Header("Credits")]
    public CreditsScreenBehaviour creditsScreen;

    #endregion

    #region In Game

    [Header("Chapter selection")]
    public GameObject chapterSelectionScreen;
    public List<GameObject> chapterSelectionButtons;

    [Header("Chapter Start")]
    public GameObject chapterStartScreen;

    [Header("In game UI")]
    public GameObject inGameUIPanel;

    [Header("Level UP Panel")]
    public GameObject levelUpPanel;
    public Animator levelUpPanelAnimator;
    public GameObject selectedButtonLevelUpPanel;

    [Header("Pause")]
    public GameObject pausePanel;
    public Animator pausePanelAnimator;
    public GameObject selectedButtonPausePanel;
    private bool makeLevelUpPanelInteractableAfterClosingPausePanel = false;

    #endregion

    #region End Game

    [Header("Game over Screen")]
    public GameObject gameOverPanel;
    public GameObject gameOverRespawnButton;
    public GameObject gameOverGiveUpButton;

    [Header("Score Screen")]
    public GameObject scoreScreen;
    public GameObject selectedButtonScoreScreen;

    #endregion

    [Header("Confirmation panels")]
    public GameObject backToTitleScreenConfirmationPanel;
    public GameObject selectedButtonBackToTitleScreenConfirmationPanel;
    public GameObject clearSaveFileConfirmationPanel;
    public GameObject selectedButtonClearSaveFileConfirmationPanel;

    [Header("Demo stuff")]
    public List<GameObject> demoPanelsList;
    [Space]
    public GameObject demoLimitationSticker;
    public TextMeshProUGUI demoLimitationText;
    [Space]
    public GameObject demoDisclaimerScreen;
    public TextMeshProUGUI demoDisclaimerText;
    public GameObject demoDisclaimerOkButton;
    [Space]
    public GameObject endOfDemoScreen;
    public GameObject endOfDemoSteamButton;

    private List<GameObject> rememberThisButton = new List<GameObject>();

    const string demoRunLimitationStr = "Available Runs: DEMO_RUNCOUNT_LIMIT";
    const string demoTimeLimitationStr = "Remaining: DEMO_TIME_LIMIT";

    const string disclaimerTextIntroStr = "This Demo is meant to show a glimpse of what the full game will be. However, it is limited.";
    const string disclaimerTextNoSaveStr = "- your progress will not be saved";
    const string disclaimerTextRunLimitStr = "- you will only be able to play DEMO_RUNCOUNT_LIMIT runs";
    const string disclaimerTextTimeLimitStr = "- you will only be able to play for DEMO_TIME_LIMIT minutes";
    const string disclaimerTextNoLimitStr = "The content is only a fraction of the full game.";

    [Header("Runtime")]
    public bool endOfDemoHasBeenShown;
    private float removedDisclaimerTime;

    public void StartDemoTimer()
    {
        removedDisclaimerTime = Time.unscaledTime;
    }

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
        endOfDemoHasBeenShown = false;
        removedDisclaimerTime = 24 * 60 * 60;
    }

    private void Start()
    {
        UpdateDemoPanels();
        SetScreenInteractability(pausePanel, false);
    }

    private void Update()
    {
        /*
         * UNCOMMENT THIS PART IF YOU WANT A TIME LIMIT (VISIBLE ON THE TITLE SCREEN) THAT IS UPDATED EVERY FRAME
         * 
        if (GameManager.instance.demoBuild 
            && GameManager.instance.demoLimitationType == DemoLimitationType.TIMER 
            && titleScreen.activeInHierarchy
            && (demoDisclaimerScreen == null || (demoDisclaimerScreen != null && !demoDisclaimerScreen.activeInHierarchy)))
        {
            UpdateDemoLimitationSticker();
        }*/
    }

    public void UpdateDemoPanels()
    {
        foreach (GameObject demoPanel in demoPanelsList)
        {
            demoPanel.GetComponentInChildren<Button>().interactable = GameManager.instance.demoBuild;
            demoPanel.SetActive(GameManager.instance.demoBuild);

        }
    }

    private void HideAllScreens()
    {
        // EventSystem.current.SetSelectedGameObject(null);
        ClearSelectedButton();
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
        creditsScreen.gameObject.SetActive(false);
    }

    public void UpdateDemoLimitationSticker()
    {
        bool showEndOfDemoScreen = false;
        if (GameManager.instance.demoBuild)
        {
            if (GameManager.instance.demoLimitationType != DemoLimitationType.NONE)
            {
                // Demo limitation sticker must be visible and up to date
                demoLimitationSticker.SetActive(true);
                switch (GameManager.instance.demoLimitationType)
                {
                    case DemoLimitationType.NUMBER_OF_RUNS:
                        int remainingRuns = GameManager.instance.demoRunCountLimit - GameManager.instance.gameData.attempts;
                        demoLimitationText.text = demoRunLimitationStr.Replace("DEMO_RUNCOUNT_LIMIT", remainingRuns.ToString());
                        startButton.interactable = (remainingRuns > 0); // disable start button if there are no more runs
                        break;
                    case DemoLimitationType.TIMER:
                        float remainingTimeFloat = GameManager.instance.demoTimeLimit - (Time.unscaledTime - removedDisclaimerTime);
                        remainingTimeFloat = Mathf.Clamp(remainingTimeFloat, 0, float.MaxValue);
                        TimeSpan remainingTime = TimeSpan.FromSeconds(remainingTimeFloat);
                        demoLimitationText.text = demoTimeLimitationStr.Replace("DEMO_TIME_LIMIT", remainingTime.ToString(@"mm\:ss"));
                        startButton.interactable = (remainingTimeFloat > 0); // disable start button if there's no more time
                        break;
                    default:
                        break;
                }
                if (!startButton.interactable && !endOfDemoHasBeenShown && endOfDemoScreen != null)
                {
                    showEndOfDemoScreen = true;
                }
            }
            else
            {
                // There's no limitations, so no sticker to update
                // But it doesn't mean we don't have to check for the end-of-demo screen
                if (AchievementManager.instance.AllDemoAchievementsHaveBeenUnlocked() && !endOfDemoHasBeenShown && endOfDemoScreen != null)
                {
                    showEndOfDemoScreen = true;
                }
            }

            if (showEndOfDemoScreen)
            {
                endOfDemoHasBeenShown = true;
                // SetScreenInteractability(titleScreen, false); (This makes the end of demo screen not interactable as well, since it is a child of title screen /J)
                SetScreenInteractability(menuButtonsGroup, false);
                SetScreenInteractability(endOfDemoScreen, true);
                endOfDemoScreen.SetActive(true);
                SetSelectedButton(endOfDemoSteamButton);

                if (logsVerboseLevel == VerboseLevel.MAXIMAL)
                {
                    Debug.Log("UI - Display End-of-demo screen");
                }
            }
        }
    }

    public void ShowTitleScreen()
    {
        MusicManager.instance.PlayTitleMusic();
        HideAllScreens();
        UpdateTitleScreenCurrencyText(GameManager.instance.gameData.availableCurrency);

        shopButton.SetActive(ShopManager.instance.IsShopUnlocked());
        achievementsButton.SetActive(AchievementManager.instance.IsAchievementsListUnlocked());

        titleScreen.SetActive(true);
        SetScreenInteractability(menuButtonsGroup, true);

        // Set a selected button.
        SetSelectedButton(selectedButtonTitleScreen);

        rememberThisButton.Clear();

        /*titleScreenSaveLocationText.text = Application.persistentDataPath;
        if (SteamManager.Initialized)
        {
            string steamName = SteamFriends.GetPersonaName();
            titleScreenWelcomeMessageText.text = $"Welcome {steamName}! You are connected to Steam.";
        }*/

        if (demoLimitationSticker != null)
        {
            demoLimitationSticker.SetActive(false);
            UpdateDemoLimitationSticker();
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
        SetScreenInteractability(menuButtonsGroup, !isThereCharacterSelection);
        characterSelectionScreen.SetActive(isThereCharacterSelection);

        if (isThereCharacterSelection)
        {
            // Pick the first character button.
            SetSelectedButton(characterSelectionGridLayoutGroup.GetComponentInChildren<Transform>().GetComponentInChildren<Button>().gameObject);
        }
        else
        {
            SetSelectedButton(selectedButtonTitleScreen);
        }

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
            SetScreenInteractability(menuButtonsGroup, false);
        }
        chapterSelectionScreen.SetActive(true);

        // Pick the first chapter option as the selected button.
        GameObject selectedButton = chapterSelectionButtons[4].activeSelf ? chapterSelectionButtons[4] : chapterSelectionButtons[0];
        SetSelectedButton(selectedButton);

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
        SetScreenInteractability(pausePanel, false);
        SetScreenInteractability(levelUpPanel, false);

        // Display score screen
        inGameUIPanel.SetActive(true);
        scoreScreen.SetActive(true);
        SoundManager.instance.PlayLongPageSound();

        SetSelectedButton(selectedButtonScoreScreen);

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
        SoundManager.instance.UnpauseInGameLoopedSFX();
    }

    public void ShowGameOver(int respawnsAvailable)
    {
        // HideAllScreens();
        // inGameUIPanel.SetActive(true);
        bool respawnAvailable = respawnsAvailable > 0;

        SoundManager.instance.PauseInGameLoopedSFX();
        gameOverPanel.GetComponent<GameOverScreen>().UpdateGameOverScreen();
        gameOverRespawnButton.GetComponent<Button>().interactable = respawnAvailable;
        gameOverPanel.SetActive(true);
        SetSelectedButton(respawnAvailable ? gameOverRespawnButton : gameOverGiveUpButton);

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

        // Set up start button and navigation.
        SetScreenInteractability(pausePanel, true);
        if (levelUpPanel.GetComponent<CanvasGroup>().interactable)
        {
            SetScreenInteractability(levelUpPanel, false);
            makeLevelUpPanelInteractableAfterClosingPausePanel = true;
            SavePreviousSelectedButton();
        }

        SetSelectedButton(selectedButtonPausePanel);

        SoundManager.instance.PauseInGameLoopedSFX();
        // MusicManager.instance.PauseMusic(); // I took this away because I think teh music should still be playing (Johanna). // I agree that this is better but SFX should be stopped no? (R�mi)
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
        SoundManager.instance.UnpauseInGameLoopedSFX();
        // MusicManager.instance.UnpauseMusic();
        if (pausePanel.activeInHierarchy && pausePanelAnimator.GetBool("Visible"))
        {
            pausePanelAnimator.SetBool("Visible", false);

            // Make pause panel not interactable.
            SetScreenInteractability(pausePanel, false);

            if (makeLevelUpPanelInteractableAfterClosingPausePanel)
            {
                SetScreenInteractability(levelUpPanel, true);
                SetPreviousSelectedButton();
                //               SetSelectedButton(selectedButtonLevelUpPanel);
            }
        }

        if (logsVerboseLevel == VerboseLevel.MAXIMAL)
        {
            Debug.Log("UI - Hide Pause screen");
        }
    }

    public void ShowShop()
    {
        SavePreviousSelectedButton();
        HideAllScreens();
        ShopManager.instance.DisplayShop(true);
        titleScreen.SetActive(true);
        SetScreenInteractability(menuButtonsGroup, false);
        shopScreen.SetActive(true);
        selectedButtonShopScreen = ShopManager.instance.shopPanel.GetComponentInChildren<Button>().gameObject;
        SetSelectedButton(selectedButtonShopScreen);

        if (logsVerboseLevel == VerboseLevel.MAXIMAL)
        {
            Debug.Log("UI - Display Shop screen");
        }
    }

    public void HideShop()
    {
        shopScreen.SetActive(false);
        UpdateTitleScreenCurrencyText(GameManager.instance.gameData.availableCurrency);
        SetScreenInteractability(menuButtonsGroup, true);
        SetPreviousSelectedButton();
    }

    public void ShowAchievements()
    {
        SavePreviousSelectedButton();
        HideAllScreens();
        AchievementManager.instance.DisplayAchievementsScreen();
        titleScreen.SetActive(true);
        SetScreenInteractability(menuButtonsGroup, false);
        achievementsScreen.SetActive(true);
        SetSelectedButton(selectedButtonAchievementsScreen);

        if (logsVerboseLevel == VerboseLevel.MAXIMAL)
        {
            Debug.Log("UI - Display Achievements screen");
        }
    }

    public void HideAchievements()
    {
        achievementsScreen.SetActive(false);
        SetScreenInteractability(menuButtonsGroup, true);
        SetPreviousSelectedButton();
    }

    public void ShowCreditsScreen()
    {
        SavePreviousSelectedButton();
        SetScreenInteractability(menuButtonsGroup, false);
        creditsScreen.Reset();
        creditsScreen.gameObject.SetActive(true);
    }

    public void HideCreditsScreen()
    {
        creditsScreen.gameObject.SetActive(false);
        SetScreenInteractability(menuButtonsGroup, true);
        SetPreviousSelectedButton();
    }


    #region Confirmation Panels

    public void ShowBackToTitleScreenConfirmationPanel(bool active)
    {
        backToTitleScreenConfirmationPanel.SetActive(active);

        if (active)
        {
            SavePreviousSelectedButton();
            SetSelectedButton(selectedButtonBackToTitleScreenConfirmationPanel);
        }
        else
        {
            SetPreviousSelectedButton();
        }

        if (logsVerboseLevel == VerboseLevel.MAXIMAL)
        {
            Debug.Log($"UI - Display Go back to Title confirmation screen: {active}");
        }
    }

    public void ShowClearSaveFileConfirmationPanel(bool active)
    {
        clearSaveFileConfirmationPanel.SetActive(active);

        if (active)
        {
            SavePreviousSelectedButton();
            SetSelectedButton(selectedButtonClearSaveFileConfirmationPanel);
        }
        else
        {
            SetPreviousSelectedButton();
        }

        if (logsVerboseLevel == VerboseLevel.MAXIMAL)
        {
            Debug.Log($"UI - Display Clear save file confirmation screen: {active}");
        }
    }

    #endregion Confirmation Panels

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
            // SetScreenInteractability(titleScreen, !active); // (since this is a parent of the demo disclamer it also makes the demo disclamer not interactable, so I commented it out /J)
            SetScreenInteractability(menuButtonsGroup, !active);
            SetScreenInteractability(demoDisclaimerScreen, active);
            demoDisclaimerScreen.SetActive(active);
            if (active)
            {
                SetSelectedButton(demoDisclaimerOkButton);
            }
            else
            {
                SetSelectedButton(selectedButtonTitleScreen);
            }
        }

        if (logsVerboseLevel == VerboseLevel.MAXIMAL)
        {
            Debug.Log($"UI - Display Demo disclaimer screen: {active}");
        }
    }

    public void HideDemoDisclaimerScreen()
    {
        if (demoDisclaimerScreen.activeInHierarchy)
        {
            StartDemoTimer();
        }

        SetScreenInteractability(demoDisclaimerScreen, false);
        demoDisclaimerScreen.SetActive(false);

        if (endOfDemoScreen.activeInHierarchy)
        {
            SetScreenInteractability(endOfDemoScreen, true);
            SetSelectedButton(endOfDemoSteamButton);
        }
        else
        {
            SetScreenInteractability(titleScreen, true);
            SetScreenInteractability(menuButtonsGroup, true);
            SetSelectedButton(selectedButtonTitleScreen);

            UpdateDemoLimitationSticker();
        }

        if (logsVerboseLevel == VerboseLevel.MAXIMAL)
        {
            Debug.Log("UI - Hide Demo disclaimer screen");
        }
    }

    public void HideEndOfDemoScreen()
    {
        if (endOfDemoScreen != null)
        {
            SetScreenInteractability(titleScreen, true);
            SetScreenInteractability(menuButtonsGroup, true);
            SetScreenInteractability(endOfDemoScreen, false);
            endOfDemoScreen.SetActive(false);
            SetSelectedButton(settingsButton);
            if (logsVerboseLevel == VerboseLevel.MAXIMAL)
            {
                Debug.Log("UI - Hide End of Demo screen");
            }
        }
    }

    public void ShowSettingsScreen()
    {
        settingsScreen.SetActive(true);
        SavePreviousSelectedButton();
        SetSelectedButton(selectedButtonSettingsScreen);

        if (logsVerboseLevel == VerboseLevel.MAXIMAL)
        {
            Debug.Log("UI - Display Settings screen");
        }
    }

    public void HideSettingsScreen()
    {
        settingsScreen.SetActive(false);
        SetPreviousSelectedButton();

        if (logsVerboseLevel == VerboseLevel.MAXIMAL)
        {
            Debug.Log("UI - Hide Settings screen");
        }
    }

    #region Buttons

    public void SetSelectedButton(GameObject buttonGO)
    {
        if (buttonGO != null) ClearSelectedButton();

        // Set selected button.
        EventSystem.current.SetSelectedGameObject(buttonGO);
    }

    private void SavePreviousSelectedButton()
    {
        rememberThisButton.Add(EventSystem.current.currentSelectedGameObject);
    }

    private void SetPreviousSelectedButton()
    {
        if (rememberThisButton.Count > 0)
        {
            int lastButton = rememberThisButton.Count - 1;
            SetSelectedButton(rememberThisButton[lastButton]);
            rememberThisButton.RemoveAt(lastButton);
        }
    }

    private void ClearSelectedButton()
    {
        SetSelectedButton(null);
    }

    #endregion Buttons

    private void SetScreenInteractability(GameObject screen, bool isInteractable)
    {
        if (screen.GetComponent<CanvasGroup>())
        {
            screen.GetComponent<CanvasGroup>().interactable = isInteractable;
        }
        else
        {
            Debug.Log(screen.name + " doesn't have a canvas group");
        }
    }
}
