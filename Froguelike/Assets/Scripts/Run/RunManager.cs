using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

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

    public RunWeaponInfo()
    {
        activeWeaponsList = new List<GameObject>();
    }
}

public class RunManager : MonoBehaviour
{
    // Singleton
    public static RunManager instance;
    
    [Header("Settings - Logs")]
    public VerboseLevel logsVerboseLevel = VerboseLevel.NONE;

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
    public List<Image> levelUpChoicesIcons;
    [Space]
    public Color defaultUIColor;
    public Color newItemColor;
    public Color maxLevelColor;

    [Header("Settings - Chapter count")]
    public int maxChaptersInARun = 5;

    [Header("Settings - XP")]
    public float startLevelXp = 5; // XP needed to go from level 1 to level 2
    public float xpNeededForNextLevelMinFactor = 1.1f; // min factor applied on XP needed after each level
    public float xpNeededForNextLevelMaxFactor = 1.5f; // max factor applied on XP needed after each level
    [Space]
    [Tooltip("This curve will be applied on the xp needed factor. 1 means multiply by the max factor. 0 means multiply by the min factor")]
    public AnimationCurve xpNeededForEachLevelCurve; // curve applied on the xp needed factor

    [Header("Runtime - XP")]
    public float xp;
    public int level;

    [Header("Runtime - Current played character")]
    public PlayableCharacter currentPlayedCharacter;

    [Header("Runtime - Current played chapter")]
    public List<Chapter> completedChaptersList;
    public int[] playedChaptersKillCounts;
    public Chapter currentChapter;
    public float chapterRemainingTime; // in seconds

    [Header("Runtime - Current played wave")]
    public int currentWaveIndex;
    public float waveRemainingTime; // in seconds

    [Header("Runtime - Collected currency")]
    public long currentCollectedCurrency;

    [Header("Runtime - Run Items")]
    public List<RunItemInfo> ownedItems;

    [Header("Runtime - Leveling Up")]
    public List<RunItemData> selectionOfPossibleRunItemsList;
    public bool levelUpChoiceIsVisible;

    private float nextLevelXp;

    private float runPlayTime;
    private float runTotalTime; // pause time included


    #region Unity Callback Methods

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

    private void Update()
    {
        runPlayTime += Time.deltaTime;
        runTotalTime += Time.unscaledDeltaTime;

        if (GameManager.instance.isGameRunning && currentChapter != null && Time.timeScale > 0)
        {
            // Spawn current wave
            EnemiesManager.instance.TrySpawnWave(GetCurrentWave());

            // Wave remaining time decreases
            waveRemainingTime -= Time.deltaTime;

            if (waveRemainingTime < 0)
            {
                // When wave is finished, move on to the next one
                currentWaveIndex = (currentWaveIndex + 1) % currentChapter.chapterData.waves.Count;
                EnemiesManager.instance.InitializeWave(GetCurrentWave());
                waveRemainingTime = GetCurrentWave().duration;
            }
        }
    }

    #endregion


    public bool IsCurrentRunWon()
    {
        return completedChaptersList.Count >= maxChaptersInARun;
    }

    public Wave GetCurrentWave()
    {
        return currentChapter.chapterData.waves[currentWaveIndex];
    }

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

    public void StartNewRun(PlayableCharacter character)
    {
        if (logsVerboseLevel == VerboseLevel.MAXIMAL)
        {
            Debug.Log("Run Manager - Start a new Run with character: " + character.characterID);
        }

        InitializeNewRun();

        // Setup the player controller using the player data that we have
        currentPlayedCharacter = character;
        player.InitializeCharacter(character);

        // Pick all the items this character starts with
        foreach (RunItemData itemData in character.characterData.startingItems)
        {
            PickRunItem(itemData);
        }

        // Reset Chapters Weights
        ChapterManager.instance.ResetChaptersWeights();

        // Show a selection of Chapters to pick from for the first chapter of the Run
        ChapterManager.instance.ShowChapterSelection(0);
    }

    private void UpdateInGameCurrencyText(long currencyValue)
    {
        currencyText.text = Tools.FormatCurrency(currencyValue, DataManager.instance.currencySymbol);
    }

