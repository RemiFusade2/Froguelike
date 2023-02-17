using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// RunStatItemLevel stores the effect of one level of a Stat item (increasing or decreasing one or more stats)
/// </summary>
[System.Serializable]
public class RunStatItemLevel : RunItemLevel
{
    [Header("Character Boosts")]
    [Tooltip("A list of Stats that this level improves")]
    public StatsWrapper statUpgrades;
}

/// <summary>
/// RunStatItemData inherits RunItemData.
/// It specializes the class for Stats items with a level up system.
/// A Stat item is an item that increases one or more stats, every time you upgrade it.
/// Most of them would stop showing up if you upgrade them to their max level.
/// </summary>
[System.Serializable]
[CreateAssetMenu(fileName = "Run Stat Item Data", menuName = "ScriptableObjects/Froguelike/Run Stat Item Data", order = 1)]
public class RunStatItemData : RunItemData
{
    [Header("Levels")]
    [Tooltip("A list of Levels for this Stat item. The number of elements in that list will give the max level of this item")]
    public List<RunStatItemLevel> statBoostLevels;
}
