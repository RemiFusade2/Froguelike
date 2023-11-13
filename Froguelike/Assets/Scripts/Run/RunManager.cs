using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
    public Transform tongueSlotsParent;
    public Transform statItemSlotsParent;
    public GameObject slotPrefab;
    [Space]
    public Transform compassParent;
    public GameObject compassArrowPrefab;

    [Header("References - UI Level Up")]
    public GameObject levelUpPanel;
    public List<GameObject> levelUpChoicesPanels;
    public List<TextMeshProUGUI> levelUpChoicesTitles;
    public List<TextMeshProUGUI> levelUpChoicesLevels;
    public List<TextMeshProUGUI> levelUpChoicesDescriptions;
    public List<Image> levelUpChoicesIcons;
    [Space]
    public Color defaultUIColor;
    public Color newItemColor;
    public Color maxLevelColor;
    [Space]
    public GameObject rerollPostit;
    public Button rerollButton;
    public TextMeshProUGUI rerollCount;
    public GameObject banishPostit;
    public Button banishButton;
    public TextMeshProUGUI banishCount;
    public GameObject skipPostit;
    public Button skipButton;
    public TextMeshProUGUI skipCount;

    [Header("Settings - In game UI")]
    public Color defaultTextColor;
    public Color highlightedTextColor;

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
    public float nextLevelXp;
    [Space]
    public bool rerollsAvailable;
    public bool banishesAvailable;
    public bool skipsAvailable;

    [Header("Runtime - Compass")]
    public List<CompassArrowBehaviour> compassArrowsList;

    private float runPlayTime;
    private float runTotalTime; // pause time included

    private Coroutine setCurrencyTextMeshColorCoroutine;
    private Coroutine setExtraLivesTextMeshColorCoroutine;

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

            // Spawn bounties
            int playedTimeThisChapter = Mathf.RoundToInt(currentChapter.chapterData.chapterLengthInSeconds - chapterRemainingTime);
            float delayDuringWhichWeTryToSpawnBounties = 5; // For 5 seconds after bounty spawn time, we try to spawn bounty
            for (int i = 0; i < currentChapter.chapterData.bountyBugs.Count; i++)
            {
                BountyBug bounty = currentChapter.chapterData.bountyBugs[i];
                if (playedTimeThisChapter >= bounty.spawnTime && playedTimeThisChapter <= bounty.spawnTime + delayDuringWhichWeTryToSpawnBounties)
                {
                    EnemiesManager.instance.TrySpawnBounty(i, bounty);
                }
            }

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

    #region Accessors

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

    #endregion

    public void StartNewRun(PlayableCharacter character)
    {
        if (logsVerboseLevel == VerboseLevel.MAXIMAL)
        {
            Debug.Log("Run Manager - Start a new Run with character: " + character.characterID);
        }

        // Stop playing any looped sound
        SoundManager.instance.StopAllLoops();

        // Setup the player controller using the player data that we have
        currentPlayedCharacter = character;
        player.InitializeCharacter(character);

        InitializeNewRun();

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

    private void UpdateInGameCurrencyText(long currencyValue, bool highlight)
    {
        currencyText.text = Tools.FormatCurrency(currencyValue, DataManager.instance.currencySymbol);
        if (highlight)
        {
            currencyText.color = highlightedTextColor;
            if (setCurrencyTextMeshColorCoroutine != null)
            {
                StopCoroutine(setCurrencyTextMeshColorCoroutine);
            }
            setCurrencyTextMeshColorCoroutine = StartCoroutine(SetTextMeshColor(currencyText, defaultTextColor, 1.0f));
        }
    }

    private IEnumerator SetTextMeshColor(TextMeshProUGUI textMesh, Color color, float delay)
    {
        yield return new WaitForSeconds(delay);
        textMesh.color = color;
    }

    public void IncreaseCollectedCurrency(long currencyValue)
    {
        currentCollectedCurrency += currencyValue;
        UpdateInGameCurrencyText(currentCollectedCurrency, true);
    }

    public void InitializeNewRun()
    {
        currentCollectedCurrency = 0;
        UpdateInGameCurrencyText(currentCollectedCurrency, false);

        levelUpChoiceIsVisible = false;

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

        // Reset Items
        ClearAllItems();

        // Reset item slots.
        InitializeInRunItemSlots();

        // Reset kill counts
        for (int i = 0; i < playedChaptersKillCounts.Length; i++)
        {
            playedChaptersKillCounts[i] = 0;
        }

        // Reroll, Banish, Skip
        rerollsAvailable = false;
        if (player.rerolls > 0)
        {
            rerollsAvailable = true;
        }
        banishesAvailable = false;
        if (player.banishs > 0)
        {
            banishesAvailable = true;
        }
        skipsAvailable = false;
        if (player.skips > 0)
        {
            skipsAvailable = true;
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

    public void SetExtraLives(int reviveCount, bool highlight)
    {
        extraLivesCountText.text = extraLivesPrefix + reviveCount.ToString();
        if (highlight)
        {
            extraLivesCountText.color = highlightedTextColor;
            if (setExtraLivesTextMeshColorCoroutine != null)
            {
                StopCoroutine(setExtraLivesTextMeshColorCoroutine);
            }
            setExtraLivesTextMeshColorCoroutine = StartCoroutine(SetTextMeshColor(extraLivesCountText, defaultTextColor, 1.0f));
        }
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
        SetExtraLives(player.revivals, true);
    }

    public void EatFly(float experiencePoints)
    {
        // Increase kill count by 1 and display it
        IncreaseKillCount(1);

        IncreaseXP(experiencePoints * (1 + player.experienceBoost));

        // Also collect some currency when eating a bug
        Vector2 currencyProbabilityMinMax = new Vector2(player.currencyBoost, (1 + player.currencyBoost));
        float currencyProba = Random.Range(currencyProbabilityMinMax.x, currencyProbabilityMinMax.y);
        float currencyAmount = Mathf.Floor(currencyProba) + ((Random.Range(Mathf.Floor(currencyProba), Mathf.Ceil(currencyProba)) < currencyProba) ? 1 : 0);
        long currencyAmountLong = (long)Mathf.RoundToInt(currencyAmount);
        if (currencyAmountLong > 0)
        {
            IncreaseCollectedCurrency(currencyAmountLong);
        }
    }

    private void ComputeScore()
    {
        // Stop time & music
        GameManager.instance.SetTimeScale(0);
        MusicManager.instance.PauseMusic();
        SoundManager.instance.PauseInGameSounds();

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

        // Maybe unlock some achievements if conditions are met
        List<Achievement> unlockedAchievements = AchievementManager.instance.GetUnlockedAchievementsForCurrentRun(true, false);

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

        // Audio
        player.StopTakeDamageEffect();
        SoundManager.instance.StopAllLoops();

        // Stop time
        GameManager.instance.SetTimeScale(0);

        // Reset all tongues
        foreach (RunWeaponInfo weaponInfo in GetOwnedWeapons())
        {
            foreach (GameObject activeWeapon in weaponInfo.activeWeaponsList)
            {
                activeWeapon.GetComponent<WeaponBehaviour>().ResetTongue();
            }
        }

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

        /*
        // Remove collectibles on screen
        CollectiblesManager.instance.ClearCollectibles();*/

        // Register a new attempt if this is the first chapter of that run
        if (GetChapterCount() == 1)
        {
            GameManager.instance.RegisterANewAttempt();
        }

        currentChapter = chapter;
        StartCoroutine(StartChapterAsync());
    }

    public int GetChapterCount()
    {
        return completedChaptersList.Count + 1;
    }

    private void ClearCompassArrows()
    {
        foreach (Transform compassChild in compassParent)
        {
            Destroy(compassChild.gameObject, 0.01f);
        }
        compassArrowsList.Clear();
    }

    public CompassArrowBehaviour GetCompassArrowForCollectible(FixedCollectible collectible)
    {
        return compassArrowsList.FirstOrDefault(x => x.collectibleTileCoordinates.Equals(collectible.tileCoordinates));
    }

    public void RemoveCompassArrowForCollectible(FixedCollectible collectible)
    {
        CompassArrowBehaviour arrow = GetCompassArrowForCollectible(collectible);
        if (arrow != null)
        {
            compassArrowsList.Remove(arrow);
            Destroy(arrow.gameObject);
        }
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
        player.Heal(player.maxHealth);

        // Clear old map
        MapBehaviour.instance.ClearMap();

        // Prepare compass arrows
        ClearCompassArrows();
        List<FixedCollectible> fixedCollectiblesList = currentChapter.chapterData.specialCollectiblesOnTheMap;
        foreach (FixedCollectible collectible in fixedCollectiblesList)
        {
            GameObject arrow = Instantiate(compassArrowPrefab, compassParent);
            arrow.GetComponent<CompassArrowBehaviour>().SetCollectibleTileCoordinates(collectible.tileCoordinates);
            compassArrowsList.Add(arrow.GetComponent<CompassArrowBehaviour>());
        }

        // Create starting tiles
        UpdateMap();

        // Reinitialize all weapons
        foreach (Transform tongue in player.weaponsParent)
        {
            if (tongue.GetComponent<WeaponBehaviour>() != null)
            {
                tongue.GetComponent<WeaponBehaviour>().ResetTongue();
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
            chapterRemainingTime = 0; // float.MaxValue;
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
            weaponGo.GetComponent<WeaponBehaviour>().CopyWeaponStats(weaponItem.activeWeaponsList);
        }
        else
        {
            // weapon is the first, we should initialize it from the data we have
            weaponGo.GetComponent<WeaponBehaviour>().Initialize(weaponData, weaponItem.activeWeaponsList);
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

                    // Show the new item in the in run UI.
                    UpdateInRunItemSlots(newStatItemInfo.GetRunItemData());
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

                    // Show the new item in the in run UI.
                    UpdateInRunItemSlots(newWeaponInfo.GetRunItemData());
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

    #region Level Up

    private void CloseLevelUp()
    {
        HideLevelUpItemSelection();
        levelUpChoiceIsVisible = false;
        GameManager.instance.SetTimeScale(1);
        IncreaseXP(0);
    }

    public void ChooseLevelUpChoice(int index)
    {
        RunItemData pickedItem = selectionOfPossibleRunItemsList[index];
        PickRunItem(pickedItem);

        CloseLevelUp();
    }

    private void RandomLevelUpItemSelection()
    {
        string log = "Selection of items is:";

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

    public void SkipLevelUpItemSelection()
    {
        if (player.skips > 0)
        {
            player.skips--;
            UpdateRerollBanishSkipPostIts();
            if (logsVerboseLevel == VerboseLevel.MAXIMAL)
            {
                Debug.Log("Use Skip on level up item selection");
            }
            CloseLevelUp();
        }
    }

    public void RerollLevelUpItemSelection()
    {
        if (player.rerolls > 0)
        {
            player.rerolls--;
            UpdateRerollBanishSkipPostIts();
            if (logsVerboseLevel == VerboseLevel.MAXIMAL)
            {
                Debug.Log("Use reroll on level up item selection");
            }
            RandomLevelUpItemSelection();
        }
    }

    public void LevelUP()
    {
        levelUpChoiceIsVisible = true;
        level++;
        GameManager.instance.SetTimeScale(0);
        levelUpParticleSystem.Play();
        SoundManager.instance.PlayLevelUpSound();

        if (logsVerboseLevel == VerboseLevel.MAXIMAL)
        {
            Debug.Log("Run - Level Up!");

            string weaponLog = "Summary of kill count per tongue:";
            foreach (RunWeaponInfo weaponInfo in GetOwnedWeapons())
            {
                weaponLog += "\n -> " + weaponInfo.weaponItemData.weaponData.weaponName + " ate " + weaponInfo.killCount + " bugs";
            }
            Debug.Log(weaponLog);
            Debug.Log("New level is " + level);
        }

        selectionOfPossibleRunItemsList.Clear();
        RandomLevelUpItemSelection();
    }

    private void UpdateRerollBanishSkipPostIts()
    {
        rerollPostit.SetActive(rerollsAvailable);
        rerollButton.interactable = (player.rerolls > 0);
        rerollCount.SetText($"x{player.rerolls}");
        rerollPostit.GetComponent<CanvasGroup>().blocksRaycasts = (player.rerolls > 0);
        

        banishPostit.SetActive(banishesAvailable);
        banishButton.interactable = (player.banishs > 0);
        banishCount.SetText($"x{player.banishs}");
        banishPostit.GetComponent<CanvasGroup>().blocksRaycasts = (player.banishs > 0);

        skipPostit.SetActive(skipsAvailable);
        skipButton.interactable = (player.skips > 0);
        skipCount.SetText($"x{player.skips}");
        skipPostit.GetComponent<CanvasGroup>().blocksRaycasts = (player.skips > 0);
    }

    public void ShowLevelUpItemSelection(List<RunItemData> possibleItems, List<int> itemLevels)
    {
        levelUpPanel.GetComponent<CanvasGroup>().interactable = true;
        EventSystem.current.SetSelectedGameObject(levelUpChoicesPanels[0]);

        UpdateRerollBanishSkipPostIts();

        // Audio
        SoundManager.instance.PlaySlideBookSound();
        SoundManager.instance.PauseInGameSounds();

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
                    RunWeaponItemLevel weaponLevel = (item as RunWeaponItemData).weaponBoostLevels[level - 1];
                    if (string.IsNullOrEmpty(weaponLevel.description))
                    {
                        description = weaponLevel.weaponStatUpgrades.GetDescription();
                    }
                    else
                    {
                        description = weaponLevel.description;
                    }
                }
                else if (itemType == RunItemType.STAT_BONUS && level < item.GetMaxLevelCount())
                {
                    // Stat bonus item that we already have
                    description = (item as RunStatItemData).statBoostLevels[level].description;
                }

                levelUpChoicesDescriptions[index].text = description.Replace("\\n", "\n");
            }

            levelUpChoicesIcons[index].sprite = item.icon;
            levelUpChoicesIcons[index].color = (item.icon == null) ? new Color(0, 0, 0, 0) : Color.white;

            index++;
        }
    }

    #endregion

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
        levelUpPanel.GetComponent<CanvasGroup>().interactable = false;
        UIManager.instance.levelUpPanelAnimator.SetBool("Visible", false);
        SoundManager.instance.PlaySlideBookSound();
    }

    private void InitializeInRunItemSlots()
    {
        // Remove previous slots.
        DestroyAndDeactivateChildren(tongueSlotsParent);
        DestroyAndDeactivateChildren(statItemSlotsParent);

        // Show as manys slots as the player has.
        for (int slots = 0; slots < player.weaponSlotsCount; slots++) AddNewRunItemSlot(tongueSlotsParent);
        for (int slots = 0; slots < player.statItemSlotsCount; slots++) AddNewRunItemSlot(statItemSlotsParent);
    }

    private void DestroyAndDeactivateChildren(Transform parent)
    {
        foreach (Transform child in parent)
        {
            child.gameObject.SetActive(false);
            Destroy(child.gameObject);
        }
    }

    private void UpdateInRunItemSlots(RunItemData newItem)
    {
        // Pick the next empty slot.
        SpriteRenderer nextFreeIconSlot = null;
        Transform parent = null;
        switch (newItem.GetItemType())
        {
            case RunItemType.WEAPON:
                parent = tongueSlotsParent;
                break;

            case RunItemType.STAT_BONUS:
                parent = statItemSlotsParent;
                break;

            // If an item is not a tongue or a stat item, it should not be displayed.
            default:
                return;
        }

        // Look for the first empty slot in the right parent.
        foreach (Transform slot in parent)
        {
            if (!slot.gameObject.activeSelf) continue;

            GameObject icon = slot.transform.Find("Icon").gameObject;
            if (!icon.activeSelf)
            {
                nextFreeIconSlot = icon.GetComponent<SpriteRenderer>();
                break;
            }
        }

        // If no empty slot was found, create a new one.
        if (nextFreeIconSlot == null)
        {
            nextFreeIconSlot = AddNewRunItemSlot(parent).Find("Icon").GetComponent<SpriteRenderer>();
        }

        // Set the icon.
        nextFreeIconSlot.gameObject.SetActive(true);
        nextFreeIconSlot.sprite = newItem.icon;
    }

    private Transform AddNewRunItemSlot(Transform parent)
    {
        return Instantiate(slotPrefab, parent).transform;
    }

    #endregion

    #region Collect Collectible

    public void CollectCollectible(CollectibleType collectibleType, float collectibleValue)
    {
        switch (collectibleType)
        {
            case CollectibleType.FROINS:
                int collectedCurrency = Mathf.RoundToInt(collectibleValue * (1 + player.currencyBoost));
                IncreaseCollectedCurrency(collectedCurrency);
                SoundManager.instance.PlayPickUpFroinsSound();

                if (logsVerboseLevel == VerboseLevel.MAXIMAL)
                {
                    Debug.Log("Run - Collected " + collectedCurrency + " Froins. Current collected currency is: " + currentCollectedCurrency + " Froins");
                }
                break;
            case CollectibleType.LEVEL_UP:
                if (logsVerboseLevel == VerboseLevel.MAXIMAL)
                {
                    Debug.Log("Run - Collected a Level Up");
                }
                IncreaseXP(nextLevelXp - xp);
                break;
            case CollectibleType.XP_BONUS:
                if (logsVerboseLevel == VerboseLevel.MAXIMAL)
                {
                    Debug.Log("Run - Collected a XP Bonus: +" + collectibleValue + "XP");
                }
                IncreaseXP(collectibleValue);
                break;
            case CollectibleType.HEALTH:
                if (logsVerboseLevel == VerboseLevel.MAXIMAL)
                {
                    Debug.Log("Run - Collected some health. Healing: +" + collectibleValue + "HP");
                }
                SoundManager.instance.PlayHealSound();
                player.Heal(collectibleValue);
                break;
            case CollectibleType.POWERUP_FREEZE_ALL:
                EnemiesManager.instance.ApplyGlobalFreezeEffect(DataManager.instance.powerUpFreezeDuration);
                SoundManager.instance.PlayFreezeAllSound();
                if (logsVerboseLevel == VerboseLevel.MAXIMAL)
                {
                    Debug.Log("Run - Collected Freeze power-up");
                }
                break;
            case CollectibleType.POWERUP_POISON_ALL:
                EnemiesManager.instance.ApplyGlobalPoisonEffect(DataManager.instance.powerUpPoisonDuration);
                SoundManager.instance.PlayFreezeAllSound();
                if (logsVerboseLevel == VerboseLevel.MAXIMAL)
                {
                    Debug.Log("Run - Collected Poison power-up");
                }
                break;
            case CollectibleType.POWERUP_CURSE_ALL:
                EnemiesManager.instance.ApplyGlobalCurseEffect(DataManager.instance.powerUpCurseDuration);
                SoundManager.instance.PlayFreezeAllSound();
                if (logsVerboseLevel == VerboseLevel.MAXIMAL)
                {
                    Debug.Log("Run - Collected Curse power-up");
                }
                break;
            default:
                break;
        }
    }

    #endregion
}
