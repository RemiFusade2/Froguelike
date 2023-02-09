using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UIManager deals with navigation in the menus, as well as in-game UI.
/// </summary>
public class UIManager : MonoBehaviour
{
    public static UIManager instance;

    #region Title Screen / Start Game

    [Header("Title")]
    public GameObject titleScreen;
    public TextMeshProUGUI titleScreenCurrencyText;

    [Header("Shop")]
    public GameObject shopScreen;

    [Header("Character Selection")]
    public GameObject characterSelectionScreen;

    #endregion

    #region In Game

    [Header("Chapter selection")]
    public GameObject chapterSelectionScreen;
    public TextMeshProUGUI chapterSelectionTopText;
    public List<TextMeshProUGUI> chapterTitleTextsList;
    public List<TextMeshProUGUI> chapterDescriptionTextsList;

    [Header("Chapter Start")]
    public GameObject chapterStartScreen;
    public TextMeshProUGUI chapterStartTopText;
    public TextMeshProUGUI chapterStartBottomText;

    [Header("In game UI")]
    public GameObject inGameUIPanel;
    public Slider xpSlider;
    public TextMeshProUGUI levelText;
    [Space]
    public TextMeshProUGUI currencyText;
    [Space]
    public string timerPrefix;
    public TextMeshProUGUI timerText;
    [Space]
    public string killCountPrefix;
    public TextMeshProUGUI killCountText;
    [Space]
    public string extraLivesPrefix;
    public TextMeshProUGUI extraLivesCountText;

    [Header("Level UP Panel")]
    public GameObject levelUpPanel;
    public Animator levelUpPanelAnimator;
    [Space]
    public List<GameObject> levelUpChoicesPanels;
    public List<TextMeshProUGUI> levelUpChoicesTitles;
    public List<TextMeshProUGUI> levelUpChoicesLevels;
    public List<TextMeshProUGUI> levelUpChoicesDescriptions;
    [Space]
    public Color defaultUIColor;
    public Color newItemColor;
    public Color maxLevelColor;

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
    public List<TextMeshProUGUI> chaptersTextList;
    public List<TextMeshProUGUI> chaptersScoreTextList;
    public TextMeshProUGUI totalScoreText;
    public TextMeshProUGUI moralText;
    [Space]
    public TextMeshProUGUI upgradesText;
    public TextMeshProUGUI upgradesLevelsText;
    public GameObject unlockPanel;
    public TextMeshProUGUI unlockedCharacterName;
    public Image unlockedCharacterImage;

    #endregion

    [Header("Confirmation panels")]
    public GameObject backToTitleScreenConfirmationPanel;
    public GameObject clearSaveFileConfirmationPanel;

    [Header("Sound")]
    public SoundManager soundManager;
    public MusicManager musicManager;

    [Header("Currency symbol")]
    public string currencySymbol = "₣";

    [Header("Settings Screen")]
    public GameObject settingsScreen;

    private void Awake()
    {
        instance = this;
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
        settingsScreen.SetActive(false);
    }

    public void SetTimer(float remainingTime)
    {
        System.TimeSpan time = new System.TimeSpan(0, 0, Mathf.RoundToInt(remainingTime));
        timerText.text = timerPrefix + time.ToString("m\\:ss");
    }

    public void SetEatenCount(int eatenBugs)
    {
        killCountText.text = killCountPrefix + eatenBugs.ToString();
    }

    public void SetExtraLives(int reviveCount)
    {
        extraLivesCountText.text = extraLivesPrefix + reviveCount.ToString();
    }

