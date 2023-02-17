using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// RunWeaponData describes information about a weapon, including its initial stats.
/// </summary>
[System.Serializable]
public class RunWeaponData
{
    [Header("Main info")]
    [Tooltip("The name of that weapon (may be redundant with the item name)")]
    public string weaponName;

    [Tooltip("The description of that weapon. You can read it the first time you pick that weapon item")]
    public string description;

    [Header("Prefab reference")]
    [Tooltip("The prefab that is spawn when a new instance of that weapon appears in game")]
    public GameObject weaponPrefab;

    [Header("Base settings")]
    [Tooltip("The type of that weapon, is then used in the WeaponBehaviour script to choose that weapon specific behaviour")]
    public WeaponType weaponType;

    [Header("Base Stats")]
    [Tooltip("Basically represents this weapons stats at level 1")]
    public WeaponStatsWrapper weaponBaseStats;
}

/// <summary>
/// RunWeaponItemLevel stores the effect of one level of a Weapon item (increasing or decreasing one or more weapon stats)
/// </summary>
[System.Serializable]
public class RunWeaponItemLevel : RunItemLevel
{
    [Header("Weapon Boosts")]
    [Tooltip("A list of Weapon Stats that this level improves")]
    public WeaponStatsWrapper weaponStatUpgrades;
}

/// <summary>
/// RunWeaponItemData inherits RunItemData.
/// It specializes the class for Weapons, with a level up system.
/// A Weapon item is an item that add a weapon to the game, then increase its stats every time you upgrade it.
/// Most of them would stop showing up if you upgrade them to their max level.
/// </summary>
[System.Serializable]
[CreateAssetMenu(fileName = "Run Weapon Item Data", menuName = "ScriptableObjects/Froguelike/Run Weapon Item Data", order = 1)]
public class RunWeaponItemData : RunItemData
{
    [Header("Weapon settings")]
    [Tooltip("A set of data describing the initial state of that weapon. Basically represents its stats at level 1")]
    public RunWeaponData weaponData;

    [Header("Levels")]
    [Tooltip("A list of Levels for this Weapon item. The number of elements in that list will give the amount of level up of that weapon (above the first level)")]
    public List<RunWeaponItemLevel> weaponBoostLevels;
}
