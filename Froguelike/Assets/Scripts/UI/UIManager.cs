using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// UIManager deals with navigation in the menus, as well as in-game UI.
/// </summary>
public class UIManager : MonoBehaviour
{
    public static UIManager instance;

    #region Title Screen / Start Game

    [Header("Title")]
    public GameObject titleScreen;
    public Text titleScreenCurrencyText;

    [Header("Shop")]
    public GameObject shopScreen;

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
        ShowTitleScreen();
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
    }

    public void ShowTitleScreen()
    {
        MusicManager.instance.PlayTitleMusic();
        HideAllScreens();
        UpdateTitleScreenCurrencyText(GameManager.instance.gameData.availableCurrency);
        titleScreen.SetActive(true);
    }

    public void UpdateCurrencyDisplay()
    {
        long currencyValue = GameManager.instance.gameData.availableCurrency;

        UpdateTitleScreenCurrencyText(currencyValue);
        ShopManager.instance.DisplayShop();
    }

    private void UpdateTitleScreenCurrencyText(long currencyValue)
    {
        titleScreenCurrencyText.text = Tools.FormatCurrency(currencyValue, DataManager.instance.currencySymbol);
    }

    public void ShowCharacterSelectionScreen()
    {
        HideAllScreens();

        CharacterManager.instance.UpdateCharacterSelectionScreen();

        titleScreen.SetActive(true);
        characterSelectionScreen.SetActive(true);
    }

    public void ShowChapterSelectionScreen(bool forceTitleScreen = false)
    {
        HideAllScreens();
        if (forceTitleScreen)
        {
            titleScreen.SetActive(true);
        }
        chapterSelectionScreen.SetActive(true);
    }

    public void ShowChapterStart()
    {
        HideAllScreens();
        chapterStartScreen.SetActive(true);
        SoundManager.instance.PlayLongPageSound();
    }

    public void ShowScoreScreen()
    {
        HideAllScreens();
        inGameUIPanel.SetActive(true);
        scoreScreen.SetActive(true);
        SoundManager.instance.PlayLongPageSound();
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
    }

    public void ShowPauseScreen()
    {
        MusicManager.instance.PauseMusic();
        pausePanel.SetActive(true);
        pausePanelAnimator.SetBool("Visible", true);
    }

    public void HidePauseScreen()
    {
        MusicManager.instance.UnpauseMusic();
        pausePanelAnimator.SetBool("Visible", false);
    }

    public void ShowShop()
    {
        HideAllScreens();
        ShopManager.instance.DisplayShop();
        titleScreen.SetActive(true);
        shopScreen.SetActive(true);
    }

    #region Confirmation Panels

    public void ShowBackToTitleScreenConfirmationPanel(bool active)
    {
        backToTitleScreenConfirmationPanel.SetActive(active);
    }

    public void ShowClearSaveFileConfirmationPanel(bool active)
    {
        clearSaveFileConfirmationPanel.SetActive(active);
    }
    #endregion

    public void ShowSettingsScreen()
    {
        settingsScreen.SetActive(true);
    }

    public void HideSettingsScreen()
    {
        settingsScreen.SetActive(false);
    }

}
