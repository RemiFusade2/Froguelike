using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Froguelike_TongueBehaviour : MonoBehaviour
{
    public float cooldown;
    public float damage;
    public float attackSpeed;
    public int maxFlies;

    private float maxLength;
    private PolygonCollider2D polygonCollider;
    private LineRenderer lineRenderer;

    private float lastAttackTime;
    
    private bool isTongueGoingOut;

    private int eatenFliesCount;

    private void SetTongueScale(float scale)
    {
        this.transform.localScale = Vector3.forward + Vector3.up + scale * Vector3.right;
    }

    private void SetTongueDirection(Vector2 direction)
    {
        float angle = -Vector2.SignedAngle(direction.normalized, Vector2.right);
        this.transform.localRotation = Quaternion.Euler(0, 0, angle);
    }

    // Start is called before the first frame update
    void Start()
    {
        SetTongueScale(0);
        lastAttackTime = Time.time;
        polygonCollider = GetComponent<PolygonCollider2D>();
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.transform.GetChild(0).GetComponent<LineRenderer>().startWidth = 0.2f;
        lineRenderer.transform.GetChild(0).GetComponent<LineRenderer>().endWidth = 0.2f;
    }

    public void TryAttack()
    {
        if (Time.time - lastAttackTime > cooldown)
        {
            Attack();
        }
    }

    public void Attack()
    {
        eatenFliesCount = 0;
        lastAttackTime = Time.time;
        Froguelike_Fly fly = Froguelike_FliesManager.instance.GetNearest();
        if (fly != null)
        {
            StartCoroutine(SendTongueInDirection((fly.flyTransform.position - this.transform.position).normalized));
        }
    }

    private IEnumerator SendTongueInDirection(Vector2 direction)
    {
        SetTongueDirection(direction);
        float t = 0;
        isTongueGoingOut = true;
        lineRenderer.enabled = true;
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
        lineRenderer.enabled = false;
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
                if (eatenFliesCount >= maxFlies)
                {
                    isTongueGoingOut = false;
                }
            }
        }
    }
}
