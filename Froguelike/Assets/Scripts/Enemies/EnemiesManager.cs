using Rewired;
using Steamworks;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.Rendering.Universal;

/// <summary>
/// EnemyInfo describes an enemy in its current state.
/// It has a reference to EnemyData, the scriptable object that describes the enemy. This is not serialized with the rest.
/// It keeps the enemyName there for serialization. When saving/loading this enemy from a save file, the name will be used to retrieve the right enemy in the program.
/// The information that can change at runtime is:
/// - the total amount of these enemies that were eaten
/// </summary>
[System.Serializable]
public class EnemyInfo
{
    [System.NonSerialized]
    public EnemyData enemyData;

    // Defined at runtime, using enemyData
    [HideInInspector]
    public string enemyName;

    // All information about current state of the enemy
    public int totalEatenCount;

    public override bool Equals(object obj)
    {
        bool result = false;
        if (obj is EnemyInfo && !string.IsNullOrEmpty(enemyName))
        {
            result = enemyName.Equals((obj as EnemyInfo).enemyName);
        }
        return result;
    }

    public override int GetHashCode()
    {
        return enemyName.GetHashCode();
    }
}

/// <summary>
/// EnemiesSaveData contains all information that must be saved about the enemies.
/// - the total amount of enemies of this kind having been eaten
/// </summary>
[System.Serializable]
public class EnemiesSaveData : SaveData
{
    public List<EnemyInfo> enemiesList;

    public EnemiesSaveData()
    {
        Reset();
    }

    public override void Reset()
    {
        base.Reset();
        enemiesList = new List<EnemyInfo>();
    }
}

/// <summary>
/// EnemyTypeData associate a set of EnemyData to an EnemyType.
/// Each EnemyType has 5 difficulty tiers, meaning 5 different EnemyData
/// </summary>
[System.Serializable]
public class EnemyTypeData
{
    public EnemyType enemyType;
    public List<EnemyData> enemiesList;
}

/// <summary>
/// Describes an instance of an enemy, with all relevant data linked to it
/// </summary>
[System.Serializable]
public class EnemyInstance
{
    // Keyword that is used to find the EnemyData
    public string enemyName;
    public int enemyID;

    // References to EnemyInfo
    public EnemyInfo enemyInfo;

    // Bounty
    public BountyBug bountyBug;

    // Current in-game state
    public bool active;
    public bool alive;
    public float HP;
    public Vector2 moveDirection;
    public float lastUpdateTime;
    public float mass; // mass will change during knockback, freeze and curse and must be restored afterwards

    public float lastChangeOfDirectionTime; // for directionless enemies
    public int bounceCount; // for enemies that bounce on screen edges

    public float lastDamageInflictedTime; // last time this enemy inflicted damage to player

    // Move pattern
    public EnemyMovePattern movePattern;

    // Origin wave
    public Wave wave;
    public bool neverDespawn;

    // Difficulty tier
    public int difficultyTier;

    // Current in-game state - knockback & cooldown
    public float knockbackCooldown;

    // Current in-game state - poison
    public float poisonDamage;
    public float poisonRemainingTime;
    public float lastPoisonDamageTime;

    // Current in-game state - curse
    public float curseRemainingTime;
    public float freezeRemainingTime;

    // References to Components
    public Transform enemyTransform;
    public Rigidbody2D enemyRigidbody;
    public SpriteRenderer enemyRenderer;
    public Animator enemyAnimator;
    public Collider2D enemyCollider;
    public ParticleSystem enemyFreezeParticles;
    public ParticleSystem enemyPoisonParticles;
    public ParticleSystem enemyCurseParticles;

    // A link to the last weapon that hit this enemy
    public Transform lastWeaponHitTransform;

    public Coroutine removeOverlayCoroutine;

    public void RemoveOverlay()
    {
        enemyRenderer.material.SetFloat("_OverlayVisible", 0);
    }

    public void SetOverlayColor(Color newColor)
    {
        enemyRenderer.material.SetFloat("_OverlayVisible", 1);
        enemyRenderer.material.SetColor("_OverlayColor", newColor);
    }

    public void RemoveOutline()
    {
        enemyRenderer.material.SetFloat("_OutlineThickness", 0);
    }

    public void SetOutlineColor(Color newColor, int thickness = 1)
    {
        enemyRenderer.material.SetFloat("_OutlineThickness", thickness);
        enemyRenderer.material.SetColor("_OutlineColor", newColor);
    }

    public void Knockback(Vector2 direction, float strength, float duration)
    {
        enemyRigidbody.mass = 1000;
        enemyRigidbody.velocity = direction * strength;
        knockbackCooldown = duration;
    }

    public void StopKnockback()
    {
        if (enemyRigidbody.mass == 1000)
        {
            enemyRigidbody.mass = mass;
        }
    }

    public void ForceStopAllStatusEffects()
    {
        curseRemainingTime = 0;
        freezeRemainingTime = 0;
        poisonRemainingTime = 0;

        enemyCurseParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        enemyFreezeParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        enemyPoisonParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }
}

public class EnemiesManager : MonoBehaviour
{
    public static EnemiesManager instance;

    [Header("Settings - Logs")]
    public VerboseLevel verbose;

    [Header("References")]
    public Transform enemiesParent;
    public Transform damageTextsParent;

    [Header("Data")]
    public List<EnemyTypeData> enemiesTypesDataList;

    [Header("Prefabs")]
    public GameObject defaultEnemyPrefab;
    public GameObject damageTextPrefab;

    [Header("Settings - Spawn")]
    public float minSpawnDistanceFromPlayer = 15;
    public float maxSpawnDistanceFromPlayer = 17;
    [Space]
    public float spawnCenterMinDistanceToPlayer = 10;
    public float spawnCenterMaxDistanceToPlayer = 20;
    public float spawnCircleRadius = 20;
    [Space]
    public int findSpawnPositionMaxAttempts = 10;
    [Space]
    public float maxDistanceBeforeUnspawn = 40;
    public bool despawnEnemiesThatGoTooFar = false;

    [Header("Settings - Pooling")]
    public int maxDamageTexts = 300;
    public int maxActiveEnemies = 2000;

    [Header("Settings - Update")]
    public int updateAllEnemiesCount = 500;

    [Header("Settings - Damage texts")]
    public float damageTextLifespanInSeconds = 0.5f;
    [Space]
    public int damageTextFontSize_smol = 10;
    public float damageTextThreshold_smol = 0;
    public Color damageTextColor_smol;
    [Space]
    public int damageTextFontSize_medium = 10;
    public float damageTextThreshold_medium = 100;
    public Color damageTextColor_medium;
    [Space]
    public int damageTextFontSize_big = 10;
    public float damageTextThreshold_big = 500;
    public Color damageTextColor_big;
    [Space]
    public int damageTextFontSize_huge = 10;
    public float damageTextThreshold_huge = 1000;
    public Color damageTextColor_huge;
    [Space]
    public Color damageTextColor_vampire;
    public Color damageTextColor_poison;
    public Color damageTextColor_freeze;
    public Color damageTextColor_curse;

    [Header("Settings - Effects (poison, frozen, etc.)")]
    public Color poisonedOverlayColor;
    public Color frozenOverlayColor;
    public Color cursedOverlayColor;
    public Color vampireOverlayColor;
    public Color knockbackOverlayColor;
    public float delayBetweenPoisonDamage = 0.6f;

    [Header("Settings - Movement")]
    public float delayBetweenRandomChangeOfDirection = 1.5f;
    public bool enableAggroForRandomMovementBugs = true;
    public float aggroDistanceFromRandomMovementBugs = 5;
    [Space]
    public float knockbackDuration = 0.2f;

    [Header("Settings - Bounties")]
    public float bountyDefaultHealthMultiplier = 50;
    public float bountyDefaultDamageMultiplier = 2;
    [Space]
    public int bountyOutlineThickness = 1;
    public List<Color> bountyOutlineColorsList;

    [Header("Runtime")]
    public static int lastKey;

    [Header("Runtime - saved data")]
    public EnemiesSaveData enemiesData; // Will be loaded and saved when needed

    // private
    private List<float> lastSpawnTimesList;
    private Dictionary<string, EnemyData> enemiesDataFromNameDico;
    private Dictionary<EnemyType, List<EnemyData>> enemiesDataFromTypeDico;

    private Dictionary<int, EnemyInstance> allActiveEnemiesDico;
    private Queue<EnemyInstance> enemiesToUpdateQueue;

    private Queue<EnemyInstance> inactiveEnemiesPool;
    private Queue<GameObject> damageTextsPool;

    private HashSet<GameObject> visibleDamageTexts;

    private HashSet<int> spawnedBugsWithBountiesIDs;

    public const string pooledEnemyNameStr = "Pooled";

