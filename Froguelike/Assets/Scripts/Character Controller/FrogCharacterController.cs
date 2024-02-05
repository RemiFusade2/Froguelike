using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Rewired;
using System.Linq;
using static UnityEngine.ParticleSystem;

public class FrogCharacterController : MonoBehaviour
{
    [Header("Player id")]
    public int playerID;

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

    [Header("Hats")]
    public Transform hatsParent;
    public GameObject hatPrefab;
    public int hatSortingOrder = 10;

    [Header("Character data - Settings")]
    public Color characterBeingHitOverlayColor;

    [Header("Character data - God Mode Settings")]
    public float godModeWalkSpeedBoost = 1;
    public float godModeSwimSpeedBoost = 1.5f;
    public float godModeMinCooldownBoost = -0.8f;
    public float godModeAttackDamageBoost = 2;
    public float godModeAttackRangeBoost = 1.5f;
    public float godModeAttackSizeBoost = 0.5f;
    public float godModeAttackSpeedBoost = 1;
    public float godModeAttackDurationBoost = 1;
    public float godModeAttackSpecialStrengthBoost = 1;
    public float godModeAttackSpecialDurationBoost = 1;

    [Header("Character data - Runtime")]
    public float walkSpeedBoost;
    public float swimSpeedBoost;
    [Space]
    public float magnetRangeBoost = 0;
    [Space]
    public float armor = 0;
    public float experienceBoost = 0;
    public float currencyBoost = 0;
    public float curse = 0;
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
    public float attackSpecialStrengthBoost = 0;
    public float attackSpecialDurationBoost = 0;
    [Space]
    public int statItemSlotsCount;
    public int weaponSlotsCount;

    [Header("Settings - Logs")]
    public VerboseLevel logsVerboseLevel = VerboseLevel.NONE;

    [Header("Settings - controls")]
    public string horizontalInputName = "horizontal";
    public string verticalInputName = "vertical";
    public string pauseInputName = "pause";
    [Space]
    public string uiSubmitInputName = "UISubmit";
    public string uiCancelInputName = "UICancel";
    [Space]
    public float inputAxisDeadZone = 0.3f;

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

    private bool applyGodMode;
    private Coroutine setGodModeCoroutine;

    #region Unity Callback Methods

