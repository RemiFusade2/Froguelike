using Steamworks;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI;

/// <summary>
/// Describes an instance of a friend, with all relevant data linked to it
/// </summary>
[System.Serializable]
public class FriendInstance
{
    // Data about friend
    public FriendData data;

    // Is friend active of pooled
    public bool active;

    // Is friend temporary or permanent (temporary friends are used for power ups)
    public bool temporary;
    public float lifespan;

    // Times for spawn and last updates
    public float lastUpdateTime;
    public float lastDirectionChangeTime;
    public float spawnTime;

    // References to components
    public GameObject ParentGameObject;

    public GameObject FriendGameObject;
    public Collider2D FriendCollider;
    public Rigidbody2D FriendRigidbody;
    public SpriteRenderer FriendRenderer;
    public Animator FriendAnimator;

    public Transform TonguePositionTransform;

    public WeaponBehaviour TongueScript;
}


public class FriendsManager : MonoBehaviour
{
    public static FriendsManager instance;

    [Header("Settings - Logs")]
    public VerboseLevel verboseLevel = VerboseLevel.NONE;

    [Header("Prefabs")]
    [Tooltip("Friends, they follow you around and eat bugs")]
    public GameObject friendPrefab;

    [Header("Settings - Pooling")]
    public int maxFriends = 200;

    [Header("Settings - Update")]
    public int updateFriendsCount = 100;
    public float updateAllFriendsDelay = 0.25f;

    [Header("Settings - Movement")]
    public AnimationCurve minAngleCurve;
    public AnimationCurve maxAngleCurve;
    [Space]
    [Tooltip("After this distance, temporary friends will unspawn")]
    public float maxDistanceUntilDespawn = 45;
    [Tooltip("After this distance, friends will come back towards frog asap")]
    public float maxDistanceFromFrog = 10;
    [Tooltip("Any closer that this, friends will hop away from frog")]
    public float minDistanceFromFrog = 2;
    [Space]
    [Tooltip("Super speed to catch up with frog")]
    public float speedFactorToCatchUpWithFrogWhenFar = 4;
    [Tooltip("Super strength to catch up with frog")]
    public float forceFactorToCatchUpWithFrogWhenFar = 2;
    [Tooltip("Super speed to get away from frog (only for temporary friends)")]
    public float speedFactorToGetAwayFromFrogWhenLifespanPassed = 4;
    [Tooltip("Super strength to get away from frog (only for temporary friends)")]
    public float forceFactorToGetAwayFromFrogWhenLifespanPassed = 2;

    [Header("Settings - Update")]
    public int updateAllFriendsCount = 50;

    [Header("Runtime")]
    public List<FriendInstance> permanentFriendsList; // Used to check for achievements and chapters conditions
    public List<FriendInstance> activeFriendsList; // Used to update all friends before rendering the frame (their tongue should always align)

    private FriendInstance[] allFriends; // Used to get a friend instance from its ID (index)

    private Queue<FriendInstance> inactiveFriendsPool; // Used to grab an inactive friend instance and use it

    private Queue<FriendInstance> activeFriendsQueue; // Used to update all active friends and move them towards the (or away from) frog

    private Coroutine updateFriendsCoroutine;

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

