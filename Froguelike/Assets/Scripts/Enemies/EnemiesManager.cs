using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class EnemyInstance
{
    public string EnemyDataID;

    public bool active;
    public bool alive;

    public float HP;
    public Vector2 moveDirection;

    public float poisonDamage;
    public float poisonRemainingTime;
    public float lastPoisonDamageTime;

    public float changeSpeedFactor;
    public float changeSpeedRemainingTime;

    public Transform enemyTransform;
    public Rigidbody2D enemyRigidbody;
    public SpriteRenderer enemyRenderer;
    public Animator enemyAnimator;
    public Collider2D enemyCollider;

    public Transform lastWeaponHitTransform;

    public Color defaultSpriteColor;
}

public class EnemiesManager : MonoBehaviour
{
    public static EnemiesManager instance;
    
    [Header("References")]
    public Transform enemiesParent;

    [Header("Data")]
    public List<EnemyData> enemiesDataList;

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

    [Header("Settings")]
    public float enemyHPFactor = 1;
    public float enemySpeedFactor = 1;
    public float enemyDamageFactor = 1;
    public float enemyXPFactor = 1;
    public float enemySpawnSpeedFactor = 1;
    [Space]
    public float updateAllEnemiesDelay = 0.1f;
    [Space]
    public Color poisonedSpriteColor;
    public Color frozenSpriteColor;
    public Color cursedSpriteColor;
    public float delayBetweenPoisonDamage = 0.6f;
    [Space]
    public float enemyDamageIncreaseFactorPerChapter = 1.5f;
    public float enemyHPIncreaseFactorPerChapter = 2.5f;
    public float enemySpeedIncreaseFactorPerChapter = 1.1f;
    public float enemyXPIncreaseFactorPerChapter = 1.7f;
    public float enemySpawnSpeedIncreaseFactorPerChapter = 1.7f;

    [Header("Runtime")]
    public Wave currentWave;
    public static int lastKey;


    // private
    private List<float> lastSpawnTimesList;
    private Dictionary<string, EnemyData> enemiesDataDico;
    private Dictionary<int, EnemyInstance> allActiveEnemiesDico;

    public void ResetFactors()
    {
        enemyDamageFactor = 1;
        enemyHPFactor = 1;
        enemySpeedFactor = 1;
        enemyXPFactor = 1;
        enemySpawnSpeedFactor = 1;
    }

    public void IncreaseFactors()
    {
        enemyDamageFactor *= enemyDamageIncreaseFactorPerChapter;
        enemyHPFactor *= enemyHPIncreaseFactorPerChapter;
        enemySpeedFactor *= enemySpeedIncreaseFactorPerChapter;
        enemyXPFactor *= enemyXPIncreaseFactorPerChapter;
        enemySpawnSpeedFactor *= enemySpawnSpeedIncreaseFactorPerChapter;
    }

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

    public void TrySpawnCurrentWave()
    {
        if (RunManager.instance.currentChapter != null && RunManager.instance.currentChapter.chapterData != null)
        {
            double curseDelayFactor = (1 - GameManager.instance.player.curse); // curse will affect the delay negatively (lower delay = more spawns)
            curseDelayFactor = System.Math.Clamp(curseDelayFactor, 0.1, 1.0);
            double timeSinceChapterStartedFactor = 0.5 + 1.5 * (RunManager.instance.chapterRemainingTime / RunManager.instance.currentChapter.chapterData.chapterLengthInSeconds); // this factor will go from 1 to 0.5 (double spawns) near the end of chapter.
            for (int i = 0; i < currentWave.spawnDelays.Count; i++)
            {
                double delayBetweenSpawns = currentWave.spawnDelays[i] * curseDelayFactor * timeSinceChapterStartedFactor * (1 / enemySpawnSpeedFactor);
                delayBetweenSpawns = System.Math.Clamp(delayBetweenSpawns, 0.001, double.MaxValue);
                SpawnPattern spawnPattern = currentWave.spawnPatterns[i];
                EnemyData enemyData = currentWave.spawnEnemies[i];
                float lastSpawnTime = lastSpawnTimesList[i];

                if ((Time.time - lastSpawnTime) > delayBetweenSpawns)
                {
                    int enemyAmount = spawnPattern.spawnAmount;
                    SpawnPatternType patternType = spawnPattern.spawnPatternType;

                    GameObject enemyPrefab = enemyData.prefab;

                    int enemyCount = spawnPattern.spawnAmount;

                    Vector2 spawnPosition;
                    switch (patternType)
                    {
                        case SpawnPatternType.CHUNK:
                            // choose a position at a distance from the player and spawn a chunk of enemies
                            if (GetSpawnPosition(GameManager.instance.player.transform.position, GameManager.instance.player.GetMoveDirection(), out spawnPosition))
                            {
                                for (int j = 0; j < enemyCount; j++)
                                {
                                    SpawnEnemy(enemyPrefab, spawnPosition + Random.Range(-1.0f, 1.0f) * Vector2.right + Random.Range(-1.0f, 1.0f) * Vector2.up, enemyData);
                                }
                            }
                            break;
                        case SpawnPatternType.CIRCLE:
                            // spawn enemies all around the player
                            float deltaAngle = 360.0f / enemyCount;
                            float spawnDistanceFromPlayer = minSpawnDistanceFromPlayer;
                            for (float angle = 0; angle < 360; angle += deltaAngle)
                            {
                                spawnPosition = GameManager.instance.player.transform.position + (Mathf.Cos(angle * Mathf.Deg2Rad) * Vector3.right + Mathf.Sin(angle * Mathf.Deg2Rad) * Vector3.up) * spawnDistanceFromPlayer;
                                SpawnEnemy(enemyPrefab, spawnPosition, enemyData);
                            }
                            break;
                        case SpawnPatternType.RANDOM:
                            // Spawn enemies at random positions
                            for (int j = 0; j < enemyCount; j++)
                            {
                                if (GetSpawnPosition(GameManager.instance.player.transform.position, GameManager.instance.player.GetMoveDirection(), out spawnPosition))
                                {
                                    SpawnEnemy(enemyPrefab, spawnPosition, enemyData);
                                }
                            }
                            break;
                    }
                    lastSpawnTimesList[i] = Time.time;
                }
            }
        }
    }

