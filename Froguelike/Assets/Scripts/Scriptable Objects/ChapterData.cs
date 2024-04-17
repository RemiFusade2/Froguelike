using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

#region Enums

/// <summary>
/// A chapter can change the environment you're playing in. Here are the different environment available.
/// </summary>
[System.Serializable]
public enum EnvironmentType
{
    NO_CHANGE,
    SWAMP
}

/// <summary>
/// A chapter can change the style of the main character. Here are the different possibilities.
/// </summary>
[System.Serializable]
public enum CharacterStyle
{
    NO_CHANGE,
    GHOST
}

/// <summary>
/// A chapter can add a Hat on the frog. Here are the different style of hats available.
/// </summary>
[System.Serializable]
public enum HatType
{
    FANCY_HAT,
    FASHION_HAT,
    SUN_HAT,
    BOW_BLUE,
    BOW_RED,
    BOW_PURPLE
}

/// <summary>
/// A Fixed Collectible is a collectible that is not affected by the magnet.
/// You have to walk on it to catch it.
/// </summary>
[System.Serializable]
public enum FixedCollectibleType
{
    STATS_ITEM,
    WEAPON_ITEM,
    HAT,
    FRIEND
}

/// <summary>
/// The different values for spawn frequency of obstacles and collectibles.
/// These values will define specific numbers somewhere, different depending on what kind of stuff we're spawning.
/// </summary>
[System.Serializable]
public enum SpawnFrequency
{
    NONE,
    FEW,
    MEDIUM,
    LOTS
}

[System.Serializable]
public class CollectibleSpawnFrequency
{
    public CollectibleType Type;
    public SpawnFrequency Frequency;
}

/// <summary>
/// All different types of conditions for a Chapter to appear
/// </summary>
[System.Serializable]
public enum ChapterConditionType
{
    UNLOCKED,
    PLAYED_CHAPTER,
    CHAPTER_COUNT,
    ENVIRONMENT,
    HAT,
    FRIEND,
    RUN_ITEM,
    CHARACTER,
    FRIEND_COUNT,
    BOUNTIES_EATEN_IN_PREVIOUS_CHAPTER
}

#endregion

#region Classes

/// <summary>
/// FixedCollectible describes a Collectible that is spawn somewhere on the Map (at specific coordinates)
/// The Collectible can be a Run Item, a Hat, or a Friend
/// Collecting it would display a panel just for it
/// </summary>
[System.Serializable]
public class FixedCollectible
{
    [Tooltip("The coordinates (integers, can be negative) of the Tile where this Collectible will appear")]
    public Vector2Int tileCoordinates;
    [Tooltip("Prefab for the tile where the collectible will appear (in the center)")]
    public GameObject tilePrefab;

    [Tooltip("The name of this collectible")]
    public string collectibleName;
    [Tooltip("The description of this collectible")]
    public string collectibleDescription;
    [Tooltip("The type of collectible")]
    public FixedCollectibleType collectibleType;

    // In case the collectible is a RunStatItem
    [Tooltip("The Run Stat Item in question")]
    public RunStatItemData collectibleStatItemData;

    // In case the collectible is a RunWeapon
    [Tooltip("The Run Weapon Item in question")]
    public RunWeaponItemData collectibleWeaponItemData;

    // In case the collectible is a hat
    [Tooltip("The Hat in question")]
    public HatType collectibleHatType;

    // In case the collectible is a friend
    [Tooltip("The Friend in question")]
    public FriendType collectibleFriendType;

    [Tooltip("The title when this collectible is found")]
    public string foundCollectibleTitle;
    [Tooltip("What appears as text if you want to accept this collectible")]
    public string acceptCollectibleStr;
    [Tooltip("What appears as text if you want to refuse this collectible")]
    public string refuseCollectibleStr;

    [Tooltip("How high level your compass must be to show that item")]
    public int compassLevel;
}


/// <summary>
/// ChapterCondition describe a condition for a Chapter to show up.
/// Many of these conditions can be used together, and there are multiple kinds of conditions.
/// </summary>
[System.Serializable]
public class ChapterCondition
{
    [Tooltip("The type of condition we are talking about, choose from the list")]
    public ChapterConditionType conditionType;
    [Tooltip("This would reverse the condition")]
    public bool not = false;

    [Tooltip("Played Chapter")]
    public ChapterData chapterData;

    [Tooltip("This chapter can appear only after Chapter count # (included)")]
    [Range(1, 5)]
    public int minChapterCount = 1;
    [Tooltip("This chapter can appear only before Chapter count # (included)")]
    [Range(1, 5)]
    public int maxChapterCount = 5;

