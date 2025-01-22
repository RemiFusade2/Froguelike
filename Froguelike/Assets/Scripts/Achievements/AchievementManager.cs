using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// Achievement describes an achievement in its current state.
/// 
/// It has a reference to ChapterData, the scriptable object that describes the chapter. This is not serialized with the rest.
/// It keeps the chapterID there for serialization. When saving/loading this chapter from a save file, the ID will be used to retrieve the right chapter in the program.
/// The information that can change at runtime are:
/// - unlocked, is the status of the chapter. This value can change when the chapter is unlocked through an achievement.
/// - attemptCountByCharacters is the amount of attempts from every character
/// - completionCountByCharacters is the amount of completions from every character
/// </summary>
[System.Serializable]
public class Achievement
{
    [System.NonSerialized]
    public AchievementData achievementData;

    // Defined at runtime, using AchievementData
    [HideInInspector]
    public string achievementID;

    public bool unlocked;

    public override bool Equals(object obj)
    {
        bool equal = false;
        if (obj is Achievement)
        {
            equal = this.achievementID.Equals((obj as Achievement).achievementID);
        }
        return equal;
    }

    public override int GetHashCode()
    {
        return achievementID.GetHashCode();
    }

    public string GetRewardDescription()
    {
        string rewardDescription = achievementData.reward.rewardDescription;
        switch (achievementData.reward.rewardType)
        {
            case AchievementRewardType.CHARACTER:
                rewardDescription = rewardDescription.Replace("characterName", achievementData.reward.character.characterName);
                break;
            case AchievementRewardType.RUN_ITEM:
                rewardDescription = rewardDescription.Replace("itemName", achievementData.reward.runItem.itemName);
                break;
            case AchievementRewardType.SHOP_ITEM:
                rewardDescription = rewardDescription.Replace("itemName", achievementData.reward.shopItem.itemName);
                break;
            case AchievementRewardType.CHAPTER:
                rewardDescription = rewardDescription.Replace("chapterTitle", achievementData.reward.chapter.chapterTitle);
                break;
            default:
                break;
        }
        return rewardDescription;
    }

    public string GetAchievementDescription()
    {
        string conditionDescription = achievementData.achievementDescription;

        foreach (AchievementCondition condition in achievementData.conditionsList)
        {
            switch (condition.conditionType)
            {
                case AchievementConditionType.CHAPTER:
                    conditionDescription = conditionDescription.Replace("chapterTitle", condition.playedChapter.chapterTitle);
                    break;
                case AchievementConditionType.CHARACTER:
                    conditionDescription = conditionDescription.Replace("characterName", condition.playedCharacter.characterName);
                    break;
                case AchievementConditionType.RUNITEM:
                case AchievementConditionType.RUNITEMLEVEL:
                    conditionDescription = conditionDescription.Replace("itemName", condition.runItem.itemName);
                    break;
                default:
                    break;
            }
        }

        return conditionDescription;
    }
}

/// <summary>
/// AchievementsSaveData contains all information that must be saved about the achievements.
/// - loadedAchievementsList is the list of achievements in their current state
/// </summary>
[System.Serializable]
public class AchievementsSaveData : SaveData
{
    public List<Achievement> achievementsList;

    public bool achievementsListUnlocked;

    public AchievementsSaveData()
    {
        Reset();
    }

    public override void Reset()
    {
        base.Reset();
        achievementsList = new List<Achievement>();
    }
}

public class AchievementManager : MonoBehaviour
{
    public static AchievementManager instance;

    [Header("Settings - logs")]
    public VerboseLevel verbose = VerboseLevel.NONE;

    [Header("Data")]
    public List<AchievementData> alwaysVisibleAchievementsScriptableObjectsList;
    public List<AchievementData> achievementsScriptableObjectsList;

    [Header("UI")]
    public TextMeshProUGUI achievementCountTextMesh;
    public ScrollRect achievementsListScrollRect;
    public RectTransform achievementScrollContentPanel;
    public Transform achievementScrollEntriesParent;
    public ScrollbarKeepCursorSizeBehaviour achievementsScrollbar;
    [Space]
    public GameObject achievementEntryPrefab;
    public GameObject achievementEntryWithCountPrefab;

    [Header("Runtime")]
    public AchievementsSaveData achievementsData; // Load from save file

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

    public bool IsAchievementsListUnlocked()
    {
        return achievementsData.achievementsListUnlocked;
    }

    public void UnlockAchievementsList()
    {
        achievementsData.achievementsListUnlocked = true;
        SaveDataManager.instance.isSaveDataDirty = true;
    }

    #region Load Data

    /// <summary>
    /// Update the achievements data using a AchievementsSaveData object, that was probably loaded from a file by the SaveDataManager.
    /// </summary>
    /// <param name="saveData"></param>
    public void SetAchievementsData(AchievementsSaveData saveData)
    {
        achievementsData.achievementsListUnlocked = saveData.achievementsListUnlocked;
        foreach (Achievement achievement in achievementsData.achievementsList)
        {
            Achievement achievementFromSave = saveData.achievementsList.FirstOrDefault(x => x.achievementID.Equals(achievement.achievementID));
            if (achievementFromSave != null)
            {
                achievement.unlocked = achievementFromSave.unlocked;
                if (achievement.unlocked)
                {
                    SetSteamAchievementIfPossible(achievement.achievementData.achievementSteamID);
                }
            }
        }
        SteamStoreStats();
    }

