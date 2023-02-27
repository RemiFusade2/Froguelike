using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


[System.Serializable]
public class RunItemInfo
{
    // Defined at runtime, using RunItemData
    public string itemName;

    [System.NonSerialized]
    public int level; // current level, used only during a Run

    public RunItemData GetRunItemData()
    {
        if (this is RunStatItemInfo)
        {
            return (this as RunStatItemInfo).itemData;
        }
        if (this is RunWeaponInfo)
        {
            return (this as RunWeaponInfo).weaponItemData;
        }
        return null;
    }
}

[System.Serializable]
public class RunStatItemInfo : RunItemInfo
{
    public RunStatItemData itemData;
}

[System.Serializable]
public class RunWeaponInfo : RunItemInfo
{
    public RunWeaponItemData weaponItemData;

    public List<GameObject> activeWeaponsList; // current active weapons

    public int killCount;
}

/// <summary>
/// CharactersSaveData contains all information that must be saved about the characters.
/// - shopItems is the list of items in their current state
/// - currencySpentInShop is the amount of money that has been spent in the Shop (can be refunded when Shop is reset)
/// </summary>
[System.Serializable]
public class RunItemSaveData : SaveData
{
    public List<RunItemInfo> runItemsList;

    public RunItemSaveData()
    {
        Reset();
    }

    public override void Reset()
    {
        base.Reset();
        runItemsList = new List<RunItemInfo>();
    }
}

/// <summary>
/// RunItemManager deals with the pool of Run Items (weapons + stat items) to pick from when leveling up.
/// </summary>
public class RunItemManager : MonoBehaviour
{
    // Singleton
    public static RunItemManager instance;

    [Header("Items data")]
    public List<RunWeaponItemData> allRunWeaponsItems;
    public List<RunStatItemData> allRunStatsItems;
    public List<RunConsumableItemData> allConsumableRunItems;

    [Header("Settings")]
    public int maxWeaponCount = 4;
    public int maxStatItemCount = 5;

    /*
     * Would be useful later when dealing with unlockable Run Items
    [Header("Runtime")]
    public RunItemSaveData runItemsData; // Will be loaded and saved when needed*/

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

    private List<RunItemData> GetListOfRunItems(int minimumItemCount)
    {
        // Resulting list
        List<RunItemData> allRunItems = new List<RunItemData>();

        // Owned items count
        int weaponItemsCount = RunManager.instance.GetOwnedWeapons().Count;
        int statItemsCount = RunManager.instance.GetOwnedStatItems().Count;

        foreach (RunWeaponItemData weaponItem in allRunWeaponsItems)
        {
            int itemLevel = RunManager.instance.GetLevelForItem(weaponItem);
            int maxItemLevel = weaponItem.GetMaxLevelCount()+1;
            if ( (weaponItemsCount < maxWeaponCount && itemLevel == 0) || (itemLevel > 0 && itemLevel < maxItemLevel) )
            {
                allRunItems.Add(weaponItem);
            }
        }
        foreach (RunStatItemData statItem in allRunStatsItems)
        {
            int itemLevel = RunManager.instance.GetLevelForItem(statItem);
            int maxItemLevel = statItem.GetMaxLevelCount();
            if ((statItemsCount < maxStatItemCount && itemLevel == 0) || (itemLevel > 0 && itemLevel < maxItemLevel))
            {
                allRunItems.Add(statItem);
            }
        }

        bool atLeastOneConsumableItemAvailable = allConsumableRunItems.Count > 0;
        while (allRunItems.Count < minimumItemCount && atLeastOneConsumableItemAvailable)
        {
            atLeastOneConsumableItemAvailable = false;
            foreach (RunConsumableItemData consumableItem in allConsumableRunItems)
            {
                if (!allRunItems.Contains(consumableItem))
                {
                    allRunItems.Add(consumableItem);
                    atLeastOneConsumableItemAvailable = true;
                    break;
                }
            }
        }
        return allRunItems;
    }

    public List<RunItemData> PickARandomSelectionOfRunItems(int numberOfItems)
    {
        // Resulting list
        List<RunItemData> runItemsList = new List<RunItemData>();
        
        // Pool of items to select from
        List<RunItemData> possibleItems = GetListOfRunItems(numberOfItems);
        
        if (possibleItems.Count <= numberOfItems)
        {
            // Exactly numberOfItems available items or less, we want to keep the order
            foreach (RunItemData item in possibleItems)
            {
                runItemsList.Add(item);
            }
        }
        else
        {
            // More than numberOfItems, we want to choose numberOfItems at random from the list
            for (int i = 0; i < numberOfItems; i++)
            {
                if (possibleItems.Count == 0)
                {
                    break;
                }

                int randomIndex = Random.Range(0, possibleItems.Count);
                runItemsList.Add(possibleItems[randomIndex]);
                possibleItems.RemoveAt(randomIndex);
            }
        }

        return runItemsList;
    }






    /*
    /// <summary>
    /// Update the Run Items data using a RunItemSaveData object, that was probably loaded from a file by the SaveDataManager.
    /// </summary>
    /// <param name="saveData"></param>
    public void SetRunItemsData(RunItemSaveData saveData)
    {
        foreach (RunItemInfo runItem in runItemsData.runItemsList)
        {
            RunItemInfo runItemFromSave = saveData.runItemsList.FirstOrDefault(x => x.itemName.Equals(runItem.itemName));
            if (runItemFromSave != null)
            {
                runItem.unlocked = runItemFromSave.unlocked;
            }
        }
    }

    /// <summary>
    /// Try to unlock the run item having this name. Do not do anything if this item does not exist or is already unlocked.
    /// Return true if a new run item has been unlocked.
    /// </summary>
    /// <param name="itemName"></param>
    /// <returns></returns>
    public bool UnlockRunItem(string itemName)
    {
        bool itemNewlyUnlocked = false;
        RunItemInfo unlockedRunItem = runItemsData.runItemsList.FirstOrDefault(x => x.itemName.Equals(itemName));
        if (unlockedRunItem != null && !unlockedRunItem.unlocked)
        {
            unlockedRunItem.unlocked = true;
            itemNewlyUnlocked = true;
            SaveDataManager.instance.isSaveDataDirty = true;
        }
        return itemNewlyUnlocked;
    }*/
}
