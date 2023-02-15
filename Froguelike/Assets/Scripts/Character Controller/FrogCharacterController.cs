using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Rewired;
using System.Linq;

[System.Serializable]
public class FriendInfo
{
    public GameObject friendGameObject;
    public Transform weaponPositionTransform;
    public WeaponBehaviour weapon;
    public Animator animator;
    public FriendType friendType;
    public int style; // for animator
    public Vector2 startPosition;
}

public class FrogCharacterController : MonoBehaviour
{
    [Header("Player id")]
    public int playerID;

    [Header("References")]
    public Transform weaponsParent;
    public Transform weaponStartPoint;
    public Transform healthBar;
    [Space]
    public List<SpriteRenderer> hatRenderersList;
    public List<Sprite> hatSpritesList;

    [Header("Character default stats values")]
    public float defaultHealthRecovery = 0.1f;
    public float defaultWalkSpeed = 6;
    public float defaultSwimSpeed = 4;

    [Header("Character data")]
    public float walkSpeedBoost;
    public float swimSpeedBoost;
    [Space]
    public float currentHealth = 100;
    public float maxHealth = 100;
    public float healthRecovery;
    public float hpRecoveryDelay = 1.0f;
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
    public float attackMaxFliesBoost = 0;
    public float attackRangeBoost = 0;
    public float attackAreaBoost = 0;
    public float attackSpeedBoost = 0;

    public float attackSpecialStrengthBoost = 0;
    public float attackSpecialDurationBoost = 0;

    [Header("Settings - controls")]
    public string horizontalInputName = "horizontal";
    public string verticalInputName = "vertical";
    public string pauseInputName = "pause";
    public string cheatPlusInputName = "cheatplus";

    private Player rewiredPlayer;

    public float HorizontalInput { get; private set; }
    public float VerticalInput { get; private set; }

    [Header("Animator")]
    public Animator animator;

    [Header("Friend Frog")]
    public List<FriendInfo> allFriends;

    private List<FriendType> activeFriendsList;

    private int animatorCharacterValue;

    private Rigidbody2D playerRigidbody;

    private bool isOnLand;

    private float orientationAngle;

    private float invincibilityTime;

    private List<HatType> currentHatsList;

    private float timeSinceLastHPRecovery;

    #region Unity Callback Methods

    private void Awake()
    {
        activeFriendsList = new List<FriendType>();
        currentHatsList = new List<HatType>();
    }

    // Start is called before the first frame update
    void Start()
    {
        isOnLand = true;
        invincibilityTime = 0;
        rewiredPlayer = ReInput.players.GetPlayer(playerID);
        playerRigidbody = GetComponent<Rigidbody2D>();
        ClearFriends();
    }