    [Tooltip("The active Environment")]
    public EnvironmentType environmentType = EnvironmentType.NO_CHANGE;

    [Tooltip("An active Hat type")]
    public HatType hatType = HatType.FANCY_HAT;

    [Tooltip("An active Friend type")]
    public FriendType friendType = FriendType.FROG;

    [Tooltip("An active Item")]
    public RunItemData itemName;

    [Tooltip("The current played Character")]
    public CharacterData characterData;

    [Tooltip("This chapter can appear only if you have at least this amount of friends")]
    [Range(0, 10)]
    public int minFriendsCount = 0;
    [Tooltip("This chapter can appear only if you have at most this amount of friends")]
    [Range(0, 10)]
    public int maxFriendsCount = 10;

    [Tooltip("This chapter can appear only if you have at least this amount of bounties eaten")]
    public int minBountiesEaten = 0;
}

/// <summary>
/// A ChapterConditionsChunk is a list of ChapterCondition.
/// All of these conditions must be valid for the "chunk" to be considered valid.
/// Also we're using this structure because Unity is shit at serializing Lists of Lists
/// </summary>
[System.Serializable]
public class ChapterConditionsChunk
{
    [Tooltip("A list of conditions that must be valid together")]
    public List<ChapterCondition> conditionsList;
}

/// <summary>
/// ChapterWeightChange contains the chapter that needs to be modified and the modification to its weight (likelihood of appearance)
/// </summary>
[System.Serializable]
public class ChapterWeightChange
{
    [Tooltip("Chapter scriptable object itself")]
    public ChapterData chapter;

    [Tooltip("Change to its weight in the current Run, can be positive or negative")]
    public float weightChange;
}

/// <summary>
/// Bounty contains information about the nature and amount of collectibles
/// </summary>
[System.Serializable]
public class Bounty
{
    [Tooltip("Type of reward")]
    public CollectibleType collectibleType;
    [Tooltip("Amount of rewards")]
    public int amount;
    [Tooltip("Value of rewards")]
    public int value;
}

/// <summary>
/// BountyBug contains information about when a bounty bug must be spawned and which bug
/// </summary>
[System.Serializable]
public class BountyBug
{
    [Header("Spawn")]
    [Tooltip("Time after which this bug must be spawned")]
    public float spawnTime;

    [Header("Enemy")]
    [Tooltip("The type of enemy that would spawn (combined with its tier, we'd know what enemy spawns exactly)")]
    public EnemyType enemyType;
    [Tooltip("Formula that would give a value between 1 and 5. You can use the keyword \"chapter\" to adapt the enemy tier to the current chapter count.")]
    public string tierFormula;

    [Header("Stat Multipliers")]
    [Tooltip("Override the HP Max factor of that bounty?")]
    public bool overrideHealthMultiplier = false;
    [Tooltip("HP Max of bug would be multiplied by this value instead of the default")]
    public float healthMultiplier = 0;
    [Space]
    [Tooltip("Override the Damage factor of that bounty?")]
    public bool overrideDamageMultiplier = false;
    [Tooltip("Damage of bug is multiplied by this value")]
    public float damageMultiplier = 0;
    [Space]
    [Tooltip("Override the XP factor of that bounty?")]
    public bool overrideXpMultiplier = false;
    [Tooltip("XP given by this bug is multiplied by this value")]
    public float xpMultiplier = 0;
    [Space]
    [Tooltip("Override the Knockback Resistance of that bounty?")]
    public bool overrideKnockbackResistance = false;
    [Tooltip("Knockback resistance of this bug is multiplied by this value")]
    public float knockbackResistance = 0;

    [Header("Movement")]
    [Tooltip("Which movement pattern this enemy will follow")]
    public EnemyMovePattern movePattern;
    [Tooltip("Does this bug ignore maximum speed?")]
    public bool ignoreMaxSpeed = false;

    [Header("Bounty")]
    [Tooltip("A list of collectibles + amount that serve as a bounty")]
    public List<Bounty> bountyList;

    public BountyBug(EnemyType enemyType, 
        bool overrideHealthMultiplier, float healthMultiplier, 
        bool overrideDamageMultiplier, float damageMultiplier, 
        bool overrideXpMultiplier, float xpMultiplier, 
        bool overrideKnockbackResistance, float knockbackResistance,
        EnemyMovePattern movePattern)
    {
        this.spawnTime = 0;
        this.enemyType = enemyType;
        this.tierFormula = "";

        this.overrideHealthMultiplier = overrideHealthMultiplier;
        this.healthMultiplier = healthMultiplier;

        this.overrideDamageMultiplier = overrideDamageMultiplier;
        this.damageMultiplier = damageMultiplier;

        this.overrideXpMultiplier = overrideXpMultiplier;
        this.xpMultiplier = xpMultiplier;

        this.overrideKnockbackResistance = overrideKnockbackResistance;
        this.knockbackResistance = knockbackResistance;

        this.movePattern = movePattern;
        this.bountyList = new List<Bounty>();
    }
}

