using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// All enemy types (1 type of enemy = 5 actual enemies for 5 difficulty tiers)
/// </summary>
[System.Serializable]
public enum EnemyType
{
    FLY,
    BUTTERFLY,
    PLANT,
    BEETLE,
    WASP,
    MOSQUITO,
    BIRD,
    WORM,
    CATERPILLAR,
    CRICKET,
    SNAIL,
    SLUG,
    DRAGONFLY,
    WATER_STRIDER,
    MOTH,
    ROACH,
    SPIDER,
    CRAYFISH,
    FISH,
    LIZARD,
    SNAKE,
    BAT,
    RODENT,
    WATER_PLANT,
    WEB,
    FIREFLY,
    ANT,

    GOLDEN_FLY
}

/// <summary>
/// All different movement patterns
/// </summary>
[System.Serializable]
public enum EnemyMovePatternType
{
    NO_MOVEMENT, // completely static
    STRAIGHT_LINE, // target the player when spawning, then follow that direction until death
    FOLLOW_PLAYER, // permanently update its movement to target the player
    DIRECTIONLESS, // follow a random direction that gets updated every second or so
    BOUNCE_ON_EDGES // movement is a straigth line (probably diagonal) then changes and bounces when it reaches an edge of the screen
}

/// <summary>
/// Structure to store both movement pattern and speed factor
/// </summary>
[System.Serializable]
public class EnemyMovePattern
{
    [Tooltip("The type of move pattern (follow player, straight line, etc.")]
    public EnemyMovePatternType movePatternType;
    [Tooltip("The speed factor, applied to the enemy default speed")]
    public float speedFactor;
    [Tooltip("In case of a bouncing pattern, how many times does it bounce before exiting the screen")]
    public int bouncecount;

    public EnemyMovePattern(EnemyMovePatternType movePatternType, float speedFactor, int bouncecount = 0)
    {
        this.movePatternType = movePatternType;
        this.speedFactor = speedFactor;
        this.bouncecount = bouncecount;
    }

    public EnemyMovePattern(EnemyMovePattern origin)
    {
        this.movePatternType = origin.movePatternType;
        this.speedFactor = origin.speedFactor;
        this.bouncecount = origin.bouncecount;
    }
}

/// <summary>
/// In case of a shape spawn, a list of possible shapes
/// </summary>
[System.Serializable]
public enum SpawnShape
{
    NONE,
    CIRCLE,
    HALF_CIRCLE
}

/// <summary>
/// All different types of spawn patterns
/// </summary>
[System.Serializable]
public enum SpawnPatternType
{
    RANDOM,
    SHAPE,
    CHUNK
}

/// <summary>
/// Describes a spawn pattern (one or multiple enemies, as a chunk or in a shape, etc.)
/// </summary>
[System.Serializable]
public class SpawnPattern
{
    //[Header("Spawn type")]
    [Tooltip("The type of spawn pattern (random, chunk, shape)")]
    public SpawnPatternType spawnPatternType = SpawnPatternType.RANDOM;
    [Tooltip("Only if the type is Shape, then which Shape? (optional)")]
    public SpawnShape spawnPatternShape = SpawnShape.NONE;

    //[Header("Multiple spawns")]
    [Tooltip("How many enemies will spawn at the same time")]
    public int spawnAmount = 1;
    [Tooltip("The delay between these spawns")]
    public float multipleSpawnDelay = 0;
}

/// <summary>
/// An EnemySpawn is an association giving an Enemy, a Move pattern, and a Spawn pattern.
/// A set of multiple EnemySpawns would describe a Wave
/// </summary>
[System.Serializable]
public class EnemySpawn
{
    [Tooltip("The type of enemy that would spawn (combined with its tier, we'd know what enemy spawns exactly)")]
    public EnemyType enemyType;

    [Tooltip("Formula that would give a value between 1 and 5. You can use the keyword \"chapter\" to adapt the enemy tier to the current chapter count.")]
    public string tierFormula;

    [Tooltip("Which movement pattern this enemy will follow")]
    public EnemyMovePattern movePattern;

    [Tooltip("Describe its spawn pattern")]
    public SpawnPattern spawnPattern;

    [Tooltip("Delay between spawns")]
    public float spawnCooldown;
}

/// <summary>
/// A Wave describes a set of enemies to spawn during a specified time. (deprecated: we won't use scriptable objects for waves anymore)
/// </summary>
[System.Serializable]
[CreateAssetMenu(fileName = "Wave Data", menuName = "ScriptableObjects/Froguelike/Wave Data", order = 1)]
public class Wave : ScriptableObject
{
    [Tooltip("Duration of that wave")]
    public float duration;

    [Tooltip("All information about the enemies to spawn")]
    public List<EnemySpawn> enemies;

    [Tooltip("Only used for preview")]
    public List<int> previewChapters;
}

/// <summary>
/// A Wave describes a set of enemies to spawn during a specified time.
/// </summary>
[System.Serializable]
public class WaveData
{
    [Tooltip("Duration of that wave")]
    public float duration;

    [Tooltip("All information about the enemies to spawn")]
    public List<EnemySpawn> enemies;

    [Tooltip("Only used for preview")]
    public List<int> previewChapters;
}
