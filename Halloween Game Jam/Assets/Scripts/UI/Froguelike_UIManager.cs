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
    public List<Text> charactersNamesTextList;
    public List<string> charactersNamesList;

    [Header("In game UI")]
    public GameObject inGameUIPanel;
    public Slider xpSlider;
    public Text levelText;

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

    public void ShowTitleScreen()
    {
        HideAllScreens();
        titleScreen.SetActive(true);
    }

    public void ShowCharacterSelection(List<bool> unlockedCharactersList)
    {
        HideAllScreens();

        for (int i = 0; i < unlockedCharactersList.Count; i++)
        {
            charactersButtonsList[i].interactable = unlockedCharactersList[i];
            charactersNamesTextList[i].text = (unlockedCharactersList[i] ? charactersNamesList[i] : "???");
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
    }

    public void ShowScoreScreen()
    {
        HideAllScreens();
        scoreScreen.SetActive(true);
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
    }

    public void ShowLevelUpItemSelection(List<Froguelike_ItemScriptableObject> possibleItems, List<int> itemLevels)
    {
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
            if (itemLevels.Count == 1)
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
                else if (level == item.levels.Count - 1)
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
                if ((level-1) < item.levels.Count)
                {
                    description = item.levels[level - 1].description;
                }
                levelUpChoicesDescriptions[index].text = description;
            }
            index++;
        }
    }
}
