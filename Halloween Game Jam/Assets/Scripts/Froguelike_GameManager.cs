using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Froguelike_ItemInfo
{
    public Froguelike_ItemScriptableObject item;
    public List<GameObject> weaponsList;
    public int level;
}

public class Froguelike_GameManager : MonoBehaviour
{
    public static Froguelike_GameManager instance;

    public Froguelike_CharacterController player;

    public Transform fliesParent;

    public Froguelike_Wave waveInfo;

    public Froguelike_ItemScriptableObject startingWeapon;

    public float xp;
    public float nextLevelXp = 5;
    public float xpNeededForNextLevelFactor = 1.3f;
    public int level;

    public int respawns;
    public int currentRespawns;

    public List<Froguelike_ItemScriptableObject> availableItems;
    public List<Froguelike_ItemScriptableObject> defaultItems;

    public List<Froguelike_ItemInfo> ownedItems;

    public bool hasGameStarted;
    public bool isGameRunning;

    private void Awake()
    {
        instance = this;
        hasGameStarted = false;
        isGameRunning = false;
    }

    // Start is called before the first frame update
    void Start()
    {
        level = 1;
        xp = 0;
        Froguelike_UIManager.instance.UpdateXPSlider(0, nextLevelXp);
    }

    public void EatFly(int experiencePoints)
    {
        xp += experiencePoints;

        while (xp >= nextLevelXp)
        {
            LevelUP();
            xp -= nextLevelXp;
            nextLevelXp *= xpNeededForNextLevelFactor;
        }

        Froguelike_UIManager.instance.UpdateXPSlider(xp, nextLevelXp);
    }

    #region Level Up

    public List<Froguelike_ItemScriptableObject> levelUpPossibleItems;

    private void SpawnWeapon(Froguelike_ItemInfo weaponItem)
    {
        if (weaponItem.item.isWeapon)
        {
            GameObject weaponPrefab = weaponItem.item.weaponPrefab;
            GameObject weaponGo = Instantiate(weaponPrefab, player.weaponStartPoint.position, Quaternion.identity, player.weaponsParent);
            if (weaponItem.weaponsList.Count > 0)
            {
                weaponGo.GetComponent<Froguelike_TongueBehaviour>().CopyWeaponStats(weaponItem.weaponsList[0].GetComponent<Froguelike_TongueBehaviour>());
            }
            weaponItem.weaponsList.Add(weaponGo);
        }        
    }

    private void PickItem(Froguelike_ItemScriptableObject pickedItem)
    {
        bool itemIsNew = true;
        int level = 0;
        Froguelike_ItemInfo pickedItemInfo = null;
        foreach (Froguelike_ItemInfo itemInfo in ownedItems)
        {
            if (itemInfo.item.Equals(pickedItem))
            {
                // We already own such an item
                itemIsNew = false;
                pickedItemInfo = itemInfo;
                itemInfo.level++;
                level = itemInfo.level;

                if (pickedItem.isWeapon)
                {
                    // It is a weapon, so we need to upgrade all similar weapons
                    foreach (GameObject weaponGo in itemInfo.weaponsList)
                    {
                        weaponGo.GetComponent<Froguelike_TongueBehaviour>().LevelUp(itemInfo.item.levels[level]);
                    }
                }
                break;
            }
        }

        int spawnWeapons = pickedItem.levels[level].weaponExtraWeapon;
        Debug.Log("Pick Item - " + pickedItem.itemName + " ; level = " + level + " ; spawnWeapons = " + spawnWeapons);
        if (itemIsNew && pickedItemInfo == null)
        {
            // Create item info and add it to owned items
            pickedItemInfo = new Froguelike_ItemInfo();
            pickedItemInfo.level = 1;
            pickedItemInfo.item = pickedItem;
            pickedItemInfo.weaponsList = new List<GameObject>();
            ownedItems.Add(pickedItemInfo);
            spawnWeapons++;
        }

        if (pickedItem.isWeapon)
        {
            for (int w = 0; w < spawnWeapons; w++)
            {
                SpawnWeapon(pickedItemInfo);
            }
        }
    }

