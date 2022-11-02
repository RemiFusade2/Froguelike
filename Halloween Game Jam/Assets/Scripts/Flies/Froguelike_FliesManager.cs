using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Froguelike_EnemyInstance
{
    public string EnemyDataID;

    public float HP;
    public Vector2 moveDirection;
    public Transform enemyTransform;
    public Rigidbody2D enemyRigidbody;
    public SpriteRenderer enemyRenderer;
    public bool active;
}

public class Froguelike_FliesManager : MonoBehaviour
{
    public static Froguelike_FliesManager instance;
    
    [Header("References")]
    public Transform enemiesParent;

    [Header("Data")]
    public List<EnemyData> enemiesDataList;

    [Header("Prefabs")]
    public GameObject damageTextPrefab;

    [Header("Settings")]
    public float spawnDistanceFromPlayer = 15;
    public float enemyHPFactor = 1;
    public float enemySpeedFactor = 1;
    public float enemyDamageFactor = 1;
    public float enemyXPFactor = 1;
    [Range(0,0.5f)]
    public float curse = 0;

    [Header("Runtime")]
    public Froguelike_Wave currentWave;
    public static int lastKey;


    // private
    private List<float> lastSpawnTimesList;
    private Dictionary<string, EnemyData> enemiesDataDico;
    private Dictionary<int, Froguelike_EnemyInstance> allActiveEnemiesDico;

