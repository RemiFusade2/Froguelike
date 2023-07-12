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
    public TextMeshProUGUI titleScreenCurrencyText;
    public TextMeshProUGUI titleScreenWelcomeMessageText;
    public TextMeshProUGUI titleScreenSaveLocationText;
    [Space]
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

    public void ShowTitleScreen()
    {
        MusicManager.instance.PlayTitleMusic();
        HideAllScreens();
        UpdateTitleScreenCurrencyText(GameManager.instance.gameData.availableCurrency);

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
        // MusicManager.instance.PauseMusic();
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
