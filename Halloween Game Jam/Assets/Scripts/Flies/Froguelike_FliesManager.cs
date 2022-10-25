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

    public static int lastKey;

    [Header("Settings")]
    public int flyMaxHP;
    public int flyXPBonus;
    public float flyMoveSpeed;
    public float flyDamage;

    public Color flyAliveColor;
    public Color flyDeadColor;

    private Dictionary<int,Froguelike_Fly> allActiveFliesDico;

    public Froguelike_Fly GetFlyInfo(int ID)
    {
        return allActiveFliesDico[ID];
    }
    public Froguelike_Fly GetFlyInfo(string name)
    {
        int ID = int.Parse(name);
        return GetFlyInfo(ID);
    }

    private void Awake()
    {
        instance = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        lastKey = 1;
        allActiveFliesDico = new Dictionary<int, Froguelike_Fly>();
        InvokeRepeating("UpdateAllFlies", 0, 0.1f);
    }

    public void AddFly(Transform flyTransform)
    {
        Froguelike_Fly newFly = new Froguelike_Fly();
        newFly.flyRenderer = flyTransform.GetComponent<SpriteRenderer>();
        newFly.flyTransform = flyTransform;
        newFly.HP = flyMaxHP;
        newFly.flyRigidbody = flyTransform.GetComponent<Rigidbody2D>();
        newFly.active = true;
        lastKey++;
        flyTransform.gameObject.name = lastKey.ToString();
        newFly.flyRenderer.color = flyAliveColor;
        allActiveFliesDico.Add(lastKey, newFly);
    }

    // Return true if fly dieded
    public bool DamageFly(string flyGoName, float damage)
    {
        int index = int.Parse(flyGoName);
        Froguelike_Fly fly = allActiveFliesDico[index];
        fly.HP -= damage;

        if (fly.HP <= 0)
        {
            fly.flyRenderer.color = flyDeadColor;
            fly.flyTransform.rotation = Quaternion.Euler(0, 0, 45);
            return true;
        }

        return false;
    }

    public void UpdateAllFlies()
    {
        Transform playerTransform = Froguelike_GameManager.instance.player.transform;
        float angle = 0;
        int roundedAngle = 0;
        List<int> fliesToDestroyIDList = new List<int>();
        foreach (KeyValuePair<int, Froguelike_Fly> flyInfo in allActiveFliesDico)
        {
            Froguelike_Fly fly = flyInfo.Value;
            if (fly.active)
            {
                if (fly.HP < 0.01f)
                {
                    // fly is dead
                    fly.moveDirection = (playerTransform.position - fly.flyTransform.position).normalized;
                    fly.flyRigidbody.velocity = 2 * fly.moveDirection * Froguelike_GameManager.instance.player.landSpeed;
                    float distanceWithPlayer = Vector2.Distance(playerTransform.position, fly.flyTransform.position);
                    if (distanceWithPlayer < 1)
                    {
                        fly.flyRenderer.enabled = false;
                        fly.active = false;
                        Froguelike_GameManager.instance.EatFly(flyXPBonus);
                        fliesToDestroyIDList.Add(flyInfo.Key);
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
        foreach (int flyID in fliesToDestroyIDList)
        {
            Froguelike_Fly fly = allActiveFliesDico[flyID];
            allActiveFliesDico.Remove(flyID);
            Destroy(fly.flyTransform.gameObject, 0.1f);
        }
    }
}
