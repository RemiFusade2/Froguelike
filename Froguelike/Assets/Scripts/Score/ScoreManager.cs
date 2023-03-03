using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The ScoreManager class deals with the score screen and display all relevant information about a Run.
/// These information are provided by the RunManager.
/// </summary>
public class ScoreManager : MonoBehaviour
{
    // Singleton
    public static ScoreManager instance;

    [Header("Morals")]
    public List<string> possibleMorals;

    [Header("UI")]
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
    
    [Header("Settings")]
    public VerboseLevel logsVerboseLevel = VerboseLevel.NONE;

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


    private string GetRandomMoral()
    {
        return possibleMorals[Random.Range(0, possibleMorals.Count)];
    }

    /// <summary>
    /// Display the score screen with all information about a Run (provided as parameters)
    /// </summary>
    /// <param name="chaptersPlayed"></param>
    /// <param name="playedCharacter"></param>
    /// <param name="ownedItems"></param>
    public void ShowScores(List<Chapter> chaptersPlayed, int[] chapterKillCounts, PlayableCharacter playedCharacter, List<RunItemInfo> ownedItems, List<string> unlockedCharacters)
    {
        string scoreLog = "";

        // Hide all chapters texts
        foreach (TextMeshProUGUI chapterTextParent in chaptersTextList)
        {
            chapterTextParent.gameObject.SetActive(false);
        }
        // Display every played chapter and its kill count
        int totalScore = 0;
        for (int i = 0; i < chaptersPlayed.Count; i++)
        {
            if (i < chaptersTextList.Count && i < chaptersScoreTextList.Count)
            {
                Chapter chapter = chaptersPlayed[i];
                int enemiesKilledCount = chapterKillCounts[i];
                chaptersTextList[i].gameObject.SetActive(true);
                chaptersTextList[i].text = "Chapter " + (i+1) + "\n\t" + chapter.chapterData.chapterTitle;
                chaptersScoreTextList[i].text = enemiesKilledCount.ToString();
                totalScore += enemiesKilledCount;

                scoreLog += $"Chapter {i} - {chapter.chapterID} - Kills: {enemiesKilledCount}\n";
            }
        }
        // Display total kill count
        scoreLog += $"-> Total score: {totalScore}\n";
        totalScoreText.text = totalScore.ToString();

        // Pick a random moral to display
        string moral = GetRandomMoral();
        moralText.text = moral;
        scoreLog += $"-> Moral is: {moral}\n";

        // Display all weapons used during this run and their levels
        string allItemsNames = "";
        string allItemsLevels = "";
        scoreLog += $"Weapons:\n";
        foreach (RunItemInfo itemInfo in ownedItems)
        {
            if (itemInfo is RunWeaponInfo)
            {
                RunWeaponInfo weaponInfo = (itemInfo as RunWeaponInfo);
                allItemsNames += weaponInfo.weaponItemData.itemName + "\n";
                allItemsLevels += "LVL " + weaponInfo.level + "\n";
                scoreLog += $"-> {weaponInfo.weaponItemData.itemName} Lvl {weaponInfo.level} - ate a total of {weaponInfo.killCount.ToString("0.00")} bugs\n";
            }
        }
        allItemsNames += "\n";
        allItemsLevels += "\n";

        // Display all items used during this run and their levels
        scoreLog += $"Stat items:\n";
        foreach (RunItemInfo itemInfo in ownedItems)
        {
            if (itemInfo is RunStatItemInfo)
            {
                RunStatItemInfo statItemInfo = (itemInfo as RunStatItemInfo);
                allItemsNames += statItemInfo.itemData.itemName + "\n";
                allItemsLevels += "LVL " + statItemInfo.level + "\n";
                scoreLog += $"-> {statItemInfo.itemData.itemName} Lvl {statItemInfo.level}\n";
            }
        }
        allItemsNames += "\n";
        allItemsLevels += "\n";

        // Display all consumables used during this run
        // TODO? : Display the consumables items that were taken during this Run
        /*
        foreach (RunItemInfo itemInfo in itemsInfoList)
        {
            if (!itemInfo.item.isWeapon && itemInfo.item.levels.Count == 1)
            {
                allItemsNames += itemInfo.item.itemName + "\n";
                allItemsLevels += "x" + itemInfo.level + "\n";
            }
        }*/

        upgradesText.text = allItemsNames;
        upgradesLevelsText.text = allItemsLevels;

        // Display unlocked characters (achievements)
        unlockPanel.SetActive(false);
        if (unlockedCharacters.Count > 0)
        {
            unlockPanel.SetActive(true);
            CharacterData firstUnlockedCharacter = CharacterManager.instance.GetCharacterData(unlockedCharacters[0]);
            unlockedCharacterName.text = firstUnlockedCharacter.characterName;
            unlockedCharacterImage.sprite = firstUnlockedCharacter.characterSprite;
        }

        // Show the score screen
        UIManager.instance.ShowScoreScreen();

        if (logsVerboseLevel == VerboseLevel.MAXIMAL)
        {
            Debug.Log("Score screen - " + scoreLog);
        }
    }
}