    public void SpawnEnemy(GameObject prefab, Vector3 position, EnemyData enemyData)
    {
        GameObject newSpawn = Instantiate(prefab, position, Quaternion.identity, enemiesParent);
        AddEnemy(newSpawn.transform, enemyData);
    }

    public void SpawnStartWave(Wave wave)
    {
        currentWave = wave;
        lastSpawnTimesList.Clear();
        float time = float.MinValue; // Time.time;
        foreach (float delay in currentWave.spawnDelays)
        {
            lastSpawnTimesList.Add(time);
        }
        TrySpawnCurrentWave();
    }

    public void SetWave(Wave wave)
    {
        currentWave = wave;
        lastSpawnTimesList.Clear();
        float time = Time.time;
        foreach (float delay in currentWave.spawnDelays)
        {
            lastSpawnTimesList.Add(time);
        }
    }

    public EnemyInstance GetEnemyInfo(int ID)
    {
        EnemyInstance result = null;
        if (allActiveEnemiesDico.ContainsKey(ID))
        {
            result = allActiveEnemiesDico[ID];
        }
        return result;
    }
    public EnemyInstance GetEnemyInfo(string name)
    {
        int ID = int.Parse(name);
        return GetEnemyInfo(ID);
    }

    public EnemyData GetEnemyDataFromName(string name)
    {
        int ID = int.Parse(name);
        EnemyInstance instance = GetEnemyInfo(ID);
        return enemiesDataDico[instance.EnemyDataID];
    }

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
        enemiesDataDico = new Dictionary<string, EnemyData>();
        foreach (EnemyData enemyData in enemiesDataList)
        {
            enemiesDataDico.Add(enemyData.ID, enemyData);
        }

        lastKey = 1;

