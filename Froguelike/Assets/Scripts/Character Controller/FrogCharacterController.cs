using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Rewired;
using System.Linq;
using Unity.VisualScripting;

[System.Serializable]
public class StatScoreScaling
{
    public StatValue valueIncrease;
    public int scoreValue;

    public StatScoreScaling(StatValue valueIncrease, int scoreValue)
    {
        this.valueIncrease = new StatValue(valueIncrease);
        this.scoreValue = scoreValue;
    }
}


public class FrogCharacterController : MonoBehaviour
{
    [Header("Player id")]
    public int playerID;

    [Header("Settings - Logs")]
    public VerboseLevel logsVerboseLevel = VerboseLevel.NONE;

    [Header("References")]
    public Transform weaponsParent;
    public Transform weaponStartPoint;
    public HealthBarBehaviour healthBar;
    [Space]
    public CircleCollider2D magnetTrigger;
    [Space]
    public Animator animator;
    [Space]
    public SpriteRenderer characterRenderer;
    [Space]
    public FrogExplosionBehaviour freezeExplosionEffect;
    public FrogExplosionBehaviour poisonExplosionEffect;
    public FrogExplosionBehaviour curseExplosionEffect;
    public FrogExplosionBehaviour levelDownBugsExplosionEffect;
    public FrogExplosionBehaviour levelUpBugsExplosionEffect;

    [Header("Hats")]
    public Transform hatsParent;
    public GameObject hatPrefab;
    public int hatSortingOrder = 10;

    [Header("Character data - Settings")]
    public Color characterBeingHitOverlayColor;

    [Header("Character data - God Mode Settings")]
    public int godModeOutlineWidth = 1;
    public List<Color> godModeOutlineColors;
    public GameObject superFrogOverlay;
    [Space]
    public StatsWrapper godModeStatBonuses;

    [Header("Character data - Runtime")]
    public float walkSpeedBoost;
    public float swimSpeedBoost;
    [Space]
    public float magnetRangeBoost = 0;
    [Space]
    public float maxHealth = 0;
    public float healthRecovery = 0;
    public float armor = 0;
    [Space]
    public float experienceBoost = 0;
    public float currencyBoost = 0;
    public float curse = 0;
    [Space]
    public int revivals = 0;
    public int rerolls = 0;
    public int banishs = 0;
    public int skips = 0;
    [Space]
    public float attackCooldownBoost = 0;
    public float attackDamageBoost = 0;
    public float attackRangeBoost = 0;
    public float attackSizeBoost = 0;
    public float attackSpeedBoost = 0;
    public float attackDurationBoost = 0;
    [Space]
    public int statItemSlotsCount;
    public int weaponSlotsCount;
    [Space]
    public List<StatScoreScaling> statScaleWithScoreList;

    [Header("Settings - controls")]
    public string horizontalInputName = "horizontal";
    public string verticalInputName = "vertical";
    public string pauseInputName = "pause";
    [Space]
    public string uiSubmitInputName = "UISubmit";
    public string uiCancelInputName = "UICancel";
    [Space]
    public float inputAxisDeadZone = 0.3f;
    public float delayBeforeHidingCursor = 5;
    public GameObject cursorBlock;

    #region Cheats

    [Header("Settings - cheat controls")]
    public string cheat_inRun_levelUp = "cheat_inRun_levelUp";
    public string cheat_inRun_endChapter = "cheat_inRun_endChapter";
    public string cheat_inRun_spawnBugs_tier1 = "cheat_inRun_spawnBugs_tier1";
    public string cheat_inRun_spawnBugs_tier2 = "cheat_inRun_spawnBugs_tier2";
    public string cheat_inRun_spawnBugs_tier3 = "cheat_inRun_spawnBugs_tier3";
    public string cheat_inRun_spawnBugs_tier4 = "cheat_inRun_spawnBugs_tier4";
    public string cheat_inRun_spawnBugs_tier5 = "cheat_inRun_spawnBugs_tier5";
    public string cheat_inRun_removeBugs = "cheat_inRun_removeBugs";
    public string cheat_inRun_fullHeal = "cheat_inRun_fullHeal";
    public string cheat_inRun_speedUp = "cheat_inRun_speedUp";
    public string cheat_inRun_addRerolls = "cheat_inRun_addRerolls";
    public string cheat_inRun_megaMagnet = "cheat_inRun_megaMagnet";
    public string cheat_inRun_armorPlus = "cheat_inRun_armorPlus";
    public string cheat_inRun_rangePlus = "cheat_inRun_rangePlus";
    public string cheat_inRun_scorePlus = "cheat_inRun_scorePlus";
    public string cheat_inRun_maxHPPlus = "cheat_inRun_maxHPPlus";
    public string cheat_inRun_addHat = "cheat_inRun_addHat";
    public string cheat_inRun_removeHats = "cheat_inRun_removeHats";
    public string cheat_inRun_addFrend = "cheat_inRun_addFrend";
    public string cheat_inRun_removeFrends = "cheat_inRun_removeFrends";
    [Space]
    public string cheat_getFroins = "cheat_getFroins";
    public string cheat_unlockAllQuests = "cheat_unlockAllQuests";
    public string cheat_toggleVersionNumber = "cheat_toggleVersionNumber";
    [Space]
    public string cheat_steam_clearAchievements = "cheat_steam_clearAchievements";

    #endregion 

    private Player rewiredPlayer;
    public float HorizontalInput { get; private set; }
    public float VerticalInput { get; private set; }

    private int animatorCharacterValue;

    private Rigidbody2D playerRigidbody;

    private bool isOnLand;
    private bool onlyUseWalkSpeed;

    private float orientationAngle;

    public bool superFrogMode { private set; get; }
    private Coroutine superFrogCoroutine;

    private Vector3 previousMousePosition;
    private Coroutine hideCursorCoroutine;

    #region Unity Callback Methods

