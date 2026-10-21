using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;
using static UnityEngine.Rendering.DebugUI;

public class MenuTongueTargetingBehaviour : MonoBehaviour
{
    [Header("References")]
    public Transform outlineTransform;
    public Transform menuTonguePosition;
    public Canvas canvas;
    public GameObject tongueOnButton;

    [Header("Settings - appearance")]
    public float extraTongueWidth = 0;
    public float tongueWidth = 1;
    public Color outlineColor;
    public float outlineWeight = 0.1f;

    [Header("Settings - base metrics")]
    public TongueType tongueType;
    [Space]
    public float attackSpeed;
    public float range;

    private Color tongueColor;

    private LineRenderer tongueLineRenderer;
    private LineRenderer outlineLineRenderer;

    private bool isTongueGoingOut;

    private Coroutine sendTongueCoroutine;

    // Start is called before the first frame update
    void Start()
    {
        tongueLineRenderer = GetComponent<LineRenderer>();
        outlineLineRenderer = outlineTransform.GetComponent<LineRenderer>();
        sendTongueCoroutine = null;

        TongueStatsWrapper tongueStatsWrapper = RunItemManager.instance.allRunWeaponsItemsScriptableObjects.Find(x => x.weaponData.weaponType == TongueType.CLASSIC).weaponData.weaponBaseStats;

        Initialize(TongueType.CLASSIC, tongueStatsWrapper);
    }

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
        float actualWidth = tongueWidth * (1) + extraTongueWidth;
        actualWidth *= 2;

        MenuTongueLineRendererBehaviour menuTongueLineScript = this.GetComponent<MenuTongueLineRendererBehaviour>();
        if (menuTongueLineScript != null)
        {
            menuTongueLineScript.SetLineRenderersWidth(actualWidth, outlineWeight, 1);
        }
    }

    private void SetTongueScale(float scale)
    {
        float actualRange = range;
        float remove = range * 0.08960377693f;
        actualRange -= remove;
        float actualWidth = tongueWidth;

        if (scale <= 0)
        {
            this.transform.localScale = Vector3.zero;
            outlineTransform.localScale = Vector3.zero;
        }
        else
        {
            this.transform.localScale = Vector3.forward + (actualWidth * Vector3.up) + (scale * actualRange * Vector3.right);
            outlineTransform.localPosition = 0.01f * Vector3.right;
            outlineTransform.localScale = Vector3.forward + Vector3.up + Vector3.right;
            SetTongueWidth(tongueWidth);
        }
    }

    private void SetTongueDirection(Vector2 direction)
    {
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        Vector3 vector3Angle = new Vector3(0, 0, angle);
        Quaternion quaternion = new Quaternion(0, 0, 0, 0);

        this.transform.SetPositionAndRotation(this.transform.position, quaternion);
        this.transform.Rotate(vector3Angle);
        //this.transform.Rotate(Vector3.forward, angle);

    }

    public void ResetTongue()
    {
        isTongueGoingOut = false;

        tongueLineRenderer = GetComponent<LineRenderer>();
        outlineLineRenderer = outlineTransform.GetComponent<LineRenderer>();

        SetTongueColor(DataManager.instance.GetColorForTongueEffect(TongueEffect.NONE));

        if (sendTongueCoroutine != null)
        {
            StopCoroutine(sendTongueCoroutine);
        }
    }

    public void Initialize(TongueType tongueType, TongueStatsWrapper tongueBaseStats)
    {
        this.tongueType = tongueType;

        attackSpeed = (float)tongueBaseStats.GetStatValue(TongueStat.SPEED).value;
        range = (float)tongueBaseStats.GetStatValue(TongueStat.RANGE).value;

        SetTongueWidth((float)tongueBaseStats.GetStatValue(TongueStat.SIZE).value);

        ResetTongue();
    }

    public void GoToButton(GameObject target, bool followEnemy = false, float offsetAngle = 0)
    {
        // Show eyes.
        target.GetComponent<MenuTongueOnBook>().thisBooksEyes.SetActive(true);

        float distance = Vector2.Distance(menuTonguePosition.position, target.transform.position);
        range = distance / 16 / canvas.scaleFactor;

        Vector2 realDirection = new Vector2((target.transform.position.x - menuTonguePosition.position.x) / range, (target.transform.position.y - menuTonguePosition.position.y) / range);

        if (target != null && tongueLineRenderer != null && outlineLineRenderer != null)
        {
            sendTongueCoroutine = StartCoroutine(SendTongueInDirection(target, realDirection));
        }
    }

    public void ReturnFromButton(GameObject target)
    {
        // Hide tongue hitting button sprite.
        target.GetComponent<MenuTongueOnBook>().thisBooksTongue.SetActive(false);

        // Hide eyes.
        target.GetComponent<MenuTongueOnBook>().thisBooksEyes.SetActive(false);

        if (sendTongueCoroutine != null)
        {
            StopCoroutine(sendTongueCoroutine);
        }
    }

    private IEnumerator SendTongueInDirection(GameObject target, Vector2 direction)
    {
        SetTongueDirection(direction);
        float t = 0;
        isTongueGoingOut = true;
        tongueLineRenderer.enabled = true;
        outlineLineRenderer.enabled = true;
        float actualAttackSpeed = attackSpeed * (1);
        while (isTongueGoingOut)
        {
            SetTongueScale(t);
            t += Time.fixedDeltaTime * actualAttackSpeed;
            yield return new WaitForSecondsRealtime(Time.fixedDeltaTime);
            if (t >= 1)
            {
                isTongueGoingOut = false;
            }
        }

        // Show tongue hitting button sprite.
        target.GetComponent<MenuTongueOnBook>().thisBooksTongue.SetActive(true);
    }
}
