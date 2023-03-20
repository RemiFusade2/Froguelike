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

    public float lastUpdateTime;

    // References to EnemyInfo
    public EnemyInfo enemyInfo;

    // Current in-game state
    public bool active;
    public bool alive;
    public float HP;
    public Vector2 moveDirection;
    public float lastChangeOfDirectionTime;
    public int bounceCount;

    public Dictionary<string,float> weaponLastHitTimeDico;

    // Move pattern
    public EnemyMovePattern movePattern;

    // Current in-game state - poison
    public float poisonDamage;
    public float poisonRemainingTime;
    public float lastPoisonDamageTime;

    // Current in-game state - change speed
    public float changeSpeedFactor;
    public float changeSpeedRemainingTime;

    // References to Components
    public Transform enemyTransform;
    public Rigidbody2D enemyRigidbody;
    public SpriteRenderer enemyRenderer;
    public Animator enemyAnimator;
    public Collider2D enemyCollider;

    // A link to the last weapon that hit this enemy
    public Transform lastWeaponHitTransform;

    // The sprite color of that enemy when it spawned
    public Color defaultSpriteColor;

    public EnemyInstance()
    {
        weaponLastHitTimeDico = new Dictionary<string, float>();
    }

    public float GetLastHitTime(string weaponInstanceID)
    {
        weaponLastHitTimeDico.TryGetValue(weaponInstanceID, out float lastHitTime);
        return lastHitTime;
    }
    public void SetLastHitTime(string weaponInstanceID)
    {
        weaponLastHitTimeDico[weaponInstanceID] = Time.time;
    }
}

public class EnemiesManager : MonoBehaviour
{
    public static EnemiesManager instance;
    
    [Header("References")]
    public Transform enemiesParent;
    public Transform damageTextsParent;

    [Header("Data")]
    public List<EnemyTypeData> enemiesTypesDataList;

    [Header("Prefabs")]
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

    [Header("Settings - Pooling")]
    public int maxDamageTexts = 300;
    public int maxActiveEnemies = 2000;

    [Header("Settings")]
    public VerboseLevel verbose;
    [Space]
    public float updateAllEnemiesDelay = 0.1f;
    [Space]
    public Color poisonedSpriteColor;
    public Color frozenSpriteColor;
    public Color cursedSpriteColor;
    public float delayBetweenPoisonDamage = 0.6f;
    [Space]
    public float delayBetweenRandomChangeOfDirection = 1.5f;
    [Space]
    public float cooldownTimeBetweenHits = 1.0f;

    [Header("Runtime")]
    public static int lastKey;

    [Header("Runtime - saved data")]
    public EnemiesSaveData enemiesData; // Will be loaded and saved when needed

    // private
    private List<float> lastSpawnTimesList;
    private Dictionary<string, EnemyData> enemiesDataFromNameDico;
    private Dictionary<EnemyType, List<EnemyData>> enemiesDataFromTypeDico;

    private Dictionary<int, EnemyInstance> allActiveEnemiesDico;

    private Queue<EnemyInstance> inactiveEnemiesPool;
    private Queue<GameObject> damageTextsPool;

    public Vector3 farAwayPosition;


    #region Unity Callback Methods

    private void Awake()
    {
        instance = this;
        farAwayPosition = new Vector3(10000, 10000, 10000); // this should be far enough and always out of camera frustum
    }

    // Start is called before the first frame update
    void Start()
    {
        lastSpawnTimesList = new List<float>();

        // Initialize dictionaries
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

        // Instantiate all enemies in game and put them in the pool
        inactiveEnemiesPool = new Queue<EnemyInstance>();
        for (int i = 0; i < maxActiveEnemies; i++)
        {
            EnemyInstance newEnemy = new EnemyInstance();
            newEnemy.lastUpdateTime = Time.time;
            newEnemy.active = false;
            newEnemy.alive = false;

            GameObject firstEnemyPrefab = enemiesTypesDataList[0].enemiesList[0].prefab;
            GameObject enemyGameObject = Instantiate(firstEnemyPrefab, farAwayPosition, Quaternion.identity, enemiesParent);

            newEnemy.enemyTransform = enemyGameObject.transform;
            newEnemy.enemyRenderer = enemyGameObject.GetComponent<SpriteRenderer>();
            newEnemy.enemyRigidbody = enemyGameObject.GetComponent<Rigidbody2D>();
            newEnemy.enemyAnimator = enemyGameObject.GetComponent<Animator>();
            newEnemy.enemyCollider = enemyGameObject.GetComponent<Collider2D>();

            PutEnemyInThePool(newEnemy);
        }

        // Instantiate all damage text in game and put them in the pool
        damageTextsPool = new Queue<GameObject>();
        for (int i = 0; i < maxDamageTexts; i++)
        {
            GameObject damageText = Instantiate(damageTextPrefab, farAwayPosition, Quaternion.identity, damageTextsParent);
            damageTextsPool.Enqueue(damageText);
        }        

        InvokeRepeating("UpdateAllEnemies", 0, updateAllEnemiesDelay);
    }

