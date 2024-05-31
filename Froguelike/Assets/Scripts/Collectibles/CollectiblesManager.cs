using System.Collections;
using System.Collections.Generic;
using UnityEngine;


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

    public bool usesMagnet;
    public bool fliesAwayFromFrog;
    public bool fliesTowardsFrog;

    public Vector2 moveDirection;
    public float lastChangeOfDirectionTime;

    public float spawnTime;

    // References to Components
    public Transform collectibleTransform;
    public Rigidbody2D collectibleRigidbody;
    public SpriteRenderer collectibleRenderer;
    public Animator collectibleAnimator;
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
    public Transform fixedCollectiblesParent;

    [Header("Prefabs")]
    [Tooltip("Collectibles, they get magnetized when you walk near them")]
    public GameObject magnetCollectiblePrefab;
    [Tooltip("Collectibles placed at a specific spot on the map, you need to walk onto them to grab them")]
    public GameObject fixedCollectiblePrefab;

    [Header("Settings - Pooling")]
    public int maxCollectibles = 1000;

    [Header("Settings - Update")]
    public int updateCollectiblesCount = 200;
    public float updateAllCollectiblesDelay = 0.02f;

    [Header("Settings - Magnet")]
    public float collectMinDistance = 1.5f;

    [Header("Settings - Moving collectibles")]
    public float movingCollectiblesDefaultSpeed = 1;
    public float movingCollectiblesDelayBetweenDirectionChange = 2;
    [Space]
    public float movingCollectiblesSafeDistanceWithFrog = 8;
    public float movingCollectiblesEscapeSpeed = 4;
    public float movingCollectiblesGoTowardsFrogSpeed = 2;

    [Header("Settings - Spawn")]
    public float collectibleSpawnMinPushForce = 5;
    public float collectibleSpawnMaxPushForce = 7;
    public float collectibleSpawnDelay = 1;
    public float collectibleDespawnDistance = 150;

    private CollectibleInstance[] allCollectibles; // Used to get a collectible instance from its ID (index)

    private Queue<CollectibleInstance> inactiveCollectiblesPool; // Used to grab an inactive collectible instance and use it

    private Queue<CollectibleInstance> capturedCollectiblesQueue; // Used to update all captured collectibles and move them towards the frog

    private Queue<CollectibleInstance> movingCollectiblesQueue; // Used to update collectibles that always move (like bugs) and try to escape the frog

    private Coroutine updateCollectiblesCoroutine;

    #region Unity Callback Methods

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

        capturedCollectiblesQueue = new Queue<CollectibleInstance>();
        movingCollectiblesQueue = new Queue<CollectibleInstance>();
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

            GameObject collectibleGameObject = Instantiate(magnetCollectiblePrefab, DataManager.instance.GetFarAwayPosition(), Quaternion.identity, collectiblesParent);
            collectibleGameObject.name = GetNameFromID(i);

            collectible.collectibleTransform = collectibleGameObject.transform;
            collectible.collectibleRigidbody = collectibleGameObject.GetComponent<Rigidbody2D>();
            collectible.collectibleCollider = collectibleGameObject.GetComponent<Collider2D>();
            collectible.collectibleRenderer = collectibleGameObject.GetComponent<SpriteRenderer>();
            collectible.collectibleAnimator = collectibleGameObject.GetComponent<Animator>();

            PutCollectibleInThePool(collectible);
        }
    }

    private void PutCollectibleInThePool(CollectibleInstance collectible)
    {
        inactiveCollectiblesPool.Enqueue(collectible);

        collectible.active = false;
        collectible.captured = false;

        collectible.collectibleTransform.position = DataManager.instance.GetFarAwayPosition();
        collectible.collectibleRenderer.enabled = false;
        collectible.collectibleAnimator.SetInteger("style", 0);
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
        // Clear Magnet collectibles
        capturedCollectiblesQueue.Clear();
        movingCollectiblesQueue.Clear();
        foreach (CollectibleInstance collectible in allCollectibles)
        {
            PutCollectibleInThePool(collectible);
        }

        // Clear Fixed collectibles
        foreach (Transform child in fixedCollectiblesParent)
        {
            Destroy(child.gameObject, 0.1f);
        }
    }

    private CollectibleInstance DequeueCollectibleInstance()
    {
        CollectibleInstance collectibleInstance = null;
        bool recyclingHappened = false;
        while (!inactiveCollectiblesPool.TryDequeue(out collectibleInstance) && !recyclingHappened)
        {
            // Couldn't dequeue a collectible (pool must be empty)

            // Let's recycle an older collectible
            float lowestSpawnTime = Time.time;
            CollectibleInstance oldestCollectibleInstance = allCollectibles[0];

            foreach (CollectibleInstance collectible in allCollectibles)
            {
                if (collectible.active && collectible.usesMagnet && !collectible.captured && collectible.spawnTime < lowestSpawnTime)
                {
                    lowestSpawnTime = collectible.spawnTime;
                    oldestCollectibleInstance = collectible;
                }
            }
            if (oldestCollectibleInstance != null)
            {
                PutCollectibleInThePool(oldestCollectibleInstance); // put the oldest one in the pool so it can be reused immediately
            }
            recyclingHappened = true;
        }
        return collectibleInstance;
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
        CollectibleInstance collectible = DequeueCollectibleInstance();

        if (collectible != null)
        {
            CollectibleInfo collectibleInfo = DataManager.instance.GetCollectibleInfoFromType(collectibleType);

            collectible.spawnTime = Time.time;

            // Set Animation style (appearance)
            int animationIndex = 0;
            if (collectibleInfo.MinValueForIcons != null && collectibleInfo.MinValueForIcons.Count > 0)
            {
                int index = 0;
                foreach (float minValue in collectibleInfo.MinValueForIcons)
                {
                    if (bonusValue >= minValue)
                    {
                        animationIndex = index;
                    }
                    index++;
                }
            }
            animationIndex = Mathf.Clamp(animationIndex, 0, collectibleInfo.AnimationStyles.Count - 1);

            collectible.collectibleRenderer.enabled = true;
            collectible.collectibleRenderer.sortingOrder = Random.Range(7, 10);
            int animationStyle = collectibleInfo.AnimationStyles[animationIndex];
            collectible.collectibleAnimator.SetInteger("style", animationStyle);

            // Set type and value
            collectible.collectibleType = collectibleType;
            collectible.collectibleValue = bonusValue;

            // Set position
            collectible.collectibleTransform.position = position;

            // Set movement behaviour
            collectible.usesMagnet = !collectibleInfo.isMoving;
            collectible.fliesAwayFromFrog = collectibleInfo.fliesAwayFromFrog;
            collectible.fliesTowardsFrog = collectibleInfo.fliesTowardsFrog;

            if (collectibleInfo.isMoving)
            {
                collectible.active = true;
                // Set a default move direction that goes away from the frog
                collectible.moveDirection = (collectible.collectibleTransform.position - GameManager.instance.player.transform.position).normalized;
                collectible.collectibleRigidbody.velocity = collectible.moveDirection * movingCollectiblesDefaultSpeed;
                movingCollectiblesQueue.Enqueue(collectible);
                if (verboseLevel == VerboseLevel.MAXIMAL)
                {
                    Debug.Log($"Enqueue moving collectible {collectible.collectibleTransform.name}");
                }
            }

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
                Debug.Log("Impossible to spawn collectible (pool is empty and recycling didn't work)");
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


    #region Fixed Collectibles (not pooled)

    public void SpawnFixedCollectible(FixedCollectible collectible, Vector2 position)
    {
        GameObject newCollectible = Instantiate(fixedCollectiblePrefab, position, Quaternion.identity, fixedCollectiblesParent);
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

        // Decide if that fixed collectible is mandatory or impossible to pick.
        // It depends on a few particular cases:
        if (RunManager.instance.currentPlayedCharacter.characterID.Equals("GHOST") && superCollectible.collectibleType == FixedCollectibleType.FRIEND && superCollectible.collectibleFriendType != FriendType.GHOST)
        {
            // 1 - You are playing as Ghost and you met a companion that is not of ghost type: the companion can't see you or is scared of you
            RunManager.instance.ShowCollectSuperCollectiblePanel(superCollectible, allowAccept: false, forceChoiceDescriptionStr: $"But {superCollectible.collectibleName} is scared of ghosts!", forceChoiceButtonStr:"Oh no!");
        }
        else if (superCollectible.collectibleType == FixedCollectibleType.STATS_ITEM && superCollectible.collectibleStatItemData.itemName.Equals("Figurine"))
        {
            // 2 - You found the figurine, it is cursed and you are forced to pick it up
            RunManager.instance.ShowCollectSuperCollectiblePanel(superCollectible, allowRefuse: false, forceChoiceDescriptionStr: "You feel drawn to it.", forceChoiceButtonStr: "Shiny!");
        }
        else if (superCollectible.collectibleType == FixedCollectibleType.STATS_ITEM && !RunItemManager.instance.IsRunItemUnlocked(superCollectible.collectibleStatItemData.itemName))
        {
            // 3 - You found an item that would complete a quest if you pick it up: you can't choose to not pick it up
            RunManager.instance.ShowCollectSuperCollectiblePanel(superCollectible, allowRefuse: false, forceChoiceDescriptionStr: "It's the first time you see something like this.", forceChoiceButtonStr: "I want it!");
        }
        else
        {
            // Any other situation: you can choose to pick the collectible or not
            RunManager.instance.ShowCollectSuperCollectiblePanel(superCollectible);
        }

        if (verboseLevel == VerboseLevel.MAXIMAL)
        {
            Debug.Log("Collectible Manager - Collect fixed collectible: " + superCollectible.collectibleType.ToString());
        }
    }

    #endregion

    public void CaptureCollectible(Transform collectibleTransform)
    {
        CollectibleInstance collectible = GetCollectibleInstanceFromGameObject(collectibleTransform.gameObject);
        if (collectible != null && collectible.usesMagnet)
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
            UpdateMovingCollectibles(updateCollectiblesCount);
            UpdateCapturedCollectibles(updateCollectiblesCount);
            yield return new WaitForSeconds(delay);
        }
    }

    /// <summary>
    /// Move the collectibles that try to escape the frog.
    /// </summary>
    private void UpdateMovingCollectibles(int maxCount)
    {
        if (GameManager.instance.isGameRunning)
        {
            // Update velocity of all moving collectibles:
            // Far from the frog = moves around randomly
            // Close to the frog = attempt to move away from it 
            Transform playerTransform = GameManager.instance.player.transform;

            int collectiblesToUpdateCount = Mathf.Min(movingCollectiblesQueue.Count, maxCount);
            float distanceWithFrog;
            Vector2 directionTowardsFrog;
            for (int i = 0; i < collectiblesToUpdateCount; i++)
            {
                if (movingCollectiblesQueue.TryDequeue(out CollectibleInstance collectible))
                {
                    // Get distance with Frog
                    directionTowardsFrog = (playerTransform.position - collectible.collectibleTransform.position);
                    distanceWithFrog = directionTowardsFrog.magnitude;

                    if (distanceWithFrog > collectibleDespawnDistance)
                    {
                        if (verboseLevel == VerboseLevel.MAXIMAL)
                        {
                            Debug.Log($"Unspawn collectible {collectible.collectibleTransform.name} because it's too far");
                        }
                        PutCollectibleInThePool(collectible);
                    }
                    else if (distanceWithFrog < collectMinDistance)
                    {
                        // Collect
                        if (verboseLevel == VerboseLevel.MAXIMAL)
                        {
                            Debug.Log($"Collect moving collectible {collectible.collectibleTransform.name}");
                        }
                        RunManager.instance.CollectCollectible(collectible.collectibleType, collectible.collectibleValue);
                        PutCollectibleInThePool(collectible);
                    }
                    else if (distanceWithFrog < movingCollectiblesSafeDistanceWithFrog)
                    {
                        if (collectible.fliesAwayFromFrog)
                        {
                            // Collectible attempts to escape frog
                            collectible.moveDirection = -directionTowardsFrog.normalized;
                            collectible.collectibleRigidbody.velocity = collectible.moveDirection * movingCollectiblesEscapeSpeed;
                            collectible.collectibleAnimator.SetFloat("speed", movingCollectiblesEscapeSpeed);
                            collectible.lastChangeOfDirectionTime = Time.time;
                        }
                        else if (collectible.fliesTowardsFrog)
                        {
                            // Collectible attempts to go to the frog
                            collectible.moveDirection = directionTowardsFrog.normalized;
                            collectible.collectibleRigidbody.velocity = collectible.moveDirection * movingCollectiblesGoTowardsFrogSpeed;
                            collectible.collectibleAnimator.SetFloat("speed", movingCollectiblesGoTowardsFrogSpeed);
                            collectible.lastChangeOfDirectionTime = Time.time;
                        }
                    }
                    else
                    {
                        // Collectible moves around randomly
                        if (Time.time - collectible.lastChangeOfDirectionTime > movingCollectiblesDelayBetweenDirectionChange)
                        {
                            // Set new random direction
                            collectible.moveDirection = Random.insideUnitCircle.normalized;
                            collectible.collectibleRigidbody.velocity = collectible.moveDirection * movingCollectiblesDefaultSpeed;
                            collectible.lastChangeOfDirectionTime = Time.time;
                            if (verboseLevel == VerboseLevel.MAXIMAL)
                            {
                                Debug.Log($"Changed direction of moving collectible {collectible.collectibleTransform.name} to {collectible.moveDirection.ToString()}");
                            }
                        }
                        else
                        {
                            // Keep moving in the same direction
                            collectible.collectibleRigidbody.velocity = collectible.moveDirection * movingCollectiblesDefaultSpeed;
                        }
                        collectible.collectibleAnimator.SetFloat("speed", movingCollectiblesDefaultSpeed);
                    }

                    // If that collectible is still active, we'll update it again next time
                    if (collectible.active)
                    {
                        movingCollectiblesQueue.Enqueue(collectible);
                    }
                }
            }
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
            float distanceWithFrog;
            Vector2 moveDirection;
            float collectibleMovingSpeed = DataManager.instance.capturedCollectiblesSpeed;
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
