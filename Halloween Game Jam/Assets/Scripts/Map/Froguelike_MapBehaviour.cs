using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Froguelike_MapBehaviour : MonoBehaviour
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

    private void AddSomething(List<GameObject> prefabs, Vector2Int tileCoordinates)
    {
        int randomIndex = Random.Range(0, prefabs.Count);
        Vector2 randomVector = Random.insideUnitCircle;
        Vector2 rockPosition = GetWorldPositionOfTile(tileCoordinates) + randomVector * (tileSize / 2.0f);
        Instantiate(prefabs[randomIndex], rockPosition, Quaternion.identity, mapTilesParent);
    }

    private bool DoesTileExist(Vector2Int tileCoordinates)
    {
        return existingTilesCoordinates.Contains(tileCoordinates);
    }
    private void AddTile(Vector2Int tileCoordinates)
    {
        Vector2 tileWorldPosition = tileCoordinates * tileSize;
        Instantiate(backgroundTilePrefab, tileWorldPosition, Quaternion.identity, mapTilesParent);
        // generate rocks
        for (int i = 0; i < rockDensity * maxRockCount; i++)
        {
            if (Random.Range(0,3) != 0)
            {
                AddSomething(rocksPrefabs, tileCoordinates);
            }
        }
        // generate water
        for (int i = 0; i < waterDensity * maxWaterCount; i++)
        {
            if (Random.Range(0, 3) != 0)
            {
                AddSomething(watersPrefabs, tileCoordinates);
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
                    AddTile(tileCoordinates);
                }
            }
        }
    }
}