    // Start is called before the first frame update
    void Start()
    {
        isOnLand = true;
        onlyUseWalkSpeed = false;
        rewiredPlayer = ReInput.players.GetPlayer(playerID);
        playerRigidbody = GetComponent<Rigidbody2D>();
        FriendsManager.instance.ClearAllFriends();
        previousMousePosition = Input.mousePosition;

        if (BuildManager.instance.everythingIsUnlocked)
        {
            AchievementManager.instance.GetUnlockedAchievementsForCurrentRun(true, true);
            //UIManager.instance.ShowTitleScreen();
        }

        HideCursor();
    }

    // Update is called once per frame
    void Update()
    {
        if (BuildManager.instance.cheatsAreEnabled)
        {
            DealWithCheatInputs();
        }

        bool ignoreUICancelInput = false;

        if (GameManager.instance.isGameRunning || (ChapterManager.instance.chapterChoiceIsVisible && !ChapterManager.instance.isFirstChapter))
        {
            // Get Pause input
            if (GetPauseInput())
            {
                // If black screen between chapters is visible, we prevent pausing the game
                if (!UIManager.instance.IsChapterStartScreenVisible())
                {
                    GameManager.instance.TogglePause();
                    ignoreUICancelInput = true;
                }
            }

            // Attempt to attack with all active tongues
            foreach (Transform weaponTransform in weaponsParent)
            {
                weaponTransform.GetComponent<WeaponBehaviour>().TryAttack();
            }

            // Set animator speed
            float speed = Mathf.Clamp(playerRigidbody.velocity.magnitude, 0, 10);
            animator.SetFloat("Speed", speed);

            // Check for Game Over condition or critical health status
            CheckHealthStatus();
        }

        if (!ignoreUICancelInput && GetUICancelInput())
        {
            GameManager.instance.UICancel();
        }

        // Hide / Show mouse cursor
        bool mouseLeftPressed = Input.GetMouseButton(0);
        bool mouseRightPressed = Input.GetMouseButton(1);
        bool mouseWheelUsed = Input.mouseScrollDelta.magnitude > 0;
        float cursorMovedDistance = Vector3.Distance(previousMousePosition, Input.mousePosition);
        if (mouseLeftPressed || mouseRightPressed || cursorMovedDistance > 0 || mouseWheelUsed)
        {
            if (cursorBlock.activeInHierarchy)
            {
                Cursor.visible = true;
                cursorBlock.SetActive(false);
            }

            if (hideCursorCoroutine != null)
            {
                StopCoroutine(hideCursorCoroutine);
            }
            hideCursorCoroutine = StartCoroutine(WaitAndHideCursor(delayBeforeHidingCursor));
        }
        previousMousePosition = Input.mousePosition;
    }

    private IEnumerator WaitAndHideCursor(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        HideCursor();
    }

    private void HideCursor()
    {
        Cursor.visible = false;
        cursorBlock.SetActive(true);
    }

    private void FixedUpdate()
    {
        // Get movement input
        UpdateHorizontalInput();
        UpdateVerticalInput();

        float moveSpeed = (isOnLand || onlyUseWalkSpeed) ? (DataManager.instance.defaultWalkSpeed * (1 + GetWalkSpeedBoost())) : (DataManager.instance.defaultSwimSpeed * (1 + GetSwimSpeedBoost()));

        Vector2 moveInput = (((HorizontalInput * Vector2.right).normalized + (VerticalInput * Vector2.up).normalized)).normalized * moveSpeed;

        // If movement input is not zero, then rotate frog
        if (!moveInput.Equals(Vector2.zero))
        {
            orientationAngle = 90 + 90 * Mathf.RoundToInt((Vector2.SignedAngle(moveInput, Vector2.right)) / 90);
            transform.localRotation = Quaternion.Euler(0, 0, -orientationAngle);
        }

        // In any case, set frog velocity
        playerRigidbody.velocity = moveInput;

        // Make sure that every tongue follows the frog
        foreach (Transform weaponTransform in weaponsParent)
        {
            weaponTransform.GetComponent<WeaponBehaviour>().SetTonguePosition(weaponStartPoint);
        }
    }

    #endregion

    /// <summary>
    /// Get the current added bonus scaled with score for request stat type
    /// </summary>
    /// <param name="statType"></param>
    /// <returns></returns>
    private float GetScoreScaledBoostForStat(CharacterStat statType)
    {
        float result = 0;
        int score = RunManager.instance.GetTotalKillCount();
        IEnumerable<StatScoreScaling> statScoreScalingList = statScaleWithScoreList.Where(x => x.valueIncrease.stat.Equals(statType));
        foreach (StatScoreScaling statScoreScaling in statScoreScalingList)
        {
            result += (float)(statScoreScaling.valueIncrease.value * (score / statScoreScaling.scoreValue));
        }
        return result;
    }

    /// <summary>
    /// Get the added bonus in the current chapter for request stat type
    /// </summary>
    /// <param name="statType"></param>
    /// <returns></returns>
    private float GetChapterStatBonus(CharacterStat statType)
    {
        if (RunManager.instance != null && RunManager.instance.currentChapter != null && RunManager.instance.currentChapter.chapterData != null && RunManager.instance.currentChapter.chapterData.startingStatBonuses != null)
        {
            return (float)RunManager.instance.currentChapter.chapterData.startingStatBonuses.GetStatValue(statType).value;
        }
        return 0;
    }

    private float GetGodModeStatBonus(CharacterStat statType)
    {
        return (float)godModeStatBonuses.GetStatValue(statType).value;
    }

    private float GetCurrentStatBonus(CharacterStat statType)
    {
        float result = 0;
        result += GetScoreScaledBoostForStat(statType);
        result += GetChapterStatBonus(statType);
        if (superFrogMode)
        {
            result += GetGodModeStatBonus(statType);
        }
        return result;
    }

    #region Stats Accessors

    /// <summary>
    /// Get current walk speed boost. 
    /// Scaled with score if needed. Increased with Super Frog if active.
    /// </summary>
    /// <returns></returns>
    public float GetWalkSpeedBoost()
    {
        return walkSpeedBoost + GetCurrentStatBonus(CharacterStat.WALK_SPEED_BOOST);
    }