    #endregion

    public void ResetAchievements()
    {
        achievementsData.achievementsList.Clear();
        achievementsData.achievementsListUnlocked = false;
        foreach (AchievementData achievementData in achievementsScriptableObjectsList)
        {
            Achievement newAchievement = new Achievement()
            {
                achievementData = achievementData,
                unlocked = false,
                achievementID = achievementData.achievementID
            };
            achievementsData.achievementsList.Add(newAchievement);
        }
    }

    #region Steam

    public void ClearAllSteamAchievements()
    {
        foreach (Achievement achievement in achievementsData.achievementsList)
        {
            ClearSteamAchievementIfPossible(achievement.achievementData.achievementSteamID);
        }
        Steamworks.SteamUserStats.StoreStats();
    }

    private bool ClearSteamAchievementIfPossible(string achievementSteamKey)
    {
        bool achievementCleared = false;
        string log = $"Achievement Manager - Achievement {achievementSteamKey} ";
        if (SteamManager.Initialized)
        {
            if (Steamworks.SteamUserStats.RequestCurrentStats())
            {
                if (Steamworks.SteamUserStats.ClearAchievement(achievementSteamKey))
                {
                    achievementCleared = true;
                    log += "has just been reset on Steam!";
                }
                else
                {
                    log += "couldn't be reset on Steam!";
                }
            }
        }
        else
        {
            log += "can't be checked because Steam manager has not been initialized";
        }
        if (verbose == VerboseLevel.MAXIMAL)
        {
            Debug.Log(log);
        }
        return achievementCleared;
    }

    private bool SetSteamAchievementIfPossible(string achievementSteamKey)
    {
        bool achievementUnlocked = false;
        string log = $"Achievement Manager - Achievement {achievementSteamKey} ";
        if (SteamManager.Initialized && !BuildManager.instance.demoBuild)
        {
            if (Steamworks.SteamUserStats.GetAchievement(achievementSteamKey, out bool achieved))
            {
                if (!achieved)
                {
                    if (Steamworks.SteamUserStats.SetAchievement(achievementSteamKey))
                    {
                        achievementUnlocked = true;
                        log += "has just been unlocked on Steam!";
                    }
                    else
                    {
                        log += "couldn't be unlocked on Steam";
                    }
                }
                else
                {
                    log += "exists but is already unlocked";
                }
            }
            else
            {
                log += "doesn't exist on Steam";
            }
        }
        else
        {
            log += "can't be checked because Steam manager has not been initialized, or this is the demo build";
        }
        if (verbose == VerboseLevel.MAXIMAL)
        {
            Debug.Log(log);
        }
        return achievementUnlocked;
    }

    public void SteamStoreStats()
    {
        string log = $"Achievement Manager - SteamStoreStats ";
        if (SteamManager.Initialized && !BuildManager.instance.demoBuild)
        {
#if !DISABLESTEAMWORKS
            try
            {
                Steamworks.SteamUserStats.StoreStats();
            }
            catch (System.Exception e)
            {
                Debug.LogError(e.Message);
            }
#endif
        }
        else
        {
            log += "can't be done because Steam manager has not been initialized.";
        }
        if (verbose == VerboseLevel.MAXIMAL)
        {
            Debug.Log(log);
        }
    }

    #endregion

