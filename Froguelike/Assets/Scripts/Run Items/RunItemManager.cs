using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


[System.Serializable]
public class RunItemSaveInfo
{
    // Defined at runtime, using RunItemData
    public string itemName;

    public bool unlocked; // this is basically the only information we want to save in the save file

    public override bool Equals(object obj)
    {
        bool equ = false;
        if (obj is RunItemInfo)
        {
            equ = (obj as RunItemInfo).itemName.Equals(this.itemName);
        }
        return equ;
    }

    public override int GetHashCode()
    {
        return itemName.GetHashCode();
    }
}

/// <summary>
/// CharactersSaveData contains all information that must be saved about the characters.
/// - shopItems is the list of items in their current state
/// - currencySpentInShop is the amount of money that has been spent in the Shop (can be refunded when Shop is reset)
/// </summary>
[System.Serializable]
public class RunItemsSaveData : SaveData
{
    public List<RunItemSaveInfo> runItemsList;

    public RunItemsSaveData()
    {
        Reset();
    }

    public override void Reset()
    {
        base.Reset();
        runItemsList = new List<RunItemSaveInfo>();
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
    public List<RunWeaponItemData> allRunWeaponsItemsScriptableObjects;
    public List<RunStatItemData> allRunStatsItemsScriptableObjects;
    public List<RunConsumableItemData> allConsumableRunItemsScriptableObjects;
    public RunConsumableItemData nothingRunItemScriptableObject;

    [Header("Runtime")]
    public RunItemsSaveData runItemsData; // Will be loaded and saved when needed

    private Dictionary<string, RunItemData> runItemDataDictionary;

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
    /// Return the amount of items that would be the same in the selection if you ask for a reroll now.
    /// </summary>
    /// <returns></returns>
    public int ItemRerollSimilarItemsCount(List<RunItemData> previousSelection)
    {
        List<RunItemData> potentialSelectionOfItems = GetListOfRunItems(3, previousSelection);
        int count = 0;
        foreach (RunItemData item in previousSelection)
        {
            if (potentialSelectionOfItems.Contains(item))
            {
                count++;
            }
        }
        return count;
    }

    /// <summary>
    /// Create a pool of items (stat items and weapons) to choose from.
    /// An item can show up under certain conditions:
    /// - if it is already owned and not maxed out
    /// - if not, if it is unlocked and if there's room for it
    /// An item doesn't show up if it was banished during the run
    /// An item doesn't show up if it was part of the previous selection and you asked for a reroll
    /// </summary>
    /// <param name="minimumItemCount">The minimum number of items we need in that pool</param>
    /// <returns></returns>
    private List<RunItemData> GetListOfRunItems(int minimumItemCount, List<RunItemData> previousSelection)
    {
        // Resulting pool
        List<RunItemData> runItemsPool = new List<RunItemData>();

        // Owned items
        List<RunWeaponInfo> ownedWeapons = RunManager.instance.GetOwnedWeapons();
        List<RunStatItemInfo> ownedStatItems = RunManager.instance.GetOwnedStatItems();

        // Get a reference to PlayerController
        FrogCharacterController player = GameManager.instance.player;

        // For each item in saved data...
        foreach (RunItemSaveInfo itemSaveInfo in runItemsData.runItemsList)
        {
            // Get the corresponding data
            RunItemData itemData = runItemDataDictionary[itemSaveInfo.itemName];

            if (RunManager.instance.IsRunItemBanished(itemData))
            {
                continue;
            }

            if (previousSelection.Contains(itemData))
            {
                continue;
            }

            // Unlocked
            bool unlocked = itemSaveInfo.unlocked;
            if (GameManager.instance.thingsWithMissingSpritesAreHidden && itemData.icon == null)
            {
                unlocked = false;
            }

            if (itemData is RunStatItemData)
            {
                // stat item
                RunStatItemData statItemData = (itemData as RunStatItemData);
                int itemLevel = RunManager.instance.GetLevelForItem(statItemData);
                int maxItemLevel = statItemData.GetMaxLevelCount();                

                if ((itemLevel > 0 && itemLevel < maxItemLevel) // item is owned and not maxed out
                   || (itemLevel == 0 && ownedStatItems.Count < player.statItemSlotsCount && unlocked) // item is not owned, item is unlocked and there's space for it
                   )
                {
                    runItemsPool.Add(statItemData);
                }
            }
            else if (itemData is RunWeaponItemData)
            {
                // weapon
                RunWeaponItemData weaponItemData = (itemData as RunWeaponItemData);
                int itemLevel = RunManager.instance.GetLevelForItem(weaponItemData);
                int maxItemLevel = weaponItemData.GetMaxLevelCount() + 1;

                if ( (itemLevel > 0 && itemLevel < maxItemLevel) // weapon is owned and not maxed out
                   ||(itemLevel == 0 && ownedWeapons.Count < player.weaponSlotsCount && unlocked) // weapon is not owned, weapon is unlocked and there's space for it
                   )
                {
                    runItemsPool.Add(weaponItemData);
                }
            }
        }
        
        // Fill the rest with consumable items
        foreach (RunConsumableItemData consumableItem in allConsumableRunItemsScriptableObjects)
        {
            if (runItemsPool.Count >= minimumItemCount)
            {
                break; // Stop when the pool is big enough
            }

            if (RunManager.instance.IsRunItemBanished(consumableItem))
            {
                continue; // Ignore banished items
            }

            if (!runItemsPool.Contains(consumableItem))
            {
                runItemsPool.Add(consumableItem); // Add consumable item to the pool
            }
        }

        // If the list is still empty, add one last item as a failsafe
        if (runItemsPool.Count == 0)
        {
            runItemsPool.Add(nothingRunItemScriptableObject);
        }

        return runItemsPool;
    }

    /// <summary>
    /// Return a list of N RunItemData that are different from the previous selection (as much as possible)
    /// </summary>
    /// <param name="numberOfItems"></param>
    /// <param name="previousSelection"></param>
    /// <returns></returns>
    public List<RunItemData> PickARandomSelectionOfRunItems(int numberOfItems, List<RunItemData> previousSelection)
    {
        // Resulting list
        List<RunItemData> runItemsList = new List<RunItemData>();
        
        // Pool of items to select from
        List<RunItemData> possibleItems = GetListOfRunItems(numberOfItems, previousSelection);
        
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

    public void ResetRunItems()
    {
        runItemsData.runItemsList.Clear();
        runItemDataDictionary = new Dictionary<string, RunItemData>();
        // Add all weapons
        foreach (RunWeaponItemData runItemData in allRunWeaponsItemsScriptableObjects)
        {
            RunItemSaveInfo newItemSaveInfo = new RunItemSaveInfo()
            {
                itemName = runItemData.itemName,
                unlocked = runItemData.unlockedFromStart
            };
            runItemDataDictionary.Add(runItemData.itemName, runItemData);
            runItemsData.runItemsList.Add(newItemSaveInfo);
        }

        // Add all stat items
        foreach (RunStatItemData runItemData in allRunStatsItemsScriptableObjects)
        {
            RunItemSaveInfo newItemSaveInfo = new RunItemSaveInfo()
            {
                itemName = runItemData.itemName,
                unlocked = runItemData.unlockedFromStart
            };
            runItemDataDictionary.Add(runItemData.itemName, runItemData);
            runItemsData.runItemsList.Add(newItemSaveInfo);
        }
    }

    /// <summary>
    /// Update the Run Items data using a RunItemSaveData object, that was probably loaded from a file by the SaveDataManager.
    /// </summary>
    /// <param name="saveData"></param>
    public void SetRunItemsData(RunItemsSaveData saveData)
    {
        foreach (RunItemSaveInfo runItem in runItemsData.runItemsList)
        {
            RunItemSaveInfo runItemFromSave = saveData.runItemsList.FirstOrDefault(x => x.itemName.Equals(runItem.itemName));
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
        RunItemSaveInfo unlockedRunItem = runItemsData.runItemsList.FirstOrDefault(x => x.itemName.Equals(itemName));
        if (unlockedRunItem != null && !unlockedRunItem.unlocked)
        {
            unlockedRunItem.unlocked = true;
            itemNewlyUnlocked = true;
        }
        return itemNewlyUnlocked;
    }

    public void ApplyDemoLimitationToRunItems()
    {
        // For each item in saved data...
        foreach (RunItemSaveInfo itemSaveInfo in runItemsData.runItemsList)
        {
            // Get the corresponding data
            RunItemData itemData = runItemDataDictionary[itemSaveInfo.itemName];

            // If item was unlocked but is not part of Demo, then lock it
            if (itemSaveInfo.unlocked && !itemData.partOfDemo)
            {
                itemSaveInfo.unlocked = false;
            }
        }
    }
}
