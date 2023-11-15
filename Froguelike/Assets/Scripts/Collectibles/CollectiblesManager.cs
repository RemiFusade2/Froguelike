using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;


/// <summary>
/// Describes an instance of a collectible, with all relevant data linked to it
/// </summary>
[System.Serializable]
public class CollectibleInstance
{
    public CollectibleType collectibleType;
    public float collectibleValue;

    public bool active;
    public bool captured;

    public Vector2 moveDirection;
    public float lastUpdateTime;

    // References to Components
    public Transform collectibleTransform;
    public Rigidbody2D collectibleRigidbody;
    public SpriteRenderer collectibleRenderer;
    public Collider2D collectibleCollider;
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

    [Header("Settings - Logs")]
    public VerboseLevel verboseLevel;

    [Header("References")]
    public Transform collectiblesParent;

    [Header("Prefabs")]
    [Tooltip("Collectibles, they get magnetized when you walk near them")]
    public GameObject magnetCollectiblePrefab;
    [Tooltip("Collectibles placed at a specific spot on the map, you need to walk onto them to grab them")]
    public GameObject superCollectiblePrefab;

    [Header("Settings - Pooling")]
    public int maxCollectibles = 1000;

    [Header("Settings - Update")]
    public int updateCollectiblesCount = 100;

    [Header("Settings - Magnet")]
    public float collectibleMinMovingSpeed = 5;
    public float collectibleMaxMovingSpeed = 30;
    public float collectibleMovingSpeedFactor = 1.5f;
    public float collectMinDistance = 1.5f;
    public float updateAllCollectiblesDelay = 0.1f;

    [Header("Settings - Spawn")]
    public float collectibleSpawnMinPushForce = 5;
    public float collectibleSpawnMaxPushForce = 7;
    public float collectibleSpawnDelay = 1;
    public float collectibleDespawnDistance = 150;

    private CollectibleInstance[] allCollectibles; // Used to get a collectible instance from its ID (index)

    private Queue<CollectibleInstance> inactiveCollectiblesPool; // Used to grab an inactive collectible instance and use it

    private Queue<CollectibleInstance> capturedCollectiblesQueue; // Used to update all captured collectibles and move them towards the frog

    private Coroutine updateCollectiblesCoroutine;

    #region Unity Callback Methods

    private void Awake()
    {
        instance = this;
        capturedCollectiblesQueue = new Queue<CollectibleInstance>();
    }

    private void Start()
    {
        InstantiateAllCollectibles();
        ReinitializeUpdateCoroutine();
    }

    #endregion

    private string GetNameFromID(int id)
    {
        return id.ToString();
    }

    private int GetIDFromName(string name)
    {
        if (int.TryParse(name, out int result))
        {
            return result;
        }
        return -1;
    }

    private CollectibleInstance GetCollectibleInstanceFromGameObject(GameObject collectibleGameObject)
    {
        int ID = GetIDFromName(collectibleGameObject.name);
        CollectibleInstance result = null;
        if (ID != -1)
        {
            result = allCollectibles[ID];
        }
        return result;
    }

    private void InstantiateAllCollectibles()
    {
        allCollectibles = new CollectibleInstance[maxCollectibles];
        inactiveCollectiblesPool = new Queue<CollectibleInstance>();
        for (int i = 0; i < maxCollectibles; i++)
        {
            CollectibleInstance collectible = new CollectibleInstance();
            allCollectibles[i] = collectible;

            GameObject collectibleGameObject = Instantiate(magnetCollectiblePrefab, DataManager.instance.farAwayPosition, Quaternion.identity, collectiblesParent);
            collectibleGameObject.name = GetNameFromID(i);

            collectible.collectibleTransform = collectibleGameObject.transform;
            collectible.collectibleRigidbody = collectibleGameObject.GetComponent<Rigidbody2D>();
            collectible.collectibleCollider = collectibleGameObject.GetComponent<Collider2D>();
            collectible.collectibleRenderer = collectibleGameObject.GetComponent<SpriteRenderer>();

            PutCollectibleInThePool(collectible);
        }
    }

    private void PutCollectibleInThePool(CollectibleInstance collectible)
    {
        inactiveCollectiblesPool.Enqueue(collectible);

        collectible.active = false;
        collectible.captured = false;

        collectible.collectibleTransform.position = DataManager.instance.farAwayPosition;
        collectible.collectibleRenderer.enabled = false;
        collectible.collectibleRigidbody.velocity = Vector2.zero;
        collectible.collectibleRigidbody.simulated = false;
        collectible.collectibleCollider.enabled = false;

        if (verboseLevel == VerboseLevel.MAXIMAL)
        {
            Debug.Log($"Put collectible {collectible.collectibleTransform.name} in the pool");
        }
    }

