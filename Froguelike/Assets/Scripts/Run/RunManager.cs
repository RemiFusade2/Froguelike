using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Timeline;
using UnityEngine.UI;
using UnityEngine.XR;

[System.Flags]
public enum GameMode
{
    NONE = 0,
    HARD = 1,
    HARDER = 2
}

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
    public ParticleSystem confettiParticleSystem;

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
    public TextMeshProUGUI conditionCountText;
    public Image conditionCountIcon;
    [Space]
    public Transform tongueSlotsBackgroundsParent;
    public Transform tongueSlotsIconsParent;
    public Transform statItemSlotsBackgroundsParent;
    public Transform statItemSlotsIconsParent;
    public GameObject slotBackgroundPrefab;
    public GameObject slotIconPrefab;
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
    public List<Animator> levelUpChoicesButtonAnimators;
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

    [Header("References - UI Found Fixed Collectible")]
    public GameObject fixedCollectibleFoundPanel;
    public TextMeshProUGUI fixedCollectibleTitleText;
    public TextMeshProUGUI fixedCollectibleNameText;
    public TextMeshProUGUI fixedCollectibleBonusText;
    public Image fixedCollectibleItemIcon;
    public Image fixedCollectibleFriendIcon;
    public Image fixedCollectibleHatIcon;
    [Space]
    public GameObject fixedCollectibleTwoButtonsPanel;
    public Button fixedCollectibleTwoButtonsAcceptButton;
    public TextMeshProUGUI fixedCollectibleTwoButtonsAcceptText;
    public TextMeshProUGUI fixedCollectibleTwoButtonsRefuseText;
    [Space]
    public GameObject fixedCollectibleForceAcceptPanel;
    public TextMeshProUGUI fixedCollectibleForceAcceptDescriptionText;
    public Button fixedCollectibleForceAcceptButton;
    public TextMeshProUGUI fixedCollectibleForceAcceptText;
    [Space]
    public GameObject fixedCollectibleForceRefusePanel;
    public TextMeshProUGUI fixedCollectibleForceRefuseDescriptionText;
    public Button fixedCollectibleForceRefuseButton;
    public TextMeshProUGUI fixedCollectibleForceRefuseText;

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

    [Header("Settings - End of chapter")]
    public float delayAfterEndOfChapter = 5;
    public float enemiesSpeedFactorAfterEndOfChapter = 4;
    public Color timerTextColorDuringChapter;
    public Color timerTextColorAfterEndOfChapter;

    [Header("Runtime - XP")]
    public float xp;
    public int level;

    [Header("Runtime - Current played game mode")]
    public GameMode playedGameModes;
    [Space]
    public float gameModeBugHPMultiplier;
    public float gameModeBugSpeedMultiplier;
    public float gameModeBugDamageMultiplier;
    public float gameModeFroinsMultiplier;

    [Header("Runtime - Current played character")]
    public PlayableCharacter currentPlayedCharacter;

    [Header("Runtime - Current played chapter")]
    public List<Chapter> completedChaptersList;
    public int[] playedChaptersKillCounts;
    public int[] playedChaptersBountyEatCounts;
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
    [Space]
    public List<RunItemData> banishedItemsList;
    public bool isUsingBanishForCurrentItemSelection;

    [Header("Runtime - Fixed collectible")]
    public bool fixedCollectibleFoundPanelIsVisible;
    public FixedCollectible fixedCollectibleInfo;

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
        HideCollectFixedCollectiblePanel();
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
                currentWaveIndex = (currentWaveIndex + 1) % currentChapter.chapterData.wavesList.Count;
                EnemiesManager.instance.InitializeWave(GetCurrentWave());
                waveRemainingTime = GetCurrentWave().duration;
            }

            // Update condition counter
            UpdateNextChapterConditionCount();
        }
    }

    #endregion

    #region Accessors

    public bool IsCurrentRunWon()
    {
        return completedChaptersList.Count >= maxChaptersInARun;
    }

    public WaveData GetCurrentWave()
    {
        return currentChapter.chapterData.wavesList[currentWaveIndex];
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

    public bool IsChapterTimeOver()
    {
        return chapterRemainingTime <= 0;
    }

    #endregion

    private void SetupGameModesMultipliers(GameMode gameModes)
    {
        gameModeBugHPMultiplier = 1;
        gameModeBugSpeedMultiplier = 1;
        gameModeBugDamageMultiplier = 1;
        gameModeFroinsMultiplier = 1;
        if ((gameModes & GameMode.HARD) == GameMode.HARD)
        {
            // Hard mode activated
            gameModeBugHPMultiplier += 0.5f;
            gameModeBugSpeedMultiplier += 0.5f;
            gameModeBugDamageMultiplier += 0.5f;
            gameModeFroinsMultiplier += 0.5f;
        }
        if ((gameModes & GameMode.HARDER) == GameMode.HARDER)
        {
            // Harder mode activated
            gameModeBugHPMultiplier += 1;
            gameModeBugSpeedMultiplier += 1;
            gameModeBugDamageMultiplier += 1;
            gameModeFroinsMultiplier += 1;
        }
    }

    public void StartNewRun(PlayableCharacter character, GameMode gameModes, Chapter startingChapter = null)
    {
        if (logsVerboseLevel == VerboseLevel.MAXIMAL)
        {
            Debug.Log("Run Manager - Start a new Run with character: " + character.characterID);
        }

        // Remove banished items
        banishedItemsList.Clear();

        // Stop playing any looped sound
        SoundManager.instance.StopAllLoops();

        // Setup the player controller using the player data that we have
        currentPlayedCharacter = character;
        player.InitializeCharacter(character);

        // Setup game mode multipliers
        this.playedGameModes = gameModes;
        SetupGameModesMultipliers(gameModes);

        InitializeNewRun();

        // Pick all the items this character starts with
        foreach (RunItemData itemData in character.characterData.startingItems)
        {
            PickRunItem(itemData);
        }

        // Reset Chapters Weights
        ChapterManager.instance.ResetChaptersWeights();

        if (startingChapter == null)
        {
            // Show a selection of Chapters to pick from for the first chapter of the Run
            ChapterManager.instance.ShowChapterSelection(0);
        }
        else
        {
            // Start the first chapter immediately if it was already set
            ChapterManager.instance.StartChapter(startingChapter);
        }
    }

    private void UpdateInGameCurrencyText(long currencyValue, bool highlight)
    {
        currencyText.text = Tools.FormatCurrency(currencyValue, "");
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
        FriendsManager.instance.ClearAllFriends();
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

        // Reset bounties eaten count.
        for (int i = 0; i < playedChaptersBountyEatCounts.Length; i++)
        {
            playedChaptersBountyEatCounts[i] = 0;
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
        playedChaptersBountyEatCounts = new int[6];
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

    public void IncreaseXP(float moreXP, bool preventXPIncrease = false)
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

            if (!preventXPIncrease)
            {
                float levelOn100Ratio = Mathf.Clamp(level / 100.0f, 0, 1);
                float nextLevelFactor = xpNeededForNextLevelMinFactor + (xpNeededForNextLevelMaxFactor - xpNeededForNextLevelMinFactor) * xpNeededForEachLevelCurve.Evaluate(levelOn100Ratio);
                nextLevelXp *= nextLevelFactor;
            }
        }
    }

    private void SetTimer(float remainingTime)
    {
        System.TimeSpan time = new System.TimeSpan(0, 0, Mathf.RoundToInt(remainingTime));
        if (remainingTime > 0)
        {
            timerText.color = timerTextColorDuringChapter;
        }
        else
        {
            timerText.color = timerTextColorAfterEndOfChapter;
            time = new System.TimeSpan(0, 0, Mathf.RoundToInt(delayAfterEndOfChapter + remainingTime)); // remainingTime is negative
        }
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

    public void SetNextChapterConditionCount(NextChapterConditionCount nextChapterConditionCount)
    {
        conditionCountIcon.sprite = DataManager.instance.GetNextChapterConditionCountTypeSpriteFromType(nextChapterConditionCount.countType);
        conditionCountIcon.SetNativeSize();

        Vector2 noteSize = new Vector2(0, conditionCountIcon.transform.parent.GetComponent<RectTransform>().sizeDelta.y);
        Vector2 textContainerSize = new Vector2(0, 12);

        string countText = "";
        switch (nextChapterConditionCount.countType)
        {
            case NextChapterConditionCountType.EatBounties:
                countText = "0/" + nextChapterConditionCount.goal.ToString();
                noteSize.x = 57;
                textContainerSize.x = 35;
                break;
            case NextChapterConditionCountType.HaveFriends:
                countText = FriendsManager.instance.permanentFriendsList.Count().ToString() + "/" + nextChapterConditionCount.goal.ToString();
                noteSize.x = 57;
                textContainerSize.x = 35;
                break;
            case NextChapterConditionCountType.DistanceFromSpawn:
                noteSize.x = 110;
                textContainerSize.x = 78;
                break;
            case NextChapterConditionCountType.DistanceFromSpawnInDirection:
                noteSize.x = 65;
                textContainerSize.x = 35;
                break;
            default:
                break;
        }

        conditionCountText.rectTransform.sizeDelta = textContainerSize;
        conditionCountText.transform.parent.GetComponent<RectTransform>().sizeDelta = noteSize;

        conditionCountText.SetText(countText);
    }

    /// <summary>
    /// Update the counter for current chapter condition.
    /// </summary>
    public void UpdateNextChapterConditionCount()
    {
        // TODO where to trigger this? Both when adding firends and eating bounties? Is there a better way?
        // Remi comment: now that it can also show distance, this counter needs to be updated often (every second or every frame)
        NextChapterConditionCountType type = currentChapter.chapterData.nextChapterConditionCount.countType;
        float distanceFromSpawn = Mathf.Clamp(player.transform.position.magnitude/10, 0, currentChapter.chapterData.nextChapterConditionCount.goal);
        string countText = "";

        switch (type)
        {
            case NextChapterConditionCountType.EatBounties:
                countText = playedChaptersBountyEatCounts[GetChapterCount() - 1].ToString() + "/" + currentChapter.chapterData.nextChapterConditionCount.goal.ToString();
                break;
            case NextChapterConditionCountType.HaveFriends:
                countText = FriendsManager.instance.permanentFriendsList.Count().ToString() + "/" + currentChapter.chapterData.nextChapterConditionCount.goal.ToString();
                break;
            case NextChapterConditionCountType.DistanceFromSpawn:
                countText = $"{distanceFromSpawn.ToString("0")}/{currentChapter.chapterData.nextChapterConditionCount.goal}hops";
                break;
            case NextChapterConditionCountType.DistanceFromSpawnInDirection:
                if (distanceFromSpawn >= currentChapter.chapterData.nextChapterConditionCount.goal)
                {
                    // display direction
                    float dotRight = Vector2.Dot(player.transform.position, Vector2.right);
                    float dotUp = Vector2.Dot(player.transform.position, Vector2.up);
                    countText = Mathf.Abs(dotRight) > Mathf.Abs(dotUp) ? (dotRight > 0 ? "EAST" : "WEST") : (dotUp > 0 ? "NORTH" : "SOUTH");
                }
                else
                {
                    countText = "-       ";
                }
                break;
        }

        conditionCountText.SetText(countText);
    }

    public void IncreaseKillCount(int kills)
    {
        int chapterCount = GetChapterCount();
        playedChaptersKillCounts[chapterCount - 1] += kills;
        SetEatenCount(playedChaptersKillCounts[chapterCount - 1]);

        // Update all stats that scale with score
        player.UpdateScalingWithScoreStats();
    }

    public int GetCurrentChapterKillCount()
    {
        int killCount = 0;
        int chapterCount = GetChapterCount();
        if (playedChaptersKillCounts != null && chapterCount <= playedChaptersKillCounts.Count())
        {
            killCount = playedChaptersKillCounts[chapterCount - 1];
        }
        return killCount;
    }

    public int GetTotalKillCount()
    {
        int currentScore = 0;
        foreach (int killCount in playedChaptersKillCounts)
        {
            currentScore += killCount;
        }
        return currentScore;
    }

    public void IncreaseBountyEatCount(int amount)
    {
        int chapterCount = GetChapterCount();
        playedChaptersBountyEatCounts[chapterCount - 1] += amount;
    }

    public int GetCurrentChapterBountyEatCount()
    {
        int bountiesEaten = 0;
        int chapterCount = GetChapterCount();
        if (playedChaptersBountyEatCounts != null && chapterCount <= playedChaptersBountyEatCounts.Count())
        {
            bountiesEaten = playedChaptersBountyEatCounts[chapterCount - 1];
        }
        return bountiesEaten;
    }

    public int GetPreviousChapterBountyEatCount()
    {
        int bountiesEaten = 0;
        int previousChapterCount = GetChapterCount() - 1;
        if (playedChaptersBountyEatCounts != null && previousChapterCount <= playedChaptersBountyEatCounts.Count())
        {
            bountiesEaten = playedChaptersBountyEatCounts[previousChapterCount - 1];
        }
        return bountiesEaten;
    }

    public int GetTotalBountyEatCount()
    {
        int totalBountiesEaten = 0;
        foreach (int bountyEatCount in playedChaptersBountyEatCounts)
        {
            totalBountiesEaten += bountyEatCount;
        }
        return totalBountiesEaten;
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

    public void EatEnemy(float experiencePoints)
    {
        // Increase kill count by 1 and display it
        IncreaseKillCount(1);

        IncreaseXP(experiencePoints * (1 + player.GetExperienceBoost()));

        // Also collect some currency when eating a bug
        float currencyBoost = player.GetCurrencyBoost();
        Vector2 currencyProbabilityMinMax = new Vector2(currencyBoost, (1 + currencyBoost));
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
        MusicManager.instance.StopMusic();
        MusicManager.instance.PlayTitleMusic();
        SoundManager.instance.PauseInGameLoopedSFX();

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

        int playedTimeThisChapter = Mathf.RoundToInt(currentChapter.chapterData.chapterLengthInSeconds - Mathf.Clamp(chapterRemainingTime, 0, currentChapter.chapterData.chapterLengthInSeconds));


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

        // Remove damage texts on screen
        EnemiesManager.instance.ClearAllDamageTexts();

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

        // Remove all temporary friends and stop friends frenzy
        FriendsManager.instance.ClearAllFriends(onlyTemporary: true);
        StopConfettis();

        // Stop God Mode, Freeze/Poison/Curse & Level up/down all, Super goop
        player.ResetGodMode();
        player.StopAndResetAllExplosionEffects(); // global effects explosions
        EnemiesManager.instance.StopAndResetAllGlobalEffects();

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

        // Remove damage texts
        EnemiesManager.instance.ClearAllDamageTexts();

        // Remove collectibles on screen
        CollectiblesManager.instance.ClearAllCollectibles();

        // Register a new attempt if this is the first chapter of that run
        if (GetChapterCount() == 1)
        {
            GameManager.instance.RegisterANewAttempt();

            // Save current selection to Game Manager, for future quick start and stuff

            // EXCEPT IN THE CASE OF TOAD'S CHALLENGE
            if (chapter.chapterID.Equals("[CH_ENDING_TOAD]"))
            {
                GameManager.instance.RemoveSelectedCharacterGameModeAndStartingChapter();
            }
            else
            {
                GameManager.instance.SaveSelectedCharacterGameModeAndStartingChapter(currentPlayedCharacter.characterID, playedGameModes.ToString(), chapter.chapterID);
            }
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
        CollectiblesManager.instance.ClearAllCollectibles();

        // Teleport player to starting position
        player.ResetPosition();

        // Set health to what it's supposed to be
        player.healthBar.SetMaxHealth(player.GetMaxHealth());
        player.healthBar.SetHealthRecovery(player.GetHealthRecovery());
        player.healthBar.ResetHealth();

        // Clear old map
        MapBehaviour.instance.ClearMap();

        // Prepare compass arrows
        ClearCompassArrows();
        List<FixedCollectible> fixedCollectiblesList = currentChapter.chapterData.specialCollectiblesOnTheMap;
        foreach (FixedCollectible collectible in fixedCollectiblesList)
        {
            if (ChapterManager.instance.IsFixedCollectibleShownByCompassInChapter(currentChapter, collectible, out bool collectibleHasBeenFoundOnce))
            {
                GameObject arrow = Instantiate(compassArrowPrefab, compassParent);
                arrow.GetComponent<CompassArrowBehaviour>().SetCollectibleTileCoordinates(collectible.tileCoordinates);
                arrow.GetComponent<CompassArrowBehaviour>().SetCollectibleHasBeenFound(collectibleHasBeenFoundOnce);
                compassArrowsList.Add(arrow.GetComponent<CompassArrowBehaviour>());
            }
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
        WaveData currentWave = GetCurrentWave();
        waveRemainingTime = currentWave.duration;
        EnemiesManager.instance.InitializeWave(GetCurrentWave());

        // If character is ghost in that chapter, force it to ghost sprite
        player.ForceGhost(currentChapter.chapterData.characterStyleChange == CharacterStyle.GHOST);

        yield return new WaitForSecondsRealtime(2.9f);

        GameManager.instance.SetTimeScale(1);

        // Reset kill count
        SetEatenCount(0);

        // Play level music
        MusicManager.instance.PlayRunMusic();

        // Fade out chapter start screen
        float fadeOutDelay = 0.4f;
        ChapterManager.instance.FadeOutChapterStartScreen(fadeOutDelay);

        // Wait
        yield return new WaitForSecondsRealtime(0.1f);

        // Set up condition count UI.
        SetNextChapterConditionCount(currentChapter.chapterData.nextChapterConditionCount);
        UIManager.instance.ShowCountUI(currentChapter.chapterData.nextChapterConditionCount.countType != NextChapterConditionCountType.None);

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

        if (Mathf.RoundToInt(chapterRemainingTime + Time.deltaTime) > 0 && Mathf.RoundToInt(chapterRemainingTime) <= 0)
        {
            SoundManager.instance.PlayChapterEndSound();
        }

        SetTimer(chapterRemainingTime);
        if (chapterRemainingTime < -delayAfterEndOfChapter)
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
                levelIndex = Mathf.Clamp(levelIndex, 0, pickedItemInfo.GetRunItemData().GetMaxLevelCount() - 1);
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

                        log += $" (already owned, level {ownedWeapon.level})"; // show previous level

                        ownedWeapon.level++; // Increase level

                        // We need to upgrade all similar weapons
                        level = Mathf.Clamp(ownedWeapon.level - 2, 0, ownedWeapon.weaponItemData.GetMaxLevelCount() - 1); // This is level boost index
                        foreach (GameObject weaponGo in ownedWeapon.activeWeaponsList)
                        {
                            if (level >= 0 && level < ownedWeapon.weaponItemData.GetMaxLevelCount())
                            {
                                weaponGo.GetComponent<WeaponBehaviour>().LevelUp(ownedWeapon.weaponItemData.weaponBoostLevels[level]);
                            }
                            else if (level >= ownedWeapon.weaponItemData.GetMaxLevelCount())
                            {
                                // Weapon is maxed out. This code should never be called!
                                weaponGo.GetComponent<WeaponBehaviour>().LevelUp(ownedWeapon.weaponItemData.weaponBoostLevels[level - 1]); // repeat last level upgrade
                            }
                        }

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
                    TongueStatValue weaponCountStatValue = newWeaponInfo.weaponItemData.weaponData.weaponBaseStats.GetStatValue(TongueStat.COUNT);
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
                    if (level >= 0 && level < pickedItemData.GetMaxLevelCount())
                    {
                        TongueStatValue weaponCountStatValue = (pickedItemData as RunWeaponItemData).weaponBoostLevels[level].weaponStatUpgrades.GetStatValue(TongueStat.COUNT);
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

    private void CloseLevelUp(bool levelWasSkipped)
    {
        HideLevelUpItemSelection();
        levelUpChoiceIsVisible = false;
        GameManager.instance.SetTimeScale(1);
        IncreaseXP(0, levelWasSkipped);
    }

    public void ChooseLevelUpChoice(int index)
    {
        RunItemData pickedItem = selectionOfPossibleRunItemsList[index];

        if (isUsingBanishForCurrentItemSelection)
        {
            // This item was chosen to be BANISHED
            BanishRunItem(pickedItem);
        }
        else
        {
            // This item was chosen to be PICKED
            PickRunItem(pickedItem);
        }

        CloseLevelUp(false);
    }

    public bool IsRunItemBanished(RunItemData runItemData)
    {
        return banishedItemsList.Contains(runItemData);
    }

    public void BanishRunItem(RunItemData pickedItemData)
    {
        banishedItemsList.Add(pickedItemData);
    }

    private void RandomLevelUpItemSelection()
    {
        string log = "Selection of items is:";

        // Pick possible items from a pool
        selectionOfPossibleRunItemsList = RunItemManager.instance.PickARandomSelectionOfRunItems(3, selectionOfPossibleRunItemsList);

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
            CloseLevelUp(true);
        }
    }

    public void RerollLevelUpItemSelection()
    {
        if (player.rerolls > 0)
        {
            if (RunItemManager.instance.ItemRerollSimilarItemsCount(selectionOfPossibleRunItemsList) > 0)
            {
                // Show warning: some options will be the same, even after reroll
                UIManager.instance.ShowRerollWarningConfirmationPanel(true);
            }
            else
            {
                ConfirmRerollLevelUpItemSelection();
            }
        }
    }

    public void ConfirmRerollLevelUpItemSelection()
    {
        player.rerolls--;
        UpdateRerollBanishSkipPostIts();
        if (logsVerboseLevel == VerboseLevel.MAXIMAL)
        {
            Debug.Log("Use reroll on level up item selection");
        }
        RandomLevelUpItemSelection();
    }

    public void BanishLevelUpItemSelection()
    {
        if (isUsingBanishForCurrentItemSelection)
        {
            // Banish was active, we toggle it back to OFF
            isUsingBanishForCurrentItemSelection = false;

            player.banishs++;

            // Update post its values
            UpdateRerollBanishSkipPostIts();

            foreach (Animator animator in levelUpChoicesButtonAnimators)
            {
                animator.SetBool("Banish", false);
            }

            // Default select first item
            UIManager.instance.SetSelectedButton(levelUpChoicesPanels[0]);

            if (logsVerboseLevel == VerboseLevel.MAXIMAL)
            {
                Debug.Log("Cancel banish on level up item selection");
            }
        }
        else if (player.banishs > 0)
        {
            // Banish was inactive, we toggle it ON
            player.banishs--;

            // Update post its values
            UpdateRerollBanishSkipPostIts();

            // Disable all post-its
            SetRerollBanishSkipPostItsEnable(false);

            // Enable only Banish post-it (in case you want to cancel the banish)
            banishButton.interactable = true;
            banishPostit.GetComponent<CanvasGroup>().blocksRaycasts = true;

            isUsingBanishForCurrentItemSelection = true;
            foreach (Animator animator in levelUpChoicesButtonAnimators)
            {
                animator.SetBool("Banish", true);
            }

            // Default select first item
            UIManager.instance.SetSelectedButton(levelUpChoicesPanels[0]);

            if (logsVerboseLevel == VerboseLevel.MAXIMAL)
            {
                Debug.Log("Use banish on level up item selection");
            }
        }
    }

    public void LevelUP()
    {
        isUsingBanishForCurrentItemSelection = false;
        foreach (Animator animator in levelUpChoicesButtonAnimators)
        {
            if (levelUpPanel.activeSelf)
            {
                animator.SetBool("Banish", false);
            }
        }
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

    private void UpdateRerollBanishSkipPostIts(bool rerollWouldGiveMoreOptions = true)
    {
        rerollPostit.SetActive(rerollsAvailable);
        rerollButton.interactable = (player.rerolls > 0) && rerollWouldGiveMoreOptions;
        rerollCount.SetText($"x{player.rerolls}");
        rerollPostit.GetComponent<CanvasGroup>().blocksRaycasts = (player.rerolls > 0) && rerollWouldGiveMoreOptions;

        banishPostit.SetActive(banishesAvailable);
        banishButton.interactable = (player.banishs > 0);
        banishCount.SetText($"x{player.banishs}");
        banishPostit.GetComponent<CanvasGroup>().blocksRaycasts = (player.banishs > 0);

        skipPostit.SetActive(skipsAvailable);
        skipButton.interactable = (player.skips > 0);
        skipCount.SetText($"x{player.skips}");
        skipPostit.GetComponent<CanvasGroup>().blocksRaycasts = (player.skips > 0);
    }

    private void SetRerollBanishSkipPostItsEnable(bool enabled)
    {
        rerollButton.interactable = enabled;
        rerollPostit.GetComponent<CanvasGroup>().blocksRaycasts = enabled;
        banishButton.interactable = enabled;
        banishPostit.GetComponent<CanvasGroup>().blocksRaycasts = enabled;
        skipButton.interactable = enabled;
        skipPostit.GetComponent<CanvasGroup>().blocksRaycasts = enabled;
    }

    public void ShowLevelUpItemSelection(List<RunItemData> possibleItems, List<int> itemLevels)
    {
        levelUpPanel.GetComponent<CanvasGroup>().interactable = true;
        EventSystem.current.SetSelectedGameObject(levelUpChoicesPanels[0]);

        int similarItemsCountInCaseOfReroll = RunItemManager.instance.ItemRerollSimilarItemsCount(possibleItems);
        UpdateRerollBanishSkipPostIts((similarItemsCountInCaseOfReroll < 3));

        // Audio
        SoundManager.instance.PlaySlideBookSound();
        SoundManager.instance.PauseInGameLoopedSFX();
        // MusicManager.instance.PlayLevelUpMusic(true); for now there is now special music for picking a level up

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
                    //levelUpChoicesLevels[index].text = $"LVL {nextLevel} (MAX)";
                    levelUpChoicesLevels[index].text = "LVL " + nextLevel.ToString();
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

        // Default select first item
        UIManager.instance.SetSelectedButton(levelUpChoicesPanels[0]);
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
        SoundManager.instance.UnpauseInGameLoopedSFX();
        // MusicManager.instance.PlayLevelUpMusic(false); for now there is no special level up music
    }

    private void InitializeInRunItemSlots()
    {
        // Remove previous slots.
        DestroyAndDeactivateChildren(tongueSlotsBackgroundsParent);
        DestroyAndDeactivateChildren(tongueSlotsIconsParent);
        DestroyAndDeactivateChildren(statItemSlotsBackgroundsParent);
        DestroyAndDeactivateChildren(statItemSlotsIconsParent);

        // Show as manys slots as the player has.
        for (int slots = 0; slots < player.weaponSlotsCount; slots++) AddNewRunItemSlot(tongueSlotsBackgroundsParent, tongueSlotsIconsParent);
        for (int slots = 0; slots < player.statItemSlotsCount; slots++) AddNewRunItemSlot(statItemSlotsBackgroundsParent, statItemSlotsIconsParent);
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
        Image nextFreeIconSlot = null;
        Transform backgroundParent = null;
        Transform iconParent = null;
        switch (newItem.GetItemType())
        {
            case RunItemType.WEAPON:
                backgroundParent = tongueSlotsBackgroundsParent;
                iconParent = tongueSlotsIconsParent;
                break;

            case RunItemType.STAT_BONUS:
                backgroundParent = statItemSlotsBackgroundsParent;
                iconParent = statItemSlotsIconsParent;
                break;

            // If an item is not a tongue or a stat item, it should not be displayed.
            default:
                return;
        }

        // Look for the first empty slot in the right parent.
        foreach (Transform slot in iconParent)
        {
            if (!slot.gameObject.activeSelf) continue;

            GameObject icon = slot.transform.Find("Icon").gameObject;
            if (!icon.activeSelf)
            {
                nextFreeIconSlot = icon.GetComponent<Image>();
                break;
            }
        }

        // If no empty slot was found, create a new one.
        if (nextFreeIconSlot == null)
        {
            nextFreeIconSlot = AddNewRunItemSlot(backgroundParent, iconParent).Find("Icon").GetComponent<Image>();
        }

        // Set the icon.
        nextFreeIconSlot.gameObject.SetActive(true);
        nextFreeIconSlot.sprite = newItem.icon;
    }

    private Transform AddNewRunItemSlot(Transform backgroundParent, Transform iconParent)
    {
        Instantiate(slotBackgroundPrefab, backgroundParent);
        return Instantiate(slotIconPrefab, iconParent).transform;
    }

    #endregion

    #region Collect Collectible

    public void CollectCollectible(CollectibleType collectibleType, float collectibleValue)
    {
        switch (collectibleType)
        {
            case CollectibleType.FROINS:
                int collectedCurrency = Mathf.RoundToInt(collectibleValue);
                IncreaseCollectedCurrency(collectedCurrency);
                SoundManager.instance.PlayPickUpFroinsSound();

                if (logsVerboseLevel == VerboseLevel.MAXIMAL)
                {
                    Debug.Log("Run - Collected " + collectedCurrency + " Froins. Current collected currency is: " + currentCollectedCurrency + " Froins");
                }
                break;
            case CollectibleType.LEVEL_UP:
                IncreaseXP(nextLevelXp - xp);
                if (logsVerboseLevel == VerboseLevel.MAXIMAL)
                {
                    Debug.Log("Run - Collected a Level Up");
                }
                break;
            case CollectibleType.XP_BONUS:
                IncreaseXP(collectibleValue * (1 + player.GetExperienceBoost()));
                SoundManager.instance.PlayPickUpXPSound(collectibleValue);
                if (logsVerboseLevel == VerboseLevel.MAXIMAL)
                {
                    Debug.Log("Run - Collected a XP Bonus: +" + collectibleValue + "XP");
                }
                break;
            case CollectibleType.HEALTH:
                player.Heal(collectibleValue, cancelDamage: true);
                SoundManager.instance.PlayHealSound();
                if (logsVerboseLevel == VerboseLevel.MAXIMAL)
                {
                    Debug.Log("Run - Collected some health. Healing: +" + collectibleValue + "HP");
                }
                break;
            case CollectibleType.POWERUP_FREEZEALL:
                player.TriggerExplosionEffect(CollectibleType.POWERUP_FREEZEALL);
                SoundManager.instance.PlayFreezeAllSound();
                if (logsVerboseLevel == VerboseLevel.MAXIMAL)
                {
                    Debug.Log("Run - Collected Freeze power-up");
                }
                break;
            case CollectibleType.POWERUP_POISONALL:
                player.TriggerExplosionEffect(CollectibleType.POWERUP_POISONALL);
                SoundManager.instance.PlayFreezeAllSound();
                if (logsVerboseLevel == VerboseLevel.MAXIMAL)
                {
                    Debug.Log("Run - Collected Poison power-up");
                }
                break;
            case CollectibleType.POWERUP_CURSEALL:
                player.TriggerExplosionEffect(CollectibleType.POWERUP_CURSEALL);
                SoundManager.instance.PlayCurseAllSound();
                if (logsVerboseLevel == VerboseLevel.MAXIMAL)
                {
                    Debug.Log("Run - Collected Curse power-up");
                }
                break;
            case CollectibleType.POWERUP_GODMODE:
                player.ApplyGodMode(DataManager.instance.powerUpGodModeTotalDuration, DataManager.instance.powerUpGodModeBlinkingDuration);
                SoundManager.instance.PlayFreezeAllSound();
                if (logsVerboseLevel == VerboseLevel.MAXIMAL)
                {
                    Debug.Log("Run - Collected God Mode power-up");
                }
                break;
            case CollectibleType.POWERUP_FRIENDSFRENZY:
                // Spawn a bunch of friends around the frog
                // They will behave as "temporary friends" = stick around for a short time and then wander away
                PlayConfettis();
                SoundManager.instance.PlayPartySound();
                for (int i = 0; i < DataManager.instance.powerUpFriendsFrenzyAmount; i++)
                {
                    Vector2 friendPosition = GameManager.instance.player.transform.position;
                    friendPosition += Random.insideUnitCircle.normalized * DataManager.instance.powerUpFriendsFrenzySpawnDistanceFromPlayer;
                    FriendType friendType = FriendType.GHOST; // Default is Ghost
                    if (!currentPlayedCharacter.characterID.Equals("GHOST"))
                    {
                        // If we're not playing as Ghost, then friends can be any frog but ghosts
                        do
                        {
                            friendType = (FriendType)Random.Range(0, System.Enum.GetValues(typeof(FriendType)).Length);
                        } while (friendType == FriendType.GHOST);
                    }

                    FriendsManager.instance.AddActiveFriend(friendType, friendPosition, temporary: true, lifespan: DataManager.instance.powerUpFriendsFrenzyLifespan);
                }
                if (logsVerboseLevel == VerboseLevel.MAXIMAL)
                {
                    Debug.Log("Run - Collected Friends Frenzy power-up");
                }
                break;
            case CollectibleType.POWERUP_MEGAMAGNET:
                CollectiblesManager.instance.ApplyMegaMagnet();
                SoundManager.instance.PlayGoopSound();
                if (logsVerboseLevel == VerboseLevel.MAXIMAL)
                {
                    Debug.Log("Run - Collected Mega Magnet power-up");
                }
                break;
            case CollectibleType.POWERUP_LEVELUPBUGS:
                player.TriggerExplosionEffect(CollectibleType.POWERUP_LEVELUPBUGS);
                //EnemiesManager.instance.SwitchTierOfAllEnemies(1);
                SoundManager.instance.PlayFreezeAllSound();
                if (logsVerboseLevel == VerboseLevel.MAXIMAL)
                {
                    Debug.Log("Run - Collected Level Up Bugs power-up");
                }
                break;
            case CollectibleType.POWERUP_LEVELDOWNBUGS:
                player.TriggerExplosionEffect(CollectibleType.POWERUP_LEVELDOWNBUGS);
                //EnemiesManager.instance.SwitchTierOfAllEnemies(-1);
                SoundManager.instance.PlayFreezeAllSound();
                if (logsVerboseLevel == VerboseLevel.MAXIMAL)
                {
                    Debug.Log("Run - Collected Level Down Bugs power-up");
                }
                break;
            case CollectibleType.POWERUP_TELEPORT:
                player.TeleportToARandomPosition();
                EnemiesManager.instance.ClearAllEnemies(true);
                FriendsManager.instance.ClearAllFriends(onlyTemporary: true);
                SoundManager.instance.PlayFreezeAllSound();
                if (logsVerboseLevel == VerboseLevel.MAXIMAL)
                {
                    Debug.Log("Run - Collected Teleport power-up");
                }
                break;
            default:
                break;
        }
    }

    #endregion

    #region Collect Fixed Collectible

    private void HideCollectFixedCollectiblePanel()
    {
        // Hide UI Panel
        fixedCollectibleFoundPanel.SetActive(false);
        fixedCollectibleFoundPanelIsVisible = false;
        fixedCollectibleInfo = null;
    }

    private void CloseCollectFixedCollectiblePanel()
    {
        // Hide UI Panel
        HideCollectFixedCollectiblePanel();
    }
    public void CollectFixedCollectible(bool accepted)
    {
        if (accepted)
        {
            // Pick up Fixed Collectible
            switch (fixedCollectibleInfo.collectibleType)
            {
                case FixedCollectibleType.FRIEND:
                    Vector2 friendPosition = GameManager.instance.player.transform.position;
                    FriendsManager.instance.AddActiveFriend(fixedCollectibleInfo.collectibleFriendType, friendPosition);
                    break;
                case FixedCollectibleType.HAT:
                    GameManager.instance.player.AddHat(fixedCollectibleInfo.collectibleHatType);
                    break;
                case FixedCollectibleType.STATS_ITEM:
                    PickRunItem(fixedCollectibleInfo.collectibleStatItemData);
                    break;
                case FixedCollectibleType.WEAPON_ITEM:
                    PickRunItem(fixedCollectibleInfo.collectibleWeaponItemData);
                    break;
            }
        }

        // Close UI panel
        CloseCollectFixedCollectiblePanel();

        // If there's no level up happening at the same time, game plays again
        if (!levelUpChoiceIsVisible)
        {
            // Set Time Scale back to 1
            GameManager.instance.SetTimeScale(1);

            // Unpause sounds
            SoundManager.instance.UnpauseInGameLoopedSFX();
        }
        else
        {
            // Select first choice on level up panel
            levelUpPanel.GetComponent<CanvasGroup>().interactable = true;
            EventSystem.current.SetSelectedGameObject(levelUpChoicesPanels[0]);
        }
    }

    public void ShowCollectSuperCollectiblePanel(FixedCollectible collectibleInfo, bool allowAccept = true, bool allowRefuse = true, string forceChoiceDescriptionStr = "", string forceChoiceButtonStr = "")
    {
        // Set Time Scale back to 0
        GameManager.instance.SetTimeScale(0);

        // Update title ("you found" for items, "you met" for friends, or whatever you choose)
        string foundCollectibleTitle = string.IsNullOrEmpty(collectibleInfo.foundCollectibleTitle) ? DataManager.instance.GetDefaultFoundCollectibleTitle(collectibleInfo) : collectibleInfo.foundCollectibleTitle;
        fixedCollectibleTitleText.text = foundCollectibleTitle;

        // Update name of the item
        string foundCollectibleName = string.IsNullOrEmpty(collectibleInfo.collectibleName) ? DataManager.instance.GetDefaultFoundCollectibleName(collectibleInfo) : collectibleInfo.collectibleName;
        fixedCollectibleNameText.text = foundCollectibleName;

        // Update bonus of the item
        int currentLevelForItem;
        int maxLevelForItem;
        string descriptionStr;
        string levelStr;
        string bonusText = $"({collectibleInfo.collectibleDescription})";

        if (collectibleInfo.collectibleType == FixedCollectibleType.WEAPON_ITEM || collectibleInfo.collectibleType == FixedCollectibleType.STATS_ITEM)
        {
            // Item is tongue or stat item
            // In that case, description is "level + stats improved"
            // Unless the item is a new, in that case there's a description for it
            RunItemData item = (collectibleInfo.collectibleType == FixedCollectibleType.WEAPON_ITEM) ? collectibleInfo.collectibleWeaponItemData : collectibleInfo.collectibleStatItemData;
            maxLevelForItem = item.GetMaxLevel();
            currentLevelForItem = Mathf.Clamp(GetLevelForItem(item), 0, maxLevelForItem);

            if (currentLevelForItem <= 0)
            {
                levelStr = "NEW";
            }
            else if (currentLevelForItem == maxLevelForItem - 1)
            {
                levelStr = "MAX";
            }
            else if (currentLevelForItem >= maxLevelForItem)
            {
                levelStr = "MAX+";
                currentLevelForItem = maxLevelForItem - 1; // If item is already maxed out, then next upgrade is the same as max upgrade
            }
            else
            {
                levelStr = $"Lvl {currentLevelForItem + 1}";
            }

            if (collectibleInfo.collectibleType == FixedCollectibleType.WEAPON_ITEM && currentLevelForItem <= 0)
            {
                // Item is a new Tongue
                descriptionStr = collectibleInfo.collectibleWeaponItemData.weaponData.description;
            }
            else if (collectibleInfo.collectibleType == FixedCollectibleType.WEAPON_ITEM)
            {
                // Item is a tongue upgrade
                descriptionStr = collectibleInfo.collectibleWeaponItemData.weaponBoostLevels[currentLevelForItem - 1].weaponStatUpgrades.GetDescription().Replace("\n", " & ");
            }
            else
            {
                // Item is a stat item

                if (currentLevelForItem >= maxLevelForItem)
                {
                    currentLevelForItem = maxLevelForItem - 1; // If item is already maxed out, then next upgrade is the same as max upgrade
                }

                descriptionStr = collectibleInfo.collectibleStatItemData.statBoostLevels[currentLevelForItem].description;
            }

            bonusText = $"({levelStr}: {descriptionStr})";
        }
        fixedCollectibleBonusText.text = bonusText;

        // Update icon
        fixedCollectibleItemIcon.enabled = false;
        fixedCollectibleFriendIcon.enabled = false;
        fixedCollectibleHatIcon.enabled = false;
        switch (collectibleInfo.collectibleType)
        {
            case FixedCollectibleType.FRIEND:
                fixedCollectibleFriendIcon.enabled = true;
                fixedCollectibleFriendIcon.sprite = DataManager.instance.GetSpriteForFriend(collectibleInfo.collectibleFriendType);
                break;
            case FixedCollectibleType.HAT:
                fixedCollectibleHatIcon.enabled = true;
                fixedCollectibleHatIcon.sprite = DataManager.instance.GetSpriteForHat(collectibleInfo.collectibleHatType);
                break;
            case FixedCollectibleType.STATS_ITEM:
                fixedCollectibleItemIcon.enabled = true;
                fixedCollectibleItemIcon.sprite = collectibleInfo.collectibleStatItemData.icon;
                break;
            case FixedCollectibleType.WEAPON_ITEM:
                fixedCollectibleItemIcon.enabled = true;
                fixedCollectibleItemIcon.sprite = collectibleInfo.collectibleWeaponItemData.icon;
                break;
        }

        fixedCollectibleTwoButtonsPanel.SetActive(false);
        fixedCollectibleForceAcceptPanel.SetActive(false);
        fixedCollectibleForceRefusePanel.SetActive(false);
        Button defaultSelectedButton = fixedCollectibleTwoButtonsAcceptButton;
        if (allowAccept && allowRefuse)
        {
            // Two buttons situation
            fixedCollectibleTwoButtonsPanel.SetActive(true);
            defaultSelectedButton = fixedCollectibleTwoButtonsAcceptButton;
            // Update "Accept" text
            string foundCollectibleAcceptStr = string.IsNullOrEmpty(collectibleInfo.acceptCollectibleStr) ? DataManager.instance.defaultFoundCollectibleAcceptStr : collectibleInfo.acceptCollectibleStr;
            fixedCollectibleTwoButtonsAcceptText.text = foundCollectibleAcceptStr;
            // Update "Refuse" text
            string foundCollectibleRefuseStr = string.IsNullOrEmpty(collectibleInfo.refuseCollectibleStr) ? DataManager.instance.defaultFoundCollectibleRefuseStr : collectibleInfo.refuseCollectibleStr;
            fixedCollectibleTwoButtonsRefuseText.text = foundCollectibleRefuseStr;
        }
        else if (allowAccept)
        {
            // Force Accept
            fixedCollectibleForceAcceptPanel.SetActive(true);
            defaultSelectedButton = fixedCollectibleForceAcceptButton;
            // Update descriptions text
            fixedCollectibleForceAcceptDescriptionText.text = forceChoiceDescriptionStr;
            // Update "Accept" text
            fixedCollectibleForceAcceptText.text = forceChoiceButtonStr;
        }
        else if (allowRefuse)
        {
            // Force Refuse
            fixedCollectibleForceRefusePanel.SetActive(true);
            defaultSelectedButton = fixedCollectibleForceRefuseButton;
            // Update descriptions text
            fixedCollectibleForceRefuseDescriptionText.text = forceChoiceDescriptionStr;
            // Update "Refuse" text
            fixedCollectibleForceRefuseText.text = forceChoiceButtonStr;
        }
        else
        {
            Debug.LogWarning("Trying to call ShowCollectSuperCollectiblePanel() but neither Accept nor Refuse are accepted options.");
        }

        // Show UI Panel
        fixedCollectibleFoundPanel.SetActive(true);
        fixedCollectibleFoundPanelIsVisible = true;
        fixedCollectibleInfo = collectibleInfo;

        // Select Accept button by default
        fixedCollectibleFoundPanel.GetComponent<CanvasGroup>().interactable = true;
        EventSystem.current.SetSelectedGameObject(defaultSelectedButton.gameObject);
        defaultSelectedButton.Select();

        // Stop sounds
        SoundManager.instance.PauseInGameLoopedSFX();

        // Save that data in the save file: this fixed collectible has been found!
        ChapterManager.instance.FoundFixedCollectible(currentChapter, collectibleInfo);
    }

    #endregion

    /// <summary>
    /// Play the confetti particle effect
    /// </summary>
    public void PlayConfettis()
    {
        confettiParticleSystem.Play();
    }

    public void StopConfettis()
    {
        confettiParticleSystem.Stop();
        confettiParticleSystem.Clear();
    }
}
