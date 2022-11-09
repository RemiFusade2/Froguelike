using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public enum WeaponType
{
    CLASSIC, // target nearest, medium range, medium damage, no special
    QUICK, // target nearest, shorter, super fast, low damage, low cooldown. Upgrade to zero cooldown and triple tongue.
    WIDE, // target nearest, wider than usual, shorter, more damage
    ROTATING, // rotates around, medium range, low damage, upgrade to double
    VAMPIRE, // target nearest, medium range, low damage, heal when damage. RED
    POISON, // target nearest, low range, medium damage, poison damage during a delay after hit. GREEN
    FREEZE, // target nearest, high range, low damage, slow down enemies. BLUE
    CURSED, // target nearest, medium range, high damage, high speed, make enemies faster. PURPLE OR ORANGE
    RANDOM // target random direction, high range, high damage, medium speed. Random effect and random color
}

public class WeaponBehaviour : MonoBehaviour
{
    [Header("References")]
    public Transform outlineTransform;
    
    [Header("Settings - appearance")]
    public Color defaultColor;
    public Color vampireColor;
    public Color poisonColor;
    public Color freezeColor;
    public Color cursedColor;
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
    public bool comesBackAfterEatingFlies;
    public float maxFlies;

    [Header("Settings - special metrics")]
    public float healthAbsorbRatio;
    public float healthAbsorbMax;
    [Space]
    public float poisonDamage;
    public float poisonDuration;
    [Space]
    public float changeSpeedFactor;
    public float changeSpeedDuration;

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
                SetTongueColor(vampireColor);
                break;
            case WeaponType.POISON:
                SetTongueColor(poisonColor);
                break;
            case WeaponType.FREEZE:
                SetTongueColor(freezeColor);
                break;
            case WeaponType.CURSED:
                SetTongueColor(cursedColor);
                break;
            default:
                SetTongueColor(defaultColor);
                break;
        }
    }

    public void Initialize(WeaponData weaponData)
    {
        weaponType = weaponData.weaponType;
        attackSpeed = weaponData.startAttackSpeed;
        cooldown = weaponData.startCooldown;
        damage = weaponData.startDamage;
        range = weaponData.startRange;
        maxFlies = weaponData.startMaxFlies;

        comesBackAfterEatingFlies = weaponData.comesBackAfterEatingFlies;

        changeSpeedFactor = weaponData.startChangeSpeedFactor;
        changeSpeedDuration = weaponData.startChangeSpeedDuration;

        healthAbsorbMax = weaponData.startHealthAbsorbMax;
        healthAbsorbRatio = weaponData.startHealthAbsorbRatio;

        poisonDamage = weaponData.startPoisonDamage;
        poisonDuration = weaponData.startPoisonDuration;
        
        SetTongueWidth(weaponData.startWidth);

        ResetWeapon();
    }

    public void CopyWeaponStats(WeaponBehaviour weapon)
    {
        cooldown = weapon.cooldown;
        damage = weapon.damage;
        attackSpeed = weapon.attackSpeed;
        maxFlies = weapon.maxFlies;
        range = weapon.range;
        tongueWidth = weapon.tongueWidth;

        weaponType = weapon.weaponType;

        healthAbsorbRatio = weapon.healthAbsorbRatio;
        healthAbsorbMax = weapon.healthAbsorbMax;

        poisonDamage = weapon.poisonDamage;
        poisonDuration = weapon.poisonDuration;

        changeSpeedFactor = weapon.changeSpeedFactor;
        changeSpeedDuration = weapon.changeSpeedDuration;
    }

    public void LevelUp(ItemLevel itemLevel)
    {
        cooldown += itemLevel.weaponCooldownBoost;
        damage += itemLevel.weaponDamageBoost;
        attackSpeed += itemLevel.weaponSpeedBoost;
        maxFlies += itemLevel.weaponMaxFliesBoost;
        range += itemLevel.weaponRangeBoost;

        healthAbsorbRatio += itemLevel.weaponHealthAbsorbRatioBoost;
        healthAbsorbMax += itemLevel.weaponHealthAbsorbMaxBoost;

        poisonDamage += itemLevel.weaponPoisonDamageBoost;
        poisonDuration += itemLevel.weaponPoisonDurationBoost;

        changeSpeedFactor += itemLevel.weaponChangeSpeedFactorBoost;
        changeSpeedDuration += itemLevel.weaponChangeSpeedDurationBoost;
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
        Collider2D[] allColliders = Physics2D.OverlapCircleAll(playerPosition, actualRange * 2.0f * 3.0f, foodLayer);
        if (allColliders.Length > 0)
        {
            float shortestDistance = float.MaxValue;
            Collider2D nearestEnemy = null;
            foreach (Collider2D col in allColliders)
            {
                EnemyInstance enemyInfo = FliesManager.instance.GetEnemyInfo(col.gameObject.name);
                float distanceWithPlayer = Vector2.Distance(col.transform.position, playerPosition);
                if (distanceWithPlayer < shortestDistance && enemyInfo.active)
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
        float actualCooldown = cooldown * (1 - GameManager.instance.player.attackCooldownBoost);
        if (!isAttacking && Time.time - lastAttackTime > actualCooldown)
        {
            switch (weaponType)
            {
                // Attack random direction
                case WeaponType.RANDOM:
                    {
                        // Set random color
                        List<Color> possibleColors = new List<Color>();
                        possibleColors.Add(defaultColor);
                        if (healthAbsorbMax > 0)
                        {
                            possibleColors.Add(vampireColor);
                        }
                        if (poisonDamage > 0)
                        {
                            possibleColors.Add(poisonColor);
                        }
                        if (changeSpeedFactor != 0)
                        {
                            possibleColors.Add(freezeColor);
                            possibleColors.Add(cursedColor);
                        }
                        SetTongueColor(possibleColors[Random.Range(0, possibleColors.Count)]);
                        Vector2 direction = Random.insideUnitCircle.normalized;
                        Attack(direction);
                    }
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
                default:
                    {
                        GameObject targetEnemy = GetNearestEnemy();
                        if (targetEnemy != null)
                        {
                            Attack(FliesManager.instance.GetEnemyInfo(targetEnemy.name));
                        }
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
        SetTongueDirection(direction);
        float t = 0;
        isTongueGoingOut = true;
        tongueLineRenderer.enabled = true;
        outlineLineRenderer.enabled = true;
        float angle = 0;
        float actualAttackSpeed = attackSpeed * (1 + GameManager.instance.player.attackSpeedBoost);
        while (isTongueGoingOut)
        {
            if (t <= 1)
            {
                SetTongueScale(t);
                t += (Time.fixedDeltaTime * actualAttackSpeed);
            }
            angle += (Time.fixedDeltaTime * actualAttackSpeed * 10);
            SetTongueDirection((Mathf.Cos(angle) * Vector2.right + Mathf.Sin(angle) * Vector2.up).normalized);
            yield return new WaitForFixedUpdate();
        }
        while (t > 0)
        {
            SetTongueScale(t);
            t -= (Time.fixedDeltaTime * actualAttackSpeed);
            angle += (Time.fixedDeltaTime * actualAttackSpeed * 10);
            SetTongueDirection((Mathf.Cos(angle) * Vector2.right + Mathf.Sin(angle) * Vector2.up).normalized);
            yield return new WaitForFixedUpdate();
        }
        tongueLineRenderer.enabled = false;
        outlineLineRenderer.enabled = false;
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
            float actualMaxFiles = maxFlies + GameManager.instance.player.attackMaxFliesBoost;
            bool canKillEnemy = (eatenFliesCount < actualMaxFiles);
            bool enemyIsDead = FliesManager.instance.DamageEnemy(enemyName, actualDamage, canKillEnemy, this.transform);

            // vampire part, absorb part of damage done
            if (weaponType == WeaponType.VAMPIRE || (weaponType == WeaponType.RANDOM && tongueColor.Equals(vampireColor)))
            {
                float healAmount = Mathf.Clamp(actualDamage * healthAbsorbRatio, 0, healthAbsorbMax);
                GameManager.instance.player.Heal(healAmount);
            }

            // poison part, add poison damage to enemy
            if (weaponType == WeaponType.POISON || (weaponType == WeaponType.RANDOM && tongueColor.Equals(poisonColor)))
            {
                float actualPoisonDamage = poisonDamage * (1 + GameManager.instance.player.attackSpecialStrengthBoost);
                float actualPoisonDuration = poisonDuration * (1 + GameManager.instance.player.attackSpecialDurationBoost);
                FliesManager.instance.AddPoisonDamageToEnemy(enemyName, actualPoisonDamage, actualPoisonDuration);
            }

            // freeze part or curse part, change enemy speed
            if (weaponType == WeaponType.CURSED || (weaponType == WeaponType.RANDOM && tongueColor.Equals(cursedColor)))
            {
                float actualAccelerateFactor = changeSpeedFactor * (1 + GameManager.instance.player.attackSpecialStrengthBoost);
                float actualAccelerateDuration = changeSpeedDuration * (1 + GameManager.instance.player.attackSpecialDurationBoost);
                FliesManager.instance.ChangeEnemySpeed(enemyName, actualAccelerateFactor, actualAccelerateDuration);
            }
            if (weaponType == WeaponType.FREEZE || (weaponType == WeaponType.RANDOM && tongueColor.Equals(freezeColor)))
            {
                float actualSlowDownFactor = -changeSpeedFactor * (1 + GameManager.instance.player.attackSpecialStrengthBoost);
                float actualSlowDownDuration = changeSpeedDuration * (1 + GameManager.instance.player.attackSpecialDurationBoost);
                FliesManager.instance.ChangeEnemySpeed(enemyName, actualSlowDownFactor, actualSlowDownDuration);
            }

            if (enemyIsDead)
            {
                FliesManager.instance.SetEnemyDead(enemyName);
                eatenFliesCount++;
                CheckEatenFlyCount();
            }
        }
    }

    private void CheckEatenFlyCount()
    {
        float actualMaxFiles = maxFlies + GameManager.instance.player.attackMaxFliesBoost;
        if (comesBackAfterEatingFlies && eatenFliesCount >= actualMaxFiles)
        {
            isTongueGoingOut = false;
        }
    }
}
