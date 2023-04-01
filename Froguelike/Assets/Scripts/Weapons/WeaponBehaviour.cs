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
    public float duration;
    [Space]
    public float knockbackForce;

    [Header("Settings - special metrics")]
    public float healthAbsorbRatio;
    [Space]
    public float poisonDamage;
    [Space]
    public bool freezeEffect;
    public float curseChance;

    [Header("Collision Layer")]
    public LayerMask foodLayer;

    [Header("Runtime")]
    public float lastAttackTime;
    public bool isAttacking;
    public bool preventAttack;
    [Space]
    public int tongueTypeIndex; // Index of this tongue
    public int tongueTypeCount; // Number of tongues of the same type
    [Space]
    public List<GameObject> activeWeaponsOfSameTypeList;

    private Color tongueColor;

    private LineRenderer tongueLineRenderer;
    private LineRenderer outlineLineRenderer;

    private Collider2D tongueCollider;

    private bool isTongueGoingOut;

    private int eatenFliesCount;

    private List<string> enemiesHitNamesList;

    // Start is called before the first frame update
    void Start()
    {
        SetTongueScale(0);
        lastAttackTime = Time.time;
        tongueCollider = GetComponent<Collider2D>();
        tongueLineRenderer = GetComponent<LineRenderer>();
        outlineLineRenderer = outlineTransform.GetComponent<LineRenderer>();
        enemiesHitNamesList = new List<string>();
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
        float actualWidth = tongueWidth * (1 + GameManager.instance.player.attackSizeBoost);
        if (tongueLineRenderer != null && outlineLineRenderer != null)
        {
            tongueLineRenderer.widthMultiplier = actualWidth;
            outlineLineRenderer.widthMultiplier = actualWidth + outlineWeight * 2;
            /*tongueLineRenderer.startWidth = actualWidth;
            tongueLineRenderer.endWidth = actualWidth;
            outlineLineRenderer.startWidth = actualWidth + outlineWeight * 2;
            outlineLineRenderer.endWidth = actualWidth + outlineWeight * 2;*/
        }
    }

    private void SetTongueScale(float scale)
    {
        float actualRange = range * (1 + GameManager.instance.player.attackRangeBoost);
        float actualWidth = tongueWidth * (1 + GameManager.instance.player.attackSizeBoost);
        if (scale <= 0)
        {
            this.transform.localScale = Vector3.zero;
            outlineTransform.localScale = Vector3.zero;
        }
        else
        {
            this.transform.localScale = Vector3.forward + (actualWidth * Vector3.up) + (scale * actualRange * Vector3.right);
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

    public void Initialize(RunWeaponData tongueData, List<GameObject> activeWeapons)
    {
        weaponType = tongueData.weaponType;
        attackSpeed = (float)tongueData.weaponBaseStats.GetStatValue(WeaponStat.SPEED).value;
        cooldown = (float)tongueData.weaponBaseStats.GetStatValue(WeaponStat.COOLDOWN).value;
        damage = (float)tongueData.weaponBaseStats.GetStatValue(WeaponStat.DAMAGE).value;
        range = (float)tongueData.weaponBaseStats.GetStatValue(WeaponStat.RANGE).value;
        duration = (float)tongueData.weaponBaseStats.GetStatValue(WeaponStat.DURATION).value;

        knockbackForce = (float)tongueData.weaponBaseStats.GetStatValue(WeaponStat.KNOCKBACK).value;

        healthAbsorbRatio = (float)tongueData.weaponBaseStats.GetStatValue(WeaponStat.VAMPIRE_RATIO).value;
        poisonDamage = (float)tongueData.weaponBaseStats.GetStatValue(WeaponStat.POISON_DAMAGE).value;
        freezeEffect = (float)tongueData.weaponBaseStats.GetStatValue(WeaponStat.FREEZE_EFFECT).value > 0;
        curseChance = (float)tongueData.weaponBaseStats.GetStatValue(WeaponStat.CURSE_EFFECT).value;

        SetTongueWidth((float)tongueData.weaponBaseStats.GetStatValue(WeaponStat.SIZE).value);

        activeWeaponsOfSameTypeList = activeWeapons;
        tongueTypeIndex = 0;
        tongueTypeCount = 1;

        ResetWeapon();
    }

    public void CopyWeaponStats(List<GameObject> otherWeaponsOfTheSameType)
    {
        tongueTypeCount = otherWeaponsOfTheSameType.Count+1;
        WeaponBehaviour weapon = null;
        foreach (GameObject weaponGo in otherWeaponsOfTheSameType)
        {
            weapon = weaponGo.GetComponent<WeaponBehaviour>();
            weapon.tongueTypeCount = tongueTypeCount;
            weapon.activeWeaponsOfSameTypeList = otherWeaponsOfTheSameType;
        }

        cooldown = weapon.cooldown;
        damage = weapon.damage;
        attackSpeed = weapon.attackSpeed;
        range = weapon.range;
        duration = weapon.duration;

        tongueTypeIndex = tongueTypeCount-1;
        activeWeaponsOfSameTypeList = otherWeaponsOfTheSameType;

        knockbackForce = weapon.knockbackForce;

        lastAttackTime = float.MaxValue;

        SetTongueWidth(weapon.tongueWidth);

        weaponType = weapon.weaponType;

        healthAbsorbRatio = weapon.healthAbsorbRatio;
        poisonDamage = weapon.poisonDamage;
        freezeEffect = weapon.freezeEffect;
        curseChance = weapon.curseChance;

        ResetWeapon();

        preventAttack = true; // so this new tongue will not attack before the previous one stops it attack first
    }

    public void LevelUp(RunWeaponItemLevel weaponItemLevel)
    {
        // Damage is additive
        damage += (float)weaponItemLevel.weaponStatUpgrades.GetStatValue(WeaponStat.DAMAGE).value;

        // Cooldown is multiplicative and negative
        cooldown *= (1+(float)weaponItemLevel.weaponStatUpgrades.GetStatValue(WeaponStat.COOLDOWN).value);
        
        attackSpeed *= (1+(float)weaponItemLevel.weaponStatUpgrades.GetStatValue(WeaponStat.SPEED).value);
        range *= (1+(float)weaponItemLevel.weaponStatUpgrades.GetStatValue(WeaponStat.RANGE).value);

        duration *= (1 + (float)weaponItemLevel.weaponStatUpgrades.GetStatValue(WeaponStat.DURATION).value);

        knockbackForce *= (1 + (float)weaponItemLevel.weaponStatUpgrades.GetStatValue(WeaponStat.KNOCKBACK).value);

        // Width
        float newTongueWidth = tongueWidth * (1 + (float)weaponItemLevel.weaponStatUpgrades.GetStatValue(WeaponStat.SIZE).value);
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
        poisonDamage += (float)weaponItemLevel.weaponStatUpgrades.GetStatValue(WeaponStat.POISON_DAMAGE).value;

        // Special attack: Freeze
        if (!freezeEffect)
        {
            // First time we add a Freeze effect
            freezeEffect = (weaponItemLevel.weaponStatUpgrades.GetStatValue(WeaponStat.FREEZE_EFFECT).value > 0);
        }

        // Special attack: Curse        
        if (curseChance <= 0)
        {
            // First time we add a Curse effect
            curseChance = (float)weaponItemLevel.weaponStatUpgrades.GetStatValue(WeaponStat.CURSE_EFFECT).value;
        }
        else
        {
            curseChance *= (1 + (float)weaponItemLevel.weaponStatUpgrades.GetStatValue(WeaponStat.CURSE_EFFECT).value);
        }
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
                EnemyInstance enemyInfo = EnemiesManager.instance.GetEnemyInstanceFromGameObjectName(col.gameObject.name);
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
        if (!preventAttack && !isAttacking && Time.time - lastAttackTime > actualCooldown)
        {
            switch (weaponType)
            {
                // Attack random direction
                case WeaponType.RANDOM:
                    
                    // Set random color
                    List<Color> possibleColors = new List<Color>();
                    //possibleColors.Add(DataManager.instance.GetColorForWeaponEffect(WeaponEffect.NONE)); // No effect, I was thinking maybe the rando-tongue should always have a special effect
                    if (healthAbsorbRatio > 0)
                    {
                        possibleColors.Add(DataManager.instance.GetColorForWeaponEffect(WeaponEffect.VAMPIRE));
                    }
                    if (poisonDamage > 0)
                    {
                        possibleColors.Add(DataManager.instance.GetColorForWeaponEffect(WeaponEffect.POISON));
                    }
                    if (freezeEffect)
                    {
                        possibleColors.Add(DataManager.instance.GetColorForWeaponEffect(WeaponEffect.FREEZE));
                    }
                    if (curseChance > 0)
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
                // Attack in facing direction
                case WeaponType.CLASSIC:
                case WeaponType.CURSED:
                case WeaponType.FREEZE:
                case WeaponType.POISON:
                case WeaponType.VAMPIRE:
                case WeaponType.WIDE:
                case WeaponType.CAT:
                    Vector2 playerDirection = GameManager.instance.player.GetVelocity();
                    if (playerDirection != Vector2.zero)
                    {
                        Attack(playerDirection.normalized);
                    }
                    else
                    {
                        Attack(GameManager.instance.player.transform.up);
                    }
                    break;
                // Attack nearest enemy
                case WeaponType.QUICK:
                default:                    
                    GameObject targetEnemy = GetNearestEnemy();
                    if (targetEnemy != null)
                    {
                        Attack(EnemiesManager.instance.GetEnemyInstanceFromGameObjectName(targetEnemy.name));
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

    private IEnumerator SendTongueInDirectionRotating(Vector2 direction)
    {
        isAttacking = true;
        preventAttack = true;
        SetTongueDirection(direction);
        float t = 0;
        isTongueGoingOut = true;
        tongueLineRenderer.enabled = true;
        outlineLineRenderer.enabled = true;
        tongueCollider.enabled = true;

        float actualAttackDuration = duration * (1 + GameManager.instance.player.attackDurationBoost); // in seconds
        float actualAttackSpeed = attackSpeed * (1 + GameManager.instance.player.attackSpeedBoost);

        float angle = Vector2.SignedAngle(Vector2.up, direction) * Mathf.Deg2Rad;
        angle += (tongueTypeIndex * 2 * Mathf.PI / tongueTypeCount);

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
                actualAttackDuration -= Time.fixedDeltaTime;
                if (actualAttackDuration < 0)
                {
                    isTongueGoingOut = false;
                }
            }

            actualAttackSpeed = attackSpeed * (1 + GameManager.instance.player.attackSpeedBoost);
            angle += actualAttackSpeed * Time.fixedDeltaTime;

            SetTongueDirection((Mathf.Cos(angle) * Vector2.right + Mathf.Sin(angle) * Vector2.up).normalized);

            yield return new WaitForFixedUpdate();
        }
        while (t > 0)
        {
            SetTongueScale(t);
            t -= Time.fixedDeltaTime;

            actualAttackSpeed = attackSpeed * (1 + GameManager.instance.player.attackSpeedBoost);
            angle += actualAttackSpeed * Time.fixedDeltaTime;

            SetTongueDirection((Mathf.Cos(angle) * Vector2.right + Mathf.Sin(angle) * Vector2.up).normalized);
            yield return new WaitForFixedUpdate();
        }
        tongueLineRenderer.enabled = false;
        outlineLineRenderer.enabled = false;
        tongueCollider.enabled = false;
        isAttacking = false;
        lastAttackTime = Time.time;
        preventAttack = false;

        foreach (GameObject weapon in activeWeaponsOfSameTypeList)
        {
            weapon.GetComponent<WeaponBehaviour>().lastAttackTime = lastAttackTime;
            weapon.GetComponent<WeaponBehaviour>().preventAttack = false;
        }
    }

    public void Attack(Vector2 direction)
    {
        eatenFliesCount = 0;
        lastAttackTime = Time.time;

        // Adjusted direction using index and count
        float deltaAngle = (tongueTypeCount - 1) * 5 * Mathf.Deg2Rad;
        float angle = -deltaAngle + tongueTypeIndex * 2 * deltaAngle / (tongueTypeCount == 1 ? 1 : tongueTypeCount - 1);
        Vector2 adjustedDirection = Tools.Rotate(direction, angle);

        if (tongueLineRenderer != null && outlineLineRenderer != null)
        {
            StartCoroutine(SendTongueInDirection(adjustedDirection.normalized));
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
            t -= (Time.fixedDeltaTime * actualAttackSpeed);
            yield return new WaitForFixedUpdate();
        }
        tongueLineRenderer.enabled = false;
        outlineLineRenderer.enabled = false;
        tongueCollider.enabled = false;
        lastAttackTime = Time.time;
        isAttacking = false;        
        preventAttack = false;
        enemiesHitNamesList.Clear();

        foreach (GameObject weapon in activeWeaponsOfSameTypeList)
        {
            weapon.GetComponent<WeaponBehaviour>().lastAttackTime = lastAttackTime;
            weapon.GetComponent<WeaponBehaviour>().preventAttack = false;
        }
    }

    private IEnumerator RemoveEnemyNameWithDelay(string enemyName, float delay)
    {
        yield return new WaitForSeconds(delay);
        enemiesHitNamesList.Remove(enemyName);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Enemy"))
        {
            // default part, for any weapon
            string enemyName = collision.gameObject.name;
            if (!enemiesHitNamesList.Contains(enemyName))
            {
                enemiesHitNamesList.Add(enemyName); // to prevent a single tongue to hit the same enemy multiple times
                if (weaponType == WeaponType.ROTATING)
                {
                    float actualAttackSpeed = attackSpeed * (1 + GameManager.instance.player.attackSpeedBoost);
                    StartCoroutine(RemoveEnemyNameWithDelay(enemyName, 1.0f / actualAttackSpeed));
                }

                EnemyInstance enemy = EnemiesManager.instance.GetEnemyInstanceFromGameObjectName(enemyName);
                float actualDamage = damage * (1 + GameManager.instance.player.attackDamageBoost);

                // curse part, increase enemy speed
                bool isCursed = false;
                if (weaponType == WeaponType.CURSED || (weaponType == WeaponType.RANDOM && tongueColor.Equals(DataManager.instance.GetColorForWeaponEffect(WeaponEffect.CURSE))))
                {
                    // Apply curseFactor as a probability of curse
                    float curseProbability = curseChance * (1 + GameManager.instance.player.attackSpecialStrengthBoost);
                    if (Random.Range(0.0f, 1.0f) < curseProbability)
                    {
                        isCursed = true;
                        actualDamage = 0;
                    }
                }

                bool enemyIsDead = EnemiesManager.instance.DamageEnemy(enemyName, actualDamage, this.transform);

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
                    float actualPoisonDuration = duration * (1 + GameManager.instance.player.attackSpecialDurationBoost);
                    EnemiesManager.instance.AddPoisonDamageToEnemy(enemyName, actualPoisonDamage, actualPoisonDuration);
                }
                
                // freeze part, diminish enemy speed
                if (weaponType == WeaponType.FREEZE || (weaponType == WeaponType.RANDOM && tongueColor.Equals(DataManager.instance.GetColorForWeaponEffect(WeaponEffect.FREEZE))))
                {
                    float enemyFreezeDuration = duration * (1 + GameManager.instance.player.attackSpecialDurationBoost);
                    EnemiesManager.instance.ApplyFreezeEffect(enemyName, enemyFreezeDuration);
                }
                if (isCursed)
                {
                    float enemyCurseDuration = duration * (1 + GameManager.instance.player.attackSpecialDurationBoost);
                    EnemiesManager.instance.ApplyCurseEffect(enemyName, enemyCurseDuration);
                }

                if (enemyIsDead)
                {
                    //EnemiesManager.instance.SetEnemyDead(enemyName);
                    eatenFliesCount++;

                    foreach (RunWeaponInfo weaponInfo in RunManager.instance.GetOwnedWeapons())
                    {
                        if (weaponInfo.weaponItemData.weaponData.weaponType == weaponType)
                        {
                            weaponInfo.killCount++;
                        }
                    }
                }
            }
        }
    }
}
