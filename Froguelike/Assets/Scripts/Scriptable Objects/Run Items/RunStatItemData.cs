using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[System.Serializable]
public class RunStatItemLevel : RunItemLevel
{
    [Header("Character Boosts")]
    public StatsWrapper statUpgrades;
}

[System.Serializable]
[CreateAssetMenu(fileName = "Run Stat Item Data", menuName = "ScriptableObjects/Froguelike/Run Stat Item Data", order = 1)]
public class RunStatItemData : RunItemData
{
    [Header("Levels")]
    public List<RunStatItemLevel> statBoostLevels;

    #region overrides

    public override bool Equals(object other)
    {
        if (other is RunStatItemData)
        {
            return itemName.Equals((other as RunStatItemData).itemName);
        }
        return base.Equals(other);
    }

    public override int GetHashCode()
    {
        return itemName.GetHashCode();
    }

    #endregion
}