#endregion



/// <summary>
/// ChapterData describes all information we need to know about a Chapter
/// </summary>
[System.Serializable]
[CreateAssetMenu(fileName = "Chapter Data", menuName = "ScriptableObjects/Froguelike/Chapter Data", order = 1)]
public class ChapterData : ScriptableObject
{
    [Header("Chapter identifier")]
    [Tooltip("Chapter ID, must be unique")]
    public string chapterID = "[CHAPTER UNIQUE IDENTIFIER]";

    [Header("Chapter settings - Demo")]
    public bool partOfDemo = false;

    [Header("Chapter settings - display")]
    [Tooltip("Chapter title")]
    public string chapterTitle = "[Chapter title]";
    [Tooltip("First element would be default description, other elements would be unlocked and displayed later, maybe in the book for chapters")]
    public List<string> chapterLore;
    [Tooltip("The 3 icons that will describe a chapter. Icons can repeat.")]
    public List<Sprite> icons;

    [Header("Chapter settings - stat changes")]
    [Tooltip("Stat changes when playing this chapter. These changes affect only the current chapter")]
    public StatsWrapper startingStatBonuses;

    [Header("Chapter settings - Length")]
    [Tooltip("The length of this chapter")]
    public float chapterLengthInSeconds = 300; // default is 5mn

    [Header("Chapter settings - Special")]
    [Tooltip("The duration multiplier of freeze effects in that chapter")]
    public float freezeDurationMultiplier = 1;

    [Header("Chapter settings - Conditions of appearance")]
    [Tooltip("Is this chapter available from the start or should it be unlocked later")]
    public bool startingUnlockState = true;
    [Tooltip("This chapter starting weight. The weight is used to determine how likely it is that this chapter will show up")]
    public float startingWeight = 1;
    [Tooltip("Can this chapter still show up even after having been played once during this run?")]
    public bool canBePlayedMultipleTimesInOneRun = false; // some kind of shortcut instead of adding that condition to the list of conditions
    [Space]
    [Tooltip("A list of chunks of conditions. The chapter will show up only if at least one of these chunks of conditions is valid")]
    public List<ChapterConditionsChunk> conditions;

    [Header("Chapter settings - Obstacles")]
    [Tooltip("The amount of rocks on a tile")]
    public SpawnFrequency rocksSpawnFrequency;
    [Tooltip("The amount of ponds on a tile")]
    public SpawnFrequency pondsSpawnFrequency;

    [Header("Chapter settings - Collectibles")]
    [Tooltip("The amount of collectible coins on a tile")]
    public SpawnFrequency coinsSpawnFrequency;
    [Tooltip("The amount of collectible health on a tile")]
    public SpawnFrequency healthSpawnFrequency;
    [Tooltip("The amount of collectible levelUp on a tile")]
    public SpawnFrequency levelUpSpawnFrequency;

    [Space]
    [Header("Chapter settings - Power Ups Collectibles")]
    public List<CollectibleSpawnFrequency> otherCollectibleSpawnFrequenciesList;

    [Space]
    [Tooltip("A list of all special collectibles on the map, like items, weapons, hat, etc.")]
    public List<FixedCollectible> specialCollectiblesOnTheMap;

    [Header("Chapter settings - Unique changes")]
    [Tooltip("Does playing this chapter change the environment")]
    public EnvironmentType environmentChange = EnvironmentType.NO_CHANGE;
    [Tooltip("Does playing this chapter change the character style")]
    public CharacterStyle characterStyleChange = CharacterStyle.NO_CHANGE;

    [Header("Chapter settings - Waves of enemies")]
    /*[Tooltip("Deprecated: A series of waves happening during this chapter. Waves have their own duration")]
    public List<Wave> waves;*/

    [Tooltip("A series of waves happening during this chapter. Waves have their own duration")]
    public List<WaveData> wavesList;

    [Tooltip("A series of bounty bugs being spawned during this chapter")]
    public List<BountyBug> bountyBugs;

    [Header("Chapter settings - Weight changes to other chapters")]
    [Tooltip("A list of all the modifications to chapters likelihood of appearance after playing this one")]
    public List<ChapterWeightChange> weightChanges;
}

