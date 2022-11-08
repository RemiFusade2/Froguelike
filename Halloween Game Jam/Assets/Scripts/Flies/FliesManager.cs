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

    public Color defaultSpriteColor;
}

public class FliesManager : MonoBehaviour
{
    public static FliesManager instance;
    
    [Header("References")]
    public Transform enemiesParent;

    [Header("Data")]
    public List<EnemyData> enemiesDataList;

    [Header("Prefabs")]
    public GameObject damageTextPrefab;

    [Header("Settings")]
    public float minSpawnDistanceFromPlayer = 15;
    public float maxSpawnDistanceFromPlayer = 17;
    public float enemyHPFactor = 1;
    public float enemySpeedFactor = 1;
    public float enemyDamageFactor = 1;
    public float enemyXPFactor = 1;
    [Range(0,0.5f)]
    public float curse = 0;
    [Space]
    public float updateAllEnemiesDelay = 0.1f;
    [Space]
    public Color poisonedSpriteColor;
    public Color frozenSpriteColor;
    public Color cursedSpriteColor;
    public float delayBetweenPoisonDamage = 0.6f;

    [Header("Runtime")]
    public Wave currentWave;
    public static int lastKey;


    // private
    private List<float> lastSpawnTimesList;
    private Dictionary<string, EnemyData> enemiesDataDico;
    private Dictionary<int, EnemyInstance> allActiveEnemiesDico;

    private Vector3 GetSpawnPosition(Vector3 playerPosition, Vector2 playerMoveDirection)
    {
        Vector3 position;
        Vector2 onUnitCircle;
        float spawnPositionDotPlayerMoveDirection;
        int triesCount = 0;
        int maxTries = 10;
        do
        {
            onUnitCircle = Random.insideUnitCircle.normalized * Random.Range(minSpawnDistanceFromPlayer, maxSpawnDistanceFromPlayer);
            position = GameManager.instance.player.transform.position + onUnitCircle.x * Vector3.right + onUnitCircle.y * Vector3.up;
            if (!playerMoveDirection.Equals(Vector2.zero))
            {
                spawnPositionDotPlayerMoveDirection = Vector3.Dot((position-playerPosition), playerMoveDirection);
            }
            else
            {
                spawnPositionDotPlayerMoveDirection = 1;
            }
            triesCount++;
            Debug.Log("triesCount = " + triesCount + " ; spawnPositionDotPlayerMoveDirection = " + spawnPositionDotPlayerMoveDirection);
        } while (spawnPositionDotPlayerMoveDirection < 0 && triesCount <= maxTries);
        return position;
    }

    public void TrySpawnCurrentWave()
    {
        for (int i = 0; i < currentWave.spawnDelays.Count; i++)
        {
            float delayBetweenSpawns = currentWave.spawnDelays[i] * (1 - curse);
            SpawnPattern spawnPattern = currentWave.spawnPatterns[i];
            EnemyData enemyData = currentWave.spawnEnemies[i];
            float lastSpawnTime = lastSpawnTimesList[i];

            if ((Time.time - lastSpawnTime) > delayBetweenSpawns)
            {
                int enemyAmount = spawnPattern.spawnAmount;
                SpawnPatternType patternType = spawnPattern.spawnPatternType;
                
                GameObject enemyPrefab = enemyData.prefab;

                int enemyCount = spawnPattern.spawnAmount;

                Vector3 position;
                switch (patternType)
                {
                    case SpawnPatternType.CHUNK:
                        // choose a position at a distance from the player and spawn a chunk of enemies
                        position = GetSpawnPosition(GameManager.instance.player.transform.position, GameManager.instance.player.GetMoveDirection());
                        for (int j = 0; j < enemyCount; j++)
                        {
                            SpawnEnemy(enemyPrefab, position + Random.Range(-1.0f,1.0f) * Vector3.right + Random.Range(-1.0f, 1.0f) * Vector3.up, enemyData);
                        }
                        break;
                    case SpawnPatternType.CIRCLE:
                        // spawn enemies all around the player
                        float deltaAngle = 360.0f / enemyCount;
                        float spawnDistanceFromPlayer = minSpawnDistanceFromPlayer;
                        for (float angle = 0; angle < 360; angle += deltaAngle)
                        {
                            position = GameManager.instance.player.transform.position + (Mathf.Cos(angle*Mathf.Deg2Rad) * Vector3.right + Mathf.Sin(angle * Mathf.Deg2Rad) * Vector3.up) * spawnDistanceFromPlayer;
                            SpawnEnemy(enemyPrefab, position, enemyData);
                        }
                        break;
                    case SpawnPatternType.RANDOM:
                        // choose a position at a distance from the player and spawn enemies
                        for (int j = 0; j < enemyCount; j++)
                        {
                            position = GetSpawnPosition(GameManager.instance.player.transform.position, GameManager.instance.player.GetMoveDirection());
                            SpawnEnemy(enemyPrefab, position, enemyData);
                        }
                        break;
                }
                lastSpawnTimesList[i] = Time.time;
            }
        }
    }