        allActiveEnemiesDico = new Dictionary<int, EnemyInstance>();
        InvokeRepeating("UpdateAllEnemies", 0, updateAllEnemiesDelay);
    }

    private void Update()
    {
        if (GameManager.instance.isGameRunning)
        {
            TrySpawnCurrentWave();
        }
    }

    public void AddEnemy(Transform enemyTransform, EnemyData enemyData)
    {
        EnemyInstance newEnemy = new EnemyInstance();

        // setup enemy
        newEnemy.EnemyDataID = enemyData.ID;
        newEnemy.enemyRenderer = enemyTransform.GetComponent<SpriteRenderer>();
        newEnemy.enemyTransform = enemyTransform;
        newEnemy.HP = enemyData.maxHP * (enemyHPFactor + GameManager.instance.player.curse);
        newEnemy.enemyRigidbody = enemyTransform.GetComponent<Rigidbody2D>();
        newEnemy.enemyAnimator = enemyTransform.GetComponent<Animator>();
        newEnemy.enemyCollider = enemyTransform.GetComponent<Collider2D>();
        newEnemy.active = true;
        newEnemy.alive = true;
        newEnemy.defaultSpriteColor = newEnemy.enemyRenderer.color;
        lastKey++;
        enemyTransform.gameObject.name = lastKey.ToString();
        allActiveEnemiesDico.Add(lastKey, newEnemy);

        // set starting velocity (always moving towards player)
        if (enemyData.movePattern == EnemyMovePattern.STRAIGHTLINE)
        {
            newEnemy.moveDirection = (GameManager.instance.player.transform.position - newEnemy.enemyTransform.position).normalized;
            SetEnemyVelocity(newEnemy);
        }
    }

    /// <summary>
    /// Inflict damage to the enemy with this index value. Set it to "dead" if its HP is lower than zero.
    /// </summary>
    /// <param name="enemyIndex"></param>
    /// <param name="damage"></param>
    /// <param name="canKill">Deprecated probably. An enemy can always die, even with poison.</param>
    /// <param name="weapon"></param>
    /// <returns></returns>
    public bool DamageEnemy(int enemyIndex, float damage, bool canKill, Transform weapon)
    {
        EnemyInstance enemy = allActiveEnemiesDico[enemyIndex];
        enemy.HP -= damage;

        enemy.lastWeaponHitTransform = weapon;

        if (enemy.HP < 0.01f)
        {
            if (canKill)
            {
                // enemy died, let's eat it now
                SetEnemyDead(enemy);
                return true;
            }
            else
            {
                // Deprecated: now if an enemy can get eaten, it will die and spawn an XP collectible
                // enemy can't die, so we leave it with very low health (it will be eaten next time it gets hit)
                enemy.HP = 0.1f;
            }
        }

        // If enemy didn't die, then display damage text
        Vector2 position = (Vector2)enemy.enemyTransform.position + 0.1f * Random.insideUnitCircle;
        GameObject damageText = Instantiate(damageTextPrefab, position, Quaternion.identity, null);
        damageText.GetComponent<TMPro.TextMeshPro>().text = Mathf.CeilToInt(damage).ToString();
        Destroy(damageText, 1.0f);

        return false;
    }

    // Return true if enemy dieded
    public bool DamageEnemy(string enemyGoName, float damage, bool canKill, Transform weapon)
    {
        int index = int.Parse(enemyGoName);
        return DamageEnemy(index, damage, canKill, weapon);
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
            Destroy(allActiveEnemiesDico[id].enemyTransform.gameObject);
            allActiveEnemiesDico.Remove(id);
        }
        allActiveEnemiesDico.Clear();
        lastKey = 0;
    }

    public void UpdateAllEnemies()
    {
        if (GameManager.instance.isGameRunning)
        {
            Transform playerTransform = GameManager.instance.player.transform;
            List<int> enemiesToDestroyIDList = new List<int>();
            foreach (KeyValuePair<int, EnemyInstance> enemyInfo in allActiveEnemiesDico)
            {
                EnemyInstance enemy = enemyInfo.Value;
                EnemyData enemyData = enemiesDataDico[enemy.EnemyDataID];
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
                        enemy.moveDirection = (frogPosition - enemy.enemyTransform.position).normalized;
                        float distanceWithFrog = Vector2.Distance(frogPosition, enemy.enemyTransform.position);
                        float walkSpeed = GameManager.instance.player.defaultWalkSpeed * (1 + GameManager.instance.player.walkSpeedBoost);
                        enemy.enemyRigidbody.velocity = 2 * enemy.moveDirection * walkSpeed;
                        if (distanceWithFrog < 1.5f)
                        {
                            enemy.enemyRenderer.enabled = false;
                            enemy.active = false;
                            RunManager.instance.EatFly(enemyData.xPBonus * (enemyXPFactor + GameManager.instance.player.curse), enemyData.instantlyEndChapter);
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
                        switch (enemyData.movePattern)
                        {
                            case EnemyMovePattern.NO_MOVEMENT:
                                enemy.enemyRigidbody.velocity = Vector2.zero;
                                break;
                            case EnemyMovePattern.STRAIGHTLINE:
                                SetEnemyVelocity(enemy);
                                break;
                            case EnemyMovePattern.TARGETPLAYER:
                                enemy.moveDirection = (playerTransform.position - enemy.enemyTransform.position).normalized;
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
                                bool enemyIsDead = DamageEnemy(enemyInfo.Key, enemy.poisonDamage, true, null);
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
            foreach (int enemyID in enemiesToDestroyIDList)
            {
                EnemyInstance enemy = allActiveEnemiesDico[enemyID];
                allActiveEnemiesDico.Remove(enemyID);
                Destroy(enemy.enemyTransform.gameObject, 0.1f);
            }
        }
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
        float actualSpeed = GetEnemyDataFromName(enemyInstance.enemyTransform.name).moveSpeed * enemySpeedFactor * (1 + enemyInstance.changeSpeedFactor);
        float walkSpeed = GameManager.instance.player.defaultWalkSpeed * (1 + GameManager.instance.player.walkSpeedBoost);
        actualSpeed = Mathf.Clamp(actualSpeed, 0, walkSpeed - 0.001f);
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

    public void SetEnemyDead(EnemyInstance enemy)
    {
        enemy.HP = 0;
        enemy.alive = false;
        enemy.enemyAnimator.SetBool("IsDead", true);
        enemy.enemyCollider.enabled = false;
        enemy.enemyTransform.rotation = Quaternion.Euler(0, 0, 45);
    }

    public void SetEnemyDead(string enemyName)
    {
        int enemyIndex = int.Parse(enemyName);
        EnemyInstance enemy = allActiveEnemiesDico[enemyIndex];
        SetEnemyDead(enemy);
    }
}
