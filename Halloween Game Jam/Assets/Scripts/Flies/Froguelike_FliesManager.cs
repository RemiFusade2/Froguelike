using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Froguelike_Fly
{
    public float HP;
    public Vector2 moveDirection;
    public Transform flyTransform;
    public Rigidbody2D flyRigidbody;
    public SpriteRenderer flyRenderer;
    public bool active;
}

public class Froguelike_FliesManager : MonoBehaviour
{
    public static Froguelike_FliesManager instance;

    public Transform fliesParent;

    [Header("Settings")]
    public int flyMaxHP;
    public int flyXPBonus;
    public float flyMoveSpeed;
    public float flyDamage;

    public Color flyAliveColor;
    public Color flyDeadColor;

    private List<Froguelike_Fly> allActiveFliesList;


    public Froguelike_Fly GetNearest()
    {
        float shorterDistance = float.MaxValue;
        Froguelike_Fly nearestFly = null;
        foreach (Froguelike_Fly fly in allActiveFliesList)
        {
            float distanceWithPlayer = Vector2.Distance(fly.flyTransform.position, Froguelike_GameManager.instance.player.transform.position);
            if (distanceWithPlayer < shorterDistance)
            {
                shorterDistance = distanceWithPlayer;
                nearestFly = fly;
            }
        }
        return nearestFly;
    }

    private void Awake()
    {
        instance = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        allActiveFliesList = new List<Froguelike_Fly>();
        InvokeRepeating("MoveAllFlies", 0, 0.1f);
    }

    public void AddFly(Transform flyTransform)
    {
        Froguelike_Fly newFly = new Froguelike_Fly();
        newFly.flyRenderer = flyTransform.GetComponent<SpriteRenderer>();
        newFly.flyTransform = flyTransform;
        newFly.HP = flyMaxHP;
        newFly.flyRigidbody = flyTransform.GetComponent<Rigidbody2D>();
        newFly.active = true;
        flyTransform.gameObject.name = allActiveFliesList.Count.ToString();
        newFly.flyRenderer.color = flyAliveColor;
        allActiveFliesList.Add(newFly);
    }

    // Return true if fly dieded
    public bool DamageFly(string flyGoName, float damage)
    {
        int index = int.Parse(flyGoName);
        Froguelike_Fly fly = allActiveFliesList[index];
        fly.HP -= damage;

        if (fly.HP <= 0)
        {
            fly.flyRenderer.color = flyDeadColor;
            fly.flyTransform.rotation = Quaternion.Euler(0, 0, 45);
            return true;
        }

        return false;
    }

    public void MoveAllFlies()
    {
        Transform playerTransform = Froguelike_GameManager.instance.player.transform;
        float angle = 0;
        int roundedAngle = 0;
        foreach (Froguelike_Fly fly in allActiveFliesList)
        {
            if (fly.active)
            {
                if (fly.HP < 0.01f)
                {
                    // fly is dead
                    fly.moveDirection = (playerTransform.position - fly.flyTransform.position).normalized;
                    fly.flyRigidbody.velocity = 4 * fly.moveDirection * flyMoveSpeed;
                    float distanceWithPlayer = Vector2.Distance(playerTransform.position, fly.flyTransform.position);
                    if (distanceWithPlayer < 0.5f)
                    {
                        fly.flyRenderer.enabled = false;
                        fly.active = false;
                        Froguelike_GameManager.instance.EatFly(flyXPBonus);
                    }
                }
                else
                {
                    // fly is alive
                    fly.moveDirection = (playerTransform.position - fly.flyTransform.position).normalized;
                    angle = -Vector2.SignedAngle(fly.moveDirection, Vector2.right);
                    roundedAngle = Mathf.RoundToInt(angle / 90) * 90;
                    fly.flyTransform.rotation = Quaternion.Euler(0, 0, roundedAngle);
                    fly.flyRigidbody.velocity = fly.moveDirection * flyMoveSpeed;
                }
            }
        }
    }
}
