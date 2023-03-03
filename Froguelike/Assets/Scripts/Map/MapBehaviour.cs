using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// MapBehaviour is the class that generates the map as the character moves.
/// It's in charge of building the background, spawning the obstacles and collectibles.
/// </summary>
public class MapBehaviour : MonoBehaviour
{
    public static MapBehaviour instance;

    [Header("References")]
    public Transform mapTilesParent;

    [Header("Prefabs")]
    public GameObject emptyTilePrefab;
    public List<GameObject> treasureTilesPrefabsList;
    [Space]
    public List<GameObject> rocksPrefabs;
    public List<GameObject> watersPrefabs;

    [Header("Settings")]
    public Vector2 tileSize = new Vector2(40, 22.5f);
    public float minDistanceWithPlayer = 2;


    private List<Vector2Int> existingTilesCoordinates;


    private void Awake()
    {
        instance = this;
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

    private void AddSomething(List<GameObject> prefabs, Vector2Int tileCoordinates, bool preventSpawnAtPosition, Vector2 preventSpawnPosition, Transform tileParent)
    {
        int randomIndex = Random.Range(0, prefabs.Count);

        Vector2 randomPositionOnTile = GetWorldPositionOfTile(tileCoordinates) + Random.Range(-tileSize.x/2, tileSize.x/2) * Vector2.right + Random.Range(-tileSize.y/2, tileSize.y/2) * Vector2.up;

        if (!preventSpawnAtPosition || Vector2.Distance(randomPositionOnTile, preventSpawnPosition) > minDistanceWithPlayer)
        {
            Instantiate(prefabs[randomIndex], randomPositionOnTile, Quaternion.identity, tileParent);
        }
    }

    private bool DoesTileExist(Vector2Int tileCoordinates)
    {
        return existingTilesCoordinates.Contains(tileCoordinates);
    }
    private void AddTile(Vector2Int tileCoordinates, bool preventSpawnRocksAtPosition, Vector2 preventSpawnPosition)
    {
        GameObject tilePrefab = emptyTilePrefab;
        Vector2 tileWorldPosition = tileCoordinates * tileSize;

        Chapter currentPlayedChapter = RunManager.instance.currentChapter;
        if (currentPlayedChapter == null || currentPlayedChapter.chapterData == null) return;
        List<FixedCollectible> superCollectiblesList = currentPlayedChapter.chapterData.specialCollectiblesOnTheMap;
        FixedCollectible superCollectibleOnCurrentTile = null;
        if (superCollectiblesList != null)
        {
            superCollectibleOnCurrentTile = superCollectiblesList.FirstOrDefault(x => x.tileCoordinates.Equals(tileCoordinates));
        }
        if (superCollectibleOnCurrentTile != null)
        {
            // Special case when there is a super collectible on the new tile to spawn
            tilePrefab = treasureTilesPrefabsList[Random.Range(0, treasureTilesPrefabsList.Count)];
            GameObject tile = Instantiate(tilePrefab, tileWorldPosition, Quaternion.identity, mapTilesParent);

            // In that situation, we just spawn the super collectible and that's it
            // Everything else is already on the treasure tile
            CollectiblesManager.instance.SpawnSuperCollectible(superCollectibleOnCurrentTile, tileWorldPosition);
        }
        else
        {
            // If there's no collectible, then we spawn an empty tile and a bunch of random obstacles and collectbiles
            tilePrefab = emptyTilePrefab;
            GameObject tile = Instantiate(tilePrefab, tileWorldPosition, Quaternion.identity, mapTilesParent);
            
            // generate water
            Vector2 waterMinMax = DataManager.instance.GetSpawnProbability("pond", currentPlayedChapter.chapterData.pondsSpawnFrequency);
            float waterProba = Random.Range(waterMinMax.x, waterMinMax.y);
            float waterAmount = Mathf.Floor(waterProba) + ((Random.Range(Mathf.Floor(waterProba), Mathf.Ceil(waterProba)) < waterProba) ? 1 : 0);
            for (int i = 0; i < waterAmount; i++)
            {
                AddSomething(watersPrefabs, tileCoordinates, false, preventSpawnPosition, tile.transform);
            }

            // generate rocks
            Vector2 rockMinMax = DataManager.instance.GetSpawnProbability("rock", currentPlayedChapter.chapterData.rocksSpawnFrequency);
            float rockProba = Random.Range(rockMinMax.x, rockMinMax.y);
            float rockAmount = Mathf.Floor(rockProba) + ((Random.Range(Mathf.Floor(rockProba), Mathf.Ceil(rockProba)) < rockProba) ? 1 : 0);
            for (int i = 0; i < rockAmount; i++)
            {
                AddSomething(rocksPrefabs, tileCoordinates, preventSpawnRocksAtPosition, preventSpawnPosition, tile.transform);
            }

            // generate currency collectibles
            Vector2 currencyMinMax = DataManager.instance.GetSpawnProbability("currency", currentPlayedChapter.chapterData.coinsSpawnFrequency);
            float currencyProba = Random.Range(currencyMinMax.x, currencyMinMax.y);
            float currencyAmount = Mathf.Floor(currencyProba) + ((Random.Range(Mathf.Floor(currencyProba), Mathf.Ceil(currencyProba)) < currencyProba) ? 1 : 0);
            for (int i = 0; i < currencyAmount; i++)
            {
                Vector2 randomVector = Random.insideUnitCircle;
                Vector2 position = GetWorldPositionOfTile(tileCoordinates) + randomVector * (tileSize / 2.0f);
                int chapterMultiplicator = 1; // RunManager.instance.completedChaptersList.Count + 1;
                int bonusValue = chapterMultiplicator * 10;
                CollectiblesManager.instance.SpawnCollectible(position, CollectibleType.CURRENCY, bonusValue);
            }

            // generate health collectibles
            Vector2 healthMinMax = DataManager.instance.GetSpawnProbability("health", currentPlayedChapter.chapterData.healthSpawnFrequency);
            float healthProba = Random.Range(healthMinMax.x, healthMinMax.y);
            float healthAmount = Mathf.Floor(healthProba) + ((Random.Range(Mathf.Floor(healthProba), Mathf.Ceil(healthProba)) < healthProba) ? 1 : 0);
            for (int i = 0; i < healthAmount; i++)
            {
                Vector2 randomVector = Random.insideUnitCircle;
                Vector2 position = GetWorldPositionOfTile(tileCoordinates) + randomVector * (tileSize / 2.0f);
                CollectiblesManager.instance.SpawnCollectible(position, CollectibleType.HEALTH, 200);
            }

            // generate level up collectibles
            Vector2 levelUpMinMax = DataManager.instance.GetSpawnProbability("levelUp", currentPlayedChapter.chapterData.levelUpSpawnFrequency);
            float levelUpProba = Random.Range(levelUpMinMax.x, levelUpMinMax.y);
            float levelUpAmount = Mathf.Floor(levelUpProba) + ((Random.Range(Mathf.Floor(levelUpProba), Mathf.Ceil(levelUpProba)) < levelUpProba) ? 1 : 0);
            for (int i = 0; i < levelUpAmount; i++)
            {
                Vector2 randomVector = Random.insideUnitCircle;
                Vector2 position = GetWorldPositionOfTile(tileCoordinates) + randomVector * (tileSize / 2.0f);
                CollectiblesManager.instance.SpawnCollectible(position, CollectibleType.LEVEL_UP, 1);
            }
        }

        // We add that new tile in the list of existing tiles
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
