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
    public int maxLevel; // maxLevel = 0 means the item is displayed but out of stock. maxLevel = -1 means the item is locked and won't be displayed in the shop.
}

/// <summary>
/// ShopManager is a singleton class that deals with the shop elements.
/// It stores the current stats increases from shop items.
/// It stores all available shop items and their current level.
/// </summary>
public class ShopManager : MonoBehaviour
{
    public static ShopManager instance;

    [Header("Item data")]
    [Tooltip("Scriptable objects containing data for each item")]
    public List<ShopItemData> availableItemDataList;

    [Header("UI Reference")]
    public RectTransform shopPanelContainer;
    public Transform shopPanel;
    public Text availableCurrencyText;
    public Button refundButton;

    [Header("UI Prefab")]
    public GameObject availableShopItemPanelPrefab;
    public GameObject soldOutShopItemPanelPrefab;

    [Header("Settings")]
    public bool displaySoldOutItems = true;

    [Header("Runtime")]
    public List<ShopItem> availableItemsList;
    public List<StatValue> statsBonuses; // This is computed from the items bought
    public long currencySpentInTheShop;

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
        foreach(ShopItem item in availableItemsList)
        {
            ShopItem newItem = newItemsList.FirstOrDefault(x => x.itemName == item.itemName);
            if (newItem != null)
            {
                item.currentLevel = newItem.currentLevel;
                item.maxLevel = newItem.maxLevel;
            }
        }
        ComputeStatsBonuses();
    }

    public void BuyItem(ShopItem item)
    {
        if (item.currentLevel < item.maxLevel)
        {
            int itemCost = item.data.costForEachLevel[item.currentLevel];
            if (GameManager.instance.availableCurrency >= itemCost)
            {
                // Can buy!
                currencySpentInTheShop += itemCost;
                GameManager.instance.ChangeAvailableCurrency(-itemCost, false);
                item.currentLevel++;
                ComputeStatsBonuses();
                DisplayShop();
                GameManager.instance.UpdateShopInfoInCurrentSave();
            }
        }
    }

    public void ComputeStatsBonuses()
    {
        statsBonuses.Clear();
        foreach (ShopItem item in availableItemsList)
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
        long returnedCurrency = currencySpentInTheShop;

        // In any case, the stats upgrades are reset
        statsBonuses.Clear();

        if (hardReset)
        {
            // A hard reset will also remove unlocked items and reset everything to the start game values
            availableItemsList.Clear();
            foreach (ShopItemData itemData in availableItemDataList)
            {
                availableItemsList.Add(new ShopItem() { data = itemData, currentLevel = 0, maxLevel = itemData.maxLevelAtStart, itemName = itemData.itemName });
            }
        }
        else
        {
            // A soft reset will keep all the items max level but set their current level to zero
            foreach (ShopItem item in availableItemsList)
            {
                item.currentLevel = 0;
            }
        }

        currencySpentInTheShop = 0;
        return returnedCurrency;
    }

    public void DisplayShop()
    {
        // Update Refund Button availability
        refundButton.interactable = (currencySpentInTheShop > 0);

        // Update available currency
        availableCurrencyText.text = Tools.FormatCurrency(GameManager.instance.availableCurrency, UIManager.instance.currencySymbol);

        // Remove previous buttons
        foreach (Transform child in shopPanel)
        {
            Destroy(child.gameObject);
        }
        // Create new buttons
        int buttonCount = 0;
        foreach (ShopItem item in availableItemsList)
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
                    canBuy = GameManager.instance.availableCurrency >= itemCost;
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
        GameManager.instance.ChangeAvailableCurrency(currencySpentInTheShop, false);
        currencySpentInTheShop = 0;

        foreach (ShopItem item in availableItemsList)
        {
            item.currentLevel = 0;
        }

        ComputeStatsBonuses();
        DisplayShop();
        GameManager.instance.UpdateShopInfoInCurrentSave();
    }
}
