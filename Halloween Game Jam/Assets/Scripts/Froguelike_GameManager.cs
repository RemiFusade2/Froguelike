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

[System.Serializable]
public class Froguelike_ChapterInfo
{
    public Froguelike_ChapterData chapterData;
    public int chapterCount;
    public int enemiesKilledCount;
}

[System.Serializable]
public class Froguelike_PlayableCharacterInfo
{
    public bool unlocked;

    public string characterName;
    public string characterDescription;
    public string unlockHint;

    public int characterAnimatorValue;
    public float startingLandSpeed;
    public float startingSwimSpeed;
    public float startingMaxHealth;
    public float startingHealthRecovery;

    public float startingArmor;
    public int startingRevivals;

    public Froguelike_ItemScriptableObject startingWeapon;
}
public class Froguelike_GameManager : MonoBehaviour
{
    public static Froguelike_GameManager instance;

    [Header("References")]
    public Froguelike_CharacterController player;
    public Froguelike_MapBehaviour map;
    public Transform fliesParent;

    [Header("Chapters data")]
    public List<Froguelike_ChapterData> allPlayableChaptersList;

    [Header("Characters data")]
    public List<Froguelike_PlayableCharacterInfo> playableCharactersList;

    [Header("Items data")]
    public List<Froguelike_ItemScriptableObject> availableItems;
    public List<Froguelike_ItemScriptableObject> defaultItems;

    [Header("Death Enemy data")]
    public GameObject deathEnemyPrefab;
    public Froguelike_EnemyData deathEnemyData;

    [Header("XP")]
    public float startLevelXp = 5;
    public float startXpNeededForNextLevelFactor = 1.5f;

    [Header("Runtime")]
    public bool hasGameStarted;
    public bool isGameRunning;
    [Space]
    public float chapterRemainingTime; // in seconds

    public List<Froguelike_ChapterInfo> chaptersPlayed;
    [Space]
    public float xp;
    public int level;
    [Space]
    public List<Froguelike_ItemInfo> ownedItems;
    [Space]
    public Froguelike_PlayableCharacterInfo currentPlayedCharacter;
    [Space]
    private List<Froguelike_ChapterData> currentPlayableChaptersList;
    public Froguelike_ChapterInfo currentChapter;

    private float nextLevelXp = 5;
    private float xpNeededForNextLevelFactor = 1.5f;

    private void Awake()
    {
        instance = this;
        hasGameStarted = false;
        isGameRunning = false;
    }

    // Start is called before the first frame update
    void Start()
    {
        InitializeStuff();
        InitializeNewGame();
        Froguelike_UIManager.instance.UpdateXPSlider(0, nextLevelXp);
        InvokeRepeating("UpdateMap", 0.2f, 0.5f);
    }

    private void Update()
    {
        if (isGameRunning)
        {
            chapterRemainingTime -= Time.deltaTime;
            Froguelike_UIManager.instance.SetTimer(chapterRemainingTime);
            if (chapterRemainingTime < 0)
            {
                EndChapter();
            }
        }
    }

    public void InitializeNewGame()
    {
        level = 1;
        xp = 0;

        nextLevelXp = startLevelXp;
        xpNeededForNextLevelFactor = startXpNeededForNextLevelFactor;

        Froguelike_UIManager.instance.UpdateLevel(level);
        Froguelike_UIManager.instance.UpdateXPSlider(xp, xpNeededForNextLevelFactor);

        Froguelike_FliesManager.instance.enemyDamageFactor = 1;
        Froguelike_FliesManager.instance.enemyHPFactor = 1;
        Froguelike_FliesManager.instance.enemySpeedFactor = 1;
        Froguelike_FliesManager.instance.enemyXPFactor = 1;
        Froguelike_FliesManager.instance.curse = 0;

        ClearAllItems();
        TeleportToStart();
    }

