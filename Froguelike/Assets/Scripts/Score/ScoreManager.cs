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

    [Header("Settings")]
    public VerboseLevel logsVerboseLevel = VerboseLevel.NONE;

    [Header("Morals")]
    public List<string> possibleMorals;

    [Header("UI")]
    public List<GameObject> scoreLines;
    public List<TextMeshProUGUI> chaptersTextList;
    public List<TextMeshProUGUI> chaptersScoreTextList;
    public List<TextMeshProUGUI> chaptersTimeTextList;
    public TextMeshProUGUI totalScoreText;
    public TextMeshProUGUI totalTimeText;
    public TextMeshProUGUI currencyCollectedText;
    public TextMeshProUGUI moralText;
    [Space]
    public Transform tonguesPanel;
    public GameObject tonguesDisplayPrefab;
    public Transform runItemsPanel;
    public GameObject runItemsDisplayPrefab;
    [Space]
    public AchievementsScrollRect achievementScrollRect;
    public GameObject leftArrow;
    public GameObject rightArrow;
    [Space]
    public CharacterBookmarkInRunInfoBehaviour characterInfoBookmark;


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
    public void ShowScores(List<Chapter> chaptersPlayed, int[] chapterKillCounts, PlayableCharacter playedCharacter, List<RunItemInfo> ownedItems, List<Achievement> unlockedAchievements, int playedTimeLatestChapter, int currencyCollected)
    {
        string scoreLog = "";

        // Hide all chapters texts
        foreach (GameObject scoreLine in scoreLines)
        {
            scoreLine.SetActive(false);
        }
        // Display every played chapter and its kill count
        int totalScore = 0;
        int totalTime = 0;
        for (int i = 0; i < chaptersPlayed.Count; i++)
        {
            if (i < chaptersTextList.Count && i < chaptersScoreTextList.Count)
            {
                Chapter chapter = chaptersPlayed[i];
                int enemiesKilledCount = chapterKillCounts[i];
                int chapterTime = Mathf.FloorToInt(chapter.chapterData.chapterLengthInSeconds);
                scoreLines[i].SetActive(true);

                // Chapter number and name.
                chaptersTextList[i].text = "Chapter " + (i + 1) + "\n          " + chapter.chapterData.chapterTitle;

                // Flies eaten.
                chaptersScoreTextList[i].text = enemiesKilledCount.ToString();
                totalScore += enemiesKilledCount;

                // Chapter time.
                if (i == chaptersPlayed.Count - 1 && playedTimeLatestChapter != chapterTime)
                {
                    // For the last chapter if the run was not won.
                    int chapterMinutes = playedTimeLatestChapter / 60;
                    int chapterSeconds = playedTimeLatestChapter % 60;
                    chaptersTimeTextList[i].text = chapterMinutes.ToString("00") + ":" + chapterSeconds.ToString("00");
                    totalTime += playedTimeLatestChapter;
                }
                else
                {
                    int chapterMinutes = chapterTime / 60;
                    int chapterSeconds = chapterTime % 60;
                    chaptersTimeTextList[i].text = chapterMinutes.ToString("00") + ":" + chapterSeconds.ToString("00");
                    totalTime += chapterTime;
                }

                scoreLog += $"Chapter {i} - {chapter.chapterID} - Kills: {enemiesKilledCount}\n";
            }
        }
        // Display total kill count
        scoreLog += $"-> Total score: {totalScore}\n";
        totalScoreText.text = totalScore.ToString();

        // Display total time.
        int totalMinutes = totalTime / 60;
        int totalSeconds = totalTime % 60;
        totalTimeText.text = totalMinutes.ToString("00") + ":" + totalSeconds.ToString("00");

        // Display collected currency.
        currencyCollectedText.text = Tools.FormatCurrency(currencyCollected, DataManager.instance.currencySymbol);

        // Pick a random moral to display
        string moral = GetRandomMoral();
        moralText.text = moral;
        scoreLog += $"-> Moral is: {moral}\n";

        // Display all weapons used during this run and their levels

        // Remove previous tongue displays.
        foreach (Transform child in tonguesPanel)
        {
            Destroy(child.gameObject);
        }

        scoreLog += $"Weapons:\n";

        foreach (RunItemInfo itemInfo in ownedItems)
        {
            if (itemInfo is RunWeaponInfo)
            {
                RunWeaponInfo weaponInfo = (itemInfo as RunWeaponInfo);
                scoreLog += $"-> {weaponInfo.weaponItemData.itemName} Lvl {weaponInfo.level} - ate a total of {weaponInfo.killCount.ToString("0.00")} bugs\n";
            }
        }

        int tongueBackgroundIndex = 0;
        foreach (RunItemInfo itemInfo in ownedItems)
        {
            if (itemInfo is RunWeaponInfo)
            {
                GameObject runTongueInfoGo = Instantiate(tonguesDisplayPrefab, tonguesPanel);
                RunItemField runItemField = runTongueInfoGo.GetComponent<RunItemField>();
                RunWeaponInfo weaponInfo = (itemInfo as RunWeaponInfo);
                runItemField.Initialize(weaponInfo, tongueBackgroundIndex);
                tongueBackgroundIndex = tongueBackgroundIndex == 0 ? 1 : 0;
            }
        }

        // Display all items used during this run and their levels

        // Remove previous run item displays.
        foreach (Transform child in runItemsPanel)
        {
            Destroy(child.gameObject);
        }

        scoreLog += $"Stat items:\n";

        foreach (RunItemInfo itemInfo in ownedItems)
        {
            if (itemInfo is RunStatItemInfo)
            {
                RunStatItemInfo statItemInfo = (itemInfo as RunStatItemInfo);
                scoreLog += $"-> {statItemInfo.itemData.itemName} Lvl {statItemInfo.level}\n";
            }
        }

        // Add new run items to display.
        int runItemBackgroundIndex = 0;
        foreach (RunItemInfo itemInfo in ownedItems)
        {
            if (itemInfo is RunStatItemInfo)
            {
                GameObject runItemInfoGo = Instantiate(runItemsDisplayPrefab, runItemsPanel);
                RunItemField runItemField = runItemInfoGo.GetComponent<RunItemField>();
                RunStatItemInfo runItemInfo = (itemInfo as RunStatItemInfo);
                runItemField.Initialize(runItemInfo, runItemBackgroundIndex);
                runItemBackgroundIndex = runItemBackgroundIndex == 0 ? 1 : 0;
            }
        }

        // Display unlocked achievements
        achievementScrollRect.Initialize(unlockedAchievements);
        achievementScrollRect.transform.parent.gameObject.SetActive((unlockedAchievements.Count > 0));
        leftArrow.SetActive(unlockedAchievements.Count > 1);
        rightArrow.SetActive(unlockedAchievements.Count > 1);

        // Display character info.
        characterInfoBookmark.UpdateInRunBookmark();

        // Show the score screen
        UIManager.instance.ShowScoreScreen();

        // Add some froins in the credits screen, for funsies
        if (CreditsScreenBehaviour.instance != null)
        {
            float probabilityOfFroinsInCredits = (totalScore / 100000.0f);
            CreditsScreenBehaviour.instance.RespawnCreditFroins(probabilityOfFroinsInCredits);
        }

        if (logsVerboseLevel == VerboseLevel.MAXIMAL)
        {
            Debug.Log("Score screen - " + scoreLog);
        }
    }
}
