using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;



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

    public StatsWrapper characterStartingStats;

    public bool unlocked;
    
    // Stat must be in the list
    public bool GetValueForStat(STAT stat, out float value)
    {
        value = 0;
        StatValue statValue = characterStartingStats.GetStatValue(stat);
        bool statExists = (statValue != null);
        if (statExists)
        {
            value = (float)statValue.value;
        }
        return statExists;
    }
}

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    [Header("References")]
    public FrogCharacterController player;
    public MapBehaviour map;
    public Transform fliesParent;
    public ParticleSystem levelUpParticleSystem;

    [Header("Settings")]
    public float enemyDamageIncreaseFactorPerChapter = 1.5f;
    public float enemyHPIncreaseFactorPerChapter = 2.5f;
    public float enemySpeedIncreaseFactorPerChapter = 1.1f;
    public float enemyXPIncreaseFactorPerChapter = 1.7f;
    public float enemySpawnSpeedIncreaseFactorPerChapter = 1.7f;

    [Header("Prefabs")]
    public GameObject destroyParticleEffectPrefab;
    public float destroyParticleEffectTimespan = 1.0f;

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

    [Header("Final Chapter data")]
    public ChapterData finalChapter;

    [Header("XP")]
    public float startLevelXp = 5;
    public float startXpNeededForNextLevelFactor = 1.5f;

    [Header("Save")]
    public string saveFileName;

    [Header("Runtime - meta data")]
    public long availableCurrency;

    [Header("Runtime - one run data")]
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
    [Space]
    public long currentCollectedCurrency;

    private float nextLevelXp = 5;
    private float xpNeededForNextLevelFactor = 1.5f;

    private List<int> unlockedCharactersIndex;

    private SaveData currentSavedData;

    #region Save

    public void SaveDataToFile()
    {
        currentSavedData.Save(saveFileName);
    }

    public void TryLoadDataFromFile()
    {
        SaveData loadedData;
        if (SaveData.Load(saveFileName, out loadedData))
        {
            // Load saved data
            currentSavedData = loadedData;
        }
        else
        {
            // If there's no save file, create a new save file and save it immediately
            currentSavedData = new SaveData();

            // Fill the save file with all starting stats of characters
            for (int i=0; i< playableCharactersList.Count; i++)
            {
                foreach (StatValue statValue in playableCharactersList[i].characterData.startingStatsList)
                {
                    currentSavedData.startingStatsForCharactersList[i].statsList.Add(new StatValue(statValue));
                }
            }

            // Fill the save file with all starting stats of shop items
            currentSavedData.shopItems = ShopManager.instance.availableItemsList;

            SaveDataToFile();
        }
    }

    public bool EraseSaveFile()
    {
        return SaveData.EraseSaveFile(saveFileName);
    }

    #endregion

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
        UIManager.instance.UpdateXPSlider(0, nextLevelXp);
        InvokeRepeating("UpdateMap", 0.2f, 0.5f);
        BackToTitleScreen();
    }

    private void Update()
    {
        if (isGameRunning)
        {
            chapterRemainingTime -= Time.deltaTime;
            UIManager.instance.SetTimer(chapterRemainingTime);
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
        currentCollectedCurrency = 0;
        UIManager.instance.UpdateInGameCurrencyText(currentCollectedCurrency);

        levelUpChoiceIsVisible = false;
        chapterChoiceIsVisible = false;
        Pause(false);

        nextLevelXp = startLevelXp;
        xpNeededForNextLevelFactor = startXpNeededForNextLevelFactor;

        player.ClearFriends();
        player.ClearHats();

        UIManager.instance.UpdateLevel(level);
        UIManager.instance.UpdateXPSlider(xp, xpNeededForNextLevelFactor);

        FliesManager.instance.enemyDamageFactor = 1;
        FliesManager.instance.enemyHPFactor = 1;
        FliesManager.instance.enemySpeedFactor = 1;
        FliesManager.instance.enemyXPFactor = 1;
        FliesManager.instance.enemySpawnSpeedFactor = 1;

        WeaponBehaviour.rotatingTongueCount = 0;

        unlockedCharactersIndex = new List<int>();
        
        currentSavedData.attempts++;
        SaveDataToFile();

        ClearAllItems();
        TeleportToStart();
    }

    public void IncreaseXP(float moreXP)
    {
        xp += moreXP;

        // Update XP and Level BEFORE the level up happens
        UIManager.instance.UpdateXPSlider(xp, nextLevelXp);
        UIManager.instance.UpdateLevel(level);

        if (xp >= nextLevelXp)
        {
            // Trigger Level Up Screen!
            LevelUP();

            xp -= nextLevelXp;
            nextLevelXp *= xpNeededForNextLevelFactor;
        }
    }

    public void EatFly(float experiencePoints, bool instantlyEndChapter = false)
    {
        currentChapter.enemiesKilledCount++;
        UIManager.instance.SetEatenCount(currentChapter.enemiesKilledCount);
        if (instantlyEndChapter)
        {
            EndChapter();
        }
        else
        {
            IncreaseXP(experiencePoints * (1 + player.experienceBoost));
        }
    }

    #region Level Up

    public bool levelUpChoiceIsVisible;

    public List<ItemScriptableObject> levelUpPossibleItems;

    private void SpawnWeapon(ItemInfo weaponItem)
    {
        if (weaponItem.item.isWeapon)
        {
            WeaponData weaponData = weaponItem.item.weaponData;
            GameObject weaponPrefab = weaponItem.item.weaponData.weaponPrefab;
            GameObject weaponGo = Instantiate(weaponPrefab, player.weaponStartPoint.position, Quaternion.identity, player.weaponsParent);
            if (weaponItem.weaponsList.Count > 0)
            {
                // weapon is not the first, we should copy the values from the previous ones
                weaponGo.GetComponent<WeaponBehaviour>().CopyWeaponStats(weaponItem.weaponsList[0].GetComponent<WeaponBehaviour>());
            }
            else
            {
                // weapon is the first, we should initialize it from the data we have
                weaponGo.GetComponent<WeaponBehaviour>().Initialize(weaponData);
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
        UIManager.instance.HideLevelUpItemSelection();
        Time.timeScale = 1;
        levelUpChoiceIsVisible = false;

        IncreaseXP(0);
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
        levelUpChoiceIsVisible = true;
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

        int defaultItemIndex = 0;
        while (possibleItems.Count < 3)
        {
            possibleItems.Add(defaultItems[defaultItemIndex]);
            defaultItemIndex++;
        }

        levelUpPossibleItems.Clear();

        if (possibleItems.Count == 3)
        {
            // we want to keep the order
            foreach (ItemScriptableObject item in possibleItems)
            {
                levelUpPossibleItems.Add(item);
            }
            possibleItems.Clear();
        }
        else
        {
            // we want to choose 3 at random from the list
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
        }

        // Find levels for each of these items
        List<int> itemLevels = new List<int>();
        foreach (ItemScriptableObject item in levelUpPossibleItems)
        {
            itemLevels.Add(GetLevelForItem(item) + 1);
        }

        // Show Update level UI
        UIManager.instance.ShowLevelUpItemSelection(levelUpPossibleItems, itemLevels);
    }

    #endregion

    public void OpenCharacterSelection()
    {
        UIManager.instance.ShowCharacterSelection(playableCharactersList, ShopManager.instance.statsBonuses);
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

        InitializeNewGame();

        SelectNextPossibleChapters(3);
        chaptersPlayed = new List<ChapterInfo>();
        UIManager.instance.ShowChapterSelection(1, selectionOfNextChaptersList);

        player.InitializeCharacter(currentPlayedCharacter);
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

    public bool chapterChoiceIsVisible;

    public void EndChapter()
    {
        // Add current played chapter to the list
        chaptersPlayed.Add(currentChapter);

        // Check for possibly having unlocked characters
        if (CheckForUnlockingCharacters())
        {
            SaveDataToFile();
        }

        // Stop time
        Time.timeScale = 0;
        chapterRemainingTime = 120;

        if (chaptersPlayed.Count >= 6)
        {
            // This was the final chapter
            // Game must end now and display Score
            ShowScores();
        }
        else if (chaptersPlayed.Count == 5)
        {
            // This was the chapter before the last, we must now start the last chapter (it is forced)
            chapterRemainingTime = finalChapter.chapterLengthInSeconds;
            FliesManager.instance.ClearAllEnemies();
            ChapterInfo chapterInfo = new ChapterInfo();
            chapterInfo.chapterData = finalChapter;
            chapterInfo.chapterCount = 6;
            chapterInfo.enemiesKilledCount = 0;
            currentChapter = chapterInfo;
            StartCoroutine(StartChapter(chapterInfo.chapterCount));
        }
        else
        {
            // This was a chapter before 5th, so we offer a choice between 3 chapters for the next one
            chapterChoiceIsVisible = true;
            SelectNextPossibleChapters(3);
            UIManager.instance.ShowChapterSelection(chaptersPlayed.Count + 1, selectionOfNextChaptersList);
        }
    }

    public void SpawnDestroyParticleEffect(Vector2 position)
    {
        GameObject particleEffectGo = Instantiate(destroyParticleEffectPrefab, position, Quaternion.identity);
        Destroy(particleEffectGo, destroyParticleEffectTimespan);
    }

    public IEnumerator StartChapter(int chapterCount)
    {
        // Show chapter start screen
        UIManager.instance.ShowChapterStart(chapterCount, currentChapter.chapterData.chapterTitle);

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
                tongue.GetComponent<WeaponBehaviour>().ResetWeapon();
            }
        }

        // Remove all enemies on screen
        FliesManager.instance.ClearAllEnemies();

        // Increase enemies stats
        if (chapterCount > 1)
        {
            FliesManager.instance.enemyDamageFactor *= enemyDamageIncreaseFactorPerChapter;
            FliesManager.instance.enemyHPFactor *= enemyHPIncreaseFactorPerChapter;
            FliesManager.instance.enemySpeedFactor *= enemySpeedIncreaseFactorPerChapter;
            FliesManager.instance.enemyXPFactor *= enemyXPIncreaseFactorPerChapter;
            FliesManager.instance.enemySpawnSpeedFactor *= enemySpawnSpeedIncreaseFactorPerChapter;
        }

        // Spawn Start Wave
        if (currentChapter.chapterData.startingWave != null)
        {
            FliesManager.instance.SpawnStartWave(currentChapter.chapterData.startingWave);
        }

        // Set current wave to chapter wave
        FliesManager.instance.SetWave(currentChapter.chapterData.waves[0]);

        // If character is ghost in that chapter, force it to ghost sprite
        player.ForceGhost(currentChapter.chapterData.isCharacterGhost);

        // Add hat if needed
        if (currentChapter.chapterData.hasHat)
        {
            player.AddHat(currentChapter.chapterData.hatStyle);
        }

        // If character has friend, set friend style
        if (currentChapter.chapterData.hasFriend)
        {
            player.AddActiveFriend(currentChapter.chapterData.friendStyle);
        }

        // Wait for 1.5 seconds, real time
        yield return new WaitForSecondsRealtime(2.9f);

        Time.timeScale = 1;

        // Reset kill count
        currentChapter.enemiesKilledCount = 0;
        UIManager.instance.SetEatenCount(currentChapter.enemiesKilledCount);

        // Play level music
        MusicManager.instance.PlayLevelMusic();

        // Wait for 1.5 seconds, real time
        yield return new WaitForSecondsRealtime(0.1f);

        // Show Game UI (hide chapter start screen)
        UIManager.instance.ShowGameUI();

        // Start game!
        chapterRemainingTime = currentChapter.chapterData.chapterLengthInSeconds;
        hasGameStarted = true;
        isGameRunning = true;
        chapterChoiceIsVisible = false;
    }

    public void TriggerGameOver()
    {
        Time.timeScale = 0;
        isGameRunning = false;
        UIManager.instance.ShowGameOver((player.revivals > 0));
        currentSavedData.deathCount++;
        CheckForUnlockingCharacters();
        SaveDataToFile();
    }

    public void Respawn()
    {
        Time.timeScale = 1;
        player.Respawn();
        isGameRunning = true;
        UIManager.instance.ShowGameUI();
        player.revivals--;
        UIManager.instance.SetExtraLives(player.revivals);
    }

    private string GetRandomMoral()
    {
        return possibleMorals[Random.Range(0, possibleMorals.Count)];
    }

    private bool UnlockCharacter(int index)
    {
        bool characterNewlyUnlocked = false;
        if (!playableCharactersList[index].unlocked)
        {
            unlockedCharactersIndex.Add(index);
            playableCharactersList[index].unlocked = true;
            currentSavedData.unlockedCharacterIndexList.Add(index);
            characterNewlyUnlocked = true;
        }
        return characterNewlyUnlocked;
    }

    private bool CheckForUnlockingCharacters()
    {
        bool characterUnlocked = false;
        bool gameIsWon = chaptersPlayed.Count >= 6;

        // Unlock TOAD?
        // After winning one game
        if (gameIsWon)
        {
            characterUnlocked |= UnlockCharacter(1);
        }

        // Unlock GHOST?
        // After dying 15 times
        if (currentSavedData.deathCount >= 15)
        {
            characterUnlocked |= UnlockCharacter(2);
        }

        if (gameIsWon)
        {
            bool hasMaxedOutCurse = false;
            bool hasMaxedOutCursedTongue = false;
            bool hasMaxedOutPoisonousTongue = false;
            foreach (ItemInfo item in ownedItems)
            {
                if (item.item.itemName.Equals("Curse") && item.level.Equals(item.item.levels.Count))
                {
                    hasMaxedOutCurse = true;
                }
                if (item.item.itemName.Equals("Cursed Tongue") && item.level.Equals(item.item.levels.Count))
                {
                    hasMaxedOutCursedTongue = true;
                }
                if (item.item.itemName.Equals("Poisonous Tongue") && item.level.Equals(item.item.levels.Count))
                {
                    hasMaxedOutPoisonousTongue = true;
                }
            }

            // Unlock SWIMMING FROG?
            // After winning a game with all 3 hats
            if (player.HasHat(1) && player.HasHat(2) && player.HasHat(3))
            {
                characterUnlocked |= UnlockCharacter(4);
            }


            // Unlock POISONOUS FROG?
            // After winning a game with a maxed out poisonous tongue
            if (hasMaxedOutPoisonousTongue)
            {
                characterUnlocked |= UnlockCharacter(3);
            }

            // Unlock TOMATO FROG?
            // After winning a game with maxed out curse and maxed out cursed tongue
            if (hasMaxedOutCurse && hasMaxedOutCursedTongue)
            {
                characterUnlocked |= UnlockCharacter(5);
            }

            // Unlock STANLEY?
            // After winning a game with all 4 friends
            if (player.HasActiveFriend(0) && player.HasActiveFriend(1) && player.HasActiveFriend(2) && player.HasActiveFriend(3))
            {
                characterUnlocked |= UnlockCharacter(6);
            }
        }

        return characterUnlocked;
    }

    public void GiveUp()
    {
        Pause(false);

        // Add current played chapter to the list
        chaptersPlayed.Add(currentChapter);

        // Show score screen
        ShowScores();
    }

    public void ShowScores()
    {
        Time.timeScale = 0;
        string moral = GetRandomMoral();

        List<CharacterData> unlockedCharacters = new List<CharacterData>();
        foreach (int unlockedCharacterIndex in unlockedCharactersIndex)
        {
            unlockedCharacters.Add(playableCharactersList[unlockedCharacterIndex].characterData);
        }

        if (chaptersPlayed.Count >= 6)
        {
            // Game is won!
            currentSavedData.wins++;
            currentSavedData.wonTheGameWithCharacterIndexList[playableCharactersList.IndexOf(currentPlayedCharacter)]++;
        }

        // Compute actual score
        int currentScore = 0;
        foreach (ChapterInfo chapter in chaptersPlayed)
        {
            currentScore += chapter.enemiesKilledCount;
        }
        if (currentScore > currentSavedData.bestScore)
        {
            currentSavedData.bestScore = currentScore;
        }
        currentSavedData.cumulatedScore += currentScore;

        // Collect currency for good
        ChangeAvailableCurrency(currentCollectedCurrency, false);
        currentCollectedCurrency = 0;

        // Maybe unlock some characters if conditions are met, and save data too
        CheckForUnlockingCharacters();
        SaveDataToFile();

        UIManager.instance.ShowScoreScreen(chaptersPlayed, moral, ownedItems, unlockedCharacters);
    }

    public void BackToTitleScreen()
    {
        FliesManager.instance.ClearAllEnemies();
        UIManager.instance.ShowTitleScreen();
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
        player.ResetPosition();
    }

    public void ReinitializeChaptersList()
    {
        currentPlayableChaptersList = new List<ChapterData>(allPlayableChaptersList);
    }

    public void InitializeStuff()
    {
        // Setup the shop manager
        ShopManager.instance.ResetShop(true);

        // Load save file
        TryLoadDataFromFile();

        // Update unlocked characters
        List<int> unlockedCharacterIndexList = currentSavedData.unlockedCharacterIndexList;
        for (int i = 1; i < playableCharactersList.Count; i++)
        {
            playableCharactersList[i].unlocked = false; // lock every character by default
        }
        foreach (int unlockedCharacter in currentSavedData.unlockedCharacterIndexList)
        {
            playableCharactersList[unlockedCharacter].unlocked = true; // unlock only the ones that are in the save file
        }

        // Replace the starting stats of characters with data from save file
        for (int i = 0; i < playableCharactersList.Count; i++)
        {
            playableCharactersList[i].characterStartingStats.statsList = currentSavedData.startingStatsForCharactersList[i].statsList;
        }

        // Update shop items with data from save file
        ShopManager.instance.ReplaceAvailableItemsList(currentSavedData.shopItems);
        ShopManager.instance.currencySpentInTheShop = currentSavedData.currencySpentInShop;

        // Update available currency
        availableCurrency = currentSavedData.availableCurrency;

        ReinitializeChaptersList();
    }

    public void UpdateMap()
    {
        map.GenerateNewTilesAroundPosition(player.transform.position);
    }

    #region Pause

    public bool gameIsPaused;

    public void TogglePause()
    {
        Pause(!gameIsPaused);
    }

    public void Pause(bool pause)
    {
        gameIsPaused = pause;
        if (!levelUpChoiceIsVisible && !chapterChoiceIsVisible)
        {
            Time.timeScale = pause ? 0 : 1;
        }
        if (pause)
        {
            UIManager.instance.ShowPauseScreen();
        }
        else
        {
            UIManager.instance.HidePauseScreen();
        }
    }

    #endregion
    
    public void CollectCollectible(string collectibleName)
    {
        if (collectibleName.Contains("Currency"))
        {
            if (int.TryParse(collectibleName.Split("+")[1], out int currency))
            {
                currentCollectedCurrency += Mathf.RoundToInt(currency * ( 1 + player.currencyBoost));
                UIManager.instance.UpdateInGameCurrencyText(currentCollectedCurrency);
            }
        }
        else if (collectibleName.Contains("XP"))
        {
            if (int.TryParse(collectibleName.Split("+")[1], out int xpBonus))
            {
                IncreaseXP(xpBonus);
            }
        }
        else if (collectibleName.Contains("HP"))
        {
            if (int.TryParse(collectibleName.Split("+")[1], out int hpBonus))
            {
                player.Heal(hpBonus);
            }
        }
    }

    public void ClearSaveFile()
    {
        // Clear the shop
        ShopManager.instance.RefundAll();
        ShopManager.instance.currencySpentInTheShop = 0;
        availableCurrency = 0;

        // Clear save file and update everything accordingly
        bool fileErased = EraseSaveFile();
        if (fileErased)
        {
            TryLoadDataFromFile();
        }

        InitializeStuff();
        BackToTitleScreen();
    }

    public void UpdateShopInfoInCurrentSave()
    {
        currentSavedData.shopItems = ShopManager.instance.availableItemsList;
        currentSavedData.currencySpentInShop = ShopManager.instance.currencySpentInTheShop;
        SaveDataToFile();
    }

    public void ChangeAvailableCurrency(long currencyChange, bool saveData = true)
    {
        availableCurrency += currencyChange;
        currentSavedData.availableCurrency = availableCurrency;
        if (saveData)
        {
            SaveDataToFile();
        }
    }
}
