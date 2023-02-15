using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[System.Serializable]
public class RunWeaponData
{
    public string weaponName;
    public string description;

    [Header("Prefab reference")]
    public GameObject weaponPrefab;

    [Header("Base settings")]
    public WeaponType weaponType;
    public bool comesBackAfterEatingFlies; // probably need to remove this

    [Header("Base Stats")]
    public WeaponStatsWrapper weaponBaseStats;
}

[System.Serializable]
public class RunWeaponItemLevel : RunItemLevel
{
    [Header("Weapon Boosts")]
    public WeaponStatsWrapper weaponStatUpgrades;
}

[System.Serializable]
[CreateAssetMenu(fileName = "Run Weapon Item Data", menuName = "ScriptableObjects/Froguelike/Run Weapon Item Data", order = 1)]
public class RunWeaponItemData : RunItemData
{
    [Header("Weapon settings")]
    public RunWeaponData weaponData;

    [Header("Levels")]
    public List<RunWeaponItemLevel> weaponBoostLevels;

    #region overrides

    public override bool Equals(object other)
    {
        if (other is RunWeaponItemData)
        {
            return itemName.Equals((other as RunWeaponItemData).itemName);
        }
        return base.Equals(other);
    }

    public override int GetHashCode()
    {
        return itemName.GetHashCode();
    }

    #endregion
}
