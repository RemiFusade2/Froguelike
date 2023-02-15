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

[System.Serializable]
public enum HatType
{
    NONE,
    FANCY_HAT,
    FASHION_HAT,
    SUN_HAT
}

[System.Serializable]
public enum FriendType
{
    NONE,
    FROG,
    TOAD,
    GHOST,
    POISONOUS
}

[System.Serializable]
public enum EnvironmentType
{
    NO_CHANGE,
    SWAMP
}

[System.Serializable]
public enum CharacterStyleType
{
    NO_CHANGE,
    GHOST
}

[System.Serializable]
[CreateAssetMenu(fileName = "Chapter Data", menuName = "ScriptableObjects/Froguelike/Chapter Data", order = 1)]
public class ChapterData : ScriptableObject
{
    // Unique identifier (no two chapters with same titel)
    public string chapterTitle;

    // In case we want to display more and more lore about this chapter as we go
    [Tooltip("First element would be default description, other elements would be unlocked and displayed later.")]
    public List<string> chapterLore;

    // Should be a list of 3 icons (they can repeat)
    /*[Tooltip("Use 3 icons in that list. Icons can repeat.")]
    public List<ChapterIconType> icons;*/

    // Used for the timer when starting this chapter
    public float chapterLengthInSeconds = 300;

    // Is the chapter unlocked from the start of the game?
    public bool startingUnlockState = true;

    /*
    // Any stat change when playing this chapter
    public StatsWrapper startingStatBonuses;*/

    // Can this chapter still show up even after having been played once during this run?
    public bool canBePlayedMultipleTimesInOneRun = false;

    [Header("Obstacles settings")]
    public Vector2 amountOfRocksPerTile_minmax; // the amount of rocks on a tile will be between these two values
    public Vector2 amountOfPondsPerTile_minmax; // the amount of ponds on a tile will be between these two values

    [Header("Collectibles settings")]
    public Vector2 amountOfCoinsPerTile_minmax; // the amount of collectible coins on a tile will be between these two values
    public Vector2 amountOfHealthPerTile_minmax; // the amount of collectible health on a tile will be between these two values
    public Vector2 amountOfLevelUpPerTile_minmax; // the amount of collectible levelUp on a tile will be between these two values

    [Header("Unique overrides")]
    public EnvironmentType environmentChange = EnvironmentType.NO_CHANGE;
    public CharacterStyleType characterStyleChange = CharacterStyleType.NO_CHANGE;
    public HatType addHat = HatType.NONE;
    public FriendType addFriend = FriendType.NONE;

    [Header("Waves of enemies")]
    public List<Wave> waves; // a series of waves happening during this chapter
}

