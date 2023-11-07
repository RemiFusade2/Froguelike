using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
public enum CollectibleType
{
    FROINS,
    XP_BONUS,
    LEVEL_UP,
    HEALTH,

    POWERUP_FREEZE_ALL,
    POWERUP_POISON_ALL,
    POWERUP_CURSE_ALL
}

[System.Serializable]
public class CollectibleInfo
{
    public CollectibleType Type;
    public string Name;
    public Sprite Icon;
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
    public GameObject magnetCollectiblePrefab;
    public GameObject superCollectiblePrefab;

    [Header("Magnet collectibles icons")]
    public List<CollectibleInfo> magnetCollectiblesIconsList;

    [Header("Settings - Magnet")]
    public float collectibleMinMovingSpeed = 8;
    public float collectibleMovingSpeedFactor = 1.5f;
    public float collectMinDistance = 1.5f;
    public float updateAllCollectiblesDelay = 0.1f;

    [Header("Settings - Spawn")]
    public float collectibleSpawnMinPushForce = 5;
    public float collectibleSpawnMaxPushForce = 7;
    public float collectibleSpawnDelay = 1;

    [Header("Settings - Logs")]
    public VerboseLevel verboseLevel;


    private List<Transform> allCollectiblesList;
    private List<Transform> allCapturedCollectiblesList;

    private Coroutine updateCollectiblesCoroutine;

    #region Unity Callback Methods

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

    #endregion

    public void ClearCollectibles()
    {
        foreach(Transform collectibleChild in this.transform)
        {
            Destroy(collectibleChild.gameObject, 0.05f);
        }
        allCollectiblesList.Clear();
        allCapturedCollectiblesList.Clear();
        ReinitializeUpdateCoroutine();
    }

    public void SpawnSuperCollectible(FixedCollectible collectible, Vector2 position)
    {
        GameObject newCollectible = Instantiate(superCollectiblePrefab, position, Quaternion.identity, this.transform);
        newCollectible.GetComponent<SuperCollectibleBehaviour>().InitializeCollectible(collectible);
        newCollectible.name = collectible.collectibleType.ToString();
        RunManager.instance.GetCompassArrowForCollectible(collectible).SetCollectibleTransform(newCollectible.transform);
    }

    private CollectibleInfo GetCollectibleInfoFromType(CollectibleType collectibleType)
    {
        return magnetCollectiblesIconsList.Where(x => x.Type.Equals(collectibleType)).FirstOrDefault();
    }

    private CollectibleInfo GetCollectibleInfoFromName(string collectibleName)
    {
        return magnetCollectiblesIconsList.Where(x => x.Name.Equals(collectibleName)).FirstOrDefault();
    }


    private string GetCollectibleGameObjectName(CollectibleInfo collectibleInfo, float bonusValue)
    {
        return $"{collectibleInfo.Name}+{bonusValue.ToString()}";
    }

    private void GetCollectibleTypeAndValueFromName(string gameObjectName, out CollectibleType collectibleType, out float bonusValue)
    {
        collectibleType = CollectibleType.XP_BONUS;
        bonusValue = 0;

        string[] splitName = gameObjectName.Split("+");
        CollectibleInfo info = GetCollectibleInfoFromName(splitName[0]);

        if (info != null)
        {
            collectibleType = info.Type;
            int.TryParse(splitName[1], out int value);
            bonusValue = value;
        }
    }

    public void SpawnCollectible(Vector2 position, CollectibleType collectibleType, float bonusValue, bool pushAway = false)
    {
        GameObject newCollectible = Instantiate(magnetCollectiblePrefab, position, Quaternion.identity, this.transform);

        CollectibleInfo collectibleInfo = GetCollectibleInfoFromType(collectibleType);

        newCollectible.GetComponent<SpriteRenderer>().sprite = collectibleInfo.Icon;

        newCollectible.name = $"{collectibleInfo.Name}+{bonusValue.ToString()}";

        if (pushAway)
        {
            float randomPushForce = Random.Range(collectibleSpawnMinPushForce, collectibleSpawnMaxPushForce);
            newCollectible.GetComponent<Rigidbody2D>().AddForce(randomPushForce * Random.insideUnitCircle.normalized, ForceMode2D.Impulse);

            // Ensure that it can't be collected for a short delay after spawn
            StartCoroutine(EnableCollectibleCollider(newCollectible.GetComponent<CircleCollider2D>(), collectibleSpawnDelay));
        }
        else
        {
            newCollectible.GetComponent<CircleCollider2D>().enabled = true;
        }

        allCollectiblesList.Add(newCollectible.transform);
    }

