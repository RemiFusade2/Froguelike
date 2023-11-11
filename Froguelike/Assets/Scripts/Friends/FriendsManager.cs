using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI;

[System.Serializable]
public class FriendInstance
{
    public GameObject GameObject;
    public Transform TonguePositionTransform;
    public WeaponBehaviour Tongue;
    public Rigidbody2D Rigidbody;
    public Animator Animator;

    public FriendInfo Info;

    public bool temporary;
    public float lifespan;

    public float lastUpdateTime;
    public float lastDirectionChangeTime;

    public float spawnTime;
}


public class FriendsManager : MonoBehaviour
{
    public static FriendsManager instance;

    [Header("Settings - Logs")]
    public VerboseLevel logsVerboseLevel = VerboseLevel.NONE;

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

    [Header("Settings - Update")]
    public int updateAllFriendsCount = 50;

    [Header("Runtime")]
    public List<FriendInstance> activeFriends;

    private Queue<FriendInstance> friendsToUpdateQueue;

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

    private void Start()
    {
        friendsToUpdateQueue = new Queue<FriendInstance>();
    }
    private void Update()
    {
        UpdateAllFriends();
    }
    private void FixedUpdate()
    {
        FixedUpdateAllFriends();
    }

    /// <summary>
    /// Update friends
    /// </summary>
    private void UpdateAllFriends()
    {
        if (GameManager.instance.isGameRunning)
        {
            int maxFriendsToUpdateCount = updateAllFriendsCount;
            Transform playerTransform = GameManager.instance.player.transform;

            List<FriendInstance> friendsToDespawn = new List<FriendInstance>();

            int friendsToUpdateCount = Mathf.Min(maxFriendsToUpdateCount, friendsToUpdateQueue.Count);
            Vector3 frogPosition = playerTransform.position;

            for (int i = 0; i < friendsToUpdateCount; i++)
            {
                FriendInstance friend = friendsToUpdateQueue.Dequeue();

                if (friend.GameObject != null)
                {
                    float distanceWithFrog = Vector2.Distance(frogPosition, friend.GameObject.transform.position);

                    float hopForce = friend.Info.hopForce;
                    float timeBetweenDirectionChange = friend.Info.timeBetweenDirectionChange;
                    if (friend.temporary && (Time.time - friend.spawnTime) > friend.lifespan)
                    {
                        // Friend is temporary and its lifespan has passed
                        timeBetweenDirectionChange *= 0.5f; // it's gonna move twice as fast

                        if (distanceWithFrog > maxDistanceUntilDespawn)
                        {
                            // Friend is too far
                            friendsToDespawn.Add(friend);
                            continue; // despawn that friend, do not update its movement, do not enqueue it
                        }
                    }

                    if (!friend.temporary)
                    {
                        if (distanceWithFrog > maxDistanceUntilDespawn)
                        {
                            timeBetweenDirectionChange *= 0.33f; // it's gonna move trice as fast to catch up with frog
                        }
                        else if (distanceWithFrog > maxDistanceFromFrog)
                        {
                            timeBetweenDirectionChange *= 0.5f; // it's gonna move twice as fast to catch up with frog
                        }
                    }

                    // HOP?
                    if (Time.time - friend.lastDirectionChangeTime >= timeBetweenDirectionChange)
                    {
                        // HOP!
                        Vector3 directionTowardsFrog = (frogPosition - friend.GameObject.transform.position).normalized;

                        // Compute min Angle and max Angle
                        // Angle is the friend movement direction angle compared to the vector that goes towards frog
                        float t = Mathf.Clamp((distanceWithFrog - minDistanceFromFrog) / maxDistanceFromFrog, 0, 1);
                        float minAngle = minAngleCurve.Evaluate(t);
                        float maxAngle = maxAngleCurve.Evaluate(t);

                        if (t >= 1 && !friend.temporary)
                        {
                            // Friend is a bit far from frog
                            hopForce *= 1.5f;
                        }
                        if (friend.temporary && (Time.time - friend.spawnTime) > friend.lifespan)
                        {
                            // Friend is temporary and its lifespan has passed
                            minAngle = 180;
                            maxAngle = 180;
                            hopForce *= 1.5f;
                        }

                        // Compute direction
                        float randomAngle = (Random.Range(minAngle, maxAngle) * (Random.Range(0, 2) == 0 ? -1 : 1)) * Mathf.Deg2Rad;
                        float dirX = Mathf.Cos(randomAngle) * directionTowardsFrog.x - Mathf.Sin(randomAngle) * directionTowardsFrog.y;
                        float dirY = Mathf.Sin(randomAngle) * directionTowardsFrog.x + Mathf.Cos(randomAngle) * directionTowardsFrog.y;
                        Vector3 direction = new Vector3(dirX, dirY, 0);

                        // Add Force
                        friend.Rigidbody.AddForce(direction * hopForce, ForceMode2D.Impulse);

                        friend.lastDirectionChangeTime = Time.time;
                    }

                    // Attempt to Attack
                    friend.Tongue.TryAttack();

                    friend.lastUpdateTime = Time.time;
                    friendsToUpdateQueue.Enqueue(friend);
                }
            }

            foreach (FriendInstance friend in friendsToDespawn)
            {
                activeFriends.Remove(friend);
                Destroy(friend.GameObject.transform.parent.gameObject, 0.01f);
            }
        }
    }

