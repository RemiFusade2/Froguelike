using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Froguelike_UIManager : MonoBehaviour
{
    public static Froguelike_UIManager instance;

    [Header("Title")]
    public GameObject titleScreen;

    [Header("Character Selection")]
    public GameObject characterSelectionScreen;
    public List<Button> charactersButtonsList;
    public List<Image> charactersImagesList;
    public List<Text> charactersNamesTextList;
    public List<Text> charactersDescriptionTextList;

    [Header("In game UI")]
    public GameObject inGameUIPanel;
    public Slider xpSlider;
    public Text levelText;
    [Space]
    public string timerPrefix;
    public Text timerText;
    [Space]
    public string killCountPrefix;
    public Text killCountText;
    [Space]
    public string extraLivesPrefix;
    public Text extraLivesCountText;

    [Header("Chapter selection")]
    public GameObject chapterSelectionScreen;
    public Text chapterSelectionTopText;
    public List<Text> chapterTitleTextsList;
    public List<Text> chapterDescriptionTextsList;

    [Header("Chapter Start")]
    public GameObject chapterStartScreen;
    public Text chapterStartTopText;
    public Text chapterStartBottomText;

    [Header("Game over Screen")]
    public GameObject gameOverPanel;
    public GameObject gameOverRespawnButton;
    public GameObject gameOverGiveUpButton;

    [Header("Score Screen")]
    public GameObject scoreScreen;
    public List<Text> chaptersTextList;
    public List<Text> chaptersScoreTextList;
    public Text totalScoreText;
    public Text upgradesText;
    public Text upgradesLevelsText;

    [Header("Level UP Panel")]
    public GameObject levelUpPanel;
    public Animator levelUpPanelAnimator;
    [Space]
    public List<GameObject> levelUpChoicesPanels;
    public List<Text> levelUpChoicesTitles;
    public List<Text> levelUpChoicesLevels;
    public List<Text> levelUpChoicesDescriptions;
    [Space]
    public Color defaultUIColor;
    public Color newItemColor;
    public Color maxLevelColor;

    [Header("Sound")]
    public Froguelike_SoundManager soundManager;
    public Froguelike_MusicManager musicManager;

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
        titleScreen.SetActive(false);
        characterSelectionScreen.SetActive(false);
        chapterSelectionScreen.SetActive(false);
        chapterStartScreen.SetActive(false);
        scoreScreen.SetActive(false);
        //levelUpPanel.SetActive(false);
        inGameUIPanel.SetActive(false);
        gameOverPanel.SetActive(false);
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
        titleScreen.SetActive(true);
    }

    public void ShowCharacterSelection(List<Froguelike_PlayableCharacterInfo> playableCharactersList)
    {
        HideAllScreens();

        for (int i = 0; i < playableCharactersList.Count; i++)
        {
            if (i < charactersButtonsList.Count)
            {
                Froguelike_PlayableCharacterInfo characterInfo = playableCharactersList[i];
                charactersButtonsList[i].interactable = characterInfo.unlocked;
                charactersNamesTextList[i].text = (characterInfo.unlocked ? characterInfo.characterName : "???");
                string description = (characterInfo.unlocked ? characterInfo.characterDescription : characterInfo.unlockHint);
                description = description.Replace("\\n", "\n");
                charactersDescriptionTextList[i].text = description;
                charactersImagesList[i].enabled = characterInfo.unlocked;
            }
        }

        titleScreen.SetActive(true);
        characterSelectionScreen.SetActive(true);
    }

    public void ShowChapterSelection(int chapterCount, List<Froguelike_ChapterData> chapters)
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
            Froguelike_ChapterData chapter = chapters[i];
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

    public void ShowScoreScreen(List<Froguelike_ChapterInfo> chaptersInfoList, List<Froguelike_ItemInfo> itemsInfoList)
    {
        HideAllScreens();

        // Hide all chapters texts
        foreach (Text chapterTextParent in chaptersTextList)
        {
            chapterTextParent.gameObject.SetActive(false);
        }

        // Display the relevant ones
        int totalScore = 0;
        for (int i = 0; i < chaptersInfoList.Count; i++)
        {
            if (i < chaptersTextList.Count && i < chaptersScoreTextList.Count)
            {
                Froguelike_ChapterInfo chapterInfo = chaptersInfoList[i];
                chaptersTextList[i].gameObject.SetActive(true);
                chaptersTextList[i].text = "Chapter " + chapterInfo.chapterCount + "\n\t" + chapterInfo.chapterData.chapterTitle;
                chaptersScoreTextList[i].text = chapterInfo.enemiesKilledCount.ToString();
                totalScore += chapterInfo.enemiesKilledCount;
            }
        }
        totalScoreText.text = totalScore.ToString();

        // Display all items and their level
        string allItemsNames = "";
        string allItemsLevels = "";
        foreach (Froguelike_ItemInfo itemInfo in itemsInfoList)
        {
            if (itemInfo.item.isWeapon)
            {
                allItemsNames += itemInfo.item.itemName + "\n";
                allItemsLevels += "LVL " + itemInfo.level + "\n";
            }
        }
        allItemsNames += "\n";
        allItemsLevels += "\n";
        foreach (Froguelike_ItemInfo itemInfo in itemsInfoList)
        {
            if (!itemInfo.item.isWeapon && itemInfo.item.levels.Count > 1)
            {
                allItemsNames += itemInfo.item.itemName + "\n";
                allItemsLevels += "LVL " + itemInfo.level + "\n";
            }
        }
        allItemsNames += "\n";
        allItemsLevels += "\n";
        foreach (Froguelike_ItemInfo itemInfo in itemsInfoList)
        {
            if (!itemInfo.item.isWeapon && itemInfo.item.levels.Count == 1)
            {
                allItemsNames += itemInfo.item.itemName + "\n";
                allItemsLevels += "x" + itemInfo.level + "\n";
            }
        }
        upgradesText.text = allItemsNames;
        upgradesLevelsText.text = allItemsLevels;

        scoreScreen.SetActive(true);
        PlayLongPageSound();
    }

    public void ShowGameUI()
    {
        HideAllScreens();
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
        //Debug.Log("Call UpdateXPSlider(" + xp + " ," + maxXp);

        xpSlider.maxValue = maxXp;
        xpSlider.value = xp;

        //Debug.Log("After UpdateXPSlider. xpSlider.maxValue = " + xpSlider.maxValue + " , xpSlider.value = " + xpSlider.value);
    }

    public void UpdateLevel(int level)
    {
        levelText.text = "LVL " + level.ToString();
    }

    public void HideLevelUpItemSelection()
    {
        levelUpPanelAnimator.SetBool("Visible", false);
        //levelUpPanel.SetActive(false);
        PlaySlideBookSound();
    }

    public void ShowLevelUpItemSelection(List<Froguelike_ItemScriptableObject> possibleItems, List<int> itemLevels)
    {
        PlaySlideBookSound();
        levelUpPanel.SetActive(true);
        levelUpPanelAnimator.SetBool("Visible", true);
        foreach (GameObject panel in levelUpChoicesPanels)
        {
            panel.SetActive(false);
        }

        int index = 0;
        foreach (Froguelike_ItemScriptableObject item in possibleItems)
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

    public void PlayLongPageSound()
    {
        soundManager.PlayLongPageSound();
    }

    public void PlayDeathSound()
    {
        soundManager.PlayDeathSound();
    }

    public void PlayShortPageSound()
    {
        soundManager.PlayShortPageSound();
    }

    public void PlaySlideBookSound()
    {
        soundManager.PlaySlideBookSound();
    }
}
