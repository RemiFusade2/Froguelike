using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum CollectibleType
{
    CURRENCY,
    XP_BONUS,
    LEVEL_UP,
    HEALTH
}

/// <summary>
/// This class deals with all collectibles on the map.
/// Spawning a new collectibles is done using this class.
/// Regular calls to the UpdateAllCollectibles() method are required to keep them moving towards the character when needed.
/// Call to CaptureCollectible() should happen when the collectible is in magnet range.
/// </summary>
public class CollectiblesManager : MonoBehaviour
{
    public static CollectiblesManager instance;

    [Header("Prefabs")]
    public GameObject collectiblePrefab;
    [Space]
    public Sprite currencyCollectibleIcon;
    public Sprite superCurrencyCollectibleIcon;
    public Sprite xpCollectibleIcon;
    public Sprite levelUpCollectibleIcon;
    public Sprite healthCollectibleIcon;

    [Header("Settings")]
    public float collectibleMinMovingSpeed = 8;
    public float collectibleMovingSpeedFactor = 1.5f;
    public float collectMinDistance = 1.5f;
    public float updateAllCollectiblesDelay = 0.1f;

    private List<Transform> allCollectiblesList;
    private List<Transform> allCapturedCollectiblesList;

    private Coroutine updateCollectiblesCoroutine;

    // Start is called before the first frame update
    void Awake()
    {
        instance = this;
        allCollectiblesList = new List<Transform>();
        allCapturedCollectiblesList = new List<Transform>();
    }

    private void Start()
    {
        ReinitializeUpdateCoroutine();
    }

    private void ReinitializeUpdateCoroutine()
    {
        if (updateCollectiblesCoroutine != null)
        {
            StopCoroutine(updateCollectiblesCoroutine);
        }
        updateCollectiblesCoroutine = StartCoroutine(UpdateAllCollectiblesAsync(updateAllCollectiblesDelay));
    }

    public void SpawnCollectible(Vector2 position, CollectibleType collectibleType, int bonusValue)
    {
        GameObject newCollectible = Instantiate(collectiblePrefab, position, Quaternion.identity, this.transform);

        string collectibleName = "";
        switch (collectibleType)
        {
            case CollectibleType.CURRENCY:
                if (bonusValue > 1)
                {
                    newCollectible.GetComponent<SpriteRenderer>().sprite = superCurrencyCollectibleIcon;
                }
                else
                {
                    newCollectible.GetComponent<SpriteRenderer>().sprite = currencyCollectibleIcon;
                }
                collectibleName = "Currency";
                break;
            case CollectibleType.XP_BONUS:
                newCollectible.GetComponent<SpriteRenderer>().sprite = xpCollectibleIcon;
                collectibleName = "XP";
                break;
            case CollectibleType.LEVEL_UP:
                newCollectible.GetComponent<SpriteRenderer>().sprite = levelUpCollectibleIcon;
                collectibleName = "LevelUp";
                break;
            case CollectibleType.HEALTH:
                newCollectible.GetComponent<SpriteRenderer>().sprite = healthCollectibleIcon;
                collectibleName = "HP";
                break;
        }
        collectibleName += "+" + bonusValue.ToString();
        newCollectible.name = collectibleName;

        allCollectiblesList.Add(newCollectible.transform);
    }

    /// <summary>
    /// Add the collectible to the capture list. All captured collectibles will move towards the character.
    /// Call this method when a collectible is in magnet range.
    /// Only one call is required per collectible.
    /// </summary>
    /// <param name="collectible"></param>
    public void CaptureCollectible(Transform collectible)
    {
        Debug.Log("Capture collectible");
        if (!allCapturedCollectiblesList.Contains(collectible))
        {
            allCapturedCollectiblesList.Add(collectible);
        }
    }

    /// <summary>
    /// Destroy collectible and remove it from the list. Returns its name.
    /// Call this method if there's a collision between the collectible and the character collider.
    /// </summary>
    /// <param name="collectible">The transform of the collected collectible</param>
    /// <returns>Returns game object name, contains data on which type of collectible it was</returns>
    private string CollectCollectible(Transform collectible)
    {
        Debug.Log("Collect collectible");
        string collectibleName = collectible.gameObject.name;
        if (allCapturedCollectiblesList.Contains(collectible))
        {
            allCapturedCollectiblesList.Remove(collectible);
        }
        Destroy(collectible.gameObject);
        return collectibleName;
    }

    public IEnumerator UpdateAllCollectiblesAsync(float delay)
    {
        while (true)
        {
            UpdateAllCollectibles();
            yield return new WaitForSeconds(delay);
        }
    }

    /// <summary>
    /// Move the collectibles that were captured towards the character.
    /// </summary>
    public void UpdateAllCollectibles()
    {
        if (GameManager.instance.isGameRunning)
        {
            // Update velocity of all capture collectibles + collect the ones that are close enough to the character
            Transform playerTransform = GameManager.instance.player.transform;
            Rigidbody2D collectibleRb = null;
            float distanceWithFrog = 0;
            Vector2 moveDirection;
            List<Transform> collectedCollectibles = new List<Transform>();
            foreach (Transform collectible in allCapturedCollectiblesList)
            {
                distanceWithFrog = Vector2.Distance(playerTransform.position, collectible.position);
                moveDirection = (playerTransform.position - collectible.position).normalized;
                
                if (distanceWithFrog < collectMinDistance)
                {
                    // this collectible is close enough to be collected
                    collectedCollectibles.Add(collectible);
                }
                else
                {
                    collectibleRb = collectible.GetComponent<Rigidbody2D>();
                    float collectibleMovingSpeed = collectibleMovingSpeedFactor * GameManager.instance.player.walkSpeed;
                    collectibleMovingSpeed = Mathf.Clamp(collectibleMovingSpeed, collectibleMinMovingSpeed, 100.0f);
                    collectibleRb.velocity = moveDirection * collectibleMovingSpeed;
                }
            }

            // Collect all collectibles that are close enough (destroy them + apply their effect)
            foreach (Transform collectible in collectedCollectibles)
            {
                string collectibleName = CollectCollectible(collectible);
                GameManager.instance.CollectCollectible(collectibleName);
            }
        }
    }
}
