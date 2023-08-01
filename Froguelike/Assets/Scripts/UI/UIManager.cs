using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using Steamworks;

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
    public GameObject shopButton;
    public GameObject achievementsButton;
    public GameObject settingsButton;

    [Header("Shop")]
    public GameObject shopScreen;
    public GameObject selectedButtonShopScreen;

    [Header("Achievements")]
    public GameObject achievementsScreen;
    public GameObject selectedButtonAchievementsScreen;

    [Header("Character Selection")]
    public GameObject characterSelectionScreen;
    public GameObject characterSelectionGridLayoutGroup;

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

    [Header("Settings Screen")]
    public GameObject settingsScreen;
    public GameObject selectedButtonSettingsScreen;

    [Header("Demo panels")]
    public List<GameObject> demoPanelsList;

    private List<GameObject> rememberThisButton = new List<GameObject>();

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
        // Debug.Log("Selected button: " + EventSystem.current);
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
        SetScreenInteractability(menuButtonsGroup, !isThereCharacterSelection);
        characterSelectionScreen.SetActive(isThereCharacterSelection);

        // Pick the first character button.
        SetSelectedButton(characterSelectionGridLayoutGroup.GetComponentInChildren<Transform>().GetComponentInChildren<Button>().gameObject);

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
        SetScreenInteractability(scoreScreen, true);
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
    }

    public void ShowGameOver(int respawnsAvailable)
    {
        // HideAllScreens();
        // inGameUIPanel.SetActive(true);
        bool respawnAvailable = respawnsAvailable > 0;

        gameOverPanel.GetComponent<GameOverScreen>().UpdateGameOverScreen();
        gameOverPanel.SetActive(true);
        gameOverRespawnButton.GetComponent<Button>().interactable = respawnAvailable;
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
        HideAllScreens();
        SetScreenInteractability(menuButtonsGroup, false);
        ShopManager.instance.DisplayShop(true);
        titleScreen.SetActive(true);
        shopScreen.SetActive(true);
        selectedButtonShopScreen = ShopManager.instance.shopPanel.GetComponentInChildren<Button>().gameObject;
        SetSelectedButton(selectedButtonShopScreen);

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
            Debug.Log("UI - Display Go back to Title confirmation screen");
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
            Debug.Log("UI - Display Clear save file confirmation screen");
        }
    }
    #endregion Confirmation Panels

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

        string text = "Previous button " + EventSystem.current.currentSelectedGameObject?.name;
        // Set selected button.
        EventSystem.current.SetSelectedGameObject(buttonGO);

        string text2 = " current button " + EventSystem.current.currentSelectedGameObject?.name;

        Debug.Log(text + text2);

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
