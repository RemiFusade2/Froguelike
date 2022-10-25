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

    [Header("In game UI")]
    public GameObject inGameUIPanel;
    public Slider xpSlider;
    public Text levelText;

    [Header("Chapter selection")]
    public GameObject chapterSelectionScreen;
    public Text chapterSelectionTopText;
    public Text chapterSelectionChoice1TopText;
    public Text chapterSelectionChoice1BottomText;
    public Text chapterSelectionChoice2TopText;
    public Text chapterSelectionChoice2BottomText;

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
    [Space]
    public List<GameObject> levelUpChoicesPanels;
    public List<Text> levelUpChoicesTitles;
    public List<Text> levelUpChoicesLevels;
    public List<Text> levelUpChoicesDescriptions;
    

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
        levelUpPanel.SetActive(false);
        inGameUIPanel.SetActive(false);
        gameOverPanel.SetActive(false);
    }

    public void ShowTitleScreen()
    {
        HideAllScreens();
        titleScreen.SetActive(true);
    }

    public void ShowCharacterSelection()
    {
        HideAllScreens();
        titleScreen.SetActive(true);
        characterSelectionScreen.SetActive(true);
    }

    public void ShowChapterSelection(int chapterCount, string choice1Title, string choice2Title)
    {
        HideAllScreens();
        chapterSelectionTopText.text = (chapterCount == 1) ? "HOW DOES THE STORY START?" : "WHAT HAPPENED NEXT?";
        chapterSelectionChoice1TopText.text = "Chapter " + chapterCount.ToString();
        chapterSelectionChoice2TopText.text = "Chapter " + chapterCount.ToString();
        chapterSelectionChoice1BottomText.text = choice1Title;
        chapterSelectionChoice2BottomText.text = choice2Title;
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
        xpSlider.maxValue = maxXp;
        xpSlider.value = xp;
    }

    public void UpdateLevel(int level)
    {
        levelText.text = "LVL " + level.ToString();
    }

    public void HideLevelUpItemSelection()
    {
        levelUpPanel.SetActive(false);
    }

    public void ShowLevelUpItemSelection(List<Froguelike_ItemScriptableObject> possibleItems, List<int> itemLevels)
    {
        levelUpPanel.SetActive(true);
        foreach (GameObject panel in levelUpChoicesPanels)
        {
            panel.SetActive(false);
        }

        int index = 0;
        foreach (Froguelike_ItemScriptableObject item in possibleItems)
        {
            levelUpChoicesPanels[index].SetActive(true);
            levelUpChoicesTitles[index].text = item.itemName;
            levelUpChoicesLevels[index].text = "LVL " + itemLevels[index].ToString();
            if (itemLevels[index] == 1)
            {
                levelUpChoicesDescriptions[index].text = item.firstDescription;
            }
            else if (itemLevels[index] == item.maxLevel)
            {
                levelUpChoicesDescriptions[index].text = item.lastDescription;
            }
            else
            {
                levelUpChoicesDescriptions[index].text = item.defaultDescription;
            }
            index++;
        }
    }
}