    /// <summary>
    /// Get current swim speed boost. 
    /// Scaled with score if needed. Increased with Super Frog if active.
    /// </summary>
    /// <returns></returns>
    public float GetSwimSpeedBoost()
    {
        return swimSpeedBoost + GetCurrentStatBonus(CharacterStat.SWIM_SPEED_BOOST);
    }

    /// <summary>
    /// Get current magnet range boost. 
    /// Scaled with score if needed. Increased with Super Frog if active.
    /// </summary>
    /// <returns></returns>
    public float GetMagnetRangeBoost()
    {
        return magnetRangeBoost + GetCurrentStatBonus(CharacterStat.MAGNET_RANGE_BOOST);
    }

    /// <summary>
    /// Get current max health. 
    /// Scaled with score if needed.
    /// </summary>
    /// <returns></returns>
    public float GetMaxHealth()
    {
        return maxHealth + GetCurrentStatBonus(CharacterStat.MAX_HEALTH);
    }

    /// <summary>
    /// Get current health recovery. 
    /// Scaled with score if needed.
    /// </summary>
    /// <returns></returns>
    public float GetHealthRecovery()
    {
        return healthRecovery + GetCurrentStatBonus(CharacterStat.HEALTH_RECOVERY);
    }

    /// <summary>
    /// Get current armor. 
    /// Scaled with score if needed.
    /// </summary>
    /// <returns></returns>
    public float GetArmor()
    {
        return armor + GetCurrentStatBonus(CharacterStat.ARMOR);
    }

    /// <summary>
    /// Get current experience boost. 
    /// Scaled with score if needed.
    /// </summary>
    /// <returns></returns>
    public float GetExperienceBoost()
    {
        return experienceBoost + GetCurrentStatBonus(CharacterStat.XP_BOOST);
    }

    /// <summary>
    /// Get current currency boost. 
    /// Scaled with score if needed.
    /// </summary>
    /// <returns></returns>
    public float GetCurrencyBoost()
    {
        return currencyBoost + GetCurrentStatBonus(CharacterStat.CURRENCY_BOOST);
    }

    /// <summary>
    /// Get current curse. 
    /// Scaled with score if needed.
    /// </summary>
    /// <returns></returns>
    public float GetCurse()
    {
        return curse + GetCurrentStatBonus(CharacterStat.CURSE);
    }

    /// <summary>
    /// Get current attack cooldown boost. 
    /// Scaled with score if needed. Decreased with Super Frog if active.
    /// Minimum is -1 (-100%, meaning no cooldown)
    /// </summary>
    /// <returns></returns>
    public float GetAttackCooldownBoost()
    {
        return Mathf.Clamp(attackCooldownBoost + GetCurrentStatBonus(CharacterStat.ATK_COOLDOWN_BOOST), -1, 10000); // Maximum scaledCooldownBoost is +1000000%, it's not gonna happen
    }

    /// <summary>
    /// Get current attack damage boost. 
    /// Scaled with score if needed. Increased with Super Frog if active.
    /// </summary>
    /// <returns></returns>
    public float GetAttackDamageBoost()
    {
        return attackDamageBoost + GetCurrentStatBonus(CharacterStat.ATK_DAMAGE_BOOST);
    }

    /// <summary>
    /// Get current attack range boost. 
    /// Scaled with score if needed. Increased with Super Frog if active.
    /// </summary>
    /// <returns></returns>
    public float GetAttackRangeBoost()
    {
        return attackRangeBoost + GetCurrentStatBonus(CharacterStat.ATK_RANGE_BOOST);
    }

    /// <summary>
    /// Get current attack size boost. 
    /// Scaled with score if needed. Increased with Super Frog if active.
    /// </summary>
    /// <returns></returns>
    public float GetAttackSizeBoost()
    {
        return attackSizeBoost + GetCurrentStatBonus(CharacterStat.ATK_SIZE_BOOST);
    }

    /// <summary>
    /// Get current attack speed boost. 
    /// Scaled with score if needed. Increased with Super Frog if active.
    /// </summary>
    /// <returns></returns>
    public float GetAttackSpeedBoost()
    {
        return attackSpeedBoost + GetCurrentStatBonus(CharacterStat.ATK_SPEED_BOOST);
    }

    /// <summary>
    /// Get current attack duration boost. 
    /// Scaled with score if needed. Increased with Super Frog if active.
    /// </summary>
    /// <returns></returns>
    public float GetAttackDurationBoost()
    {
        return attackDurationBoost + GetCurrentStatBonus(CharacterStat.ATK_DURATION_BOOST);
    }

    #endregion Accessors

    public bool IsOnLand()
    {
        return isOnLand;
    }

    /// <summary>
    /// Update magnet range, health recovery and max health.
    /// If those should scale with score, they do.
    /// </summary>
    public void UpdateScalingWithScoreStats()
    {
        UpdateMagnetRange();
        healthBar.SetHealthRecovery(GetHealthRecovery());
        healthBar.SetMaxHealth(GetMaxHealth());
    }

    public Rigidbody2D GetRigidbody()
    {
        return playerRigidbody;
    }

    public Vector2 GetVelocity()
    {
        return new Vector2(HorizontalInput, VerticalInput);
        // return playerRigidbody.velocity;
    }


    public void TeleportToARandomPosition()
    {
        // Find random position
        Vector2 randomPosition = Random.insideUnitCircle * Random.Range(100, 500);
        // Prepare the map
        MapBehaviour.instance.GenerateNewTilesAroundPosition(randomPosition);
        // Reset all tongues
        foreach (Transform weaponTransform in weaponsParent)
        {
            weaponTransform.GetComponent<WeaponBehaviour>().ResetTongue();
        }
        // Destroy all captured collectibles (they are lost)
        CollectiblesManager.instance.CancelAllCapturedCollectibles();
        // Teleport
        isOnLand = true;
        transform.localPosition = randomPosition;
        // Teleport friends with you
        Vector2 frogPosition = new Vector2(this.transform.position.x, this.transform.position.y);
        FriendsManager.instance.PlacePermanentFriendsInACircleAroundFrog(frogPosition);
    }

