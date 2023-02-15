using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Parent class that will be inherited
/// </summary>
[System.Serializable]
public class RunItemLevel
{
    public string description;
}

/// <summary>
/// Parent class that will be inherited
/// </summary>
[System.Serializable]
public class RunItemData : ScriptableObject
{
    [Header("Base settings")]
    public string itemName;
    public Sprite icon;

    public bool unlockedFromStart;

    #region overrides

    public override bool Equals(object other)
    {
        if (other is RunItemData)
        {
            return itemName.Equals((other as RunItemData).itemName);
        }
        return base.Equals(other);
    }

    public override int GetHashCode()
    {
        return itemName.GetHashCode();
    }

    #endregion
    
    public int GetMaxLevel()
    {
        return GetMaxLevelCount() + 1;
    }

    public int GetMaxLevelCount()
    {
        int maxLevel = 0;

        if (this is RunStatItemData)
        {
            maxLevel = (this as RunStatItemData).statBoostLevels.Count;
        }
        if (this is RunWeaponItemData)
        {
            maxLevel = (this as RunWeaponItemData).weaponBoostLevels.Count;
        }

        return maxLevel;
    }

    public RunItemType GetItemType()
    {
        RunItemType itemType = RunItemType.CONSUMABLE;

        if (this is RunStatItemData)
        {
            itemType = RunItemType.STAT_BONUS;
        }
        if (this is RunWeaponItemData)
        {
            itemType = RunItemType.WEAPON;
        }

        return itemType;
    }
}

