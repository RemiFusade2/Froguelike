using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class RunConsumableItemEffect
{
    public int healthBonus;
    public int xpBonus;
    public int scoreBonus;
    public int currencyBonus;
}

[System.Serializable]
[CreateAssetMenu(fileName = "Run Consumable Item Data", menuName = "ScriptableObjects/Froguelike/Run Consumable Item Data", order = 1)]
public class RunConsumableItemData : RunItemData
{
    public string description;

    public RunConsumableItemEffect effect;

    #region overrides

    public override bool Equals(object other)
    {
        if (other is RunConsumableItemData)
        {
            return itemName.Equals((other as RunConsumableItemData).itemName);
        }
        return base.Equals(other);
    }

    public override int GetHashCode()
    {
        return itemName.GetHashCode();
    }

    #endregion
}
