using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

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
    [Space]
    public List<GameObject> rocksPrefabs;
    public List<GameObject> watersPrefabs;

    [Header("Settings")]
    public Vector2 tileSize = new Vector2(40, 22.5f);
    public float minDistanceWithPlayer = 2;
    [Space]
    public Vector2Int validSortingOrderForWater;
    public Vector2Int validSortingOrderForRocks;


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
            //GameManager.instance.SpawnDestroyParticleEffect(go.transform.position);
        }
    }

    private void AddSomething(List<GameObject> prefabs, Vector2Int tileCoordinates, bool preventSpawnAtPosition, Vector2 preventSpawnPosition, Transform tileParent, Vector2Int sortingOrdersRange, string layerName)
    {
        int randomIndex = Random.Range(0, prefabs.Count);

        Vector2 randomPositionOnTile = GetWorldPositionOfTile(tileCoordinates) + Random.Range(-tileSize.x/2, tileSize.x/2) * Vector2.right + Random.Range(-tileSize.y/2, tileSize.y/2) * Vector2.up;

        if (!preventSpawnAtPosition || Vector2.Distance(randomPositionOnTile, preventSpawnPosition) > minDistanceWithPlayer)
        {
            GameObject newSomethingGo = Instantiate(prefabs[randomIndex], randomPositionOnTile, Quaternion.identity, tileParent);

            // Get overlapping colliders on the same layer
            List<Collider2D> allOverlapingColliders = new List<Collider2D>();
            float maxRadiusToDetectColliders = 3.0f;
            ContactFilter2D contactFiler = new ContactFilter2D();
            contactFiler.SetLayerMask(LayerMask.GetMask(layerName));
            int contactsCount = Physics2D.OverlapCircle(randomPositionOnTile, maxRadiusToDetectColliders, contactFiler, allOverlapingColliders);

            // Prepare all valid sorting orders for that new object we spawned
            List<int> availableSortingOrders = new List<int>();
            for (int order = sortingOrdersRange.x; order <= sortingOrdersRange.y; order++)
            {
                availableSortingOrders.Add(order);
            }

            // Remove sorting orders that are already used by overlapping colliders
            foreach (Collider2D col in allOverlapingColliders)
            {
                SpriteRenderer colRenderer = col.GetComponent<SpriteRenderer>();
                if (colRenderer == null)
                {
                    colRenderer = col.GetComponentInParent<SpriteRenderer>();
                }
                if (colRenderer != null && availableSortingOrders.Contains(colRenderer.sortingOrder))
                {
                    availableSortingOrders.Remove(colRenderer.sortingOrder);
                }
            }

            // Set a sorting order that is either picked from the available ones or randomly chosen
            int sortingOrder = Random.Range(sortingOrdersRange.x, sortingOrdersRange.y + 1);
            if (availableSortingOrders.Count > 0)
            {
                sortingOrder = availableSortingOrders[Random.Range(0, availableSortingOrders.Count)];
            }
            SpriteRenderer newSomethingSpriteRenderer = newSomethingGo.GetComponent<SpriteRenderer>();
            newSomethingSpriteRenderer.sortingOrder = sortingOrder;
        }
    }

    private bool GetRandomSpawnPositionOnTile(Vector2Int tileCoordinates, out Vector2 spawnPosition)
    {
        spawnPosition = Vector2.zero;
        
        // Attemp to find a valid spawn point. Loop and try again until it works.
        bool spawnPositionIsValid = false;
        int loopAttemptCount = 10;
        Vector2 tilePosition = GetWorldPositionOfTile(tileCoordinates);
        Vector2 randomVector = Vector2.zero;
        do
        {
            // Get a random point
            randomVector = Random.insideUnitCircle;
            spawnPosition = tilePosition + randomVector * (tileSize / 2.0f);
            // Check if that random point is on an obstacle
            int layerMask = LayerMask.GetMask("Rock", "LakeCollider");
            if (Physics2D.OverlapCircle(spawnPosition, 0.1f, layerMask) == null)
            {
                spawnPositionIsValid = true;
            }
            loopAttemptCount--;
        } while (!spawnPositionIsValid && loopAttemptCount > 0);

        return spawnPositionIsValid;
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
            GameObject tile = Instantiate(superCollectibleOnCurrentTile.tilePrefab, tileWorldPosition, Quaternion.identity, mapTilesParent);

            // In that situation, we just spawn the super collectible and that's it
            // Everything else is already on the treasure tile
            CollectiblesManager.instance.SpawnFixedCollectible(superCollectibleOnCurrentTile, tileWorldPosition);
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
                AddSomething(watersPrefabs, tileCoordinates, false, preventSpawnPosition, tile.transform, validSortingOrderForWater, "LakeCollider");
            }

            // generate rocks
            Vector2 rockMinMax = DataManager.instance.GetSpawnProbability("rock", currentPlayedChapter.chapterData.rocksSpawnFrequency);
            float rockProba = Random.Range(rockMinMax.x, rockMinMax.y);
            float rockAmount = Mathf.Floor(rockProba) + ((Random.Range(Mathf.Floor(rockProba), Mathf.Ceil(rockProba)) < rockProba) ? 1 : 0);
            for (int i = 0; i < rockAmount; i++)
            {
                AddSomething(rocksPrefabs, tileCoordinates, preventSpawnRocksAtPosition, preventSpawnPosition, tile.transform, validSortingOrderForRocks, "Rock");
            }

            // generate currency collectibles
            Vector2 currencyMinMax = DataManager.instance.GetSpawnProbability("currency", currentPlayedChapter.chapterData.coinsSpawnFrequency);
            float currencyBonusFactor = 1 + RunManager.instance.player.GetCurrencyBoost() + (RunManager.instance.gameModeFroinsMultiplier - 1); // Game mode froins multiplier
            float currencyProba = Random.Range(currencyMinMax.x * currencyBonusFactor, currencyMinMax.y * currencyBonusFactor);
            float currencyAmount = Mathf.Floor(currencyProba) + ((Random.Range(Mathf.Floor(currencyProba), Mathf.Ceil(currencyProba)) < currencyProba) ? 1 : 0);
            currencyAmount *= 10;
            float spawnedCurrency = 0;
            while (spawnedCurrency < currencyAmount)
            {
                if (GetRandomSpawnPositionOnTile(tileCoordinates, out Vector2 position))
                {
                    int bonusValue = Random.Range(1, 3) * 5;
                    CollectiblesManager.instance.SpawnCollectible(position, CollectibleType.FROINS, bonusValue);
                    spawnedCurrency += bonusValue;
                }
            }

            // generate health collectibles
            Vector2 healthMinMax = DataManager.instance.GetSpawnProbability("health", currentPlayedChapter.chapterData.healthSpawnFrequency);
            float healthAmount = Random.Range(healthMinMax.x, healthMinMax.y) * 100;
            float smolHealthValue = 20;
            float bigHealthValue = 100;
            bool skipBigHealth = false;
            while (healthAmount > 0)
            {
                skipBigHealth = (Random.Range(0, 2) == 0);
                if (!skipBigHealth && Random.Range(0, bigHealthValue) < healthAmount)
                {
                    if (GetRandomSpawnPositionOnTile(tileCoordinates, out Vector2 position))
                    {
                        CollectiblesManager.instance.SpawnCollectible(position, CollectibleType.HEALTH, bigHealthValue);
                        healthAmount -= bigHealthValue;
                    }
                }
                else if (smolHealthValue < healthAmount || Random.Range(0, smolHealthValue) < healthAmount)
                {
                    if (GetRandomSpawnPositionOnTile(tileCoordinates, out Vector2 position))
                    {
                        CollectiblesManager.instance.SpawnCollectible(position, CollectibleType.HEALTH, smolHealthValue);
                        healthAmount -= smolHealthValue;
                    }
                }
                else
                {
                    healthAmount -= smolHealthValue;
                }
            }

            // generate level up collectibles
            Vector2 levelUpMinMax = DataManager.instance.GetSpawnProbability("levelUp", currentPlayedChapter.chapterData.levelUpSpawnFrequency);
            float levelUpProba = Random.Range(levelUpMinMax.x, levelUpMinMax.y);
            float levelUpAmount = Mathf.Floor(levelUpProba) + ((Random.Range(Mathf.Floor(levelUpProba), Mathf.Ceil(levelUpProba)) < levelUpProba) ? 1 : 0);
            for (int i = 0; i < levelUpAmount; i++)
            {
                if (GetRandomSpawnPositionOnTile(tileCoordinates, out Vector2 position))
                {
                    CollectiblesManager.instance.SpawnCollectible(position, CollectibleType.LEVEL_UP, 1);
                }
            }

            // generate other collectibles
            foreach (CollectibleSpawnFrequency collectibleSpawnFrequency in currentPlayedChapter.chapterData.otherCollectibleSpawnFrequenciesList)
            {                
                if (BuildManager.instance.IsCollectibleAvailable(collectibleSpawnFrequency.Type))
                {
                    Vector2 powerUpMinMax = DataManager.instance.GetSpawnProbability("powerUp", collectibleSpawnFrequency.Frequency);
                    float powerUpProba = Random.Range(powerUpMinMax.x, powerUpMinMax.y);
                    float powerUpAmount = Mathf.Floor(powerUpProba) + ((Random.Range(Mathf.Floor(powerUpProba), Mathf.Ceil(powerUpProba)) < powerUpProba) ? 1 : 0);
                    for (int i = 0; i < powerUpAmount; i++)
                    {
                        if (GetRandomSpawnPositionOnTile(tileCoordinates, out Vector2 position))
                        {
                            CollectiblesManager.instance.SpawnCollectible(position, collectibleSpawnFrequency.Type, 1);
                        }
                    }
                }
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
