using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Rewired;

public class Froguelike_CharacterController : MonoBehaviour
{
    [Header("Player id")]
    public int playerID;

    [Header("References")]
    public Transform weaponsParent;
    public Transform weaponStartPoint;
    public Transform healthBar;

    [Header("Character data")]
    public float landSpeed;
    public float swimSpeed;

    public float currentHealth = 100;
    public float maxHealth = 100;
    public float healthRecovery = 0.01f;

    [Header("Settings - controls")]
    public string horizontalInputName;
    public string verticalInputName;

    private Player rewiredPlayer;

    public float HorizontalInput { get; private set; }
    public float VerticalInput { get; private set; }

    [Header("Animator")]
    public Animator animator;

    private Rigidbody2D playerRigidbody;

    private bool isOnLand;

    private float orientationAngle;

    private float invincibilityTime;

    // Start is called before the first frame update
    void Start()
    {
        isOnLand = true;
        invincibilityTime = 0;
        rewiredPlayer = ReInput.players.GetPlayer(playerID);
        playerRigidbody = GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
    void Update()
    {
        if (Froguelike_GameManager.instance.isGameRunning)
        {
            foreach (Transform weaponTransform in weaponsParent)
            {
                weaponTransform.GetComponent<Froguelike_TongueBehaviour>().TryAttack();
            }
            ChangeHealth(healthRecovery);
        }
    }
    
    public void Respawn()
    {
        currentHealth = maxHealth;
        UpdateHealthBar();
        invincibilityTime = 1;
    }

    private void FixedUpdate()
    {
        UpdateHorizontalInput();
        UpdateVerticalInput();

        if (invincibilityTime > 0)
        {
            invincibilityTime -= Time.fixedDeltaTime;
        }

        float moveSpeed = isOnLand ? landSpeed : swimSpeed;
        Vector2 moveInput = (((HorizontalInput * Vector2.right).normalized + (VerticalInput * Vector2.up).normalized)).normalized * moveSpeed;

        if (!moveInput.Equals(Vector2.zero))
        {
            orientationAngle = 90 + 90 * Mathf.RoundToInt((Vector2.SignedAngle(moveInput, Vector2.right)) / 90);
            transform.localRotation = Quaternion.Euler(0, 0, -orientationAngle);
        }

        playerRigidbody.velocity = moveInput;

        foreach (Transform weaponTransform in weaponsParent)
        {
            weaponTransform.GetComponent<Froguelike_TongueBehaviour>().SetTonguePosition(weaponStartPoint);
        }
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
        if (collision.collider.CompareTag("Fly") && Froguelike_GameManager.instance.isGameRunning && invincibilityTime <= 0)
        {
            float damage = Froguelike_FliesManager.instance.GetEnemyDataFromName(collision.gameObject.name).damage;
            ChangeHealth(-damage);
        }
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
            Froguelike_GameManager.instance.TriggerGameOver();
        }
        UpdateHealthBar();
    }

    private void UpdateHealthBar()
    {
        float healthRatio = currentHealth / maxHealth;
        if (healthRatio < 0)
            healthRatio = 0;
        healthBar.localScale = healthRatio * Vector3.right + healthBar.localScale.y * Vector3.up + healthBar.localScale.z * Vector3.forward;
    }
}