    public List<Achievement> TryUnlockAchievementsOutOfARun(bool unlockAchievements)
    {
        List<Achievement> unlockedAchievementsList = new List<Achievement>();

        List<Achievement> metaAchievements = new List<Achievement>(); // achievements that depend on other achievements

        foreach (Achievement achievement in achievementsData.achievementsList)
        {
            bool isDemoBuildAndAchievementIsNotPartOfDemo = IsAchievementLockedBehindDemo(achievement);
            bool achievementIsLockedBehindMissingIcon = IsAchievementLockedBehindMissingIcon(achievement);
            if (!achievement.unlocked && !isDemoBuildAndAchievementIsNotPartOfDemo /*&& !achievementIsLockedBehindMissingIcon*/)
            {
                bool conditionsAreMet = true;
                foreach (AchievementCondition condition in achievement.achievementData.conditionsList)
                {
                    switch (condition.conditionType)
                    {
                        case AchievementConditionType.SPECIAL:
                            switch (condition.specialKey)
                            {
                                case AchievementConditionSpecialKey.GET_100_FROINS:
                                    conditionsAreMet &= (GameManager.instance.gameData.availableCurrency >= 100);
                                    break;
                                case AchievementConditionSpecialKey.UNLOCK_A_CHARACTER:
                                    conditionsAreMet &= (CharacterManager.instance.GetUnlockedCharacterCount() > 1);
                                    break;
                                case AchievementConditionSpecialKey.COMPLETE_1_ACHIEVEMENT:
                                    if (!metaAchievements.Contains(achievement))
                                    {
                                        metaAchievements.Add(achievement);
                                    }
                                    conditionsAreMet &= (achievementsData.achievementsList.Count(x => x.unlocked) >= 1);
                                    break;
                                case AchievementConditionSpecialKey.COMPLETE_10_ACHIEVEMENTS:
                                    if (!metaAchievements.Contains(achievement))
                                    {
                                        metaAchievements.Add(achievement);
                                    }
                                    conditionsAreMet &= (achievementsData.achievementsList.Count(x => x.unlocked) >= 10);
                                    break;
                                case AchievementConditionSpecialKey.DIE_A_BUNCH_OF_TIMES:
                                    conditionsAreMet &= GameManager.instance.gameData.deathCount >= 10;
                                    break;
                                case AchievementConditionSpecialKey.EAT_20000_BUGS:
                                    conditionsAreMet &= (GameManager.instance.gameData.cumulatedScore >= 20000);
                                    break;
                                case AchievementConditionSpecialKey.UNLOCK_10_CHAPTERS:
                                    if (!metaAchievements.Contains(achievement))
                                    {
                                        metaAchievements.Add(achievement);
                                    }
                                    conditionsAreMet &= (ChapterManager.instance.GetUnlockedChaptersCount() >= 10);
                                    break;
                                case AchievementConditionSpecialKey.UNLOCK_5_CHAPTERS:
                                    if (!metaAchievements.Contains(achievement))
                                    {
                                        metaAchievements.Add(achievement);
                                    }
                                    conditionsAreMet &= (ChapterManager.instance.GetUnlockedChaptersCount() >= 5);
                                    break;
                                default:
                                    conditionsAreMet = false;
                                    break;
                            }
                            break;
                        default:
                            conditionsAreMet = false;
                            break;
                    }
                    if (!conditionsAreMet)
                    {
                        break; // if one achievementCondition was false, there's no need to check the other ones, let's move on to the next achievement instead
                    }
                }
                if (conditionsAreMet)
                {
                    // Conditions are met to unlock this Achievement!
                    unlockedAchievementsList.Add(achievement);
                    if (unlockAchievements)
                    {
                        UnlockAchievement(achievement);
                    }
                }
            }
        }

        // Special case: there is one last achievement we want to "double check" after all other achievements were computed
        UnlockMetaAchievements(metaAchievements, unlockedAchievementsList);

        SteamStoreStats();

        return unlockedAchievementsList;
    }

