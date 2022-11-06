using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public enum WeaponDirection
{
    RANDOM,
    NEAREST,
    ROTATING,
    ULTIMATE
}

public class WeaponBehaviour : MonoBehaviour
{
    [Header("References")]
    public Transform outlineTransform;
    
    [Header("Settings - appearance")]
    public Color tongueColor;
    public float tongueWidth = 1;
    public Color outlineColor;
    public float outlineWeight = 0.1f;

    [Header("Settings - base metrics")]
    public WeaponDirection weaponDirection;
    public float cooldown;
    public float damage;
    public float attackSpeed;
    public float maxFlies;
    public float range;

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


    private LineRenderer tongueLineRenderer;
    private LineRenderer outlineLineRenderer;

    private Collider2D tongueCollider;

    private float lastAttackTime;

    private bool isTongueGoingOut;

    private int eatenFliesCount;

    private bool isAttacking;
    
    public void SetTongueColor()
    {
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

    public void Initialize()
    {
        isAttacking = false;
        eatenFliesCount = 0;
        isTongueGoingOut = false;
        lastAttackTime = Time.time;
        SetTongueScale(0);
        tongueLineRenderer = GetComponent<LineRenderer>();
        outlineLineRenderer = outlineTransform.GetComponent<LineRenderer>();
    }

    public void CopyWeaponStats(WeaponBehaviour weapon)
    {
        cooldown = weapon.cooldown;
        damage = weapon.damage;
        attackSpeed = weapon.attackSpeed;
        maxFlies = weapon.maxFlies;
        range = weapon.range;
        tongueWidth = weapon.tongueWidth;
    }

    public void LevelUp(Froguelike_ItemLevel itemLevel)
    {
        cooldown += itemLevel.weaponCooldownBoost;
        damage += itemLevel.weaponDamageBoost;
        attackSpeed += itemLevel.weaponSpeedBoost;
        maxFlies += itemLevel.weaponMaxFliesBoost;
        range += itemLevel.weaponRangeBoost;
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
            switch (weaponDirection)
            {
                case WeaponDirection.NEAREST:
                    GameObject targetEnemy = GetNearestEnemy();
                    if (targetEnemy != null)
                    {
                        Attack(FliesManager.instance.GetEnemyInfo(targetEnemy.name));
                    }
                    break;
                case WeaponDirection.RANDOM:
                    Vector2 direction = Random.insideUnitCircle.normalized;
                    Attack(direction);
                    break;
                case WeaponDirection.ROTATING:
                    AttackRotating();
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
            string enemyName = collision.gameObject.name;
            float actualDamage = damage * (1 + GameManager.instance.player.attackDamageBoost);
            float actualMaxFiles = maxFlies + GameManager.instance.player.attackMaxFliesBoost;
            bool canKillEnemy = (eatenFliesCount < actualMaxFiles);
            bool enemyIsDead = FliesManager.instance.DamageEnemy(enemyName, actualDamage, canKillEnemy);

            float healAmount = Mathf.Clamp(actualDamage * healthAbsorbRatio, 0, healthAbsorbMax);
            GameManager.instance.player.Heal(healAmount);

            if (enemyIsDead)
            {
                collision.GetComponent<Animator>().SetBool("IsDead", true);
                collision.enabled = false;
                eatenFliesCount++;
                CheckEatenFlyCount();
            }
        }
    }

    private void CheckEatenFlyCount()
    {
        float actualMaxFiles = maxFlies + GameManager.instance.player.attackMaxFliesBoost;
        if (eatenFliesCount >= actualMaxFiles)
        {
            switch (weaponDirection)
            {
                case WeaponDirection.NEAREST:
                case WeaponDirection.ROTATING:
                case WeaponDirection.RANDOM:
                    isTongueGoingOut = false;
                    break;
                default:
                    break;
            }
        }
    }
}
