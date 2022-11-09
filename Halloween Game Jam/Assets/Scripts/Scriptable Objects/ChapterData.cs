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