    public List<Achievement> GetUnlockedAchievementsForCurrentRun(bool unlockAchievements, bool forceUnlockEverything)
    {
        List<Achievement> unlockedAchievementsList = new List<Achievement>();

        // Gather useful data about the current Run
        bool runIsWon = RunManager.instance.IsCurrentRunWon();
        int level = RunManager.instance.level;
        List<RunItemInfo> allOwnedItems = RunManager.instance.ownedItems;
        PlayableCharacter playedCharacter = RunManager.instance.currentPlayedCharacter;
        int chapterCount = RunManager.instance.GetChapterCount();
        List<Chapter> completedChapters = RunManager.instance.completedChaptersList;
        FrogCharacterController player = GameManager.instance.player;
        GameMode playedGameMode = RunManager.instance.playedGameModes;

        List<Achievement> metaAchievements = new List<Achievement>(); // achievements that depend on other achievements

        foreach (Achievement achievement in achievementsData.achievementsList)
        {
            bool isDemoBuildAndAchievementIsNotPartOfDemo = IsAchievementLockedBehindDemo(achievement);
            bool achievementIsLockedBehindMissingIcon = IsAchievementLockedBehindMissingIcon(achievement);
            if (!achievement.unlocked && !isDemoBuildAndAchievementIsNotPartOfDemo /*&& !achievementIsLockedBehindMissingIcon*/)
            {
                bool conditionsAreMet = true;
                foreach (AchievementCondition condition in achievement.achievementData.conditionsList)
                {
                    switch (condition.conditionType)
                    {
                        case AchievementConditionType.CHAPTER:
                            conditionsAreMet &= (completedChapters.FirstOrDefault(x => x.chapterID.Equals(condition.playedChapter.chapterID)) != null);
                            break;
                        case AchievementConditionType.CHAPTERCOUNT:
                            conditionsAreMet &= (chapterCount >= condition.chapterCount);
                            break;
                        case AchievementConditionType.CHARACTER:
                            conditionsAreMet &= playedCharacter.characterID.Equals(condition.playedCharacter.characterID);
                            break;
                        case AchievementConditionType.FINISH_RUN:
                            conditionsAreMet &= runIsWon;
                            break;
                        case AchievementConditionType.LEVEL:
                            conditionsAreMet &= (level >= condition.reachLevel);
                            break;
                        case AchievementConditionType.RUNITEM:
                            conditionsAreMet &= (allOwnedItems.FirstOrDefault(x => x.itemName.Equals(condition.runItem.itemName)) != null);
                            break;
                        case AchievementConditionType.RUNITEMLEVEL:
                            RunItemInfo runItem = allOwnedItems.FirstOrDefault(x => x.itemName.Equals(condition.runItem.itemName));
                            if (runItem != null)
                            {
                                conditionsAreMet &= (runItem.level >= condition.reachLevel);
                            }
                            else
                            {
                                conditionsAreMet &= false;
                            }
                            break;
                        case AchievementConditionType.GAME_MODE:
                            GameMode requiredGameMode = condition.gameModes;
                            if ((playedGameMode & requiredGameMode) != requiredGameMode)
                            {
                                conditionsAreMet &= false;
                            }
                            break;
                        case AchievementConditionType.SPECIAL:
                            switch (condition.specialKey)
                            {
                                case AchievementConditionSpecialKey.GET_100_FROINS:
                                    conditionsAreMet &= (GameManager.instance.gameData.availableCurrency >= 100);
                                    break;
                                case AchievementConditionSpecialKey.UNLOCK_A_CHARACTER:
                                    conditionsAreMet &= (CharacterManager.instance.GetUnlockedCharacterCount() > 1);
                                    break;
                                case AchievementConditionSpecialKey.COMPLETE_1_ACHIEVEMENT:
                                    if (!metaAchievements.Contains(achievement))
                                    {
                                        metaAchievements.Add(achievement);
                                    }
                                    conditionsAreMet &= (achievementsData.achievementsList.Count(x => x.unlocked) >= 1);
                                    break;
                                case AchievementConditionSpecialKey.COMPLETE_10_ACHIEVEMENTS:
                                    if (!metaAchievements.Contains(achievement))
                                    {
                                        metaAchievements.Add(achievement);
                                    }
                                    conditionsAreMet &= (achievementsData.achievementsList.Count(x => x.unlocked) >= 10);
                                    break;
                                case AchievementConditionSpecialKey.DIE_A_BUNCH_OF_TIMES:
                                    conditionsAreMet &= GameManager.instance.gameData.deathCount >= 10;
                                    break;
                                case AchievementConditionSpecialKey.EAT_20000_BUGS:
                                    conditionsAreMet &= (GameManager.instance.gameData.cumulatedScore >= 20000);
                                    break;
                                case AchievementConditionSpecialKey.GATHER_ALL_FRIENDS:
                                    int friendCount = FriendsManager.instance.HasPermanentFriendsCount();
                                    conditionsAreMet &= (friendCount >= 3);
                                    break;
                                case AchievementConditionSpecialKey.UNLOCK_10_CHAPTERS:
                                    if (!metaAchievements.Contains(achievement))
                                    {
                                        metaAchievements.Add(achievement);
                                    }
                                    conditionsAreMet &= (ChapterManager.instance.GetUnlockedChaptersCount() >= 10);
                                    break;
                                case AchievementConditionSpecialKey.UNLOCK_5_CHAPTERS:
                                    if (!metaAchievements.Contains(achievement))
                                    {
                                        metaAchievements.Add(achievement);
                                    }
                                    conditionsAreMet &= (ChapterManager.instance.GetUnlockedChaptersCount() >= 5);
                                    break;
                                case AchievementConditionSpecialKey.DIE_IN_TOADS_END_CHAPTER:
                                    Chapter lastChapter = RunManager.instance.currentChapter;
                                    conditionsAreMet &= (lastChapter?.chapterID == ChapterManager.instance.toadEndChapterForSpecialStuff.chapterID);
                                    break;
                                case AchievementConditionSpecialKey.MOVE_FAR_ENOUGH_IN_KERMITS_END_CHAPTER:
                                    ChapterData kermitEndChapter = ChapterManager.instance.kermitEndChapterForSpecialStuff;
                                    float playerDistanceFromSpawn = RunManager.instance.player.transform.position.magnitude / 10;
                                    float minDistanceNeeded = kermitEndChapter.nextChapterConditionCount.goal;
                                    conditionsAreMet &= (RunManager.instance.currentChapter?.chapterID == kermitEndChapter.chapterID && playerDistanceFromSpawn >= minDistanceNeeded);
                                    if (conditionsAreMet) UnlockAchievement(achievement);
                                    break;
                            }
                            break;
                    }
                    if (!conditionsAreMet)
                    {
                        break; // if one achievementCondition was false, there's no need to check the other ones, let's move on to the next achievement instead
                    }
                }

                if (conditionsAreMet || forceUnlockEverything)
                {
                    // Conditions are met to unlock this Achievement!
                    unlockedAchievementsList.Add(achievement);
                    if (unlockAchievements)
                    {
                        UnlockAchievement(achievement);
                    }
                }
            }
        }

        // Special case: there is one last achievement we want to "double check" after all other achievements were computed
        UnlockMetaAchievements(metaAchievements, unlockedAchievementsList);

        SteamStoreStats();

        return unlockedAchievementsList;
    }

