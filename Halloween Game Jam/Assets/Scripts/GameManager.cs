using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ItemInfo
{
    public ItemScriptableObject item;
    public List<GameObject> weaponsList;
    public int level;
}

[System.Serializable]
public class ChapterInfo
{
    public ChapterData chapterData;
    public int chapterCount;
    public int enemiesKilledCount;
}

[System.Serializable]
public class PlayableCharacterInfo
{
    public CharacterData characterData;
    public bool unlocked;
}
public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    [Header("References")]
    public Froguelike_CharacterController player;
    public Froguelike_MapBehaviour map;
    public Transform fliesParent;
    public ParticleSystem levelUpParticleSystem;

    [Header("Prefabs")]
    public GameObject destroyParticleEffectPrefab;

    [Header("Chapters data")]
    public List<ChapterData> allPlayableChaptersList;
    [Space]
    public List<string> possibleMorals;

    [Header("Characters data")]
    public List<PlayableCharacterInfo> playableCharactersList;

    [Header("Items data")]
    public List<ItemScriptableObject> availableItems;
    public List<ItemScriptableObject> defaultItems;
    public int maxWeaponCount = 3;
    public int maxNonWeaponCount = 5;

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

    public List<ChapterInfo> chaptersPlayed;
    [Space]
    public float xp;
    public int level;
    [Space]
    public List<ItemInfo> ownedItems;
    [Space]
    public PlayableCharacterInfo currentPlayedCharacter;
    [Space]
    private List<ChapterData> currentPlayableChaptersList;
    public ChapterInfo currentChapter;

    private float nextLevelXp = 5;
    private float xpNeededForNextLevelFactor = 1.5f;

    private List<CharacterData> unlockedCharacters;

    private void Awake()
    {
        instance = this;
        hasGameStarted = false;
        isGameRunning = false;
        unlockedCharacters = new List<CharacterData>();
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

        player.ClearFriends();

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
        Froguelike_UIManager.instance.SetEatenCount(currentChapter.enemiesKilledCount);

        Froguelike_UIManager.instance.UpdateXPSlider(xp, nextLevelXp);
    }

    #region Level Up

    public List<ItemScriptableObject> levelUpPossibleItems;

    private void SpawnWeapon(ItemInfo weaponItem)
    {
        if (weaponItem.item.isWeapon)
        {
            GameObject weaponPrefab = weaponItem.item.weaponPrefab;
            GameObject weaponGo = Instantiate(weaponPrefab, player.weaponStartPoint.position, Quaternion.identity, player.weaponsParent);
            if (weaponItem.weaponsList.Count > 0)
            {
                weaponGo.GetComponent<WeaponBehaviour>().CopyWeaponStats(weaponItem.weaponsList[0].GetComponent<WeaponBehaviour>());
            }
            weaponItem.weaponsList.Add(weaponGo);
        }
    }

    private void PickItem(ItemScriptableObject pickedItem)
    {
        bool itemIsNew = true;
        int level = 0;
        ItemInfo pickedItemInfo = null;
        foreach (ItemInfo itemInfo in ownedItems)
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
                            weaponGo.GetComponent<WeaponBehaviour>().LevelUp(itemInfo.item.levels[level]);
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
            pickedItemInfo = new ItemInfo();
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
        ItemScriptableObject pickedItem = levelUpPossibleItems[index];
        PickItem(pickedItem);
        Froguelike_UIManager.instance.HideLevelUpItemSelection();
        Time.timeScale = 1;
    }

    private int GetLevelForItem(ItemScriptableObject item)
    {
        int level = 0;
        foreach (ItemInfo itemInfo in ownedItems)
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

        levelUpParticleSystem.Play();

        // Pick possible items from a pool
        List<ItemScriptableObject> possibleItems = new List<ItemScriptableObject>();

        int weaponCount = 0;
        int itemNotWeaponCount = 0;

        foreach (ItemInfo itemInfo in ownedItems)
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

        foreach (ItemScriptableObject possibleItem in availableItems)
        {
            bool itemLevelIsNotMaxed = (GetLevelForItem(possibleItem) < possibleItem.levels.Count);
            if (itemLevelIsNotMaxed)
            {
                if (possibleItem.isWeapon)
                {
                    if (weaponCount >= maxWeaponCount)
                    {
                        // only add that item IF it is already part of our owned items
                        bool alreadyOwned = false;
                        foreach (ItemInfo itemInfo in ownedItems)
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
                    if (itemNotWeaponCount >= maxNonWeaponCount)
                    {
                        // only add that item IF it is already part of our owned items
                        bool alreadyOwned = false;
                        foreach (ItemInfo itemInfo in ownedItems)
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
        }

        if (possibleItems.Count == 0)
        {
            foreach (ItemScriptableObject possibleItem in defaultItems)
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
        foreach (ItemScriptableObject item in levelUpPossibleItems)
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

    private List<ChapterData> selectionOfNextChaptersList;

    private void SelectNextPossibleChapters(int chapterCount)
    {
        selectionOfNextChaptersList = new List<ChapterData>();

        while (selectionOfNextChaptersList.Count < chapterCount)
        {
            if (currentPlayableChaptersList.Count < 1)
            {
                ReinitializeChaptersList();
            }

            ChapterData selectedChapter = currentPlayableChaptersList[Random.Range(0, currentPlayableChaptersList.Count)];
            if ( (selectedChapter.hasFriend && player.HasActiveFriend(selectedChapter.friendStyle)) || selectionOfNextChaptersList.Contains(selectedChapter))
            {
                currentPlayableChaptersList.Remove(selectedChapter);
                continue;
            }

            currentPlayableChaptersList.Remove(selectedChapter);
            selectionOfNextChaptersList.Add(selectedChapter);
        }
    }

    public void SelectCharacter(int index)
    {
        currentPlayedCharacter = playableCharactersList[index];
        SelectNextPossibleChapters(3);

        chaptersPlayed = new List<ChapterInfo>();
        Froguelike_UIManager.instance.ShowChapterSelection(1, selectionOfNextChaptersList);

        player.InitializeCharacter(currentPlayedCharacter.characterData);
        PickItem(currentPlayedCharacter.characterData.startingWeapon);
    }

    public void SelectChapter(int index)
    {
        ChapterInfo chapterInfo = new ChapterInfo();
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

        if (chaptersPlayed.Count < 5)
        {
            chaptersPlayed.Add(currentChapter);
        }
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
            map.ClearRocks();
        }
    }

    public void SpawnDestroyParticleEffect(Vector2 position)
    {
        Instantiate(destroyParticleEffectPrefab, position, Quaternion.identity);
    }

    public IEnumerator StartChapter(int chapterCount)
    {
        // Show chapter start screen
        Froguelike_UIManager.instance.ShowChapterStart(chapterCount, currentChapter.chapterData.chapterTitle);

        // Move frog to center
        TeleportToStart();
        // Set map info
        map.rockDensity = currentChapter.chapterData.amountOfRocks;
        map.waterDensity = currentChapter.chapterData.amountOfPonds;
        // Update map
        map.ClearMap();
        UpdateMap();

        // Reinitialize all weapons
        foreach (Transform tongue in player.weaponsParent)
        {
            if (tongue.GetComponent<WeaponBehaviour>() != null)
            {
                tongue.GetComponent<WeaponBehaviour>().Initialize();
            }
        }

        // Remove all enemies on screen
        Froguelike_FliesManager.instance.ClearAllEnemies();

        // Increase enemies stats
        if (chapterCount > 1)
        {
            Froguelike_FliesManager.instance.enemyDamageFactor *= 1.5f;
            Froguelike_FliesManager.instance.enemyHPFactor *= 3;
            Froguelike_FliesManager.instance.enemySpeedFactor *= 1.1f;
            Froguelike_FliesManager.instance.enemyXPFactor *= 1.8f;
        }

        // Set current wave to chapter wave
        Froguelike_FliesManager.instance.SetWave(currentChapter.chapterData.waves[0]);

        // If character is ghost in that chapter, force it to ghost sprite
        player.ForceGhost(currentChapter.chapterData.isCharacterGhost);

        // If character has hat, set hat style
        int hatStyle = 0;
        if (currentChapter.chapterData.hasHat)
        {
            hatStyle = currentChapter.chapterData.hatStyle;
        }
        player.SetHat(hatStyle);

        // If character has friend, set friend style
        if (currentChapter.chapterData.hasFriend)
        {
            player.AddActiveFriend(currentChapter.chapterData.friendStyle);
        }

        // Wait for 1.5 seconds, real time
        yield return new WaitForSecondsRealtime(1.5f);

        Time.timeScale = 1;

        // Reset kill count
        currentChapter.enemiesKilledCount = 0;
        Froguelike_UIManager.instance.SetEatenCount(currentChapter.enemiesKilledCount);

        // Play level music
        Froguelike_MusicManager.instance.PlayLevelMusic();

        // Wait for 1.5 seconds, real time
        yield return new WaitForSecondsRealtime(1.5f);

        // Show Game UI (hide chapter start screen)
        Froguelike_UIManager.instance.ShowGameUI();

        // Start game!
        chapterRemainingTime = currentChapter.chapterData.chapterLengthInSeconds;
        hasGameStarted = true;
        isGameRunning = true;
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
        Froguelike_UIManager.instance.SetExtraLives(player.revivals);
    }

    private string GetRandomMoral()
    {
        return possibleMorals[Random.Range(0, possibleMorals.Count)];
    }

    public void ShowScores()
    {
        chaptersPlayed.Add(currentChapter);
        string moral = GetRandomMoral();
        Froguelike_UIManager.instance.ShowScoreScreen(chaptersPlayed, moral, ownedItems, unlockedCharacters);
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
        foreach (ItemInfo item in ownedItems)
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
        currentPlayableChaptersList = new List<ChapterData>(allPlayableChaptersList);
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
