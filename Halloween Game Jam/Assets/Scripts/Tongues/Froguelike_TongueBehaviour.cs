using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public enum WeaponType
{
    RANDOM,
    NEAREST,
    QUICK,
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

    [Header("Layer")]
    public LayerMask foodLayer;


    private PolygonCollider2D polygonCollider;

    private float lastAttackTime;
    
    private bool isTongueGoingOut;

    private int eatenFliesCount;

    private bool isAttacking;


    private void SetTongueScale(float scale)
    {
        this.transform.localScale =  Vector3.forward + Vector3.up + scale * range * Vector3.right;
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
        tongueLineRenderer1.startWidth = 0.1f;
        tongueLineRenderer1.endWidth = 0.1f;
        tongueLineRenderer2.startWidth = 0.2f;
        tongueLineRenderer2.endWidth = 0.2f;
    }

    private GameObject GetNearestFly()
    {
        GameObject fly = null;
        Vector2 playerPosition = Froguelike_GameManager.instance.player.transform.position;
        Collider2D[] allColliders = Physics2D.OverlapCircleAll(playerPosition, range, foodLayer);
        if (allColliders.Length > 0)
        {
            float shortestDistance = float.MaxValue;
            Collider2D nearestFly = null;
            foreach (Collider2D col in allColliders)
            {
                Froguelike_Fly flyInfo = Froguelike_FliesManager.instance.GetFlyInfo(col.gameObject.name);
                float distanceWithPlayer = Vector2.Distance(col.transform.position, playerPosition);
                if (distanceWithPlayer < shortestDistance && flyInfo.active)
                {
                    shortestDistance = distanceWithPlayer;
                    nearestFly = col;
                }
            }
            fly = nearestFly.gameObject;
        }
        return fly;
    }

    public void TryAttack()
    {
        if (!isAttacking && Time.time - lastAttackTime > cooldown)
        {
            switch(weaponType)
            {
                case WeaponType.QUICK:
                case WeaponType.NEAREST:
                    GameObject targetFly = GetNearestFly();
                    Attack(Froguelike_FliesManager.instance.GetFlyInfo(targetFly.name));
                    break;
                case WeaponType.RANDOM:
                    Vector2 direction = Random.insideUnitCircle.normalized;
                    Attack(direction);
                    break;
            }

        }
    }

    public void Attack(Vector2 direction)
    {
        eatenFliesCount = 0;
        lastAttackTime = Time.time;
        StartCoroutine(SendTongueInDirection(direction.normalized));
    }

    public void Attack(Froguelike_Fly fly)
    {
        eatenFliesCount = 0;
        lastAttackTime = Time.time;
        if (fly != null)
        {
            StartCoroutine(SendTongueInDirection((fly.flyTransform.position - this.transform.position).normalized));
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
        while (isTongueGoingOut)
        {
            SetTongueScale(t);
            t += (Time.fixedDeltaTime * attackSpeed);
            yield return new WaitForFixedUpdate();
            if (t >= 1)
            {
                isTongueGoingOut = false;
            }
        }
        while (t > 0)
        {
            SetTongueScale(t);
            t -= (Time.fixedDeltaTime * attackSpeed);
            yield return new WaitForFixedUpdate();
        }
        tongueLineRenderer1.enabled = false;
        tongueLineRenderer2.enabled = false;
        isAttacking = false;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Fly") && eatenFliesCount < maxFlies)
        {
            string flyName = collision.gameObject.name;
            bool flyIsDead = Froguelike_FliesManager.instance.DamageFly(flyName, damage);
            if (flyIsDead)
            {
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
            switch(weaponType)
            {
                case WeaponType.NEAREST:
                case WeaponType.QUICK:
                case WeaponType.RANDOM:
                    isTongueGoingOut = false;
                    break;
                default:
                    break;
            }
        }
    }
}
