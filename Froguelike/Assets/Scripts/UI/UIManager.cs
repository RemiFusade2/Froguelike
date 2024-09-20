using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using Steamworks;
using System;
using Unity.VisualScripting;
using System.IO;
using UnityEditor;

/// <summary>
/// UIManager deals with navigation in the menus, as well as in-game UI.
/// </summary>
public class UIManager : MonoBehaviour
{
    public static UIManager instance;

    [Header("Settings")]
    public VerboseLevel logsVerboseLevel = VerboseLevel.NONE;

    [Header("Version")]
    public TextMeshProUGUI versionNumberText;

    #region Title Screen / Start Game

    [Header("Title")]
    public GameObject titleScreen;
    public GameObject menuButtonsGroup;
    public GameObject selectedButtonTitleScreen;
    public GameObject shopButtonActiveNote;
    public GameObject shopButtonLockedNote;
    public TextMeshProUGUI titleScreenCurrencyText;
    public TextMeshProUGUI titleScreenWelcomeMessageText;
    public TextMeshProUGUI titleScreenSaveLocationText;
    [Space]
    public Button startButton;
    public GameObject quickStartButton;
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

    [Header("Disclaimers")]
    public DisclaimerScreen DemoDisclaimerScreen;
    public DisclaimerScreen EADisclaimerScreen;

    [Header("Error pop ups")]
    public GameObject saveFileCorruptedPopUp;
    public Button saveFileCorruptedPopUpDefaultSelectedButton;

    #endregion

    #region In Game

    [Header("Chapter selection")]
    public GameObject chapterSelectionScreen;
    public List<GameObject> chapterSelectionButtons;

    [Header("Chapter Start")]
    public GameObject chapterStartScreen;

    [Header("In game UI")]
    public GameObject inGameUIPanel;
    public GameObject inGameCountUI;

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
    public GameObject tryAgainButton;

    #endregion

    [Header("Confirmation panels")]
    public GameObject backToTitleScreenConfirmationPanel;
    public GameObject selectedButtonBackToTitleScreenConfirmationPanel;
    [Space]
    public GameObject clearSaveFileConfirmationPanel;
    public GameObject selectedButtonClearSaveFileConfirmationPanel;
    [Space]
    public GameObject rerollWarningConfirmationPanel;
    public GameObject selectedButtonRerollWarningConfirmationPanel;

    [Header("Demo stuff")]
    public List<GameObject> demoPanelsList;
    [Space]
    public GameObject endOfDemoScreen;
    public GameObject endOfDemoSteamButton;

    [Header("Showcase stuff")]
    public GameObject showcaseWarningPanel;
    public TextMeshProUGUI showcaseWarningText;
    public string showcaseWarningString = "Restarting the game in X seconds (no input)";
    [Space]
    public GameObject ShowcaseCTAOverlayPanel;

    private List<GameObject> rememberThisButton = new List<GameObject>();

    [Header("Links Settings")]
    public bool useSteamOverlay = true;

