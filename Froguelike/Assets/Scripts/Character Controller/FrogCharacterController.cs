using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Rewired;
using System.Linq;


public class FrogCharacterController : MonoBehaviour
{
    [Header("Player id")]
    public int playerID;

    [Header("References")]
    public Transform weaponsParent;
    public Transform weaponStartPoint;
    public Transform healthBar;
    [Space]
    public CircleCollider2D magnetTrigger;
    [Space]
    public Animator animator;

    [Header("Hats")]
    public Transform hatsParent;
    public GameObject hatPrefab;
    public int hatSortingOrder = 10;

    [Header("Character data")]
    public float walkSpeedBoost;
    public float swimSpeedBoost;
    [Space]
    public float currentHealth = 100;
    public float maxHealth = 100;
    public float healthRecovery;
    public float hpRecoveryDelay = 1.0f;
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
    public string levelUpCheatInputName = "levelupcheat";
    public string froinsCheatInputName = "froinscheat";
    public string skipChapterCheatInputName = "skipchaptercheat";
    public string unlockAllCheatInputName = "unlockallcheat";
    [Space]
    public float inputAxisDeadZone = 0.3f;
    
    [Header("Friend Frogs")]
    public Transform friendsParent;


    private Player rewiredPlayer;
    public float HorizontalInput { get; private set; }
    public float VerticalInput { get; private set; }

    private int animatorCharacterValue;

    private Rigidbody2D playerRigidbody;

    private bool isOnLand;

    private float orientationAngle;

    private float invincibilityTime;

    private float timeSinceLastHPRecovery;

    #region Unity Callback Methods