    private void UnlockMetaAchievements(List<Achievement> metaAchievements, List<Achievement> unlockedAchievementsList)
    {
        foreach (Achievement achievement in metaAchievements)
        {
            if (!achievement.unlocked)
            {
                bool conditionsAreMet = true;
                foreach (AchievementCondition condition in achievement.achievementData.conditionsList)
                {
                    if (condition.conditionType == AchievementConditionType.SPECIAL && condition.specialKey == AchievementConditionSpecialKey.COMPLETE_1_ACHIEVEMENT)
                    {
                        conditionsAreMet &= (achievementsData.achievementsList.Count(x => x.unlocked) >= 1);
                    }
                    if (condition.conditionType == AchievementConditionType.SPECIAL && condition.specialKey == AchievementConditionSpecialKey.COMPLETE_10_ACHIEVEMENTS)
                    {
                        conditionsAreMet &= (achievementsData.achievementsList.Count(x => x.unlocked) >= 10);
                    }
                    if (condition.conditionType == AchievementConditionType.SPECIAL && condition.specialKey == AchievementConditionSpecialKey.UNLOCK_10_CHAPTERS)
                    {
                        conditionsAreMet &= (ChapterManager.instance.GetUnlockedChaptersCount() >= 10);
                    }
                    if (condition.conditionType == AchievementConditionType.SPECIAL && condition.specialKey == AchievementConditionSpecialKey.UNLOCK_5_CHAPTERS)
                    {
                        conditionsAreMet &= (ChapterManager.instance.GetUnlockedChaptersCount() >= 5);
                    }
                }
                if (conditionsAreMet)
                {
                    // Conditions are met to unlock this Achievement!
                    UnlockAchievement(achievement);
                    unlockedAchievementsList.Add(achievement);
                }
            }
        }
    }

    public void UnlockListOfAchievements(List<Achievement> loadedAchievementsList)
    {
        if (verbose == VerboseLevel.MAXIMAL)
        {
            Debug.Log($"Achievement Manager - Calling UnlockListOfAchievements()");
        }

        // Go through every achievement
        foreach (Achievement achievement in achievementsData.achievementsList)
        {
            bool isDemoBuildAndAchievementIsNotPartOfDemo = IsAchievementLockedBehindDemo(achievement);

            if (!isDemoBuildAndAchievementIsNotPartOfDemo && loadedAchievementsList.Contains(achievement))
            {
                // Achievement is not locked and the loaded list contains this achievement
                Achievement loadedAchievement = loadedAchievementsList.FirstOrDefault(x => x.achievementID == achievement.achievementID);

                if (verbose == VerboseLevel.MAXIMAL)
                {
                    Debug.Log($"Achievement Manager - Achievement {achievement.achievementID} should be unlocked");
                }

                if (loadedAchievement != null && loadedAchievement.unlocked && !achievement.unlocked)
                {
                    // Loaded achievement is unlocked in save file but not in-game (we need to unlock it now)
                    UnlockAchievement(achievement);
                }
            }
        }
        SteamStoreStats();
    }

    private void UnlockAchievement(Achievement achievement)
    {
        string unlockLog = $"Achievements Manager - Unlocking {achievement.achievementID}: ";

        // unlock reward
        AchievementReward reward = achievement.achievementData.reward;
        switch (reward.rewardType)
        {
            case AchievementRewardType.CHAPTER:
                ChapterManager.instance.UnlockChapter(reward.chapter);
                unlockLog = $"Unlock chapter {reward.chapter.chapterID}";
                break;
            case AchievementRewardType.CHARACTER:
                CharacterManager.instance.UnlockCharacter(reward.character.characterID);
                unlockLog = $"Unlock character {reward.character.characterID}";
                break;
            case AchievementRewardType.CURRENCY:
                GameManager.instance.ChangeAvailableCurrency(reward.currencyAmount);
                unlockLog = $"Get {reward.currencyAmount} froins";
                break;
            case AchievementRewardType.FEATURE:
                GameManager.instance.UnlockFeature(reward.featureID);
                unlockLog = $"Unlock feature {reward.featureID}";
                break;
            case AchievementRewardType.RUN_ITEM:
                RunItemManager.instance.UnlockRunItem(reward.runItem.itemName);
                unlockLog = $"Unlock run item {reward.runItem.itemName}";
                break;
            case AchievementRewardType.SHOP_ITEM:
                ShopManager.instance.RestockItem(reward.shopItem, reward.shopItemRestockCount);
                unlockLog = $"Restock shop item {reward.shopItem.itemName}";
                break;
            case AchievementRewardType.GAME_MODE:
                CharacterManager.instance.UnlockGameMode(reward.gameMode);
                unlockLog = $"Unlock game mode {reward.gameMode.ToString()}";
                break;
        }
        if (verbose == VerboseLevel.MAXIMAL)
        {
            Debug.Log(unlockLog);
        }

        // set achievement state to unlock
        achievement.unlocked = true;

        // get Steam achievement
        SetSteamAchievementIfPossible(achievement.achievementData.achievementSteamID);
    }

    private bool IsAchievementLockedBehindDemo(Achievement achievement)
    {
        return BuildManager.instance.demoBuild && !achievement.achievementData.partOfDemo;
    }

    private bool IsAchievementLockedBehindMissingIcon(Achievement achievement)
    {
        return BuildManager.instance.thingsWithMissingSpritesAreHidden && (achievement.achievementData.achievementUnlockedIcon == null || achievement.achievementData.achievementLockedIcon == null);
    }

