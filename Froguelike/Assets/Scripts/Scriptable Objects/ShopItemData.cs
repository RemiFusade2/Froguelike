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
    [Tooltip("The item name as displayed in the shop")]
    public string itemName;

    [Tooltip("The item description as displayed in the shop")]
    public string description;

    [Tooltip("The item icon as displayed in the shop")]
    public Sprite icon;

    [Tooltip("How many level of this item can you buy at the start of the game (this max level can be increased with achievements later)")]
    public int maxLevelAtStart_EA;
    [Tooltip("How many level of this item can you buy at the start of the demo (this max level can be increased with achievements later)")]
    public int maxLevelAtStart_Demo;
    [Tooltip("How many level of this item can you have in the demo")]
    public int maxLevel_Demo;

    [Tooltip("Is this item hidden at the start (if yes, it would be unlocked later with achievements)")]
    public bool hiddenAtStart;

    [Tooltip("Which stats are this item increasing")]
    public List<StatValue> statIncreaseList;
    [Tooltip("Does this item increase compass level")]
    public bool compassLevelIncrease;

    [Tooltip("How much each level of this item costs")]
    public List<int> costForEachLevel;
}