    /// <summary>
    /// Remove all collectibles from the map.
    /// Put them back in the pool.
    /// </summary>
    public void ClearAllCollectibles()
    {
        capturedCollectiblesQueue.Clear();
        foreach (CollectibleInstance collectible in allCollectibles)
        {
            if (collectible.active)
            {
                PutCollectibleInThePool(collectible);
            }
        }
    }

    /// <summary>
    /// Spawn a collectible (from the pool) at a position on the map.
    /// Optional: add a push force to it
    /// </summary>
    /// <param name="position"></param>
    /// <param name="collectibleType"></param>
    /// <param name="bonusValue"></param>
    /// <param name="pushAwayForce"></param>
    public void SpawnCollectible(Vector2 position, CollectibleType collectibleType, float bonusValue, float pushAwayForce = 0)
    {
        if (inactiveCollectiblesPool.TryDequeue(out CollectibleInstance collectible))
        {
            CollectibleInfo collectibleInfo = DataManager.instance.GetCollectibleInfoFromType(collectibleType);

            // Set Icon
            // For XP and Froins, there are multiple icons depending on the value of XP and Froins this collectible has
            int iconIndex = 0;
            // TODO: make it so it's possible to choose the values from which we change icons
            switch (collectibleType)
            {
                case CollectibleType.XP_BONUS:
                    float maxXpBonusValue = 10;
                    iconIndex = Mathf.FloorToInt((bonusValue / maxXpBonusValue) * collectibleInfo.Icons.Count);
                    iconIndex = Mathf.Clamp(iconIndex, 0, collectibleInfo.Icons.Count - 1);
                    break;
                case CollectibleType.FROINS:
                    float maxfroinsBonusValue = 10;
                    iconIndex = Mathf.FloorToInt((bonusValue / maxfroinsBonusValue) * collectibleInfo.Icons.Count);
                    iconIndex = Mathf.Clamp(iconIndex, 0, collectibleInfo.Icons.Count - 1);
                    break;
            }
            Sprite collectibleIcon = collectibleInfo.Icons[iconIndex];
            collectible.collectibleRenderer.sprite = collectibleIcon;
            collectible.collectibleRenderer.enabled = true;

            // Set type and value
            collectible.collectibleType = collectibleType;
            collectible.collectibleValue = bonusValue;

            // Set position
            collectible.collectibleTransform.position = position;

            if (pushAwayForce > 0)
            {
                // Enable rigidbody but not collider
                // Enable collider after a delay
                float randomPushForce = pushAwayForce * Random.Range(collectibleSpawnMinPushForce, collectibleSpawnMaxPushForce);
                collectible.collectibleRigidbody.velocity = Vector2.zero;
                collectible.collectibleRigidbody.simulated = true;
                collectible.collectibleRigidbody.AddForce(randomPushForce * Random.insideUnitCircle.normalized, ForceMode2D.Impulse);

                StartCoroutine(ActivateCollectible(collectible, collectibleSpawnDelay)); // short delay after spawn
            }
            else
            {
                StartCoroutine(ActivateCollectible(collectible, 0)); // no delay
            }

            if (verboseLevel == VerboseLevel.MAXIMAL)
            {
                Debug.Log($"Spawn collectible {collectible.collectibleTransform.name} as {collectible.collectibleType.ToString()} + {collectible.collectibleValue}");
            }
        }
        else
        {
            if (verboseLevel == VerboseLevel.MAXIMAL)
            {
                Debug.Log("Impossible to spawn collectible because pool is empty");
            }
        }
    }

    private IEnumerator ActivateCollectible(CollectibleInstance collectible, float delay)
    {
        yield return new WaitForSeconds(delay);

        collectible.active = true;
        collectible.captured = false;
        collectible.collectibleCollider.enabled = true;
        collectible.collectibleRigidbody.simulated = true;

        collectible.collectibleRigidbody.velocity = Vector2.zero;
    }


    #region Super Collectibles (not pooled)

    public void SpawnSuperCollectible(FixedCollectible collectible, Vector2 position)
    {
        GameObject newCollectible = Instantiate(superCollectiblePrefab, position, Quaternion.identity, this.transform);
        newCollectible.GetComponent<FixedCollectibleBehaviour>().InitializeCollectible(collectible);
        newCollectible.name = collectible.collectibleType.ToString();
        CompassArrowBehaviour compassArrow = RunManager.instance.GetCompassArrowForCollectible(collectible);
        if (compassArrow != null)
        {
            compassArrow.SetCollectibleTransform(newCollectible.transform);
        }
    }

    public void CollectFixedCollectible(FixedCollectible superCollectible)
    {
        RunManager.instance.RemoveCompassArrowForCollectible(superCollectible);
        SoundManager.instance.PlayPickUpCollectibleSound();

        RunManager.instance.ShowCollectSuperCollectiblePanel(superCollectible);

        if (verboseLevel == VerboseLevel.MAXIMAL)
        {
            Debug.Log("Collectible Manager - Collect fixed collectible: " + superCollectible.collectibleType.ToString());
        }
    }