    private bool IsAchievementAvailable(Achievement achievement)
    {
        bool achievementConditionsCanBeFulfilled = true;
        foreach (AchievementCondition achievementCondition in achievement.achievementData.conditionsList)
        {
            if (achievementCondition.conditionType == AchievementConditionType.GAME_MODE && !CharacterManager.instance.IsGameModeUnlocked(achievementCondition.gameModes))
            {
                // The achievement requires to play a game mode that has not been unlocked yet
                achievementConditionsCanBeFulfilled = false;
            }
            if (achievementCondition.conditionType == AchievementConditionType.CHARACTER && achievementCondition.playedCharacter != null && !CharacterManager.instance.IsCharacterUnlocked(achievementCondition.playedCharacter.characterID))
            {
                // The achievement requires to play as a character that has not been unlocked yet
                achievementConditionsCanBeFulfilled = false;
            }
            if (achievementCondition.conditionType == AchievementConditionType.CHAPTER && achievementCondition.playedChapter != null)
            {
                // The achievement requires to play a specific chapter
                bool atLeastOneChapterConditionChunkIsValid = false;
                foreach (ChapterConditionsChunk chapterConditionsChunk in achievementCondition.playedChapter.conditions)
                {
                    bool allChapterConditionsAreValid = true;
                    foreach (ChapterCondition chapterCondition in chapterConditionsChunk.conditionsList)
                    {
                        if (chapterCondition.conditionType == ChapterConditionType.CHARACTER && chapterCondition.characterData != null && !CharacterManager.instance.IsCharacterUnlocked(chapterCondition.characterData.characterID))
                        {
                            // The condition chunk requires to play as a character that has not been unlocked yet
                            allChapterConditionsAreValid = false;
                            break;
                        }
                    }
                    if (allChapterConditionsAreValid)
                    {
                        atLeastOneChapterConditionChunkIsValid = true;
                        break;
                    }
                }
                if (!atLeastOneChapterConditionChunkIsValid)
                {
                    achievementConditionsCanBeFulfilled = false;
                }
            }

            if (!achievementConditionsCanBeFulfilled)
            {
                break;
            }
        }
        return achievementConditionsCanBeFulfilled;
    }

    private List<Achievement> SortAchievementList(List<Achievement> achievements)
    {
        List<Achievement> result = achievements.OrderBy(x => x.achievementID).OrderBy(x => (x.achievementData.overrideOrder > -1 ? x.achievementData.overrideOrder : int.MaxValue)).OrderBy(x => (x.achievementData.isSecret && !x.unlocked)).OrderByDescending(x => IsAchievementAvailable(x)).OrderBy(x => IsAchievementLockedBehindDemo(x)).ToList();

        return result;
    }