    // Start is called before the first frame update
    void Start()
    {
        isOnLand = true;
        invincibilityTime = 0;
        rewiredPlayer = ReInput.players.GetPlayer(playerID);
        playerRigidbody = GetComponent<Rigidbody2D>();
        ClearFriends();

        if (GameManager.instance.everythingIsUnlocked)
        {
            AchievementManager.instance.GetUnlockedAchievementsForCurrentRun(true);
            UIManager.instance.ShowTitleScreen();
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (GameManager.instance.cheatsAreEnabled && GetFroinsCheatInput())
        {
            GameManager.instance.ChangeAvailableCurrency(10000);
            UIManager.instance.UpdateCurrencyDisplay();
        }
        if (GameManager.instance.cheatsAreEnabled && GetLevelUpCheatInput())
        {
            if (GameManager.instance.isGameRunning)
            {
                RunManager.instance.IncreaseXP(RunManager.instance.nextLevelXp);
            }
        }
        if (GameManager.instance.cheatsAreEnabled && GetSkipChapterCheatInput())
        {
            if (GameManager.instance.isGameRunning)
            {
                RunManager.instance.chapterRemainingTime = 0.1f;
            }
        }
        if (GameManager.instance.cheatsAreEnabled && GetUnlockAllCheatInput())
        {
            AchievementManager.instance.GetUnlockedAchievementsForCurrentRun(true);
        }

        if (GameManager.instance.isGameRunning)
        {

            if (GetPauseInput())
            {
                GameManager.instance.TogglePause();
            }

            foreach (Transform weaponTransform in weaponsParent)
            {
                weaponTransform.GetComponent<WeaponBehaviour>().TryAttack();
            }

            // Make friends attack!
            foreach (Transform friend in friendsParent)
            {
                FriendBehaviour friendScript = friend.GetComponent<FriendBehaviour>();
                friendScript.TryAttack();
                friendScript.ClampSpeed();
            }

            if (!GameManager.instance.gameIsPaused && !ChapterManager.instance.chapterChoiceIsVisible && !RunManager.instance.levelUpChoiceIsVisible && (Time.time - timeSinceLastHPRecovery) > hpRecoveryDelay)
            {
                Heal(healthRecovery);
                timeSinceLastHPRecovery = Time.time;
            }

            float speed = Mathf.Clamp(playerRigidbody.velocity.magnitude, 0, 10);
            animator.SetFloat("Speed", speed);
        }
    }

    private void FixedUpdate()
    {
        UpdateHorizontalInput();
        UpdateVerticalInput();

        if (invincibilityTime > 0)
        {
            invincibilityTime -= Time.fixedDeltaTime;
        }

        float moveSpeed = isOnLand ? (DataManager.instance.defaultWalkSpeed * (1+walkSpeedBoost)) : (DataManager.instance.defaultSwimSpeed * (1 + swimSpeedBoost));
        Vector2 moveInput = (((HorizontalInput * Vector2.right).normalized + (VerticalInput * Vector2.up).normalized)).normalized * moveSpeed;

        if (!moveInput.Equals(Vector2.zero))
        {
            orientationAngle = 90 + 90 * Mathf.RoundToInt((Vector2.SignedAngle(moveInput, Vector2.right)) / 90);
            transform.localRotation = Quaternion.Euler(0, 0, -orientationAngle);
        }

        playerRigidbody.velocity = moveInput;

        foreach (Transform weaponTransform in weaponsParent)
        {
            weaponTransform.GetComponent<WeaponBehaviour>().SetTonguePosition(weaponStartPoint);
        }

        // Make friends move
        foreach (Transform friend in friendsParent)
        {
            FriendBehaviour friendScript = friend.GetComponent<FriendBehaviour>();
            friendScript.TryAttack();
            friendScript.UpdateOrientation();
        }
    }

    #endregion

    public Rigidbody2D GetRigidbody()
    {
        return playerRigidbody;
    }

    public Vector2 GetVelocity()
    {
        return new Vector2(HorizontalInput, VerticalInput);
        // return playerRigidbody.velocity;
    }

    public void ResetPosition()
    {
        isOnLand = true;
        transform.localPosition = Vector3.zero;
        int numberOfActiveFriends = friendsParent.childCount;
        float deltaAngle = 0;
        float distance = 2.5f;
        if (numberOfActiveFriends > 0)
        {
            deltaAngle = (Mathf.PI * 2) / numberOfActiveFriends;
        }
        float angle = 0;
        foreach (Transform friend in friendsParent)
        {
            FriendBehaviour friendScript = friend.GetComponent<FriendBehaviour>();
            friendScript.SetPosition(distance * (Mathf.Cos(angle) * Vector2.right + Mathf.Sin(angle) * Vector2.up));
            angle += deltaAngle;
        }
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
    }

    public void InitializeCharacter(PlayableCharacter characterInfo)
    {
        SetAnimatorCharacterValue(characterInfo.characterData.characterAnimatorValue);

        // Starting Stats for this character
        StatsWrapper allStartingStatsWrapper = StatsWrapper.JoinLists(characterInfo.characterStartingStats, ShopManager.instance.statsBonuses);

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
        timeSinceLastHPRecovery = Time.time;

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

        currentHealth = maxHealth;
        UpdateHealthBar();
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
        healthRecovery += (float)itemLevelData.statUpgrades.GetStatValue(CharacterStat.HEALTH_RECOVERY).value;
        maxHealth += (float)itemLevelData.statUpgrades.GetStatValue(CharacterStat.MAX_HEALTH).value;

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

        UpdateHealthBar();
    }

    public void Respawn()
    {
        currentHealth = maxHealth;
        UpdateHealthBar();
        invincibilityTime = 1;

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

    public void ClearFriends()
    {
        foreach (Transform friend in friendsParent)
        {
            Destroy(friend.gameObject);
        }
    }

    public bool HasActiveFriend(FriendType friendType)
    {
        bool friendIsActive = false;
        foreach (Transform friend in friendsParent)
        {
            FriendBehaviour friendScript = friend.GetComponent<FriendBehaviour>();
            friendIsActive |= (friendScript.GetFriendType() == friendType);
            if (friendIsActive) break;
        }
        return friendIsActive;
    }

    public void AddActiveFriend(FriendType friendType, Vector2 friendPosition)
    {
        FriendInfo friendInfo = DataManager.instance.GetInfoForFriend(friendType);

        GameObject newFriend = Instantiate(friendInfo.prefab, friendsParent);
        FriendBehaviour newFriendScript = newFriend.GetComponent<FriendBehaviour>();
        newFriendScript.Initialize(friendInfo, friendPosition);

        if (logsVerboseLevel == VerboseLevel.MAXIMAL)
        {
            Debug.Log($"Player - Add Friend: {friendType.ToString()} at position {friendPosition.ToString()}");
        }
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

    private bool GetLevelUpCheatInput()
    {
        return rewiredPlayer.GetButtonDown(levelUpCheatInputName);
    }

    private bool GetFroinsCheatInput()
    {
        return rewiredPlayer.GetButtonDown(froinsCheatInputName);
    }

    private bool GetSkipChapterCheatInput()
    {
        return rewiredPlayer.GetButtonDown(skipChapterCheatInputName);
    }

    private bool GetUnlockAllCheatInput()
    {
        return rewiredPlayer.GetButtonDown(unlockAllCheatInputName);
    }

    #endregion

    // This method is called every physics frame when colliders are touching or player collider is in enemy trigger
    private void DealWithEnemyCollision(Collider2D collider)
    {
        // Get data about attacking enemy
        EnemyInstance enemy = EnemiesManager.instance.GetEnemyInstanceFromGameObjectName(collider.gameObject.name);

        float damageCooldown = 1.0f;
        if (Time.time - enemy.lastDamageInflictedTime >= damageCooldown)
        {
            // This enemy can inflict damage now
            enemy.lastDamageInflictedTime = Time.time;
            EnemyData enemyData = EnemiesManager.instance.GetEnemyDataFromGameObjectName(collider.gameObject.name);

            // Compute actual damage
            float damage = Mathf.Clamp(enemyData.damage - armor, 0.1f, float.MaxValue);

            if (logsVerboseLevel == VerboseLevel.MAXIMAL)
            {
                Debug.Log("Player - Take damage from " + enemyData.enemyName + ": " + damage.ToString("0.00") + " HP. Current health was: " + currentHealth.ToString("0.00") + " HP; new health is: " + (currentHealth - damage).ToString("0.00") + " HP");
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
        if (collider.CompareTag("Enemy") && GameManager.instance.isGameRunning && invincibilityTime <= 0)
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
        if (collision.collider.CompareTag("Enemy") && GameManager.instance.isGameRunning && invincibilityTime <= 0)
        {
            DealWithEnemyCollision(collision.collider);
        }
    }

    public void Heal(float healAmount)
    {
        ChangeHealth((healAmount > 0) ? healAmount : 0);
        if (logsVerboseLevel == VerboseLevel.MAXIMAL)
        {
            Debug.Log("Player - Healing: +" + healAmount + "HP. Current HP is " + currentHealth.ToString("0.00") + " HP, max HP is " + maxHealth.ToString("0.00") + " HP.");
        }
    }

    private void ChangeHealth(float change)
    {
        bool deathImminent = (currentHealth < 5); 

        currentHealth += change;
        if (currentHealth >= maxHealth)
        {
            currentHealth = maxHealth;
        }
        if (currentHealth <= 0)
        {
            if (deathImminent)
            {
                // If frog was low HP and took enough damage to die, then it's game over
                GameManager.instance.TriggerGameOver();
            }
            else
            {
                // If frog had enough HP and took enough damage to die, then it's second chance time! 2s invicibility and 0.1 HP left!
                currentHealth = 0.1f;
                invincibilityTime = 2;
            }
        }
        UpdateHealthBar();
    }

    private void UpdateHealthBar()
    {
        float healthRatio = currentHealth / maxHealth;
        if (healthRatio < 0)
        {
            healthRatio = 0;
        }
        healthBar.localScale = healthRatio * Vector3.right + healthBar.localScale.y * Vector3.up + healthBar.localScale.z * Vector3.forward;
    }
}