    public void ChooseLevelUpChoice(int index)
    {
        Froguelike_ItemScriptableObject pickedItem = levelUpPossibleItems[index];
        PickItem(pickedItem);
        Froguelike_UIManager.instance.HideLevelUpItemSelection();
        Time.timeScale = 1;
    }

    private int GetLevelForItem(Froguelike_ItemScriptableObject item)
    {
        int level = 0;
        foreach (Froguelike_ItemInfo itemInfo in ownedItems)
        {
            if (itemInfo.item.Equals(item))
            {
                level = itemInfo.level;
            }
        }
        return level;
    }

    public void LevelUP()
    {
        level++;
        Time.timeScale = 0;

        // Pick possible items from a pool
        List<Froguelike_ItemScriptableObject> possibleItems = new List<Froguelike_ItemScriptableObject>();
        foreach (Froguelike_ItemScriptableObject possibleItem in availableItems)
        {
            if (GetLevelForItem(possibleItem) < (possibleItem.levels.Count-1))
            {
                possibleItems.Add(possibleItem);
            }
        }
        if (possibleItems.Count == 0)
        {
            foreach (Froguelike_ItemScriptableObject possibleItem in defaultItems)
            {
                possibleItems.Add(possibleItem);
            }
        }
        
        levelUpPossibleItems.Clear();
        for (int i = 0; i < 3; i++)
        {
            if (possibleItems.Count == 0)
            {
                break;
            }

            int randomIndex = Random.Range(0, possibleItems.Count);
            levelUpPossibleItems.Add(possibleItems[randomIndex]);
            possibleItems.RemoveAt(randomIndex);
        }

        // Find levels for each of these items
        List<int> itemLevels = new List<int>();
        foreach (Froguelike_ItemScriptableObject item in levelUpPossibleItems)
        {
            itemLevels.Add(GetLevelForItem(item)+1);
        }

        // Show Update level UI
        Froguelike_UIManager.instance.ShowLevelUpItemSelection(levelUpPossibleItems, itemLevels);
        Froguelike_UIManager.instance.UpdateLevel(level);
    }

    #endregion

    public void OpenCharacterSelection()
    {
        Froguelike_UIManager.instance.ShowCharacterSelection();
    }

    public void SelectCharacter(int index)
    {
        Froguelike_UIManager.instance.ShowChapterSelection(1, "THE SWAMP", "BEES");
    }

    public void SelectChapter(int index)
    {
        StartCoroutine(StartGame(1, "THE SWAMP"));
    }

    public IEnumerator StartGame(int chapterCount, string chapterTitle)
    {
        Froguelike_UIManager.instance.ShowChapterStart(chapterCount, chapterTitle);
        yield return new WaitForSecondsRealtime(3.0f);
        Froguelike_UIManager.instance.ShowGameUI();
        hasGameStarted = true;
        isGameRunning = true;
        PickItem(startingWeapon);
        Froguelike_FliesManager.instance.SetWave(waveInfo);
        Time.timeScale = 1;
        player.Respawn();
        currentRespawns = respawns;
    }

    public void TriggerGameOver()
    {
        Time.timeScale = 0;
        isGameRunning = false;
        Froguelike_UIManager.instance.ShowGameOver((currentRespawns > 0));
    }

    public void Respawn()
    {
        Time.timeScale = 1;
        player.Respawn();
        isGameRunning = true;
        Froguelike_UIManager.instance.ShowGameUI();
        currentRespawns--;
    }

    public void ShowScores()
    {
        Froguelike_UIManager.instance.ShowScoreScreen();
    }

    public void BackToTitleScreen()
    {
        Froguelike_FliesManager.instance.ClearAllEnemies();
        Froguelike_UIManager.instance.ShowTitleScreen();
        hasGameStarted = false;
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