    /// <summary>
    /// Update the UI of the Achievements screen (Quest book).
    /// </summary>
    public void DisplayAchievementsScreen()
    {
        List<Achievement> orderedAchievements = SortAchievementList(achievementsData.achievementsList); ;

        // OLD CODE: this was used to only display achievements that have icons and are part of the demo
        //int allAchievementsCount = orderedAchievements.Count(x => !IsAchievementLockedBehindDemo(x) && !IsAchievementLockedBehindMissingIcon(x));
        //int allUnlockedAchievementsCount = orderedAchievements.Count(x => !IsAchievementLockedBehindDemo(x) && !IsAchievementLockedBehindMissingIcon(x) && x.unlocked);

        // NEW CODE: we display every achievement, regardless of missing icons or demo stuff
        // It is way better to have many achievements to show in the list!
        int allAchievementsCount = 2;
        int allUnlockedAchievementsCount = 0;

        // Remove previous buttons except the first empty one
        GameObject emptySlot = null;
        foreach (Transform child in achievementScrollEntriesParent)
        {
            if (!child.name.Contains("DO NOT DESTROY"))
            {
                Destroy(child.gameObject);
            }
            else if (emptySlot == null)
            {
                emptySlot = child.gameObject;
            }
        }

        int entryCount = 1;

        if (IsAchievementsListUnlocked())
        {
            orderedAchievements = SortAchievementList(achievementsData.achievementsList);
            allAchievementsCount = orderedAchievements.Count();
            allUnlockedAchievementsCount = orderedAchievements.Count(x => x.unlocked);
            achievementCountTextMesh.text = $"{allUnlockedAchievementsCount} / {allAchievementsCount}";

            // Create new entries
            foreach (Achievement achievement in orderedAchievements)
            {
                bool achievementIsLockedBehindDemo = IsAchievementLockedBehindDemo(achievement);
                bool achievementIsLockedBehindMissingIcon = IsAchievementLockedBehindMissingIcon(achievement);
                bool achievementIsNotSetup = (achievement.achievementData.reward.rewardType == AchievementRewardType.RUN_ITEM && achievement.achievementData.reward.runItem == null)
                    || (achievement.achievementData.reward.rewardType == AchievementRewardType.SHOP_ITEM && achievement.achievementData.reward.shopItem == null);

                bool addThisAchievementToTheList = !achievementIsNotSetup;
                // addThisAchievementToTheList &= !achievementIsLockedBehindDemo && !achievementIsLockedBehindMissingIcon;

                if (addThisAchievementToTheList)
                {
                    GameObject achievementEntryGo = null;

                    if (achievement.achievementData.conditionsList[0].specialKey == AchievementConditionSpecialKey.DIE_A_BUNCH_OF_TIMES || (achievement.achievementData.conditionsList[0].specialKey == AchievementConditionSpecialKey.EAT_20000_BUGS && !BuildManager.instance.demoBuild))
                    {
                        achievementEntryGo = Instantiate(achievementEntryWithCountPrefab, achievementScrollEntriesParent);
                    }
                    else
                    {
                        achievementEntryGo = Instantiate(achievementEntryPrefab, achievementScrollEntriesParent);
                    }

                    AchievementEntryPanelBehaviour achievementEntryScript = achievementEntryGo.GetComponent<AchievementEntryPanelBehaviour>();
                    bool darkerBkg = (entryCount / 2) % 2 == 0;
                    bool canAchieve = IsAchievementAvailable(achievement);
                    achievementEntryScript.Initialize(achievement, darkerBkg, canAchieve);
                    entryCount++;
                }
            }
        }
        else
        {
            achievementCountTextMesh.text = "0 / ???";
            entryCount = 1; // 2 quests + 2 empty slots

            foreach (AchievementData achievementData in alwaysVisibleAchievementsScriptableObjectsList)
            {
                // Add an empty slot
                GameObject newEmptySlot = Instantiate(emptySlot, achievementScrollEntriesParent);
                newEmptySlot.name = "Empty Slot";
                entryCount++;

                // Add the quest button
                GameObject achievementEntryGo = Instantiate(achievementEntryPrefab, achievementScrollEntriesParent);
                AchievementEntryPanelBehaviour achievementEntryScript = achievementEntryGo.GetComponent<AchievementEntryPanelBehaviour>();
                bool darkerBkg = (entryCount / 2) % 2 == 0;
                bool canAchieve = true;
                Achievement achievement = new Achievement();
                achievement.achievementData = achievementData;
                achievement.unlocked = false;
                achievement.achievementID = achievementData.achievementID;
                achievementEntryScript.Initialize(achievement, darkerBkg, canAchieve);
                entryCount++;

            }
            entryCount = 11; // to make sure container size is correct
        }

        // Set size of container panel
        GridLayoutGroup containerGridLayoutGroup = achievementScrollEntriesParent.GetComponent<GridLayoutGroup>();
        float entryHeight = containerGridLayoutGroup.cellSize.y + containerGridLayoutGroup.spacing.y;
        float padding = containerGridLayoutGroup.padding.top + containerGridLayoutGroup.padding.bottom;
        achievementScrollContentPanel.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, ((entryCount + 1) / 2) * entryHeight + padding);

        // Set scroll view to top position
        achievementsListScrollRect.normalizedPosition = new Vector2(0, 1);

