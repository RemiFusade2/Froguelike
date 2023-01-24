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
    public int style;
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
    public float defaultHealthRecovery = 0.001f;
    public float defaultWalkSpeed = 6;
    public float defaultSwimSpeed = 4;

    [Header("Character data")]
    public float walkSpeed;
    public float swimSpeed;
    [Space]
    public float currentHealth = 100;
    public float maxHealth = 100;
    public float healthRecovery = 0.01f;
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

    private Player rewiredPlayer;

    public float HorizontalInput { get; private set; }
    public float VerticalInput { get; private set; }

    [Header("Animator")]
    public Animator animator;

    [Header("Friend Frog")]
    public List<FriendInfo> allFriends;

    private List<int> activeFriendsIndexList;

    private int animatorCharacterValue;

    private Rigidbody2D playerRigidbody;

    private bool isOnLand;

    private float orientationAngle;

    private float invincibilityTime;

    private List<int> currentHatsList;

    #region Unity Callback Methods

    private void Awake()
    {
        activeFriendsIndexList = new List<int>();
        currentHatsList = new List<int>();
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

            foreach (int friendIndex in activeFriendsIndexList)
            {
                FriendInfo friend = allFriends[friendIndex];
                friend.weapon.TryAttack();
                float friendSpeed = Mathf.Clamp(friend.friendGameObject.GetComponent<Rigidbody2D>().velocity.magnitude, 0, 3);
                friend.animator.SetFloat("Speed", friendSpeed);
            }

            if (!GameManager.instance.gameIsPaused && !GameManager.instance.chapterChoiceIsVisible && !GameManager.instance.levelUpChoiceIsVisible)
            {
                ChangeHealth(healthRecovery);
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

        float moveSpeed = isOnLand ? walkSpeed : swimSpeed;
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

        foreach (int friendIndex in activeFriendsIndexList)
        {
            FriendInfo friend = allFriends[friendIndex];
            friend.weapon.TryAttack();
            float friendOrientationAngle = 90 + 90 * Mathf.RoundToInt((Vector2.SignedAngle(friend.friendGameObject.GetComponent<Rigidbody2D>().velocity.normalized, Vector2.right)) / 90);
            friend.friendGameObject.transform.localRotation = Quaternion.Euler(0, 0, -friendOrientationAngle);
            friend.weapon.SetTonguePosition(friend.weaponPositionTransform);
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

    public void InitializeCharacter(PlayableCharacterInfo characterInfo)
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
        walkSpeed = defaultWalkSpeed;
        if (allStartingStatsWrapper.GetValueForStat(STAT.WALK_SPEED_BOOST, out float startingWalkSpeedBoost))
        {
            walkSpeed *= (1 + startingWalkSpeedBoost);
        }

        // Swim speed
        swimSpeed = defaultSwimSpeed;
        if (allStartingStatsWrapper.GetValueForStat(STAT.SWIM_SPEED_BOOST, out float startingSwimSpeedBoost))
        {
            swimSpeed *= (1 + startingSwimSpeedBoost);
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


        UIManager.instance.SetExtraLives(revivals);

        currentHealth = maxHealth;
    }

    public void ResolvePickedItemLevel(ItemLevel itemLevelData)
    {
        curse += itemLevelData.curseBoost;

        // character stats
        armor += itemLevelData.armorBoost;
        experienceBoost += itemLevelData.experienceBoost;
        healthRecovery += itemLevelData.healthRecoveryBoost;
        maxHealth += itemLevelData.maxHealthBoost;
        revivals += itemLevelData.revivalBoost;

        UIManager.instance.SetExtraLives(revivals);

        GameManager.instance.currentChapter.enemiesKilledCount += itemLevelData.extraScore;
        UIManager.instance.SetEatenCount(GameManager.instance.currentChapter.enemiesKilledCount);

        if (itemLevelData.extraXP > 0)
        {
            GameManager.instance.IncreaseXP(itemLevelData.extraXP);
        }

        walkSpeed += itemLevelData.walkSpeedBoost;
        swimSpeed += itemLevelData.swimSpeedBoost;

        // attack stuff
        attackCooldownBoost += itemLevelData.attackCooldownBoost;
        attackDamageBoost += itemLevelData.attackDamageBoost;
        //attackMaxFliesBoost += itemLevelData.attackMaxFliesBoost;
        attackRangeBoost += itemLevelData.attackRangeBoost;
        attackSpeedBoost += itemLevelData.attackSpeedBoost;

        attackSpecialDurationBoost += itemLevelData.attackSpecialDurationBoost;
        attackSpecialStrengthBoost += itemLevelData.attackSpecialStrengthBoost;

        if (itemLevelData.recoverHealth > 0)
        {
            currentHealth += System.Math.Clamp(currentHealth + itemLevelData.recoverHealth, 0, maxHealth);
        }
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
        if (activeFriendsIndexList == null)
        {
            activeFriendsIndexList = new List<int>();
        }
        activeFriendsIndexList.Clear();
    }

    public bool HasActiveFriend(int index)
    {
        return activeFriendsIndexList.Contains(index);
    }

    public void AddActiveFriend(int index)
    {
        if (!HasActiveFriend(index))
        {
            activeFriendsIndexList.Add(index);
            allFriends[index].friendGameObject.SetActive(true);
            allFriends[index].weapon.gameObject.SetActive(true);
            allFriends[index].weapon.ResetWeapon();
            allFriends[index].animator.SetInteger("Style", allFriends[index].style);
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

    public void AddHat(int style)
    {
        currentHatsList.Add(style);
        foreach (SpriteRenderer hatRenderer in hatRenderersList)
        {
            if (!hatRenderer.gameObject.activeInHierarchy)
            {
                hatRenderer.gameObject.SetActive(true);
                hatRenderer.sprite = hatSpritesList[((style - 1) % hatSpritesList.Count)];
                break;
            }
        }
    }

    public bool HasHat(int style)
    {
        return currentHatsList.Contains(style);
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
            float damage = FliesManager.instance.GetEnemyDataFromName(collision.gameObject.name).damage * FliesManager.instance.enemyDamageFactor;

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
