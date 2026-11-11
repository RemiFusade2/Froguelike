using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// RunPermanentUpgradeItemEffect stores the effect of this permanent upgrade
/// </summary>
[System.Serializable]
public class RunPermanentUpgradeItemEffect
{
    [Tooltip("A list of Stats that this item improves permanently")]
    public StatsWrapper statUpgrades;
}

/// <summary>
/// RunPermanentUpgradeItemData inherits RunItemData.
/// It specializes the class for permanent upgrade items that don't have a level up system.
/// Instead, these items apply once when they are chosen during a level up.
/// </summary>
[System.Serializable]
[CreateAssetMenu(fileName = "Run Permanent Upgrade Item Data", menuName = "ScriptableObjects/Froguelike/Run Permanent Upgrade Item Data", order = 1)]
public class RunPermanentUpgradeItemData : RunItemData
{
    [Tooltip("A short description (two lines max, split with \\n) to explain what this item gives")]
    public string description;

    [Tooltip("Describe the effect of this item")]
    public RunPermanentUpgradeItemEffect effect;
}
