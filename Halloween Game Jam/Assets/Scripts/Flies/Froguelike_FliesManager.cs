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
    public Animator enemyAnimator;
    public bool active;
}

public class Froguelike_FliesManager : MonoBehaviour
{
    public static Froguelike_FliesManager instance;

    public Transform enemiesParent;

    public static int lastKey;

    public Froguelike_Wave currentWave;

    public List<Froguelike_EnemyData> enemiesDataList;
    private Dictionary<string, Froguelike_EnemyData> enemiesDataDico;
    
    private Dictionary<int, Froguelike_EnemyInstance> allActiveEnemiesDico;

    public GameObject damageTextPrefab;

    private List<float> lastSpawnTimesList;

    public float spawnDistanceFromPlayer = 15;

    public void TrySpawnCurrentWave()
    {
        for (int i = 0; i < currentWave.spawnDelays.Count; i++)
        {
            float delayBetweenSpawns = currentWave.spawnDelays[i];
            Froguelike_SpawnPattern spawnPattern = currentWave.spawnPatterns[i];
            Froguelike_EnemyData enemyData = currentWave.spawnEnemies[i];
            float lastSpawnTime = lastSpawnTimesList[i];

            if ((Time.time - lastSpawnTime) > delayBetweenSpawns)
            {
                int enemyAmount = spawnPattern.spawnAmount;
                Froguelike_SpawnPatternType patternType = spawnPattern.spawnPatternType;
                
                GameObject enemyPrefab = enemyData.prefab;

                int enemyCount = spawnPattern.spawnAmount;
                Vector2 onUnitCircle = Random.insideUnitCircle.normalized * spawnDistanceFromPlayer;
                Vector3 position = Froguelike_GameManager.instance.player.transform.position + onUnitCircle.x * Vector3.right + onUnitCircle.y * Vector3.up;

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
                            position = Froguelike_GameManager.instance.player.transform.position + (Mathf.Cos(angle*Mathf.Deg2Rad) * Vector3.right + Mathf.Sin(angle * Mathf.Deg2Rad) * Vector3.up) * spawnDistanceFromPlayer;
                            SpawnEnemy(enemyPrefab, position, enemyData);
                        }
                        break;
                    case Froguelike_SpawnPatternType.RANDOM:
                        // choose a position at a distance from the player and spawn just one enemy
                        SpawnEnemy(enemyPrefab, position, enemyData);
                        break;
                }

                lastSpawnTimesList[i] = Time.time;
            }

        }
    }

    private void SpawnEnemy(GameObject prefab, Vector3 position, Froguelike_EnemyData enemyData)
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

    public Froguelike_EnemyData GetEnemyDataFromName(string name)
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
        enemiesDataDico = new Dictionary<string, Froguelike_EnemyData>();
        foreach (Froguelike_EnemyData enemyData in enemiesDataList)
        {
            enemiesDataDico.Add(enemyData.ID, enemyData);
        }

        lastKey = 1;

        allActiveEnemiesDico = new Dictionary<int, Froguelike_EnemyInstance>();
        InvokeRepeating("UpdateAllEnemies", 0, 0.1f);
    }

    private void Update()
    {
        if (Froguelike_GameManager.instance.isGameRunning)
        {
            TrySpawnCurrentWave();
        }
    }

    public void AddEnemy(Transform enemyTransform, Froguelike_EnemyData enemyData)
    {
        Froguelike_EnemyInstance newEnemy = new Froguelike_EnemyInstance();

        // setup enemy
        newEnemy.EnemyDataID = enemyData.ID;
        newEnemy.enemyRenderer = enemyTransform.GetComponent<SpriteRenderer>();
        newEnemy.enemyAnimator = enemyTransform.GetComponent<Animator>();
        newEnemy.enemyTransform = enemyTransform;
        newEnemy.HP = enemyData.maxHP;
        newEnemy.enemyRigidbody = enemyTransform.GetComponent<Rigidbody2D>();
        newEnemy.active = true;
        lastKey++;
        enemyTransform.gameObject.name = lastKey.ToString();
        allActiveEnemiesDico.Add(lastKey, newEnemy);

        // set starting velocity (always moving towards player)
        if (enemyData.movePattern == Froguelike_EnemyMovePattern.STRAIGHTLINE)
        {
            newEnemy.moveDirection = (Froguelike_GameManager.instance.player.transform.position - newEnemy.enemyTransform.position).normalized;
            SetEnemyVelocity(newEnemy);
        }
    }

    // Return true if enemy dieded
    public bool DamageEnemy(string enemyGoName, float damage)
    {
        int index = int.Parse(enemyGoName);
        Froguelike_EnemyInstance enemy = allActiveEnemiesDico[index];
        enemy.HP -= damage;

        if (enemy.HP <= 0)
        {
            // enemy died, let's eat it now
            enemy.enemyTransform.rotation = Quaternion.Euler(0, 0, 45);
            return true;
        }
        else
        {
            // if enemy didn't die, then display damage text
            Vector2 position = enemy.enemyTransform.position;
            GameObject damageText = Instantiate(damageTextPrefab, position, Quaternion.identity, null);
            damageText.GetComponent<TMPro.TextMeshPro>().text = Mathf.RoundToInt(damage).ToString();
            Destroy(damageText, 1.0f);
        }

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
            Destroy(allActiveEnemiesDico[id].enemyTransform.gameObject, .1f);
            allActiveEnemiesDico.Remove(id);
        }
    }

    public void UpdateAllEnemies()
    {
        if (Froguelike_GameManager.instance.isGameRunning)
        {
            Transform playerTransform = Froguelike_GameManager.instance.player.transform;
            List<int> enemiesToDestroyIDList = new List<int>();
            foreach (KeyValuePair<int, Froguelike_EnemyInstance> enemyInfo in allActiveEnemiesDico)
            {
                Froguelike_EnemyInstance enemy = enemyInfo.Value;
                Froguelike_EnemyData enemyData = enemiesDataDico[enemy.EnemyDataID];
                if (enemy.active)
                {
                    if (enemy.HP < 0.01f)
                    {
                        // enemy is dead
                        enemy.moveDirection = (playerTransform.position - enemy.enemyTransform.position).normalized;
                        enemy.enemyRigidbody.velocity = 2 * enemy.moveDirection * Froguelike_GameManager.instance.player.landSpeed;
                        float distanceWithPlayer = Vector2.Distance(playerTransform.position, enemy.enemyTransform.position);
                        if (distanceWithPlayer < 1)
                        {
                            enemy.enemyRenderer.enabled = false;
                            enemy.active = false;
                            Froguelike_GameManager.instance.EatFly(enemyData.xPBonus);
                            enemiesToDestroyIDList.Add(enemyInfo.Key);
                        }
                    }
                    else
                    {
                        // enemy is alive
                        switch (enemyData.movePattern)
                        {
                            case Froguelike_EnemyMovePattern.NO_MOVEMENT:
                                enemy.enemyRigidbody.velocity = Vector2.zero;
                                break;
                            case Froguelike_EnemyMovePattern.STRAIGHTLINE:
                                break;
                            case Froguelike_EnemyMovePattern.TARGETPLAYER:
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
        float roundedAngle = Mathf.RoundToInt(angle / 90) * 90;
        enemyInstance.enemyTransform.rotation = Quaternion.Euler(0, 0, roundedAngle);
        enemyInstance.enemyRigidbody.velocity = enemyInstance.moveDirection * GetEnemyDataFromName(enemyInstance.enemyTransform.name).moveSpeed;
    }
}