    public void ResetPosition()
    {
        isOnLand = true;
        transform.localPosition = Vector3.zero;
        ResetGodMode();
        Vector2 frogPosition = new Vector2(this.transform.position.x, this.transform.position.y);
        FriendsManager.instance.PlacePermanentFriendsInACircleAroundFrog(frogPosition);
    }

    private void SetAnimatorCharacterValue(int value)
    {
        animatorCharacterValue = value;
        animator.SetInteger("character", animatorCharacterValue);
    }

    public void ForceGhost(bool isGhost)
    {
        animator.SetInteger("character", isGhost ? 2 : animatorCharacterValue);
    }

    private void UpdateMagnetRange()
    {
        magnetTrigger.radius = (DataManager.instance.defaultMagnetRange * (1 + GetMagnetRangeBoost()));
        magnetTrigger.gameObject.SetActive(true);
        magnetTrigger.enabled = true;
    }

    private void SetCharacterOutline(Color newColor, int thickness = 1)
    {
        characterRenderer.material.SetFloat("_OutlineThickness", thickness);
        characterRenderer.material.SetColor("_OutlineColor", newColor);
    }

    public void InitializeCharacter(PlayableCharacter characterInfo)
    {
        onlyUseWalkSpeed = characterInfo.characterID.Equals("GHOST");

        // Set up animator
        SetAnimatorCharacterValue(characterInfo.characterData.characterAnimatorValue);

        // Reset sprite overlay & outline
        characterRenderer.material.SetFloat("_OverlayVisible", 0);
        characterRenderer.material.SetColor("_OverlayColor", characterBeingHitOverlayColor);
        SetCharacterOutline(Color.white, 0);

        // Starting Stats for this character
        StatsWrapper allStartingStatsWrapper = StatsWrapper.JoinLists(characterInfo.GetCharacterStartingStats(), ShopManager.instance.statsBonuses);

        if (logsVerboseLevel == VerboseLevel.MAXIMAL)
        {
            Debug.Log("Player - Initialize character with stats " + allStartingStatsWrapper.ToString());
        }

        // MAX HP should always be defined for any character
        maxHealth = DataManager.instance.defaultMaxHP;
        if (allStartingStatsWrapper.GetValueForStat(CharacterStat.MAX_HEALTH, out float startingMaxHPBonus))
        {
            maxHealth += startingMaxHPBonus;
        }

        // HP Recovery
        healthRecovery = DataManager.instance.defaultHealthRecovery;
        if (allStartingStatsWrapper.GetValueForStat(CharacterStat.HEALTH_RECOVERY, out float startingHPRecoveryBoost))
        {
            healthRecovery += startingHPRecoveryBoost;
        }

        healthBar.SetMaxHealth(maxHealth);
        healthBar.SetHealthRecovery(healthRecovery);

        // Armor
        armor = 0;
        if (allStartingStatsWrapper.GetValueForStat(CharacterStat.ARMOR, out float startingArmor))
        {
            armor = startingArmor;
        }

        // Experience boost
        experienceBoost = 0;
        if (allStartingStatsWrapper.GetValueForStat(CharacterStat.XP_BOOST, out float startingXPBoost))
        {
            experienceBoost = startingXPBoost;
        }

        // Currency boost
        currencyBoost = 0;
        if (allStartingStatsWrapper.GetValueForStat(CharacterStat.CURRENCY_BOOST, out float startingCurrencyBoost))
        {
            currencyBoost = startingCurrencyBoost;
        }

        // Curse boost
        curse = 0;
        if (allStartingStatsWrapper.GetValueForStat(CharacterStat.CURSE, out float startingCurse))
        {
            curse = startingCurse;
        }

        // Walk speed
        walkSpeedBoost = 0;
        if (allStartingStatsWrapper.GetValueForStat(CharacterStat.WALK_SPEED_BOOST, out float startingWalkSpeedBoost))
        {
            walkSpeedBoost = startingWalkSpeedBoost;
        }

        // Swim speed
        swimSpeedBoost = 0;
        if (allStartingStatsWrapper.GetValueForStat(CharacterStat.SWIM_SPEED_BOOST, out float startingSwimSpeedBoost))
        {
            swimSpeedBoost = startingSwimSpeedBoost;
        }

        // Revivals
        revivals = 0;
        if (allStartingStatsWrapper.GetValueForStat(CharacterStat.REVIVAL, out float startingRevivals))
        {
            revivals = Mathf.FloorToInt(startingRevivals);
        }

        // Rerolls
        rerolls = 0;
        if (allStartingStatsWrapper.GetValueForStat(CharacterStat.REROLL, out float startingRerolls))
        {
            rerolls = Mathf.FloorToInt(startingRerolls);
        }

        // Banishs
        banishs = 0;
        if (allStartingStatsWrapper.GetValueForStat(CharacterStat.BANISH, out float startingBanishs))
        {
            banishs = Mathf.FloorToInt(startingBanishs);
        }

        // Skips
        skips = 0;
        if (allStartingStatsWrapper.GetValueForStat(CharacterStat.SKIP, out float startingSkips))
        {
            skips = Mathf.FloorToInt(startingSkips);
        }

        // Atk Damage Boost
        attackDamageBoost = 0;
        if (allStartingStatsWrapper.GetValueForStat(CharacterStat.ATK_DAMAGE_BOOST, out float startingAtkDmgBoost))
        {
            attackDamageBoost = startingAtkDmgBoost;
        }

        // Atk Speed Boost
        attackSpeedBoost = 0;
        if (allStartingStatsWrapper.GetValueForStat(CharacterStat.ATK_SPEED_BOOST, out float startingAtkSpeedBoost))
        {
            attackSpeedBoost = startingAtkSpeedBoost;
        }

        // Atk Cooldown Boost
        attackCooldownBoost = 0;
        if (allStartingStatsWrapper.GetValueForStat(CharacterStat.ATK_COOLDOWN_BOOST, out float startingAtkCooldownBoost))
        {
            attackCooldownBoost = startingAtkCooldownBoost;
        }

        // Atk Range Boost
        attackRangeBoost = 0;
        if (allStartingStatsWrapper.GetValueForStat(CharacterStat.ATK_RANGE_BOOST, out float startingAtkRangeBoost))
        {
            attackRangeBoost = startingAtkRangeBoost;
        }

        // Atk Size Boost
        attackSizeBoost = 0;
        if (allStartingStatsWrapper.GetValueForStat(CharacterStat.ATK_SIZE_BOOST, out float startingAtkSizeBoost))
        {
            attackSizeBoost = startingAtkSizeBoost;
        }

        // Atk Duration Boost
        attackDurationBoost = 0;
        if (allStartingStatsWrapper.GetValueForStat(CharacterStat.ATK_DURATION_BOOST, out float startingAtkDurationBoost))
        {
            attackDurationBoost = startingAtkDurationBoost;
        }

        // Magnet Range
        magnetRangeBoost = 0;
        if (allStartingStatsWrapper.GetValueForStat(CharacterStat.MAGNET_RANGE_BOOST, out float startingMagnetRangeBoost))
        {
            magnetRangeBoost = startingMagnetRangeBoost;
        }
        UpdateMagnetRange();

        // Item slots
        statItemSlotsCount = DataManager.instance.defaultStatItemSlotCount;
        if (allStartingStatsWrapper.GetValueForStat(CharacterStat.ITEM_SLOT, out float statItemSlotsCountIncrease))
        {
            statItemSlotsCount += Mathf.RoundToInt(statItemSlotsCountIncrease);
        }

        // Weapon slots
        weaponSlotsCount = DataManager.instance.defaultWeaponSlotCount;
        if (allStartingStatsWrapper.GetValueForStat(CharacterStat.WEAPON_SLOT, out float weaponSlotsCountIncrease))
        {
            weaponSlotsCount += Mathf.RoundToInt(weaponSlotsCountIncrease);
        }

        // Clear stats that scale with score
        statScaleWithScoreList = new List<StatScoreScaling>();

        RunManager.instance.SetExtraLives(revivals, false);

        healthBar.ResetHealth();
    }

