using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapBehaviour : MonoBehaviour
{
    [Header("References")]
    public Transform mapTilesParent;

    [Header("Prefabs")]
    public GameObject backgroundTilePrefab;
    public List<GameObject> rocksPrefabs;
    public List<GameObject> watersPrefabs;

    [Header("Settings")]
    public Vector2 tileSize = new Vector2(40, 22.5f);
    [Range(0,1)]
    public float rockDensity = 0.2f;
    public int maxRockCount = 13;
    [Range(0, 1)]
    public float waterDensity = 0.2f;
    public int maxWaterCount = 13;
    [Space]
    public float minDistanceWithPlayer = 2;
    [Space]
    public int minCollectiblesPerTile = 1;
    public int maxCollectiblesPerTile = 4;
    public float collectibleLevelUpSpawnLikelihood = 0.01f;
    public float collectibleCurrencySpawnLikelihood = 1.0f;
    public float collectibleHealthSpawnLikelihood = 0.01f;


    private List<Vector2Int> existingTilesCoordinates;

    // Start is called before the first frame update
    void Start()
    {
        existingTilesCoordinates = new List<Vector2Int>();
    }

    public void ClearMap()
    {
        existingTilesCoordinates.Clear();
        List<GameObject> gameObjectsToDestroyList = new List<GameObject>();
        foreach (Transform tile in mapTilesParent)
        {
            gameObjectsToDestroyList.Add(tile.gameObject);
        }
        foreach (GameObject go in gameObjectsToDestroyList)
        {
            Destroy(go);
        }
    }

    public void ClearRocks()
    {
        List<GameObject> gameObjectsToDestroyList = new List<GameObject>();
        foreach (Transform tile in mapTilesParent)
        {
            if (tile.CompareTag("Rock"))
            {
                gameObjectsToDestroyList.Add(tile.gameObject);
            }
        }
        foreach (GameObject go in gameObjectsToDestroyList)
        {
            Destroy(go);
            GameManager.instance.SpawnDestroyParticleEffect(go.transform.position);
        }
    }

    private void AddSomething(List<GameObject> prefabs, Vector2Int tileCoordinates, bool preventSpawnAtPosition, Vector2 preventSpawnPosition)
    {
        int randomIndex = Random.Range(0, prefabs.Count);
        Vector2 randomVector = Random.insideUnitCircle;
        Vector2 position = GetWorldPositionOfTile(tileCoordinates) + randomVector * (tileSize / 2.0f);
        if (!preventSpawnAtPosition || Vector2.Distance(position, preventSpawnPosition) > minDistanceWithPlayer)
        {
            Instantiate(prefabs[randomIndex], position, Quaternion.identity, mapTilesParent);
        }
    }

    private bool DoesTileExist(Vector2Int tileCoordinates)
    {
        return existingTilesCoordinates.Contains(tileCoordinates);
    }
    private void AddTile(Vector2Int tileCoordinates, bool preventSpawnRocksAtPosition, Vector2 preventSpawnPosition)
    {
        Vector2 tileWorldPosition = tileCoordinates * tileSize;
        Instantiate(backgroundTilePrefab, tileWorldPosition, Quaternion.identity, mapTilesParent);
        // generate water
        for (int i = 0; i < waterDensity * maxWaterCount; i++)
        {
            if (Random.Range(0, 3) != 0)
            {
                AddSomething(watersPrefabs, tileCoordinates, false, preventSpawnPosition);
            }
        }
        // generate rocks
        for (int i = 0; i < rockDensity * maxRockCount; i++)
        {
            if (Random.Range(0,3) != 0)
            {
                AddSomething(rocksPrefabs, tileCoordinates, preventSpawnRocksAtPosition, preventSpawnPosition);
            }
        }

        // generate collectibles
        int collectibleCount = Random.Range(minCollectiblesPerTile, maxCollectiblesPerTile+1);
        for (int i=0; i<collectibleCount; i++)
        {
            Vector2 randomVector = Random.insideUnitCircle;
            Vector2 position = GetWorldPositionOfTile(tileCoordinates) + randomVector * (tileSize / 2.0f);

            int chapterMultiplicator = 1 + GameManager.instance.chaptersPlayed.Count;
            float randomCollectibleType = Random.Range(0, collectibleLevelUpSpawnLikelihood + collectibleCurrencySpawnLikelihood + collectibleHealthSpawnLikelihood);
            if (randomCollectibleType < collectibleLevelUpSpawnLikelihood)
            {
                CollectiblesManager.instance.SpawnCollectible(position, CollectibleType.LEVEL_UP, 1);
            }
            else if (randomCollectibleType < collectibleLevelUpSpawnLikelihood + collectibleHealthSpawnLikelihood)
            {
                CollectiblesManager.instance.SpawnCollectible(position, CollectibleType.HEALTH, 200);
            }
            else
            {
                int bonusValue = chapterMultiplicator * 10;
                CollectiblesManager.instance.SpawnCollectible(position, CollectibleType.CURRENCY, bonusValue);
            }
        }
        existingTilesCoordinates.Add(tileCoordinates);
    }

    private Vector2Int GetTileForPosition(Vector2 position)
    {
        Vector2Int coordinates = new Vector2Int(Mathf.RoundToInt(position.x / tileSize.x), Mathf.RoundToInt(position.y / tileSize.y));
        return coordinates;
    }
    private Vector2 GetWorldPositionOfTile(Vector2Int tileCoordinates)
    {
        Vector2 position = tileCoordinates * tileSize;
        return position;
    }

    public void GenerateNewTilesAroundPosition(Vector2 position)
    {
        Vector2Int centralTileCoordinates = GetTileForPosition(position);
        for (int y = -1; y <= 1; y++)
        {
            for (int x = -1; x <= 1; x++)
            {
                Vector2Int tileCoordinates = centralTileCoordinates + x * Vector2Int.right + y * Vector2Int.up;
                if (!DoesTileExist(tileCoordinates))
                {
                    AddTile(tileCoordinates, true, position);
                }
            }
        }
    }
}
