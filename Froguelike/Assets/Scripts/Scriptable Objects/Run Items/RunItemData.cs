using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Parent class that will be inherited.
/// A RunItemLevel represents one level of a RunItem.
/// It will be inherited by StatItems and WeaponItems alike, the only common thing being the "description" which shows when leveling up.
/// </summary>
[System.Serializable]
public class RunItemLevel
{
    [Tooltip("A short description (two lines max, split with \\n) to explain what this level improves")]
    public string description;
}

/// <summary>
/// Parent class that will be inherited.
/// RunItemData represents one RunItem.
/// It will be inherited by ConsumableItem, StatItems, and WeaponItems alike.
/// The common data are the item name, icon, and if it is unlocked from the start or not.
/// The common methods are used to know which type this item is (Stat, Weapon, or Consumable), and how many levels it has.
/// </summary>
[System.Serializable]
public class RunItemData : ScriptableObject
{
    [Header("Base settings")]
    [Tooltip("The name of this item")]
    public string itemName;
    [Tooltip("The icon of this item")]
    public Sprite icon;
    [Space]
    [Tooltip("Is this item unlocked from the start of the game? It may be unlocked later with an achievement")]
    public bool unlockedFromStart;

    #region overrides

    /// <summary>
    /// Two items having the same name are considered to be the same item.
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
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
    
    /// <summary>
    /// The starting level being 1, adding 1 to the number of levels it can gain would give the "max level" of this item.
    /// </summary>
    /// <returns></returns>
    public int GetMaxLevel()
    {
        return GetMaxLevelCount() + 1;
    }

    /// <summary>
    /// How many levels can this item gain
    /// </summary>
    /// <returns></returns>
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

    /// <summary>
    /// Returns the RunItemType depending on which class this RunItem is.
    /// </summary>
    /// <returns></returns>
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

