using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// In case one day we want enemies that shoot stuff
/// </summary>
[System.Serializable]
public enum EnemyAttackPattern
{
    NO_ATTACK,
    SHOOT_STUFF
}

/// <summary>
/// EnemyData stores all data about one enemy (one Enemy is usually an association of EnemyType and DifficultyTier)
/// </summary>
[System.Serializable]
[CreateAssetMenu(fileName = "Enemy Data", menuName = "ScriptableObjects/Froguelike/Enemy Data", order = 1)]
public class EnemyData : ScriptableObject
{
    [Header("Unique identifier")]
    [Tooltip("The name is used as an ID, there must not be two enemies with the same name")]
    public string enemyName = "[Unique name]";

    [Header("Lore")]
    [Tooltip("The list of descriptions would be displayed on the bestiary screen. Possibly with multiple levels unlocked when enough enemies of this kind were eaten")]
    public List<string> description;

    [Header("Prefab")]
    [Tooltip("The prefab will describe how the enemy looks, which layer it is on, and how heavy it is")]
    public GameObject prefab;

    [Header("Enemy settings - stats")]
    [Tooltip("Does this enemy shoot stuff? NOT IMPLEMENTED YET")]
    public EnemyAttackPattern attackPattern = EnemyAttackPattern.NO_ATTACK;
    [Tooltip("How much damage this enemy can take before being eaten")]
    public float maxHP = 10;
    [Tooltip("How much XP this enemy gives when eaten")]
    public float xPBonus = 1;
    [Tooltip("How fast this enemy moves by default")]
    public float moveSpeed = 1;
    [Tooltip("How much damage/s this enemy inflicts when touching the frog")]
    public float damage = 10;

    [Header("Enemy settings - visuals")]
    [Tooltip("Thickness of the outline by default")]
    public int outlineThickness = 1;
    [Tooltip("Color of the outline by default")]
    public Color outlineColor;
}
