using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapBehaviour : MonoBehaviour
{
    public static MapBehaviour instance;

    [Header("References")]
    public Transform mapTilesParent;

    [Header("Prefabs")]
    public GameObject backgroundTilePrefab;
    public List<GameObject> rocksPrefabs;
    public List<GameObject> watersPrefabs;

    [Header("Settings")]
    public Vector2 tileSize = new Vector2(40, 22.5f);
    public Vector2 rockMinMax;
    public Vector2 waterMinMax;
    [Space]
    public float minDistanceWithPlayer = 2;
    [Space]
    public int minCollectiblesPerTile = 1;
    public int maxCollectiblesPerTile = 4;
    public float collectibleLevelUpSpawnLikelihood = 0.01f;
    public float collectibleCurrencySpawnLikelihood = 1.0f;
    public float collectibleHealthSpawnLikelihood = 0.01f;


    private List<Vector2Int> existingTilesCoordinates;


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

        Vector2 randomPositionOnTile = GetWorldPositionOfTile(tileCoordinates) + Random.Range(-tileSize.x/2, tileSize.x/2) * Vector2.right + Random.Range(-tileSize.y/2, tileSize.y/2) * Vector2.up;

        if (!preventSpawnAtPosition || Vector2.Distance(randomPositionOnTile, preventSpawnPosition) > minDistanceWithPlayer)
        {
            Instantiate(prefabs[randomIndex], randomPositionOnTile, Quaternion.identity, mapTilesParent);
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
        float waterProba = Random.Range(waterMinMax.x, waterMinMax.y);
        float waterAmount = Mathf.Floor(waterProba) + ((Random.Range(Mathf.Floor(waterProba), Mathf.Ceil(waterProba)) < waterProba) ? 1 : 0);
        for (int i = 0; i < waterAmount; i++)
        {
            AddSomething(watersPrefabs, tileCoordinates, false, preventSpawnPosition);
        }

        // generate rocks
        float rockProba = Random.Range(rockMinMax.x, rockMinMax.y);
        float rockAmount = Mathf.Floor(rockProba) + ((Random.Range(Mathf.Floor(rockProba), Mathf.Ceil(rockProba)) < rockProba) ? 1 : 0);
        for (int i = 0; i < rockAmount; i++)
        {
            AddSomething(rocksPrefabs, tileCoordinates, preventSpawnRocksAtPosition, preventSpawnPosition);
        }

        // generate collectibles
        int collectibleCount = Random.Range(minCollectiblesPerTile, maxCollectiblesPerTile+1);
        for (int i=0; i<collectibleCount; i++)
        {
            Vector2 randomVector = Random.insideUnitCircle;
            Vector2 position = GetWorldPositionOfTile(tileCoordinates) + randomVector * (tileSize / 2.0f);
            
            int chapterMultiplicator = RunManager.instance.completedChaptersList.Count + 1;
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