    // Start is called before the first frame update
    void Start()
    {
        isOnLand = true;
        onlyUseWalkSpeed = false;
        rewiredPlayer = ReInput.players.GetPlayer(playerID);
        playerRigidbody = GetComponent<Rigidbody2D>();
        FriendsManager.instance.ClearAllFriends();

        if (BuildManager.instance.everythingIsUnlocked)
        {
            AchievementManager.instance.GetUnlockedAchievementsForCurrentRun(true, true);
            //UIManager.instance.ShowTitleScreen();
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (BuildManager.instance.cheatsAreEnabled)
        {
            DealWithCheatInputs();
        }

        bool ignoreUICancelInput = false;

        if (GameManager.instance.isGameRunning)
        {
            // Get Pause input
            if (GetPauseInput())
            {
                GameManager.instance.TogglePause();
                ignoreUICancelInput = true;
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

    #region Accessors

    public float GetWalkSpeedBoost()
    {
        if (applyGodMode)
        {
            return walkSpeedBoost + godModeWalkSpeedBoost;
        }
        return walkSpeedBoost;
    }
    public float GetSwimSpeedBoost()
    {
        if (applyGodMode)
        {
            return swimSpeedBoost + godModeSwimSpeedBoost;
        }
        return swimSpeedBoost;
    }
    public float GetAttackCooldownBoost()
    {
        if (applyGodMode)
        {
            return Mathf.Min(godModeMinCooldownBoost, attackCooldownBoost);
        }
        return attackCooldownBoost;
    }
    public float GetAttackDamageBoost()
    {
        if (applyGodMode)
        {
            return attackDamageBoost + godModeAttackDamageBoost;
        }
        return attackDamageBoost;
    }
    public float GetAttackRangeBoost()
    {
        if (applyGodMode)
        {
            return attackRangeBoost + godModeAttackRangeBoost;
        }
        return attackRangeBoost;
    }
    public float GetAttackSizeBoost()
    {
        if (applyGodMode)
        {
            return attackSizeBoost + godModeAttackSizeBoost;
        }
        return attackSizeBoost;
    }
    public float GetAttackSpeedBoost()
    {
        if (applyGodMode)
        {
            return attackSpeedBoost + godModeAttackSpeedBoost;
        }
        return attackSpeedBoost;
    }
    public float GetAttackDurationBoost()
    {
        if (applyGodMode)
        {
            return attackDurationBoost + godModeAttackDurationBoost;
        }
        return attackDurationBoost;
    }
    public float GetAttackSpecialStrengthBoost()
    {
        if (applyGodMode)
        {
            return attackSpecialStrengthBoost + godModeAttackSpecialStrengthBoost;
        }
        return attackSpecialStrengthBoost;
    }
    public float GetAttackSpecialDurationBoost()
    {
        if (applyGodMode)
        {
            return attackSpecialDurationBoost + godModeAttackSpecialDurationBoost;
        }
        return attackSpecialDurationBoost;
    }

    #endregion Accessors

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
        applyGodMode = false;
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
        magnetTrigger.radius = (DataManager.instance.defaultMagnetRange * (1 + magnetRangeBoost));
        magnetTrigger.gameObject.SetActive(true);
        magnetTrigger.enabled = true;
    }

    public void InitializeCharacter(PlayableCharacter characterInfo)
    {
        onlyUseWalkSpeed = characterInfo.characterID.Equals("GHOST");

        // Set up animator
        SetAnimatorCharacterValue(characterInfo.characterData.characterAnimatorValue);

        // Reset sprite overlay & outline
        characterRenderer.material.SetFloat("_OverlayVisible", 0);
        characterRenderer.material.SetColor("_OverlayColor", characterBeingHitOverlayColor);
        characterRenderer.material.SetFloat("_OutlineThickness", 0);

        // Starting Stats for this character
        StatsWrapper allStartingStatsWrapper = StatsWrapper.JoinLists(characterInfo.characterStartingStats, ShopManager.instance.statsBonuses);

        if (logsVerboseLevel == VerboseLevel.MAXIMAL)
        {
            Debug.Log("Player - Initialize character with stats " + allStartingStatsWrapper.ToString());
        }

        // MAX HP should always be defined for any character
        float maxHealth = DataManager.instance.defaultMaxHP;
        if (allStartingStatsWrapper.GetValueForStat(CharacterStat.MAX_HEALTH, out float startingMaxHPBonus))
        {
            maxHealth += startingMaxHPBonus;
        }

        // HP Recovery
        float healthRecovery = DataManager.instance.defaultHealthRecovery;
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
        if (allStartingStatsWrapper.GetValueForStat(CharacterStat.WALK_SPEED_BOOST, out float startingWalkSpeedBoost))
        {
            walkSpeedBoost = startingWalkSpeedBoost;
        }

        // Swim speed
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

        // Atk Special Strength Boost
        attackSpecialStrengthBoost = 0;
        if (allStartingStatsWrapper.GetValueForStat(CharacterStat.ATK_SPECIAL_STRENGTH_BOOST, out float startingAtkSpecStrengthBoost))
        {
            attackSpecialStrengthBoost = startingAtkSpecStrengthBoost;
        }

        // Atk Special Strength Boost
        attackSpecialDurationBoost = 0;
        if (allStartingStatsWrapper.GetValueForStat(CharacterStat.ATK_SPECIAL_DURATION_BOOST, out float startingAtkSpecDurationBoost))
        {
            attackSpecialDurationBoost = startingAtkSpecDurationBoost;
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

        RunManager.instance.SetExtraLives(revivals, false);

        healthBar.ResetHealth();
    }

    public void ResolvePickedConsumableItem(RunConsumableItemData consumableData)
    {
        RunManager.instance.IncreaseCollectedCurrency(consumableData.effect.currencyBonus);

        RunManager.instance.IncreaseKillCount(consumableData.effect.scoreBonus);

        RunManager.instance.IncreaseXP(consumableData.effect.xpBonus);

        Heal(consumableData.effect.healthBonus);
    }

    public void ResolvePickedStatItemLevel(RunStatItemLevel itemLevelData)
    {
        // All of these stats could probably be stored in a better way 
        // TODO: Use a list<StatValue> instead, or the Wrapper
        curse += (float)itemLevelData.statUpgrades.GetStatValue(CharacterStat.CURSE).value;

        // character stats
        armor += (float)itemLevelData.statUpgrades.GetStatValue(CharacterStat.ARMOR).value;
        experienceBoost += (float)itemLevelData.statUpgrades.GetStatValue(CharacterStat.XP_BOOST).value;
        currencyBoost += (float)itemLevelData.statUpgrades.GetStatValue(CharacterStat.CURRENCY_BOOST).value;
        healthBar.IncreaseHealthRecovery((float)itemLevelData.statUpgrades.GetStatValue(CharacterStat.HEALTH_RECOVERY).value);
        healthBar.IncreaseMaxHealth((float)itemLevelData.statUpgrades.GetStatValue(CharacterStat.MAX_HEALTH).value);

        revivals += (int)itemLevelData.statUpgrades.GetStatValue(CharacterStat.REVIVAL).value;
        RunManager.instance.SetExtraLives(revivals, true);

        magnetRangeBoost += (float)itemLevelData.statUpgrades.GetStatValue(CharacterStat.MAGNET_RANGE_BOOST).value;
        UpdateMagnetRange();

        walkSpeedBoost += (float)itemLevelData.statUpgrades.GetStatValue(CharacterStat.WALK_SPEED_BOOST).value;
        swimSpeedBoost += (float)itemLevelData.statUpgrades.GetStatValue(CharacterStat.SWIM_SPEED_BOOST).value;

        // attack stuff
        attackCooldownBoost += (float)itemLevelData.statUpgrades.GetStatValue(CharacterStat.ATK_COOLDOWN_BOOST).value;
        attackDamageBoost += (float)itemLevelData.statUpgrades.GetStatValue(CharacterStat.ATK_DAMAGE_BOOST).value;
        attackRangeBoost += (float)itemLevelData.statUpgrades.GetStatValue(CharacterStat.ATK_RANGE_BOOST).value;
        attackSpeedBoost += (float)itemLevelData.statUpgrades.GetStatValue(CharacterStat.ATK_SPEED_BOOST).value;
        attackSizeBoost += (float)itemLevelData.statUpgrades.GetStatValue(CharacterStat.ATK_SIZE_BOOST).value;
        attackDurationBoost += (float)itemLevelData.statUpgrades.GetStatValue(CharacterStat.ATK_DURATION_BOOST).value;

        attackSpecialStrengthBoost += (float)itemLevelData.statUpgrades.GetStatValue(CharacterStat.ATK_SPECIAL_STRENGTH_BOOST).value;
        attackSpecialDurationBoost += (float)itemLevelData.statUpgrades.GetStatValue(CharacterStat.ATK_SPECIAL_DURATION_BOOST).value;

        // item and weapon slots
        statItemSlotsCount += (int)itemLevelData.statUpgrades.GetStatValue(CharacterStat.ITEM_SLOT).value;
        weaponSlotsCount += (int)itemLevelData.statUpgrades.GetStatValue(CharacterStat.WEAPON_SLOT).value;
    }

    public void Respawn()
    {
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
            enemyCanInflictDamage &= enemy.active && enemy.alive; // Enemy is active and alive
            enemyCanInflictDamage &= (Time.time - enemy.lastDamageInflictedTime) >= damageCooldown; // This enemy didn't do damage for a while
            enemyCanInflictDamage &= !applyGodMode; // God mode is off
            enemyCanInflictDamage &= !EnemiesManager.instance.IsGlobalFreezeActive(); // Global freeze is off
            enemyCanInflictDamage &= (enemy.freezeRemainingTime <= 0); // This enemy is not frozen
        }
        if (enemyCanInflictDamage)
        {
            // This enemy can inflict damage now
            enemy.lastDamageInflictedTime = Time.time;
            EnemyData enemyData = EnemiesManager.instance.GetEnemyDataFromGameObjectName(collider.gameObject.name, out BountyBug bountyBug);

            float damageFactor = 1;
            if (bountyBug != null)
            {
                damageFactor = bountyBug.damageMultiplier;
            }

            // Compute actual damage


            float damage = Mathf.Clamp((enemyData.damage * damageFactor) - armor, 0.1f, float.MaxValue);
            if (logsVerboseLevel == VerboseLevel.MAXIMAL)
            {
                Debug.Log("Player - Take damage from " + enemyData.enemyName + ": " + damage.ToString("0.00") + " HP.");
            }

            ChangeHealth(-damage);
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

    public void Heal(float healAmount)
    {
        ChangeHealth((healAmount > 0) ? healAmount : 0);
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
        characterRenderer.material.SetFloat("_OverlayVisible", 1);
        SoundManager.instance.PlayTakeDamageLoopSound();
        if (DamageTookEndOfEffectCoroutine != null)
        {
            StopCoroutine(DamageTookEndOfEffectCoroutine);
        }
        DamageTookEndOfEffectCoroutine = StartCoroutine(TakingDamageEndOfEffectAsync(0.7f));
    }

    private void ChangeHealth(float change)
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
            healthBar.IncreaseHealth(change);
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

    public void ApplyGodMode(float duration)
    {
        applyGodMode = true;
        if (setGodModeCoroutine != null)
        {
            StopCoroutine(setGodModeCoroutine);
        }
        setGodModeCoroutine = StartCoroutine(SetGodMode(false, duration));
    }

    private IEnumerator SetGodMode(bool active, float duration)
    {
        yield return new WaitForSeconds(duration);
        applyGodMode = active;
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
            default:
                break;
        }
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
                RunManager.instance.chapterRemainingTime = 0.01f;
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
                Heal(100000);
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
                // +100 score (kills)
                RunManager.instance.IncreaseKillCount(100);
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