    public void EatFly(float experiencePoints)
    {
        xp += (experiencePoints * (1 + player.experienceBoost));

        if (xp >= nextLevelXp)
        {
            LevelUP();
            xp -= nextLevelXp;
            nextLevelXp *= xpNeededForNextLevelFactor;
        }

        currentChapter.enemiesKilledCount++;

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
                level = itemInfo.level - 1;

                if (pickedItem.isWeapon)
                {
                    // It is a weapon, so we need to upgrade all similar weapons
                    foreach (GameObject weaponGo in itemInfo.weaponsList)
                    {
                        if (level >= 0 && level < itemInfo.item.levels.Count)
                        {
                            weaponGo.GetComponent<Froguelike_TongueBehaviour>().LevelUp(itemInfo.item.levels[level]);
                        }
                    }
                }
                break;
            }
        }

        int spawnWeapons = 0;
        if (pickedItem.isWeapon && level >= 0 && level < pickedItem.levels.Count)
        {
            spawnWeapons = pickedItem.levels[level].weaponExtraWeapon;
        }
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
            // spawn as many weapons as needed
            for (int w = 0; w < spawnWeapons; w++)
            {
                SpawnWeapon(pickedItemInfo);
            }
        }
        else
        {
            // resolve the item picked (according to its current level)
            int levelIndex = pickedItemInfo.level - 1;
            levelIndex = Mathf.Clamp(levelIndex, 0, pickedItemInfo.item.levels.Count - 1);
            player.ResolvePickedItemLevel(pickedItemInfo.item.levels[levelIndex]);
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

        int weaponCount = 0;
        int itemNotWeaponCount = 0;

        foreach (Froguelike_ItemInfo itemInfo in ownedItems)
        {
            if (itemInfo.item.isWeapon)
            {
                weaponCount++;
            }
            else
            {
                itemNotWeaponCount++;
            }
        }

        foreach (Froguelike_ItemScriptableObject possibleItem in availableItems)
        {
            bool itemLevelIsNotMaxed = GetLevelForItem(possibleItem) < (possibleItem.levels.Count - 1);
            if (possibleItem.isWeapon)
            {
                if (weaponCount >= 3)
                {
                    // only add that item IF it is already part of our owned items
                    bool alreadyOwned = false;
                    foreach (Froguelike_ItemInfo itemInfo in ownedItems)
                    {
                        if (itemInfo.item.Equals(possibleItem))
                        {
                            alreadyOwned = true;
                            break;
                        }
                    }
                    if (alreadyOwned)
                    {
                        possibleItems.Add(possibleItem);
                    }
                }
                else
                {
                    possibleItems.Add(possibleItem);
                }
            }
            else
            {
                if (itemNotWeaponCount >= 3)
                {
                    // only add that item IF it is already part of our owned items
                    bool alreadyOwned = false;
                    foreach (Froguelike_ItemInfo itemInfo in ownedItems)
                    {
                        if (itemInfo.item.Equals(possibleItem))
                        {
                            alreadyOwned = true;
                            break;
                        }
                    }
                    if (alreadyOwned)
                    {
                        possibleItems.Add(possibleItem);
                    }
                }
                else
                {
                    possibleItems.Add(possibleItem);
                }
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
            itemLevels.Add(GetLevelForItem(item) + 1);
        }

        // Show Update level UI
        Froguelike_UIManager.instance.ShowLevelUpItemSelection(levelUpPossibleItems, itemLevels);
        Froguelike_UIManager.instance.UpdateLevel(level);
    }

    #endregion

    public void OpenCharacterSelection()
    {
        Froguelike_UIManager.instance.ShowCharacterSelection(playableCharactersList);
    }

    #region Level Up

    private List<Froguelike_ChapterData> selectionOfNextChaptersList;

    private void SelectNextPossibleChapters(int chapterCount)
    {
        selectionOfNextChaptersList = new List<Froguelike_ChapterData>();

        if (currentPlayableChaptersList.Count < chapterCount)
        {
            ReinitializeChaptersList();
        }

        for (int i = 0; i < chapterCount; i++)
        {
            Froguelike_ChapterData selectedChapter = currentPlayableChaptersList[Random.Range(0, currentPlayableChaptersList.Count)];
            currentPlayableChaptersList.Remove(selectedChapter);
            selectionOfNextChaptersList.Add(selectedChapter);
        }
    }

    public void SelectCharacter(int index)
    {
        currentPlayedCharacter = playableCharactersList[index];
        SelectNextPossibleChapters(3);

        chaptersPlayed = new List<Froguelike_ChapterInfo>();
        Froguelike_UIManager.instance.ShowChapterSelection(1, selectionOfNextChaptersList);

        player.InitializeCharacter(currentPlayedCharacter);
        PickItem(currentPlayedCharacter.startingWeapon);
    }

    public void SelectChapter(int index)
    {
        Froguelike_ChapterInfo chapterInfo = new Froguelike_ChapterInfo();
        chapterInfo.chapterData = selectionOfNextChaptersList[index];
        chapterInfo.chapterCount = (chaptersPlayed.Count + 1);
        chapterInfo.enemiesKilledCount = 0;

        currentChapter = chapterInfo;
        StartCoroutine(StartChapter(chapterInfo.chapterCount));
    }

    #endregion

    public void EndChapter()
    {
        if (currentChapter.chapterData.unlocksACharacter)
        {
            playableCharactersList[currentChapter.chapterData.unlockedCharacterIndex].unlocked = true;
        }

        chaptersPlayed.Add(currentChapter);
        if (chaptersPlayed.Count < 5)
        {
            Time.timeScale = 0;
            chapterRemainingTime = 120;
            SelectNextPossibleChapters(3);
            Froguelike_UIManager.instance.ShowChapterSelection(chaptersPlayed.Count + 1, selectionOfNextChaptersList);
        }
        else
        {
            chapterRemainingTime = 4 * 60 + 59;
            Froguelike_FliesManager.instance.ClearAllEnemies();
            Froguelike_FliesManager.instance.SpawnEnemy(deathEnemyPrefab, player.transform.position + 30 * Vector3.right, deathEnemyData);
        }
    }

    public IEnumerator StartChapter(int chapterCount)
    {
        Froguelike_UIManager.instance.ShowChapterStart(chapterCount, currentChapter.chapterData.chapterTitle);
        map.rockDensity = currentChapter.chapterData.amountOfRocks;
        map.waterDensity = currentChapter.chapterData.amountOfPonds;
        Froguelike_FliesManager.instance.ClearAllEnemies();

        if (chapterCount > 1)
        {
            Froguelike_FliesManager.instance.enemyDamageFactor *= 1.5f;
            Froguelike_FliesManager.instance.enemyHPFactor *= 2;
            Froguelike_FliesManager.instance.enemySpeedFactor *= 1.1f;
            Froguelike_FliesManager.instance.enemyXPFactor *= 1.5f;
        }

        map.ClearMap();
        TeleportToStart();
        UpdateMap();
        Froguelike_FliesManager.instance.SetWave(currentChapter.chapterData.waves[0]);
        player.ForceGhost(currentChapter.chapterData.isCharacterGhost);
        int hatStyle = 0;
        if (currentChapter.chapterData.hasHat)
        {
            hatStyle = currentChapter.chapterData.hatStyle;
        }

        player.SetPetActive(currentChapter.chapterData.hasPetFrog);

        player.SetHat(hatStyle);

        yield return new WaitForSecondsRealtime(3.0f);

        Froguelike_MusicManager.instance.PlayLevelMusic();
        Froguelike_UIManager.instance.ShowGameUI();
        hasGameStarted = true;
        isGameRunning = true;
        chapterRemainingTime = currentChapter.chapterData.chapterLengthInSeconds;
        Time.timeScale = 1;
    }

    public void TriggerGameOver()
    {
        Time.timeScale = 0;
        isGameRunning = false;
        Froguelike_UIManager.instance.ShowGameOver((player.revivals > 0));
    }

    public void Respawn()
    {
        Time.timeScale = 1;
        player.Respawn();
        isGameRunning = true;
        Froguelike_UIManager.instance.ShowGameUI();
        player.revivals--;
    }

    public void ShowScores()
    {
        chaptersPlayed.Add(currentChapter);
        Froguelike_UIManager.instance.ShowScoreScreen(chaptersPlayed, ownedItems);
    }

    public void BackToTitleScreen()
    {
        Froguelike_FliesManager.instance.ClearAllEnemies();
        InitializeNewGame();
        Froguelike_UIManager.instance.ShowTitleScreen();
        hasGameStarted = false;
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    private void ClearAllItems()
    {
        foreach (Froguelike_ItemInfo item in ownedItems)
        {
            foreach (GameObject weaponGo in item.weaponsList)
            {
                Destroy(weaponGo, 0.1f);
            }
        }
        ownedItems.Clear();
    }

    private void TeleportToStart()
    {
        player.transform.localPosition = Vector3.zero;
    }

    public void ReinitializeChaptersList()
    {
        currentPlayableChaptersList = new List<Froguelike_ChapterData>(allPlayableChaptersList);
    }

    public void InitializeStuff()
    {
        ReinitializeChaptersList();
    }

    public void UpdateMap()
    {
        map.GenerateNewTilesAroundPosition(player.transform.position);
    }
}
