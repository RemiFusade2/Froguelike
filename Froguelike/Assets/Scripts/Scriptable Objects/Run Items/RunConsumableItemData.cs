using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// RunConsumableItemEffect stores the effect of this consumable (Heals HP, or gives froins, or increase score... or everything at the same time?)
/// </summary>
[System.Serializable]
public class RunConsumableItemEffect
{
    [Tooltip("How many HP this item heals")]
    public int healthBonus;
    [Tooltip("How many XP this item gives")]
    public int xpBonus;
    [Tooltip("How many \"kills\" this item gives (counts for scoring)")]
    public int scoreBonus;
    [Tooltip("How many froins this item gives")]
    public int currencyBonus;
}

/// <summary>
/// RunConsumableItemData inherits RunItemData.
/// It specializes the class for consumable items that don't have a level up system.
/// Instead, these items apply once when they are chosen during a level up.
/// Also they keep showing up, even if you already bought them.
/// </summary>
[System.Serializable]
[CreateAssetMenu(fileName = "Run Consumable Item Data", menuName = "ScriptableObjects/Froguelike/Run Consumable Item Data", order = 1)]
public class RunConsumableItemData : RunItemData
{
    [Tooltip("A short description (two lines max, split with \\n) to explain what this item gives")]
    public string description;

    [Tooltip("Describe the effect of this item")]
    public RunConsumableItemEffect effect;
}
