using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ShopItemData is a scriptable object that describe an item in the shop.
/// It contains all information of this item such as: its name, description, icon, cost, and starting state (hidden or not, max available level)
/// </summary>
[System.Serializable]
[CreateAssetMenu(fileName = "Shop Item Data", menuName = "ScriptableObjects/Froguelike/Shop Item Data", order = 1)]
public class ShopItemData : ScriptableObject
{
    public string itemName;
    public string description;

    public Sprite icon;

    public int maxLevelAtStart; // How many of those items can you buy at the start of the game?
    public bool hiddenAtStart;

    public List<StatValue> statIncreaseList;

    public List<int> costForEachLevel;
}
