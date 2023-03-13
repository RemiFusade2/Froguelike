using System.Collections;
using System.Collections.Generic;
using UnityEngine;


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
    OTHER, // specific condition, hardcoded and identified with a string key

    /*
     * EVERYTHING UNDER HERE IS NOT IMPLEMENTED, AND MAYBE NOT NEEDED
    PLAYTIME, // play the game for at least that amount of time
    PLAYTIME_MIN, // play the game for at least that specific amount of time
    PLAYTIME_MAX, // play the game for no more than that specific amount of time
    CHAPTERUNIQUECOUNT, // complete that many unique chapters
    CHAPTER, // complete a specific chapter
    FRIEND, // find a specific friend
    FRIENDCOUNT, // find an amount of friends
    RUNITEM, // have a specific item (can be a weapon too)
    RUNITEMLEVEL, // have a specific item (can be a weapon too) at a specific level
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

    [Tooltip("The level you have to reach to unlock this achievement")]
    public int reachLevel = 0;

    [Tooltip("The chapter count you have to reach to unlock this achievement")]
    public int chapterCount = 0;

    [Tooltip("The run item you have to own to unlock this achievement")]
    public RunItemData runItem = null;

    [Tooltip("The key used in the code to check for a special condition")]
    public string other_keyCode = "[CONDITION_KEY]";
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
/// An AchievementReward is a reward that you get when fulfilling the conditions for an achievement.
/// The reward is given at the end of a Run, when achievements are computed.
/// </summary>
[System.Serializable]
public class AchievementReward
{
    [Tooltip("The type of reward we are talking about, choose from the list")]
    public AchievementRewardType rewardType;

    [Tooltip("A short description of that reward")]
    public string rewardDescription;

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

    [Tooltip("A unique string to identify a feature (hardcoded)")]
    public string featureID = "[FEATURE_IDENTIFIER]";
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

    [Header("Achievement settings - display")]
    [Tooltip("Achievement title")]
    public string achievementTitle = "[Achievement title]";
    [Tooltip("A description of the achievement")]
    public string achievementDescription = "[Achievement description]";
    [Tooltip("A visual representation of the achievement")]
    public Sprite achievementIcon;

    [Header("Achievement settings - Steam")]
    [Tooltip("The key used by Steam to identify that achievement")]
    public string achievementSteamID = "[ACH_UNIQUE_KEY]";
    
    [Header("Condition")]
    [Tooltip("A list of conditions that must be valid together")]
    public List<AchievementCondition> conditionsList;

    [Header("Reward")]
    [Tooltip("The reward you get when fulfilling these conditions")]
    public AchievementReward reward;    
}