    public void ResolvePickedConsumableItem(RunConsumableItemData consumableData)
    {
        RunManager.instance.IncreaseCollectedCurrency(consumableData.effect.currencyBonus);

        RunManager.instance.IncreaseKillCount(consumableData.effect.scoreBonus);

        RunManager.instance.IncreaseXP(consumableData.effect.xpBonus);

        Heal(consumableData.effect.healthBonus, cancelDamage: true);
    }

    public void ResolvePickedStatItemLevel(RunStatItemLevel itemLevelData)
    {
        if (itemLevelData.scaleWithScore)
        {
            // This upgrade adds a bonus that scales with score
            foreach (StatValue statValue in itemLevelData.statUpgrades.statsList)
            {
                bool statIncreaseAdded = false;

                StatScoreScaling newStatScoreScaling = new StatScoreScaling(statValue, itemLevelData.scaleWithScoreValue);

                foreach (StatScoreScaling statScoreScaling in statScaleWithScoreList)
                {
                    if (statScoreScaling.valueIncrease.Equals(statValue))
                    {
                        // That new increase is for a stat that is already in the list, we'll attempt to increment the existing one
                        if (statScoreScaling.scoreValue == newStatScoreScaling.scoreValue)
                        {
                            statScoreScaling.valueIncrease.value += newStatScoreScaling.valueIncrease.value;
                            statIncreaseAdded = true;
                            break;
                        }
                    }
                }
                if (statIncreaseAdded)
                {
                    continue;
                }
                else
                {
                    // We couldn't increment an existing stat so we add this increment to the list
                    statScaleWithScoreList.Add(newStatScoreScaling);
                }
            }
        }
        else
        {
            // This is a regular level up, no scaling with score
            curse += (float)itemLevelData.statUpgrades.GetStatValue(CharacterStat.CURSE).value;

            // character stats
            armor += (float)itemLevelData.statUpgrades.GetStatValue(CharacterStat.ARMOR).value;
            experienceBoost += (float)itemLevelData.statUpgrades.GetStatValue(CharacterStat.XP_BOOST).value;
            currencyBoost += (float)itemLevelData.statUpgrades.GetStatValue(CharacterStat.CURRENCY_BOOST).value;

            maxHealth += (float)itemLevelData.statUpgrades.GetStatValue(CharacterStat.MAX_HEALTH).value;
            healthRecovery += (float)itemLevelData.statUpgrades.GetStatValue(CharacterStat.HEALTH_RECOVERY).value;

            revivals += (int)itemLevelData.statUpgrades.GetStatValue(CharacterStat.REVIVAL).value;
            RunManager.instance.SetExtraLives(revivals, true);

            magnetRangeBoost += (float)itemLevelData.statUpgrades.GetStatValue(CharacterStat.MAGNET_RANGE_BOOST).value;

            walkSpeedBoost += (float)itemLevelData.statUpgrades.GetStatValue(CharacterStat.WALK_SPEED_BOOST).value;
            swimSpeedBoost += (float)itemLevelData.statUpgrades.GetStatValue(CharacterStat.SWIM_SPEED_BOOST).value;

            // attack stuff
            attackCooldownBoost += (float)itemLevelData.statUpgrades.GetStatValue(CharacterStat.ATK_COOLDOWN_BOOST).value;
            attackDamageBoost += (float)itemLevelData.statUpgrades.GetStatValue(CharacterStat.ATK_DAMAGE_BOOST).value;
            attackRangeBoost += (float)itemLevelData.statUpgrades.GetStatValue(CharacterStat.ATK_RANGE_BOOST).value;
            attackSpeedBoost += (float)itemLevelData.statUpgrades.GetStatValue(CharacterStat.ATK_SPEED_BOOST).value;
            attackSizeBoost += (float)itemLevelData.statUpgrades.GetStatValue(CharacterStat.ATK_SIZE_BOOST).value;
            attackDurationBoost += (float)itemLevelData.statUpgrades.GetStatValue(CharacterStat.ATK_DURATION_BOOST).value;

            // item and weapon slots
            statItemSlotsCount += (int)itemLevelData.statUpgrades.GetStatValue(CharacterStat.ITEM_SLOT).value;
            weaponSlotsCount += (int)itemLevelData.statUpgrades.GetStatValue(CharacterStat.WEAPON_SLOT).value;
        }

        UpdateScalingWithScoreStats();
    }