    // Global effects
    private bool applyGlobalFreeze = false;
    private bool applyGlobalPoison = false;
    private bool applyGlobalCurse = false;
    private Coroutine SetGlobalFreezeEffectCoroutine;
    private Coroutine SetGlobalPoisonEffectCoroutine;
    private Coroutine SetGlobalCurseEffectCoroutine;


    #region Unity Callback Methods

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(this);
        }
        else
        {
            Destroy(this.gameObject);
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        lastSpawnTimesList = new List<float>();

        // Initialize data structures
        spawnedBugsWithBountiesIDs = new HashSet<int>();
        enemiesDataFromNameDico = new Dictionary<string, EnemyData>();
        enemiesDataFromTypeDico = new Dictionary<EnemyType, List<EnemyData>>();
        foreach (EnemyTypeData enemyTypeData in enemiesTypesDataList)
        {
            enemiesDataFromTypeDico.Add(enemyTypeData.enemyType, enemyTypeData.enemiesList);
            foreach (EnemyData enemyData in enemyTypeData.enemiesList)
            {
                enemiesDataFromNameDico.Add(enemyData.enemyName, enemyData);
            }
        }

        lastKey = 1;

        allActiveEnemiesDico = new Dictionary<int, EnemyInstance>();
        enemiesToUpdateQueue = new Queue<EnemyInstance>();

        // Instantiate all enemies in game and put them in the pool
        inactiveEnemiesPool = new Queue<EnemyInstance>();
        for (int i = 0; i < maxActiveEnemies; i++)
        {
            EnemyInstance newEnemy = new EnemyInstance();
            newEnemy.active = false;
            newEnemy.alive = false;

            GameObject firstEnemyPrefab = defaultEnemyPrefab;
            GameObject enemyGameObject = Instantiate(firstEnemyPrefab, DataManager.instance.GetFarAwayPosition(), Quaternion.identity, enemiesParent);

            newEnemy.enemyTransform = enemyGameObject.transform;
            newEnemy.enemyRenderer = enemyGameObject.GetComponent<SpriteRenderer>();
            newEnemy.enemyRigidbody = enemyGameObject.GetComponent<Rigidbody2D>();
            newEnemy.enemyAnimator = enemyGameObject.GetComponent<Animator>();
            newEnemy.enemyCollider = enemyGameObject.GetComponent<Collider2D>();

            newEnemy.enemyFreezeParticles = newEnemy.enemyTransform.Find("Freeze Particles").GetComponent<ParticleSystem>();
            newEnemy.enemyPoisonParticles = newEnemy.enemyTransform.Find("Poison Particles").GetComponent<ParticleSystem>();
            newEnemy.enemyCurseParticles = newEnemy.enemyTransform.Find("Curse Particles").GetComponent<ParticleSystem>();

            PutEnemyInThePool(newEnemy);
        }

        // Instantiate all damage text in game and put them in the pool
        damageTextsPool = new Queue<GameObject>();
        visibleDamageTexts = new HashSet<GameObject>();
        for (int i = 0; i < maxDamageTexts; i++)
        {
            GameObject damageText = Instantiate(damageTextPrefab, DataManager.instance.GetFarAwayPosition(), Quaternion.identity, damageTextsParent);
            damageTextsPool.Enqueue(damageText);
        }
    }

    private void Update()
    {
        UpdateAllEnemies();
    }

    #endregion

    public bool IsGlobalFreezeActive()
    {
        return applyGlobalFreeze;
    }

    /// <summary>
    /// Returns a random spawn position around the player and in the direction of its movement.
    /// Prevent spawn on a rock or pond.
    /// </summary>
    /// <param name="playerPosition"></param>
    /// <param name="playerMoveDirection">This vector is either zero (player doesn't move) or normalized</param>
    /// <param name="spawnPosition">Out position, valid only if method returns true</param>
    /// <returns>Returns true if it successfully found a spawn position</returns>
    private bool GetSpawnPosition(Vector2 playerPosition, Vector2 playerMoveDirection, out Vector2 spawnPosition)
    {
        spawnPosition = Vector2.zero;

        // Will spawn in a circle around a position
        // Compute the center of that circle (player position + move direction * a distance)
        float spawnCenterDistanceToPlayer = (playerMoveDirection.Equals(Vector2.zero)) ? 0 : Random.Range(spawnCenterMinDistanceToPlayer, spawnCenterMaxDistanceToPlayer);
        Vector2 spawnCenter = playerPosition + playerMoveDirection * spawnCenterDistanceToPlayer;

        // Attemp to find a valid spawn point. Loop and try again until it works.
        bool spawnPositionIsValid = false;
        int loopAttemptCount = findSpawnPositionMaxAttempts;
        do
        {
            // Get a random point in the spawn circle
            spawnPosition = spawnCenter;
            spawnPosition += Random.insideUnitCircle * spawnCircleRadius;
            loopAttemptCount--;
            // Check if that random point is not in sight (you don't want to spawn an enemy where you can see it)
            if (Vector2.Distance(spawnPosition, playerPosition) > minSpawnDistanceFromPlayer)
            {
                spawnPositionIsValid = true;
            }
            // Check if that random point is on an obstacle
            int layerMask = LayerMask.GetMask("Rock", "LakeCollider");
            if (Physics2D.OverlapCircle(spawnPosition, 0.1f, layerMask) != null)
            {
                spawnPositionIsValid = false;
            }
        } while (!spawnPositionIsValid && loopAttemptCount > 0); // Redo until the random point is out of sight

        return spawnPositionIsValid;
    }

    private int GetFormulaValue(string formula, int chapterCount)
    {
        int result = 0;
        if (!formula.Contains('+') && !formula.Contains('-') && !formula.Contains('*') && !formula.Contains('/'))
        {
            // formula doesn't contain any operation symbol, so it's either a number of the keyord "chapter"
            if (formula.Equals("chapter"))
            {
                result = chapterCount;
            }
            else
            {
                result = int.Parse(formula);
            }
        }
        else
        {
            // Deal with additions
            string[] formulaElementsArray = formula.Split('+');
            if (formulaElementsArray.Length > 1)
            {
                foreach (string element in formulaElementsArray)
                {
                    result += GetFormulaValue(element, chapterCount);
                }
                return result;
            }

            // Deal with substractions
            formulaElementsArray = formula.Split('-');
            if (formulaElementsArray.Length > 1)
            {
                result = GetFormulaValue(formulaElementsArray[0], chapterCount);
                for (int i = 1; i < formulaElementsArray.Length; i++)
                {
                    string element = formulaElementsArray[i];
                    result -= GetFormulaValue(element, chapterCount);
                }
                return result;
            }

            // Deal with multiplications
            formulaElementsArray = formula.Split('*');
            if (formulaElementsArray.Length > 1)
            {
                result = GetFormulaValue(formulaElementsArray[0], chapterCount);
                for (int i = 1; i < formulaElementsArray.Length; i++)
                {
                    string element = formulaElementsArray[i];
                    result *= GetFormulaValue(element, chapterCount);
                }
                return result;
            }

            // Deal with divisions
            formulaElementsArray = formula.Split('/');
            if (formulaElementsArray.Length > 1)
            {
                result = GetFormulaValue(formulaElementsArray[0], chapterCount);
                for (int i = 1; i < formulaElementsArray.Length; i++)
                {
                    string element = formulaElementsArray[i];
                    result /= GetFormulaValue(element, chapterCount);
                }
                return result;
            }
        }
        return result;
    }

    private int GetTierFromFormulaAndChapterCount(string tierFormula, int chapterCount)
    {
        int formulaValue = GetFormulaValue(tierFormula, chapterCount);
        return Mathf.Clamp(formulaValue, 1, 5);
    }

    private EnemyData GetEnemyDataFromTypeAndDifficultyTier(EnemyType type, int difficultyTier)
    {
        EnemyData resultData = null;

        if (enemiesDataFromTypeDico.ContainsKey(type) && difficultyTier >= 1 && difficultyTier <= enemiesDataFromTypeDico[type].Count)
        {
            resultData = enemiesDataFromTypeDico[type][difficultyTier - 1];
        }

        return resultData;
    }

    /// <summary>
    /// Attempt to spawn a new "bounty" (ie: an enemy that doesn't despawn unless killed)
    /// </summary>
    /// <param name="bountyID">Unique identifier for this bounty in this chapter</param>
    /// <param name="bountyBugData">EnemyData contains data about the enemy to spawn</param>
    public void TrySpawnBounty(int bountyID, BountyBug bountyBug)
    {
        if (RunManager.instance.currentChapter != null && RunManager.instance.currentChapter.chapterData != null)
        {
            if (spawnedBugsWithBountiesIDs != null && !spawnedBugsWithBountiesIDs.Contains(bountyID))
            {
                // Keep a dictionary of current bounties
                spawnedBugsWithBountiesIDs.Add(bountyID);

                // Get random spawn position, around and in front of frog
                GetSpawnPosition(GameManager.instance.player.transform.position, GameManager.instance.player.GetMoveDirection(), out Vector2 spawnPosition);
                
                // Get EnemyData & prefab according to relevant difficulty tier
                int difficultyTier = GetTierFromFormulaAndChapterCount(bountyBug.tierFormula, RunManager.instance.GetChapterCount());
                EnemyData enemyData = GetEnemyDataFromTypeAndDifficultyTier(bountyBug.enemyType, difficultyTier);
                GameObject enemyPrefab = enemyData.prefab;

                // Spawn bug using info we have
                StartCoroutine(SpawnEnemyAsync(enemyPrefab, spawnPosition + Random.Range(-1.0f, 1.0f) * Vector2.right + Random.Range(-1.0f, 1.0f) * Vector2.up, enemyData, bountyBug.movePattern, originWave: null, delay: 0, difficultyTier: difficultyTier, neverDespawn: true, bounty: bountyBug));
                
            }
        }
    }


    public void TrySpawnWave(Wave currentWave)
    {
        if (RunManager.instance.currentChapter != null && RunManager.instance.currentChapter.chapterData != null)
        {
            double curseDelayFactor = (1 - GameManager.instance.player.curse / 2.0f); // curse will affect the delay negatively (lower delay = more spawns)
            curseDelayFactor = System.Math.Clamp(curseDelayFactor, 0.5, 2.0); // maximum curse would be half delay (so twice the amount of enemies). Negative curse is possible

            int enemyIndex = 0;
            for (int i = 0; i < currentWave.enemies.Count; i++)
            {
                // Pick a random spawn
                enemyIndex = Random.Range(0, currentWave.enemies.Count);

                EnemySpawn enemySpawn = currentWave.enemies[enemyIndex];
                float lastSpawnTime = lastSpawnTimesList[enemyIndex];

                // Get info about spawn delays
                double delayBetweenSpawns = enemySpawn.spawnCooldown * curseDelayFactor;
                delayBetweenSpawns = System.Math.Clamp(delayBetweenSpawns, 0.01, double.MaxValue);

                bool spawn = (Time.time - lastSpawnTime) > delayBetweenSpawns;

                if (spawn)
                {
                    // If spawn cooldown is over, then spawn!

                    // Get EnemyData & prefab according to relevant difficulty tier
                    int difficultyTier = GetTierFromFormulaAndChapterCount(enemySpawn.tierFormula, RunManager.instance.GetChapterCount());
                    EnemyData enemyData = GetEnemyDataFromTypeAndDifficultyTier(enemySpawn.enemyType, difficultyTier);
                    GameObject enemyPrefab = enemyData.prefab;

                    // Get spawn pattern info
                    SpawnPattern spawnPattern = enemySpawn.spawnPattern;
                    int enemyCount = spawnPattern.spawnAmount;
                    SpawnPatternType patternType = spawnPattern.spawnPatternType;
                    float delayBetweenSpawn = spawnPattern.multipleSpawnDelay;
                    float currentDelay = delayBetweenSpawn;

                    Vector2 spawnPosition;
                    switch (patternType)
                    {
                        case SpawnPatternType.CHUNK:
                            // choose a position at a distance from the player and spawn a chunk of enemies
                            if (GetSpawnPosition(GameManager.instance.player.transform.position, GameManager.instance.player.GetMoveDirection(), out spawnPosition))
                            {
                                for (int j = 0; j < enemyCount; j++)
                                {
                                    StartCoroutine(SpawnEnemyAsync(enemyPrefab, spawnPosition + Random.Range(-1.0f, 1.0f) * Vector2.right + Random.Range(-1.0f, 1.0f) * Vector2.up, enemyData, enemySpawn.movePattern, currentWave, currentDelay, difficultyTier));
                                    currentDelay += delayBetweenSpawn;
                                }
                            }
                            break;
                        case SpawnPatternType.SHAPE:
                            float arcAngle = 0;
                            // TODO: other shapes than circles
                            switch (spawnPattern.spawnPatternShape)
                            {
                                case SpawnShape.NONE:
                                case SpawnShape.CIRCLE:
                                    arcAngle = 360.0f;
                                    break;
                                case SpawnShape.HALF_CIRCLE:
                                    arcAngle = 180.0f;
                                    break;
                            }

                            // spawn enemies all around the player
                            float deltaAngle = arcAngle / enemyCount;
                            float spawnDistanceFromPlayer = minSpawnDistanceFromPlayer;
                            for (float angle = 0; angle < arcAngle; angle += deltaAngle)
                            {
                                spawnPosition = GameManager.instance.player.transform.position + (Mathf.Cos(angle * Mathf.Deg2Rad) * Vector3.right + Mathf.Sin(angle * Mathf.Deg2Rad) * Vector3.up) * spawnDistanceFromPlayer;
                                StartCoroutine(SpawnEnemyAsync(enemyPrefab, spawnPosition, enemyData, enemySpawn.movePattern, currentWave, currentDelay, difficultyTier));
                                currentDelay += delayBetweenSpawn;
                            }
                            break;

                        case SpawnPatternType.RANDOM:
                            // Spawn enemies at random positions
                            for (int j = 0; j < enemyCount; j++)
                            {
                                if (GetSpawnPosition(GameManager.instance.player.transform.position, GameManager.instance.player.GetMoveDirection(), out spawnPosition))
                                {
                                    StartCoroutine(SpawnEnemyAsync(enemyPrefab, spawnPosition, enemyData, enemySpawn.movePattern, currentWave, currentDelay, difficultyTier));
                                    currentDelay += delayBetweenSpawn;
                                }
                            }
                            break;
                    }
                    lastSpawnTimesList[enemyIndex] = Time.time;
                }

                enemyIndex++;
            }
        }
    }

    public void SpawnEnemy(GameObject prefab, Vector3 position, EnemyData enemyData, EnemyMovePattern movePattern, Wave originWave, int difficultyTier, bool neverDespawn = false, BountyBug bounty = null, bool forceMovementDirection = false, Vector2? moveDirection = null)
    {
        // Get an enemy from the pool
        EnemyInstance enemyFromPool = null;

        if (inactiveEnemiesPool.TryDequeue(out enemyFromPool))
        {
            // Set all components values to prefab values
            enemyFromPool.enemyTransform.localScale = prefab.GetComponent<Transform>().localScale;

            enemyFromPool.enemyCollider.GetComponent<CircleCollider2D>().radius = prefab.GetComponent<CircleCollider2D>().radius;

            enemyFromPool.enemyRigidbody.mass = prefab.GetComponent<Rigidbody2D>().mass;
            enemyFromPool.mass = prefab.GetComponent<Rigidbody2D>().mass;

            enemyFromPool.enemyAnimator.runtimeAnimatorController = prefab.GetComponent<Animator>().runtimeAnimatorController;
            enemyFromPool.enemyAnimator.SetBool("IsDead", false);

            enemyFromPool.enemyRenderer.sprite = prefab.GetComponent<SpriteRenderer>().sprite;
            enemyFromPool.enemyRenderer.color = prefab.GetComponent<SpriteRenderer>().color;
            enemyFromPool.enemyRenderer.sortingOrder = prefab.GetComponent<SpriteRenderer>().sortingOrder;
            enemyFromPool.enemyRenderer.sortingLayerID = prefab.GetComponent<SpriteRenderer>().sortingLayerID;

            enemyFromPool.enemyTransform.gameObject.layer = prefab.gameObject.layer;

            enemyFromPool.enemyAnimator.enabled = true;
            enemyFromPool.enemyCollider.enabled = true;
            enemyFromPool.enemyRenderer.enabled = true;
            enemyFromPool.enemyRigidbody.simulated = true;

            enemyFromPool.enemyTransform.position = position;

            // Set random orientation
            enemyFromPool.enemyTransform.rotation = Quaternion.Euler(0, 0, Random.Range(0,4) * 90);

            enemyFromPool.wave = originWave;
            enemyFromPool.difficultyTier = difficultyTier;
            enemyFromPool.neverDespawn = neverDespawn;

            int outlineThickness = enemyData.outlineThickness;
            Color outlineColor = enemyData.outlineColor;
            if (bounty != null)
            {
                outlineThickness = bountyOutlineThickness;
                outlineColor = bountyOutlineColorsList[0];
            }

            if (outlineThickness > 0)
            {
                enemyFromPool.SetOutlineColor(outlineColor, outlineThickness);
            }
            else
            {
                enemyFromPool.RemoveOutline();
            }

            if (verbose == VerboseLevel.MAXIMAL)
            {
                Debug.Log($"Spawning a {enemyData.enemyName} with move pattern {movePattern.movePatternType.ToString()}. Total amount of active enemies = {allActiveEnemiesDico.Count + 1}");
            }

            AddEnemy(enemyFromPool, enemyData, movePattern, bounty);

            if (forceMovementDirection && moveDirection.HasValue)
            {
                enemyFromPool.moveDirection = moveDirection.Value;
                SetEnemyVelocity(enemyFromPool, 0);
            }
        }
    }

    public void CheatSpawnRandomBugs(int count, int tier = 0, float speed = 1)
    {
        // Prepare spawn pattern (follow player)
        EnemyMovePattern movePatternFollowPlayer = new EnemyMovePattern(EnemyMovePatternType.FOLLOW_PLAYER, speedFactor: speed);

        EnemyType randomEnemyType = EnemyType.FLY;
        int difficultyTier = 1;
        EnemyData enemyData;
        GameObject enemyPrefab;
        for (int i = 0; i < count; i++)
        {
            // Pick random type of bug
            do {
                randomEnemyType = (EnemyType)(Random.Range(0, 6));
            } while (randomEnemyType == EnemyType.PLANT);
            if (tier <= 0)
            {
                difficultyTier = Random.Range(1, 6);
            }
            else
            {
                difficultyTier = tier;
            }
            enemyData = GetEnemyDataFromTypeAndDifficultyTier(randomEnemyType, difficultyTier);
            enemyPrefab = enemyData.prefab;

            // Spawn bug
            if (GetSpawnPosition(GameManager.instance.player.transform.position, GameManager.instance.player.GetMoveDirection(), out Vector2 spawnPosition))
            {
                StartCoroutine(SpawnEnemyAsync(enemyPrefab, spawnPosition, enemyData, movePatternFollowPlayer, originWave: null, delay: 0, difficultyTier: difficultyTier));
            }
        }
    }

    private IEnumerator SpawnEnemyAsync(GameObject prefab, Vector3 position, EnemyData enemyData, EnemyMovePattern movePattern, Wave originWave, float delay, int difficultyTier, bool neverDespawn = false, BountyBug bounty = null, bool forceMovementDirection = false, Vector2? moveDirection = null)
    {
        yield return new WaitForSeconds(delay);
        SpawnEnemy(prefab, position, enemyData, movePattern, originWave, difficultyTier, neverDespawn, bounty, forceMovementDirection, moveDirection);
    }

    public void InitializeWave(Wave wave)
    {
        lastSpawnTimesList.Clear();
        float time = float.MinValue; // or Time.time;
        foreach (EnemySpawn enemySpawn in wave.enemies)
        {
            lastSpawnTimesList.Add(time);
        }
    }

    public EnemyInstance GetEnemyInstanceFromID(int ID)
    {
        EnemyInstance result = null;
        if (allActiveEnemiesDico.ContainsKey(ID))
        {
            result = allActiveEnemiesDico[ID];
        }
        return result;
    }
    public EnemyInstance GetEnemyInstanceFromGameObjectName(string gameObjectName)
    {
        if (gameObjectName.Equals(pooledEnemyNameStr))
            return null;
        int ID = int.Parse(gameObjectName);
        return GetEnemyInstanceFromID(ID);
    }

    public EnemyData GetEnemyDataFromGameObjectName(string gameObjectName, out BountyBug bountyBug)
    {
        int ID = int.Parse(gameObjectName);
        EnemyInstance enemyInstance = GetEnemyInstanceFromID(ID);
        bountyBug = enemyInstance.bountyBug; // Get potential bounty
        return enemiesDataFromNameDico[enemyInstance.enemyName];
    }

    public void AddEnemy(EnemyInstance newEnemy, EnemyData enemyData, EnemyMovePattern movePattern, BountyBug bounty = null)
    {
        // setup enemy - name
        newEnemy.enemyName = enemyData.enemyName;
        lastKey++;
        newEnemy.enemyTransform.gameObject.name = lastKey.ToString();

        // add enemy to dico
        newEnemy.enemyID = lastKey;
        allActiveEnemiesDico.Add(lastKey, newEnemy);

        // Bounty multipliers
        float hpMultiplier = 1;
        if (bounty != null)
        {
            if (bounty.overrideHealthMultiplier)
            {
                hpMultiplier = bounty.healthMultiplier;
            }
            else
            {
                hpMultiplier = bountyDefaultHealthMultiplier;
            }

            // add bounty to dico
            newEnemy.bountyBug = bounty;
        }

        // setup enemy - state
        newEnemy.HP = (enemyData.maxHP * hpMultiplier) * (1 + GameManager.instance.player.curse); // Max HP is affected by the curse
        newEnemy.active = true;
        newEnemy.alive = true;

        // setup enemy - misc
        newEnemy.movePattern = movePattern;

        // setup enemy - enemy info
        EnemyInfo enemyInfo = enemiesData.enemiesList.FirstOrDefault(x => x.enemyName.Equals(enemyData.enemyName));
        if (enemyInfo == null)
        {
            Debug.LogWarning("Initializing enemy instance but EnemyInfo is null!");
        }
        newEnemy.enemyInfo = enemyInfo;

        // setup enemy - data
        newEnemy.enemyInfo.enemyData = enemyData;

        // add enemy to update queue
        enemiesToUpdateQueue.Enqueue(newEnemy);

        // Set starting velocity (depends on move pattern)
        Vector2 vectorTowardsPlayer = (GameManager.instance.player.transform.position - newEnemy.enemyTransform.position).normalized;
        switch (movePattern.movePatternType)
        {
            case EnemyMovePatternType.BOUNCE_ON_EDGES: // diagonal 45° movement, somewhat towards player
                // Special move pattern where this enemy should not interact with anything, and would be displayed on top of everything
                newEnemy.enemyCollider.isTrigger = true;
                newEnemy.enemyRenderer.sortingOrder = 1000;
                // Always follow a diagonal
                newEnemy.moveDirection = (new Vector2(Mathf.Sign(vectorTowardsPlayer.x), Mathf.Sign(vectorTowardsPlayer.y))).normalized;
                newEnemy.bounceCount = movePattern.bouncecount;
                break;
            case EnemyMovePatternType.DIRECTIONLESS: // starts static
            case EnemyMovePatternType.NO_MOVEMENT:
                newEnemy.enemyCollider.isTrigger = false;
                newEnemy.moveDirection = Vector2.zero;
                break;
            case EnemyMovePatternType.FOLLOW_PLAYER:
                newEnemy.enemyCollider.isTrigger = false;
                newEnemy.moveDirection = vectorTowardsPlayer;
                break;
            case EnemyMovePatternType.STRAIGHT_LINE: // but still moving towards player
                newEnemy.enemyCollider.isTrigger = true;
                newEnemy.moveDirection = vectorTowardsPlayer;
                break;
        }
        SetEnemyVelocity(newEnemy, 0);
    }

    /// <summary>
    /// Inflict damage to the enemy with this index value. Set it to "dead" if its HP is lower than zero.
    /// </summary>
    /// <param name="enemyIndex"></param>
    /// <param name="damage"></param>
    /// <param name="weapon"></param>
    /// <returns></returns>
    public bool DamageEnemy(int enemyIndex, float damage, Transform weapon, bool applyVampireEffect = false, bool applyFreezeEffect = false, bool applyCurse = false, bool poisonSource = false)
    {
        EnemyInstance enemy = allActiveEnemiesDico[enemyIndex];

        float randomizedDamage = damage * Random.Range(0.9f, 1.1f);
        enemy.HP -= randomizedDamage;

        bool knockback = false;
        bool vampireEffect = false;

        float visualDamageAmount = randomizedDamage * 10;
        int visualDamageAmountInt = Mathf.RoundToInt(visualDamageAmount);

        // Display damage text
        GameObject damageText = null;
        if ((visualDamageAmountInt > 0 || applyCurse) && damageTextsPool.TryDequeue(out damageText))
        {
            Vector2 position = (Vector2)enemy.enemyTransform.position + 0.1f * Random.insideUnitCircle;
            damageText.transform.position = position;
            TMPro.TextMeshPro damageTMPScript = damageText.GetComponent<TMPro.TextMeshPro>();

            string damageAmountStr = visualDamageAmountInt.ToString();

            // Todo: Fractions!
            /*
            int randomMultiplier = Random.Range(2, 9);
            damageAmountStr =  $"{visualDamageAmountInt * randomMultiplier}\n-\n{randomMultiplier+1}";*/

            damageTMPScript.text = damageAmountStr;
            float fontSize = 10;
            if (poisonSource)
            {
                // poison damage
                damageTMPScript.color = damageTextColor_poison;
            }
            else if (visualDamageAmount < damageTextThreshold_medium)
            {
                // Smol text
                fontSize = damageTextFontSize_smol;
                damageTMPScript.color = damageTextColor_smol;
            }
            else if (visualDamageAmount < damageTextThreshold_big)
            {
                // Medium text
                fontSize = damageTextFontSize_medium;
                damageTMPScript.color = damageTextColor_medium;
            }
            else if (visualDamageAmount < damageTextThreshold_huge)
            {
                // Big text
                fontSize = damageTextFontSize_big;
                damageTMPScript.color = damageTextColor_big;
            }
            else
            {
                // Huge text
                fontSize = damageTextFontSize_huge;
                damageTMPScript.color = damageTextColor_huge;
            }
            if (applyVampireEffect)
            {
                damageTMPScript.color = damageTextColor_vampire;
            }
            else if (applyFreezeEffect)
            {
                damageTMPScript.color = damageTextColor_freeze;
            }
            else if (applyCurse)
            {
                damageTMPScript.text = "#";
                damageAmountStr = "#";
                damageTMPScript.color = damageTextColor_curse;
            }
            damageTMPScript.fontSize = fontSize;

            int sortedOrderLayer = Random.Range(100, 200);
            damageTMPScript.sortingOrder = sortedOrderLayer+1;

            // Todo: implement a proper outline for damage text and remove this shit
            foreach (Transform child in damageText.transform)
            {
                child.GetComponent<TMPro.TextMeshPro>().text = damageAmountStr;
                child.GetComponent<TMPro.TextMeshPro>().fontSize = fontSize;
                child.GetComponent<TMPro.TextMeshPro>().sortingOrder = sortedOrderLayer;
            }

            damageText.GetComponent<MeshRenderer>().enabled = true;
            damageText.GetComponent<Rigidbody2D>().simulated = true;
            damageText.GetComponent<Rigidbody2D>().velocity = Vector2.up;
            if (applyVampireEffect)
            {
                // damage is not null and vampire effect is ON
                vampireEffect = true;
                damageText.GetComponent<ParticleSystem>().Play();
            }
            visibleDamageTexts.Add(damageText);
            StartCoroutine(PutDamageTextIntoPoolAsync(damageText, damageTextLifespanInSeconds));
        }

        // Bunty outline color
        if (enemy.bountyBug != null && bountyOutlineThickness > 0)
        {
            float enemyMaxHP = enemy.enemyInfo.enemyData.maxHP;
            if (enemy.bountyBug.overrideHealthMultiplier)
            {
                enemyMaxHP *= enemy.bountyBug.healthMultiplier;
            }
            else
            {
                enemyMaxHP *= bountyDefaultHealthMultiplier;
            }

            int outlineColorIndex = Mathf.FloorToInt(((enemyMaxHP-enemy.HP) / enemyMaxHP) * bountyOutlineColorsList.Count);
            outlineColorIndex = (outlineColorIndex >= bountyOutlineColorsList.Count) ? (bountyOutlineColorsList.Count - 1) : outlineColorIndex;
            Color bountyOutlineColor = bountyOutlineColorsList[outlineColorIndex];
            enemy.SetOutlineColor(bountyOutlineColor, bountyOutlineThickness);
        }

        bool enemyDied = false;
        if (enemy.HP < 0.01f)
        {
            // enemy died, let's eat it now
            SetEnemyDead(enemy, despawnEnemyAndSpawnXP: true);
            enemyDied = true;
        }

        if (weapon != null)
        {
            enemy.lastWeaponHitTransform = weapon;

            if (!enemyDied && enemy.knockbackCooldown <= 0)
            {
                Vector2 knockbackDirection = (enemy.enemyTransform.position - weapon.position).normalized;
                float knockbackStrengthMassRatio = Mathf.Clamp(enemy.enemyRigidbody.mass/8, 1, 20);
                float knockbackForce = weapon.GetComponent<WeaponBehaviour>().knockbackForce / knockbackStrengthMassRatio;

                enemy.Knockback(knockbackDirection, knockbackForce, knockbackDuration);
                knockback = true;
            }
        }

        if (vampireEffect)
        {
            SetOverlayColor(enemy, vampireOverlayColor, 0.3f);
        }
        else if (knockback)
        {
            SetOverlayColor(enemy, knockbackOverlayColor, 0.3f);
        }

        return enemyDied;
    }

    private IEnumerator PutDamageTextIntoPoolAsync(GameObject damageText, float delay)
    {
        yield return new WaitForSeconds(delay);
        HideDamageText(damageText);
        if (visibleDamageTexts.Contains(damageText))
        {
            visibleDamageTexts.Remove(damageText);
            damageTextsPool.Enqueue(damageText);
        }
    }

    private void HideDamageText(GameObject damageText)
    {
        damageText.GetComponent<MeshRenderer>().enabled = false;
        damageText.GetComponent<Rigidbody2D>().simulated = false;
        damageText.transform.position = DataManager.instance.GetFarAwayPosition();
    }

    // Return true if enemy dieded
    public bool DamageEnemy(string enemyGoName, float damage, Transform weapon, bool applyVampireEffect = false, bool applyFreezeEffect = false, bool applyCurse = false, bool poisonSource = false)
    {
        int index = int.Parse(enemyGoName);
        return DamageEnemy(index, damage, weapon, applyVampireEffect, applyFreezeEffect, applyCurse, poisonSource);
    }

    public void ClearAllDamageTexts()
    {
        foreach (GameObject damageTextGameObject in visibleDamageTexts)
        {
            HideDamageText(damageTextGameObject);
        }
    }

    public void ClearAllEnemies(bool ignoreBounties = false)
    {
        List<int> enemiesToDestroyIDList = new List<int>();
        Queue<EnemyInstance> remainingEnemiesQueue = new Queue<EnemyInstance>();
        foreach (KeyValuePair<int, EnemyInstance> enemyInfo in allActiveEnemiesDico)
        {
            if (!ignoreBounties || enemyInfo.Value.bountyBug == null)
            {
                enemiesToDestroyIDList.Add(enemyInfo.Key);
            }
            else
            {
                remainingEnemiesQueue.Enqueue(enemyInfo.Value);
            }
        }
        foreach (int id in enemiesToDestroyIDList)
        {
            // do not destroy the enemy gameobject but instead deactivate it and put it back in the pool
            PutEnemyInThePool(allActiveEnemiesDico[id]);
            allActiveEnemiesDico.Remove(id);
        }
        enemiesToUpdateQueue.Clear();
        while (remainingEnemiesQueue.TryDequeue(out EnemyInstance enemy))
        {
            enemiesToUpdateQueue.Enqueue(enemy);
        }
        //allActiveEnemiesDico.Clear();

        if (!ignoreBounties)
        {
            spawnedBugsWithBountiesIDs.Clear();
        }
    }

    /// <summary>
    /// Update enemies
    /// </summary>
    public void UpdateAllEnemies()
    {
        if (GameManager.instance.isGameRunning)
        {
            int maxEnemiesToUpdateCount = updateAllEnemiesCount;
            Transform playerTransform = GameManager.instance.player.transform;

            List<int> enemiesToDestroyIDList = new List<int>();

            int enemiesToUpdateCount = Mathf.Min(maxEnemiesToUpdateCount, enemiesToUpdateQueue.Count);
            for (int i = 0; i < enemiesToUpdateCount; i++)
            {
                EnemyInstance enemy = enemiesToUpdateQueue.Dequeue();                
                if (enemy.active)
                {
                    EnemyData enemyData = enemiesDataFromNameDico[enemy.enemyName];
                    EnemyMovePattern movePattern = enemy.movePattern;

                    float enemyUpdateDeltaTime = (Time.time - enemy.lastUpdateTime);
                    Vector3 frogPosition = playerTransform.position;
                    if (!enemy.alive)
                    {
                        // enemy is dead
                        enemy.poisonRemainingTime = 0;
                        enemy.poisonDamage = 0;
                        enemy.lastPoisonDamageTime = float.MinValue;
                        enemy.freezeRemainingTime = 0;
                        enemy.curseRemainingTime = 0;
                        UpdateStatusParticles(enemy);
                        if (enemy.lastWeaponHitTransform != null)
                        {
                            frogPosition = enemy.lastWeaponHitTransform.position;
                        }
                        float dot = Vector2.Dot(enemy.moveDirection, (frogPosition - enemy.enemyTransform.position).normalized);
                        float distanceWithFrog = Vector2.Distance(frogPosition, enemy.enemyTransform.position);
                        enemy.moveDirection = (frogPosition - enemy.enemyTransform.position).normalized;
                        float walkSpeed = DataManager.instance.defaultWalkSpeed * (1 + GameManager.instance.player.GetWalkSpeedBoost());
                        enemy.enemyRigidbody.velocity = 2 * enemy.moveDirection * walkSpeed;
                        if (dot < 0 || distanceWithFrog < 1.5f)
                        {
                            enemy.enemyRenderer.enabled = false;
                            enemy.active = false;

                            // Spawn XP
                            float XPEarned = enemyData.xPBonus;
                            if (enemy.bountyBug != null)
                            {
                                XPEarned *= enemy.bountyBug.xpMultiplier;
                            }
                            XPEarned *= (1 + GameManager.instance.player.curse);

                            RunManager.instance.EatEnemy(XPEarned);
                            enemiesToDestroyIDList.Add(enemy.enemyID);
                        }
                    }
                    else
                    {
                        // Enemy is alive

                        // Check distance (if enemy is too far, unspawn it)
                        float distanceWithFrog = Vector2.Distance(frogPosition, enemy.enemyTransform.position);
                        if (distanceWithFrog > maxDistanceBeforeUnspawn)
                        {
                            // Enemy is too far, either it is despawned or it is immediately move and spawned again
                            bool despawnEnemy = despawnEnemiesThatGoTooFar; // Despawn enemies? (an option in the settings)
                            despawnEnemy |= (enemy.wave != null && !enemy.wave.Equals(RunManager.instance.GetCurrentWave())); // Enemy despawn if it is not part of current wave
                            despawnEnemy |= (enemy.movePattern.movePatternType == EnemyMovePatternType.STRAIGHT_LINE); // Enemy despawn if it goes in a straight line
                            despawnEnemy |= (enemy.movePattern.movePatternType == EnemyMovePatternType.BOUNCE_ON_EDGES); // Enemy despawn if it bounces on edges

                            // Special case: this enemy never despawn (maybe it is a bounty)
                            despawnEnemy &= !enemy.neverDespawn;

                            if (despawnEnemy)
                            {
                                // Unspawn enemy
                                enemy.enemyRenderer.enabled = false;
                                enemy.enemyCollider.enabled = false;
                                enemy.active = false;
                                enemiesToDestroyIDList.Add(enemy.enemyID);
                            }
                            else
                            {
                                // Place enemy in front of frog (as if it was spawned again)
                                if (GetSpawnPosition(GameManager.instance.player.transform.position, GameManager.instance.player.GetMoveDirection(), out Vector2 spawnPosition))
                                {
                                    // Teleport
                                    enemy.enemyTransform.position = spawnPosition;

                                    // Reset status effects
                                    enemy.ForceStopAllStatusEffects();
                                }

                                // Make sure its direction is reset
                                switch(enemy.movePattern.movePatternType)
                                {
                                    case EnemyMovePatternType.STRAIGHT_LINE:
                                        enemy.moveDirection = (playerTransform.position - enemy.enemyTransform.position).normalized;
                                        SetEnemyVelocity(enemy, enemyUpdateDeltaTime);
                                        break;
                                    case EnemyMovePatternType.BOUNCE_ON_EDGES:
                                        enemy.moveDirection = (playerTransform.position - enemy.enemyTransform.position).normalized;
                                        enemy.moveDirection = (new Vector2(Mathf.Sign(enemy.moveDirection.x), Mathf.Sign(enemy.moveDirection.y))).normalized;
                                        enemy.bounceCount = enemy.movePattern.bouncecount;
                                        SetEnemyVelocity(enemy, enemyUpdateDeltaTime);
                                        break;
                                    default:
                                        // Velocity is going to be updated anyway
                                        break;
                                }
                            }
                        }
                        else
                        {
                            if (enableAggroForRandomMovementBugs && enemy.movePattern.movePatternType == EnemyMovePatternType.DIRECTIONLESS)
                            {
                                if (distanceWithFrog <= aggroDistanceFromRandomMovementBugs)
                                {
                                    // Change move pattern from DIRECTIONLESS to FOLLOW_PLAYER
                                    enemy.movePattern = new EnemyMovePattern(enemy.movePattern);
                                    enemy.movePattern.movePatternType = EnemyMovePatternType.FOLLOW_PLAYER;
                                    enemy.movePattern.speedFactor *= 1.5f;
                                }
                            }

                            // Move 
                            if (enemy.knockbackCooldown > 0)
                            {
                                // being knocked back
                                enemy.knockbackCooldown -= enemyUpdateDeltaTime;
                            }
                            else
                            {
                                // Knockback effect is over
                                enemy.StopKnockback(); 
                                enemy.knockbackCooldown = 0;

                                // Freeze and Curse remaining time decreases
                                enemy.freezeRemainingTime -= enemyUpdateDeltaTime;
                                enemy.curseRemainingTime -= enemyUpdateDeltaTime;

                                // Enemy get its color back (depending on current effect applied)
                                UpdateStatusParticles(enemy);

                                switch (movePattern.movePatternType)
                                {
                                    case EnemyMovePatternType.NO_MOVEMENT:
                                        enemy.enemyRigidbody.velocity = Vector2.zero;
                                        break;
                                    case EnemyMovePatternType.STRAIGHT_LINE:
                                        SetEnemyVelocity(enemy, enemyUpdateDeltaTime);
                                        break;
                                    case EnemyMovePatternType.FOLLOW_PLAYER:
                                        enemy.moveDirection = (playerTransform.position - enemy.enemyTransform.position).normalized;
                                        SetEnemyVelocity(enemy, enemyUpdateDeltaTime);
                                        break;
                                    case EnemyMovePatternType.BOUNCE_ON_EDGES:
                                        if (enemy.bounceCount > 0)
                                        {
                                            // detect if enemy current position is on an edge of the screen, bounce it in the right direction if it is                                
                                            Vector3 cameraBottomLeftCorner = Camera.main.ScreenToWorldPoint(20 * Vector3.right + 20 * Vector3.up);
                                            Vector3 cameraTopRightCorner = Camera.main.ScreenToWorldPoint((Camera.main.GetComponent<PixelPerfectCamera>().refResolutionX - 20) * Vector3.right + (Camera.main.GetComponent<PixelPerfectCamera>().refResolutionY - 50) * Vector3.up);
                                            if (enemy.moveDirection.x > 0 && (enemy.enemyTransform.position.x > cameraTopRightCorner.x))
                                            {
                                                // move to the left
                                                enemy.moveDirection = (new Vector2(-Mathf.Abs(enemy.moveDirection.x), enemy.moveDirection.y)).normalized;
                                                enemy.bounceCount--;
                                            }
                                            if (enemy.moveDirection.x < 0 && (enemy.enemyTransform.position.x < cameraBottomLeftCorner.x))
                                            {
                                                // move to the right
                                                enemy.moveDirection = (new Vector2(Mathf.Abs(enemy.moveDirection.x), enemy.moveDirection.y)).normalized;
                                                enemy.bounceCount--;
                                            }
                                            if (enemy.moveDirection.y < 0 && (enemy.enemyTransform.position.y < cameraBottomLeftCorner.y))
                                            {
                                                // move up
                                                enemy.moveDirection = (new Vector2(enemy.moveDirection.x, Mathf.Abs(enemy.moveDirection.y))).normalized;
                                                enemy.bounceCount--;
                                            }
                                            if (enemy.moveDirection.y > 0 && (enemy.enemyTransform.position.y > cameraTopRightCorner.y))
                                            {
                                                // move down
                                                enemy.moveDirection = (new Vector2(enemy.moveDirection.x, -Mathf.Abs(enemy.moveDirection.y))).normalized;
                                                enemy.bounceCount--;
                                            }
                                        }
                                        SetEnemyVelocity(enemy, enemyUpdateDeltaTime);
                                        break;
                                    case EnemyMovePatternType.DIRECTIONLESS:
                                        if (Time.time - enemy.lastChangeOfDirectionTime > delayBetweenRandomChangeOfDirection)
                                        {
                                            enemy.moveDirection = Random.insideUnitCircle.normalized;
                                            enemy.lastChangeOfDirectionTime = Time.time;
                                        }
                                        SetEnemyVelocity(enemy, enemyUpdateDeltaTime);
                                        break;
                                }
                            }

                            // Poison Damage
                            enemy.poisonRemainingTime -= enemyUpdateDeltaTime;
                            if (enemy.poisonRemainingTime <= 0 && !applyGlobalPoison)
                            {
                                // No poison effect
                                enemy.poisonRemainingTime = 0;
                                enemy.poisonDamage = 0;
                                enemy.lastPoisonDamageTime = float.MinValue;
                                UpdateStatusParticles(enemy);
                            }
                            else if (Time.time - enemy.lastPoisonDamageTime > delayBetweenPoisonDamage)
                            {
                                // Poison effect is on, and cooldown since last poison damage is over
                                float poisonDamage = enemy.poisonDamage;
                                if (applyGlobalPoison)
                                    poisonDamage = 0.1f;
                                bool enemyIsDead = DamageEnemy(enemy.enemyID, poisonDamage, null, poisonSource: true);
                                enemy.lastPoisonDamageTime = Time.time;

                                /*
                                if (enemyIsDead && !enemiesToDestroyIDList.Contains(enemy.enemyID))
                                {
                                    Something wrong here
                                    enemiesToDestroyIDList.Add(enemy.enemyID);
                                    enemy.active = false;
                                    CollectiblesManager.instance.SpawnCollectible(enemy.enemyTransform.position, CollectibleType.XP_BONUS, enemyData.xPBonus);
                                }*/
                            }
                        }
                    }
                }
                if (enemy.active)
                {
                    enemy.lastUpdateTime = Time.time;
                    enemiesToUpdateQueue.Enqueue(enemy);
                }
            }
            if (verbose == VerboseLevel.MAXIMAL && enemiesToDestroyIDList.Count > 0)
            {
                Debug.Log($"Destroying {enemiesToDestroyIDList.Count} enemies. Total amount of active enemies = {allActiveEnemiesDico.Count - enemiesToDestroyIDList.Count}");
            }
            foreach (int enemyID in enemiesToDestroyIDList)
            {
                if (allActiveEnemiesDico.ContainsKey(enemyID))
                {
                    EnemyInstance enemy = allActiveEnemiesDico[enemyID];
                    PutEnemyInThePool(enemy);
                    allActiveEnemiesDico.Remove(enemyID);
                }
            }
        }
    }

    private void PutEnemyInThePool(EnemyInstance enemy)
    {
        inactiveEnemiesPool.Enqueue(enemy);
        enemy.ForceStopAllStatusEffects();
        enemy.HP = 10000;
        enemy.poisonDamage = 0;
        enemy.enemyTransform.name = pooledEnemyNameStr;
        enemy.active = false;
        enemy.enemyTransform.position = DataManager.instance.GetFarAwayPosition();
        enemy.enemyRenderer.enabled = false;
        enemy.enemyRigidbody.simulated = false;
        enemy.enemyAnimator.enabled = false;
        enemy.enemyCollider.enabled = false;
        enemy.bountyBug = null;

    }

    public void AddPoisonDamageToEnemy(string enemyGoName, float poisonDamage, float poisonDuration)
    {
        int enemyIndex = int.Parse(enemyGoName);
        if (allActiveEnemiesDico.ContainsKey(enemyIndex))
        {
            EnemyInstance enemy = allActiveEnemiesDico[enemyIndex];
            enemy.poisonDamage = poisonDamage;
            enemy.poisonRemainingTime = poisonDuration;
            enemy.lastPoisonDamageTime = Time.time;
            SetOverlayColor(enemy, poisonedOverlayColor, 0.3f);
            UpdateStatusParticles(enemy);
        }
    }

    public void ApplyFreezeEffect(string enemyGoName, float duration)
    {
        int enemyIndex = int.Parse(enemyGoName);
        if (allActiveEnemiesDico.ContainsKey(enemyIndex))
        {
            EnemyInstance enemy = allActiveEnemiesDico[enemyIndex];
            enemy.freezeRemainingTime = duration;
            SetOverlayColor(enemy, frozenOverlayColor, 0.3f);
            UpdateStatusParticles(enemy);
        }
    }

    public void ApplyGlobalFreezeEffect()
    {
        applyGlobalFreeze = true;
        if (SetGlobalFreezeEffectCoroutine != null)
        {
            StopCoroutine(SetGlobalFreezeEffectCoroutine);
        }
        SetGlobalFreezeEffectCoroutine = StartCoroutine(SetGlobalFreezeEffect(false, DataManager.instance.powerUpFreezeDuration));
    }

    private IEnumerator SetGlobalFreezeEffect(bool active, float duration)
    {
        yield return new WaitForSeconds(duration);
        applyGlobalFreeze = active;
    }

    public void ApplyGlobalPoisonEffect()
    {
        applyGlobalPoison = true;
        if (SetGlobalPoisonEffectCoroutine != null)
        {
            StopCoroutine(SetGlobalPoisonEffectCoroutine);
        }
        SetGlobalPoisonEffectCoroutine = StartCoroutine(SetGlobalPoisonEffect(false, DataManager.instance.powerUpPoisonDuration));
    }

    private IEnumerator SetGlobalPoisonEffect(bool active, float duration)
    {
        yield return new WaitForSeconds(duration);
        applyGlobalPoison = active;
    }

    public void ApplyGlobalCurseEffect()
    {
        applyGlobalCurse = true;
        if (SetGlobalCurseEffectCoroutine != null)
        {
            StopCoroutine(SetGlobalCurseEffectCoroutine);
        }
        SetGlobalCurseEffectCoroutine = StartCoroutine(SetGlobalCurseEffect(false, DataManager.instance.powerUpCurseDuration));
    }

    private IEnumerator SetGlobalCurseEffect(bool active, float duration)
    {
        yield return new WaitForSeconds(duration);
        applyGlobalCurse = active;
    }

    public void ApplyCurseEffect(string enemyGoName, float duration)
    {
        int enemyIndex = int.Parse(enemyGoName);
        if (allActiveEnemiesDico.ContainsKey(enemyIndex))
        {
            EnemyInstance enemy = allActiveEnemiesDico[enemyIndex];
            enemy.curseRemainingTime = duration;
            SetOverlayColor(enemy, cursedOverlayColor, 0.3f);
            UpdateStatusParticles(enemy);
        }
    }

    /// <summary>
    /// Reset every active & alive bug to another bug of the same Kind, with a different tier
    /// If tier is < 1 and tier should decrease, then the bug is killed (XP is spawned)
    /// If tier is > 5 and tier should increase, then the bug is turned into a bounty giving extra XP & Froins
    /// If the bug was a bounty already, then it is not changed
    /// </summary>
    /// <param name="deltaTier"></param>
    public void SwitchTierOfAllEnemies(int deltaTier)
    {
        Dictionary<int, EnemyInstance> temporaryAllActiveEnemiesDico = new Dictionary<int, EnemyInstance>(allActiveEnemiesDico);
        foreach (KeyValuePair<int, EnemyInstance> keyValue in temporaryAllActiveEnemiesDico)
        {
            int enemyID = keyValue.Key;
            EnemyInstance enemyInstance = keyValue.Value;

            if (enemyInstance.bountyBug != null)
            {
                // Enemy has a bounty, it is not changed one way or the other
            }
            else
            {
                // Enemy has no bounty, let's switch its tier!
                int newTier = enemyInstance.difficultyTier + deltaTier;
                Vector3 enemyPosition = enemyInstance.enemyTransform.position;
                Vector2 enemyMoveDirection = enemyInstance.moveDirection;
                EnemyMovePattern enemyMovePattern = new EnemyMovePattern(enemyInstance.movePattern);
                EnemyData enemyData = GetEnemyDataFromTypeAndDifficultyTier(enemyInstance.enemyInfo.enemyData.enemyType, Mathf.Clamp(newTier, 1, 5));
                if (newTier < 1)
                {
                    // Spawn XP
                    float XPEarned = Mathf.Clamp(enemyData.xPBonus / 2, 1, 100);
                    XPEarned *= (1 + GameManager.instance.player.curse);
                    CollectiblesManager.instance.SpawnCollectible(enemyPosition, CollectibleType.XP_BONUS, XPEarned);
                }
                else if (newTier > 5)
                {
                    // Spawn the same enemy but with a bounty
                    BountyBug bountyBug = new BountyBug(enemyData.enemyType, 
                        overrideHealthMultiplier: false, healthMultiplier: bountyDefaultHealthMultiplier,
                        2, 20, enemyMovePattern);
                    bountyBug.bountyList.Add( new Bounty() { collectibleType = CollectibleType.FROINS, amount = 10, value = 10 });
                    bountyBug.bountyList.Add( new Bounty() { collectibleType = CollectibleType.XP_BONUS, amount = 10, value = 50 });
                    StartCoroutine(SpawnEnemyAsync(enemyData.prefab, enemyPosition, enemyData, enemyMovePattern, enemyInstance.wave, delay: 0.01f, difficultyTier: 5, neverDespawn: true, bounty: bountyBug, forceMovementDirection: true, moveDirection: enemyMoveDirection));
                }
                else
                {
                    // Spawn a new enemy with new tier
                    GameObject enemyPrefab = enemyData.prefab;
                    StartCoroutine(SpawnEnemyAsync(enemyPrefab, enemyPosition, enemyData, enemyMovePattern, enemyInstance.wave, delay: 0.01f, newTier, forceMovementDirection: true, moveDirection: enemyMoveDirection));
                }
                // Unspawn that enemy
                enemyInstance.enemyRenderer.enabled = false;
                enemyInstance.active = false;
                PutEnemyInThePool(allActiveEnemiesDico[enemyID]);
                allActiveEnemiesDico.Remove(enemyID);
            }
        }
    }

    private void SetEnemyVelocity(EnemyInstance enemy, float updateDeltaTime)
    {
        // Set speed factor
        float changeSpeedFactor = 1;
        enemy.enemyRigidbody.simulated = true;
        float newMass = enemy.mass;
        if (enemy.freezeRemainingTime > 0 || applyGlobalFreeze)
        {
            changeSpeedFactor = 0;
            newMass = 10000;
        }
        else if (enemy.curseRemainingTime > 0 || applyGlobalCurse)
        {
            changeSpeedFactor = 2;
            newMass = 1000;
        }
        enemy.enemyRigidbody.mass = newMass;

        if (changeSpeedFactor > 0 && enemy.movePattern.movePatternType != EnemyMovePatternType.NO_MOVEMENT)
        {
            // Set orientation
            float angle = -Vector2.SignedAngle(enemy.moveDirection, Vector2.right);
            float roundedAngle = -90 + Mathf.RoundToInt(angle / 90) * 90;
            enemy.enemyTransform.rotation = Quaternion.Euler(0, 0, roundedAngle);
        }

        // Set speed
        float actualSpeed = GetEnemyDataFromGameObjectName(enemy.enemyTransform.name, out BountyBug bountyBug).moveSpeed * changeSpeedFactor * enemy.movePattern.speedFactor;
        actualSpeed = Mathf.Clamp(actualSpeed, 0, 30);
        enemy.enemyRigidbody.velocity = enemy.moveDirection * actualSpeed;
        enemy.enemyAnimator.SetFloat("Speed", actualSpeed);
    }

    private void SetOverlayColor(EnemyInstance enemyInstance, Color color, float delay)
    {
        enemyInstance.SetOverlayColor(color);
        if (enemyInstance.removeOverlayCoroutine != null)
        {
            StopCoroutine(enemyInstance.removeOverlayCoroutine);
            enemyInstance.removeOverlayCoroutine = null;
        }
        StartCoroutine(RemoveOverlayColorAsync(enemyInstance, delay));
    }

    private IEnumerator RemoveOverlayColorAsync(EnemyInstance enemyInstance, float delay)
    {
        yield return new WaitForSeconds(delay);
        enemyInstance.RemoveOverlay();
    }

    private void UpdateStatusParticles(EnemyInstance enemyInstance)
    {
        // Freeze effect
        if (enemyInstance.enemyFreezeParticles != null)
        {
            if (applyGlobalFreeze || enemyInstance.freezeRemainingTime > 0)
            {   
                if (!enemyInstance.enemyFreezeParticles.isPlaying)
                {
                    enemyInstance.enemyFreezeParticles.Play();
                }
            }
            else
            {
                if (enemyInstance.enemyFreezeParticles.isPlaying)
                {
                    enemyInstance.enemyFreezeParticles.Stop();
                }
            }
        }

        // Curse effect
        if (enemyInstance.enemyCurseParticles != null)
        {
            if (applyGlobalCurse || enemyInstance.curseRemainingTime > 0)
            {
                if (!enemyInstance.enemyCurseParticles.isPlaying)
                {
                    enemyInstance.enemyCurseParticles.Play();
                }
            }
            else
            {
                if (enemyInstance.enemyCurseParticles.isPlaying)
                {
                    enemyInstance.enemyCurseParticles.Stop();
                }
            }
        }

        // Poison effect
        if (enemyInstance.enemyPoisonParticles != null)
        {
            if (applyGlobalPoison || enemyInstance.poisonRemainingTime > 0)
            {
                if (!enemyInstance.enemyPoisonParticles.isPlaying)
                {
                    enemyInstance.enemyPoisonParticles.Play();
                }
            }
            else
            {
                if (enemyInstance.enemyPoisonParticles.isPlaying)
                {
                    enemyInstance.enemyPoisonParticles.Stop();
                }
            }
        }
    }

    public void SetEnemyDead(EnemyInstance enemyInstance, bool despawnEnemyAndSpawnXP = false)
    {
        if (enemyInstance.enemyInfo != null)
        {
            enemyInstance.enemyInfo.totalEatenCount++;
            SaveDataManager.instance.isSaveDataDirty = true;
        }

        enemyInstance.HP = 0;
        enemyInstance.alive = false;

        enemyInstance.enemyAnimator.SetBool("IsDead", true);
        enemyInstance.enemyCollider.enabled = false;

        // Remove visual effect on the enemy
        //enemyInstance.RemoveOutline();
        enemyInstance.RemoveOverlay();

        // Spawn rewards for bounties
        if (enemyInstance.bountyBug != null)
        {
            if (enemyInstance.bountyBug.bountyList != null
                && enemyInstance.bountyBug.bountyList.Count > 0)
            {
                foreach (Bounty bounty in enemyInstance.bountyBug.bountyList)
                {
                    for (int i = 0; i < bounty.amount; i++)
                    {
                        CollectiblesManager.instance.SpawnCollectible(enemyInstance.enemyTransform.position, bounty.collectibleType, bounty.value, pushAwayForce: bounty.amount);
                    }
                }

                // Play SFX
                SoundManager.instance.PlayEatBountySound();
            }
        }

        if (despawnEnemyAndSpawnXP)
        {
            enemyInstance.enemyRenderer.enabled = false;
            enemyInstance.active = false;

            // Increase kill count by 1 and display it
            RunManager.instance.IncreaseKillCount(1);

            // Spawn XP
            Vector3 xpSpawnPosition = enemyInstance.enemyTransform.position - 0.1f * Vector3.up;
            float XPEarned = enemyInstance.enemyInfo.enemyData.xPBonus;
            if (enemyInstance.bountyBug != null)
            {
                XPEarned *= enemyInstance.bountyBug.xpMultiplier;
            }
            XPEarned *= (1 + GameManager.instance.player.curse);
            CollectiblesManager.instance.SpawnCollectible(xpSpawnPosition, CollectibleType.XP_BONUS, XPEarned);

            // Spawn Froins (a chance to get froins when killing a bug)
            float probabilityToSpawn1SmolFroin = DataManager.instance.baseCurrencyProbabilitySpawnFromBugs * (1 + GameManager.instance.player.currencyBoost); // worth 1 Froin
            float probabilityToSpawn1BigFroin = probabilityToSpawn1SmolFroin / 10; // worth 5 Froins
            float spawnFroinsRoll = Random.Range(0, 1f);
            float valueOfFroinSpawned = 0;
            if (spawnFroinsRoll <= probabilityToSpawn1BigFroin)
            {
                valueOfFroinSpawned = 5;
            } else if (spawnFroinsRoll <= probabilityToSpawn1SmolFroin)
            {
                valueOfFroinSpawned = 1;
            }
            if (valueOfFroinSpawned > 0)
            {
                // Spawn froin
                Vector3 froinsSpawnPosition = enemyInstance.enemyTransform.position;
                CollectiblesManager.instance.SpawnCollectible(froinsSpawnPosition, CollectibleType.FROINS, bonusValue: valueOfFroinSpawned, pushAwayForce: 1);
            }

            PutEnemyInThePool(enemyInstance);
            allActiveEnemiesDico.Remove(enemyInstance.enemyID);
        }
        
    }

    public void SetEnemyDead(string enemyGameObjectName)
    {
        int enemyIndex = int.Parse(enemyGameObjectName);
        EnemyInstance enemyInstance = allActiveEnemiesDico[enemyIndex];
        SetEnemyDead(enemyInstance);
    }

    /// <summary>
    /// Update the enemies data using a EnemiesSaveData object, that was probably loaded from a file by the SaveDataManager.
    /// </summary>
    /// <param name="saveData"></param>
    public void SetEnemiesData(EnemiesSaveData saveData)
    {
        foreach (EnemyInfo enemy in enemiesData.enemiesList)
        {
            EnemyInfo enemyFromSave = saveData.enemiesList.FirstOrDefault(x => x.enemyName.Equals(enemy.enemyName));
            if (enemyFromSave != null)
            {
                enemy.totalEatenCount = enemyFromSave.totalEatenCount;
            }
        }
    }

    /// <summary>
    /// Reset all enemies. Set the kill counts to zero
    /// </summary>
    public void ResetEnemies()
    {
        enemiesData.enemiesList.Clear();
        foreach (EnemyTypeData enemyTypeData in enemiesTypesDataList)
        {
            foreach (EnemyData enemyData in enemyTypeData.enemiesList)
            {
                EnemyInfo enemyInfo = new EnemyInfo() { enemyData = enemyData, enemyName = enemyData.enemyName, totalEatenCount = 0 };
                if (!enemiesData.enemiesList.Contains(enemyInfo))
                {
                    enemiesData.enemiesList.Add(enemyInfo);
                }
            }
        }
        SaveDataManager.instance.isSaveDataDirty = true;
    }
}
