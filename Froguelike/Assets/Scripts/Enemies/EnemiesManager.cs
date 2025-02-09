using Rewired;
using Steamworks;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
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
    public static float knockbackMass = 100;

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
    public float spawnTime;
    public float HP;
    public float HPMax;
    public Vector2 moveDirection;
    public float lastUpdateTime;
    public float mass; // mass will change during knockback, freeze and curse and must be restored afterwards
    public float damageMultiplier;
    public float xpMultiplier;
    public float knockbackResistance;

    public float lastChangeOfDirectionTime; // for directionless enemies
    public int bounceCount; // for enemies that bounce on screen edges

    public float lastDamageInflictedTime; // last time this enemy inflicted damage to player

    // Move pattern
    public EnemyMovePattern movePattern;

    // Origin wave
    public WaveData wave;
    public bool neverDespawn;

    // Origin spawn pattern
    public SpawnPattern spawnPattern;

    // Difficulty tier
    public int difficultyTier;

    // Current in-game state - knockback & cooldown
    public float knockbackCooldown;

    // Current in-game state - poison
    public List<float> poisonDamageList;
    public List<float> poisonRemainingTimeList;
    public float lastPoisonDamageTime;

    // Current in-game state - curse
    public float curseRemainingTime;

    // Current in-game state - freeze
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

    public EnemyInstance(GameObject enemyGameObject)
    {
        poisonDamageList = new List<float>();
        poisonRemainingTimeList = new List<float>();

        active = false;
        alive = false;

        damageMultiplier = 1;
        xpMultiplier = 1;
        knockbackResistance = 0;

        enemyTransform = enemyGameObject.transform;
        enemyRenderer = enemyGameObject.GetComponent<SpriteRenderer>();
        enemyRigidbody = enemyGameObject.GetComponent<Rigidbody2D>();
        enemyAnimator = enemyGameObject.GetComponent<Animator>();
        enemyCollider = enemyGameObject.GetComponent<Collider2D>();

        enemyFreezeParticles = enemyTransform.Find("Freeze Particles").GetComponent<ParticleSystem>();
        enemyPoisonParticles = enemyTransform.Find("Poison Particles").GetComponent<ParticleSystem>();
        enemyCurseParticles = enemyTransform.Find("Curse Particles").GetComponent<ParticleSystem>();
    }

    public void RemovePoison()
    {
        poisonDamageList.Clear();
        poisonRemainingTimeList.Clear();
    }

    public void AddPoisonDamage(float damage, float remainingTime)
    {
        poisonDamageList.Add(damage);
        poisonRemainingTimeList.Add(remainingTime);
    }

    public void DecreasePoisonRemainingTimes(float deltaTime)
    {
        for (int i = 0; i < poisonDamageList.Count; i++)
        {
            poisonRemainingTimeList[i] -= deltaTime;
            if (poisonRemainingTimeList[i] <= 0)
            {
                poisonRemainingTimeList.RemoveAt(i);
                poisonDamageList.RemoveAt(i);
                i--;
            }
        }
    }

    public bool IsAnyPoisonActive()
    {
        return poisonDamageList.Count > 0;
    }

    public float GetActivePoisonDamage()
    {
        return poisonDamageList.Sum(x => x);
    }

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
        enemyRigidbody.mass = knockbackMass;
        enemyRigidbody.velocity = direction * strength;
        knockbackCooldown = duration;
    }

    public void StopKnockback()
    {
        if (enemyRigidbody.mass == knockbackMass)
        {
            enemyRigidbody.mass = mass;
        }
    }

    public void ForceStopAllStatusEffects()
    {
        curseRemainingTime = 0;
        freezeRemainingTime = 0;

        RemovePoison();

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
    public int damageTextFontSize_PixelFont = 10;
    public int damageTextFontSize_AccessibleFont = 16;
    public int damageTextFontSize_BoringFont = 16;
    [Space]
    public float damageTextThreshold_smol = 0;
    public Color damageTextColor_smol;
    [Space]
    public float damageTextThreshold_medium = 100;
    public Color damageTextColor_medium;
    [Space]
    public float damageTextThreshold_big = 500;
    public Color damageTextColor_big;
    [Space]
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
    public float bountyDefaultXPMultiplier = 20;
    public float bountyDefaultKnockbackResistance = 0;
    [Space]
    public float delayBetweenBountyRewardSpawn = 0.1f;
    [Space]
    public int bountyOutlineThickness = 1;
    public List<Color> bountyOutlineColorsList;

    [Header("Runtime")]
    public static int lastKey;

    [Header("Runtime - saved data")]
    public EnemiesSaveData enemiesData; // Will be loaded and saved when needed

    // private
    private List<float> lastSpawnTimesList;
    private Dictionary<string, EnemyData> enemiesDataFromNameDico; // Associate enemy name and EnemyData
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
            GameObject firstEnemyPrefab = defaultEnemyPrefab;
            GameObject enemyGameObject = Instantiate(firstEnemyPrefab, DataManager.instance.GetFarAwayPosition(), Quaternion.identity, enemiesParent);

            EnemyInstance newEnemy = new EnemyInstance(enemyGameObject);

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

    private bool IsSpawnPositionFreeFromCollisions(Vector3 spawnPosition, int layer)
    {
        bool collision = false;

        string layerName = LayerMask.LayerToName(layer);
        int layerMask;
        if (layerName.Equals("EnemiesGrounded"))
        {
            layerMask = LayerMask.GetMask("LakeCollider", "Rock");
        }
        else if (layerName.Equals("EnemiesGroundedAndFloating"))
        {
            layerMask = LayerMask.GetMask("Rock");
        }
        else
        {
            layerMask = LayerMask.GetMask();
        }

        if (Physics2D.OverlapCircle(spawnPosition, 0.1f, layerMask) != null)
        {
            collision = true;
        }

        return !collision;
    }

    /// <summary>
    /// Returns a random spawn position around the player and in the direction of its movement.
    /// </summary>
    /// <param name="playerPosition"></param>
    /// <param name="playerMoveDirection">This vector is either zero (player doesn't move) or normalized</param>
    /// <param name="spawnPosition">Out position, valid only if method returns true</param>
    /// <param name="overrideDirection">If override, then we use directionAngle, otherwise spawnPosition is going to depend on frog position and movement direction</param>
    /// <param name="directionAngle">Angle between Vector2.Right and frog to spawn position</param>
    /// <returns>Returns true if it successfully found a spawn position</returns>
    private bool GetSpawnPosition(Vector2 playerPosition, Vector2 playerMoveDirection, out Vector3 spawnPosition, bool overrideDirection = false, float overridenDirectionAngle = 0, float overridenDirectionSpread = 1)
    {
        spawnPosition = Vector2.zero;

        // Will spawn in a circle around a position
        Vector2 spawnDirection = Vector2.zero;
        float actualSpawnCircleRadius = spawnCircleRadius; // Default radius is quite big to also work when centered around frog
        float spawnCenterDistanceToPlayer = Random.Range(spawnCenterMinDistanceToPlayer, spawnCenterMaxDistanceToPlayer);

        if (overrideDirection)
        {
            // The center of that circle is in the given direction
            spawnDirection = new Vector2(Mathf.Cos(overridenDirectionAngle * Mathf.Deg2Rad), Mathf.Sin(overridenDirectionAngle * Mathf.Deg2Rad));
            actualSpawnCircleRadius = overridenDirectionSpread; // if we override direction, then the spawn circle must be small, otherwise it doesn't feel like the bugs are coming from that direction
        }
        else if (!playerMoveDirection.Equals(Vector2.zero))
        {
            // The center of that circle is in front of the frog
            spawnDirection = playerMoveDirection;
        }
        Vector2 spawnCenter = playerPosition + spawnDirection * spawnCenterDistanceToPlayer;
        
        // Attemp to find a valid spawn point. Loop and try again until it works.
        bool spawnPositionIsValid = false;
        int loopAttemptCount = findSpawnPositionMaxAttempts;

        do
        {
            // Get a random point in the spawn circle
            spawnPosition = spawnCenter;

            float randomAngle = Random.Range(0, 360.0f) * (loopAttemptCount+1);
            Vector2 randomVector = new Vector2(Mathf.Cos(randomAngle * Mathf.Deg2Rad), Mathf.Sin(randomAngle * Mathf.Deg2Rad)) * (Random.Range(0.0f, (loopAttemptCount+1)) / (loopAttemptCount+1));
            spawnPosition += new Vector3(randomVector.x, randomVector.y) * actualSpawnCircleRadius;

            loopAttemptCount--;
            // Check if that random point is not in sight (you don't want to spawn an enemy where you can see it)
            if (Vector2.Distance(spawnPosition, playerPosition) > minSpawnDistanceFromPlayer)
            {
                spawnPositionIsValid = true;
            }

            // Don't check if that random point is on an obstacle
            /*
            int layerMask = LayerMask.GetMask("Rock", "LakeCollider");
            if (Physics2D.OverlapCircle(spawnPosition, 0.1f, layerMask) != null)
            {
                spawnPositionIsValid = false;
            }*/
        } while (!spawnPositionIsValid && loopAttemptCount > 0); // Redo until the random point is out of sight

        return spawnPositionIsValid;
    }

    public static int GetFormulaValue(string formula, int chapterCount)
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
        if (RunManager.instance.currentChapter != null && RunManager.instance.currentChapter.chapterData != null && !RunManager.instance.IsChapterTimeOver())
        {
            if (spawnedBugsWithBountiesIDs != null && !spawnedBugsWithBountiesIDs.Contains(bountyID))
            {
                // Keep a dictionary of current bounties
                spawnedBugsWithBountiesIDs.Add(bountyID);

                // Get random spawn position, around and in front of frog
                GetSpawnPosition(GameManager.instance.player.transform.position, GameManager.instance.player.GetMoveDirection(), out Vector3 spawnPosition);

                // Get EnemyData & prefab according to relevant difficulty tier
                int difficultyTier = GetTierFromFormulaAndChapterCount(bountyBug.tierFormula, RunManager.instance.GetChapterCount());
                EnemyData enemyData = GetEnemyDataFromTypeAndDifficultyTier(bountyBug.enemyType, difficultyTier);
                GameObject enemyPrefab = enemyData.prefab;

                // Spawn bug using info we have
                Vector3 positionRelativeToFrog = (spawnPosition - GameManager.instance.player.transform.position) + Random.Range(-1.0f, 1.0f) * Vector3.right + Random.Range(-1.0f, 1.0f) * Vector3.up;
                StartCoroutine(TrySpawnEnemyAsync(enemyPrefab, positionRelativeToFrog, enemyData, bountyBug.movePattern, originWave: null, originSpawnPattern: null, delay: 0, difficultyTier: difficultyTier, neverDespawn: true, bounty: bountyBug));

            }
        }
    }


    public void TrySpawnWave(WaveData currentWave)
    {
        if (RunManager.instance.currentChapter != null && RunManager.instance.currentChapter.chapterData != null && !RunManager.instance.IsChapterTimeOver())
        {
            // TODO: remove log
            string log = $"TrySpawnWave from chapter {RunManager.instance.currentChapter.chapterID}. Current wave is {currentWave.ToString()}";
            bool showLog = false; 

            // Here's a table to describe the figurine curse:
            // - Curse -100% =  Spawn halved     =  Spawn cooldown doubled (delay factor = 2) 
            // - Curse 0%    =  Spawn untouched  =  Spawn cooldown untouched (delay factor = 1)
            // - Curse 50%   =  Spawn            =  Spawn cooldown
            // - Curse 100%  =  Spawn doubled    =  Spawn cooldown halved (delay factor = 0.5)
            // - Curse 200%  =  Spawn tripled    =  Spawn cooldown divided by 3 (delay factor = 0.33f)
            // - Curse 300%  =  Spawn quadrupled =  Spawn cooldown divided by 4 (delay factor = 0.25f)

            float figurineCurse = System.Math.Clamp(GameManager.instance.player.GetCurse(), -0.99f, 10); // Figurine curse value is between -99% and +1000%
            float figurineCurseDelayFactor = 1 / (1 + figurineCurse); // Figurine curse will affect the delay negatively (lower delay = more spawns)

            for (int i = 0; i < currentWave.enemies.Count; i++)
            {
                // Pick a random spawn
                //enemyIndex = Random.Range(0, currentWave.enemies.Count);

                EnemySpawn enemySpawn = currentWave.enemies[i];
                float lastSpawnTime = lastSpawnTimesList[i];

                // Get info about spawn delays
                double delayBetweenSpawns = enemySpawn.spawnCooldown * figurineCurseDelayFactor;
                delayBetweenSpawns = System.Math.Clamp(delayBetweenSpawns, 0.01, double.MaxValue);

                bool spawn = (Time.time - lastSpawnTime) > delayBetweenSpawns;

                if (spawn)
                {
                    // If spawn cooldown is over, then spawn!
                    log += $"\nSpawning enemy: {enemySpawn}. Last spawn time = {lastSpawnTime}. Delay between spawns = {delayBetweenSpawns}";
                    showLog = true;

                    // Get EnemyData & prefab according to relevant difficulty tier
                    int difficultyTier = GetTierFromFormulaAndChapterCount(enemySpawn.tierFormula, RunManager.instance.GetChapterCount());
                    EnemyData enemyData = GetEnemyDataFromTypeAndDifficultyTier(enemySpawn.enemyType, difficultyTier);
                    GameObject enemyPrefab = enemyData.prefab;

                    log += $". enemyPrefab = {enemyPrefab}";

                    /// Get spawn pattern info
                    SpawnPattern spawnPattern = enemySpawn.spawnPattern;
                    log += $". spawnPattern = {spawnPattern}";
                    // Spawn type
                    SpawnPatternType spawnPatternType = spawnPattern.spawnPatternType; // The type of spawn pattern (random, chunk, shape)
                    // Spawn position settings
                    bool spawnOverrideDefaultSpawnPosition = spawnPattern.overrideDefaultSpawnPosition; // Default is spawn ahead of frog movement direction, or random if frog isn't moving
                    float spawnPositionAngle = spawnPattern.spawnPositionAngle; // Angle = 0 means East, Angle = 90 means North, etc.
                    float spawnPositionSpread = spawnPattern.spawnPositionSpread; // Radius around overriden spawn position where the bugs can spawn
                    // Multiple spawns settings
                    int spawnBugsAmount = spawnPattern.spawnAmount; // How many enemies will spawn
                    float spawnMultipleDelayBetweenSpawns = spawnPattern.multipleSpawnDelay; // The delay between these spawns
                    // Shape settings
                    SpawnShape spawnPatternShape = spawnPattern.spawnPatternShape; // Only if the type is Shape, then which Shape?
                    bool spawnShapeCenteredOnFrog = spawnPattern.shapeCenteredOnFrog; // Is shape around frog or around spawn point?
                    float shapeOrientationAngle = spawnPattern.shapeOrientationAngle;
                    if (spawnPattern.shapeOrientationIsRandomized)
                    {
                        shapeOrientationAngle = Random.Range(spawnPattern.shapeOrientationMinAngle, spawnPattern.shapeOrientationMaxAngle);
                    }
                    // Shape CIRCLE_ARC settings
                    float spawnShapeCircleArcRadius = spawnPattern.circleArcRadius; // Circle arc size
                    float spawnShapeCircleArcStartAngle = spawnPattern.circleArcStartAngle; // Angle = 0 means aligned with vector 'frog to spawn'
                    float spawnShapeCircleArcEndAngle = spawnPattern.circleArcEndAngle; // Angle = 0 means aligned with vector 'frog to spawn'
                    // Shape SPIRAL settings
                    float spawnShapeSpiralStartRadius = spawnPattern.spiralStartRadius; // Spiral start radius
                    float spawnShapeSpiralRadiusIncrease = spawnPattern.spiralRadiusIncreasePerFullRotation; // Spiral radius increase per 360° rotation
                    float spawnShapeSpiralStartAngle = spawnPattern.spiralStartAngle; // Spiral start angle
                    float spawnShapeSpiralEndAngle = spawnPattern.spiralEndAngle; // Spiral end angle
                    bool spawnShapeSpiralIsClockwise = spawnPattern.spiralIsClockwise; // Spiral direction
                    // Shape LINES settings
                    float spawnShapeLineAngle = spawnPattern.lineAngle; // Angle = 0 means aligned with vector 'frog to spawn'
                    float spawnShapeLineLength = spawnPattern.lineLength; // Length of the line
                    // Shape WAVE_LINE settings
                    float spawnShapeWaveLineAmplitude = spawnPattern.waveLineAmplitude; // How big are the waves
                    float spawnShapeWaveLineFrequency = spawnPattern.waveLineFrequency; // How close together are the waves
                    float spawnShapeWaveLineOffset = spawnPattern.waveLineOffset; // Where do the waves start (default is zero/middle)
                    // Shape POLYGON and SPRITE settings
                    float spawnShapePolygonOrSpriteRadius = spawnPattern.shapeRadius; // The radius of a circle that contains that shape
                    float spawnShapePolygonOrSpriteAngle = spawnPattern.shapeAngle; // The angle of that shape relative to the vector 'frog to spawn'
                    // Shape POLYGON setting
                    int spawnShapePolygonNumberOfSides = spawnPattern.shapePolygonNumberOfSides; // Only for SpawnShape of type POLYGON
                    // Shape SPRITE setting
                    Sprite spawnShapeSprite = spawnPattern.shapeSprite; // Only for SpawnShape of type SPRITE
                                        
                    float currentDelay = 0;
                    Vector3 spawnPosition = Vector3.zero;

                    log += $". spawnPatternType = {spawnPatternType}";
                    switch (spawnPatternType)
                    {
                        case SpawnPatternType.CHUNK:
                            // Choose a position at a distance from the player and spawn a chunk of enemies at that position
                            if (GetSpawnPosition(GameManager.instance.player.transform.position, GameManager.instance.player.GetMoveDirection(), out spawnPosition, overrideDirection: spawnOverrideDefaultSpawnPosition, overridenDirectionAngle: spawnPositionAngle, overridenDirectionSpread: spawnPositionSpread))
                            {
                                for (int j = 0; j < spawnBugsAmount; j++)
                                {
                                    Vector3 positionRelativeToFrog = (spawnPosition - GameManager.instance.player.transform.position) + Random.Range(-1.0f, 1.0f) * Vector3.right + Random.Range(-1.0f, 1.0f) * Vector3.up;
                                    StartCoroutine(TrySpawnEnemyAsync(enemyPrefab, positionRelativeToFrog, enemyData, enemySpawn.movePattern, currentWave, spawnPattern, currentDelay, difficultyTier));
                                    currentDelay += spawnMultipleDelayBetweenSpawns;
                                }
                            }
                            break;
                        case SpawnPatternType.SHAPE:

                            Vector3 shapePositionRelativeToFrog = Vector3.zero;
                            log += $". shapePositionRelativeToFrog = {shapePositionRelativeToFrog.ToString("0.00")}";

                            log += $". spawnPatternShape = {spawnPatternShape}";
                            if (spawnPatternShape == SpawnShape.STRAIGHT_LINE || spawnPatternShape == SpawnShape.WAVE_LINE || spawnPatternShape == SpawnShape.SPRITE)
                            {
                                // Lines and sprites can't be centered on frog, they have to spawn further
                                spawnShapeCenteredOnFrog = false;
                            }

                            log += $". spawnShapeCenteredOnFrog = {spawnShapeCenteredOnFrog}";
                            if (spawnShapeCenteredOnFrog)
                            {
                                // Shape is centered on the frog
                            }
                            else
                            {
                                // Shape is placed somewhere further around the frog
                                log += $". spawnOverrideDefaultSpawnPosition = {spawnOverrideDefaultSpawnPosition}";
                                log += $". spawnPositionAngle = {spawnPositionAngle}";
                                log += $". spawnPositionSpread = {spawnPositionSpread}";
                                if (GetSpawnPosition(GameManager.instance.player.transform.position, GameManager.instance.player.GetMoveDirection(), out spawnPosition, overrideDirection: spawnOverrideDefaultSpawnPosition, overridenDirectionAngle: spawnPositionAngle, overridenDirectionSpread: spawnPositionSpread))
                                {
                                    // Here we got a position, but we'll need to push it a bit further depending on how big is the shape
                                    shapePositionRelativeToFrog = (spawnPosition - GameManager.instance.player.transform.position);
                                }
                                else
                                {
                                    // If GetSpawnPosition didn't give a position, then we'll take the furthest one possible with current parameters
                                    if (spawnOverrideDefaultSpawnPosition)
                                    {
                                        shapePositionRelativeToFrog = new Vector3(Mathf.Cos(spawnPositionAngle * Mathf.Deg2Rad), Mathf.Sin(spawnPositionAngle * Mathf.Deg2Rad)) * (minSpawnDistanceFromPlayer + spawnPositionSpread*2);
                                    }
                                    else
                                    {
                                        float randomAngle = Random.Range(0, 360.0f);
                                        shapePositionRelativeToFrog = new Vector3(Mathf.Cos(randomAngle * Mathf.Deg2Rad), Mathf.Sin(randomAngle * Mathf.Deg2Rad)) * (minSpawnDistanceFromPlayer + spawnPositionSpread*2);
                                    }
                                }
                            }
                            log += $". spawnShapeCenteredOnFrog = {spawnShapeCenteredOnFrog}";
                            log += $". spawnPosition = {spawnPosition.ToString("0.00")}";

                            // Special case: if bug movement pattern is straight line and if shape is not centered on frog, then we'll force movement direction to keep the shape intact
                            bool forceMovementDirection = false;
                            Vector2? movementDirection = null;
                            if (!spawnShapeCenteredOnFrog && enemySpawn.movePattern.movePatternType == EnemyMovePatternType.STRAIGHT_LINE)
                            {
                                forceMovementDirection = true;
                                movementDirection = new Vector2((-shapePositionRelativeToFrog).normalized.x, (-shapePositionRelativeToFrog).normalized.y);
                            }

                            float angleInDegrees;
                            log += $". spawnPatternShape = {spawnPatternShape}";
                            switch (spawnPatternShape)
                            {
                                case SpawnShape.NONE:
                                case SpawnShape.CIRCLE_ARC:
                                    float circleArcMinAngle = (spawnShapeCircleArcEndAngle < spawnShapeCircleArcStartAngle) ? spawnShapeCircleArcEndAngle : spawnShapeCircleArcStartAngle;
                                    float circleArcMaxAngle = (spawnShapeCircleArcEndAngle >= spawnShapeCircleArcStartAngle) ? spawnShapeCircleArcEndAngle : spawnShapeCircleArcStartAngle;

                                    float circleArcRadius = spawnShapeCircleArcRadius;
                                    if (spawnShapeCenteredOnFrog)
                                    {
                                        if (circleArcRadius < minSpawnDistanceFromPlayer)
                                        {
                                            // If overriden circle radius is under the minimum spawn distance from frog, and if the circle is centered on frog, then we force the radius to be big enough
                                            circleArcRadius = minSpawnDistanceFromPlayer;
                                        }
                                    }
                                    else
                                    {
                                        // If shape is not centered on frog, we want to make sure it spawns far enough so no part of the circle is visible
                                        shapePositionRelativeToFrog += ((shapePositionRelativeToFrog.normalized) * (circleArcRadius/2));
                                    }

                                    for (int spawnCount = 0; spawnCount < spawnBugsAmount; spawnCount++)
                                    {
                                        // Find position of next bug on the circle
                                        angleInDegrees = (circleArcMinAngle + ((spawnCount * 1.0f / spawnBugsAmount) * (circleArcMaxAngle - circleArcMinAngle))) + shapeOrientationAngle;
                                        spawnPosition = shapePositionRelativeToFrog + (Mathf.Cos(angleInDegrees * Mathf.Deg2Rad) * Vector3.right + Mathf.Sin(angleInDegrees * Mathf.Deg2Rad) * Vector3.up) * circleArcRadius;

                                        // Spawn one bug on the circle
                                        StartCoroutine(TrySpawnEnemyAsync(enemyPrefab, spawnPosition, enemyData, enemySpawn.movePattern, currentWave, spawnPattern, currentDelay, difficultyTier,
                                        forceMovementDirection: forceMovementDirection, moveDirection: movementDirection));

                                        // Eventually increase the delay before a spawn (WARNING: delay will change how the shape looks)
                                        currentDelay += spawnMultipleDelayBetweenSpawns;
                                    }
                                    break;
                                case SpawnShape.SPIRAL:
                                    float spiralArcAngle = Mathf.Abs(spawnShapeSpiralEndAngle - spawnShapeSpiralStartAngle); // Angle between start and end, can be more than full circle

                                    float spiralMinAngle = (spawnShapeSpiralEndAngle < spawnShapeSpiralStartAngle) ? spawnShapeSpiralEndAngle : spawnShapeSpiralStartAngle;
                                    float spiralMaxAngle = (spawnShapeSpiralEndAngle >= spawnShapeSpiralStartAngle) ? spawnShapeSpiralEndAngle : spawnShapeSpiralStartAngle;

                                    float spiralRadius = spawnShapeSpiralStartRadius;

                                    if (spawnShapeCenteredOnFrog)
                                    {
                                        if (spiralRadius < minSpawnDistanceFromPlayer)
                                        {
                                            // If overriden spiral start radius is under the minimum spawn distance from frog, and if the spiral is centered on frog, then we force the radius to be big enough
                                            spiralRadius = minSpawnDistanceFromPlayer;
                                        }
                                    }
                                    else
                                    {
                                        // If shape is not centered on frog, we want to make sure it spawns far enough so no part of the spiral is visible
                                        shapePositionRelativeToFrog += ((shapePositionRelativeToFrog.normalized) * ((spawnShapeSpiralStartRadius + (spiralArcAngle / 360) * spawnShapeSpiralRadiusIncrease) / 2));
                                    }

                                    for (int spawnCount = 0; spawnCount < spawnBugsAmount; spawnCount++)
                                    {
                                        // Find position of next bug on the spiral
                                        angleInDegrees = (spawnShapeSpiralIsClockwise ? -1 : 1) * (spiralMinAngle + ((spawnCount * 1.0f / spawnBugsAmount) * (spiralMaxAngle - spiralMinAngle))) + shapeOrientationAngle;
                                        spawnPosition = shapePositionRelativeToFrog + (Mathf.Cos(angleInDegrees * Mathf.Deg2Rad) * Vector3.right + Mathf.Sin(angleInDegrees * Mathf.Deg2Rad) * Vector3.up) * spiralRadius;

                                        // Spiral radius increase with angle (Radius increase by 1 unit per full circle)
                                        spiralRadius += (1.0f / spawnBugsAmount) * (spiralArcAngle / 360) * spawnShapeSpiralRadiusIncrease;

                                        // Spawn one bug on the spiral
                                        StartCoroutine(TrySpawnEnemyAsync(enemyPrefab, spawnPosition, enemyData, enemySpawn.movePattern, currentWave, spawnPattern, currentDelay, difficultyTier,
                                        forceMovementDirection: forceMovementDirection, moveDirection: movementDirection));

                                        // Eventually increase the delay before a spawn (WARNING: delay will change how the shape looks)
                                        currentDelay += spawnMultipleDelayBetweenSpawns;
                                    }
                                    break;
                                case SpawnShape.STRAIGHT_LINE:

                                    log += $". Switch SpawnShape.STRAIGHT_LINE:";

                                    // spawnShapeLineAngle == 0 means aligned with vector from frog
                                    float straightLineAngle = Vector3.Angle(Vector3.right, shapePositionRelativeToFrog) + spawnShapeLineAngle;
                                    // On top of that, we add the orientation angle that may have been randomized
                                    straightLineAngle += shapeOrientationAngle;

                                    Vector3 straightLineDirectionVector = Mathf.Cos(straightLineAngle * Mathf.Deg2Rad) * Vector3.right + Mathf.Sin(straightLineAngle * Mathf.Deg2Rad) * Vector3.up;
                                    Vector3 straightLineStartPosition = shapePositionRelativeToFrog - straightLineDirectionVector * (spawnShapeLineLength / 2);
                                    Vector3 straightLineEndPosition = shapePositionRelativeToFrog + straightLineDirectionVector * (spawnShapeLineLength / 2);

                                    Vector3 straightLinePushAwayVector = Vector3.zero;
                                    if (shapePositionRelativeToFrog.magnitude < minSpawnDistanceFromPlayer)
                                    {
                                        // Center of line is too close to frog
                                        straightLinePushAwayVector = shapePositionRelativeToFrog.normalized * (minSpawnDistanceFromPlayer - shapePositionRelativeToFrog.magnitude);
                                    }
                                    if ((straightLineStartPosition + straightLinePushAwayVector).magnitude < minSpawnDistanceFromPlayer)
                                    {
                                        // Start position of line is too close to frog
                                        straightLinePushAwayVector = shapePositionRelativeToFrog.normalized * (minSpawnDistanceFromPlayer - (straightLineStartPosition + straightLinePushAwayVector).magnitude);
                                    }
                                    if ((straightLineEndPosition + straightLinePushAwayVector).magnitude < minSpawnDistanceFromPlayer)
                                    {
                                        // End position of line is too close to frog
                                        straightLinePushAwayVector = shapePositionRelativeToFrog.normalized * (minSpawnDistanceFromPlayer - (straightLineEndPosition + straightLinePushAwayVector).magnitude);
                                    }

                                    // Translate the line further
                                    shapePositionRelativeToFrog += straightLinePushAwayVector;
                                    straightLineStartPosition += straightLinePushAwayVector;
                                    straightLineEndPosition += straightLinePushAwayVector;

                                    StartCoroutine(SpawnLineOfEnemiesAsync(straightLineStartPosition, straightLineEndPosition, enemyPrefab, spawnBugsAmount, enemyData, enemySpawn.movePattern, currentWave, spawnPattern, currentDelay, spawnMultipleDelayBetweenSpawns, difficultyTier,
                                            forceMovementDirection: forceMovementDirection, moveDirection: movementDirection));
                                    break;
                                case SpawnShape.WAVE_LINE:
                                    float waveLineAngle = spawnShapeLineAngle + shapeOrientationAngle;
                                    Vector3 waveLineDirectionVector = Mathf.Cos(waveLineAngle * Mathf.Deg2Rad) * Vector3.right + Mathf.Sin(waveLineAngle * Mathf.Deg2Rad) * Vector3.up;
                                    Vector3 waveLineNormalVector = Mathf.Cos(waveLineAngle * Mathf.Deg2Rad) * Vector3.up - Mathf.Sin(waveLineAngle * Mathf.Deg2Rad) * Vector3.right;
                                    Vector3 waveLineStartPosition = shapePositionRelativeToFrog - waveLineDirectionVector * (spawnShapeLineLength / 2);
                                    Vector3 waveLineEndPosition = shapePositionRelativeToFrog + waveLineDirectionVector * (spawnShapeLineLength / 2);

                                    Vector3 waveLinePushAwayVector = Vector3.zero;
                                    if (shapePositionRelativeToFrog.magnitude < minSpawnDistanceFromPlayer)
                                    {
                                        // Center of line is too close to frog
                                        waveLinePushAwayVector = shapePositionRelativeToFrog.normalized * (minSpawnDistanceFromPlayer - shapePositionRelativeToFrog.magnitude);
                                    }
                                    if ((waveLineStartPosition + waveLinePushAwayVector).magnitude < minSpawnDistanceFromPlayer)
                                    {
                                        // Start position of line is too close to frog
                                        waveLinePushAwayVector = shapePositionRelativeToFrog.normalized * (minSpawnDistanceFromPlayer - (waveLineStartPosition + waveLinePushAwayVector).magnitude);
                                    }
                                    if ((waveLineEndPosition + waveLinePushAwayVector).magnitude < minSpawnDistanceFromPlayer)
                                    {
                                        // End position of line is too close to frog
                                        waveLinePushAwayVector = shapePositionRelativeToFrog.normalized * (minSpawnDistanceFromPlayer - (waveLineEndPosition + waveLinePushAwayVector).magnitude);
                                    }

                                    // Translate the line further
                                    shapePositionRelativeToFrog += waveLinePushAwayVector;
                                    waveLineStartPosition += waveLinePushAwayVector;
                                    waveLineEndPosition += waveLinePushAwayVector;

                                    for (int spawnCount = 0; spawnCount < spawnBugsAmount; spawnCount++)
                                    {
                                        // Find position of next bug on the line
                                        spawnPosition = waveLineStartPosition + (spawnCount * 1.0f / (spawnBugsAmount - 1)) * (waveLineEndPosition - waveLineStartPosition);
                                        spawnPosition += waveLineNormalVector * spawnShapeWaveLineAmplitude * Mathf.Sin((((spawnCount * 1.0f / (spawnBugsAmount - 1)) * 360) * spawnShapeWaveLineFrequency + spawnShapeWaveLineOffset) * Mathf.Deg2Rad);

                                        // Spawn bug
                                        StartCoroutine(TrySpawnEnemyAsync(enemyPrefab, spawnPosition, enemyData, enemySpawn.movePattern, currentWave, spawnPattern, currentDelay, difficultyTier,
                                            forceMovementDirection: forceMovementDirection, moveDirection: movementDirection));

                                        // Eventually increase the delay before a spawn (WARNING: delay will change how the shape looks)
                                        currentDelay += spawnMultipleDelayBetweenSpawns;
                                    }
                                    break;
                                case SpawnShape.POLYGON:

                                    if (spawnShapePolygonNumberOfSides < 3)
                                    {
                                        spawnShapePolygonNumberOfSides = 3; // polygon must be at least a triangle, otherwise it'll break
                                    }

                                    // spawnShapeLineAngle == 0 means aligned with vector from frog
                                    float polygonAngle = Vector3.Angle(Vector3.right, shapePositionRelativeToFrog) + spawnShapePolygonOrSpriteAngle;
                                    // On top of that, we add the orientation angle that may have been randomized
                                    polygonAngle += shapeOrientationAngle;

                                    // Polygon radius is the radius or the circumscribed circle (the circle outside of the polygon)
                                    // We'll also use the inscribed circle (the circle inside of the polygon)
                                    float polygonInscribedCircleRadius = spawnShapePolygonOrSpriteRadius * Mathf.Cos(Mathf.PI / spawnShapePolygonNumberOfSides);
                                    if (spawnShapeCenteredOnFrog)
                                    {
                                        // Polygon is around frog, the radius of its inscribed circle must be big enough to circle the frog
                                        if (polygonInscribedCircleRadius < minSpawnDistanceFromPlayer)
                                        {
                                            spawnShapePolygonOrSpriteRadius = minSpawnDistanceFromPlayer / Mathf.Cos(Mathf.PI / spawnShapePolygonNumberOfSides);
                                        }
                                    }
                                    else
                                    {
                                        // If shape is not centered on frog, we want to make sure it spawns far enough so no part of the polygon is visible
                                        shapePositionRelativeToFrog += ((shapePositionRelativeToFrog.normalized) * spawnShapePolygonOrSpriteRadius);
                                    }

                                    int bugAmountOnOneSide = spawnBugsAmount / spawnShapePolygonNumberOfSides;
                                    for (int polygonSideCount = 0; polygonSideCount < spawnShapePolygonNumberOfSides; polygonSideCount++)
                                    {
                                        Vector3 polygonLineStartPosition = new Vector3 ( spawnShapePolygonOrSpriteRadius * Mathf.Cos(polygonAngle * Mathf.Deg2Rad + 2 * Mathf.PI * polygonSideCount / spawnShapePolygonNumberOfSides) + shapePositionRelativeToFrog.x, spawnShapePolygonOrSpriteRadius * Mathf.Sin(polygonAngle * Mathf.Deg2Rad + 2 * Mathf.PI * polygonSideCount / spawnShapePolygonNumberOfSides) + shapePositionRelativeToFrog.y);
                                        Vector3 polygonLineEndPosition = new Vector3(spawnShapePolygonOrSpriteRadius * Mathf.Cos(polygonAngle * Mathf.Deg2Rad + 2 * Mathf.PI * (polygonSideCount+1) / spawnShapePolygonNumberOfSides) + shapePositionRelativeToFrog.x, spawnShapePolygonOrSpriteRadius * Mathf.Sin(polygonAngle * Mathf.Deg2Rad + 2 * Mathf.PI * (polygonSideCount + 1) / spawnShapePolygonNumberOfSides) + shapePositionRelativeToFrog.y);

                                        StartCoroutine(SpawnLineOfEnemiesAsync(polygonLineStartPosition, polygonLineEndPosition, enemyPrefab, bugAmountOnOneSide, enemyData, enemySpawn.movePattern, currentWave, spawnPattern, currentDelay, spawnMultipleDelayBetweenSpawns, difficultyTier,
                                                forceMovementDirection: forceMovementDirection, moveDirection: movementDirection));

                                        currentDelay += spawnMultipleDelayBetweenSpawns * bugAmountOnOneSide;
                                    }
                                    break;
                                case SpawnShape.SPRITE:

                                    float spriteDistanceBetweenSpawns = spawnShapePolygonOrSpriteRadius / Mathf.Max(spawnShapeSprite.texture.width, spawnShapeSprite.texture.height);
                                    Color[] spritePixelsArray = spawnShapeSprite.texture.GetPixels();

                                    shapePositionRelativeToFrog += spawnShapePolygonOrSpriteRadius * shapePositionRelativeToFrog.normalized;

                                    float spriteAngle = spawnShapePolygonOrSpriteAngle;
                                    // On top of that, we add the orientation angle that may have been randomized
                                    spriteAngle += shapeOrientationAngle;
                                    Vector3 spriteRightDirectionVector = Mathf.Cos(spriteAngle * Mathf.Deg2Rad) * Vector3.right + Mathf.Sin(spriteAngle * Mathf.Deg2Rad) * Vector3.up;
                                    Vector3 spriteUpDirectionVector = Mathf.Cos(spriteAngle * Mathf.Deg2Rad) * Vector3.up - Mathf.Sin(spriteAngle * Mathf.Deg2Rad) * Vector3.right;

                                    for (int spriteY = 0; spriteY < spawnShapeSprite.texture.height; spriteY++)
                                    {
                                        for (int spriteX = 0; spriteX < spawnShapeSprite.texture.width; spriteX++)
                                        {
                                            Color pixelColor = spritePixelsArray[spriteY * spawnShapeSprite.texture.width + spriteX];
                                            if (pixelColor.a > 0.5f)
                                            {
                                                spawnPosition = shapePositionRelativeToFrog + ((spriteX - spawnShapeSprite.texture.width / 2.0f) * spriteRightDirectionVector + (spriteY - spawnShapeSprite.texture.height / 2.0f) * spriteUpDirectionVector) * spriteDistanceBetweenSpawns;

                                                // Spawn a bug on white pixels
                                                StartCoroutine(TrySpawnEnemyAsync(enemyPrefab, spawnPosition, enemyData, enemySpawn.movePattern, currentWave, spawnPattern, currentDelay, difficultyTier,
                                                    forceMovementDirection: forceMovementDirection, moveDirection: movementDirection));
                                            }
                                        }
                                    }
                                    break;
                            }
                            break;
                        case SpawnPatternType.RANDOM:
                            // Spawn enemies at random positions
                            for (int j = 0; j < spawnBugsAmount; j++)
                            {
                                if (GetSpawnPosition(GameManager.instance.player.transform.position, GameManager.instance.player.GetMoveDirection(), out spawnPosition, overrideDirection: spawnOverrideDefaultSpawnPosition, overridenDirectionAngle: spawnPositionAngle, overridenDirectionSpread: spawnPositionSpread))
                                {
                                    Vector3 positionRelativeToFrog = spawnPosition - GameManager.instance.player.transform.position;
                                    StartCoroutine(TrySpawnEnemyAsync(enemyPrefab, positionRelativeToFrog, enemyData, enemySpawn.movePattern, currentWave, spawnPattern, currentDelay, difficultyTier));
                                    currentDelay += spawnMultipleDelayBetweenSpawns;
                                }
                            }
                            break;
                    }
                    lastSpawnTimesList[i] = Time.time;
                }
            }

            if (verbose == VerboseLevel.MAXIMAL && showLog)
            {
                Debug.Log(log);
            }
        }
    }

    private void ResetEnemyValues(EnemyInstance enemyInstance, GameObject prefab, EnemyData enemyData, EnemyMovePattern movePattern, WaveData originWave, SpawnPattern originSpawnPattern, int difficultyTier, bool neverDespawn = false, BountyBug bounty = null, bool forceMovementDirection = false, Vector2? moveDirection = null, bool preventAnyPhysicsShenanigans = false)
    {
        enemyInstance.movePattern = movePattern;
        enemyInstance.spawnPattern = originSpawnPattern;

        if (!preventAnyPhysicsShenanigans)
        {
            enemyInstance.enemyTransform.localScale = prefab.GetComponent<Transform>().localScale;
            enemyInstance.enemyCollider.GetComponent<CircleCollider2D>().radius = prefab.GetComponent<CircleCollider2D>().radius;
            enemyInstance.enemyRigidbody.mass = prefab.GetComponent<Rigidbody2D>().mass;
            enemyInstance.mass = prefab.GetComponent<Rigidbody2D>().mass;

            // Set Velocity if needed
            if (forceMovementDirection && moveDirection.HasValue)
            {
                enemyInstance.moveDirection = moveDirection.Value;
                SetEnemyVelocity(enemyInstance, 0);
            }
        }

        // setup enemy - enemy info
        EnemyInfo enemyInfo = enemiesData.enemiesList.FirstOrDefault(x => x.enemyName.Equals(enemyData.enemyName));
        if (enemyInfo == null)
        {
            Debug.LogWarning("Initializing enemy instance but EnemyInfo is null!");
        }
        enemyInstance.enemyInfo = enemyInfo;

        // setup enemy - data
        enemyInstance.enemyInfo.enemyData = enemyData;

        enemyInstance.enemyAnimator.runtimeAnimatorController = prefab.GetComponent<Animator>().runtimeAnimatorController;
        enemyInstance.enemyAnimator.SetBool("IsDead", false);

        enemyInstance.enemyRenderer.sprite = prefab.GetComponent<SpriteRenderer>().sprite;
        enemyInstance.enemyRenderer.color = prefab.GetComponent<SpriteRenderer>().color;
        enemyInstance.enemyRenderer.sortingOrder = prefab.GetComponent<SpriteRenderer>().sortingOrder;
        enemyInstance.enemyRenderer.sortingOrder = bounty == null ? prefab.GetComponent<SpriteRenderer>().sortingOrder : prefab.GetComponent<SpriteRenderer>().sortingOrder + 1; // Bounties get put on layer + 1 to not be hidden under normal bugs.

        enemyInstance.enemyTransform.gameObject.layer = prefab.gameObject.layer;

        // Then set parameters
        enemyInstance.wave = originWave;
        enemyInstance.difficultyTier = difficultyTier;
        enemyInstance.neverDespawn = neverDespawn;
        enemyInstance.spawnPattern = originSpawnPattern;

        // Set bounty
        SetBountyToEnemy(enemyInstance, bounty); // This will set HP Max, multipliers and bonuses

        // Set current HP to HPMax
        enemyInstance.HP = enemyInstance.HPMax;

        // Set outline
        int outlineThickness = enemyData.outlineThickness;
        Color outlineColor = enemyData.outlineColor;
        if (bounty != null)
        {
            outlineThickness = bountyOutlineThickness;
            outlineColor = bountyOutlineColorsList[0];
        }

        if (outlineThickness > 0)
        {
            enemyInstance.SetOutlineColor(outlineColor, outlineThickness);
        }
        else
        {
            enemyInstance.RemoveOutline();
        }

        if (verbose == VerboseLevel.MAXIMAL)
        {
            Debug.Log($"ResetEnemyValues {enemyData.enemyName} with move pattern {movePattern.movePatternType.ToString()}.");
        }
    }


    private EnemyInstance DequeueEnemyInstance(bool recyclingAllowed = true)
    {
        EnemyInstance enemyInstance;
        if (!inactiveEnemiesPool.TryDequeue(out enemyInstance) && recyclingAllowed)
        {
            // Couldn't dequeue an enemy (pool must be empty)

            // Let's recycle an enemy that's out of sight
            EnemyInstance oldestEnemyInstance = null;
            float lowestSpawnTime = Time.time;
            foreach (KeyValuePair<int, EnemyInstance> enemyInfo in allActiveEnemiesDico)
            {
                if (enemyInfo.Value.active && enemyInfo.Value.alive && Vector3.Distance(GameManager.instance.player.transform.position, enemyInfo.Value.enemyTransform.position) > minSpawnDistanceFromPlayer && enemyInfo.Value.spawnTime < lowestSpawnTime)
                {
                    lowestSpawnTime = enemyInfo.Value.spawnTime;
                    oldestEnemyInstance = enemyInfo.Value;
                }
            }

            if (oldestEnemyInstance != null)
            {
                allActiveEnemiesDico.Remove(oldestEnemyInstance.enemyID);
                PutEnemyInThePool(oldestEnemyInstance);
                inactiveEnemiesPool.TryDequeue(out enemyInstance);
            }
        }
        return enemyInstance;
    }

    public void SpawnEnemy(GameObject prefab, Vector3 positionRelativeToFrog, EnemyData enemyData, EnemyMovePattern movePattern, WaveData originWave, SpawnPattern originSpawnPattern, int difficultyTier, bool neverDespawn = false, BountyBug bounty = null, bool forceMovementDirection = false, Vector2? moveDirection = null)
    {
        // Get an enemy from the pool
        EnemyInstance enemyFromPool = DequeueEnemyInstance(recyclingAllowed: true);
                
        if (enemyFromPool != null)
        {
            enemyFromPool.enemyAnimator.enabled = true;
            enemyFromPool.enemyCollider.enabled = true;
            enemyFromPool.enemyRenderer.enabled = true;
            enemyFromPool.enemyRigidbody.simulated = true;

            ResetEnemyValues(enemyFromPool, prefab, enemyData, movePattern, originWave, originSpawnPattern, difficultyTier, neverDespawn, bounty);

            Vector3 position = GameManager.instance.player.transform.position + positionRelativeToFrog;
            enemyFromPool.enemyTransform.position = position;
            enemyFromPool.enemyTransform.rotation = Quaternion.Euler(0, 0, Random.Range(0, 4) * 90); // Set random orientation (so static plants don't always face the same way)

            if (verbose == VerboseLevel.MAXIMAL)
            {
                Debug.Log($"Spawning a {enemyData.enemyName} with move pattern {movePattern.movePatternType.ToString()}. Total amount of active enemies = {allActiveEnemiesDico.Count + 1}");
            }

            AddEnemy(enemyFromPool, enemyData, movePattern, forceMovementDirection, moveDirection);
        }
    }

    public void CheatSpawnRandomBugs(int count, int tier = 0, float speed = 1)
    {
        if (verbose == VerboseLevel.MAXIMAL)
        {
            Debug.Log($"CheatSpawnRandomBugs count {count} tier {tier}");
        }

        // Prepare spawn pattern (follow player)
        EnemyMovePattern movePatternFollowPlayer = new EnemyMovePattern(EnemyMovePatternType.FOLLOW_PLAYER, speedFactor: speed);

        EnemyType randomEnemyType = EnemyType.FLY;
        int difficultyTier = 1;
        EnemyData enemyData;
        GameObject enemyPrefab;
        for (int i = 0; i < count; i++)
        {
            // Pick random type of bug
            do
            {
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
            if (GetSpawnPosition(GameManager.instance.player.transform.position, GameManager.instance.player.GetMoveDirection(), out Vector3 spawnPosition))
            {
                Vector3 positionRelativeToFrog = spawnPosition - GameManager.instance.player.transform.position;
                StartCoroutine(TrySpawnEnemyAsync(enemyPrefab, positionRelativeToFrog, enemyData, movePatternFollowPlayer, originWave: RunManager.instance.GetCurrentWave(), originSpawnPattern: null, delay: 0, difficultyTier: difficultyTier));
            }
        }
    }

    private IEnumerator SpawnLineOfEnemiesAsync(Vector3 lineStartPosition, Vector3 lineEndPosition, GameObject enemyPrefab, int spawnBugsAmount, EnemyData enemyData, EnemyMovePattern movePattern, WaveData currentWave, SpawnPattern spawnPattern, float delay, float delayBetweenSpawns, int difficultyTier,
                                            bool forceMovementDirection = false, Vector2? moveDirection = null)
    {
        yield return new WaitForSeconds(delay);

        Vector3 spawnPosition;
        float currentDelay = 0;
        for (int spawnCount = 0; spawnCount < spawnBugsAmount; spawnCount++)
        {
            // Find position of next bug on the line
            spawnPosition = lineStartPosition + (spawnCount * 1.0f / spawnBugsAmount) * (lineEndPosition - lineStartPosition);

            // Spawn bug
            StartCoroutine(TrySpawnEnemyAsync(enemyPrefab, spawnPosition, enemyData, movePattern, currentWave, spawnPattern, currentDelay, difficultyTier,
                forceMovementDirection: forceMovementDirection, moveDirection: moveDirection));

            // Eventually increase the delay before a spawn (WARNING: delay will change how the shape looks)
            currentDelay += delayBetweenSpawns;
        }
    }

    private IEnumerator TrySpawnEnemyAsync(GameObject prefab, Vector3 positionRelativeToFrog, EnemyData enemyData, EnemyMovePattern movePattern, WaveData originWave, SpawnPattern originSpawnPattern, float delay, int difficultyTier, bool neverDespawn = false, BountyBug bounty = null, bool forceMovementDirection = false, Vector2? moveDirection = null)
    {
        yield return new WaitForSeconds(delay);

        if ((originWave != null && originWave.Equals(RunManager.instance.GetCurrentWave())) || (bounty != null))
        {
            if (IsSpawnPositionFreeFromCollisions(GameManager.instance.player.transform.position + positionRelativeToFrog, prefab.layer))
            {
                // Only spawn the enemy if it's part of the current wave and that wave is still active
                SpawnEnemy(prefab, positionRelativeToFrog, enemyData, movePattern, originWave, originSpawnPattern, difficultyTier, neverDespawn, bounty, forceMovementDirection, moveDirection);
            }
        }
    }

    public void InitializeWave(WaveData wave)
    {
        lastSpawnTimesList.Clear();
        float time = -100; // or Time.time;
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
        bountyBug = null;
        if (int.TryParse(gameObjectName, out int ID))
        {
            EnemyInstance enemyInstance = GetEnemyInstanceFromID(ID);
            bountyBug = enemyInstance.bountyBug; // Get potential bounty
            return enemiesDataFromNameDico[enemyInstance.enemyName];
        }
        else
        {
            Debug.LogWarning($"GetEnemyDataFromGameObjectName couldn't find enemy with name: {gameObjectName}");
        }
        return null;
    }

    private void SetBountyToEnemy(EnemyInstance enemyInstance, BountyBug bounty = null)
    {
        // Set bounty (or absence of bounty)
        enemyInstance.bountyBug = bounty;

        // Bounty multipliers
        float hpMultiplier = 1;
        float damageMultiplier = 1;
        float xpMultiplier = 1;
        float knockbackResistance = enemyInstance.enemyInfo.enemyData.knockbackResistance;
        if (bounty != null)
        {
            // HP Multiplier
            if (bounty.overrideHealthMultiplier)
            {
                hpMultiplier = bounty.healthMultiplier;
            }
            else
            {
                hpMultiplier = bountyDefaultHealthMultiplier;
            }

            // Damage multiplier
            if (bounty.overrideDamageMultiplier)
            {
                damageMultiplier = bounty.damageMultiplier;
            }
            else
            {
                damageMultiplier = bountyDefaultDamageMultiplier;
            }

            // XP Multiplier
            if (bounty.overrideXpMultiplier)
            {
                xpMultiplier = bounty.xpMultiplier;
            }
            else
            {
                xpMultiplier = bountyDefaultXPMultiplier;
            }

            // Knockback resistance
            if (bounty.overrideKnockbackResistance)
            {
                knockbackResistance = bounty.knockbackResistance;
            }
            else
            {
                knockbackResistance += bountyDefaultKnockbackResistance;
            }
        }

        // Set HP Max
        enemyInstance.HPMax = (enemyInstance.enemyInfo.enemyData.maxHP * hpMultiplier);

        // Game mode HP multiplier
        enemyInstance.HPMax *= RunManager.instance.gameModeBugHPMultiplier;

        enemyInstance.damageMultiplier = damageMultiplier;
        enemyInstance.xpMultiplier = xpMultiplier;
        enemyInstance.knockbackResistance = knockbackResistance;
    }

    public void AddEnemy(EnemyInstance newEnemy, EnemyData enemyData, EnemyMovePattern movePattern, bool forceMovementDirection, Vector2? moveDirection)
    {
        // setup enemy - name
        newEnemy.enemyName = enemyData.enemyName;
        lastKey++;
        newEnemy.enemyTransform.gameObject.name = lastKey.ToString();

        // add enemy to dico
        newEnemy.enemyID = lastKey;
        allActiveEnemiesDico.Add(lastKey, newEnemy);
        MusicManager.instance.AdjustTensionLevel(allActiveEnemiesDico.Count);

        // setup enemy - state
        newEnemy.HP = newEnemy.HPMax;
        newEnemy.active = true;
        newEnemy.alive = true;
        newEnemy.spawnTime = Time.time;

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
        if (forceMovementDirection && moveDirection.HasValue)
        {
            newEnemy.moveDirection = moveDirection.Value;
            switch (movePattern.movePatternType)
            {
                case EnemyMovePatternType.BOUNCE_ON_EDGES: // diagonal 45? movement, somewhat towards player
                                                           // Special move pattern where this enemy should not interact with anything, and would be displayed on top of everything
                    newEnemy.enemyCollider.isTrigger = true;
                    newEnemy.enemyRenderer.sortingOrder = 1000;
                    // Always follow a diagonal
                    newEnemy.moveDirection = (new Vector2(Mathf.Sign(moveDirection.Value.x), Mathf.Sign(moveDirection.Value.y))).normalized;
                    newEnemy.bounceCount = movePattern.bouncecount;
                    break;
                case EnemyMovePatternType.DIRECTIONLESS:
                    newEnemy.enemyCollider.isTrigger = false;
                    break;
                case EnemyMovePatternType.NO_MOVEMENT:
                    newEnemy.enemyCollider.isTrigger = false;
                    newEnemy.moveDirection = Vector2.zero;
                    break;
                case EnemyMovePatternType.FOLLOW_PLAYER:
                    newEnemy.enemyCollider.isTrigger = false;
                    break;
                case EnemyMovePatternType.STRAIGHT_LINE: // but still moving towards player
                    newEnemy.enemyCollider.isTrigger = true;
                    break;
            }
        }
        else
        {
            Vector2 vectorTowardsPlayer = (GameManager.instance.player.transform.position - newEnemy.enemyTransform.position).normalized;
            switch (movePattern.movePatternType)
            {
                case EnemyMovePatternType.BOUNCE_ON_EDGES: // diagonal 45? movement, somewhat towards player
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

        float randomizedDamage = Mathf.Floor(10 * damage * Random.Range(0.9f, 1.1f)) / 10.0f; // round up to nearest 0.1
        enemy.HP -= randomizedDamage;

        bool knockback = false;
        bool vampireEffect = false;

        float visualDamageAmount = randomizedDamage * 10;
        int visualDamageAmountInt = Mathf.RoundToInt(visualDamageAmount);

        // Display damage text
        GameObject damageText = null;
        if (SettingsManager.instance.showDamageText && (visualDamageAmountInt > 0 || applyCurse) && damageTextsPool.TryDequeue(out damageText))
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

            // Font settings
            TMP_FontAsset currentFontAsset = SettingsManager.instance.GetCurrentFontAsset();
            damageTMPScript.font = currentFontAsset;

            if (verbose == VerboseLevel.MAXIMAL)
            {
                Debug.Log($"currentFontAsset.name = {currentFontAsset.name}");
            }

            bool isPixelFont = currentFontAsset.name.Equals("Thintel");
            bool isAccessibleFont = currentFontAsset.name.Equals("OpenDyslexic-Regular");
            foreach (Transform damageTextChild in damageTMPScript.transform)
            {
                damageTextChild.GetComponent<MeshRenderer>().enabled = isPixelFont;
                damageTextChild.gameObject.layer = LayerMask.NameToLayer("Default");
            }
            float fontSize = isPixelFont ? damageTextFontSize_PixelFont : (isAccessibleFont ? damageTextFontSize_AccessibleFont : damageTextFontSize_BoringFont);

            damageTMPScript.gameObject.layer = isPixelFont ? LayerMask.NameToLayer("Default") : LayerMask.NameToLayer("Overlay layer on game camera");

            Color transparentColor = new Color(0, 0, 0, 0);
            Color blackOutlineColor = new Color(0.09411f, 0.09804f, 0.12157f, 1);

            if (isPixelFont)
            {
                damageText.GetComponent<TextMeshPro>().fontMaterial.DisableKeyword("UNDERLAY_ON");
                damageText.GetComponent<TextMeshPro>().fontMaterial.SetFloat(ShaderUtilities.ID_UnderlayDilate, 0f);
                damageText.GetComponent<TextMeshPro>().fontMaterial.SetColor(ShaderUtilities.ID_UnderlayColor, transparentColor);
            }
            else if (isAccessibleFont)
            {
                damageText.GetComponent<TextMeshPro>().fontMaterial.EnableKeyword("UNDERLAY_ON");
                damageText.GetComponent<TextMeshPro>().fontMaterial.SetFloat(ShaderUtilities.ID_UnderlayDilate, 0.5f);
                damageText.GetComponent<TextMeshPro>().fontMaterial.SetColor(ShaderUtilities.ID_UnderlayColor, blackOutlineColor);
            }
            else
            {
                damageText.GetComponent<TextMeshPro>().fontMaterial.EnableKeyword("UNDERLAY_ON");
                damageText.GetComponent<TextMeshPro>().fontMaterial.SetFloat(ShaderUtilities.ID_UnderlayDilate, 0.8f);
                damageText.GetComponent<TextMeshPro>().fontMaterial.SetColor(ShaderUtilities.ID_UnderlayColor, blackOutlineColor);
            }

            if (poisonSource)
            {
                // poison damage
                damageTMPScript.color = damageTextColor_poison;
            }
            else if (visualDamageAmount < damageTextThreshold_medium)
            {
                // Smol text
                damageTMPScript.color = damageTextColor_smol;
            }
            else if (visualDamageAmount < damageTextThreshold_big)
            {
                // Medium text
                damageTMPScript.color = damageTextColor_medium;
            }
            else if (visualDamageAmount < damageTextThreshold_huge)
            {
                // Big text
                damageTMPScript.color = damageTextColor_big;
            }
            else
            {
                // Huge text
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
            damageTMPScript.sortingOrder = sortedOrderLayer + 1;

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

        // Bounty outline color
        if (enemy.bountyBug != null && bountyOutlineThickness > 0)
        {
            /*
            float enemyMaxHP = enemy.enemyInfo.enemyData.maxHP;
            if (enemy.bountyBug.overrideHealthMultiplier)
            {
                enemyMaxHP *= enemy.bountyBug.healthMultiplier;
            }
            else
            {
                enemyMaxHP *= bountyDefaultHealthMultiplier;
            }*/

            int outlineColorIndex = Mathf.FloorToInt(((enemy.HPMax - enemy.HP) / enemy.HPMax) * bountyOutlineColorsList.Count);
            outlineColorIndex = Mathf.Clamp(outlineColorIndex, 0, bountyOutlineColorsList.Count - 1);
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
                float knockbackStrengthMassRatio = Mathf.Clamp(enemy.enemyRigidbody.mass / 8, 1, 20);
                float knockbackForce = weapon.GetComponent<WeaponBehaviour>().knockbackForce / knockbackStrengthMassRatio;

                // knockback resistance from enemy
                knockbackForce = Mathf.Clamp(knockbackForce - enemy.knockbackResistance, 0, 100);

                if (knockbackForce > 0)
                {
                    enemy.Knockback(knockbackDirection, knockbackForce, knockbackDuration);
                }
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
        damageText.GetComponent<ParticleSystem>().Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
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
            MusicManager.instance.AdjustTensionLevel(allActiveEnemiesDico.Count);
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

        if (verbose == VerboseLevel.MAXIMAL)
        {
            Debug.Log($"EnemiesManager - ClearAllEnemies(): Enemies remaining = {allActiveEnemiesDico.Count}");
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
                        enemy.RemovePoison();
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

                            // Todo: does XP increase with Figurine curse?
                            // XPEarned *= (1 + GameManager.instance.player.GetCurse());

                            RunManager.instance.EatEnemy(XPEarned);
                            enemiesToDestroyIDList.Add(enemy.enemyID);
                        }
                    }
                    else
                    {
                        // Enemy is alive

                        bool chapterTimerHasEnded = RunManager.instance.IsChapterTimeOver();

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

                            // Special case: this enemy always despawns if chapter time is over
                            despawnEnemy |= chapterTimerHasEnded;

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
                                if (enemy.spawnPattern != null && GetSpawnPosition(GameManager.instance.player.transform.position, GameManager.instance.player.GetMoveDirection(), out Vector3 spawnPosition, overrideDirection: enemy.spawnPattern.overrideDefaultSpawnPosition, overridenDirectionAngle: enemy.spawnPattern.spawnPositionAngle, overridenDirectionSpread: enemy.spawnPattern.spawnPositionSpread))
                                {
                                    // Teleport
                                    enemy.enemyTransform.position = spawnPosition;

                                    // Reset status effects
                                    enemy.ForceStopAllStatusEffects();
                                }

                                // Make sure its direction is reset
                                switch (enemy.movePattern.movePatternType)
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
                            // Enemy is in range, it is not despawned

                            // Special case for random enemies: they start following the frog if it goes too close
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
                                        if (chapterTimerHasEnded)
                                        {
                                            // Goes away from frog
                                            enemy.moveDirection = (enemy.enemyTransform.position - playerTransform.position).normalized;
                                            SetEnemyVelocity(enemy, enemyUpdateDeltaTime, RunManager.instance.enemiesSpeedFactorAfterEndOfChapter);
                                        }
                                        else
                                        {
                                            // Stay in the same direction
                                            SetEnemyVelocity(enemy, enemyUpdateDeltaTime);
                                        }
                                        break;
                                    case EnemyMovePatternType.FOLLOW_PLAYER:
                                        if (chapterTimerHasEnded)
                                        {
                                            // Goes away from frog
                                            enemy.moveDirection = (enemy.enemyTransform.position - playerTransform.position).normalized;
                                            SetEnemyVelocity(enemy, enemyUpdateDeltaTime, RunManager.instance.enemiesSpeedFactorAfterEndOfChapter);
                                        }
                                        else
                                        {
                                            // Follow frog
                                            enemy.moveDirection = (playerTransform.position - enemy.enemyTransform.position).normalized;
                                            SetEnemyVelocity(enemy, enemyUpdateDeltaTime);
                                        }
                                        break;
                                    case EnemyMovePatternType.BOUNCE_ON_EDGES:
                                        if (chapterTimerHasEnded)
                                        {
                                            // Goes away from frog
                                            enemy.moveDirection = (enemy.enemyTransform.position - playerTransform.position).normalized;
                                            SetEnemyVelocity(enemy, enemyUpdateDeltaTime, RunManager.instance.enemiesSpeedFactorAfterEndOfChapter);
                                        }
                                        else if (enemy.bounceCount > 0)
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
                                            SetEnemyVelocity(enemy, enemyUpdateDeltaTime);
                                        }
                                        else
                                        {
                                            // Stay in the same direction
                                            SetEnemyVelocity(enemy, enemyUpdateDeltaTime);
                                        }
                                        break;
                                    case EnemyMovePatternType.DIRECTIONLESS:
                                        if (chapterTimerHasEnded)
                                        {
                                            // Goes away from frog
                                            enemy.moveDirection = (enemy.enemyTransform.position - playerTransform.position).normalized;
                                            SetEnemyVelocity(enemy, enemyUpdateDeltaTime, RunManager.instance.enemiesSpeedFactorAfterEndOfChapter);
                                        }
                                        else if (Time.time - enemy.lastChangeOfDirectionTime > delayBetweenRandomChangeOfDirection)
                                        {
                                            // Choose new random direction
                                            enemy.moveDirection = Random.insideUnitCircle.normalized;
                                            enemy.lastChangeOfDirectionTime = Time.time;
                                            SetEnemyVelocity(enemy, enemyUpdateDeltaTime);
                                        }
                                        else
                                        {
                                            // Stay in the same direction
                                            SetEnemyVelocity(enemy, enemyUpdateDeltaTime);
                                        }
                                        break;
                                }
                            }

                            // Poison Damage
                            enemy.DecreasePoisonRemainingTimes(enemyUpdateDeltaTime);
                            if (!enemy.IsAnyPoisonActive() && !applyGlobalPoison)
                            {
                                // No poison effect
                                enemy.lastPoisonDamageTime = float.MinValue;
                                UpdateStatusParticles(enemy);
                            }
                            else if (Time.time - enemy.lastPoisonDamageTime > delayBetweenPoisonDamage)
                            {
                                // Poison effect is on, and cooldown since last poison damage is over
                                float poisonDamage = enemy.GetActivePoisonDamage();
                                if (applyGlobalPoison)
                                {
                                    poisonDamage = Mathf.Max(0.1f, poisonDamage);
                                }
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
                    MusicManager.instance.AdjustTensionLevel(allActiveEnemiesDico.Count);
                }
            }
        }
    }

    private void PutEnemyInThePool(EnemyInstance enemy)
    {
        inactiveEnemiesPool.Enqueue(enemy);
        enemy.ForceStopAllStatusEffects();
        enemy.HP = 10000;
        enemy.spawnTime = -100;
        enemy.RemovePoison();
        enemy.enemyTransform.name = pooledEnemyNameStr;
        enemy.active = false;
        enemy.enemyTransform.position = DataManager.instance.GetFarAwayPosition();
        enemy.enemyRenderer.enabled = false;
        enemy.enemyRigidbody.simulated = false;
        enemy.enemyAnimator.enabled = false;
        enemy.enemyCollider.enabled = false;
        enemy.bountyBug = null;

        enemy.xpMultiplier = 1;
        enemy.damageMultiplier = 1;
        enemy.knockbackResistance = 0;
    }

    public void AddPoisonDamageToEnemy(string enemyGoName, float poisonDamage, float poisonDuration)
    {
        int enemyIndex = int.Parse(enemyGoName);
        if (allActiveEnemiesDico.ContainsKey(enemyIndex))
        {
            EnemyInstance enemy = allActiveEnemiesDico[enemyIndex];
            AddPoisonDamageToEnemy(enemy, poisonDamage, poisonDuration);
        }
    }

    public void AddPoisonDamageToEnemy(EnemyInstance enemy, float poisonDamage, float poisonDuration)
    {
        enemy.AddPoisonDamage(poisonDamage, poisonDuration);
        enemy.lastPoisonDamageTime = Time.time;
        SetOverlayColor(enemy, poisonedOverlayColor, 0.3f);
        UpdateStatusParticles(enemy);
    }

    public void ApplyFreezeEffect(string enemyGoName, float duration)
    {
        int enemyIndex = int.Parse(enemyGoName);
        if (allActiveEnemiesDico.ContainsKey(enemyIndex))
        {
            EnemyInstance enemy = allActiveEnemiesDico[enemyIndex];
            ApplyFreezeEffect(enemy, duration);
        }
    }

    public void ApplyFreezeEffect(EnemyInstance enemy, float duration)
    {
        // SPECIAL CASE: CURRENT PLAYED CHAPTER MAY HAVE A FREEZE DURATION MULTIPLIER
        float realDuration = duration * RunManager.instance.currentChapter.chapterData.freezeDurationMultiplier;

        enemy.freezeRemainingTime = realDuration;
        SetOverlayColor(enemy, frozenOverlayColor, 0.3f);
        UpdateStatusParticles(enemy);
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

    public void StopAndResetAllGlobalEffects()
    {
        if (SetGlobalFreezeEffectCoroutine != null)
        {
            StopCoroutine(SetGlobalFreezeEffectCoroutine);
        }
        if (SetGlobalPoisonEffectCoroutine != null)
        {
            StopCoroutine(SetGlobalPoisonEffectCoroutine);
        }
        if (SetGlobalCurseEffectCoroutine != null)
        {
            StopCoroutine(SetGlobalCurseEffectCoroutine);
        }
        applyGlobalFreeze = false;
        applyGlobalPoison = false;
        applyGlobalCurse = false;
    }

    public void ApplyCurseEffect(string enemyGoName, float duration)
    {
        int enemyIndex = int.Parse(enemyGoName);
        if (allActiveEnemiesDico.ContainsKey(enemyIndex))
        {
            EnemyInstance enemy = allActiveEnemiesDico[enemyIndex];
            ApplyCurseEffect(enemy, duration);
        }
    }

    public void ApplyCurseEffect(EnemyInstance enemy, float duration)
    {
        enemy.curseRemainingTime = duration;
        SetOverlayColor(enemy, cursedOverlayColor, 0.3f);
        UpdateStatusParticles(enemy);
    }

    public void SwitchTierOfEnemy(string enemyGoName, int deltaTier, float explosionDuration)
    {
        int enemyIndex = int.Parse(enemyGoName);
        if (allActiveEnemiesDico.ContainsKey(enemyIndex))
        {
            EnemyInstance enemyInstance = allActiveEnemiesDico[enemyIndex];
            SwitchTierOfEnemy(enemyInstance, deltaTier, explosionDuration);
        }
    }


    /// <summary>
    /// Reset an enemy to another enemy of the same Kind, with a different tier.
    /// If tier is <= 1 and tier should decrease, then the enemy is killed and XP is spawned
    /// If tier is >= 5 and tier should increase, then the enemy is turned into a bounty of the same tier
    /// If the enemy was already a bounty and tier should decrease, then its bounty is removed but the enemy is kept (health reset though)
    /// If the enemy was already a bounty and tier should increase, then it is healed to max HP
    /// </summary>
    /// <param name="enemyGoName"></param>
    /// <param name="deltaTier"></param>
    public void SwitchTierOfEnemy(EnemyInstance enemyInstance, int deltaTier, float explosionDuration)
    {
        if (verbose == VerboseLevel.MAXIMAL)
        {
            Debug.Log($"SwitchTierOfEnemy {enemyInstance.enemyName}, delta tier {deltaTier}.");
        }

        int newDifficultyTier = enemyInstance.difficultyTier + deltaTier;
        EnemyData originEnemyData = GetEnemyDataFromTypeAndDifficultyTier(enemyInstance.enemyInfo.enemyData.enemyType, enemyInstance.difficultyTier);
        EnemyData newEnemyData = GetEnemyDataFromTypeAndDifficultyTier(enemyInstance.enemyInfo.enemyData.enemyType, Mathf.Clamp(newDifficultyTier, 1, 5));
        EnemyMovePattern enemyMovePattern = new EnemyMovePattern(enemyInstance.movePattern);

        Vector3 enemyPosition = enemyInstance.enemyTransform.position;
        Vector2 enemyMoveDirection = enemyInstance.moveDirection;

        if (enemyInstance.bountyBug != null)
        {
            // Current enemy has a bounty
            if (deltaTier < 0)
            {
                // Leveling down: the bounty is removed but the enemy keeps the same tier
                SetBountyToEnemy(enemyInstance, null); // This will set new HP Max, mutipliers and bonuses
                enemyInstance.HP = enemyInstance.HPMax;
                enemyInstance.RemoveOutline();
            }
            else
            {
                // Leveling up: the bounty is healed to full life
                enemyInstance.HP = enemyInstance.HPMax;
                enemyInstance.SetOutlineColor(bountyOutlineColorsList[0], bountyOutlineThickness);
            }
        }
        else
        {
            // No bounty
            if (newDifficultyTier <= 0)
            {
                // Unspawn that enemy                
                PutEnemyInThePool(enemyInstance);
                allActiveEnemiesDico.Remove(enemyInstance.enemyID);
                MusicManager.instance.AdjustTensionLevel(allActiveEnemiesDico.Count);

                // Spawn XP instead
                float XPEarned = Mathf.Clamp(originEnemyData.xPBonus / 2, 1, 100);

                CollectiblesManager.instance.SpawnCollectible(enemyPosition, CollectibleType.XP_BONUS, XPEarned);
            }
            else if (newDifficultyTier <= 5)
            {
                // Reset the enemy to its new difficulty tier
                ResetEnemyValues(enemyInstance, newEnemyData.prefab, newEnemyData, enemyMovePattern, enemyInstance.wave, enemyInstance.spawnPattern, newDifficultyTier, forceMovementDirection: true, moveDirection: enemyMoveDirection, preventAnyPhysicsShenanigans: false);
                //StartCoroutine(TrySpawnEnemyAsync(enemyPrefab, enemyPosition, newEnemyData, enemyMovePattern, enemyInstance.wave, delay: 0.01f, newDifficultyTier, forceMovementDirection: true, moveDirection: enemyMoveDirection));
            }
            else
            {
                // New tier is above 5, which means we replace the previous enemy by an enemy of the same tier with a bounty
                BountyBug bountyBug = new BountyBug(originEnemyData.enemyType,
                    overrideHealthMultiplier: false, healthMultiplier: 0,
                    overrideDamageMultiplier: false, damageMultiplier: 0,
                    overrideXpMultiplier: false, xpMultiplier: 0,
                    overrideKnockbackResistance: false, knockbackResistance: 0, movePattern: enemyMovePattern,
                    overrideRewards: false);
                /*bountyBug.bountyList.Add(new Bounty() { collectibleType = CollectibleType.FROINS, amount = 20, value = 1 });
                bountyBug.bountyList.Add(new Bounty() { collectibleType = CollectibleType.FROINS, amount = 20, value = 5 });
                bountyBug.bountyList.Add(new Bounty() { collectibleType = CollectibleType.FROINS, amount = 10, value = 10 });
                bountyBug.bountyList.Add(new Bounty() { collectibleType = CollectibleType.XP_BONUS, amount = 10, value = 10 });
                bountyBug.bountyList.Add(new Bounty() { collectibleType = CollectibleType.XP_BONUS, amount = 10, value = 25 });
                bountyBug.bountyList.Add(new Bounty() { collectibleType = CollectibleType.XP_BONUS, amount = 10, value = 50 });*/
                ResetEnemyValues(enemyInstance, originEnemyData.prefab, originEnemyData, enemyMovePattern, enemyInstance.wave, enemyInstance.spawnPattern, enemyInstance.difficultyTier, neverDespawn: true, bounty: bountyBug, forceMovementDirection: true, moveDirection: enemyMoveDirection, preventAnyPhysicsShenanigans: false);
                //StartCoroutine(TrySpawnEnemyAsync(originEnemyData.prefab, enemyPosition, originEnemyData, enemyMovePattern, enemyInstance.wave, delay: 0.01f, difficultyTier: enemyInstance.difficultyTier, neverDespawn: true, bounty: bountyBug, forceMovementDirection: true, moveDirection: enemyMoveDirection));
            }

        }
    }


    private void SetEnemyVelocity(EnemyInstance enemy, float updateDeltaTime, float speedFactorOverride = -1)
    {
        // Set speed factor
        float changeSpeedFactor = 1;

        if (speedFactorOverride != -1)
        {
            changeSpeedFactor = speedFactorOverride;
        }

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


        if (changeSpeedFactor > 0 && enemy.movePattern != null && enemy.movePattern.movePatternType != EnemyMovePatternType.NO_MOVEMENT)
        {
            // Set orientation
            float angle = -Vector2.SignedAngle(enemy.moveDirection, Vector2.right);
            float roundedAngle = -90 + Mathf.RoundToInt(angle / 90) * 90;
            enemy.enemyTransform.rotation = Quaternion.Euler(0, 0, roundedAngle);
        }

        // Set speed
        EnemyData enemyData = GetEnemyDataFromGameObjectName(enemy.enemyTransform.name, out BountyBug bountyBug);
        float enemyDataSpeed = 1;
        if (enemyData != null)
        {
            enemyDataSpeed = enemyData.moveSpeed;
        }
        else if (enemy.enemyInfo != null && enemy.enemyInfo.enemyData != null)
        {
            enemyDataSpeed = enemy.enemyInfo.enemyData.moveSpeed;
        }
        float actualSpeed = enemyDataSpeed * changeSpeedFactor;
        if (enemy.movePattern != null)
        {
            actualSpeed *= enemy.movePattern.speedFactor;
        }

        // Game mode speed multiplier
        actualSpeed *= RunManager.instance.gameModeBugSpeedMultiplier;

        float maximumSpeed = 8;
        bool clampToMaxSpeed = true;
        if (bountyBug != null && bountyBug.ignoreMaxSpeed)
        {
            clampToMaxSpeed = false;
        }
        actualSpeed = Mathf.Clamp(actualSpeed, 0, clampToMaxSpeed ? maximumSpeed : 10000);
        enemy.enemyRigidbody.velocity = enemy.moveDirection * actualSpeed;

        if (enemy.movePattern != null && enemy.movePattern.movePatternType == EnemyMovePatternType.NO_MOVEMENT)
        {
            // Force animation to play at speed 1
            enemy.enemyAnimator.SetFloat("Speed", 1);
        }
        else
        {
            // Play animation at whatever true speed is
            enemy.enemyAnimator.SetFloat("Speed", actualSpeed);
        }
    }

    private void SetOverlayColor(EnemyInstance enemyInstance, Color color, float delay)
    {
        if (SettingsManager.instance.showFlashingEffects)
        {
            enemyInstance.SetOverlayColor(color);
            if (enemyInstance.removeOverlayCoroutine != null)
            {
                StopCoroutine(enemyInstance.removeOverlayCoroutine);
                enemyInstance.removeOverlayCoroutine = null;
            }
            StartCoroutine(RemoveOverlayColorAsync(enemyInstance, delay));
        }
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
            if (applyGlobalPoison || enemyInstance.IsAnyPoisonActive())
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
            List<CollectibleType> rewardList = new List<CollectibleType>();
            // Default rewards
            rewardList.Add(CollectibleType.XP_BONUS);
            rewardList.Add(CollectibleType.FROINS);
            rewardList.Add(CollectibleType.HEALTH);
            if (enemyInstance.bountyBug.overrideRewards)
            {
                // Override rewards
                rewardList = new List<CollectibleType>(enemyInstance.bountyBug.rewardsList);
            }
            if (rewardList.Count <= 0)
            {
                rewardList.Add(CollectibleType.XP_BONUS); // failsafe
            }

            // Spawn all collectibles
            // The amount depends on bounty tier
            float delay = 0;
            int rewardCount = enemyInstance.difficultyTier * 20;
            if (verbose == VerboseLevel.MAXIMAL)
            {
                Debug.Log($"Spawn bounty reward. Reward count = {rewardCount}");
            }
            int count = 0;
            while (count <= rewardCount)
            {
                CollectibleType randomType = CollectibleType.XP_BONUS;
                float bonusValue = 5;
                float typeRollValue = Random.Range(0, 1.0f);
                bool spawnReward = false;
                if (typeRollValue > 0.99f && rewardList.Contains(CollectibleType.LEVEL_UP))
                {
                    randomType = CollectibleType.LEVEL_UP;
                    bonusValue = 1;
                    spawnReward = true;
                }
                else if (typeRollValue > 0.9f && rewardList.Contains(CollectibleType.HEALTH))
                {
                    randomType = CollectibleType.HEALTH;
                    bonusValue = (Random.Range(0, 5) == 0) ? 20 : 100;
                    spawnReward = true;
                }
                else if (typeRollValue > 0.6f && rewardList.Contains(CollectibleType.FROINS))
                {
                    randomType = CollectibleType.FROINS;
                    bonusValue = (Random.Range(0, 3) == 0) ? 5 : 10;
                    spawnReward = true;
                }
                else if (typeRollValue <= 0.6f && rewardList.Contains(CollectibleType.XP_BONUS))
                {
                    randomType = CollectibleType.XP_BONUS;
                    bonusValue = Random.Range(1, 12) * 10;
                    bonusValue = Mathf.Clamp(bonusValue, 10, 100);
                    spawnReward = true;
                }
                if (spawnReward)
                {
                    StartCoroutine(CollectiblesManager.instance.SpawnCollectibleAsync(delay, enemyInstance.enemyTransform.position, randomType, bonusValue, pushAwayForce: Random.Range(8, 13)));
                    delay += delayBetweenBountyRewardSpawn;
                    count++;
                }
            }

            // Play SFX
            SoundManager.instance.PlayEatBountySound();
            // Increase bounty eaten count by 1 for that chapter.
            RunManager.instance.IncreaseBountyEatCount(1);
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
            float RealXPEarner = XPEarned * enemyInstance.xpMultiplier;

            float xpCounter = RealXPEarner;
            Vector2 randomPointInCircle = Vector2.zero;
            do
            {
                float xpSpawn = Mathf.Min(xpCounter, 100);
                xpCounter -= xpSpawn;
                Vector3 randomizedSpawnPosition = xpSpawnPosition + randomPointInCircle.x * Vector3.right + randomPointInCircle.y * Vector3.up;
                CollectiblesManager.instance.SpawnCollectible(randomizedSpawnPosition, CollectibleType.XP_BONUS, xpSpawn);
                randomPointInCircle = 0.5f * Random.insideUnitCircle;
            } while (xpCounter > 0);

            // Spawn Froins (a chance to get froins when killing a bug)
            float probabilityToSpawn1SmolFroin = DataManager.instance.baseCurrencyProbabilitySpawnFromBugs * (1 + GameManager.instance.player.GetCurrencyBoost()); // worth 1 Froin

            // Game mode froins multiplier
            probabilityToSpawn1SmolFroin *= RunManager.instance.gameModeFroinsMultiplier;

            float probabilityToSpawn1BigFroin = probabilityToSpawn1SmolFroin / 10; // worth 5 Froins
            float spawnFroinsRoll = Random.Range(0, 1f);
            float valueOfFroinSpawned = 0;
            if (spawnFroinsRoll <= probabilityToSpawn1BigFroin)
            {
                valueOfFroinSpawned = 5;
            }
            else if (spawnFroinsRoll <= probabilityToSpawn1SmolFroin)
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
            MusicManager.instance.AdjustTensionLevel(allActiveEnemiesDico.Count);
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

    public List<List<EnemyInstance>> GetActiveEnemiesSplitByDistanceToFrog(float maxDistance, int splitsCount)
    {
        // Initialize empty list
        List<List<EnemyInstance>> result = new List<List<EnemyInstance>>();
        for (int i = 0; i < splitsCount; i++)
        {
            result.Add(new List<EnemyInstance>());
        }

        // Sort every active enemy into the lists
        Vector3 frogPosition = RunManager.instance.player.transform.position;
        float distanceToFrog = 0;
        int listIndex = 0;
        foreach (KeyValuePair<int, EnemyInstance> enemyKeyValuePair in allActiveEnemiesDico)
        {
            distanceToFrog = Vector3.Distance(frogPosition, enemyKeyValuePair.Value.enemyTransform.position);
            if (distanceToFrog >= maxDistance)
            {
                continue;
            }
            listIndex = Mathf.FloorToInt(distanceToFrog * splitsCount / maxDistance);
            result[listIndex].Add(enemyKeyValuePair.Value);
        }

        return result;
    }

}