        activeFriendsQueue = new Queue<FriendInstance>();
    }

    private void Start()
    {
        InstantiateAllFriends();
        ReinitializeUpdateCoroutine();
    }

    private void Update()
    {
        UpdateAllActiveFriends();
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

    private FriendInstance GetFriendInstanceFromGameObject(GameObject friendGameObject)
    {
        int ID = GetIDFromName(friendGameObject.name);
        FriendInstance result = null;
        if (ID != -1)
        {
            result = allFriends[ID];
        }
        return result;
    }

    private void InstantiateAllFriends()
    {
        allFriends = new FriendInstance[maxFriends];
        inactiveFriendsPool = new Queue<FriendInstance>();
        for (int i = 0; i < maxFriends; i++)
        {
            FriendInstance friend = new FriendInstance();
            allFriends[i] = friend;

            friend.ParentGameObject = Instantiate(friendPrefab, DataManager.instance.GetFarAwayPosition(), Quaternion.identity, this.transform);
            friend.ParentGameObject.name = GetNameFromID(i);

            friend.FriendGameObject = friend.ParentGameObject.transform.Find("Friend").gameObject;
            friend.FriendCollider = friend.FriendGameObject.GetComponent<Collider2D>();
            friend.FriendRigidbody = friend.FriendGameObject.GetComponent<Rigidbody2D>();
            friend.FriendRenderer = friend.FriendGameObject.GetComponent<SpriteRenderer>();
            friend.FriendAnimator = friend.FriendGameObject.GetComponent<Animator>();

            friend.TonguePositionTransform = friend.FriendGameObject.transform.Find("Tongue start point");
            friend.TongueScript = friend.ParentGameObject.transform.Find("Friend Tongue").GetComponent<WeaponBehaviour>();

            PutFriendInThePool(friend);
        }
    }

    private void PutFriendInThePool(FriendInstance friend)
    {
        friend.active = false;

        if (activeFriendsList.Contains(friend))
        {
            activeFriendsList.Remove(friend);
        }

        friend.ParentGameObject.transform.position = DataManager.instance.GetFarAwayPosition();

        friend.TongueScript.ResetTongue();

        SetFriendsComponentsEnabled(friend, false);

        inactiveFriendsPool.Enqueue(friend);

        if (verboseLevel == VerboseLevel.MAXIMAL)
        {
            Debug.Log($"Put friend {friend.ParentGameObject.name} in the pool");
        }
    }

    private void SetFriendsComponentsEnabled(FriendInstance friend, bool enabled)
    {
        friend.FriendCollider.enabled = enabled;
        friend.FriendRigidbody.velocity = Vector2.zero;
        friend.FriendRigidbody.simulated = enabled;
        friend.FriendRenderer.enabled = enabled;
        friend.FriendAnimator.enabled = enabled;
    }

    /// <summary>
    /// Remove all friends from the map.
    /// Put them back in the pool.
    /// </summary>
    public void ClearAllFriends(bool onlyTemporary = false)
    {
        int activeFriendsToCheckCount = activeFriendsQueue.Count();

        for (int i = 0; i < activeFriendsToCheckCount; i++)
        {
            if (activeFriendsQueue.TryDequeue(out FriendInstance friend))
            {
                if (!onlyTemporary || friend.temporary)
                {
                    // Either we remove all friend (permanent included), or this friend is temporary anyway
                    if (activeFriendsList.Contains(friend))
                    {
                        activeFriendsList.Remove(friend);
                    }
                    if (permanentFriendsList.Contains(friend))
                    {
                        permanentFriendsList.Remove(friend);
                    }
                    PutFriendInThePool(friend);
                }
                else
                {
                    // Friend is permanent and we don't remove permanent friends
                    activeFriendsQueue.Enqueue(friend);
                }
            }
        }
    }

    private void ReinitializeUpdateCoroutine()
    {
        if (updateFriendsCoroutine != null)
        {
            StopCoroutine(updateFriendsCoroutine);
        }
        updateFriendsCoroutine = StartCoroutine(UpdateAllFriendsMovementAsync(updateAllFriendsDelay));
    }

    private IEnumerator UpdateAllFriendsMovementAsync(float delay)
    {
        while (true)
        {
            UpdateAllFriendsMovement();
            yield return new WaitForSeconds(delay);
        }
    }

    /// <summary>
    /// Update friends movement.
    /// Use the Queue so we update friends which haven't been updated for a while first.
    /// </summary>
    private void UpdateAllFriendsMovement()
    {
        if (GameManager.instance.isGameRunning)
        {
            int maxFriendsToUpdateCount = updateAllFriendsCount;
            Transform playerTransform = GameManager.instance.player.transform;

            int friendsToUpdateCount = Mathf.Min(maxFriendsToUpdateCount, activeFriendsQueue.Count);
            Vector3 frogPosition = playerTransform.position;
            Vector2 frogPositionVec2 = new Vector2(frogPosition.x, frogPosition.y);

            for (int i = 0; i < friendsToUpdateCount; i++)
            {
                if (activeFriendsQueue.TryDequeue(out FriendInstance friend))
                {
                    if (friend.active)
                    {
                        float distanceWithFrog = Vector2.Distance(frogPosition, friend.FriendGameObject.transform.position);

                        float hopForce = friend.data.hopForce;
                        float timeBetweenDirectionChange = friend.data.timeBetweenDirectionChange;

                        if (distanceWithFrog > maxDistanceUntilDespawn)
                        {
                            // Friend should despawn because it's too far
                            if (friend.temporary)
                            {
                                // Temporary friends just despawn
                                friend.active = false;
                            }
                            else
                            {
                                // Permanent friends get teleported closer
                                float spawnCircleRadius = maxDistanceUntilDespawn - 5;
                                Vector2 teleportPosition = frogPositionVec2 + Random.insideUnitCircle * spawnCircleRadius;

                                // Check if that random point is on an obstacle
                                int layerMask = LayerMask.GetMask("Rock");
                                if (Physics2D.OverlapCircle(teleportPosition, 0.5f, layerMask) != null)
                                {
                                    // Nope! Friend can't be teleported on a rock!
                                }
                                else
                                {
                                    // Reset tongue
                                    friend.TongueScript.ResetTongue();

                                    // Teleport friend
                                    friend.FriendGameObject.transform.position = teleportPosition;
                                }
                            }
                        }
                        else
                        {
                            // Friend is close enough, we need to update it
                            if (friend.temporary && (Time.time - friend.spawnTime) > friend.lifespan)
                            {
                                // Friend is temporary and its lifespan has passed, it will need to move away from frog quickly
                                timeBetweenDirectionChange /= speedFactorToGetAwayFromFrogWhenLifespanPassed;
                            }
                            else if (distanceWithFrog > maxDistanceFromFrog)
                            {
                                // Friend is temporary or permanent, but it's far from frog, it will need to move fast towards frog quickly
                                timeBetweenDirectionChange /= speedFactorToCatchUpWithFrogWhenFar;
                            }

                            if (Time.time - friend.lastDirectionChangeTime >= timeBetweenDirectionChange)
                            {
                                // Friend didn't change direction for a while, it should HOP!
                                Vector3 directionTowardsFrog = (frogPosition - friend.FriendGameObject.transform.position).normalized;

                                // Compute min Angle and max Angle
                                // Angle is the friend movement direction angle compared to the vector that goes towards frog
                                float t = Mathf.Clamp((distanceWithFrog - minDistanceFromFrog) / maxDistanceFromFrog, 0, 1);
                                float minAngle = minAngleCurve.Evaluate(t);
                                float maxAngle = maxAngleCurve.Evaluate(t);

                                if (t >= 1 && !friend.temporary)
                                {
                                    // Friend is a bit far from frog
                                    hopForce *= forceFactorToCatchUpWithFrogWhenFar;
                                }
                                if (friend.temporary && (Time.time - friend.spawnTime) > friend.lifespan)
                                {
                                    // Friend is temporary and its lifespan has passed
                                    minAngle = 180;
                                    maxAngle = 180;
                                    hopForce *= forceFactorToGetAwayFromFrogWhenLifespanPassed;
                                }

                                // Compute direction
                                float randomAngle = (Random.Range(minAngle, maxAngle) * (Random.Range(0, 2) == 0 ? -1 : 1)) * Mathf.Deg2Rad;
                                float dirX = Mathf.Cos(randomAngle) * directionTowardsFrog.x - Mathf.Sin(randomAngle) * directionTowardsFrog.y;
                                float dirY = Mathf.Sin(randomAngle) * directionTowardsFrog.x + Mathf.Cos(randomAngle) * directionTowardsFrog.y;
                                Vector3 direction = new Vector3(dirX, dirY, 0);

                                // Add Force
                                friend.FriendRigidbody.AddForce(direction * hopForce, ForceMode2D.Impulse);

                                friend.lastDirectionChangeTime = Time.time;
                            }

                            // Attempt to use Tongue
                            friend.TongueScript.TryAttack();
                        }

                        if (friend.active)
                        {
                            friend.lastUpdateTime = Time.time;
                            activeFriendsQueue.Enqueue(friend);
                        }
                        else
                        {
                            PutFriendInThePool(friend);
                        }
                    }
                }
            }
        }
    }

    private void UpdateAllActiveFriends()
    {
        foreach (FriendInstance friendInstance in activeFriendsList)
        {
            // Set tongue position
            friendInstance.TongueScript.SetTonguePosition(friendInstance.TonguePositionTransform);

            // Set up orientation
            float friendOrientationAngle = 90 + 90 * Mathf.RoundToInt((Vector2.SignedAngle(friendInstance.FriendRigidbody.velocity.normalized, Vector2.right)) / 90);
            friendInstance.FriendGameObject.transform.localRotation = Quaternion.Euler(0, 0, -friendOrientationAngle);
            // Clamp speed
            float friendSpeed = Mathf.Clamp(friendInstance.FriendRigidbody.velocity.magnitude, 0, 3);
            // Set speed in Animator
            friendInstance.FriendAnimator.SetFloat("Speed", friendSpeed);
        }
    }


    public void AddActiveFriend(FriendType friendType, Vector2 friendPosition, bool temporary = false, float lifespan = 0)
    {
        FriendData friendData = DataManager.instance.GetDataForFriend(friendType);

        if (inactiveFriendsPool.TryDequeue(out FriendInstance friend))
        {
            // Place parent at zero
            friend.ParentGameObject.transform.position = Vector3.zero;

            // Position friend on the given position
            friend.FriendGameObject.transform.position = friendPosition;

            friend.data = friendData;

            // Setup Rigidbody
            friend.FriendRigidbody.mass = friendData.Mass;
            friend.FriendRigidbody.drag = friendData.LinearDrag;

            // Activate all components
            SetFriendsComponentsEnabled(friend, true);

            // Setup Tongue
            friend.TongueScript.Initialize(friend.data.tongueType, friend.data.tongueBaseStats);
            friend.TongueScript.ResetTongue();

            friend.temporary = temporary;
            friend.lifespan = lifespan;

            friend.spawnTime = Time.time;

            friend.FriendAnimator.SetInteger("Style", friendData.style);

            friend.active = true;

            activeFriendsList.Add(friend);
            activeFriendsQueue.Enqueue(friend);

            if (!temporary)
            {
                permanentFriendsList.Add(friend);
            }

            if (verboseLevel == VerboseLevel.MAXIMAL)
            {
                Debug.Log($"Friend Manager - Add Friend: {friendType.ToString()} at position {friendPosition.ToString()}");
            }
        }

    }

    private void SetFriendPosition(FriendInstance friend, Vector2 position)
    {
        friend.FriendGameObject.transform.position = position;
    }

    public void PlacePermanentFriendsInACircleAroundFrog(Vector2 frogPosition)
    {
        int numberOfActiveFriends = permanentFriendsList.Count();
        float deltaAngle = 0;
        float distance = 2.5f;
        if (numberOfActiveFriends > 0)
        {
            deltaAngle = (Mathf.PI * 2) / numberOfActiveFriends;
        }
        float angle = 0;
        foreach (FriendInstance friend in permanentFriendsList)
        {
            friend.TongueScript.ResetTongue();
            SetFriendPosition(friend, frogPosition + distance * (Mathf.Cos(angle) * Vector2.right + Mathf.Sin(angle) * Vector2.up));
            angle += deltaAngle;
        }
    }

    public bool HasPermanentFriend(FriendType friendType)
    {
        bool friendIsActive = false;
        foreach (FriendInstance friend in permanentFriendsList)
        {
            friendIsActive |= (friend.data.friendType == friendType && friend.active);
            if (friendIsActive) 
                break;
        }
        return friendIsActive;
    }
}