    public void TrySpawnCurrentWave()
    {
        for (int i = 0; i < currentWave.spawnDelays.Count; i++)
        {
            float delayBetweenSpawns = currentWave.spawnDelays[i] * (1 - curse);
            Froguelike_SpawnPattern spawnPattern = currentWave.spawnPatterns[i];
            EnemyData enemyData = currentWave.spawnEnemies[i];
            float lastSpawnTime = lastSpawnTimesList[i];

            if ((Time.time - lastSpawnTime) > delayBetweenSpawns)
            {
                int enemyAmount = spawnPattern.spawnAmount;
                Froguelike_SpawnPatternType patternType = spawnPattern.spawnPatternType;
                
                GameObject enemyPrefab = enemyData.prefab;

                int enemyCount = spawnPattern.spawnAmount;
                Vector2 onUnitCircle = Random.insideUnitCircle.normalized * spawnDistanceFromPlayer * Random.Range(0.8f, 1.7f);
                Vector3 position = GameManager.instance.player.transform.position + onUnitCircle.x * Vector3.right + onUnitCircle.y * Vector3.up;

                switch (patternType)
                {
                    case Froguelike_SpawnPatternType.CHUNK:
                        // choose a position at a distance from the player and spawn a chunk of enemies
                        for (int j = 0; j < enemyCount; j++)
                        {
                            SpawnEnemy(enemyPrefab, position + Random.Range(-1.0f,1.0f) * Vector3.right + Random.Range(-1.0f, 1.0f) * Vector3.up, enemyData);
                        }
                        break;
                    case Froguelike_SpawnPatternType.CIRCLE:
                        // spawn enemies all around the player
                        float deltaAngle = 360.0f / enemyCount;
                        for (float angle = 0; angle < 360; angle += deltaAngle)
                        {
                            position = GameManager.instance.player.transform.position + (Mathf.Cos(angle*Mathf.Deg2Rad) * Vector3.right + Mathf.Sin(angle * Mathf.Deg2Rad) * Vector3.up) * spawnDistanceFromPlayer;
                            SpawnEnemy(enemyPrefab, position, enemyData);
                        }
                        break;
                    case Froguelike_SpawnPatternType.RANDOM:
                        // choose a position at a distance from the player and spawn enemies
                        for (int j = 0; j < enemyCount; j++)
                        {
                            onUnitCircle = Random.insideUnitCircle.normalized * spawnDistanceFromPlayer * Random.Range(0.8f, 1.7f);
                            position = GameManager.instance.player.transform.position + onUnitCircle.x * Vector3.right + onUnitCircle.y * Vector3.up;
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

    public void SetWave(Froguelike_Wave wave)
    {
        currentWave = wave;
        lastSpawnTimesList.Clear();
        float time = Time.time;
        foreach (float delay in currentWave.spawnDelays)
        {
            lastSpawnTimesList.Add(time);
        }
    }

    public Froguelike_EnemyInstance GetEnemyInfo(int ID)
    {
        return allActiveEnemiesDico[ID];
    }
    public Froguelike_EnemyInstance GetEnemyInfo(string name)
    {
        int ID = int.Parse(name);
        return GetEnemyInfo(ID);
    }

    public EnemyData GetEnemyDataFromName(string name)
    {
        int ID = int.Parse(name);
        Froguelike_EnemyInstance instance = GetEnemyInfo(ID);
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

        allActiveEnemiesDico = new Dictionary<int, Froguelike_EnemyInstance>();
        InvokeRepeating("UpdateAllEnemies", 0, 0.1f);
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
        Froguelike_EnemyInstance newEnemy = new Froguelike_EnemyInstance();

        // setup enemy
        newEnemy.EnemyDataID = enemyData.ID;
        newEnemy.enemyRenderer = enemyTransform.GetComponent<SpriteRenderer>();
        newEnemy.enemyTransform = enemyTransform;
        newEnemy.HP = enemyData.maxHP * (enemyHPFactor + curse);
        newEnemy.enemyRigidbody = enemyTransform.GetComponent<Rigidbody2D>();
        newEnemy.active = true;
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

    // Return true if enemy dieded
    public bool DamageEnemy(string enemyGoName, float damage, bool canKill)
    {
        int index = int.Parse(enemyGoName);
        Froguelike_EnemyInstance enemy = allActiveEnemiesDico[index];
        enemy.HP -= damage;

        if (enemy.HP < 0.01f)
        {
            if (canKill)
            {
                // enemy died, let's eat it now
                enemy.enemyTransform.rotation = Quaternion.Euler(0, 0, 45);
                return true;
            }
            else
            {
                // enemy can't die because of tongue limit
                enemy.HP = 0.1f;
            }
        }
        
        // if enemy didn't die, then display damage text
        Vector2 position = (Vector2)enemy.enemyTransform.position + 0.1f*Random.insideUnitCircle;
        GameObject damageText = Instantiate(damageTextPrefab, position, Quaternion.identity, null);
        damageText.GetComponent<TMPro.TextMeshPro>().text = Mathf.CeilToInt(damage).ToString();
        Destroy(damageText, 1.0f);

        return false;
    }

    public void ClearAllEnemies()
    {
        List<int> enemiesToDestroyIDList = new List<int>();
        foreach (KeyValuePair<int, Froguelike_EnemyInstance> enemyInfo in allActiveEnemiesDico)
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
            foreach (KeyValuePair<int, Froguelike_EnemyInstance> enemyInfo in allActiveEnemiesDico)
            {
                Froguelike_EnemyInstance enemy = enemyInfo.Value;
                EnemyData enemyData = enemiesDataDico[enemy.EnemyDataID];
                if (enemy.active)
                {
                    if (enemy.HP < 0.01f)
                    {
                        // enemy is dead
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
                    }
                }
            }
            foreach (int enemyID in enemiesToDestroyIDList)
            {
                Froguelike_EnemyInstance enemy = allActiveEnemiesDico[enemyID];
                allActiveEnemiesDico.Remove(enemyID);
                Destroy(enemy.enemyTransform.gameObject, 0.1f);
            }
        }
    }

    private void SetEnemyVelocity(Froguelike_EnemyInstance enemyInstance)
    {
        float angle = -Vector2.SignedAngle(enemyInstance.moveDirection, Vector2.right);
        float roundedAngle = -90 + Mathf.RoundToInt(angle / 90) * 90;
        enemyInstance.enemyTransform.rotation = Quaternion.Euler(0, 0, roundedAngle);
        enemyInstance.enemyRigidbody.velocity = enemyInstance.moveDirection * GetEnemyDataFromName(enemyInstance.enemyTransform.name).moveSpeed * enemySpeedFactor;
    }
}