    [Header("Runtime")]
    public bool endOfDemoHasBeenShown;

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
    }

    private void Start()
    {
        UpdateDemoPanels();
        SetScreenInteractability(pausePanel, false);

        string versionNumber = BuildManager.instance.demoBuild ? "Demo." : "Early Access.";
        string fullVersionNumber = versionNumber + BuildManager.instance.versionNumber;
        if (BuildManager.instance.showcaseBuild)
        {
            fullVersionNumber = "Showcase Build";
        }
        versionNumberText.text = fullVersionNumber;
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
            demoPanel.GetComponentInChildren<Button>().interactable = BuildManager.instance.demoBuild;
            demoPanel.SetActive(BuildManager.instance.demoBuild);
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

        rerollWarningConfirmationPanel.SetActive(false);
        clearSaveFileConfirmationPanel.SetActive(false);
        backToTitleScreenConfirmationPanel.SetActive(false);
    }

    public void ShowTitleScreen()
    {
        MusicManager.instance.PlayTitleMusic();
        HideAllScreens();
        UpdateTitleScreenCurrencyText(GameManager.instance.gameData.availableCurrency);

        bool shopAndQuestsButtonsAreAlwaysVisible = true;
        if (shopAndQuestsButtonsAreAlwaysVisible)
        {
            shopButton.SetActive(true);
            shopButton.GetComponent<Button>().interactable = ShopManager.instance.IsShopUnlocked();
            shopButton.GetComponent<CanvasGroup>().blocksRaycasts = ShopManager.instance.IsShopUnlocked();
            if (!ShopManager.instance.IsShopUnlocked())
            {
                shopButtonActiveNote.SetActive(false);
                shopButtonLockedNote.SetActive(true);
            }
            else
            {
                shopButtonActiveNote.SetActive(true);
                shopButtonLockedNote.SetActive(false);
            }

            achievementsButton.SetActive(true);
            achievementsButton.GetComponent<Button>().interactable = AchievementManager.instance.IsAchievementsListUnlocked();
            achievementsButton.GetComponent<CanvasGroup>().blocksRaycasts = AchievementManager.instance.IsAchievementsListUnlocked();
        }
        else
        {
            shopButton.SetActive(ShopManager.instance.IsShopUnlocked());
            achievementsButton.SetActive(AchievementManager.instance.IsAchievementsListUnlocked());
        }

        // Quick start button
        quickStartButton.SetActive(GameManager.instance.AreThereQuickStartOptions());

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
        titleScreenCurrencyText.text = Tools.FormatCurrency(currencyValue, " " + DataManager.instance.currencyName);
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

    public bool IsCharacterSelectionScreenVisible()
    {
        return characterSelectionScreen.activeInHierarchy;
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
        SetSelectedButton(chapterSelectionButtons[0]);

        if (logsVerboseLevel == VerboseLevel.MAXIMAL)
        {
            Debug.Log("UI - Display Chapter selection screen");
        }
    }

    public bool IsChapterSelectionScreenVisible(out bool isTitleScreenAlsoVisible)
    {
        isTitleScreenAlsoVisible = titleScreen.activeInHierarchy;
        return chapterSelectionScreen.activeInHierarchy;
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

        // Try again button
        tryAgainButton.SetActive(GameManager.instance.AreThereQuickStartOptions());

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

    public bool IsScoreScreenVisible()
    {
        return scoreScreen.activeInHierarchy;
    }

    public void ShowGameUI()
    {
        HideAllScreens();
        HidePauseScreen();
        inGameUIPanel.SetActive(true);
        SoundManager.instance.UnpauseInGameLoopedSFX();
    }

    public void ShowCountUI(bool showCount)
    {
        inGameCountUI.SetActive(showCount);
    }

    public void ShowGameOver(int respawnsAvailable)
    {
        // HideAllScreens();
        // inGameUIPanel.SetActive(true);
        bool respawnAvailable = respawnsAvailable > 0;

        SoundManager.instance.PauseInGameLoopedSFX();
        gameOverPanel.GetComponent<GameOverScreen>().UpdateGameOverScreen();
        gameOverRespawnButton.GetComponent<Button>().interactable = respawnAvailable;
        gameOverRespawnButton.GetComponent<CanvasGroup>().blocksRaycasts = respawnAvailable;
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
        try
        {
            // Tell the pause screen to update its information.
            pausePanel.GetComponent<PauseScreen>().UpdatePauseScreen();
        }
        catch (Exception e)
        {
            Debug.LogError($"Exception when calling UpdatePauseScreen(): {e.Message}");
        }

        try
        {
            // Set up start button and navigation.
            SetScreenInteractability(pausePanel, true);
            if (levelUpPanel.GetComponent<CanvasGroup>().interactable)
            {
                SetScreenInteractability(levelUpPanel, false);
                makeLevelUpPanelInteractableAfterClosingPausePanel = true;
                SavePreviousSelectedButton();
            }
            else
            {
                makeLevelUpPanelInteractableAfterClosingPausePanel = false;
            }

            SetSelectedButton(selectedButtonPausePanel);
        }
        catch (Exception e)
        {
            Debug.LogError($"Exception when attempting to set up start button and navigation: {e.Message}");
        }

        SoundManager.instance.PauseInGameLoopedSFX();
        // MusicManager.instance.PauseMusic(); // I took this away because I think teh music should still be playing (Johanna). // I agree that this is better but SFX should be stopped no? (Remi)
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
            }
            else
            {
                // Make sure there isn't a selected button if the game is unpaused (since the pause panel doesn't have automatic button selection this need to be here to not be able to navigate between the buttons when the pause panel is closed)
                EventSystem.current.SetSelectedGameObject(null);
            }
        }

        if (logsVerboseLevel == VerboseLevel.MAXIMAL)
        {
            Debug.Log("UI - Hide Pause screen");
        }
    }

    #region Shop

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

    public bool IsShopScreenVisible()
    {
        return shopScreen.activeInHierarchy;
    }

    #endregion Shop

    #region Quests

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

    public bool IsAchievementScreenVisible()
    {
        return achievementsScreen.activeInHierarchy;
    }

    #endregion Quests

    #region Credits

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

    public bool IsCreditsScreenVisible()
    {
        return creditsScreen.gameObject.activeInHierarchy;
    }

    #endregion


    #region Confirmation Panels

    public void ShowRerollWarningConfirmationPanel(bool active)
    {
        rerollWarningConfirmationPanel.SetActive(active);

        if (active)
        {
            SavePreviousSelectedButton();
            SetSelectedButton(selectedButtonRerollWarningConfirmationPanel);
        }
        else
        {
            SetPreviousSelectedButton();
        }

        if (logsVerboseLevel == VerboseLevel.MAXIMAL)
        {
            Debug.Log($"UI - Display Reroll Warning confirmation screen: {active}");
        }
    }

    public bool IsRerollWarningConfirmationPanelActive()
    {
        return rerollWarningConfirmationPanel.activeInHierarchy;
    }

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

    public bool IsBackToTitleScreenConfirmationPanelActive()
    {
        return backToTitleScreenConfirmationPanel.activeInHierarchy;
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

    public bool IsClearSaveFileConfirmationPanelActive()
    {
        return clearSaveFileConfirmationPanel.activeInHierarchy;
    }

    #endregion Confirmation Panels

    #region Disclaimer screens & end of demo 

    public void TryShowDisclaimerScreen()
    {
        DemoDisclaimerScreen.HideDisclaimer();
        EADisclaimerScreen.HideDisclaimer();
        DisclaimerScreen disclaimerScreen = GetDisclaimerScreenForCurrentBuild();
        if (disclaimerScreen != null)
        {
            bool isDisclaimerVisible = disclaimerScreen.TryShowDisclaimer();
            SetScreenInteractability(menuButtonsGroup, !isDisclaimerVisible);
            SetScreenInteractability(disclaimerScreen.gameObject, isDisclaimerVisible);
            if (!isDisclaimerVisible)
            {
                SetSelectedButton(selectedButtonTitleScreen);
            }
            if (logsVerboseLevel == VerboseLevel.MAXIMAL)
            {
                Debug.Log($"UI - Display disclaimer screen: {isDisclaimerVisible}");
            }
        }
    }

    private DisclaimerScreen GetDisclaimerScreenForCurrentBuild()
    {
        DisclaimerScreen result = null;
        if (BuildManager.instance != null && !BuildManager.instance.showcaseBuild)
        {
            if (BuildManager.instance.demoBuild && BuildManager.instance.showDemoDisclaimer)
            {
                result = DemoDisclaimerScreen;
            }
            else if (!BuildManager.instance.demoBuild && BuildManager.instance.showEADisclaimer)
            {
                result = EADisclaimerScreen;
            }
        }
        return result;
    }

    public void DisableDisclaimerScreen(GameObject previousScreen)
    {
        // Disable disclaimer
        previousScreen.SetActive(false);
        SetScreenInteractability(previousScreen, false);

        if (saveFileCorruptedPopUp.activeInHierarchy)
        {
            // Next screen after disclaimer is the error message, because loading the save file failed
            SetScreenInteractability(saveFileCorruptedPopUp, true);
            SetSelectedButton(saveFileCorruptedPopUpDefaultSelectedButton);
        }
        else
        {
            // No error message, next screen after disclaimer is title screen
            SetScreenInteractability(titleScreen, true);
            SetScreenInteractability(menuButtonsGroup, true);
            SetSelectedButton(startButton);
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

    #endregion Disclaimer screens & end of demo

    #region Error pop ups

    public void ShowSaveFileCorruptedPopUp()
    {
        saveFileCorruptedPopUp.SetActive(true);
        ResetTitleScreenInteractability();
    }

    public void HideSaveFileCorruptedPopUp()
    {
        saveFileCorruptedPopUp.SetActive(false);
        ResetTitleScreenInteractability();
    }

    #endregion


    #region Settings

    public void ShowSettingsScreen()
    {
        settingsScreen.SetActive(true);
        SavePreviousSelectedButton();
        SetSelectedButton(selectedButtonSettingsScreen);
        if (titleScreen.activeInHierarchy) SetScreenInteractability(titleScreen, false);
        if (pausePanel.activeInHierarchy) SetScreenInteractability(pausePanel, false);
        SettingsManager.instance.ChangeTab(0);

        if (logsVerboseLevel == VerboseLevel.MAXIMAL)
        {
            Debug.Log("UI - Display Settings screen");
        }
    }

    public void HideSettingsScreen()
    {
        settingsScreen.SetActive(false);
        SetPreviousSelectedButton();
        if (titleScreen.activeInHierarchy) SetScreenInteractability(titleScreen, true);
        if (pausePanel.activeInHierarchy) SetScreenInteractability(pausePanel, true);

        if (logsVerboseLevel == VerboseLevel.MAXIMAL)
        {
            Debug.Log("UI - Hide Settings screen");
        }
    }

    public bool IsSettingsScreenVisible()
    {
        return settingsScreen.activeInHierarchy;
    }

    #endregion Settings

    #region Buttons

    public void SetSelectedButton(Button button)
    {
        SetSelectedButton(button.gameObject);
    }

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
        SetSelectedButton(buttonGO: null);
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

    public void ToggleVersionNumberVisible()
    {
        versionNumberText.enabled = !versionNumberText.enabled;
    }

    #region Links to outside of the game

    public void OpenSteamPage()
    {
        if (useSteamOverlay && SteamManager.Initialized)
        {
            Steamworks.AppId_t appId = new Steamworks.AppId_t(2315020);
            Steamworks.SteamFriends.ActivateGameOverlayToStore(appId, Steamworks.EOverlayToStoreFlag.k_EOverlayToStoreFlag_None);
        }
        else
        {
            Application.OpenURL("https://store.steampowered.com/app/2315020/Froguelike/");
        }
    }

    public void OpenDiscordInvitation()
    {
        Application.OpenURL("https://discord.gg/5vMdA97TWp"); // Invite to Froguelike discord server
    }


    public void OpenSaveFolder()
    {
        Application.OpenURL($"file://{SaveDataManager.instance.GetSaveFolderPath()}");

        // The following line works only from Unity Editor:
        // EditorUtility.RevealInFinder(SaveDataManager.instance.GetSaveFilePath());
    }

    #endregion

    // Check which screens are active on Title screen and reset the interactability properly
    public void ResetTitleScreenInteractability()
    {
        SetScreenInteractability(titleScreen, true);
        if (EADisclaimerScreen.gameObject.activeInHierarchy)
        {
            // Disclaimer is on top, it is interactable
            SetScreenInteractability(menuButtonsGroup, false);
            SetScreenInteractability(saveFileCorruptedPopUp, false);
            SetScreenInteractability(EADisclaimerScreen.gameObject, true);
        }
        else if (DemoDisclaimerScreen.gameObject.activeInHierarchy)
        {
            // Disclaimer is on top, it is interactable
            SetScreenInteractability(menuButtonsGroup, false);
            SetScreenInteractability(saveFileCorruptedPopUp, false);
            SetScreenInteractability(DemoDisclaimerScreen.gameObject, true);
        }
        else if (saveFileCorruptedPopUp.activeInHierarchy)
        {
            // Loading error pop up is on top, it is interactable
            SetScreenInteractability(menuButtonsGroup, false);
            SetScreenInteractability(saveFileCorruptedPopUp, true);
            SetSelectedButton(saveFileCorruptedPopUpDefaultSelectedButton);
        }
        else if (titleScreen.activeInHierarchy)
        {
            // No disclaimer and no pop up, title screen is interactable
            SetScreenInteractability(menuButtonsGroup, true);
            SetSelectedButton(startButton);
        }
    }

    #region Showcase

    public void ShowWarningTimerBeforeRestarting(float remainingTime)
    {
        showcaseWarningPanel.SetActive(true);
        showcaseWarningText.text = showcaseWarningString.Replace("X", remainingTime.ToString("0"));
    }

    public void HideWarningTimerBeforeRestarting()
    {
        showcaseWarningPanel.SetActive(false);
    }

    public void ShowShowcaseCTAPanel()
    {
        ShowcaseCTAOverlayPanel.SetActive(true);
        Color transparentColor = new Color(0, 0, 0, 0);
        Color blackOutlineColor = new Color(0.09411f, 0.09804f, 0.12157f, 1);
        foreach (Transform child in ShowcaseCTAOverlayPanel.transform)
        {
            child.GetComponent<TextMeshProUGUI>().fontMaterial.EnableKeyword("UNDERLAY_ON");
            child.GetComponent<TextMeshProUGUI>().fontMaterial.SetFloat(ShaderUtilities.ID_UnderlayDilate, 0.5f);
            child.GetComponent<TextMeshProUGUI>().fontMaterial.SetColor(ShaderUtilities.ID_UnderlayColor, blackOutlineColor);
        }
        inGameUIPanel.SetActive(false);
    }

    public void HideShowcaseCTAPanel()
    {
        ShowcaseCTAOverlayPanel.SetActive(false);
        inGameUIPanel.SetActive(true);
    }

    #endregion
}
