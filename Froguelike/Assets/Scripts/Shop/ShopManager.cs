using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ShopItem describes an item in the shop in its current state.
/// It has a reference to ShopItemData, the scriptable object that describes the item. This is not serialized with the rest.
/// It keeps the itemName there for serialization. When saving/loading this item from a save file, the name will be used to retrieve the right item in the program.
/// The information that can change at runtime are:
/// - currentLevel, is the number of times this item has been bought. Is reset to zero when reseting the shop.
/// - maxLevel, is the number of times this can be bought at max. Can be "upgraded" through an achievement.
/// - hidden, is the status of the item. Is it visible in the shop? This value can change when the item is unlocked through an achievement.
/// </summary>
[System.Serializable]
public class ShopItem
{
    [System.NonSerialized]
    public ShopItemData data;

    // Defined at runtime, using ShopItemData
    [HideInInspector]
    public string itemName;

    public int currentLevel;
    public int maxLevel; // maxLevel = 0 means the item is displayed but out of stock.

    public bool hidden; // is the item visible in the shop?
}

/// <summary>
/// ShopSaveData contains all information that must be saved about the Shop.
/// - shopItems is the list of items in their current state
/// - currencySpentInShop is the amount of money that has been spent in the Shop (can be refunded when Shop is reset)
/// </summary>
[System.Serializable]
public class ShopSaveData : SaveData
{
    public List<ShopItem> shopItems;
    public long currencySpentInShop;

    public ShopSaveData()
    {
        Reset();
    }

    public override void Reset()
    {
        base.Reset();
        shopItems = new List<ShopItem>();
        currencySpentInShop = 0;
    }
}

/// <summary>
/// ShopManager is a singleton class that deals with the shop elements.
/// It stores the current stats increases from shop items. This stats increases are computed at runtime, depending on the items.
/// It stores all available shop items and their current level.
/// It display a screen with all shop information and possibility to buy items in the shop.
/// </summary>
public class ShopManager : MonoBehaviour
{
    // Singleton
    public static ShopManager instance;

    [Header("Item data")]
    [Tooltip("Scriptable objects containing data for each item")]
    public List<ShopItemData> availableItemDataList;

    [Header("UI References")]
    public RectTransform shopPanelContainer;
    public Transform shopPanel;
    public Text availableCurrencyText;
    public Button refundButton;

    [Header("UI Prefabs")]
    public GameObject availableShopItemPanelPrefab;
    public GameObject soldOutShopItemPanelPrefab;

    [Header("Settings")]
    public bool displaySoldOutItems = true;