    private IEnumerator EnableCollectibleCollider(CircleCollider2D colliderComponent, float delay)
    {
        yield return new WaitForSeconds(delay);
        colliderComponent.enabled = true;
    }

    /// <summary>
    /// Add the collectible to the capture list. All captured collectibles will move towards the character.
    /// Call this method when a collectible is in magnet range.
    /// Only one call is required per collectible.
    /// </summary>
    /// <param name="collectible"></param>
    public void CaptureCollectible(Transform collectible)
    {
        if (!allCapturedCollectiblesList.Contains(collectible))
        {
            allCapturedCollectiblesList.Add(collectible);
            if (verboseLevel == VerboseLevel.MAXIMAL)
            {
                Debug.Log("Capture collectible: " + collectible.gameObject.name);
            }
        }
    }

    /// <summary>
    /// Destroy collectible and remove it from the list. Returns its type and value.
    /// Call this method if there's a collision between the collectible and the character collider.
    /// </summary>
    /// <param name="collectible">The transform of the collected collectible</param>
    /// <returns>Returns game object name, contains data on which type of collectible it was</returns>
    private string CollectCollectible(Transform collectible, out CollectibleType collectibleType, out float collectibleValue)
    {
        string collectibleName = collectible.gameObject.name;

        GetCollectibleTypeAndValueFromName(collectibleName, out collectibleType, out collectibleValue);

        if (allCapturedCollectiblesList.Contains(collectible))
        {
            allCapturedCollectiblesList.Remove(collectible);
        }
        Destroy(collectible.gameObject);
        return collectibleName;
    }

    public void CollectSuperCollectible(FixedCollectible superCollectible)
    {
        RunManager.instance.RemoveCompassArrowForCollectible(superCollectible);
        SoundManager.instance.PlayPickUpCollectibleSound();
        switch (superCollectible.collectibleType)
        {
            case FixedCollectibleType.FRIEND:
                GameManager.instance.player.AddActiveFriend(superCollectible.collectibleFriendType, GameManager.instance.player.transform.position);
                break;
            case FixedCollectibleType.HAT:
                GameManager.instance.player.AddHat(superCollectible.collectibleHatType);
                break;
            case FixedCollectibleType.STATS_ITEM:
                RunManager.instance.PickRunItem(superCollectible.collectibleStatItemData);
                break;
            case FixedCollectibleType.WEAPON_ITEM:
                RunManager.instance.PickRunItem(superCollectible.collectibleWeaponItemData);
                break;
        }
        if (verboseLevel == VerboseLevel.MAXIMAL)
        {
            Debug.Log("Collect super collectible: " + superCollectible.collectibleType.ToString());
        }
    }

    #region Update Magnet Collectibles

    private void ReinitializeUpdateCoroutine()
    {
        if (updateCollectiblesCoroutine != null)
        {
            StopCoroutine(updateCollectiblesCoroutine);
        }
        updateCollectiblesCoroutine = StartCoroutine(UpdateAllCollectiblesAsync(updateAllCollectiblesDelay));
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
                    float walkSpeed = DataManager.instance.defaultWalkSpeed * (1 + GameManager.instance.player.walkSpeedBoost);
                    float collectibleMovingSpeed = collectibleMovingSpeedFactor * walkSpeed;
                    collectibleMovingSpeed = Mathf.Clamp(collectibleMovingSpeed, collectibleMinMovingSpeed, 100.0f);
                    collectibleRb.velocity = moveDirection * collectibleMovingSpeed;
                }
            }

            // Collect all collectibles that are close enough (destroy them + apply their effect)
            foreach (Transform collectible in collectedCollectibles)
            {
                string collectibleName = CollectCollectible(collectible, out CollectibleType collectibleType, out float collectibleValue);
                RunManager.instance.CollectCollectible(collectibleType, collectibleValue);
            }
        }
    }

    #endregion
}
