using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Froguelike_SpawnManager : MonoBehaviour
{
    public List<GameObject> spawnPrefabsList;

    public float delayBetweenSpawns;

    public Transform spawnParent;

    private bool spawnActive;
    private float lastSpawnTime;

    public float distanceFromPlayer = 10;

    public void Spawn()
    {
        lastSpawnTime = Time.time;
        GameObject prefab = spawnPrefabsList[Random.Range(0, spawnPrefabsList.Count)];
        Vector2 onUnitCircle = Random.insideUnitCircle.normalized * distanceFromPlayer;
        Vector3 position = Froguelike_GameManager.instance.player.transform.position + onUnitCircle.x * Vector3.right + onUnitCircle.y * Vector3.up;
        GameObject newSpawn = Instantiate(prefab, position, Quaternion.identity, spawnParent);
        Froguelike_FliesManager.instance.AddFly(newSpawn.transform);
    }

    // Start is called before the first frame update
    void Start()
    {
        spawnActive = true;
    }

    // Update is called once per frame
    void Update()
    {
        if (spawnActive && (Time.time - lastSpawnTime) > delayBetweenSpawns)
        {
            Spawn();
        }
    }
}