    // Update is called once per frame
    void Update()
    {
        if (GetCheatPlusInput())
        {
            GameManager.instance.ChangeAvailableCurrency(1000);
            UIManager.instance.UpdateCurrencyDisplay();
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
            foreach (FriendInfo friend in allFriends)
            {
                if (activeFriendsList.Contains(friend.friendType))
                {
                    // this friend is active!
                    friend.weapon.TryAttack();
                    float friendSpeed = Mathf.Clamp(friend.friendGameObject.GetComponent<Rigidbody2D>().velocity.magnitude, 0, 3);
                    friend.animator.SetFloat("Speed", friendSpeed);
                }
            }

            if (!GameManager.instance.gameIsPaused && !ChapterManager.instance.chapterChoiceIsVisible && !RunManager.instance.levelUpChoiceIsVisible && (Time.time - timeSinceLastHPRecovery) > hpRecoveryDelay)
            {
                ChangeHealth(healthRecovery);
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

        float moveSpeed = isOnLand ? (defaultWalkSpeed * (1+walkSpeedBoost)) : (defaultSwimSpeed * (1 + swimSpeedBoost));
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
        foreach (FriendInfo friend in allFriends)
        {
            if (activeFriendsList.Contains(friend.friendType))
            {
                // this friend is active!
                friend.weapon.TryAttack();
                float friendOrientationAngle = 90 + 90 * Mathf.RoundToInt((Vector2.SignedAngle(friend.friendGameObject.GetComponent<Rigidbody2D>().velocity.normalized, Vector2.right)) / 90);
                friend.friendGameObject.transform.localRotation = Quaternion.Euler(0, 0, -friendOrientationAngle);
                friend.weapon.SetTonguePosition(friend.weaponPositionTransform);
            }
        }
    }

    #endregion

    public void ResetPosition()
    {
        transform.localPosition = Vector3.zero;
        foreach (FriendInfo friend in allFriends)
        {
            friend.friendGameObject.transform.localPosition = friend.startPosition;
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

    public void InitializeCharacter(PlayableCharacter characterInfo)
    {        
        SetAnimatorCharacterValue(characterInfo.characterData.characterAnimatorValue);

        // Starting Stats for this character
        StatsWrapper allStartingStatsWrapper = StatsWrapper.JoinLists(characterInfo.characterStartingStats, ShopManager.instance.statsBonuses);

        // MAX HP should always be defined for any character
        maxHealth = 0;
        if (allStartingStatsWrapper.GetValueForStat(STAT.MAX_HEALTH, out float startingMaxHP))
        {
            maxHealth = startingMaxHP;
        }
        else
        {
            Debug.LogError("Max Health was not defined for this character");
        }

        // HP Recovery
        healthRecovery = defaultHealthRecovery;
        if (allStartingStatsWrapper.GetValueForStat(STAT.HEALTH_RECOVERY_BOOST, out float startingHPRecoveryBoost))
        {
            healthRecovery *= (1 + startingHPRecoveryBoost);
        }
        timeSinceLastHPRecovery = Time.time;

        // Armor
        armor = 0;
        if (allStartingStatsWrapper.GetValueForStat(STAT.ARMOR, out float startingArmor))
        {
            armor = startingArmor;
        }

        // Experience boost
        experienceBoost = 0;
        if (allStartingStatsWrapper.GetValueForStat(STAT.XP_BOOST, out float startingXPBoost))
        {
            experienceBoost = startingXPBoost;
        }

        // Currency boost
        currencyBoost = 0;
        if (allStartingStatsWrapper.GetValueForStat(STAT.CURRENCY_BOOST, out float startingCurrencyBoost))
        {
            currencyBoost = startingCurrencyBoost;
        }

        // Curse boost
        curse = 0;
        if (allStartingStatsWrapper.GetValueForStat(STAT.CURSE, out float startingCurse))
        {
            curse = startingCurse;
        }

        // Walk speed
        if (allStartingStatsWrapper.GetValueForStat(STAT.WALK_SPEED_BOOST, out float startingWalkSpeedBoost))
        {
            walkSpeedBoost = startingWalkSpeedBoost;
        }

        // Swim speed
        if (allStartingStatsWrapper.GetValueForStat(STAT.SWIM_SPEED_BOOST, out float startingSwimSpeedBoost))
        {
            swimSpeedBoost = startingSwimSpeedBoost;
        }

        // Revivals
        revivals = 0;
        if (allStartingStatsWrapper.GetValueForStat(STAT.REVIVAL, out float startingRevivals))
        {
            revivals = Mathf.FloorToInt(startingRevivals);
        }

        // Rerolls
        rerolls = 0;
        if (allStartingStatsWrapper.GetValueForStat(STAT.REROLL, out float startingRerolls))
        {
            rerolls = Mathf.FloorToInt(startingRerolls);
        }

        // Banishs
        banishs = 0;
        if (allStartingStatsWrapper.GetValueForStat(STAT.BANISH, out float startingBanishs))
        {
            banishs = Mathf.FloorToInt(startingBanishs);
        }

        // Skips
        skips = 0;
        if (allStartingStatsWrapper.GetValueForStat(STAT.SKIP, out float startingSkips))
        {
            skips = Mathf.FloorToInt(startingSkips);
        }

        // Deprecated: Atk max flies boost
        attackMaxFliesBoost = 0;

        // Atk Damage Boost
        attackDamageBoost = 0;
        if (allStartingStatsWrapper.GetValueForStat(STAT.ATK_DAMAGE_BOOST, out float startingAtkDmgBoost))
        {
            attackDamageBoost = startingAtkDmgBoost;
        }

        // Atk Speed Boost
        attackSpeedBoost = 0;
        if (allStartingStatsWrapper.GetValueForStat(STAT.ATK_SPEED_BOOST, out float startingAtkSpeedBoost))
        {
            attackSpeedBoost = startingAtkSpeedBoost;
        }

        // Atk Cooldown Boost
        attackCooldownBoost = 0;
        if (allStartingStatsWrapper.GetValueForStat(STAT.ATK_COOLDOWN_BOOST, out float startingAtkCooldownBoost))
        {
            attackCooldownBoost = startingAtkCooldownBoost;
        }

        // Atk Range Boost
        attackRangeBoost = 0;
        if (allStartingStatsWrapper.GetValueForStat(STAT.ATK_RANGE_BOOST, out float startingAtkRangeBoost))
        {
            attackRangeBoost = startingAtkRangeBoost;
        }

        // Atk Area Boost
        attackAreaBoost = 0;
        if (allStartingStatsWrapper.GetValueForStat(STAT.ATK_AREA_BOOST, out float startingAtkAreaBoost))
        {
            attackAreaBoost = startingAtkAreaBoost;
        }

        // Atk Special Strength Boost
        attackSpecialStrengthBoost = 0;
        if (allStartingStatsWrapper.GetValueForStat(STAT.ATK_SPECIAL_STRENGTH_BOOST, out float startingAtkSpecStrengthBoost))
        {
            attackSpecialStrengthBoost = startingAtkSpecStrengthBoost;
        }

        // Atk Special Strength Boost
        attackSpecialDurationBoost = 0;
        if (allStartingStatsWrapper.GetValueForStat(STAT.ATK_SPECIAL_DURATION_BOOST, out float startingAtkSpecDurationBoost))
        {
            attackSpecialDurationBoost = startingAtkSpecDurationBoost;
        }

        // Magnet Range
        // TO DO


        RunManager.instance.SetExtraLives(revivals);

        currentHealth = maxHealth;
    }

    public void ResolvePickedConsumableItem(RunConsumableItemData consumableData)
    {
        RunManager.instance.currentCollectedCurrency += consumableData.effect.currencyBonus; // TODO: Use a method to increase currency and update UI

        //RunManager.instance.enemiesKilledCount += consumableData.effect.scoreBonus; // TODO: Store somewhere the current kill count for this chapter
        // UIManager.instance.SetEatenCount(RunManager.instance.currentChapter.enemiesKilledCount);

        RunManager.instance.IncreaseXP(consumableData.effect.xpBonus);

        Heal(consumableData.effect.healthBonus);        
    }
    
    public void ResolvePickedStatItemLevel(RunStatItemLevel itemLevelData)
    {
        // All of these stats could probably be stored in a better way 
        // TODO: Use a list<StatValue> instead, or the Wrapper
        curse += (float)itemLevelData.statUpgrades.GetStatValue(STAT.CURSE).value;

        // character stats
        armor += (float)itemLevelData.statUpgrades.GetStatValue(STAT.ARMOR).value;
        experienceBoost += (float)itemLevelData.statUpgrades.GetStatValue(STAT.XP_BOOST).value;
        currencyBoost += (float)itemLevelData.statUpgrades.GetStatValue(STAT.CURRENCY_BOOST).value;
        healthRecovery += (float)itemLevelData.statUpgrades.GetStatValue(STAT.HEALTH_RECOVERY_BOOST).value;
        maxHealth += (float)itemLevelData.statUpgrades.GetStatValue(STAT.MAX_HEALTH).value;
        revivals += (int)itemLevelData.statUpgrades.GetStatValue(STAT.REVIVAL).value;

        RunManager.instance.SetExtraLives(revivals);

        walkSpeedBoost += (float)itemLevelData.statUpgrades.GetStatValue(STAT.WALK_SPEED_BOOST).value;
        swimSpeedBoost += (float)itemLevelData.statUpgrades.GetStatValue(STAT.SWIM_SPEED_BOOST).value;

        // attack stuff
        attackCooldownBoost += (float)itemLevelData.statUpgrades.GetStatValue(STAT.ATK_COOLDOWN_BOOST).value;
        attackDamageBoost += (float)itemLevelData.statUpgrades.GetStatValue(STAT.ATK_DAMAGE_BOOST).value;
        attackRangeBoost += (float)itemLevelData.statUpgrades.GetStatValue(STAT.ATK_RANGE_BOOST).value;
        attackSpeedBoost += (float)itemLevelData.statUpgrades.GetStatValue(STAT.ATK_SPEED_BOOST).value;

        attackSpecialStrengthBoost += (float)itemLevelData.statUpgrades.GetStatValue(STAT.ATK_SPECIAL_STRENGTH_BOOST).value;
        attackSpecialDurationBoost += (float)itemLevelData.statUpgrades.GetStatValue(STAT.ATK_SPECIAL_DURATION_BOOST).value;
    }


    public void Respawn()
    {
        currentHealth = maxHealth;
        UpdateHealthBar();
        invincibilityTime = 1;
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
        foreach (FriendInfo friend in allFriends)
        {
            friend.friendGameObject.SetActive(false);
            friend.weapon.gameObject.SetActive(false);
        }
        if (activeFriendsList == null)
        {
            activeFriendsList = new List<FriendType>();
        }
        activeFriendsList.Clear();
    }

    public bool HasActiveFriend(FriendType friendType)
    {
        return activeFriendsList.Contains(friendType);
    }

    public void AddActiveFriend(FriendType friendType)
    {
        if (!HasActiveFriend(friendType))
        {
            activeFriendsList.Add(friendType);
            foreach (FriendInfo friend in allFriends)
            {
                if (friend.friendType.Equals(friendType))
                {
                    friend.friendGameObject.SetActive(true);
                    friend.weapon.gameObject.SetActive(true);
                    friend.weapon.ResetWeapon();
                    friend.animator.SetInteger("Style", friend.style);
                }
            }
        }
    }

    public void ClearHats()
    {
        currentHatsList.Clear();
        foreach (SpriteRenderer hatRenderer in hatRenderersList)
        {
            hatRenderer.gameObject.SetActive(false);
        }
    }

    public void AddHat(HatType hatType)
    {
        currentHatsList.Add(hatType);
        foreach (SpriteRenderer hatRenderer in hatRenderersList)
        {
            if (!hatRenderer.gameObject.activeInHierarchy)
            {
                hatRenderer.gameObject.SetActive(true);
                switch (hatType)
                {
                    case HatType.FANCY_HAT:
                        hatRenderer.sprite = hatSpritesList[0 % hatSpritesList.Count];
                        break;
                    case HatType.FASHION_HAT:
                        hatRenderer.sprite = hatSpritesList[2 % hatSpritesList.Count];
                        break;
                    case HatType.SUN_HAT:
                        hatRenderer.sprite = hatSpritesList[1 % hatSpritesList.Count];
                        break;
                }
                break;
            }
        }
    }

    public bool HasHat(HatType hatType)
    {
        return currentHatsList.Contains(hatType);
    }

    #region Update Inputs

    private void UpdateHorizontalInput()
    {
        HorizontalInput = rewiredPlayer.GetAxis(horizontalInputName);
    }

    private void UpdateVerticalInput()
    {
        VerticalInput = rewiredPlayer.GetAxis(verticalInputName);
    }

    private bool GetPauseInput()
    {
        return rewiredPlayer.GetButtonDown(pauseInputName);
    }

    private bool GetCheatPlusInput()
    {
        return rewiredPlayer.GetButtonDown(cheatPlusInputName);
    }

    #endregion


    private void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.CompareTag("Water"))
        {
            isOnLand = false;
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
        if (collision.collider.CompareTag("Fly") && GameManager.instance.isGameRunning && invincibilityTime <= 0)
        {
            float damage = EnemiesManager.instance.GetEnemyDataFromName(collision.gameObject.name).damage * EnemiesManager.instance.enemyDamageFactor;

            damage = Mathf.Clamp(damage - armor, 0.1f, float.MaxValue);
            ChangeHealth(-damage);
        }
    }

    public void Heal(float healAmount)
    {
        ChangeHealth((healAmount > 0) ? healAmount : 0);
    }

    private void ChangeHealth(float change)
    {
        currentHealth += change;
        if (currentHealth >= maxHealth)
        {
            currentHealth = maxHealth;
        }
        if (currentHealth <= 0)
        {
            // game over
            GameManager.instance.TriggerGameOver();
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