    #endregion


    /// <summary>
    /// Returns a random spawn position around the player and in the direction of its movement.
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
            resultData = enemiesDataFromTypeDico[type][difficultyTier-1];
        }

        return resultData;
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

                if ((Time.time - lastSpawnTime) > delayBetweenSpawns)
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
                                    StartCoroutine(SpawnEnemyAsync(enemyPrefab, spawnPosition + Random.Range(-1.0f, 1.0f) * Vector2.right + Random.Range(-1.0f, 1.0f) * Vector2.up, enemyData, enemySpawn.movePattern, currentDelay));
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
                                StartCoroutine(SpawnEnemyAsync(enemyPrefab, spawnPosition, enemyData, enemySpawn.movePattern, currentDelay));
                                currentDelay += delayBetweenSpawn;
                            }
                            break;

                        case SpawnPatternType.RANDOM:
                            // Spawn enemies at random positions
                            for (int j = 0; j < enemyCount; j++)
                            {
                                if (GetSpawnPosition(GameManager.instance.player.transform.position, GameManager.instance.player.GetMoveDirection(), out spawnPosition))
                                {
                                    StartCoroutine(SpawnEnemyAsync(enemyPrefab, spawnPosition, enemyData, enemySpawn.movePattern, currentDelay));
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

    public void SpawnEnemy(GameObject prefab, Vector3 position, EnemyData enemyData, EnemyMovePattern movePattern)
    {
        // Get an enemy from the pool
        EnemyInstance enemyFromPool = null;
        if (inactiveEnemiesPool.TryDequeue(out enemyFromPool))
        {
            // Set all components values to prefab values
            enemyFromPool.enemyTransform.localScale = prefab.GetComponent<Transform>().localScale;

            enemyFromPool.enemyCollider.GetComponent<CircleCollider2D>().radius = prefab.GetComponent<CircleCollider2D>().radius;

            enemyFromPool.enemyRigidbody.mass = prefab.GetComponent<Rigidbody2D>().mass;

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

            if (verbose == VerboseLevel.MAXIMAL)
            {
                Debug.Log($"Spawning a {enemyData.enemyName} with move pattern {movePattern.movePatternType.ToString()}. Total amount of active enemies = {allActiveEnemiesDico.Count + 1}");
            }

            AddEnemy(enemyFromPool, enemyData, movePattern);
        }
    }

    private IEnumerator SpawnEnemyAsync(GameObject prefab, Vector3 position, EnemyData enemyData, EnemyMovePattern movePattern, float delay)
    {
        yield return new WaitForSeconds(delay);
        SpawnEnemy(prefab, position, enemyData, movePattern);
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
        int ID = int.Parse(gameObjectName);
        return GetEnemyInstanceFromID(ID);
    }

    public EnemyData GetEnemyDataFromGameObjectName(string gameObjectName)
    {
        int ID = int.Parse(gameObjectName);
        EnemyInstance instance = GetEnemyInstanceFromID(ID);
        return enemiesDataFromNameDico[instance.enemyName];
    }

    public void AddEnemy(EnemyInstance newEnemy, EnemyData enemyData, EnemyMovePattern movePattern)
    {
        // setup enemy - name
        newEnemy.enemyName = enemyData.enemyName;
        lastKey++;
        newEnemy.enemyTransform.gameObject.name = lastKey.ToString();
        
        // setup enemy - state
        newEnemy.HP = enemyData.maxHP * (1 + GameManager.instance.player.curse); // Max HP is affected by the curse
        newEnemy.active = true;
        newEnemy.alive = true;

        // setup enemy - misc
        newEnemy.defaultSpriteColor = newEnemy.enemyRenderer.color;
        newEnemy.movePattern = movePattern;

        // setup enemy - enemy info
        EnemyInfo enemyInfo = enemiesData.enemiesList.FirstOrDefault(x => x.enemyName.Equals(enemyData.enemyName));
        if (enemyInfo == null)
        {
            Debug.LogWarning("Initializing enemy instance but EnemyInfo is null!");
        }
        newEnemy.enemyInfo = enemyInfo;

        // add enemy to dico
        allActiveEnemiesDico.Add(lastKey, newEnemy);
        
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
        SetEnemyVelocity(newEnemy);
    }

    /// <summary>
    /// Inflict damage to the enemy with this index value. Set it to "dead" if its HP is lower than zero.
    /// </summary>
    /// <param name="enemyIndex"></param>
    /// <param name="damage"></param>
    /// <param name="weapon"></param>
    /// <returns></returns>
    public bool DamageEnemy(int enemyIndex, float damage, Transform weapon)
    {
        EnemyInstance enemy = allActiveEnemiesDico[enemyIndex];
        enemy.HP -= damage;

        enemy.SetLastHitTime(weapon.gameObject.name);

        enemy.lastWeaponHitTransform = weapon;

        // Display damage text
        GameObject damageText = null;
        if (Mathf.RoundToInt(damage) > 0 && damageTextsPool.TryDequeue(out damageText))
        {
            Vector2 position = (Vector2)enemy.enemyTransform.position + 0.1f * Random.insideUnitCircle;
            damageText.transform.position = position;
            damageText.GetComponent<TMPro.TextMeshPro>().text = Mathf.RoundToInt(damage).ToString();
            damageText.GetComponent<MeshRenderer>().enabled = true;
            StartCoroutine(PutDamageTextIntoPool(damageText, 1.0f));
        }

        bool enemyDied = false;
        if (enemy.HP < 0.01f)
        {
            // enemy died, let's eat it now
            SetEnemyDead(enemy);
            enemyDied = true;
        }

        return enemyDied;
    }

    private IEnumerator PutDamageTextIntoPool(GameObject damageText, float delay)
    {
        yield return new WaitForSeconds(delay);
        damageText.GetComponent<MeshRenderer>().enabled = false;
        damageText.transform.position = farAwayPosition;
        damageTextsPool.Enqueue(damageText);
    }

    // Return true if enemy dieded
    public bool DamageEnemy(string enemyGoName, float damage, Transform weapon)
    {
        int index = int.Parse(enemyGoName);
        return DamageEnemy(index, damage, weapon);
    }

    public void ClearAllEnemies()
    {
        List<int> enemiesToDestroyIDList = new List<int>();
        foreach (KeyValuePair<int, EnemyInstance> enemyInfo in allActiveEnemiesDico)
        {
            enemiesToDestroyIDList.Add(enemyInfo.Key);
        }
        foreach (int id in enemiesToDestroyIDList)
        {
            GameManager.instance.SpawnDestroyParticleEffect(allActiveEnemiesDico[id].enemyTransform.position);

            // do not destroy the enemy gameobject but instead deactivate it and put it back in the pool
            PutEnemyInThePool(allActiveEnemiesDico[id]);
            allActiveEnemiesDico.Remove(id);
        }
        allActiveEnemiesDico.Clear();
        lastKey = 0;
    }

    /// <summary>
    /// Update enemies
    /// </summary>
    public void UpdateAllEnemies()
    {
        if (GameManager.instance.isGameRunning)
        {
            Transform playerTransform = GameManager.instance.player.transform;

            List<KeyValuePair<int, EnemyInstance>> enemiesToUpdate = allActiveEnemiesDico.ToList();
            enemiesToUpdate = enemiesToUpdate.OrderBy(x => x.Value.lastUpdateTime).Take(50).ToList();
            
            List<int> enemiesToDestroyIDList = new List<int>();

            //foreach (KeyValuePair<int, EnemyInstance> enemyInfo in allActiveEnemiesDico)
            foreach (KeyValuePair<int, EnemyInstance> enemyInfo in enemiesToUpdate)
            {
                EnemyInstance enemy = enemyInfo.Value;
                enemy.lastUpdateTime = Time.time;
                EnemyData enemyData = enemiesDataFromNameDico[enemy.enemyName];
                EnemyMovePattern movePattern = enemy.movePattern;
                if (enemy.active)
                {
                    Vector3 frogPosition = playerTransform.position;
                    if (!enemy.alive)
                    {
                        // enemy is dead
                        enemy.poisonRemainingTime = 0;
                        enemy.poisonDamage = 0;
                        enemy.lastPoisonDamageTime = float.MinValue;
                        enemy.changeSpeedFactor = 0;
                        enemy.changeSpeedRemainingTime = 0;
                        UpdateSpriteColor(enemy);
                        if (enemy.lastWeaponHitTransform != null)
                        {
                            frogPosition = enemy.lastWeaponHitTransform.position;
                        }
                        float dot = Vector2.Dot(enemy.moveDirection, (frogPosition - enemy.enemyTransform.position).normalized);
                        float distanceWithFrog = Vector2.Distance(frogPosition, enemy.enemyTransform.position);
                        enemy.moveDirection = (frogPosition - enemy.enemyTransform.position).normalized;
                        float walkSpeed = DataManager.instance.defaultWalkSpeed * (1 + GameManager.instance.player.walkSpeedBoost);
                        enemy.enemyRigidbody.velocity = 2 * enemy.moveDirection * walkSpeed;
                        if (dot < 0 || distanceWithFrog < 1.5f)
                        {
                            enemy.enemyRenderer.enabled = false;
                            enemy.active = false;
                            RunManager.instance.EatFly(enemyData.xPBonus * (1 + GameManager.instance.player.curse), enemyData.instantlyEndChapter);
                            enemiesToDestroyIDList.Add(enemyInfo.Key);
                        }
                    }
                    else
                    {
                        // enemy is alive

                        // Check distance (if enemy is too far, unspawn it)
                        float distanceWithFrog = Vector2.Distance(frogPosition, enemy.enemyTransform.position);
                        if (distanceWithFrog > maxDistanceBeforeUnspawn)
                        {
                            enemy.enemyRenderer.enabled = false;
                            enemy.enemyCollider.enabled = false;
                            enemy.active = false;
                            enemiesToDestroyIDList.Add(enemyInfo.Key);
                        }

                        // Move                        
                        switch (movePattern.movePatternType)
                        {
                            case EnemyMovePatternType.NO_MOVEMENT:
                                enemy.enemyRigidbody.velocity = Vector2.zero;
                                break;
                            case EnemyMovePatternType.STRAIGHT_LINE:
                                SetEnemyVelocity(enemy);
                                break;
                            case EnemyMovePatternType.FOLLOW_PLAYER:
                                enemy.moveDirection = (playerTransform.position - enemy.enemyTransform.position).normalized;
                                SetEnemyVelocity(enemy);
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
                                SetEnemyVelocity(enemy);
                                break;
                            case EnemyMovePatternType.DIRECTIONLESS:
                                if (Time.time - enemy.lastChangeOfDirectionTime > delayBetweenRandomChangeOfDirection)
                                {
                                    enemy.moveDirection = Random.insideUnitCircle.normalized;
                                    enemy.lastChangeOfDirectionTime = Time.time;
                                }
                                SetEnemyVelocity(enemy);
                                break;
                        }

                        // Poison Damage
                        enemy.poisonRemainingTime -= updateAllEnemiesDelay;
                        if (enemy.poisonRemainingTime <= 0)
                        {
                            enemy.poisonRemainingTime = 0;
                            enemy.poisonDamage = 0;
                            enemy.lastPoisonDamageTime = float.MinValue;
                            UpdateSpriteColor(enemy);
                        }
                        else
                        {
                            if (Time.time - enemy.lastPoisonDamageTime > delayBetweenPoisonDamage)
                            {
                                bool enemyIsDead = DamageEnemy(enemyInfo.Key, enemy.poisonDamage, null);
                                enemy.lastPoisonDamageTime = Time.time;

                                if (enemyIsDead && !enemiesToDestroyIDList.Contains(enemyInfo.Key))
                                {
                                    enemiesToDestroyIDList.Add(enemyInfo.Key);
                                    CollectiblesManager.instance.SpawnCollectible(enemy.enemyTransform.position, CollectibleType.XP_BONUS, enemyData.xPBonus);
                                }
                            }
                        }
                    }
                }
            }
            if (verbose == VerboseLevel.MAXIMAL && enemiesToDestroyIDList.Count > 0)
            {
                Debug.Log($"Destroying {enemiesToDestroyIDList.Count} enemies. Total amount of active enemies = {allActiveEnemiesDico.Count - enemiesToDestroyIDList.Count}");
            }
            foreach (int enemyID in enemiesToDestroyIDList)
            {
                EnemyInstance enemy = allActiveEnemiesDico[enemyID];
                PutEnemyInThePool(enemy);
                allActiveEnemiesDico.Remove(enemyID);
            }
        }
    }

    private void PutEnemyInThePool(EnemyInstance enemy)
    {
        inactiveEnemiesPool.Enqueue(enemy);
        enemy.enemyTransform.name = "Pooled";
        enemy.active = false;
        enemy.enemyTransform.position = farAwayPosition;
        enemy.enemyRenderer.enabled = false;
        enemy.enemyRigidbody.simulated = false;
        enemy.enemyAnimator.enabled = false;
        enemy.enemyCollider.enabled = false;
    }

    public void AddPoisonDamageToEnemy(string enemyGoName, float poisonDamage, float poisonDuration)
    {
        int enemyIndex = int.Parse(enemyGoName);
        EnemyInstance enemy = allActiveEnemiesDico[enemyIndex];
        enemy.poisonDamage = poisonDamage;
        enemy.poisonRemainingTime = poisonDuration;
        enemy.lastPoisonDamageTime = Time.time;
        UpdateSpriteColor(enemy);
    }

    public void ChangeEnemySpeed(string enemyGoName, float speedChangeFactor, float speedChangeDuration)
    {
        int enemyIndex = int.Parse(enemyGoName);
        EnemyInstance enemy = allActiveEnemiesDico[enemyIndex];
        enemy.changeSpeedFactor = speedChangeFactor;
        enemy.changeSpeedRemainingTime = speedChangeDuration;
        UpdateSpriteColor(enemy);
    }

    private void SetEnemyVelocity(EnemyInstance enemyInstance)
    {
        float angle = -Vector2.SignedAngle(enemyInstance.moveDirection, Vector2.right);
        float roundedAngle = -90 + Mathf.RoundToInt(angle / 90) * 90;
        enemyInstance.enemyTransform.rotation = Quaternion.Euler(0, 0, roundedAngle);
        enemyInstance.changeSpeedRemainingTime -= updateAllEnemiesDelay;
        if (enemyInstance.changeSpeedRemainingTime <= 0)
        {
            enemyInstance.changeSpeedRemainingTime = 0;
            enemyInstance.changeSpeedFactor = 0;
            UpdateSpriteColor(enemyInstance);
        }
        float actualSpeed = GetEnemyDataFromGameObjectName(enemyInstance.enemyTransform.name).moveSpeed * (1 + enemyInstance.changeSpeedFactor) * enemyInstance.movePattern.speedFactor;
        float walkSpeed = DataManager.instance.defaultWalkSpeed * (1 + GameManager.instance.player.walkSpeedBoost);
        actualSpeed = Mathf.Clamp(actualSpeed, 0, 30);
        enemyInstance.enemyRigidbody.velocity = enemyInstance.moveDirection * actualSpeed;
    }

    private void UpdateSpriteColor(EnemyInstance enemyInstance)
    {
        if (enemyInstance.poisonRemainingTime > 0)
        {
            enemyInstance.enemyRenderer.color = poisonedSpriteColor;
        }
        else if (enemyInstance.changeSpeedRemainingTime > 0)
        {
            if (enemyInstance.changeSpeedFactor > 0)
            {
                enemyInstance.enemyRenderer.color = cursedSpriteColor;
            }
            else if (enemyInstance.changeSpeedFactor < 0)
            {
                enemyInstance.enemyRenderer.color = frozenSpriteColor;
            }
        }
        else
        {
            enemyInstance.enemyRenderer.color = enemyInstance.defaultSpriteColor;
        }
    }

    public void SetEnemyDead(EnemyInstance enemyInstance)
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
        enemyInstance.enemyTransform.rotation = Quaternion.Euler(0, 0, 45);
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
