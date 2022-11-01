using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
[CreateAssetMenu(fileName = "Chapter Data", menuName = "ScriptableObjects/Froguelike/Chapter Data", order = 1)]
public class ChapterData : ScriptableObject
{
    public string chapterTitle;
    public string chapterDescription;

    public float chapterLengthInSeconds;

    public int backgroundStyle;
    [Range(0, 1)]
    public float amountOfRocks;
    [Range(0, 1)]
    public float amountOfPonds;

    public bool isCharacterGhost;

    public bool hasHat;
    public int hatStyle;

    public bool hasFriend;
    public int friendStyle;

    public bool unlocksACharacter;
    public int unlockedCharacterIndex;

    public List<Froguelike_Wave> waves;
}

