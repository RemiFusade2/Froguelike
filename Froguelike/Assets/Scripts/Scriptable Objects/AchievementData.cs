using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public enum AchievementConditionSpecialKey
{
    GET_100_FROINS = 1,
    UNLOCK_A_CHARACTER = 2,
    COMPLETE_10_ACHIEVEMENTS = 3,
    PLAY_GHOST_CHAPTER = 4,
    EAT_100000_BUGS = 5,
    GATHER_ALL_FRIENDS = 6
}

/// <summary>
/// All different types of conditions for an Achievement
/// </summary>
[System.Serializable]
public enum AchievementConditionType
{
    FINISH_RUN, // complete the run (all chapters, no game over)
    CHARACTER, // play as a specific character
    LEVEL, // reach at least that level
    CHAPTERCOUNT, // complete that many chapters
    RUNITEM, // have a specific item (can be a weapon too)
    SPECIAL, // specific condition, hardcoded and identified with a string key
    RUNITEMLEVEL, // have a specific item (can be a weapon too) at a specific level
    CHAPTER, // complete a specific chapter

    /*
     * EVERYTHING UNDER HERE IS NOT IMPLEMENTED, AND MAYBE NOT NEEDED
    PLAYTIME, // play the game for at least that amount of time
    PLAYTIME_MIN, // play the game for at least that specific amount of time
    PLAYTIME_MAX, // play the game for no more than that specific amount of time
    CHAPTERUNIQUECOUNT, // complete that many unique chapters
    FRIEND, // find a specific friend
    FRIENDCOUNT, // find an amount of friends
    RUNSTATITEMCOUNT, // have an amount of stat items
    RUNWEAPONCOUNT, // have an amount of weapons
    SHOPITEM, // buy a specific item from the Shop
    SHOPITEMLEVEL, // have a specific item at this level in the Shop
    SHOPSPENT, // spend that amount of froins in the shop
    HAT, // find a specific hat
    HACOUNT, // find an amount of hats*/
}

/// <summary>
/// An AchievementCondition is a specific condition to fulfill in order to unlock an achievement.
/// AchievementConditions can be combined together for a more specific condition.
/// Implemented type of conditions: 
/// - FINISH_RUN // complete the run
/// - CHARACTER, // play as a specific character
/// - LEVEL, // reach at least that level
/// - CHAPTERCOUNT, // complete that many chapters
/// - RUNITEM, // have a specific item (can be a weapon too)
/// - OTHER, // specific condition, hardcoded and identified with a string key
/// 
/// Example: Reach chapter 3 with Toad
/// - conditionType = CHAPTERCOUNT with chapterCount = 3
/// - conditionType = CHARACTER with playedCharacter = Toad
/// </summary>
[System.Serializable]
public class AchievementCondition
{
    [Tooltip("The type of condition we are talking about, choose from the list")]
    public AchievementConditionType conditionType;

    [Tooltip("The character you have to play as to unlock this achievement")]
    public CharacterData playedCharacter = null;

    [Tooltip("The chapter you have to play to unlock this achievement")]
    public ChapterData playedChapter = null;

    [Tooltip("The level you have to reach to unlock this achievement")]
    public int reachLevel = 0;

    [Tooltip("The chapter count you have to reach to unlock this achievement")]
    public int chapterCount = 0;

    [Tooltip("The run item you have to own to unlock this achievement")]
    public RunItemData runItem = null;

    [Tooltip("The key used in the code to check for a special condition")]
    public AchievementConditionSpecialKey specialKey;
}

/// <summary>
/// All different types of rewards for an Achievement
/// </summary>
[System.Serializable]
public enum AchievementRewardType
{
    CHARACTER, // unlock a new playable character
    CHAPTER, // unlock a new playable chapter
    SHOP_ITEM, // unlock/restock a new item in the shop (or increase its max level)
    RUN_ITEM, // unlock a new run item to get during a level up
    CURRENCY, // give a bunch of froins
    FEATURE // unlock a new feature (will be hardcoded)
}

/// <summary>
/// All different features you can get as a reward
/// </summary>
[System.Serializable]
public enum RewardFeatureType
{
    SHOP, // Open the shop
    CHARACTER_SELECTION, // Make it possible to select a character at the start of the game
    ACHIEVEMENTS_LIST, // See a list of all achievements in the game
}

