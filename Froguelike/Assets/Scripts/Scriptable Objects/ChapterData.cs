using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
[System.Serializable]
public enum ChapterIconType
{
    HEART,
    COIN,
    STAR,
    SKULL,
    ROCK,
    POND,
    HAT,
    FROG,
    FLY,
    BUTTERFLY,
    PLANT,
    HOURGLASS_PLUS,
    HOURGLASS_MINUS
}*/

/// <summary>
/// A chapter can add a Hat on the frog. Here are the different style of hats available.
/// </summary>
[System.Serializable]
public enum HatType
{
    NONE,
    FANCY_HAT,
    FASHION_HAT,
    SUN_HAT
}

/// <summary>
/// A chapter can add a Friend helping you. Here are the different friends available.
/// </summary>
[System.Serializable]
public enum FriendType
{
    NONE,
    FROG,
    TOAD,
    GHOST,
    POISONOUS
}

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
public enum CharacterStyleType
{
    NO_CHANGE,
    GHOST
}

/// <summary>
/// ChapterData describes all information we need to know about a Chapter:
/// - Title & Description (or lore)
/// - Length
/// - Is it unlocked from the start?
/// - Frequency of obstacles and collectibles
/// - Unique changes (environment, character style, friend, hat)
/// - A list of Waves
/// TODO for later:
/// - Add duration for each wave
/// - Add temporary stat changes 
/// - Add icons
/// </summary>
[System.Serializable]
[CreateAssetMenu(fileName = "Chapter Data", menuName = "ScriptableObjects/Froguelike/Chapter Data", order = 1)]
public class ChapterData : ScriptableObject
{
    // Unique identifier (no two chapters with same titel)
    [Tooltip("Chapter title, must be unique")]
    public string chapterTitle;

    // In case we want to display more and more lore about this chapter as we go
    [Tooltip("First element would be default description, other elements would be unlocked and displayed later.")]
    public List<string> chapterLore;

    // Should be a list of 3 icons (they can repeat)
    /*[Tooltip("Use 3 icons in that list. Icons can repeat.")]
    public List<ChapterIconType> icons;*/

    // Used to set the timer when starting this chapter
    [Tooltip("The length of this chapter")]
    public float chapterLengthInSeconds = 300;

    [Tooltip("Is this chapter available from the start or should it be unlocked later")]
    public bool startingUnlockState = true;

    /*
    // Any stat change when playing this chapter
    public StatsWrapper startingStatBonuses;*/

    [Tooltip("Can this chapter still show up even after having been played once during this run?")]
    public bool canBePlayedMultipleTimesInOneRun = false;

    [Header("Obstacles settings")]
    [Tooltip("The amount of rocks on a tile will be between these two values")]
    public Vector2 amountOfRocksPerTile_minmax;
    [Tooltip("The amount of ponds on a tile will be between these two values")]
    public Vector2 amountOfPondsPerTile_minmax;

    [Header("Collectibles settings")]
    [Tooltip("The amount of collectible coins on a tile will be between these two values")]
    public Vector2 amountOfCoinsPerTile_minmax;
    [Tooltip("The amount of collectible health on a tile will be between these two values")]
    public Vector2 amountOfHealthPerTile_minmax;
    [Tooltip("The amount of collectible levelUp on a tile will be between these two values")]
    public Vector2 amountOfLevelUpPerTile_minmax;

    [Header("Unique overrides")]
    [Tooltip("Does playing this chapter change the environment")]
    public EnvironmentType environmentChange = EnvironmentType.NO_CHANGE;
    [Tooltip("Does playing this chapter change the character style")]
    public CharacterStyleType characterStyleChange = CharacterStyleType.NO_CHANGE;
    [Tooltip("Does playing this chapter adds a hat on your head")]
    public HatType addHat = HatType.NONE;
    [Tooltip("Does playing this chapter adds a friend by your side")]
    public FriendType addFriend = FriendType.NONE;

    [Header("Waves of enemies")]
    [Tooltip("A series of waves happening during this chapter")]
    public List<Wave> waves;
}

