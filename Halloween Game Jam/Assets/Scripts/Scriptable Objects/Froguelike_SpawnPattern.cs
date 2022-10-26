using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public enum Froguelike_SpawnPatternType
{
    RANDOM,
    CIRCLE,
    CHUNK
}

[System.Serializable]
[CreateAssetMenu(fileName = "Spawn Pattern Data", menuName = "ScriptableObjects/Froguelike/Spawn Pattern Data", order = 1)]
public class Froguelike_SpawnPattern : ScriptableObject
{
    public Froguelike_EnemyData enemySpawned;
    public Froguelike_SpawnPatternType spawnPatternType;
    public int spawnAmount;
}