/// <summary>
/// An AchievementReward is a reward that you get when fulfilling the conditions for an achievement.
/// The reward is given at the end of a Run, when achievements are computed.
/// </summary>
[System.Serializable]
public class AchievementReward
{
    [Tooltip("The type of reward we are talking about, choose from the list")]
    public AchievementRewardType rewardType;

    [Tooltip("A short description of that reward")]
    public string rewardDescription; // it will replace some keywords with the name they refer to

    [Tooltip("Unlocked Character")]
    public CharacterData character = null;

    [Tooltip("Unlocked Chapter")]
    public ChapterData chapter = null;

    [Tooltip("Shop Item")]
    public ShopItemData shopItem = null;
    [Tooltip("Shop Item Restock Count")]
    public int shopItemRestockCount = 1;

    [Tooltip("Unlocked Run Item")]
    public RunItemData runItem = null;

    [Tooltip("Amount of Froins")]
    public int currencyAmount = 0;

    [Tooltip("A value to identify a feature")]
    public RewardFeatureType featureID;
}


/// <summary>
/// AchievementData describes all information we need to know about an Achievement
/// </summary>
[System.Serializable]
[CreateAssetMenu(fileName = "Achievement Data", menuName = "ScriptableObjects/Froguelike/Achievement Data", order = 1)]
public class AchievementData : ScriptableObject
{
    [Header("Achievement identifier")]
    [Tooltip("Achievement ID, must be unique. Used only by the game")]
    public string achievementID = "[ACHIEVEMENT UNIQUE IDENTIFIER]";

    [Header("Achievement settings")]
    [Tooltip("Achievement is hidden until you get it")]
    public bool isSecret = false;
    [Tooltip("Achievement is available in the demo of the game")]
    public bool partOfDemo = false;

    [Header("Achievement settings - display")]
    [Tooltip("Achievement title")]
    public string achievementTitle = "[Achievement title]";
    [Tooltip("A description of the achievement")]
    public string achievementDescription = "[Achievement description]";
    [Tooltip("A visual representation of the achievement when it is locked")]
    public Sprite achievementLockedIcon;
    [Tooltip("A visual representation of the achievement when it has been achieved")]
    public Sprite achievementUnlockedIcon;

    [Header("Achievement settings - Steam")]
    [Tooltip("The key used by Steam to identify that achievement")]
    public string achievementSteamID = "[ACH_UNIQUE_KEY]";
    
    [Header("Condition")]
    [Tooltip("A list of conditions that must be valid together")]
    public List<AchievementCondition> conditionsList;

    [Header("Reward")]
    [Tooltip("The reward you get when fulfilling these conditions")]
    public AchievementReward reward;
    
    /*
    /// <summary>
    /// Get a value for ordering the achievements. Values can go from int.MinValue to int.MaxValue
    /// Values depends on conditions and rewards, here is how it goes from lower to higher:
    /// - Reward is a feature
    /// - Find an item
    /// - Reach a Chapter with a Character
    /// - Reach a Level with a Character
    /// - Finish a Run with a Character
    /// - Other special conditions
    /// </summary>
    /// <returns></returns>
    public int GetOrder()
    {
        int order = 0;

        if (reward.rewardType == AchievementRewardType.FEATURE)
        {
            order -= 100000 + reward.featureID.Length;
        }

        foreach (AchievementCondition condition in conditionsList)
        {
            switch (condition.conditionType)
            {
                case AchievementConditionType.RUNITEM:
                    order += condition.runItem.GetOrder();
                    break;
                case AchievementConditionType.CHARACTER:
                    order += 10000 * condition.playedCharacter.GetOrder();
                    break;
                case AchievementConditionType.CHAPTERCOUNT:
                    order += 100 * condition.chapterCount;
                    break;
                case AchievementConditionType.LEVEL:
                    order += 1000 * condition.reachLevel;
                    break;
                case AchievementConditionType.FINISH_RUN:
                    order += 10000000;
                    break;
                case AchievementConditionType.OTHER:
                    order += 10000000 * condition.other_keyCode.Length;
                    break;
            }
        }

        Debug.Log($"{this.achievementID}, order is = {order}");

        return order;
    }*/
}