    private void FixedUpdateAllFriends()
    {
        foreach (FriendInstance friendInstance in activeFriends)
        {
            // Set tongue position
            friendInstance.Tongue.SetTonguePosition(friendInstance.TonguePositionTransform);

            // Set up orientation
            float friendOrientationAngle = 90 + 90 * Mathf.RoundToInt((Vector2.SignedAngle(friendInstance.Rigidbody.velocity.normalized, Vector2.right)) / 90);
            friendInstance.GameObject.transform.localRotation = Quaternion.Euler(0, 0, -friendOrientationAngle);
            // Clamp speed
            float friendSpeed = Mathf.Clamp(friendInstance.Rigidbody.velocity.magnitude, 0, 3);
            // Set speed in Animator
            friendInstance.Animator.SetFloat("Speed", friendSpeed);
        }
    }


    public void AddActiveFriend(FriendType friendType, Vector2 friendPosition, bool temporary = false, float lifespan = 0)
    {
        FriendInfo friendInfo = DataManager.instance.GetInfoForFriend(friendType);

        GameObject friendGameObject = Instantiate(friendInfo.prefab, this.transform);
        FriendInstance friendInstance = new FriendInstance();

        friendGameObject.transform.position = friendPosition + Random.insideUnitCircle;

        friendInstance.Tongue = friendGameObject.transform.Find("Friend Tongue").GetComponent<WeaponBehaviour>();
        friendInstance.Tongue.ResetTongue();

        Transform friendTransform = friendGameObject.transform.Find("Friend");
        friendInstance.GameObject = friendTransform.gameObject;

        friendInstance.TonguePositionTransform = friendTransform.Find("Tongue start point");

        friendInstance.Rigidbody = friendTransform.GetComponent<Rigidbody2D>();
        friendInstance.Animator = friendTransform.GetComponent<Animator>();

        friendInstance.Info = friendInfo;
        friendInstance.temporary = temporary;
        friendInstance.lifespan = lifespan;

        friendInstance.spawnTime = Time.time;

        friendInstance.Animator.SetInteger("Style", friendInfo.style);

        activeFriends.Add(friendInstance);
        friendsToUpdateQueue.Enqueue(friendInstance);

        if (logsVerboseLevel == VerboseLevel.MAXIMAL)
        {
            Debug.Log($"Friend Manager - Add Friend: {friendType.ToString()} at position {friendPosition.ToString()}");
        }
    }

    private void SetFriendPosition(FriendInstance friend, Vector2 position)
    {
        friend.GameObject.transform.position = position;
    }

    public void SetFriendsInACircleAroundFrog(Vector2 frogPosition)
    {
        int numberOfActiveFriends = activeFriends.Count(x => !x.temporary);
        float deltaAngle = 0;
        float distance = 2.5f;
        if (numberOfActiveFriends > 0)
        {
            deltaAngle = (Mathf.PI * 2) / numberOfActiveFriends;
        }
        float angle = 0;
        foreach (FriendInstance friend in activeFriends)
        {
            if (!friend.temporary)
            {
                SetFriendPosition(friend, frogPosition + distance * (Mathf.Cos(angle) * Vector2.right + Mathf.Sin(angle) * Vector2.up));
                angle += deltaAngle;
            }
        }
    }

    public void ClearFriends(bool onlyTemporary = false)
    {
        List<FriendInstance> friendsToRemove = new List<FriendInstance>();
        foreach (FriendInstance friend in activeFriends)
        {
            if (!onlyTemporary || friend.temporary)
            {
                Destroy(friend.GameObject.transform.parent.gameObject, 0.01f);
                friendsToRemove.Add(friend);
            }
        }
        foreach (FriendInstance friend in friendsToRemove)
        {
            activeFriends.Remove(friend);
        }
    }

    public bool HasActiveFriend(FriendType friendType)
    {
        bool friendIsActive = false;
        foreach (FriendInstance friend in activeFriends)
        {
            friendIsActive |= (friend.Info.friendType == friendType);
            if (friendIsActive) break;
        }
        return friendIsActive;
    }
}
