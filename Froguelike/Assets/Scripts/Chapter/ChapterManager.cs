using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Chapter describes a chapter in its current state.
/// It has a reference to ChapterData, the scriptable object that describes the chapter. This is not serialized with the rest.
/// It keeps the chapterTitle there for serialization. When saving/loading this chapter from a save file, the title will be used to retrieve the right chapter in the program.
/// The information that can change at runtime are:
/// - unlocked, is the status of the chapter. Is it available to play during a Run? This value can change when the chapter is unlocked through an achievement.
/// - currentLevel, is the number of times this item has been bought. Is reset to zero when reseting the shop.
/// - maxLevel, is the number of times this can be bought at max. Can be "upgraded" through an achievement.
/// </summary>
[System.Serializable]
public class Chapter
{
    [System.NonSerialized]
    public ChapterData chapterData;

    // Defined at runtime, using ShopItemData
    [HideInInspector]
    public string chapterTitle;

    public bool unlocked;

    [System.NonSerialized]
    public int enemiesKilledCount;
}

/*
/// <summary>
/// A structure to link a ChapterIconType with its corresponding Sprite
/// </summary>
[System.Serializable]
public class ChapterIcon
{
    public ChapterIconType iconType;
    public Sprite iconSprite;
}*/

public class ChapterManager : MonoBehaviour
{
    public static ChapterManager instance;

    /*
     * // Code for later when we implement icons
    [Header("Icons for chapters")]
    public List<ChapterIcon> availableChapterIcons;
    private Dictionary<ChapterIconType, Sprite> chapterIconsDictionary;*/

    [Header("All Chapters Data")]
    public List<ChapterData> allChaptersDataList;

    [Header("Final Chapter")]
    public ChapterData finalChapterData;

    [Header("UI - Chapter Selection")]
    public Text chapterSelectionTopText;
    public List<Text> chapterTitleTextsList;
    public List<Text> chapterDescriptionTextsList;

    [Header("UI - Chapter Start")]
    public Text chapterStartTopText;
    public Text chapterStartBottomText;

    
    [Header("Runtime")]
    public Chapter finalChapter;

    private List<ChapterData> selectionOfNextChaptersList;
    /*
    [Tooltip("Contains the current chapters that can be played. Will be reset at the start of a Run and updated during a Run.")]*/
    private List<Chapter> currentPlayableChaptersList; // This serves as the deck of chapters where we pick the next chapters from
    
    // This dictionary is a handy way to get the Chapter object from its title (unique ID)
    private Dictionary<string, Chapter> allChaptersDico;

    public bool chapterChoiceIsVisible;


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
        allChaptersDico = new Dictionary<string, Chapter>();
        foreach (ChapterData chapterData in allChaptersDataList)
        {
            Chapter chapter = new Chapter() { chapterData = chapterData, chapterTitle = chapterData.chapterTitle, unlocked = chapterData.startingUnlockState };
            allChaptersDico.Add(chapter.chapterTitle, chapter);
        }
        finalChapter = new Chapter() { chapterData = finalChapterData, chapterTitle = finalChapterData.chapterTitle, unlocked = finalChapterData.startingUnlockState };
    }

    private void SelectNextPossibleChapters(int chapterCount)
    {
        selectionOfNextChaptersList = new List<ChapterData>();

        while (selectionOfNextChaptersList.Count < chapterCount)
        {
            if (currentPlayableChaptersList.Count < 1)
            {
                ReinitializeChaptersList();
            }
            
            Chapter selectedChapter = currentPlayableChaptersList[Random.Range(0, currentPlayableChaptersList.Count)];
            bool hasFriend = selectedChapter.chapterData.addFriend != FriendType.NONE;

            FrogCharacterController player = GameManager.instance.player;

            if ((hasFriend && player.HasActiveFriend(0)) || selectionOfNextChaptersList.Contains(selectedChapter.chapterData))
            {
                currentPlayableChaptersList.Remove(selectedChapter);
                continue;
            }

            currentPlayableChaptersList.Remove(selectedChapter);
            selectionOfNextChaptersList.Add(selectedChapter.chapterData);
        }
    }

    public void ShowChapterSelection(List<Chapter> chaptersPlayed)
    {
        // Choose 3 chapters from the list of available chapters
        SelectNextPossibleChapters(3);

        // Update the UI to show these 3 chapters
        int chapterCount = chaptersPlayed.Count;
        string chapterIntro = "";
        if (chapterCount == 0)
        {
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
        for (int i = 0; i < selectionOfNextChaptersList.Count; i++)
        {
            ChapterData chapter = selectionOfNextChaptersList[i];
            chapterTitleTextsList[i].text = chapter.chapterTitle;
            chapterDescriptionTextsList[i].text = chapter.chapterLore[0];
        }

        // Call the UIManager to display the chapter selection screen
        UIManager.instance.ShowChapterSelectionScreen((chapterCount == 0));
    }

    public void ShowChapterStartScreen(int chapterCount, string chapterTitle)
    {
        UIManager.instance.ShowChapterStart();
        chapterStartTopText.text = "Chapter " + chapterCount.ToString();
        chapterStartBottomText.text = chapterTitle;
    }

    public void ReinitializeChaptersList()
    {
        currentPlayableChaptersList = new List<Chapter>();

        foreach (ChapterData chapterData in allChaptersDataList)
        {
            currentPlayableChaptersList.Add(new Chapter() { chapterData = chapterData, chapterTitle = chapterData.chapterTitle, enemiesKilledCount = 0, unlocked = chapterData.startingUnlockState });
        }
    }

    public void SelectChapter(int index)
    {
        Chapter chapterInfo = new Chapter();
        chapterInfo.chapterData = selectionOfNextChaptersList[index];
        chapterInfo.chapterTitle = chapterInfo.chapterData.chapterTitle;
        chapterInfo.enemiesKilledCount = 0;

        RunManager.instance.StartChapter(chapterInfo);
    }
}
