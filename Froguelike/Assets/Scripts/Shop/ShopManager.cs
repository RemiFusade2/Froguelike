using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

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

    public override string ToString()
    {
        string result = "shopItems = " + shopItems.ToString() + " - ";
        result += "currencySpentInShop = " + currencySpentInShop.ToString();
        return result;
    }
}

/// <summary>
/// ShopManager is a singleton class that deals with the shop elements.
/// It stores the current stats increases from shop items.
/// It stores all available shop items and their current level.
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
        instance = this;
    }

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

    public void ReplaceAvailableItemsList(List<ShopItem> newItemsList)
    {
        foreach(ShopItem item in shopData.shopItems)
        {
            ShopItem newItem = newItemsList.FirstOrDefault(x => x.itemName == item.itemName);
            if (newItem != null)
            {
                item.currentLevel = newItem.currentLevel;
                item.maxLevel = newItem.maxLevel;
            }
        }
        SaveDataManager.instance.isSaveDataDirty = true;
        ComputeStatsBonuses();
    }

    public void BuyItem(ShopItem item)
    {
        if (item.currentLevel < item.maxLevel)
        {
            int itemCost = item.data.costForEachLevel[item.currentLevel];
            if (GameManager.instance.gameData.availableCurrency >= itemCost)
            {
                // Can buy!
                shopData.currencySpentInShop += itemCost;
                GameManager.instance.ChangeAvailableCurrency(-itemCost);
                item.currentLevel++;
                ComputeStatsBonuses();
                DisplayShop();
                SaveDataManager.instance.isSaveDataDirty = true;
            }
        }
    }

    public void ComputeStatsBonuses()
    {
        statsBonuses.Clear();
        foreach (ShopItem item in shopData.shopItems)
        {
            for (int level = 0; level < item.currentLevel; level++)
            {
                foreach (StatValue statIncrease in item.data.statIncreaseList)
                {
                    if (statsBonuses.Contains(statIncrease))
                    {
                        statsBonuses.First(x => x.stat == statIncrease.stat).value += statIncrease.value;
                    }
                    else
                    {
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

    public void RefundAll()
    {
        GameManager.instance.ChangeAvailableCurrency(shopData.currencySpentInShop);
        shopData.currencySpentInShop = 0;

        foreach (ShopItem item in shopData.shopItems)
        {
            item.currentLevel = 0;
        }

        ComputeStatsBonuses();
        DisplayShop();
        SaveDataManager.instance.isSaveDataDirty = true;
    }

    public void SetShopData(ShopSaveData saveData)
    {
        shopData.currencySpentInShop = saveData.currencySpentInShop;
        foreach (ShopItem item in shopData.shopItems)
        {
            ShopItem itemFromSave = saveData.shopItems.First(x => x.itemName.Equals(item.itemName));
            if (itemFromSave != null)
            {
                item.currentLevel = itemFromSave.currentLevel;
                item.hidden = itemFromSave.hidden;
                item.maxLevel = itemFromSave.maxLevel;
            }
        }
        ComputeStatsBonuses();
        DisplayShop();
    }
}
