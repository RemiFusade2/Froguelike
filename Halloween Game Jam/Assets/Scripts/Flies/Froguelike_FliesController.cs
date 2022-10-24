using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Froguelike_FliesController : MonoBehaviour
{
    /*
    [Header("Attributes")]
    public float HP;
    public int XPBonus;
    public float moveSpeed;
    public float damage;
    [Space]
    public Color aliveColor;
    public Color deadColor;

    private Froguelike_CharacterController player;

    private Vector2 moveDirection;

    private Rigidbody2D flyRigidbody;
    private SpriteRenderer flyRenderer;
    private Collider2D flyCollider;    

    public float updateVelocityTime;
    private bool updateVelocity;

    // Start is called before the first frame update
    void Start()
    {
        player = Froguelike_GameManager.instance.player;
        flyRigidbody = GetComponent<Rigidbody2D>();
        flyRenderer = GetComponent<SpriteRenderer>();
        flyCollider = GetComponent<CircleCollider2D>();
        flyRenderer.color = aliveColor;

        updateVelocityTime = 1.0f;
        updateVelocity = true;
        StartCoroutine(WaitAndUpdateVelocity());
    }

    private IEnumerator WaitAndUpdateVelocity()
    {
        while (updateVelocity)
        {
            UpdateVelocity();
            yield return new WaitForSeconds(updateVelocityTime);

            if (Time.deltaTime < 0.03f)
            {
                updateVelocityTime = Mathf.Clamp(updateVelocityTime - 0.1f, 0.1f, 5.0f);
            }
            if (Time.deltaTime > 0.06f)
            {
                updateVelocityTime = Mathf.Clamp(updateVelocityTime + 0.1f, 0.1f, 5.0f);
            }
        }
    }

    private void UpdateVelocity()
    {
        if (!IsDead())
        {
            UpdateDirection();
            Move();
        }
        else
        {
            Move(4);
        }
    }


    private void Move(float factor = 1.0f)
    {
        flyRigidbody.velocity = factor * moveDirection * moveSpeed;
    }

    private void UpdateDirection()
    {
        moveDirection = (player.transform.position - this.transform.position).normalized;
        float angle = -Vector2.SignedAngle(moveDirection, Vector2.right);
        int roundedAngle = Mathf.RoundToInt(angle / 90) * 90;
        this.transform.rotation = Quaternion.Euler(0, 0, roundedAngle);
    }
    

    public void TakeDamage(float damageTaken)
    {
        HP -= damageTaken;
        HP = (HP <= 0) ? 0 : HP;

        if (IsDead())
        {
            flyRenderer.color = deadColor;
            this.transform.rotation = Quaternion.Euler(0, 0, 45);
            flyCollider.enabled = false;
            flyRigidbody.velocity = Vector2.zero;
        }
    }

    public bool IsDead()
    {
        return (HP <= 0.01f);
    }*/
}