        // Set scroll bar
        achievementsScrollbar.SetCursorCentered(entryCount <= 11);
    }

    public bool IsChapterPartOfALockedAchievement(Chapter chapter)
    {
        bool achievementFound = false;

        List<Achievement> newUnlockedAchievementsSoFar = GetUnlockedAchievementsForCurrentRun(false, false);

        foreach (Achievement achievement in achievementsData.achievementsList)
        {
            bool isDemoBuildAndAchievementIsNotPartOfDemo = IsAchievementLockedBehindDemo(achievement);
            bool achievementIsLockedBehindMissingIcon = IsAchievementLockedBehindMissingIcon(achievement);
            bool achievementIsAvailable = IsAchievementAvailable(achievement);
            bool achievementIsLocked = (!newUnlockedAchievementsSoFar.Contains(achievement) && !achievement.unlocked);

            if (verbose == VerboseLevel.MAXIMAL)
            {
                string log = $"Achievement Manager - isDemoBuildAndAchievementIsNotPartOfDemo for chapter {chapter.chapterID} returned {isDemoBuildAndAchievementIsNotPartOfDemo}\n";
                log += $"achievementIsLockedBehindMissingIcon for chapter {chapter.chapterID} returned {achievementIsLockedBehindMissingIcon}\n";
                log += $"achievementIsAvailable for chapter {chapter.chapterID} returned {achievementIsAvailable}\n";
                log += $"achievementIsLocked for chapter {chapter.chapterID} returned {achievementIsLocked}";
                Debug.Log(log);
            }

            if (achievementIsLocked && !isDemoBuildAndAchievementIsNotPartOfDemo && !achievementIsLockedBehindMissingIcon && achievementIsAvailable)
            {
                foreach (AchievementCondition condition in achievement.achievementData.conditionsList)
                {
                    switch (condition.conditionType)
                    {
                        case AchievementConditionType.CHAPTER:
                            if (condition.playedChapter.chapterID != null && condition.playedChapter.chapterID.Equals(chapter.chapterID))
                            {
                                achievementFound = true; // Finishing that chapter is a achievementCondition to unlock this achievement                                
                            }
                            break;
                        case AchievementConditionType.RUNITEM:
                            foreach (FixedCollectible collectible in chapter.chapterData.specialCollectiblesOnTheMap)
                            {
                                if (collectible.collectibleType == FixedCollectibleType.STATS_ITEM
                                    && condition.runItem.itemName.Equals(collectible.collectibleStatItemData.itemName))
                                {
                                    achievementFound = true; // There's an item in this chapter that is a achievementCondition to unlock this achievement
                                    break;
                                }
                            }
                            break;
                        case AchievementConditionType.SPECIAL:
                            if (condition.specialKey == AchievementConditionSpecialKey.MOVE_FAR_ENOUGH_IN_KERMITS_END_CHAPTER)
                            {
                                achievementFound = true; // There is a goal in this chapter that must be reached to unlock this achievement.
                            }
                            break;
                    }
                    if (achievementFound)
                    {
                        break;
                    }
                }
            }
            if (achievementFound)
            {
                break;
            }
        }

        return achievementFound;
    }

    public bool AllDemoAchievementsHaveBeenUnlocked()
    {
        int lockedDemoAchievements = achievementsData.achievementsList.Count(x => x.achievementData.partOfDemo && !x.unlocked);
        return (lockedDemoAchievements == 0);
    }

    /// <summary>
    /// Lock all achievements that are not part of the demo.
    /// </summary>
    public void ApplyDemoLimitationToAchievements()
    {
        foreach (Achievement achievement in achievementsData.achievementsList)
        {
            if (achievement.unlocked && !achievement.achievementData.partOfDemo)
            {
                achievement.unlocked = false;
            }
        }
    }

    public void ApplyCharacterStatsIncrementsFromAchievementsIfNeeded()
    {
        List<Achievement> unlockedStatIncrementAchievementsList = achievementsData.achievementsList.Where(
            x => x.unlocked // only unlocked achievements
            && (!BuildManager.instance.demoBuild || x.achievementData.partOfDemo) // if we're playing the Demo then only achievements that are part of Demo
            && x.achievementData.reward.rewardType == AchievementRewardType.FEATURE // only achievements that unlock a new "feature"
            && (x.achievementData.reward.featureID == RewardFeatureType.GHOST_BUFF ||
                x.achievementData.reward.featureID == RewardFeatureType.RIBBIT_BUFF ||
                x.achievementData.reward.featureID == RewardFeatureType.STANLEY_BUFF ||
                x.achievementData.reward.featureID == RewardFeatureType.TOAD_BUFF) // only if "feature" is a stat boost for a frog
            ).ToList();

        foreach (Achievement achievement in unlockedStatIncrementAchievementsList)
        {
            PlayableCharacter playableFrog = null;
            switch (achievement.achievementData.reward.featureID)
            {
                case RewardFeatureType.GHOST_BUFF:
                    playableFrog = CharacterManager.instance.charactersData.charactersList.FirstOrDefault(x => x.characterID.Equals("GHOST"));
                    break;
                case RewardFeatureType.RIBBIT_BUFF:
                    playableFrog = CharacterManager.instance.charactersData.charactersList.FirstOrDefault(x => x.characterID.Equals("POISONOUS_FROG"));
                    break;
                case RewardFeatureType.STANLEY_BUFF:
                    playableFrog = CharacterManager.instance.charactersData.charactersList.FirstOrDefault(x => x.characterID.Equals("STANLEY"));
                    break;
                case RewardFeatureType.TOAD_BUFF:
                    playableFrog = CharacterManager.instance.charactersData.charactersList.FirstOrDefault(x => x.characterID.Equals("TOAD"));
                    break;
                default:
                    break;
            }
            if (playableFrog != null && playableFrog.unlocked)
            {
                playableFrog.characterStatsIncrements = new StatsWrapper();
                /*if (playableFrog.characterStatsIncrements == null)
                {
                    playableFrog.characterStatsIncrements = new StatsWrapper();
                }*/
                if (!playableFrog.storyCompleted)
                {
                    GameManager.instance.UnlockFeature(achievement.achievementData.reward.featureID); // Add story stat increment
                }
            }
        }
    }

    public int GetLockedRestocksForItem(ShopItemData shopItemData)
    {
        List<Achievement> lockedAchievementsThatRestockThisItem = achievementsData.achievementsList.Where(
            x => !x.unlocked // only locked achievements
            && (!BuildManager.instance.demoBuild || x.achievementData.partOfDemo) // if we're playing the Demo then only achievements that are part of Demo
            && x.achievementData.reward.rewardType == AchievementRewardType.SHOP_ITEM // only achievements that give a shop item
            && (x.achievementData.reward.shopItem == shopItemData) // only if shop item is the one we want
            ).ToList();

        return lockedAchievementsThatRestockThisItem.Count;
    }

    public Achievement GetAchievementThatUnlocksCharacter(string characterID)
    {
        Achievement achievementThatUnlocksCharacter = achievementsData.achievementsList.FirstOrDefault(x =>
            x.achievementData.reward.rewardType == AchievementRewardType.CHARACTER
            && (x.achievementData.reward.character.characterID == characterID)
            );
        return achievementThatUnlocksCharacter;
    }
}
