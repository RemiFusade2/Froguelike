using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class RunManager : MonoBehaviour
{
    // Singleton
    public static RunManager instance;

    [Header("References")]
    public FrogCharacterController player;
    public Transform fliesParent;
    public ParticleSystem levelUpParticleSystem;
    
    [Header("References - UI")]
    public Slider xpSlider;
    public TextMeshProUGUI levelText;
    [Space]
    public TextMeshProUGUI currencyText;
    [Space]
    public string timerPrefix;
    public TextMeshProUGUI timerText;
    [Space]
    public string killCountPrefix;
    public TextMeshProUGUI killCountText;
    [Space]
    public string extraLivesPrefix;
    public TextMeshProUGUI extraLivesCountText;
    [Space]
    public List<GameObject> levelUpChoicesPanels;
    public List<TextMeshProUGUI> levelUpChoicesTitles;
    public List<TextMeshProUGUI> levelUpChoicesLevels;
    public List<TextMeshProUGUI> levelUpChoicesDescriptions;
    [Space]
    public Color defaultUIColor;
    public Color newItemColor;
    public Color maxLevelColor;

    [Header("Settings - XP")]
    public float startLevelXp = 5;
    public float startXpNeededForNextLevelFactor = 1.5f;

    [Header("Runtime - XP")]
    public float xp;
    public int level;

    private float nextLevelXp = 5;
    private float xpNeededForNextLevelFactor = 1.5f;

    [Header("Runtime - Current played character")]
    public PlayableCharacter currentPlayedCharacter;

    [Header("Runtime - Current played chapter")]
    public List<Chapter> completedChaptersList;
    public Chapter currentChapter;
    public float chapterRemainingTime; // in seconds

    [Header("Runtime - Collected currency")]
    public long currentCollectedCurrency;

    [Header("Runtime - Run Items")]
    public List<RunItemInfo> ownedItems;
    
    [Header("Runtime - Leveling Up")]
    public List<RunItemData> selectionOfPossibleRunItemsList;
    public bool levelUpChoiceIsVisible;

    public List<RunWeaponInfo> GetOwnedWeapons()
    {
        List<RunWeaponInfo> weapons = new List<RunWeaponInfo>();
        foreach (RunItemInfo runItem in ownedItems)
        {
            if (runItem is RunWeaponInfo)
            {
                weapons.Add(runItem as RunWeaponInfo);
            }
        }
        return weapons;
    }

    public List<RunStatItemInfo> GetOwnedStatItems()
    {
        List<RunStatItemInfo> statItems = new List<RunStatItemInfo>();
        foreach (RunItemInfo runItem in ownedItems)
        {
            if (runItem is RunStatItemInfo)
            {
                statItems.Add(runItem as RunStatItemInfo);
            }
        }
        return statItems;
    }

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

    void Start()
    {
        InvokeRepeating("UpdateMap", 0.2f, 0.5f);
    }
       
    public void StartNewRun(PlayableCharacter character)
    {
        InitializeNewRun();

        // Setup the player controller using the player data that we have
        currentPlayedCharacter = character;
        player.InitializeCharacter(character);

        // Pick all the items this character starts with
        foreach (RunItemData itemData in character.characterData.startingItems)
        {
            PickRunItem(itemData);
        }

        // Show a selection of Chapters to pick from for the first chapter of the Run
        ChapterManager.instance.ShowChapterSelection(completedChaptersList);
    }

    public void UpdateInGameCurrencyText(long currencyValue)
    {
        currencyText.text = Tools.FormatCurrency(currencyValue, DataManager.instance.currencySymbol);
    }


    public void InitializeNewRun()
    {
        currentCollectedCurrency = 0;
        UpdateInGameCurrencyText(currentCollectedCurrency);

        levelUpChoiceIsVisible = false;
        //Pause(false);
        
        // Remove all friends and hats
        player.ClearFriends();
        player.ClearHats();

        // Level and XP
        level = 1;
        xp = 0;
        nextLevelXp = startLevelXp;
        xpNeededForNextLevelFactor = startXpNeededForNextLevelFactor;
        // In-game UI
        UpdateLevelText(level);
        UpdateXPSlider(xp, xpNeededForNextLevelFactor);

        // Reset EnemiesManager
        EnemiesManager.instance.ResetFactors();

        // Reset WeaponBehaviour static values
        WeaponBehaviour.ResetStaticValues();
        
        // Reset Items
        ClearAllItems();

        // Reset chapters
        completedChaptersList.Clear();
        currentChapter = null;

        // Teleport player to starting position
        player.ResetPosition();
    }

    private void ClearAllItems()
    {
        List<RunWeaponInfo> ownedWeapons = GetOwnedWeapons();
        foreach (RunWeaponInfo weapon in ownedWeapons)
        {
            foreach (GameObject weaponGo in weapon.activeWeaponsList)
            {
                Destroy(weaponGo, 0.1f);
            }
            weapon.activeWeaponsList.Clear();
        }
        ownedWeapons.Clear();
        ownedItems.Clear();
    }

    public void UpdateMap()
    {
        MapBehaviour.instance.GenerateNewTilesAroundPosition(player.transform.position);
    }

    public void IncreaseXP(float moreXP)
    {
        xp += moreXP;

        // Update XP and Level BEFORE the level up happens
        UpdateXPSlider(xp, nextLevelXp);
        UpdateLevelText(level);

        if (xp >= nextLevelXp)
        {
            // Trigger Level Up Screen!
            LevelUP();

            xp -= nextLevelXp;
            nextLevelXp *= xpNeededForNextLevelFactor;
        }
    }
    
    private void SetTimer(float remainingTime)
    {
        System.TimeSpan time = new System.TimeSpan(0, 0, Mathf.RoundToInt(remainingTime));
        timerText.text = timerPrefix + time.ToString("m\\:ss");
    }

    private void SetEatenCount(int eatenBugs)
    {
        killCountText.text = killCountPrefix + eatenBugs.ToString();
    }

    public void SetExtraLives(int reviveCount)
    {
        extraLivesCountText.text = extraLivesPrefix + reviveCount.ToString();
    }

    private void IncreaseKillCount(int kills)
    {
        currentChapter.enemiesKilledCount += kills;
        SetEatenCount(currentChapter.enemiesKilledCount);
    }
    
    public void Respawn()
    {
        GameManager.instance.SetTimeScale(1);

        player.Respawn();
        GameManager.instance.isGameRunning = true;
        player.revivals--;

        UIManager.instance.ShowGameUI();
        SetExtraLives(player.revivals);
    }

    public void EatFly(float experiencePoints, bool instantlyEndChapter = false)
    {
        // Increase kill count by 1 and display it
        IncreaseKillCount(1);

        if (instantlyEndChapter)
        {
            EndChapter();
        }
        else
        {
            IncreaseXP(experiencePoints * (1 + player.experienceBoost));
        }
    }

    private void ComputeScore()
    {
        // Stop time & music
        GameManager.instance.SetTimeScale(0);
        MusicManager.instance.PauseMusic();

        // Check if this is a win
        if (completedChaptersList.Count >= 6)
        {
            // Game is won!
            GameManager.instance.RegisterWin();
            CharacterManager.instance.WonTheGameWithCharacter(currentPlayedCharacter);
        }
        
        // Compute actual score
        int currentScore = 0;
        foreach (Chapter chapter in completedChaptersList)
        {
            currentScore += chapter.enemiesKilledCount;
        }
        if (completedChaptersList.Count < 6 && currentChapter != null)
        {
            currentScore += currentChapter.enemiesKilledCount;
        }
        GameManager.instance.RegisterScore(currentScore);

        // Collect currency for good
        GameManager.instance.ChangeAvailableCurrency(currentCollectedCurrency);
        currentCollectedCurrency = 0;

        // Maybe unlock some characters if conditions are met
        List<string> unlockedCharacters = AchievementManager.instance.CheckForUnlockingCharacters();

        // Add the current chapter to the list (even if current chapter was not completed)
        List<Chapter> chaptersPlayed = new List<Chapter>(completedChaptersList);
        if (currentChapter != null)
        {
            chaptersPlayed.Add(currentChapter);
        }

        // Game must be saved
        SaveDataManager.instance.isSaveDataDirty = true;

        // Display the score screen
        ScoreManager.instance.ShowScores(chaptersPlayed, currentPlayedCharacter, ownedItems, unlockedCharacters);
    }

    public void EndChapter()
    {
        // Add current played chapter to the list
        completedChaptersList.Add(currentChapter);

        // Stop time
        GameManager.instance.SetTimeScale(0);

        if (completedChaptersList.Count >= 6)
        {
            // This was the final chapter
            // Game must end now and display Score
            ComputeScore();
        }
        else if (completedChaptersList.Count == 5)
        {
            // This was the chapter before the last, we must now start the last chapter (it is forced)
            StartChapter(ChapterManager.instance.finalChapter);
        }
        else
        {
            // This was a chapter before 5th, so we offer a choice for the next chapter
            ChapterManager.instance.ShowChapterSelection(completedChaptersList);
        }
    }

    public void StartChapter(Chapter chapter)
    {
        // Set timer
        chapterRemainingTime = chapter.chapterData.chapterLengthInSeconds;

        // Remove enemies on screen
        EnemiesManager.instance.ClearAllEnemies();

        chapter.enemiesKilledCount = 0;

        currentChapter = chapter;
        
        StartCoroutine(StartChapterAsync());
    }

    private IEnumerator StartChapterAsync()
    {
        int chapterCount = completedChaptersList.Count + 1;

        // Show chapter start screen
        ChapterManager.instance.ShowChapterStartScreen(chapterCount, currentChapter.chapterData.chapterTitle);

        // Teleport player to starting position
        player.ResetPosition();

        // Set map info
        MapBehaviour.instance.rockMinMax = currentChapter.chapterData.amountOfRocksPerTile_minmax;
        MapBehaviour.instance.waterMinMax = currentChapter.chapterData.amountOfPondsPerTile_minmax;
        // Update map
        MapBehaviour.instance.ClearMap();
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
        EnemiesManager.instance.ClearAllEnemies();

        // Increase enemies stats
        if (chapterCount > 1)
        {
            EnemiesManager.instance.IncreaseFactors();
        }

        // Spawn Start Wave
        if (currentChapter.chapterData.waves.Count > 0)
        {
            EnemiesManager.instance.SpawnStartWave(currentChapter.chapterData.waves[0]);
        }

        // Set current wave to chapter wave
        EnemiesManager.instance.SetWave(currentChapter.chapterData.waves[0]);

        // If character is ghost in that chapter, force it to ghost sprite
        player.ForceGhost(currentChapter.chapterData.characterStyleChange == CharacterStyleType.GHOST);

        // Add hat if needed
        if (currentChapter.chapterData.addHat != HatType.NONE)
        {
            player.AddHat(currentChapter.chapterData.addHat);
        }

        // If character has friend in this chapter, add it
        if (currentChapter.chapterData.addFriend != FriendType.NONE)
        {
            player.AddActiveFriend(currentChapter.chapterData.addFriend);
        }
        
        yield return new WaitForSecondsRealtime(2.9f);

        GameManager.instance.SetTimeScale(1);

        // Reset kill count
        currentChapter.enemiesKilledCount = 0;
        SetEatenCount(currentChapter.enemiesKilledCount);

        // Play level music
        MusicManager.instance.PlayLevelMusic();
        
        yield return new WaitForSecondsRealtime(0.1f);

        // Show Game UI (hide chapter start screen)
        UIManager.instance.ShowGameUI();

        // Start game!
        chapterRemainingTime = currentChapter.chapterData.chapterLengthInSeconds;
        GameManager.instance.hasGameStarted = true;
        GameManager.instance.isGameRunning = true;
        ChapterManager.instance.chapterChoiceIsVisible = false;
    }

    public void EndRun()
    {
        // Show score screen
        ComputeScore();
    }

    public void UpdateTime(float deltaTime)
    {
        chapterRemainingTime -= Time.deltaTime;
        SetTimer(chapterRemainingTime);
        if (chapterRemainingTime < 0)
        {
            chapterRemainingTime = float.MaxValue;
            EndChapter();
        }
    }

    private void SpawnWeapon(RunWeaponInfo weaponItem)
    {
        RunWeaponItemData weaponItemData = weaponItem.weaponItemData;
        RunWeaponData weaponData = weaponItemData.weaponData;
        GameObject weaponPrefab = weaponData.weaponPrefab;

        // Instantiate weapon from prefab
        GameObject weaponGo = Instantiate(weaponPrefab, player.weaponStartPoint.position, Quaternion.identity, player.weaponsParent);
        
        if (weaponItem.activeWeaponsList.Count > 0)
        {
            // weapon is not the first, we should copy the values from the previous ones
            weaponGo.GetComponent<WeaponBehaviour>().CopyWeaponStats(weaponItem.activeWeaponsList[0].GetComponent<WeaponBehaviour>());
        }
        else
        {
            // weapon is the first, we should initialize it from the data we have
            weaponGo.GetComponent<WeaponBehaviour>().Initialize(weaponData);
        }
        // Add the new weapon to the list of active weapons for this RunItem
        weaponItem.activeWeaponsList.Add(weaponGo);
    }
    
    private void PickRunItem(RunItemData pickedItemData)
    {
        // Get Item Type (weapon, stat boost, or consumable)
        RunItemType pickedItemType = pickedItemData.GetItemType();

        bool itemIsNew = true;
        int level = 0;
        RunItemInfo pickedItemInfo = null;

        switch (pickedItemType)
        {
            case RunItemType.CONSUMABLE:
                // Resolve this consumable once
                player.ResolvePickedConsumableItem(pickedItemData as RunConsumableItemData);
                break;
            case RunItemType.STAT_BONUS:
                // Add this item to the list or upgrade the corresponding item level

                // Check if we already have this item
                foreach (RunItemInfo ownedItem in ownedItems)
                {
                    RunItemData itemData = ownedItem.GetRunItemData();
                    if (itemData.Equals(pickedItemData))
                    {
                        // We already own such an item
                        itemIsNew = false;
                        pickedItemInfo = ownedItem;
                        ownedItem.level++;
                        level = ownedItem.level - 1;
                        break;
                    }
                }

                // If we didn't have that item, then create a new RunItemInfo object and store it for future use
                if (itemIsNew && pickedItemInfo == null)
                {
                    // Create item info and add it to owned items
                    RunStatItemInfo newStatItemInfo = new RunStatItemInfo();
                    newStatItemInfo.level = 1;
                    newStatItemInfo.itemData = pickedItemData as RunStatItemData;

                    pickedItemInfo = newStatItemInfo;

                    ownedItems.Add(pickedItemInfo);
                }

                // resolve the item picked (according to its current level)
                int levelIndex = pickedItemInfo.level - 1;
                levelIndex = Mathf.Clamp(levelIndex, 0, pickedItemInfo.GetRunItemData().GetMaxLevelCount());
                player.ResolvePickedStatItemLevel((pickedItemData as RunStatItemData).statBoostLevels[levelIndex]);

                break;
            case RunItemType.WEAPON:
                // Add this weapon to the list or upgrade the corresponding weapon level
                // Eventually spawn new weapon(s) if needed

                // Check if we already have this weapon, and if we do, upgrade all active weapons with the new stats
                List<RunWeaponInfo> ownedWeapons = GetOwnedWeapons();
                foreach (RunWeaponInfo ownedWeapon in ownedWeapons)
                {
                    if (ownedWeapon.weaponItemData.Equals(pickedItemData))
                    {
                        // We already own such an item
                        itemIsNew = false;
                        pickedItemInfo = ownedWeapon;
                        level = ownedWeapon.level - 1;
                        
                        // we need to upgrade all similar weapons
                        foreach (GameObject weaponGo in ownedWeapon.activeWeaponsList)
                        {
                            if (level >= 0 && level < ownedWeapon.weaponItemData.GetMaxLevelCount())
                            {
                                weaponGo.GetComponent<WeaponBehaviour>().LevelUp(ownedWeapon.weaponItemData.weaponBoostLevels[level]);
                            }
                        }

                        ownedWeapon.level++;

                        break;
                    }
                }

                // Will store how many weapons we need to spawn
                int spawnWeapons = 0;

                if (itemIsNew && pickedItemInfo == null)
                {
                    // If we didn't have that weapon, then create a new RunItemInfo object and store it for future use
                    // Create item info and add it to owned items
                    RunWeaponInfo newWeaponInfo = new RunWeaponInfo();
                    newWeaponInfo.level = 1;
                    newWeaponInfo.weaponItemData = pickedItemData as RunWeaponItemData;
                    newWeaponInfo.activeWeaponsList = new List<GameObject>();
                    
                    pickedItemInfo = newWeaponInfo;
                    
                    ownedItems.Add(pickedItemInfo);

                    // Also spawn as many weapons as required
                    int firstSpawnCount = 1;
                    WeaponStatValue weaponCountStatValue = newWeaponInfo.weaponItemData.weaponData.weaponBaseStats.GetStatValue(WeaponStat.COUNT);
                    if (weaponCountStatValue != null)
                    {
                        firstSpawnCount = Mathf.RoundToInt((float)weaponCountStatValue.value);
                    }
                    for (int w = 0; w < firstSpawnCount; w++)
                    {
                        SpawnWeapon(pickedItemInfo as RunWeaponInfo);
                    }
                }
                else
                {
                    // We already had that weapon, maybe we need to spawn more weapons depending on if this level adds any
                    if (level >= 0 && level < pickedItemData.GetMaxLevelCount())
                    {
                        WeaponStatValue weaponCountStatValue = (pickedItemData as RunWeaponItemData).weaponBoostLevels[level].weaponStatUpgrades.GetStatValue(WeaponStat.COUNT);
                        spawnWeapons = (weaponCountStatValue == null) ? 0 : Mathf.RoundToInt((float)weaponCountStatValue.value);

                        // Spawn as many weapons as needed
                        for (int w = 0; w < spawnWeapons; w++)
                        {
                            SpawnWeapon(pickedItemInfo as RunWeaponInfo);
                        }
                    }
                }

                break;
        }
    }

    public void ChooseLevelUpChoice(int index)
    {
        RunItemData pickedItem = selectionOfPossibleRunItemsList[index];
        PickRunItem(pickedItem);

        HideLevelUpItemSelection();

        levelUpChoiceIsVisible = false;

        GameManager.instance.SetTimeScale(1);

        IncreaseXP(0);
    }

    public int GetLevelForItem(RunItemData itemData)
    {
        int level = 0;
        foreach (RunItemInfo itemInfo in ownedItems)
        {
            bool foundIt = false;
            if (itemInfo is RunStatItemInfo)
            {
                // Stat
                if ((itemInfo as RunStatItemInfo).itemData.Equals(itemData))
                {
                    foundIt = true;
                }
            }
            if (itemInfo is RunWeaponInfo)
            {
                // Weapon
                if ((itemInfo as RunWeaponInfo).weaponItemData.Equals(itemData))
                {
                    foundIt = true;
                }
            }

            if (foundIt)
            {
                level = itemInfo.level;
                break;
            }
        }
        return level;
    }

    public void LevelUP()
    {
        levelUpChoiceIsVisible = true;
        level++;
        GameManager.instance.SetTimeScale(0);

        levelUpParticleSystem.Play();

        // Pick possible items from a pool
        selectionOfPossibleRunItemsList = RunItemManager.instance.PickARandomSelectionOfRunItems(3);

        // Find next levels for each of these items
        List<int> itemLevels = new List<int>();
        foreach (RunItemData item in selectionOfPossibleRunItemsList)
        {
            itemLevels.Add(GetLevelForItem(item));
        }

        // Show Update level Up
        ShowLevelUpItemSelection(selectionOfPossibleRunItemsList, itemLevels);
    }

    #region UI

    private void UpdateXPSlider(float xp, float maxXp)
    {
        xpSlider.maxValue = maxXp;
        xpSlider.value = xp;
    }

    private void UpdateLevelText(int level)
    {
        levelText.text = "LVL " + level.ToString();
    }

    public void HideLevelUpItemSelection()
    {
        UIManager.instance.levelUpPanelAnimator.SetBool("Visible", false);
        SoundManager.instance.PlaySlideBookSound();
    }

    #endregion

    public void ShowLevelUpItemSelection(List<RunItemData> possibleItems, List<int> itemLevels)
    {
        EventSystem.current.SetSelectedGameObject(null);
        SoundManager.instance.PlaySlideBookSound();

        UIManager.instance.levelUpPanel.SetActive(true);
        UIManager.instance.levelUpPanelAnimator.SetBool("Visible", true);

        foreach (GameObject panel in levelUpChoicesPanels)
        {
            panel.SetActive(false);
        }

        int index = 0;
        foreach (RunItemData item in possibleItems)
        {
            RunItemType itemType = item.GetItemType();
            int level = itemLevels[index];
            int nextLevel = level + 1;

            levelUpChoicesPanels[index].SetActive(true);
            levelUpChoicesTitles[index].text = item.itemName;

            bool newItem = false;
            bool maxLevelItem = false;

            switch (itemType)
            {
                case RunItemType.CONSUMABLE:
                    // item without levels
                    levelUpChoicesLevels[index].color = defaultUIColor;
                    levelUpChoicesLevels[index].text = "";
                    levelUpChoicesDescriptions[index].text = (item as RunConsumableItemData).description;
                    break;
                case RunItemType.WEAPON:
                    newItem = (level == 0);
                    maxLevelItem = (nextLevel >= item.GetMaxLevel());
                    break;
                case RunItemType.STAT_BONUS:
                    newItem = (level == 0);
                    maxLevelItem = (nextLevel >= item.GetMaxLevelCount());
                    break;
            }

            if (itemType != RunItemType.CONSUMABLE)
            {
                if (newItem)
                {
                    levelUpChoicesLevels[index].color = newItemColor;
                    levelUpChoicesLevels[index].text = "New!";
                }
                else if (maxLevelItem)
                {
                    levelUpChoicesLevels[index].color = maxLevelColor;
                    levelUpChoicesLevels[index].text = "LVL MAX";
                }
                else
                {
                    levelUpChoicesLevels[index].color = defaultUIColor;
                    levelUpChoicesLevels[index].text = "LVL " + nextLevel.ToString();
                }

                string description = "Better?";
                if (newItem)
                {
                    if (itemType == RunItemType.WEAPON)
                    {
                        // New weapon
                        description = (item as RunWeaponItemData).weaponData.description;
                    }
                    else if (itemType == RunItemType.STAT_BONUS)
                    {
                        // New stat bonus item
                        description = (item as RunStatItemData).statBoostLevels[0].description;
                    }
                }
                else if (itemType == RunItemType.WEAPON && (level - 1) < item.GetMaxLevelCount())
                {
                    // Weapon that we already have
                    description = (item as RunWeaponItemData).weaponBoostLevels[level - 1].description;
                }
                else if (itemType == RunItemType.STAT_BONUS && level < item.GetMaxLevelCount())
                {
                    // Stat bonus item that we already have
                    description = (item as RunStatItemData).statBoostLevels[level].description;
                }

                levelUpChoicesDescriptions[index].text = description.Replace("\\n","\n");
            }

            index++;
        }
    }
    
    public void CollectCollectible(string collectibleName)
    {
        if (collectibleName.Contains("Currency"))
        {
            if (int.TryParse(collectibleName.Split("+")[1], out int currency))
            {
                currentCollectedCurrency += Mathf.RoundToInt(currency * (1 + player.currencyBoost));
                UpdateInGameCurrencyText(currentCollectedCurrency);
            }
        }
        else if (collectibleName.Contains("LevelUp"))
        {
            IncreaseXP(nextLevelXp - xp);
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
}
