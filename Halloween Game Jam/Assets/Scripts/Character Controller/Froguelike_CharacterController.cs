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

    [Header("Character data")]
    public float landSpeed;
    public float swimSpeed;

    [Header("Settings - controls")]
    public string horizontalInputName;
    public string verticalInputName;

    private Player rewiredPlayer;

    public float HorizontalInput { get; private set; }
    public float VerticalInput { get; private set; }

    [Header("Animator")]
    public Animator animator;

    private Rigidbody2D playerRigidbody;
    private float moveSpeed;

    // Start is called before the first frame update
    void Start()
    {
        moveSpeed = landSpeed;
        rewiredPlayer = ReInput.players.GetPlayer(playerID);
        playerRigidbody = GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
    void Update()
    {
        foreach (Transform weaponTransform in weaponsParent)
        {
            weaponTransform.GetComponent<Froguelike_TongueBehaviour>().TryAttack();
        }
    }

    public void LevelUP()
    {
        landSpeed *= 1.2f;
        swimSpeed *= 1.2f;
        foreach (Transform weaponTransform in weaponsParent)
        {
            weaponTransform.GetComponent<Froguelike_TongueBehaviour>().attackSpeed *= 1.5f;
            weaponTransform.GetComponent<Froguelike_TongueBehaviour>().cooldown /= 1.5f;
        }
    }

    private void FixedUpdate()
    {
        UpdateHorizontalInput();
        UpdateVerticalInput();

        Vector2 moveInput = (((HorizontalInput * Vector2.right).normalized + (VerticalInput * Vector2.up).normalized)).normalized * moveSpeed;
        playerRigidbody.velocity = moveInput;
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
            moveSpeed = swimSpeed;
        }
    }
    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Water"))
        {
            moveSpeed = landSpeed;
        }
    }
}