    public void Respawn()
    {
        healthBar.SetMaxHealth(GetMaxHealth());
        healthBar.SetHealthRecovery(GetHealthRecovery());
        healthBar.ResetHealth();

        if (logsVerboseLevel == VerboseLevel.MAXIMAL)
        {
            Debug.Log("Player - Respawn");
        }
    }

    public Vector2 GetMoveDirection()
    {
        Vector2 moveDirection = Vector2.zero;
        if (playerRigidbody.velocity.magnitude > 0.1f)
        {
            moveDirection = playerRigidbody.velocity.normalized;
        }
        return moveDirection;
    }

    public void ClearHats()
    {
        foreach (Transform child in hatsParent)
        {
            Destroy(child.gameObject);
        }
    }

    public void AddHat(HatType hatType)
    {
        int hatCount = hatsParent.childCount;
        Transform previousHatTransform = (hatCount <= 0) ? null : hatsParent.GetChild(hatCount - 1);
        GameObject newHatGo = Instantiate(hatPrefab, hatsParent);
        HatInfo hatInfo = DataManager.instance.GetInfoForHat(hatType);
        int sortingOrder = hatSortingOrder;
        if (previousHatTransform != null)
        {
            // hat is not the first, we move it
            float previousHatHeight = previousHatTransform.GetComponent<HatBehaviour>().GetHatHeight();
            newHatGo.transform.localPosition = previousHatTransform.localPosition - previousHatHeight * Vector3.up;
        }
        sortingOrder += hatCount;
        newHatGo.GetComponent<HatBehaviour>().Initialize(hatInfo);

        if (logsVerboseLevel == VerboseLevel.MAXIMAL)
        {
            Debug.Log($"Player - Add Hat:{hatType}");
        }
    }

    public bool HasHat(HatType hatType)
    {
        bool hasHat = false;
        foreach (Transform hat in hatsParent)
        {
            HatBehaviour hatScript = hat.GetComponent<HatBehaviour>();
            if (hatScript != null)
            {
                hasHat = (hatScript.GetHatType() == hatType);
            }
            if (hasHat) break;
        }
        return hasHat;
    }

    #region Update Inputs

    private void UpdateHorizontalInput()
    {
        HorizontalInput = rewiredPlayer.GetAxis(horizontalInputName);
        if (Mathf.Abs(HorizontalInput) < inputAxisDeadZone)
        {
            HorizontalInput = 0;
        }
    }

    private void UpdateVerticalInput()
    {
        VerticalInput = rewiredPlayer.GetAxis(verticalInputName);
        if (Mathf.Abs(VerticalInput) < inputAxisDeadZone)
        {
            VerticalInput = 0;
        }
    }

    private bool GetPauseInput()
    {
        return rewiredPlayer.GetButtonDown(pauseInputName);
    }

    private bool GetUICancelInput()
    {
        return rewiredPlayer.GetButtonDown(uiCancelInputName);
    }

    #endregion

    // This method is called every physics frame when colliders are touching or player collider is in enemy trigger
    private void DealWithEnemyCollision(Collider2D collider)
    {
        // Get data about attacking enemy
        EnemyInstance enemy = EnemiesManager.instance.GetEnemyInstanceFromGameObjectName(collider.gameObject.name);

        float damageCooldown = 1.0f;
        bool enemyCanInflictDamage = (enemy != null); // Enemy exists
        if (enemyCanInflictDamage)
        {
            enemyCanInflictDamage &= !RunManager.instance.IsChapterTimeOver(); // chapter is not over
            enemyCanInflictDamage &= enemy.active && enemy.alive; // Enemy is active and alive
            enemyCanInflictDamage &= (Time.time - enemy.lastDamageInflictedTime) >= damageCooldown; // This enemy didn't do damage for a while
            enemyCanInflictDamage &= !superFrogMode; // God mode is off
            enemyCanInflictDamage &= !EnemiesManager.instance.IsGlobalFreezeActive(); // Global freeze is off
            enemyCanInflictDamage &= (enemy.freezeRemainingTime <= 0); // This enemy is not frozen
        }
        if (enemyCanInflictDamage)
        {
            // This enemy can inflict damage now
            enemy.lastDamageInflictedTime = Time.time;
            EnemyData enemyData = null;

            BountyBug bountyBug = null;
            if (enemy != null && enemy.enemyInfo != null)
            {
                enemyData = enemy.enemyInfo.enemyData;
            }
            if (enemyData == null)
            {
                enemyData = EnemiesManager.instance.GetEnemyDataFromGameObjectName(collider.gameObject.name, out bountyBug);
            }

            float damageFactor = enemy.damageMultiplier;

            // Compute actual damage
            float damage = 1;
            float maxDamage = float.MaxValue;
            if (enemyData != null)
            {
                damage = enemyData.damage * damageFactor;

                // Game mode damage multiplier
                damage *= RunManager.instance.gameModeBugDamageMultiplier;

                damage = Mathf.Clamp(damage - GetArmor(), 0.1f, maxDamage);

                if (logsVerboseLevel == VerboseLevel.MAXIMAL)
                {
                    Debug.Log("Player - Take damage from " + enemyData.enemyName + ": " + damage.ToString("0.00") + " HP.");
                }
            }

            ChangeHealth(-damage, false);
        }
    }