    public void ShowTitleScreen()
    {
        musicManager.PlayTitleMusic();
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

    public void UpdateInGameCurrencyText(long currencyValue)
    {
        currencyText.text = Tools.FormatCurrency(currencyValue, currencySymbol);
    }

    private void UpdateTitleScreenCurrencyText(long currencyValue)
    {
        titleScreenCurrencyText.text = Tools.FormatCurrency(currencyValue, currencySymbol);
    }

    public void ShowCharacterSelectionScreen()
    {
        HideAllScreens();

        CharacterManager.instance.UpdateCharacterSelectionScreen();

        titleScreen.SetActive(true);
        characterSelectionScreen.SetActive(true);
    }

    public void ShowChapterSelection(int chapterCount, List<ChapterData> chapters)
    {
        HideAllScreens();
        string chapterIntro = "";
        if (chapterCount == 1)
        {
            titleScreen.SetActive(true);
            chapterIntro = "How does the story start?";
        }
        else if (chapterCount == 5)
        {
            chapterIntro = "How does that story end?";
        }
        else
        {
            chapterIntro = "What happens in chapter " + chapterCount.ToString() + "?";
        }
        chapterSelectionTopText.text = chapterIntro;
        for (int i = 0; i < chapters.Count; i++)
        {
            ChapterData chapter = chapters[i];
            chapterTitleTextsList[i].text = chapter.chapterTitle;
            chapterDescriptionTextsList[i].text = chapter.chapterDescription;
        }
        chapterSelectionScreen.SetActive(true);
    }

    public void ShowChapterStart(int chapterCount, string chapterTitle)
    {
        HideAllScreens();
        chapterStartScreen.SetActive(true);
        chapterStartTopText.text = "Chapter " + chapterCount.ToString();
        chapterStartBottomText.text = chapterTitle;
        PlayLongPageSound();
    }

    public void ShowScoreScreen(List<ChapterInfo> chaptersInfoList, string moral, List<ItemInfo> itemsInfoList)
    {
        HideAllScreens();

        // Hide all chapters texts
        foreach (TextMeshProUGUI chapterTextParent in chaptersTextList)
        {
            chapterTextParent.gameObject.SetActive(false);
        }

        // Display the relevant ones
        int totalScore = 0;
        for (int i = 0; i < chaptersInfoList.Count; i++)
        {
            if (i < chaptersTextList.Count && i < chaptersScoreTextList.Count)
            {
                ChapterInfo chapterInfo = chaptersInfoList[i];
                chaptersTextList[i].gameObject.SetActive(true);
                chaptersTextList[i].text = "Chapter " + chapterInfo.chapterCount + "\n\t" + chapterInfo.chapterData.chapterTitle;
                chaptersScoreTextList[i].text = chapterInfo.enemiesKilledCount.ToString();
                totalScore += chapterInfo.enemiesKilledCount;
            }
        }
        totalScoreText.text = totalScore.ToString();

        // Display random moral
        moralText.text = moral;

        // Display all items and their level
        string allItemsNames = "";
        string allItemsLevels = "";
        foreach (ItemInfo itemInfo in itemsInfoList)
        {
            if (itemInfo.item.isWeapon)
            {
                allItemsNames += itemInfo.item.itemName + "\n";
                allItemsLevels += "LVL " + itemInfo.level + "\n";
            }
        }
        allItemsNames += "\n";
        allItemsLevels += "\n";
        foreach (ItemInfo itemInfo in itemsInfoList)
        {
            if (!itemInfo.item.isWeapon && itemInfo.item.levels.Count > 1)
            {
                allItemsNames += itemInfo.item.itemName + "\n";
                allItemsLevels += "LVL " + itemInfo.level + "\n";
            }
        }
        allItemsNames += "\n";
        allItemsLevels += "\n";
        foreach (ItemInfo itemInfo in itemsInfoList)
        {
            if (!itemInfo.item.isWeapon && itemInfo.item.levels.Count == 1)
            {
                allItemsNames += itemInfo.item.itemName + "\n";
                allItemsLevels += "x" + itemInfo.level + "\n";
            }
        }
        upgradesText.text = allItemsNames;
        upgradesLevelsText.text = allItemsLevels;

        // Display unlocked character info
        unlockPanel.SetActive(false);

        // Display score screen
        inGameUIPanel.SetActive(true);
        scoreScreen.SetActive(true);
        PlayLongPageSound();
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
        PlayDeathSound();
    }

    public void UpdateXPSlider(float xp, float maxXp)
    {
        xpSlider.maxValue = maxXp;
        xpSlider.value = xp;
    }

    public void UpdateLevel(int level)
    {
        levelText.text = "LVL " + level.ToString();
    }

    public void HideLevelUpItemSelection()
    {
        levelUpPanelAnimator.SetBool("Visible", false);
        PlaySlideBookSound();
    }

    public void ShowLevelUpItemSelection(List<ItemScriptableObject> possibleItems, List<int> itemLevels)
    {
        EventSystem.current.SetSelectedGameObject(null);
        PlaySlideBookSound();
        levelUpPanel.SetActive(true);
        levelUpPanelAnimator.SetBool("Visible", true);
        foreach (GameObject panel in levelUpChoicesPanels)
        {
            panel.SetActive(false);
        }

        int index = 0;
        foreach (ItemScriptableObject item in possibleItems)
        {
            levelUpChoicesPanels[index].SetActive(true);
            levelUpChoicesTitles[index].text = item.itemName;
            if (item.levels.Count == 1)
            {
                // item without levels
                levelUpChoicesLevels[index].color = defaultUIColor;
                levelUpChoicesLevels[index].text = "";
                levelUpChoicesDescriptions[index].text = item.levels[0].description;
            }
            else
            {
                int level = itemLevels[index];
                if (level == 1)
                {
                    // new item!
                    levelUpChoicesLevels[index].color = newItemColor;
                    levelUpChoicesLevels[index].text = "New!";
                }
                else if (level >= item.levels.Count)
                {
                    // max level
                    levelUpChoicesLevels[index].color = maxLevelColor;
                    levelUpChoicesLevels[index].text = "LVL MAX";
                }
                else
                {
                    levelUpChoicesLevels[index].color = defaultUIColor;
                    levelUpChoicesLevels[index].text = "LVL " + level.ToString();
                }
                string description = "Better I guess...";
                if ((level - 1) < item.levels.Count)
                {
                    description = item.levels[level - 1].description;
                }
                levelUpChoicesDescriptions[index].text = description;
            }
            index++;
        }
    }

    #region Audio
    // Audio

    // Used for starting a chapter and end screen.
    public void PlayLongPageSound()
    {
        soundManager.PlayLongPageSound();
    }

    // Ripping page.
    public void PlayDeathSound()
    {
        soundManager.PlayDeathSound();
    }

    public void PlayShortPageSound()
    {
        soundManager.PlayShortPageSound();
    }

    // When showing level up book.
    public void PlaySlideBookSound()
    {
        soundManager.PlaySlideBookSound();
    }

    #endregion Audio

    public void ShowPauseScreen()
    {
        musicManager.PauseMusic();
        pausePanel.SetActive(true);
        pausePanelAnimator.SetBool("Visible", true);
    }

    public void HidePauseScreen()
    {
        musicManager.UnpauseMusic();
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
