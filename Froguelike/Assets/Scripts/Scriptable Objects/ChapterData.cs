using System.Collections;
using System.Collections.Generic;
using UnityEngine;


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

}

[System.Serializable]
[CreateAssetMenu(fileName = "Chapter Data", menuName = "ScriptableObjects/Froguelike/Chapter Data", order = 1)]
public class ChapterData : ScriptableObject
{
    // Unique identifier (no two chapters with same titel)
    public string chapterTitle;

    // In case we want to display more and more lore about this chapter as we go
    public string chapterDescription;

    public float chapterLengthInSeconds;
    [Space]
    public int backgroundStyle;
    [Range(0, 1)]
    public float amountOfRocks;
    [Range(0, 1)]
    public float amountOfPonds;
    [Space]
    public bool isCharacterGhost;
    [Space]
    public bool hasHat;
    public int hatStyle;
    [Space]
    public bool hasFriend;
    public int friendStyle;
    [Space]
    public Wave startingWave;
    [Space]
    public List<Wave> waves;
}