    private void OnTriggerStay2D(Collider2D collider)
    {
        if (collider.CompareTag("Water"))
        {
            isOnLand = false;
        }
        if (collider.CompareTag("Enemy") && GameManager.instance.isGameRunning)
        {
            DealWithEnemyCollision(collider);
        }
    }
    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Water"))
        {
            isOnLand = true;
        }
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.collider.CompareTag("Enemy") && GameManager.instance.isGameRunning)
        {
            DealWithEnemyCollision(collision.collider);
        }
    }

    public void Heal(float healAmount, bool cancelDamage)
    {
        ChangeHealth((healAmount > 0) ? healAmount : 0, cancelDamage);
        if (logsVerboseLevel == VerboseLevel.MAXIMAL)
        {
            Debug.Log("Player - Healing: +" + healAmount + "HP.");
        }
    }

    private Coroutine DamageTookEndOfEffectCoroutine;

    public void StopTakeDamageEffect()
    {
        if (DamageTookEndOfEffectCoroutine != null)
        {
            StopCoroutine(DamageTookEndOfEffectCoroutine);
        }
        characterRenderer.material.SetFloat("_OverlayVisible", 0);
        SoundManager.instance.StopAllLoops();
    }

    private IEnumerator TakingDamageEndOfEffectAsync(float delay)
    {
        yield return new WaitForSeconds(delay);
        SoundManager.instance.StopPlayingTakeDamageLoopSound();
        characterRenderer.material.SetFloat("_OverlayVisible", 0);
    }

    private void TakingDamageEffect()
    {
        // Overlay.
        if (SettingsManager.instance.showFlashingEffects)
        {
            characterRenderer.material.SetFloat("_OverlayVisible", 1);
            if (DamageTookEndOfEffectCoroutine != null)
            {
                StopCoroutine(DamageTookEndOfEffectCoroutine);
            }
            DamageTookEndOfEffectCoroutine = StartCoroutine(TakingDamageEndOfEffectAsync(0.7f));
        }

        // SFX.
        SoundManager.instance.PlayTakeDamageLoopSound();
    }

    private void ChangeHealth(float change, bool cancelDamage)
    {
        if (change < 0)
        {
            // Take damage
            healthBar.DecreaseHealth(-change);
            TakingDamageEffect();
        }
        else if (change > 0)
        {
            // Heal
            healthBar.IncreaseHealth(change, cancelDamage);
        }
    }

    private void CheckHealthStatus()
    {
        if (healthBar.IsDead())
        {
            GameManager.instance.TriggerGameOver();
        }
        else if (healthBar.IsSuperCritical())
        {
            // Super critical health effect (music? UI change?)
        }
        else if (healthBar.IsCritical())
        {
            // Critical health effect (music? UI change?)
        }
    }

    public void ApplyGodMode(float totalDuration, float blinkDuration)
    {
        superFrogMode = true;
        MusicManager.instance.PlaySuperFrogMusic(superFrogMode);
        UpdateMagnetRange();

        // Outline
        SetCharacterOutline(godModeOutlineColors[0], godModeOutlineWidth);
        superFrogOverlay.SetActive(true);

        // Coroutine that will update god mode outline and eventually deactivate the god mode
        if (superFrogCoroutine != null)
        {
            StopCoroutine(superFrogCoroutine);
        }
        superFrogCoroutine = StartCoroutine(UpdateGodModeAsync(totalDuration, blinkDuration));
    }

    public void ResetGodMode()
    {
        if (superFrogCoroutine != null)
        {
            StopCoroutine(superFrogCoroutine);
        }
        superFrogMode = false;
        MusicManager.instance.PlaySuperFrogMusic(superFrogMode);
        SetCharacterOutline(godModeOutlineColors[0], 0);
        superFrogOverlay.SetActive(false);
        UpdateMagnetRange();
    }

    private IEnumerator UpdateGodModeAsync(float totalDuration, float blinkDuration)
    {
        float t = 0;
        int outlineColorIndex = 0;
        int outlineWidth = 0;
        float delayAfterWhichGodModeIsBlinking = totalDuration - blinkDuration;
        while (t < totalDuration)
        {
            yield return new WaitForEndOfFrame();
            t += Time.deltaTime;
            outlineWidth = godModeOutlineWidth;
            if (t >= delayAfterWhichGodModeIsBlinking)
            {
                if (Mathf.RoundToInt(t * 5) % 2 == 0)
                {
                    // Every 5th of a second, we toggle the outline (it blinks)
                    outlineWidth = 0;
                }
            }
            SetCharacterOutline(godModeOutlineColors[outlineColorIndex], outlineWidth);

            if (Mathf.RoundToInt(t * 10) % 2 == 0)
            {
                // Every 10th of a second, we change the outline color
                outlineColorIndex = (outlineColorIndex + 1) % godModeOutlineColors.Count;
            }
        }

        // Turn god mode off
        superFrogMode = false;
        MusicManager.instance.PlaySuperFrogMusic(superFrogMode);
        SetCharacterOutline(godModeOutlineColors[0], 0);
        superFrogOverlay.SetActive(false);
        UpdateMagnetRange();
    }

    public void TriggerExplosionEffect(CollectibleType statusEffectType)
    {
        switch (statusEffectType)
        {
            case CollectibleType.POWERUP_FREEZEALL:
                freezeExplosionEffect.TriggerExplosion(EnemiesManager.instance.ApplyGlobalFreezeEffect);
                break;
            case CollectibleType.POWERUP_POISONALL:
                poisonExplosionEffect.TriggerExplosion(EnemiesManager.instance.ApplyGlobalPoisonEffect);
                break;
            case CollectibleType.POWERUP_CURSEALL:
                curseExplosionEffect.TriggerExplosion(EnemiesManager.instance.ApplyGlobalCurseEffect);
                break;
            case CollectibleType.POWERUP_LEVELDOWNBUGS:
                levelDownBugsExplosionEffect.TriggerExplosion(null);
                break;
            case CollectibleType.POWERUP_LEVELUPBUGS:
                levelUpBugsExplosionEffect.TriggerExplosion(null);
                break;
            default:
                break;
        }
    }

    public void StopAndResetAllExplosionEffects()
    {
        freezeExplosionEffect.StopAndResetExplosion();
        poisonExplosionEffect.StopAndResetExplosion();
        curseExplosionEffect.StopAndResetExplosion();
        levelDownBugsExplosionEffect.StopAndResetExplosion();
        levelUpBugsExplosionEffect.StopAndResetExplosion();
    }

    #region Cheat codes

    public void DealWithCheatInputs()
    {
        if (GameManager.instance.isGameRunning)
        {
            // In Run Cheats
            if (rewiredPlayer.GetButtonDown(cheat_inRun_levelUp))
            {
                // Level Up
                RunManager.instance.IncreaseXP(RunManager.instance.nextLevelXp);
            }
            if (rewiredPlayer.GetButtonDown(cheat_inRun_endChapter))
            {
                // End Chapter
                if (RunManager.instance.chapterRemainingTime < 1)
                {
                    RunManager.instance.chapterRemainingTime = -9.9f;
                }
                else
                {
                    RunManager.instance.chapterRemainingTime = 0.9f;
                }
            }
            if (rewiredPlayer.GetButtonDown(cheat_inRun_spawnBugs_tier1))
            {
                // Spawn 100 tier 1 bugs
                EnemiesManager.instance.CheatSpawnRandomBugs(100, tier: 1);
            }
            if (rewiredPlayer.GetButtonDown(cheat_inRun_spawnBugs_tier2))
            {
                // Spawn 100 tier 2 bugs
                EnemiesManager.instance.CheatSpawnRandomBugs(100, tier: 2);
            }
            if (rewiredPlayer.GetButtonDown(cheat_inRun_spawnBugs_tier3))
            {
                // Spawn 100 tier 3 bugs
                EnemiesManager.instance.CheatSpawnRandomBugs(100, tier: 3, speed: 0.9f);
            }
            if (rewiredPlayer.GetButtonDown(cheat_inRun_spawnBugs_tier4))
            {
                // Spawn 100 tier 4 bugs
                EnemiesManager.instance.CheatSpawnRandomBugs(100, tier: 4, speed: 0.8f);
            }
            if (rewiredPlayer.GetButtonDown(cheat_inRun_spawnBugs_tier5))
            {
                // Spawn 100 tier 5 bugs
                EnemiesManager.instance.CheatSpawnRandomBugs(100, tier: 5, speed: 0.8f);
            }
            if (rewiredPlayer.GetButtonDown(cheat_inRun_removeBugs))
            {
                // Unspawn all bugs
                EnemiesManager.instance.ClearAllEnemies();
            }
            if (rewiredPlayer.GetButtonDown(cheat_inRun_fullHeal))
            {
                // Full Heal
                Heal(100000, cancelDamage: true);
            }
            if (rewiredPlayer.GetButtonDown(cheat_inRun_speedUp))
            {
                // Speed +10%
                walkSpeedBoost += 0.1f;
                swimSpeedBoost += 0.1f;
            }
            if (rewiredPlayer.GetButtonDown(cheat_inRun_addRerolls))
            {
                // +10 rerolls
                rerolls += 10;
                RunManager.instance.rerollsAvailable = true;
            }
            if (rewiredPlayer.GetButtonDown(cheat_inRun_megaMagnet))
            {
                // Mega Magnet!
                CollectiblesManager.instance.ApplyMegaMagnet();
            }
            if (rewiredPlayer.GetButtonDown(cheat_inRun_armorPlus))
            {
                // +1 Armor
                armor += 1;
            }
            if (rewiredPlayer.GetButtonDown(cheat_inRun_rangePlus))
            {
                // +10% range
                attackRangeBoost += 0.1f;
            }
            if (rewiredPlayer.GetButtonDown(cheat_inRun_scorePlus))
            {
                // +1000 score (kills)
                RunManager.instance.IncreaseKillCount(1000);
            }
            if (rewiredPlayer.GetButtonDown(cheat_inRun_maxHPPlus))
            {
                // +25 Max HP
                healthBar.IncreaseMaxHealth(25);
            }
            if (rewiredPlayer.GetButtonDown(cheat_inRun_addHat))
            {
                // Add a random hat
                AddHat((HatType)(Random.Range(0, 3)));
            }
            if (rewiredPlayer.GetButtonDown(cheat_inRun_removeHats))
            {
                // Clear hats
                ClearHats();
            }
            if (rewiredPlayer.GetButtonDown(cheat_inRun_addFrend))
            {
                // Add a random frend                
                FriendType friendType = (FriendType)(Random.Range(0, System.Enum.GetValues(typeof(FriendType)).Length));
                Vector2 frogPosition = new Vector2(this.transform.position.x, this.transform.position.y);
                FriendsManager.instance.AddActiveFriend(friendType, frogPosition + Random.insideUnitCircle.normalized);
            }
            if (rewiredPlayer.GetButtonDown(cheat_inRun_removeFrends))
            {
                // Clear frends
                FriendsManager.instance.ClearAllFriends();
            }
        }

        // Anytime Cheats
        if (rewiredPlayer.GetButtonDown(cheat_getFroins))
        {
            // Get 10k Froins
            GameManager.instance.ChangeAvailableCurrency(10000);
            UIManager.instance.UpdateCurrencyDisplay();
        }
        if (rewiredPlayer.GetButtonDown(cheat_unlockAllQuests))
        {
            // Unlock all quests/achievements
            AchievementManager.instance.GetUnlockedAchievementsForCurrentRun(true, true);
        }
        if (rewiredPlayer.GetButtonDown(cheat_toggleVersionNumber))
        {
            // Show/Hide version number
            UIManager.instance.ToggleVersionNumberVisible();
        }

        if (rewiredPlayer.GetButtonDown(cheat_steam_clearAchievements))
        {
            // Clear all achievements on Steam
            AchievementManager.instance.ClearAllSteamAchievements();
        }

        return;
    }

    #endregion
}
