using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public enum SpawnPatternType
{
    RANDOM,
    CIRCLE,
    CHUNK
}

[System.Serializable]
[CreateAssetMenu(fileName = "Spawn Pattern Data", menuName = "ScriptableObjects/Froguelike/Spawn Pattern Data", order = 1)]
public class SpawnPattern : ScriptableObject
{
    public SpawnPatternType spawnPatternType;
    public int spawnAmount;
}