    public void SpawnEnemy(GameObject prefab, Vector3 position, EnemyData enemyData)
    {
        GameObject newSpawn = Instantiate(prefab, position, Quaternion.identity, enemiesParent);
        AddEnemy(newSpawn.transform, enemyData);
    }

    public void SetWave(Wave wave)
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

    public EnemyInstance GetEnemyInfo(int ID)
    {
        return allActiveEnemiesDico[ID];
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
        instance = this;
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
        newEnemy.HP = enemyData.maxHP * (enemyHPFactor + curse);
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

    public bool DamageEnemy(int enemyIndex, float damage, bool canKill)
    {
        EnemyInstance enemy = allActiveEnemiesDico[enemyIndex];
        enemy.HP -= damage;

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
                // enemy can't die because of tongue limit
                enemy.HP = 0.1f;
            }
        }

        // if enemy didn't die, then display damage text
        Vector2 position = (Vector2)enemy.enemyTransform.position + 0.1f * Random.insideUnitCircle;
        GameObject damageText = Instantiate(damageTextPrefab, position, Quaternion.identity, null);
        damageText.GetComponent<TMPro.TextMeshPro>().text = Mathf.CeilToInt(damage).ToString();
        Destroy(damageText, 1.0f);

        return false;
    }

    // Return true if enemy dieded
    public bool DamageEnemy(string enemyGoName, float damage, bool canKill)
    {
        int index = int.Parse(enemyGoName);
        return DamageEnemy(index, damage, canKill);
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
                    if (!enemy.alive)
                    {
                        // enemy is dead
                        enemy.poisonRemainingTime = 0;
                        enemy.poisonDamage = 0;
                        enemy.lastPoisonDamageTime = float.MinValue;
                        enemy.changeSpeedFactor = 0;
                        enemy.changeSpeedRemainingTime = 0;
                        UpdateSpriteColor(enemy);
                        enemy.moveDirection = (playerTransform.position - enemy.enemyTransform.position).normalized;
                        enemy.enemyRigidbody.velocity = 2 * enemy.moveDirection * GameManager.instance.player.landSpeed;
                        float distanceWithPlayer = Vector2.Distance(playerTransform.position, enemy.enemyTransform.position);
                        if (distanceWithPlayer < 1.5f)
                        {
                            enemy.enemyRenderer.enabled = false;
                            enemy.active = false;
                            GameManager.instance.EatFly(enemyData.xPBonus * (enemyXPFactor + curse), enemyData.instantlyEndChapter);
                            enemiesToDestroyIDList.Add(enemyInfo.Key);
                        }
                    }
                    else
                    {
                        // enemy is alive

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
                                bool enemyIsDead = DamageEnemy(enemyInfo.Key, enemy.poisonDamage, false);
                                enemy.lastPoisonDamageTime = Time.time;
                                /*if (enemyIsDead)
                                {
                                    SetEnemyDead(enemy);
                                }*/
                                //UpdateSpriteColor(enemy);
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