    [Header("Runtime")]
    public ShopSaveData shopData; // Will be loaded and saved when needed
    public List<StatValue> statsBonuses; // This is computed from the items bought

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(this);
        }
        else
        {
            Destroy(this.gameObject);
        }
    }

    /// <summary>
    /// A handy method to get the starting bonus from the Shop for a specific STAT
    /// </summary>
    /// <param name="stat"></param>
    /// <returns></returns>
    public float GetStatBonus(STAT stat)
    {
        double bonus = 0;
        StatValue statValue = statsBonuses.FirstOrDefault(x => x.stat.Equals(stat));
        if (statValue != null)
        {
            bonus = statValue.value;
        }
        return (float)bonus;
    }

    /// <summary>
    /// Update status of every item in the shop from a list of ShopItem
    /// </summary>
    /// <param name="newItemsList"></param>
    public void ReplaceAvailableItemsList(List<ShopItem> newItemsList)
    {
        // Go through every item currently in the list
        foreach(ShopItem item in shopData.shopItems)
        {
            // Get the item with the same name in the new item list
            ShopItem newItem = newItemsList.FirstOrDefault(x => x.itemName == item.itemName);
            if (newItem != null)
            {
                // Update all item information
                item.currentLevel = newItem.currentLevel;
                item.maxLevel = newItem.maxLevel;
                item.hidden = newItem.hidden;
            }
        }
        // Compute all starting stats bonuses from the item values
        ComputeStatsBonuses();
        // Signal the SaveDataManager that information from the shop have been updated and should be saved when possible
        SaveDataManager.instance.isSaveDataDirty = true;
    }

    /// <summary>
    /// Attempt to buy an item.
    /// Spend the currency if available and upgrade the item to its next level.
    /// </summary>
    /// <param name="item"></param>
    public void BuyItem(ShopItem item)
    {
        // Need to check if item current level is below its max level
        if (item.currentLevel < item.maxLevel)
        {
            // Get the cost of that upgrade
            int itemCost = item.data.costForEachLevel[item.currentLevel];
            if (GameManager.instance.gameData.availableCurrency >= itemCost)
            {
                // There's enough available currency, so buy the item!
                // Increase currency spend in shop
                shopData.currencySpentInShop += itemCost;
                // Remove that amount from available currency
                GameManager.instance.ChangeAvailableCurrency(-itemCost); 
                // Upgrade item
                item.currentLevel++; 
                // Compute new starting stats bonuses
                ComputeStatsBonuses(); 
                // Update the shop display
                DisplayShop(); 
                // Signal the SaveDataManager that information from the shop have been updated and should be saved when possible
                SaveDataManager.instance.isSaveDataDirty = true;
            }
        }
    }

    /// <summary>
    /// Update the list of Stat bonuses using the current state of shop items.
    /// </summary>
    public void ComputeStatsBonuses()
    {
        // Remove all previous bonuses
        statsBonuses.Clear();
        foreach (ShopItem item in shopData.shopItems)
        {
            // For each item...
            for (int level = 0; level < item.currentLevel; level++)
            {
                // Apply its bonus once for each level
                foreach (StatValue statIncrease in item.data.statIncreaseList)
                {
                    if (statsBonuses.Contains(statIncrease))
                    {
                        // There was a bonus for this stat already, so we increase this bonus (don't duplicate that stat)
                        statsBonuses.First(x => x.stat == statIncrease.stat).value += statIncrease.value;
                    }
                    else
                    {
                        // This is the first bonus for this stat, so add the stat bonus as a new element in the list
                        statsBonuses.Add(new StatValue(statIncrease));
                    }
                }                
            }
        }
    }

    /// <summary>
    /// Remove all bought upgrades. Hard reset means setting everything back to start game values. Soft reset means setting current levels to zero.
    /// Will return the amount of currency spent in the shop.
    /// </summary>
    /// <param name="hardReset"></param>
    public long ResetShop(bool hardReset = false)
    {
        long returnedCurrency = shopData.currencySpentInShop;

        // In any case, the stats upgrades are reset
        statsBonuses.Clear();

        if (hardReset)
        {
            // A hard reset will also remove unlocked items and reset everything to the start game values
            shopData.shopItems.Clear();
            foreach (ShopItemData itemData in availableItemDataList)
            {
                shopData.shopItems.Add(new ShopItem() { data = itemData, currentLevel = 0, maxLevel = itemData.maxLevelAtStart, itemName = itemData.itemName });
            }
        }
        else
        {
            // A soft reset will keep all the items max level but set their current level to zero
            foreach (ShopItem item in shopData.shopItems)
            {
                item.currentLevel = 0;
            }
        }

        shopData.currencySpentInShop = 0;
        SaveDataManager.instance.isSaveDataDirty = true;
        return returnedCurrency;
    }

    /// <summary>
    /// Update the UI of the Shop.
    /// Will update both the available currency and redraw the buttons for each item using updated values.
    /// </summary>
    public void DisplayShop()
    {
        // Update Refund Button availability
        refundButton.interactable = (shopData.currencySpentInShop > 0);
        
        // Update available currency
        availableCurrencyText.text = Tools.FormatCurrency(GameManager.instance.gameData.availableCurrency, UIManager.instance.currencySymbol);
        
        // Remove previous buttons
        foreach (Transform child in shopPanel)
        {
            Destroy(child.gameObject);
        }
        // Create new buttons
        int buttonCount = 0;
        foreach (ShopItem item in shopData.shopItems)
        {
            bool itemIsLocked = (item.maxLevel == -1);
            bool itemIsOutOfStock = (item.maxLevel == 0);
            bool itemIsAvailable = (item.maxLevel > 0 && item.currentLevel < item.maxLevel);
            bool itemIsMaxedOut = (item.maxLevel > 0 && item.currentLevel == item.maxLevel);
            if ( itemIsAvailable )
            {
                bool canBuy = false;
                if (item.currentLevel < item.data.costForEachLevel.Count)
                {
                    int itemCost = item.data.costForEachLevel[item.currentLevel];
                    canBuy = GameManager.instance.gameData.availableCurrency >= itemCost;
                }
                
                GameObject shopItemButtonGo = Instantiate(availableShopItemPanelPrefab, shopPanel);
                ShopItemButton shopItemButton = shopItemButtonGo.GetComponent<ShopItemButton>();
                shopItemButton.buyButton.onClick.AddListener(delegate { BuyItem(item); });
                shopItemButton.Initialize(item, itemIsAvailable && canBuy);
                buttonCount++;
            }
            else if (displaySoldOutItems && (itemIsOutOfStock || itemIsMaxedOut))
            {
                GameObject shopItemButtonGo = Instantiate(soldOutShopItemPanelPrefab, shopPanel);
                ShopItemButton shopItemButton = shopItemButtonGo.GetComponent<ShopItemButton>();
                shopItemButton.Initialize(item, false);
                buttonCount++;
            }
        }
        
        // Set size of container panel
        float buttonHeight = shopPanel.GetComponent<GridLayoutGroup>().cellSize.y + shopPanel.GetComponent<GridLayoutGroup>().spacing.y;
        float padding = shopPanel.GetComponent<GridLayoutGroup>().padding.top + shopPanel.GetComponent<GridLayoutGroup>().padding.bottom;
        shopPanelContainer.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, ( (buttonCount+1) / 2) * buttonHeight + padding);
    }

    /// <summary>
    /// Reset all item levels and refund spent currency.
    /// Will keep max levels and unlocks untouched.
    /// </summary>
    public void RefundAll()
    {
        GameManager.instance.ChangeAvailableCurrency(shopData.currencySpentInShop);
        shopData.currencySpentInShop = 0;

        foreach (ShopItem item in shopData.shopItems)
        {
            item.currentLevel = 0;
        }

        // Compute new starting stats bonuses
        ComputeStatsBonuses();
        // Update the shop display
        DisplayShop();
        // Signal the SaveDataManager that information from the shop have been updated and should be saved when possible
        SaveDataManager.instance.isSaveDataDirty = true;
    }

    /// <summary>
    /// Update the shop data using a ShopSaveData object, that was probably loaded from a file by the SaveDataManager.
    /// </summary>
    /// <param name="saveData"></param>
    public void SetShopData(ShopSaveData saveData)
    {
        shopData.currencySpentInShop = saveData.currencySpentInShop;
        foreach (ShopItem item in shopData.shopItems)
        {
            ShopItem itemFromSave = saveData.shopItems.First(x => x.itemName.Equals(item.itemName));
            if (itemFromSave != null)
            {
                item.currentLevel = itemFromSave.currentLevel;
                item.maxLevel = itemFromSave.maxLevel;
                item.hidden = itemFromSave.hidden;
            }
        }
        // Compute new starting stats bonuses
        ComputeStatsBonuses();
        // Update the shop display
        DisplayShop();
    }
}
