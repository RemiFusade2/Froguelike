using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class WeaponBehaviour : MonoBehaviour
{
    [Header("References")]
    public Transform outlineTransform;
    
    [Header("Settings - appearance")]
    public float tongueWidth = 1;
    public Color outlineColor;
    public float outlineWeight = 0.1f;

    [Header("Settings - base metrics")]
    public WeaponType weaponType;
    [Space]
    public float cooldown;
    public float damage;
    public float attackSpeed;
    public float range;
    [Space]
    public bool comesBackAfterEatingFlies; // not sure we need that

    [Header("Settings - special metrics")]
    public float healthAbsorbRatio;
    [Space]
    public float poisonDamage;
    public float poisonDuration;
    [Space]
    public float freezeFactor;
    public float freezeDuration;
    [Space]
    public float curseFactor;
    public float curseDuration;

    [Header("Collision Layer")]
    public LayerMask foodLayer;


    private Color tongueColor;

    private LineRenderer tongueLineRenderer;
    private LineRenderer outlineLineRenderer;

    private Collider2D tongueCollider;

    private float lastAttackTime;

    private bool isTongueGoingOut;

    private int eatenFliesCount;

    private bool isAttacking;
    
    public static float rotatingTongueCurrentAngle = 0;
    public static int rotatingTongueCount = 0;
    public int rotatingTongueIndex;

    private Coroutine computeRotatingAngleCoroutine;

    public static void ResetStaticValues()
    {
        rotatingTongueCurrentAngle = 0;
        rotatingTongueCount = 0;
    }

    public void SetTongueColor(Color color)
    {
        tongueColor = color;
        if (tongueLineRenderer != null && outlineLineRenderer != null)
        {
            tongueLineRenderer.startColor = tongueColor;
            tongueLineRenderer.endColor = tongueColor;
            outlineLineRenderer.startColor = outlineColor;
            outlineLineRenderer.endColor = outlineColor;
        }
    }

    public void SetTongueWidth(float width)
    {
        tongueWidth = width;
        if (tongueLineRenderer != null && outlineLineRenderer != null)
        {
            tongueLineRenderer.startWidth = width;
            tongueLineRenderer.endWidth = width;
            outlineLineRenderer.startWidth = width + outlineWeight * 2;
            outlineLineRenderer.endWidth = width + outlineWeight * 2;
        }
    }

    private void SetTongueScale(float scale)
    {
        float actualRange = range * (1 + GameManager.instance.player.attackRangeBoost);
        if (scale <= 0)
        {
            this.transform.localScale = Vector3.zero;
            outlineTransform.localScale = Vector3.zero;
        }
        else
        {
            this.transform.localScale = Vector3.forward + (tongueWidth * Vector3.up) + (scale * actualRange * Vector3.right);
            outlineTransform.localPosition = (-outlineWeight / (scale * actualRange)) * Vector3.right;
            outlineTransform.localScale = Vector3.forward + Vector3.up + ((1 + ((outlineWeight*2)/(scale * actualRange))) * Vector3.right);
            SetTongueWidth(tongueWidth);
        }
    }

    private void SetTongueDirection(Vector2 direction)
    {
        float angle = -Vector2.SignedAngle(direction.normalized, Vector2.right);
        this.transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    public void SetTonguePosition(Transform weaponStartPosition)
    {
        this.transform.position = weaponStartPosition.position;
    }

    public void ResetWeapon()
    {
        isAttacking = false;
        eatenFliesCount = 0;
        isTongueGoingOut = false;
        lastAttackTime = Time.time;
        SetTongueScale(0);
        tongueLineRenderer = GetComponent<LineRenderer>();
        outlineLineRenderer = outlineTransform.GetComponent<LineRenderer>();

        switch (weaponType)
        {
            case WeaponType.VAMPIRE:
                SetTongueColor(DataManager.instance.GetColorForWeaponEffect(WeaponEffect.VAMPIRE));
                break;
            case WeaponType.POISON:
                SetTongueColor(DataManager.instance.GetColorForWeaponEffect(WeaponEffect.POISON));
                break;
            case WeaponType.FREEZE:
                SetTongueColor(DataManager.instance.GetColorForWeaponEffect(WeaponEffect.FREEZE));
                break;
            case WeaponType.CURSED:
                SetTongueColor(DataManager.instance.GetColorForWeaponEffect(WeaponEffect.CURSE));
                break;
            default:
                SetTongueColor(DataManager.instance.GetColorForWeaponEffect(WeaponEffect.NONE));
                break;
        }
    }

    public void Initialize(RunWeaponData weaponData)
    {
        weaponType = weaponData.weaponType;
        attackSpeed = (float)weaponData.weaponBaseStats.GetStatValue(WeaponStat.SPEED).value;
        cooldown = (float)weaponData.weaponBaseStats.GetStatValue(WeaponStat.COOLDOWN).value;
        damage = (float)weaponData.weaponBaseStats.GetStatValue(WeaponStat.DAMAGE).value;
        range = (float)weaponData.weaponBaseStats.GetStatValue(WeaponStat.RANGE).value;
        
        comesBackAfterEatingFlies = weaponData.comesBackAfterEatingFlies; // not sure it's needed

        freezeFactor = (float)weaponData.weaponBaseStats.GetStatValue(WeaponStat.FREEZE_RATIO).value;
        freezeDuration = (float)weaponData.weaponBaseStats.GetStatValue(WeaponStat.FREEZE_DURATION).value;

        curseFactor = (float)weaponData.weaponBaseStats.GetStatValue(WeaponStat.CURSE_RATIO).value;
        curseDuration = (float)weaponData.weaponBaseStats.GetStatValue(WeaponStat.CURSE_DURATION).value;

        healthAbsorbRatio = (float)weaponData.weaponBaseStats.GetStatValue(WeaponStat.VAMPIRE_RATIO).value;

        poisonDamage = (float)weaponData.weaponBaseStats.GetStatValue(WeaponStat.POISON_DAMAGE).value;
        poisonDuration = (float)weaponData.weaponBaseStats.GetStatValue(WeaponStat.POISON_DURATION).value;
        
        SetTongueWidth((float)weaponData.weaponBaseStats.GetStatValue(WeaponStat.WIDTH).value);

        if (weaponData.weaponType == WeaponType.ROTATING)
        {
            rotatingTongueIndex = rotatingTongueCount;
            rotatingTongueCount++;
            if (rotatingTongueIndex == 0)
            {
                if (computeRotatingAngleCoroutine != null)
                {
                    StopCoroutine(computeRotatingAngleCoroutine);
                    computeRotatingAngleCoroutine = null;
                }
                computeRotatingAngleCoroutine = StartCoroutine(ComputeRotatingTongueAngle());
            }
        }

        ResetWeapon();
    }

    public void CopyWeaponStats(WeaponBehaviour weapon)
    {
        cooldown = weapon.cooldown;
        damage = weapon.damage;
        attackSpeed = weapon.attackSpeed;
        range = weapon.range;

        SetTongueWidth(weapon.tongueWidth);

        weaponType = weapon.weaponType;
        if (weaponType == WeaponType.ROTATING)
        {
            rotatingTongueIndex = rotatingTongueCount;
            rotatingTongueCount++;
        }

        healthAbsorbRatio = weapon.healthAbsorbRatio;

        poisonDamage = weapon.poisonDamage;
        poisonDuration = weapon.poisonDuration;

        freezeFactor = weapon.freezeFactor;
        freezeDuration = weapon.freezeDuration;

        curseFactor = weapon.curseFactor;
        curseDuration = weapon.curseDuration;

        comesBackAfterEatingFlies = weapon.comesBackAfterEatingFlies;

        ResetWeapon();
    }

    public void LevelUp(RunWeaponItemLevel weaponItemLevel)
    {
        // Damage is additive
        damage += (float)weaponItemLevel.weaponStatUpgrades.GetStatValue(WeaponStat.DAMAGE).value;

        // Cooldown is multiplicative and negative
        cooldown *= (1+(float)weaponItemLevel.weaponStatUpgrades.GetStatValue(WeaponStat.COOLDOWN).value);
        
        attackSpeed *= (1+(float)weaponItemLevel.weaponStatUpgrades.GetStatValue(WeaponStat.SPEED).value);
        range *= (1+(float)weaponItemLevel.weaponStatUpgrades.GetStatValue(WeaponStat.RANGE).value);

        // Width
        float newTongueWidth = tongueWidth * (1 + (float)weaponItemLevel.weaponStatUpgrades.GetStatValue(WeaponStat.WIDTH).value);
        SetTongueWidth(newTongueWidth);

        // Special attack: Vampire
        if (healthAbsorbRatio == 0)
        {
            // First time we add a Vampire effect
            healthAbsorbRatio = (float)weaponItemLevel.weaponStatUpgrades.GetStatValue(WeaponStat.VAMPIRE_RATIO).value;
        }
        else
        {
            healthAbsorbRatio *= (1 + (float)weaponItemLevel.weaponStatUpgrades.GetStatValue(WeaponStat.VAMPIRE_RATIO).value);
        }

        // Special attack: Poison
        if (poisonDamage == 0)
        {
            // First time we add a Poison effect
            poisonDuration = (float)weaponItemLevel.weaponStatUpgrades.GetStatValue(WeaponStat.POISON_DURATION).value;
        }
        else
        {
            poisonDuration *= (1 + (float)weaponItemLevel.weaponStatUpgrades.GetStatValue(WeaponStat.POISON_DURATION).value);
        }
        poisonDamage += (float)weaponItemLevel.weaponStatUpgrades.GetStatValue(WeaponStat.POISON_DAMAGE).value;

        // Special attack: Freeze
        if (freezeFactor == 0)
        {
            // First time we add a Freeze effect
            freezeFactor = (float)weaponItemLevel.weaponStatUpgrades.GetStatValue(WeaponStat.FREEZE_RATIO).value;
            freezeDuration = (float)weaponItemLevel.weaponStatUpgrades.GetStatValue(WeaponStat.FREEZE_DURATION).value;
        }
        else
        {
            freezeFactor *= (1 + (float)weaponItemLevel.weaponStatUpgrades.GetStatValue(WeaponStat.FREEZE_RATIO).value);
            freezeDuration *= (1 + (float)weaponItemLevel.weaponStatUpgrades.GetStatValue(WeaponStat.FREEZE_DURATION).value);
        }

        // Special attack: Curse
        if (curseFactor == 0)
        {
            // First time we add a Curse effect
            curseFactor = (float)weaponItemLevel.weaponStatUpgrades.GetStatValue(WeaponStat.CURSE_RATIO).value;
            curseDuration = (float)weaponItemLevel.weaponStatUpgrades.GetStatValue(WeaponStat.CURSE_DURATION).value;
        }
        else
        {
            curseFactor *= (1 + (float)weaponItemLevel.weaponStatUpgrades.GetStatValue(WeaponStat.CURSE_RATIO).value);
            curseDuration *= (1 + (float)weaponItemLevel.weaponStatUpgrades.GetStatValue(WeaponStat.CURSE_DURATION).value);
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        SetTongueScale(0);
        lastAttackTime = Time.time;
        tongueCollider = GetComponent<Collider2D>();
        tongueLineRenderer = GetComponent<LineRenderer>();
        outlineLineRenderer = outlineTransform.GetComponent<LineRenderer>();
    }

    private GameObject GetNearestEnemy()
    {
        GameObject enemy = null;
        Vector2 playerPosition = GameManager.instance.player.transform.position;
        float actualRange = range * (1 + GameManager.instance.player.attackRangeBoost);
        Collider2D[] allColliders = Physics2D.OverlapCircleAll(playerPosition, actualRange * 1.5f, foodLayer);
        if (allColliders.Length > 0)
        {
            float shortestDistance = float.MaxValue;
            Collider2D nearestEnemy = null;
            foreach (Collider2D col in allColliders)
            {
                EnemyInstance enemyInfo = EnemiesManager.instance.GetEnemyInfo(col.gameObject.name);
                float distanceWithPlayer = Vector2.Distance(col.transform.position, playerPosition);
                if (distanceWithPlayer < shortestDistance && enemyInfo != null && enemyInfo.active)
                {
                    shortestDistance = distanceWithPlayer;
                    nearestEnemy = col;
                }
            }
            enemy = nearestEnemy.gameObject;
        }
        return enemy;
    }

    public void TryAttack()
    {
        float actualCooldown = cooldown * (1 + GameManager.instance.player.attackCooldownBoost);
        if (!isAttacking && Time.time - lastAttackTime > actualCooldown)
        {
            switch (weaponType)
            {
                // Attack random direction
                case WeaponType.RANDOM:
                    
                    // Set random color
                    List<Color> possibleColors = new List<Color>();
                    possibleColors.Add(DataManager.instance.GetColorForWeaponEffect(WeaponEffect.NONE));
                    if (healthAbsorbRatio > 0)
                    {
                        possibleColors.Add(DataManager.instance.GetColorForWeaponEffect(WeaponEffect.VAMPIRE));
                    }
                    if (poisonDamage > 0)
                    {
                        possibleColors.Add(DataManager.instance.GetColorForWeaponEffect(WeaponEffect.POISON));
                    }
                    if (freezeFactor > 0)
                    {
                        possibleColors.Add(DataManager.instance.GetColorForWeaponEffect(WeaponEffect.FREEZE));
                    }
                    if (curseFactor > 0)
                    {
                        possibleColors.Add(DataManager.instance.GetColorForWeaponEffect(WeaponEffect.CURSE));
                    }
                    SetTongueColor(possibleColors[Random.Range(0, possibleColors.Count)]);
                    Vector2 direction = Random.insideUnitCircle.normalized;
                    Attack(direction);
                    
                    break;
                // Attack rotating
                case WeaponType.ROTATING:
                    AttackRotating();
                    break;
                // Attack nearest enemy
                case WeaponType.CLASSIC:
                case WeaponType.CURSED:
                case WeaponType.FREEZE:
                case WeaponType.POISON:
                case WeaponType.QUICK:
                case WeaponType.VAMPIRE:
                case WeaponType.WIDE:
                case WeaponType.CAT:
                default:
                    
                    GameObject targetEnemy = GetNearestEnemy();
                    if (targetEnemy != null)
                    {
                        Attack(EnemiesManager.instance.GetEnemyInfo(targetEnemy.name));
                    }
                    
                    break;
            }
        }
    }

    public void AttackRotating()
    {
        eatenFliesCount = 0;
        lastAttackTime = Time.time;
        Vector2 direction = GameManager.instance.player.transform.up;
        if (tongueLineRenderer != null && outlineLineRenderer != null)
        {
            StartCoroutine(SendTongueInDirectionRotating(direction));
        }
    }

    private IEnumerator ComputeRotatingTongueAngle()
    {
        while (true)
        {
            float actualAttackSpeed = attackSpeed * (1 + GameManager.instance.player.attackSpeedBoost);
            rotatingTongueCurrentAngle += (Time.fixedDeltaTime * actualAttackSpeed * 10);
            yield return new WaitForFixedUpdate();
        }
    }

    private IEnumerator SendTongueInDirectionRotating(Vector2 direction)
    {
        isAttacking = true;
        SetTongueDirection(direction);
        float t = 0;
        isTongueGoingOut = true;
        tongueLineRenderer.enabled = true;
        outlineLineRenderer.enabled = true;
        tongueCollider.enabled = true;

        float angle = rotatingTongueCurrentAngle + (rotatingTongueIndex * 2 * Mathf.PI / rotatingTongueCount);

        float actualAttackSpeed = attackSpeed * (1 + GameManager.instance.player.attackSpeedBoost);
        while (isTongueGoingOut)
        {
            if (t <= 1)
            {
                SetTongueScale(t);
                t += Time.fixedDeltaTime;
            }
            else
            {
                SetTongueScale(1);
            }

            actualAttackSpeed = attackSpeed * (1 + GameManager.instance.player.attackSpeedBoost);

            angle = rotatingTongueCurrentAngle + (rotatingTongueIndex * 2 * Mathf.PI / rotatingTongueCount);

            SetTongueDirection((Mathf.Cos(angle) * Vector2.right + Mathf.Sin(angle) * Vector2.up).normalized);
            yield return new WaitForFixedUpdate();
        }
        while (t > 0)
        {
            SetTongueScale(t);
            t -= Time.fixedDeltaTime;

            actualAttackSpeed = attackSpeed * (1 + GameManager.instance.player.attackSpeedBoost);

            angle = rotatingTongueCurrentAngle + (rotatingTongueIndex * 2 * Mathf.PI / rotatingTongueCount);

            SetTongueDirection((Mathf.Cos(angle) * Vector2.right + Mathf.Sin(angle) * Vector2.up).normalized);
            yield return new WaitForFixedUpdate();
        }
        tongueLineRenderer.enabled = false;
        outlineLineRenderer.enabled = false;
        tongueCollider.enabled = false;
        lastAttackTime = Time.time;
        isAttacking = false;
    }

    public void Attack(Vector2 direction)
    {
        eatenFliesCount = 0;
        lastAttackTime = Time.time;
        if (tongueLineRenderer != null && outlineLineRenderer != null)
        {
            StartCoroutine(SendTongueInDirection(direction.normalized));
        }
    }

    public void Attack(EnemyInstance enemy)
    {
        eatenFliesCount = 0;
        if (enemy != null && tongueLineRenderer != null && outlineLineRenderer != null)
        {
            lastAttackTime = Time.time;
            StartCoroutine(SendTongueInDirection((enemy.enemyTransform.position - this.transform.position).normalized));
        }
    }

    private IEnumerator SendTongueInDirection(Vector2 direction)
    {
        isAttacking = true;
        SetTongueDirection(direction);
        float t = 0;
        isTongueGoingOut = true;
        tongueLineRenderer.enabled = true;
        outlineLineRenderer.enabled = true;
        tongueCollider.enabled = true;
        float actualAttackSpeed = attackSpeed * (1+GameManager.instance.player.attackSpeedBoost);
        while (isTongueGoingOut)
        {
            SetTongueScale(t);
            t += (Time.fixedDeltaTime * actualAttackSpeed);
            yield return new WaitForFixedUpdate();
            if (t >= 1)
            {
                isTongueGoingOut = false;
            }
        }
        while (t > 0)
        {
            SetTongueScale(t);
            t -= (Time.fixedDeltaTime * actualAttackSpeed * 2);
            yield return new WaitForFixedUpdate();
        }
        tongueLineRenderer.enabled = false;
        outlineLineRenderer.enabled = false;
        tongueCollider.enabled = false;
        lastAttackTime = Time.time;
        isAttacking = false;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Fly"))
        {
            // default part, for any weapon
            string enemyName = collision.gameObject.name;
            float actualDamage = damage * (1 + GameManager.instance.player.attackDamageBoost);

            // curse part, increase enemy speed
            bool isCursed = false;
            if (weaponType == WeaponType.CURSED || (weaponType == WeaponType.RANDOM && tongueColor.Equals(DataManager.instance.GetColorForWeaponEffect(WeaponEffect.CURSE))))
            {
                // Apply curseFactor as a probability of curse
                float curseProbability = curseFactor * (1 + GameManager.instance.player.attackSpecialStrengthBoost);
                if (Random.Range(0.0f, 1.0f) < curseProbability)
                {
                    isCursed = true;
                    actualDamage = 0;
                }
            }

            bool canKillEnemy = true; //  (eatenFliesCount < actualMaxFiles);

            bool enemyIsDead = EnemiesManager.instance.DamageEnemy(enemyName, actualDamage, canKillEnemy, this.transform);

            // vampire part, absorb part of damage done
            if (weaponType == WeaponType.VAMPIRE || (weaponType == WeaponType.RANDOM && tongueColor.Equals(DataManager.instance.GetColorForWeaponEffect(WeaponEffect.VAMPIRE))))
            {
                float healAmount = actualDamage * healthAbsorbRatio;
                GameManager.instance.player.Heal(healAmount);
            }

            // poison part, add poison damage to enemy
            if (weaponType == WeaponType.POISON || (weaponType == WeaponType.RANDOM && tongueColor.Equals(DataManager.instance.GetColorForWeaponEffect(WeaponEffect.POISON))))
            {
                float actualPoisonDamage = poisonDamage * (1 + GameManager.instance.player.attackSpecialStrengthBoost);
                float actualPoisonDuration = poisonDuration * (1 + GameManager.instance.player.attackSpecialDurationBoost);
                EnemiesManager.instance.AddPoisonDamageToEnemy(enemyName, actualPoisonDamage, actualPoisonDuration);
            }

            float enemySpeedChangeFactor = 0; // a factor of -1 will stop the movement, a factor of 1 will double the speed
            float enemySpeedChangeDuration = 0;
            // freeze part, diminish enemy speed
            if (weaponType == WeaponType.FREEZE || (weaponType == WeaponType.RANDOM && tongueColor.Equals(DataManager.instance.GetColorForWeaponEffect(WeaponEffect.FREEZE))))
            {
                enemySpeedChangeFactor = -freezeFactor * (1 + GameManager.instance.player.attackSpecialStrengthBoost);
                enemySpeedChangeDuration = freezeDuration * (1 + GameManager.instance.player.attackSpecialDurationBoost);
            }
            if (isCursed)
            {
                enemySpeedChangeFactor = 1;
                enemySpeedChangeDuration = curseDuration * (1 + GameManager.instance.player.attackSpecialDurationBoost);
            }

            if (enemySpeedChangeFactor != 0)
            {
                EnemiesManager.instance.ChangeEnemySpeed(enemyName, enemySpeedChangeFactor, enemySpeedChangeDuration);
            }

            if (enemyIsDead)
            {
                EnemiesManager.instance.SetEnemyDead(enemyName);
                eatenFliesCount++;
                CheckEatenFlyCount();
            }
        }
    }

    private void CheckEatenFlyCount()
    {
        /*
        float actualMaxFiles = maxFlies + GameManager.instance.player.attackMaxFliesBoost;
        if (comesBackAfterEatingFlies && eatenFliesCount >= actualMaxFiles)
        {
            isTongueGoingOut = false;
        }
        */
    }
}
