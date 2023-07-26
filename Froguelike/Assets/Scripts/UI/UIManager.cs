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

    #endregion

    #region End Game

    [Header("Game over Screen")]
    public GameObject gameOverPanel;
    public GameObject gameOverRespawnButton;
    public GameObject gameOverGiveUpButton;
    public GameObject selectedButtonGameOverPanel;

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
        titleScreen.GetComponentInChildren<CanvasGroup>().interactable = true;

        // Set a selected button.
        SetSelectedButton(selectedButtonTitleScreen);

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
        titleScreen.GetComponentInChildren<CanvasGroup>().interactable = !isThereCharacterSelection;
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
            titleScreen.GetComponentInChildren<CanvasGroup>().interactable = false;
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
            Debug.Log("UI - Display Go back to Title confirmation screen");
        }
    }

    public void ShowClearSaveFileConfirmationPanel(bool active)
    {
        clearSaveFileConfirmationPanel.SetActive(active);

        if (logsVerboseLevel == VerboseLevel.MAXIMAL)
        {
            Debug.Log("UI - Display Clear save file confirmation screen");
        }
    }
    #endregion

    public void ShowSettingsScreen()
    {
        HideAllScreens();
        titleScreen.SetActive(true);
        settingsScreen.SetActive(true);
        SetSelectedButton(selectedButtonSettingsScreen);

        if (logsVerboseLevel == VerboseLevel.MAXIMAL)
        {
            Debug.Log("UI - Display Settings screen");
        }
    }

    public void HideSettingsScreen()
    {
        ClearSelectedButton();
        settingsScreen.SetActive(false);
        // TODO this only applies when going to the setting screen from title screen, it will be wrong when going to the settings from the pause screen
        SetSelectedButton(settingsButton);

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

    private void ClearSelectedButton()
    {
        SetSelectedButton(null);
    }

    #endregion Buttons
}
