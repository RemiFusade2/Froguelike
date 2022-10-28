using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public enum WeaponType
{
    RANDOM,
    NEAREST,
    ROTATING,
    ULTIMATE
}

public class Froguelike_TongueBehaviour : MonoBehaviour
{
    [Header("References")]
    public LineRenderer tongueLineRenderer1;
    public LineRenderer tongueLineRenderer2;

    [Header("Settings")]
    public WeaponType weaponType;

    public float cooldown;
    public float damage;
    public float attackSpeed;
    public float maxFlies;
    public float range;

    public float width = 1;

    [Header("Layer")]
    public LayerMask foodLayer;


    private PolygonCollider2D polygonCollider;

    private float lastAttackTime;

    private bool isTongueGoingOut;

    private int eatenFliesCount;

    private bool isAttacking;

    public void CopyWeaponStats(Froguelike_TongueBehaviour weapon)
    {
        cooldown = weapon.cooldown;
        damage = weapon.damage;
        attackSpeed = weapon.attackSpeed;
        maxFlies = weapon.maxFlies;
        range = weapon.range;
        width = weapon.width;
    }

    public void LevelUp(Froguelike_ItemLevel itemLevel)
    {
        cooldown += itemLevel.weaponCooldownBoost;
        damage += itemLevel.weaponDamageBoost;
        attackSpeed += itemLevel.weaponSpeedBoost;
        maxFlies += itemLevel.weaponMaxFliesBoost;
        range += itemLevel.weaponRangeBoost;
    }

    private void SetTongueScale(float scale)
    {
        float actualRange = range * (1 + Froguelike_GameManager.instance.player.attackRangeBoost);
        this.transform.localScale = Vector3.forward + (width * Vector3.up) + (scale * actualRange * Vector3.right);
        tongueLineRenderer1.startWidth = 0.1f * width;
        tongueLineRenderer1.endWidth = 0.1f * width;
        tongueLineRenderer2.startWidth = 0.1f * width + 0.1f;
        tongueLineRenderer2.endWidth = 0.1f * width + 0.1f;
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

    // Start is called before the first frame update
    void Start()
    {
        SetTongueScale(0);
        lastAttackTime = Time.time;
        polygonCollider = GetComponent<PolygonCollider2D>();
    }

    private GameObject GetNearestEnemy()
    {
        GameObject enemy = null;
        Vector2 playerPosition = Froguelike_GameManager.instance.player.transform.position;
        float actualRange = range * (1 + Froguelike_GameManager.instance.player.attackRangeBoost);
        Collider2D[] allColliders = Physics2D.OverlapCircleAll(playerPosition, actualRange * 2.0f * 3.0f, foodLayer);
        if (allColliders.Length > 0)
        {
            float shortestDistance = float.MaxValue;
            Collider2D nearestEnemy = null;
            foreach (Collider2D col in allColliders)
            {
                Froguelike_EnemyInstance enemyInfo = Froguelike_FliesManager.instance.GetEnemyInfo(col.gameObject.name);
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
        if (!isAttacking && Time.time - lastAttackTime > cooldown)
        {
            switch (weaponType)
            {
                case WeaponType.NEAREST:
                    GameObject targetEnemy = GetNearestEnemy();
                    if (targetEnemy != null)
                    {
                        Attack(Froguelike_FliesManager.instance.GetEnemyInfo(targetEnemy.name));
                    }
                    break;
                case WeaponType.RANDOM:
                    Vector2 direction = Random.insideUnitCircle.normalized;
                    Attack(direction);
                    break;
                case WeaponType.ROTATING:
                    AttackRotating();
                    break;
            }

        }
    }

    public void AttackRotating()
    {
        eatenFliesCount = 0;
        lastAttackTime = Time.time;
        Vector2 direction = Froguelike_GameManager.instance.player.transform.up;
        StartCoroutine(SendTongueInDirectionRotating(direction));
    }

    private IEnumerator SendTongueInDirectionRotating(Vector2 direction)
    {
        isAttacking = true;
        SetTongueDirection(direction);
        float t = 0;
        isTongueGoingOut = true;
        tongueLineRenderer1.enabled = true;
        tongueLineRenderer2.enabled = true;
        float angle = 0;
        float actualAttackSpeed = attackSpeed * (1 + Froguelike_GameManager.instance.player.attackSpeedBoost);
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
        tongueLineRenderer1.enabled = false;
        tongueLineRenderer2.enabled = false;
        lastAttackTime = Time.time;
        isAttacking = false;
    }

    public void Attack(Vector2 direction)
    {
        eatenFliesCount = 0;
        lastAttackTime = Time.time;
        StartCoroutine(SendTongueInDirection(direction.normalized));
    }

    public void Attack(Froguelike_EnemyInstance enemy)
    {
        eatenFliesCount = 0;
        if (enemy != null)
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
        tongueLineRenderer1.enabled = true;
        tongueLineRenderer2.enabled = true;
        float actualAttackSpeed = attackSpeed * (1+Froguelike_GameManager.instance.player.attackSpeedBoost);
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
        tongueLineRenderer1.enabled = false;
        tongueLineRenderer2.enabled = false;
        lastAttackTime = Time.time;
        isAttacking = false;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Fly"))
        {
            string enemyName = collision.gameObject.name;
            float actualDamage = damage * (1 + Froguelike_GameManager.instance.player.attackDamageBoost);
            float actualMaxFiles = maxFlies + Froguelike_GameManager.instance.player.attackMaxFliesBoost;
            bool canKillEnemy = (eatenFliesCount < actualMaxFiles);
            bool enemyIsDead = Froguelike_FliesManager.instance.DamageEnemy(enemyName, damage, canKillEnemy);
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
        if (eatenFliesCount >= maxFlies)
        {
            switch (weaponType)
            {
                case WeaponType.NEAREST:
                case WeaponType.ROTATING:
                case WeaponType.RANDOM:
                    isTongueGoingOut = false;
                    break;
                default:
                    break;
            }
        }
    }
}