    #endregion

    public void CaptureCollectible(Transform collectibleTransform)
    {
        CollectibleInstance collectible = GetCollectibleInstanceFromGameObject(collectibleTransform.gameObject);
        if (collectible != null)
        {
            CaptureCollectible(collectible);
        }
    }

    /// <summary>
    /// Add the collectible to the capture queue. All captured collectibles will move towards the character.
    /// Call this method when a collectible is in magnet range.
    /// Only one call is required per collectible.
    /// </summary>
    /// <param name="collectible"></param>
    public void CaptureCollectible(CollectibleInstance collectible)
    {
        if (!collectible.captured)
        {
            collectible.captured = true;
            capturedCollectiblesQueue.Enqueue(collectible);
            collectible.collectibleCollider.enabled = false;

            if (verboseLevel == VerboseLevel.MAXIMAL)
            {
                Debug.Log("Capture collectible: " + collectible.collectibleTransform.name);
            }
        }
    }
    
    /// <summary>
    /// Instantly capture all active collectibles that give XP or Froins
    /// </summary>
    public void ApplyMegaMagnet()
    {
        foreach (CollectibleInstance collectible in allCollectibles)
        {
            if (collectible.active && !collectible.captured && (collectible.collectibleType == CollectibleType.FROINS || collectible.collectibleType == CollectibleType.XP_BONUS || collectible.collectibleType == CollectibleType.LEVEL_UP))
            {
                CaptureCollectible(collectible);
            }
        }
    }
        
    /// <summary>
    /// Put all captured collectibles back into the pool, without collecting them
    /// </summary>
    public void CancelAllCapturedCollectibles()
    {
        while (capturedCollectiblesQueue.TryDequeue(out CollectibleInstance collectible))
        {
            PutCollectibleInThePool(collectible);
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

    private IEnumerator UpdateAllCollectiblesAsync(float delay)
    {
        while (true)
        {
            UpdateCapturedCollectibles(updateCollectiblesCount);
            yield return new WaitForSeconds(delay);
        }
    }

    /// <summary>
    /// Move the collectibles that were captured towards the frog.
    /// </summary>
    private void UpdateCapturedCollectibles(int maxCount)
    {
        if (GameManager.instance.isGameRunning)
        {
            // Update velocity of all captured collectibles + collect the ones that are close enough to the frog
            Transform playerTransform = GameManager.instance.player.transform;

            int collectiblesToUpdateCount = Mathf.Min(capturedCollectiblesQueue.Count, maxCount);
            float distanceWithFrog = 0;
            Vector2 moveDirection;
            float walkSpeed = DataManager.instance.defaultWalkSpeed * (1 + GameManager.instance.player.GetWalkSpeedBoost());
            float collectibleMovingSpeed = collectibleMovingSpeedFactor * walkSpeed;
            for (int i = 0; i < collectiblesToUpdateCount; i++)
            {
                if (capturedCollectiblesQueue.TryDequeue(out CollectibleInstance collectible))
                {
                    // Move collectible towards frog
                    moveDirection = (playerTransform.position - collectible.collectibleTransform.position);
                    distanceWithFrog = moveDirection.magnitude;

                    // Despawn the collectible if it is too far
                    if (distanceWithFrog > collectibleDespawnDistance)
                    {
                        if (verboseLevel == VerboseLevel.MAXIMAL)
                        {
                            Debug.Log($"Unspawn collectible {collectible.collectibleTransform.name} because it's too far");
                        }
                        PutCollectibleInThePool(collectible);
                    }
                    else
                    {
                        // Collect the collectible if it is close enough or if its speed is moving it away from the frog (should only happen if the collectible passed the frog)
                        moveDirection = moveDirection.normalized; 
                        float dot = Vector2.Dot(collectible.collectibleRigidbody.velocity, moveDirection);
                        if (distanceWithFrog < collectMinDistance || dot < 0)
                        {
                            // Collect
                            if (verboseLevel == VerboseLevel.MAXIMAL)
                            {
                                Debug.Log($"Collect collectible {collectible.collectibleTransform.name}");
                            }
                            RunManager.instance.CollectCollectible(collectible.collectibleType, collectible.collectibleValue);
                            PutCollectibleInThePool(collectible);
                        }
                        else
                        {
                            // Update collectible velocity
                            collectibleMovingSpeed = Mathf.Clamp(collectibleMovingSpeed, collectibleMinMovingSpeed, collectibleMaxMovingSpeed);
                            collectible.collectibleRigidbody.velocity = moveDirection * collectibleMovingSpeed;
                        }
                    }

                    // If that collectible is still active and captured, we'll update it again next time
                    if (collectible.active && collectible.captured)
                    {
                        capturedCollectiblesQueue.Enqueue(collectible);
                    }
                }
            }
        }
    }

    #endregion
}