    public void IncreaseCollectedCurrency(long currencyValue)
    {
        currentCollectedCurrency += currencyValue;
        UpdateInGameCurrencyText(currentCollectedCurrency);
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
        // In-game UI
        UpdateLevelText(level);
        UpdateXPSlider(xp, nextLevelXp);

        // Reset WeaponBehaviour static values
        WeaponBehaviour.ResetStaticValues();

        // Reset Items
        ClearAllItems();

        // Reset kill counts
        for (int i = 0; i < playedChaptersKillCounts.Length; i++)
        {
            playedChaptersKillCounts[i] = 0;
        }

        // Reset chapters
        completedChaptersList.Clear();
        playedChaptersKillCounts = new int[6];
        currentChapter = null;

        // Teleport player to starting position
        player.ResetPosition();

        runPlayTime = 0;
        runTotalTime = 0;
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

            float levelOn100Ratio = Mathf.Clamp(level / 100.0f, 0, 1);
            float nextLevelFactor = xpNeededForNextLevelMinFactor + (xpNeededForNextLevelMaxFactor - xpNeededForNextLevelMinFactor) * xpNeededForEachLevelCurve.Evaluate(levelOn100Ratio);
            nextLevelXp *= nextLevelFactor;
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

    public void IncreaseKillCount(int kills)
    {
        int chapterCount = GetChapterCount();
        playedChaptersKillCounts[chapterCount - 1] += kills;
        SetEatenCount(playedChaptersKillCounts[chapterCount - 1]);
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
        if (IsCurrentRunWon())
        {
            // Game is won!
            GameManager.instance.RegisterWin();
            CharacterManager.instance.WonTheGameWithCharacter(currentPlayedCharacter);
        }

        // Compute actual score
        int currentScore = 0;
        foreach (int killCount in playedChaptersKillCounts)
        {
            currentScore += killCount;
        }
        GameManager.instance.RegisterScore(currentScore);

        // Collect currency for good
        GameManager.instance.ChangeAvailableCurrency(currentCollectedCurrency);
        int currencyCollectedInThisRun = Mathf.RoundToInt(currentCollectedCurrency);
        currentCollectedCurrency = 0;

        // Maybe unlock some characters if conditions are met
        List<Achievement> unlockedAchievements = AchievementManager.instance.GetUnlockedAchievementsForCurrentRun();

        // Add the current chapter to the list (even if current chapter was not completed)
        List<Chapter> chaptersPlayed = new List<Chapter>(completedChaptersList);
        if (currentChapter != null && (chaptersPlayed.Count == 0 || (chaptersPlayed.Count > 0 && !chaptersPlayed[chaptersPlayed.Count - 1].Equals(currentChapter))))
        {
            chaptersPlayed.Add(currentChapter);
        }

        // Game must be saved
        SaveDataManager.instance.isSaveDataDirty = true;

        if (logsVerboseLevel == VerboseLevel.MAXIMAL)
        {
            Debug.Log($"Run - Play time is {runPlayTime.ToString("0.00")} seconds. Total time (pause included) is {runTotalTime.ToString("0.00")} seconds");
            Debug.Log("Run - Show scores. " + chapterRemainingTime.ToString("0.00") + " seconds left on the timer");
        }

        int playedTimeThisChapter = Mathf.RoundToInt(currentChapter.chapterData.chapterLengthInSeconds - chapterRemainingTime);


        // Display the score screen
        ScoreManager.instance.ShowScores(chaptersPlayed, playedChaptersKillCounts, currentPlayedCharacter, ownedItems, unlockedAchievements, playedTimeThisChapter, currencyCollectedInThisRun);
    }

    public void EndChapter()
    {
        GameManager.instance.isGameRunning = false;

        // Add current played chapter to the list
        completedChaptersList.Add(currentChapter);

        // Tell ChapterManager to register that the current character completed that chapter
        ChapterManager.instance.CompleteChapter(currentChapter, currentPlayedCharacter);

        // Stop time
        GameManager.instance.SetTimeScale(0);

        if (completedChaptersList.Count >= maxChaptersInARun)
        {
            // This was the final chapter
            // Game must end now and display Score
            ComputeScore();
        }
        else
        {
            // This was a chapter before 5th, so we offer a choice for the next chapter
            ChapterManager.instance.ShowChapterSelection(GetChapterCount());
        }
    }

    public void StartChapter(Chapter chapter)
    {
        // Set timer
        chapterRemainingTime = chapter.chapterData.chapterLengthInSeconds;

        // Remove enemies on screen
        EnemiesManager.instance.ClearAllEnemies();

        currentChapter = chapter;

        StartCoroutine(StartChapterAsync());
    }

    public int GetChapterCount()
    {
        return completedChaptersList.Count + 1;
    }

    private IEnumerator StartChapterAsync()
    {
        int chapterCount = GetChapterCount();

        // Show chapter start screen
        ChapterManager.instance.ShowChapterStartScreen(chapterCount, currentChapter);

        // Clear all collectible
        CollectiblesManager.instance.ClearCollectibles();

        // Teleport player to starting position
        player.ResetPosition();

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

        // Set current wave
        currentWaveIndex = 0;
        Wave currentWave = GetCurrentWave();
        waveRemainingTime = currentWave.duration;
        EnemiesManager.instance.InitializeWave(GetCurrentWave());

        // If character is ghost in that chapter, force it to ghost sprite
        player.ForceGhost(currentChapter.chapterData.characterStyleChange == CharacterStyle.GHOST);

        yield return new WaitForSecondsRealtime(2.9f);

        GameManager.instance.SetTimeScale(1);

        // Reset kill count
        SetEatenCount(0);

        // Play level music
        MusicManager.instance.PlayLevelMusic();

        // Fade out chapter start screen
        float fadeOutDelay = 0.4f;
        ChapterManager.instance.FadeOutChapterStartScreen(fadeOutDelay);

        // Wait
        yield return new WaitForSecondsRealtime(0.1f);

        // Show Game UI
        UIManager.instance.ShowGameUI();

        // Start game!
        chapterRemainingTime = currentChapter.chapterData.chapterLengthInSeconds;
        GameManager.instance.hasGameStarted = true;
        GameManager.instance.isGameRunning = true;
        ChapterManager.instance.chapterChoiceIsVisible = false;
    }

    public void EndRun()
    {
        GameManager.instance.isGameRunning = false;

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
        weaponGo.name = $"{weaponData.weaponName}_{weaponItem.activeWeaponsList.Count + 1}";

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

    public void PickRunItem(RunItemData pickedItemData)
    {
        string log = "Run - Pick a Run item: " + pickedItemData.itemName;

        // Get Item Type (weapon, stat boost, or consumable)
        RunItemType pickedItemType = pickedItemData.GetItemType();

        bool itemIsNew = true;
        int level = 0;
        RunItemInfo pickedItemInfo = null;

        switch (pickedItemType)
        {
            case RunItemType.CONSUMABLE:
                // Resolve this consumable once
                log += " > item is a consumable\n";
                player.ResolvePickedConsumableItem(pickedItemData as RunConsumableItemData);
                break;
            case RunItemType.STAT_BONUS:
                // Add this item to the list or upgrade the corresponding item level
                log += " > item is a stat bonus";

                // Check if we already have this item
                foreach (RunItemInfo ownedItem in ownedItems)
                {
                    RunItemData itemData = ownedItem.GetRunItemData();
                    if (itemData.Equals(pickedItemData))
                    {
                        // We already own such an item
                        itemIsNew = false;
                        pickedItemInfo = ownedItem;
                        log += " (already owned, level " + ownedItem.level + ")";
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
                    newStatItemInfo.itemName = pickedItemData.itemName;
                    newStatItemInfo.level = 1;
                    newStatItemInfo.itemData = pickedItemData as RunStatItemData;

                    pickedItemInfo = newStatItemInfo;

                    ownedItems.Add(pickedItemInfo);
                }

                // resolve the item picked (according to its current level)
                int levelIndex = pickedItemInfo.level - 1;
                levelIndex = Mathf.Clamp(levelIndex, 0, pickedItemInfo.GetRunItemData().GetMaxLevelCount());
                RunStatItemLevel levelUpgrades = (pickedItemData as RunStatItemData).statBoostLevels[levelIndex];
                log += " Improve stats: " + levelUpgrades.statUpgrades.ToString();
                player.ResolvePickedStatItemLevel(levelUpgrades);

                break;
            case RunItemType.WEAPON:
                // Add this weapon to the list or upgrade the corresponding weapon level
                // Eventually spawn new weapon(s) if needed
                log += " > item is a weapon";

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
                        log += " (already owned, level " + ownedWeapon.level + ")";

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
                    newWeaponInfo.itemName = pickedItemData.itemName;
                    newWeaponInfo.killCount = 0;
                    newWeaponInfo.level = 1;
                    newWeaponInfo.weaponItemData = pickedItemData as RunWeaponItemData;

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
                        log += " -> Spawn new weapon!";
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
                            log += " -> Spawn new weapon!";
                            SpawnWeapon(pickedItemInfo as RunWeaponInfo);
                        }
                    }
                }

                break;
        }

        if (logsVerboseLevel == VerboseLevel.MAXIMAL)
        {
            Debug.Log(log);
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

        if (logsVerboseLevel == VerboseLevel.MAXIMAL)
        {
            Debug.Log("Run - Level Up!");

            string weaponLog = "Summary of kill count per tongue:";
            foreach (RunWeaponInfo weaponInfo in GetOwnedWeapons())
            {
                weaponLog += "\n -> " + weaponInfo.weaponItemData.weaponData.weaponName + " ate " + weaponInfo.killCount + " bugs";
            }
            Debug.Log(weaponLog);
        }

        string log = "New level is " + level + ". Selection of items is:";

        levelUpParticleSystem.Play();

        // Pick possible items from a pool
        selectionOfPossibleRunItemsList = RunItemManager.instance.PickARandomSelectionOfRunItems(3);

        // Find next levels for each of these items
        List<int> itemLevels = new List<int>();
        foreach (RunItemData item in selectionOfPossibleRunItemsList)
        {
            int itemLevel = GetLevelForItem(item);
            itemLevels.Add(itemLevel);
            log += "\n-> " + item.itemName + " - next LVL: " + (itemLevel + 1).ToString();
        }

        // Show item selection
        ShowLevelUpItemSelection(selectionOfPossibleRunItemsList, itemLevels);

        if (logsVerboseLevel == VerboseLevel.MAXIMAL)
        {
            Debug.Log(log);
        }
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

                levelUpChoicesDescriptions[index].text = description.Replace("\\n", "\n");
            }

            levelUpChoicesIcons[index].sprite = item.icon;

            index++;
        }
    }

    public void CollectCollectible(string collectibleName)
    {
        if (collectibleName.Contains("Currency"))
        {
            if (int.TryParse(collectibleName.Split("+")[1], out int currency))
            {
                int collectedCurrency = Mathf.RoundToInt(currency * (1 + player.currencyBoost));
                currentCollectedCurrency += collectedCurrency;
                UpdateInGameCurrencyText(currentCollectedCurrency);

                if (logsVerboseLevel == VerboseLevel.MAXIMAL)
                {
                    Debug.Log("Run - Collected " + collectedCurrency + " Froins. Current collected currency is: " + currentCollectedCurrency + " Froins");
                }
            }
        }
        else if (collectibleName.Contains("LevelUp"))
        {
            if (logsVerboseLevel == VerboseLevel.MAXIMAL)
            {
                Debug.Log("Run - Collected a Level Up");
            }

            IncreaseXP(nextLevelXp - xp);
        }
        else if (collectibleName.Contains("XP"))
        {
            if (float.TryParse(collectibleName.Split("+")[1], out float xpBonus))
            {
                if (logsVerboseLevel == VerboseLevel.MAXIMAL)
                {
                    Debug.Log("Run - Collected a XP Bonus: +" + xpBonus + "XP");
                }

                IncreaseXP(xpBonus);
            }
        }
        else if (collectibleName.Contains("HP"))
        {
            if (int.TryParse(collectibleName.Split("+")[1], out int hpBonus))
            {
                if (logsVerboseLevel == VerboseLevel.MAXIMAL)
                {
                    Debug.Log("Run - Collected some health. Healing: +" + hpBonus + "HP");
                }
                player.Heal(hpBonus);
            }
        }
    }
}
