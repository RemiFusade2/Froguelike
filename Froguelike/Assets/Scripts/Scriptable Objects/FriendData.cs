using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// FriendData is a scriptable object that describes a friend frog that follows you.
/// It contains all information of this friend such as: 
/// - its type (from Enum)
/// - its style (for Animator)
/// - its Mass (for Rigidbody)
/// - its Linear Drag (for Rigidbody too, it has to do with how it's slowed down after movement)
/// - its tongue type
/// - its tongue base stats
/// - its hop force (how fast it moves when jumping)
/// - the time between two jumps
/// </summary>
[System.Serializable]
[CreateAssetMenu(fileName = "Friend Data", menuName = "ScriptableObjects/Froguelike/Friend Data", order = 1)]
public class FriendData : ScriptableObject
{
    [Tooltip("The type of this friend (picked from Enum)")]
    public FriendType friendType; 

    [Tooltip("The picture used to represent this friend in UI")]
    public Sprite sprite;
    [Tooltip("The style value used in Animator to know which animations to play")]
    public int style; // for animator

    // Rigidbody:
    [Tooltip("The mass of this friend's Rigidbody")]
    public float Mass;
    [Tooltip("The linear drag of this friend's Rigidbody")]
    public float LinearDrag;

    // Tongue type and base stats
    [Tooltip("The type of tongue this friend uses")]
    public TongueType tongueType;
    [Tooltip("The base stats of this friends tongue")]
    public TongueStatsWrapper tongueBaseStats;

    [Tooltip("The force applied to this friend when it jumps")]
    public float hopForce;
    [Tooltip("The standard time between two jumps")]
    public float timeBetweenDirectionChange;
}

/// <summary>
/// A Friend can help you. Here are the different friends available.
/// </summary>
[System.Serializable]
public enum FriendType
{
    FROG,
    TOAD,
    GHOST,
    POISONOUS,
    BLUE
}