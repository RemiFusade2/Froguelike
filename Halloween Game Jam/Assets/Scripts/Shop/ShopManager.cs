using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public enum SHOP_ITEM
{
    MAXHP_UPGRADE
}

/// <summary>
/// ShopManager is a singleton class that deals with the shop elements.
/// It stores the current stats increases from shop items.
/// It stores all available shop items and their current level.
/// </summary>
public class ShopManager : MonoBehaviour
{
    public static ShopManager instance;

    public List<StatValue> statsBonuses;

    private void Awake()
    {
        instance = this;
    }

    public void ResetShop()
    {
        statsBonuses.Clear();
    }

    public void BuyItem(ShopItemData item)
    {
        Debug.Log("Buying an item");
    }
}
