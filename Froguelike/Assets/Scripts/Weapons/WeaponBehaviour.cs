using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI;

public enum LineShape
{
    STRAIGHT_LINE,
    SINUSOID_LINE,
    TRIANGLE_LINE,
    SQUARE_LINE,
    LOOPS_LINE,
    SPIRAL_LINE
}

public class WeaponBehaviour : MonoBehaviour
{
    [Header("References")]
    public Transform outlineTransform;

    [Header("Settings - playtests")]
    public bool forceTargetClosestEnemy = false;

    [Header("Settings - appearance")]
    public float tongueWidth = 1;
    public Color outlineColor;
    public float outlineWeight = 0.1f;

    [Header("Settings - base metrics")]
    public TongueType tongueType;
    [Space]
    public float cooldown;
    public float damage;
    public float attackSpeed;
    public float range;
    public float duration;
    [Space]
    public float knockbackForce;

    [Header("Settings - special metrics")]
    public float healthAbsorbRatio;
    [Space]
    public float poisonDamage;
    [Space]
    public bool freezeEffect;
    public float curseChance;

    [Header("Collision Layer")]
    public LayerMask foodLayer;

    [Header("Runtime")]
    public float lastAttackTime;
    public bool isAttacking;
    public bool preventAttack;
    [Space]
    public int tongueTypeIndex; // Index of this tongue
    public int tongueTypeCount; // Number of tongues of the same type
    [Space]
    public List<GameObject> activeTonguesOfSameTypeList;

    private Color tongueColor;

    private LineRenderer tongueLineRenderer;
    private LineRenderer outlineLineRenderer;

    private bool isTongueGoingOut;

    private int eatenFliesCount;

    private List<string> enemiesHitNamesList;

    private Coroutine sendTongueCoroutine;

    private bool isCurseEffectActive;
    private int curseEffectActiveCount;

    // Start is called before the first frame update
    void Start()
    {
        lastAttackTime = Time.time;
        tongueLineRenderer = GetComponent<LineRenderer>();
        outlineLineRenderer = outlineTransform.GetComponent<LineRenderer>();
        enemiesHitNamesList = new List<string>();
        sendTongueCoroutine = null;
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
        float actualWidth = tongueWidth * (1 + GameManager.instance.player.GetAttackSizeBoost());        
        actualWidth *= 2;

        TongueLineRendererBehaviour tongueLineScript = this.GetComponent<TongueLineRendererBehaviour>();
        if (tongueLineScript != null)
        {
            tongueLineScript.SetLineRenderersWidth(actualWidth, outlineWeight, 1, tongueType != TongueType.CAT);
        }
    }

    private void SetTongueScale(float scale)
    {
        float actualRange = range * (1 + GameManager.instance.player.GetAttackRangeBoost());
        float actualWidth = tongueWidth * (1 + GameManager.instance.player.GetAttackSizeBoost());
        if (scale <= 0)
        {
            this.transform.localScale = Vector3.zero;
            outlineTransform.localScale = Vector3.zero;
        }
        else
        {
            this.transform.localScale = Vector3.forward + (actualWidth * Vector3.up) + (scale * actualRange * Vector3.right);
            outlineTransform.localPosition = (-outlineWeight / (scale * actualRange)) * Vector3.right;
            outlineTransform.localScale = Vector3.forward + Vector3.up + ((1 + ((outlineWeight*2)/(scale * actualRange))) * Vector3.right);
            SetTongueWidth(tongueWidth);
        }
    }

    private void SetTongueDirection(Vector2 direction)
    {
        float angle = -Vector2.SignedAngle(direction.normalized, Vector2.right);
        if (tongueType != TongueType.ROTATING)
        {
            this.transform.rotation = Quaternion.identity;
        }
        else
        {
            this.transform.rotation = Quaternion.Euler(0, 0, angle);
        }
    }

    public void SetTonguePosition(Transform weaponStartPosition)
    {
        this.transform.position = weaponStartPosition.position;
    }

    public void ResetTongue()
    {
        isAttacking = false;
        isTongueGoingOut = false;
        lastAttackTime = Time.time;
        preventAttack = false;
        isCurseEffectActive = false;
        curseEffectActiveCount = 0;

        tongueLineRenderer = GetComponent<LineRenderer>();
        outlineLineRenderer = outlineTransform.GetComponent<LineRenderer>();

        this.GetComponent<TongueLineRendererBehaviour>().ResetLine();

        switch (tongueType)
        {
            case TongueType.VAMPIRE:
                SetTongueColor(DataManager.instance.GetColorForTongueEffect(TongueEffect.VAMPIRE));
                break;
            case TongueType.POISON:
                SetTongueColor(DataManager.instance.GetColorForTongueEffect(TongueEffect.POISON));
                break;
            case TongueType.FREEZE:
                SetTongueColor(DataManager.instance.GetColorForTongueEffect(TongueEffect.FREEZE));
                break;
            case TongueType.CURSED:
                SetTongueColor(DataManager.instance.GetColorForTongueEffect(TongueEffect.CURSE));
                break;
            default:
                SetTongueColor(DataManager.instance.GetColorForTongueEffect(TongueEffect.NONE));
                break;
        }
        
        eatenFliesCount = 0;

        if (sendTongueCoroutine != null)
        {
            StopCoroutine(sendTongueCoroutine);
        }
    }

    public void Initialize(TongueType tongueType, TongueStatsWrapper tongueBaseStats)
    {
        this.tongueType = tongueType;

        attackSpeed = (float)tongueBaseStats.GetStatValue(TongueStat.SPEED).value;
        cooldown = (float)tongueBaseStats.GetStatValue(TongueStat.COOLDOWN).value;
        damage = (float)tongueBaseStats.GetStatValue(TongueStat.DAMAGE).value;
        range = (float)tongueBaseStats.GetStatValue(TongueStat.RANGE).value;
        duration = (float)tongueBaseStats.GetStatValue(TongueStat.DURATION).value;

        knockbackForce = (float)tongueBaseStats.GetStatValue(TongueStat.KNOCKBACK).value;

        healthAbsorbRatio = (float)tongueBaseStats.GetStatValue(TongueStat.VAMPIRE_RATIO).value;
        poisonDamage = (float)tongueBaseStats.GetStatValue(TongueStat.POISON_DAMAGE).value;
        freezeEffect = (float)tongueBaseStats.GetStatValue(TongueStat.FREEZE_EFFECT).value > 0;
        curseChance = (float)tongueBaseStats.GetStatValue(TongueStat.CURSE_EFFECT).value;

        SetTongueWidth((float)tongueBaseStats.GetStatValue(TongueStat.SIZE).value);

        activeTonguesOfSameTypeList = new List<GameObject>();
        tongueTypeIndex = 0;
        tongueTypeCount = 1;

        ResetTongue();
    }

    public void Initialize(RunWeaponData tongueData, List<GameObject> activeWeapons)
    {
        tongueType = tongueData.weaponType;

        this.GetComponent<TongueLineRendererBehaviour>().SetCurves(tongueData.frogMovementWeightOnTongue, tongueData.targetMovementWeightOnTongue);
        

        attackSpeed = (float)tongueData.weaponBaseStats.GetStatValue(TongueStat.SPEED).value;
        cooldown = (float)tongueData.weaponBaseStats.GetStatValue(TongueStat.COOLDOWN).value;
        damage = (float)tongueData.weaponBaseStats.GetStatValue(TongueStat.DAMAGE).value;
        range = (float)tongueData.weaponBaseStats.GetStatValue(TongueStat.RANGE).value;
        duration = (float)tongueData.weaponBaseStats.GetStatValue(TongueStat.DURATION).value;

        knockbackForce = (float)tongueData.weaponBaseStats.GetStatValue(TongueStat.KNOCKBACK).value;

        healthAbsorbRatio = (float)tongueData.weaponBaseStats.GetStatValue(TongueStat.VAMPIRE_RATIO).value;
        poisonDamage = (float)tongueData.weaponBaseStats.GetStatValue(TongueStat.POISON_DAMAGE).value;
        freezeEffect = (float)tongueData.weaponBaseStats.GetStatValue(TongueStat.FREEZE_EFFECT).value > 0;
        curseChance = (float)tongueData.weaponBaseStats.GetStatValue(TongueStat.CURSE_EFFECT).value;

        SetTongueWidth((float)tongueData.weaponBaseStats.GetStatValue(TongueStat.SIZE).value);

        activeTonguesOfSameTypeList = activeWeapons;
        tongueTypeIndex = 0;
        tongueTypeCount = 1;

        ResetTongue();
    }

    public void CopyWeaponStats(List<GameObject> otherWeaponsOfTheSameType)
    {
        tongueTypeCount = otherWeaponsOfTheSameType.Count+1;
        WeaponBehaviour weapon = null;
        foreach (GameObject weaponGo in otherWeaponsOfTheSameType)
        {
            weapon = weaponGo.GetComponent<WeaponBehaviour>();
            weapon.tongueTypeCount = tongueTypeCount;
            weapon.activeTonguesOfSameTypeList = otherWeaponsOfTheSameType;
        }

        TongueLineRendererBehaviour baseWeaponLineScript = weapon.gameObject.GetComponent<TongueLineRendererBehaviour>();
        this.GetComponent<TongueLineRendererBehaviour>().SetCurves(baseWeaponLineScript.frogMovementWeightOnTongue, baseWeaponLineScript.targetMovementWeightOnTongue);
        
        cooldown = weapon.cooldown;
        damage = weapon.damage;
        attackSpeed = weapon.attackSpeed;
        range = weapon.range;
        duration = weapon.duration;

        tongueTypeIndex = tongueTypeCount-1;
        activeTonguesOfSameTypeList = otherWeaponsOfTheSameType;

        knockbackForce = weapon.knockbackForce;

        lastAttackTime = float.MaxValue;

        SetTongueWidth(weapon.tongueWidth);

        tongueType = weapon.tongueType;

        healthAbsorbRatio = weapon.healthAbsorbRatio;
        poisonDamage = weapon.poisonDamage;
        freezeEffect = weapon.freezeEffect;
        curseChance = weapon.curseChance;

        ResetTongue();

        if (tongueType == TongueType.ROTATING)
        {
            preventAttack = true; // so this new tongue will not attack before the previous one stops it attack first
        }
    }

    public void LevelUp(RunWeaponItemLevel weaponItemLevel)
    {
        // Damage is additive
        damage += (float)weaponItemLevel.weaponStatUpgrades.GetStatValue(TongueStat.DAMAGE).value;

        // Cooldown is multiplicative and negative
        cooldown *= (1+(float)weaponItemLevel.weaponStatUpgrades.GetStatValue(TongueStat.COOLDOWN).value);
        
        attackSpeed *= (1+(float)weaponItemLevel.weaponStatUpgrades.GetStatValue(TongueStat.SPEED).value);
        range *= (1+(float)weaponItemLevel.weaponStatUpgrades.GetStatValue(TongueStat.RANGE).value);

        duration *= (1 + (float)weaponItemLevel.weaponStatUpgrades.GetStatValue(TongueStat.DURATION).value);

        knockbackForce *= (1 + (float)weaponItemLevel.weaponStatUpgrades.GetStatValue(TongueStat.KNOCKBACK).value);

        // Width
        float newTongueWidth = tongueWidth * (1 + (float)weaponItemLevel.weaponStatUpgrades.GetStatValue(TongueStat.SIZE).value);
        SetTongueWidth(newTongueWidth);

        // Special attack: Vampire
        if (healthAbsorbRatio == 0)
        {
            // First time we add a Vampire effect
            healthAbsorbRatio = (float)weaponItemLevel.weaponStatUpgrades.GetStatValue(TongueStat.VAMPIRE_RATIO).value;
        }
        else
        {
            healthAbsorbRatio *= (1 + (float)weaponItemLevel.weaponStatUpgrades.GetStatValue(TongueStat.VAMPIRE_RATIO).value);
        }

        // Special attack: Poison
        poisonDamage += (float)weaponItemLevel.weaponStatUpgrades.GetStatValue(TongueStat.POISON_DAMAGE).value;

        // Special attack: Freeze
        if (!freezeEffect)
        {
            // First time we add a Freeze effect
            freezeEffect = (weaponItemLevel.weaponStatUpgrades.GetStatValue(TongueStat.FREEZE_EFFECT).value > 0);
        }

        // Special attack: Curse        
        if (curseChance <= 0)
        {
            // First time we add a Curse effect
            curseChance = (float)weaponItemLevel.weaponStatUpgrades.GetStatValue(TongueStat.CURSE_EFFECT).value;
        }
        else
        {
            curseChance *= (1 + (float)weaponItemLevel.weaponStatUpgrades.GetStatValue(TongueStat.CURSE_EFFECT).value);
        }
    }

    private GameObject GetNearestEnemy(int nearestRank = 1)
    {
        GameObject enemy = null;
        Vector2 weaponOriginPosition = this.transform.position; 
        float actualRange = range * (1 + GameManager.instance.player.GetAttackRangeBoost());
        Collider2D[] allColliders = Physics2D.OverlapCircleAll(weaponOriginPosition, actualRange, foodLayer);
        if (allColliders.Length > 0)
        {
            List<float> shortestDistancesList = new List<float>();
            List<Collider2D> nearestEnemiesList = new List<Collider2D>();
            foreach (Collider2D col in allColliders)
            {
                EnemyInstance enemyInfo = EnemiesManager.instance.GetEnemyInstanceFromGameObjectName(col.gameObject.name);
                float distanceWithPlayer = Vector2.Distance(col.transform.position, weaponOriginPosition);

                if (enemyInfo != null && enemyInfo.active)
                {
                    // Enemy is valid target

                    // Compare current enemy to all enemies in the list
                    int indexOfNewEnemy = -1;
                    for (int index = 0; index < shortestDistancesList.Count; index++)
                    {
                        float shortestDistanceOfEnemy = shortestDistancesList[index];
                        Collider2D currentEnemy = nearestEnemiesList[index];

                        if (distanceWithPlayer < shortestDistanceOfEnemy)
                        {
                            // Found one enemy that is not as near as the new one
                            indexOfNewEnemy = index;
                            break;
                        }
                    }

                    if (indexOfNewEnemy > -1)
                    {
                        // New enemy was found closer to another one in the list
                        shortestDistancesList.Insert(indexOfNewEnemy, distanceWithPlayer);
                        nearestEnemiesList.Insert(indexOfNewEnemy, col);
                    }
                    else if (shortestDistancesList.Count < nearestRank)
                    {
                        // We don't have enough enemies in the list, let's add this one at the end of the list
                        shortestDistancesList.Add(distanceWithPlayer);
                        nearestEnemiesList.Add(col);
                    }

                    // Special case if we have too many enemies in the list, we get rid of the last one
                    if (shortestDistancesList.Count > nearestRank)
                    {
                        shortestDistancesList.RemoveAt(shortestDistancesList.Count - 1);
                        nearestEnemiesList.RemoveAt(nearestEnemiesList.Count - 1);
                    }
                }
            }
            
            if (nearestEnemiesList.Count >= nearestRank)
            {
                enemy = nearestEnemiesList[nearestRank - 1].gameObject;
            }
        }
        return enemy;
    }

    private List<TongueEffect> GetRandomEffects()
    {
        List<TongueEffect> result = new List<TongueEffect>();
        List<TongueEffect> possibleEffects = new List<TongueEffect>();
        if (healthAbsorbRatio > 0)
        {
            possibleEffects.Add(TongueEffect.VAMPIRE);
        }
        if (poisonDamage > 0)
        {
            possibleEffects.Add(TongueEffect.POISON);
        }
        if (freezeEffect)
        {
            possibleEffects.Add(TongueEffect.FREEZE);
        }
        if (curseChance > 0)
        {
            possibleEffects.Add(TongueEffect.CURSE);
        }

        TongueEffect previousEffect = TongueEffect.NONE;
        while (result.Count < 4)
        {
            TongueEffect newEffect = possibleEffects[Random.Range(0, possibleEffects.Count)];
            if (newEffect != previousEffect)
            {
                result.Add(newEffect);
                previousEffect = newEffect;
            }
        }

        return result;
    }

    private List<Vector2> GetPositionsTowardsEnemy(GameObject targetEnemy, LineShape lineShape = LineShape.STRAIGHT_LINE, bool stopAtTarget = false, bool inverseValues = false, bool amplitudeScaleWithRange = false)
    {
        List<Vector2> result = new List<Vector2>();
        
        float actualRange = range * (1 + GameManager.instance.player.GetAttackRangeBoost());
        Vector2 direction = Vector2.up;
        float distanceToTarget = actualRange;

        if (targetEnemy != null)
        {
            direction = (targetEnemy.transform.position - this.transform.position).normalized;
            distanceToTarget = (targetEnemy.transform.position - this.transform.position).magnitude;
        }

        float distanceFromFrog = 0;
        float frequency = 0.6f;
        float baseAmplitude = 0.3f;
        float amplitude = baseAmplitude;
        float t = 0;
        Vector2 upVector = new Vector2(-direction.y, direction.x);
        bool switched = false;
        float offsetFromCenter = 0;
        float offsetFromCenterDirection = 1;
        Vector2 lastPosition = Vector2.zero;
        float coneAngleInDegrees = 20;
        float scaleWithAmplitudeFactor = Mathf.Tan(Mathf.Deg2Rad * coneAngleInDegrees);
        switch (lineShape)
        {
            case LineShape.STRAIGHT_LINE:
                while (distanceFromFrog < actualRange && (!stopAtTarget || distanceFromFrog < distanceToTarget))
                {
                    result.Add(distanceFromFrog * direction);
                    distanceFromFrog += 0.1f;
                }
                break;

            case LineShape.SINUSOID_LINE:
                frequency = 0.6f;

                /*
                if (amplitudeScaleWithRange)
                {
                    frequency = 1.0f;
                }*/

                amplitude = 0.3f;
                t = 0;
                while (distanceFromFrog < actualRange && (!stopAtTarget || distanceFromFrog < distanceToTarget))
                {
                    if (amplitudeScaleWithRange)
                    {
                        amplitude = baseAmplitude + distanceFromFrog * scaleWithAmplitudeFactor;
                    }
                    result.Add(distanceFromFrog * direction + amplitude * Mathf.Sin((t * 2 + (inverseValues ? 1:0)) * Mathf.PI) * upVector);
                    distanceFromFrog += 0.1f;
                    t += 0.1f * frequency;
                }
                break;
            case LineShape.TRIANGLE_LINE:
                frequency = 1.5f;
                amplitude = 0.5f;
                t = 0;
                // TODO: implement inverseValues
                offsetFromCenterDirection = 1;
                while (distanceFromFrog < actualRange && (!stopAtTarget || distanceFromFrog < distanceToTarget))
                {
                    if (amplitudeScaleWithRange)
                    {
                        amplitude = baseAmplitude + distanceFromFrog * scaleWithAmplitudeFactor;
                    }

                    result.Add(distanceFromFrog * direction + offsetFromCenter * upVector);
                    if (switched)
                    {
                        result.Add(distanceFromFrog * direction + offsetFromCenter * upVector);
                        result.Add(distanceFromFrog * direction + offsetFromCenter * upVector);
                        switched = false;
                    }

                    offsetFromCenter += offsetFromCenterDirection * 0.1f * frequency;
                    if (offsetFromCenter >= amplitude)
                    {
                        offsetFromCenter = amplitude;
                        offsetFromCenterDirection = -1;
                        switched = true;
                    }
                    else if (offsetFromCenter <= -amplitude)
                    {
                        offsetFromCenter = -amplitude;
                        offsetFromCenterDirection = 1;
                        switched = true;
                    }

                    distanceFromFrog += 0.1f;
                    t += 0.1f * frequency;
                }
                break;

            case LineShape.SQUARE_LINE:
                float squareLineMoveState = 0; // 0 is going straight, 1 is going up, -1 is going down
                bool lastMoveWasUp = inverseValues;
                lastPosition = Vector2.zero;
                t = 0;
                result.Add(lastPosition);
                while (distanceFromFrog < actualRange && (!stopAtTarget || distanceFromFrog < distanceToTarget))
                {
                    if (amplitudeScaleWithRange)
                    {
                        amplitude = baseAmplitude + distanceFromFrog * scaleWithAmplitudeFactor;
                    }

                    if (squareLineMoveState == 0)
                    {
                        lastPosition += 0.1f * direction;
                        distanceFromFrog += 0.1f;


                        t += 0.1f * frequency;
                        if (!lastMoveWasUp && Mathf.Cos(t * 2 * Mathf.PI) < 0)
                        {
                            squareLineMoveState = 1;
                            lastMoveWasUp = true;
                        }
                        else if (lastMoveWasUp && Mathf.Cos(t * 2 * Mathf.PI) > 0)
                        {
                            squareLineMoveState = -1;
                            lastMoveWasUp = false;
                        }
                    }
                    else
                    {
                        lastPosition += 0.1f * squareLineMoveState * upVector;
                        float dotprod = Vector2.Dot(lastPosition, upVector);
                        if (dotprod >= amplitude && lastMoveWasUp)
                        {
                            squareLineMoveState = 0;
                        }
                        else if (dotprod <= -amplitude && !lastMoveWasUp)
                        {
                            squareLineMoveState = 0;
                        }
                    }
                    result.Add(lastPosition);
                }
                break;

            case LineShape.LOOPS_LINE:

                //int loopState = (inverseValues ? 3 : 0); // 6 steps
                int loopState = Random.Range(0, 6); // 6 steps, we can start with any
                lastPosition = Vector2.zero;
                t = 0;
                result.Add(lastPosition);
                float loopAngle = 0;

                float loopBigRadius = 1.8f;
                float loopSmallRadius = 0.8f;
                float loopMediumRadius = 1.3f;

                // These loop radiuses work for a range of about 3
                // Let's adapt the values to work with lower ranges
                if (actualRange < 3)
                {
                    loopBigRadius *= (actualRange / 3);
                    loopSmallRadius *= (actualRange / 3);
                    loopMediumRadius *= (actualRange / 3);
                }

                //float loopRadius = loopBigRadius;
                float loopRadius = (loopState % 3 == 0) ? loopBigRadius : ((loopState % 3 == 1) ? loopSmallRadius : loopMediumRadius);

                while (distanceFromFrog < actualRange && (!stopAtTarget || distanceFromFrog < distanceToTarget))
                {
                    if (amplitudeScaleWithRange)
                    {
                        amplitude = baseAmplitude + distanceFromFrog * scaleWithAmplitudeFactor;
                    }

                    switch (loopState)
                    {
                        case 0:
                            // Move forwards, clockwise
                            lastPosition += 0.1f * direction * Mathf.Sin(loopAngle * Mathf.Deg2Rad) * loopRadius + 0.1f * upVector * Mathf.Cos(loopAngle * Mathf.Deg2Rad) * loopRadius;
                            loopAngle += 10; // Degrees
                            distanceFromFrog += 0.1f * Mathf.Sin(loopAngle * Mathf.Deg2Rad) * loopRadius;
                            if (loopAngle > 180)
                            {
                                loopAngle = 0;
                                loopState = 1;
                                loopRadius = loopSmallRadius;
                            }
                            break;
                        case 1:
                            // Move backwards, clockwise
                            lastPosition += -0.1f * direction * Mathf.Sin(loopAngle * Mathf.Deg2Rad) * loopRadius - 0.1f * upVector * Mathf.Cos(loopAngle * Mathf.Deg2Rad) * loopRadius;
                            loopAngle += 10; // Degrees
                            distanceFromFrog -= 0.1f * Mathf.Sin(loopAngle * Mathf.Deg2Rad) * loopRadius;
                            if (loopAngle > 180)
                            {
                                loopAngle = 0;
                                loopState = 2;
                                loopRadius = loopMediumRadius;
                            }
                            break;
                        case 2:
                            // Move forwards, clockwise
                            lastPosition += 0.1f * direction * Mathf.Sin(loopAngle * Mathf.Deg2Rad) * loopRadius + 0.1f * upVector * Mathf.Cos(loopAngle * Mathf.Deg2Rad) * loopRadius;
                            loopAngle += 10; // Degrees
                            distanceFromFrog += 0.1f * Mathf.Sin(loopAngle * Mathf.Deg2Rad) * loopRadius;
                            if (loopAngle > 180)
                            {
                                loopAngle = 0;
                                loopState = 3;
                                loopRadius = loopBigRadius;
                            }
                            break;
                        case 3:
                            // Move forwards, counter clockwise
                            lastPosition += 0.1f * direction * Mathf.Sin(loopAngle * Mathf.Deg2Rad) * loopRadius - 0.1f * upVector * Mathf.Cos(loopAngle * Mathf.Deg2Rad) * loopRadius;
                            loopAngle += 10; // Degrees
                            distanceFromFrog += 0.1f * Mathf.Sin(loopAngle * Mathf.Deg2Rad) * loopRadius;
                            if (loopAngle > 180)
                            {
                                loopAngle = 0;
                                loopState = 4;
                                loopRadius = loopSmallRadius;
                            }
                            break;
                        case 4:
                            // Move backwards, counter clockwise
                            lastPosition += -0.1f * direction * Mathf.Sin(loopAngle * Mathf.Deg2Rad) * loopRadius + 0.1f * upVector * Mathf.Cos(loopAngle * Mathf.Deg2Rad) * loopRadius;
                            loopAngle += 10; // Degrees
                            distanceFromFrog -= 0.1f * Mathf.Sin(loopAngle * Mathf.Deg2Rad) * loopRadius;
                            if (loopAngle > 180)
                            {
                                loopAngle = 0;
                                loopState = 5;
                                loopRadius = loopMediumRadius;
                            }
                            break;
                        case 5:
                            // Move forwards, counter clockwise
                            lastPosition += 0.1f * direction * Mathf.Sin(loopAngle * Mathf.Deg2Rad) * loopRadius - 0.1f * upVector * Mathf.Cos(loopAngle * Mathf.Deg2Rad) * loopRadius;
                            loopAngle += 10; // Degrees
                            distanceFromFrog += 0.1f * Mathf.Sin(loopAngle * Mathf.Deg2Rad) * loopRadius;
                            if (loopAngle > 180)
                            {
                                loopAngle = 0;
                                loopState = 0;
                                loopRadius = loopBigRadius;
                            }
                            break;
                        default:
                            lastPosition += 0.1f * direction;
                            loopRadius = loopSmallRadius;
                            distanceFromFrog += 0.1f;
                            break;
                    }                    
                    result.Add(lastPosition);
                }
                break;


            case LineShape.SPIRAL_LINE:
                direction = Vector2.up;
                lastPosition = Vector2.zero;
                float spiralAngle = -5;
                // TODO: implement inverseValues
                distanceFromFrog = 0;
                while (distanceFromFrog < actualRange)
                {
                    lastPosition = distanceFromFrog * direction.normalized;

                    float x, y;
                    x = direction.x;
                    y = direction.y;
                    direction.x = x * Mathf.Cos(spiralAngle * Mathf.Deg2Rad) - y * Mathf.Sin(spiralAngle * Mathf.Deg2Rad);
                    direction.y = x * Mathf.Sin(spiralAngle * Mathf.Deg2Rad) + y * Mathf.Cos(spiralAngle * Mathf.Deg2Rad);

                    distanceFromFrog += 0.1f;

                    result.Add(lastPosition);                    
                }
                break;
        }

        return result;
    }

    private List<Vector2> GetRandomPositions()
    {
        List<Vector2> result = new List<Vector2>();
        result.Add(Vector2.zero);
        Vector2 previousPosition = result[0];
        Vector2 direction = Random.insideUnitCircle.normalized;
        float deltaDistance = 0.1f;
        float angleChange = 0;
        float minAngleChange = -0.15f;
        float maxAngleChange = 0.15f;
        float actualRange = range * (1 + GameManager.instance.player.GetAttackRangeBoost());
        for (float totalDistance = 0; totalDistance < actualRange; totalDistance += deltaDistance)
        {
            // move position following direction
            Vector2 newPosition = previousPosition + direction * deltaDistance;
            previousPosition = newPosition;

            // change direction using angleChange
            Vector2 newDirection = Vector2.zero;
            newDirection.x = Mathf.Cos(angleChange) * direction.x - Mathf.Sin(angleChange) * direction.y;
            newDirection.y = Mathf.Sin(angleChange) * direction.x + Mathf.Cos(angleChange) * direction.y;
            direction = newDirection;

            // increase of decrease angleChange by a small amount
            angleChange += Random.Range(-0.03f, 0.03f);
            angleChange = Mathf.Clamp(angleChange, minAngleChange, maxAngleChange);

            // add new position to the list
            result.Add(newPosition);
        }
        return result;
    }

    public void TryAttack()
    {
        float actualCooldown = cooldown * (1 + GameManager.instance.player.GetAttackCooldownBoost());
        if (!preventAttack && !isAttacking && Time.time - lastAttackTime > actualCooldown)
        {
            GameObject targetEnemy = null;
            List<Vector2> pos = new List<Vector2>();
            List<TongueEffect> effects = new List<TongueEffect>();

            // Pick an enemy to target
            bool tongueDoesTarget = (tongueType == TongueType.CLASSIC || tongueType == TongueType.CURSED || tongueType == TongueType.FREEZE || tongueType == TongueType.POISON || tongueType == TongueType.QUICK || tongueType == TongueType.VAMPIRE || tongueType == TongueType.WIDE);
            if (tongueDoesTarget)
            {
                targetEnemy = GetNearestEnemy(tongueTypeIndex + 1);
            }

            // Update width 
            SetTongueWidth(tongueWidth);

            if (tongueDoesTarget && targetEnemy != null)
            {
                // Set positions
                if (tongueType == TongueType.POISON)
                {
                    pos = GetPositionsTowardsEnemy(targetEnemy, LineShape.SINUSOID_LINE, stopAtTarget: false, amplitudeScaleWithRange: true);
                    effects.Add(TongueEffect.POISON);
                }
                else if (tongueType == TongueType.FREEZE)
                {
                    pos = GetPositionsTowardsEnemy(targetEnemy, LineShape.TRIANGLE_LINE, false);
                    effects.Add(TongueEffect.FREEZE);
                }
                else if (tongueType == TongueType.VAMPIRE)
                {
                    pos = GetPositionsTowardsEnemy(targetEnemy, LineShape.LOOPS_LINE, false);
                    effects.Add(TongueEffect.VAMPIRE);
                }
                else if (tongueType == TongueType.CURSED)
                {
                    // Decide if attack is cursed or not
                    float curseProbability = curseChance; // TODO: eventually we could use a "Luck" stat here
                    isCurseEffectActive = (Random.Range(0, 1.0f) < curseProbability);
                    if (isCurseEffectActive)
                    {
                        curseEffectActiveCount++;
                        if (curseEffectActiveCount > 3)
                        {
                            isCurseEffectActive = false;
                            curseEffectActiveCount = 0;
                        }
                    }
                    else
                    {
                        curseEffectActiveCount = 0;
                    }
                    pos = GetPositionsTowardsEnemy(targetEnemy, LineShape.SQUARE_LINE, stopAtTarget: false, inverseValues: isCurseEffectActive);
                    effects.Add(TongueEffect.CURSE);
                }
                else if (tongueType == TongueType.WIDE)
                {
                    pos = GetPositionsTowardsEnemy(targetEnemy, LineShape.STRAIGHT_LINE, false);
                    effects.Add(TongueEffect.NONE);
                }
                else
                {
                    pos = GetPositionsTowardsEnemy(targetEnemy, LineShape.STRAIGHT_LINE, true);
                    effects.Add(TongueEffect.NONE);
                }
                this.GetComponent<TongueLineRendererBehaviour>().InitializeTongue(pos, effects, targetEnemy);
                this.GetComponent<TongueLineRendererBehaviour>().DisplayTongue(0);

                AttackLine(this.GetComponent<TongueLineRendererBehaviour>());
            }

            if (tongueType == TongueType.RANDOM)
            {
                // Set random positions
                pos = GetRandomPositions();
                effects = GetRandomEffects();
                this.GetComponent<TongueLineRendererBehaviour>().InitializeTongue(pos, effects);
                this.GetComponent<TongueLineRendererBehaviour>().DisplayTongue(0);

                AttackLine(this.GetComponent<TongueLineRendererBehaviour>());
            }

            if (tongueType == TongueType.ROTATING)
            {
                pos = GetPositionsTowardsEnemy(null, LineShape.SPIRAL_LINE, false);
                effects.Add(TongueEffect.NONE);
                this.GetComponent<TongueLineRendererBehaviour>().InitializeTongue(pos, effects);
                this.GetComponent<TongueLineRendererBehaviour>().DisplayTongue(0);
                AttackLineRotating(this.GetComponent<TongueLineRendererBehaviour>());
            }

            if (tongueType == TongueType.CAT)
            {
                pos = GetPositionsTowardsEnemy(null, LineShape.STRAIGHT_LINE, false);
                effects.Add(TongueEffect.NONE);
                this.GetComponent<TongueLineRendererBehaviour>().InitializeTongue(pos, effects);
                this.GetComponent<TongueLineRendererBehaviour>().DisplayTongue(0);
                AttackLine(this.GetComponent<TongueLineRendererBehaviour>());
            }
        }
    }

    public void AttackLineRotating(TongueLineRendererBehaviour tongueScript)
    {
        eatenFliesCount = 0;
        lastAttackTime = Time.time;
        Vector2 direction = GameManager.instance.player.transform.up;
        if (tongueScript != null)
        {
            if (sendTongueCoroutine != null)
            {
                StopCoroutine(sendTongueCoroutine);
            }
            sendTongueCoroutine = StartCoroutine(SendLineTongueRotating(tongueScript));
        }
    }

    private IEnumerator SendLineTongueRotating(TongueLineRendererBehaviour tongueScript)
    {
        isAttacking = true;
        preventAttack = true;
        SetTongueDirection(Vector2.up);
        float t = 0;
        isTongueGoingOut = true;

        tongueScript.EnableTongue();

        float actualAttackDuration = duration * (1 + GameManager.instance.player.GetAttackDurationBoost()); // in seconds
        float actualAttackSpeed = attackSpeed * (1 + GameManager.instance.player.GetAttackSpeedBoost());
        float tongueLength = tongueScript.GetTongueLength();

        float angle = 0;
        angle += (tongueTypeIndex * 2 * Mathf.PI / tongueTypeCount);

        float tongueGoingInAndOutSpeedFactor = 0.5f;
        
        while (isTongueGoingOut)
        {
            // Set width
            float actualWidth = tongueWidth * (1 + GameManager.instance.player.GetAttackSizeBoost());
            actualWidth *= 2;

            TongueLineRendererBehaviour tongueLineScript = this.GetComponent<TongueLineRendererBehaviour>();
            if (tongueLineScript != null)
            {
                tongueLineScript.SetLineRenderersWidth(actualWidth, outlineWeight, tongueLength * t, tongueType != TongueType.CAT);
            }

            if (t <= 1)
            {
                tongueScript.DisplayTongue(t);
                t += (Time.fixedDeltaTime * actualAttackSpeed * tongueGoingInAndOutSpeedFactor);
            }
            else
            {
                tongueScript.DisplayTongue(1);
                actualAttackDuration -= Time.fixedDeltaTime;

                // In case the attack duration went down midway through the rotating
                float newAttackDuration = duration * (1 + GameManager.instance.player.GetAttackDurationBoost()); // in seconds
                if (newAttackDuration < actualAttackDuration)
                {
                    actualAttackDuration = newAttackDuration;
                }

                if (actualAttackDuration < 0)
                {
                    isTongueGoingOut = false;
                }
            }


            actualAttackSpeed = attackSpeed * (1 + GameManager.instance.player.GetAttackSpeedBoost());
            angle += actualAttackSpeed * Time.fixedDeltaTime;

            SetTongueDirection((Mathf.Cos(angle) * Vector2.right + Mathf.Sin(angle) * Vector2.up).normalized);

            yield return new WaitForFixedUpdate();
        }
        t = 1;
        while (t > 0)
        {
            // Set width
            float actualWidth = tongueWidth * (1 + GameManager.instance.player.GetAttackSizeBoost());
            actualWidth *= 2;

            TongueLineRendererBehaviour tongueLineScript = this.GetComponent<TongueLineRendererBehaviour>();
            if (tongueLineScript != null)
            {
                tongueLineScript.SetLineRenderersWidth(actualWidth, outlineWeight, tongueLength * t, tongueType != TongueType.CAT);
            }

            tongueScript.DisplayTongue(t);
            t -= (Time.fixedDeltaTime * actualAttackSpeed * tongueGoingInAndOutSpeedFactor);

            actualAttackSpeed = attackSpeed * (1 + GameManager.instance.player.GetAttackSpeedBoost());
            angle += actualAttackSpeed * Time.fixedDeltaTime;

            SetTongueDirection((Mathf.Cos(angle) * Vector2.right + Mathf.Sin(angle) * Vector2.up).normalized);
            yield return new WaitForFixedUpdate();
        }

        tongueScript.DisableTongue();

        isAttacking = false;
        lastAttackTime = Time.time;
        preventAttack = false;

        foreach (GameObject weapon in activeTonguesOfSameTypeList)
        {
            weapon.GetComponent<WeaponBehaviour>().lastAttackTime = lastAttackTime;
            weapon.GetComponent<WeaponBehaviour>().preventAttack = false;
        }
    }

    public void AttackLine(TongueLineRendererBehaviour tongueScript)
    {
        eatenFliesCount = 0;
        lastAttackTime = Time.time;

        if (tongueScript != null)
        {
            lastAttackTime = Time.time;
            if (sendTongueCoroutine != null)
            {
                StopCoroutine(sendTongueCoroutine);
            }
            sendTongueCoroutine = StartCoroutine(SendTongueLine(tongueScript));
        }
    }

    public void Attack(Vector2 direction)
    {
        eatenFliesCount = 0;
        lastAttackTime = Time.time;

        // Adjusted direction using index and count
        float deltaAngle = (tongueTypeCount - 1) * 5 * Mathf.Deg2Rad;
        float angle = -deltaAngle + tongueTypeIndex * 2 * deltaAngle / (tongueTypeCount == 1 ? 1 : tongueTypeCount - 1);
        Vector2 adjustedDirection = Tools.Rotate(direction, angle);

        if (tongueLineRenderer != null && outlineLineRenderer != null)
        {
            lastAttackTime = Time.time;
            if (sendTongueCoroutine != null)
            {
                StopCoroutine(sendTongueCoroutine);
            }
            sendTongueCoroutine = StartCoroutine(SendTongueInDirection(adjustedDirection.normalized));
        }
    }

    public void Attack(EnemyInstance enemy, bool followEnemy = false, float offsetAngle = 0)
    {
        eatenFliesCount = 0;
        if (enemy != null && tongueLineRenderer != null && outlineLineRenderer != null)
        {
            lastAttackTime = Time.time;
            if (sendTongueCoroutine != null)
            {
                StopCoroutine(sendTongueCoroutine);
            }
            if (followEnemy)
            {
                sendTongueCoroutine = StartCoroutine(SendTongueToEnemy(enemy, offsetAngle));
            }
            else
            {
                sendTongueCoroutine = StartCoroutine(SendTongueInDirection((enemy.enemyTransform.position - this.transform.position).normalized));
            }
        }
    }

    private IEnumerator SendTongueLine(TongueLineRendererBehaviour tongueScript)
    {
        isAttacking = true;
        SetTongueDirection(Vector2.up);

        float t = 0.0001f;
        isTongueGoingOut = true;

        tongueScript.EnableTongue();

        float actualAttackSpeed = attackSpeed * (1 + GameManager.instance.player.GetAttackSpeedBoost());
        float actualRange = range * (1 + GameManager.instance.player.GetAttackRangeBoost());
        float tongueLength = tongueScript.GetTongueLength();

        while (isTongueGoingOut)
        {
            // Set width
            float actualWidth = tongueWidth * (1 + GameManager.instance.player.GetAttackSizeBoost());
            actualWidth *= 2;

            TongueLineRendererBehaviour tongueLineScript = this.GetComponent<TongueLineRendererBehaviour>();
            if (tongueLineScript != null)
            {
                tongueLineScript.SetLineRenderersWidth(actualWidth, outlineWeight, actualRange * t, tongueType != TongueType.CAT);
            }

            // Display tongue
            tongueScript.DisplayTongue(t);
            t += (Time.fixedDeltaTime * (actualAttackSpeed / tongueLength));

            // Rotate tongue only if it's cat tongue (it follows character orientation)
            if (tongueType == TongueType.CAT)
            {
                this.transform.localRotation = RunManager.instance.player.transform.localRotation;
            }

            yield return new WaitForFixedUpdate();
            if (t >= 1)
            {
                t = 1;
                isTongueGoingOut = false;
            }
        }
        while (t > 0)
        {
            // Set width
            float actualWidth = tongueWidth * (1 + GameManager.instance.player.GetAttackSizeBoost());
            actualWidth *= 2;

            TongueLineRendererBehaviour tongueLineScript = this.GetComponent<TongueLineRendererBehaviour>();
            if (tongueLineScript != null)
            {
                tongueLineScript.SetLineRenderersWidth(actualWidth, outlineWeight, actualRange * t, tongueType != TongueType.CAT);
            }

            // Display tongue
            tongueScript.DisplayTongue(t);
            t -= (Time.fixedDeltaTime * (actualAttackSpeed / tongueLength));

            // Rotate tongue only if it's cat tongue (it follows character orientation)
            if (tongueType == TongueType.CAT)
            {
                this.transform.localRotation = RunManager.instance.player.transform.localRotation;
            }

            yield return new WaitForFixedUpdate();
        }

        tongueScript.DisableTongue();

        lastAttackTime = Time.time;
        isAttacking = false;
        preventAttack = false;
        enemiesHitNamesList.Clear();
        
        /*
        foreach (GameObject weapon in activeTonguesOfSameTypeList)
        {
            weapon.GetComponent<WeaponBehaviour>().lastAttackTime = lastAttackTime;
            weapon.GetComponent<WeaponBehaviour>().preventAttack = false;
            weapon.GetComponent<WeaponBehaviour>().isAttacking = false;
        }*/
    }

    private IEnumerator SendTongueToEnemy(EnemyInstance enemy, float offsetAngle)
    {
        isAttacking = true;
        Vector2 direction = (enemy.enemyTransform.position - this.transform.position).normalized;
        Vector2 adjustedDirection = Tools.Rotate(direction, offsetAngle);
        SetTongueDirection(adjustedDirection);
        float t = 0;
        isTongueGoingOut = true;
        tongueLineRenderer.enabled = true;
        outlineLineRenderer.enabled = true;
        float actualAttackSpeed = attackSpeed * (1 + GameManager.instance.player.GetAttackSpeedBoost());
        while (isTongueGoingOut)
        {
            SetTongueScale(t);
            t += (Time.fixedDeltaTime * actualAttackSpeed);
            if (enemy.active && enemy.alive)
            {
                direction = (enemy.enemyTransform.position - this.transform.position).normalized;
                adjustedDirection = Tools.Rotate(direction, offsetAngle);
                SetTongueDirection(adjustedDirection);
            }
            yield return new WaitForFixedUpdate();
            if (t >= 1)
            {
                isTongueGoingOut = false;
            }
        }
        while (t > 0)
        {
            SetTongueScale(t);
            t -= (Time.fixedDeltaTime * actualAttackSpeed);
            if (enemy.active && enemy.alive)
            {
                direction = (enemy.enemyTransform.position - this.transform.position).normalized;
                adjustedDirection = Tools.Rotate(direction, offsetAngle);
                SetTongueDirection(adjustedDirection);
            }
            yield return new WaitForFixedUpdate();
        }
        tongueLineRenderer.enabled = false;
        outlineLineRenderer.enabled = false;
        lastAttackTime = Time.time;
        isAttacking = false;
        preventAttack = false;
        enemiesHitNamesList.Clear();

        foreach (GameObject weapon in activeTonguesOfSameTypeList)
        {
            weapon.GetComponent<WeaponBehaviour>().lastAttackTime = lastAttackTime;
            weapon.GetComponent<WeaponBehaviour>().preventAttack = false;
            weapon.GetComponent<WeaponBehaviour>().isAttacking = false;
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
        float actualAttackSpeed = attackSpeed * (1+GameManager.instance.player.GetAttackSpeedBoost());
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
            t -= (Time.fixedDeltaTime * actualAttackSpeed);
            yield return new WaitForFixedUpdate();
        }
        tongueLineRenderer.enabled = false;
        outlineLineRenderer.enabled = false;
        lastAttackTime = Time.time;
        isAttacking = false;        
        preventAttack = false;
        enemiesHitNamesList.Clear();

        foreach (GameObject weapon in activeTonguesOfSameTypeList)
        {
            weapon.GetComponent<WeaponBehaviour>().lastAttackTime = lastAttackTime;
            weapon.GetComponent<WeaponBehaviour>().preventAttack = false;
            weapon.GetComponent<WeaponBehaviour>().isAttacking = false;
        }
    }

    private IEnumerator RemoveEnemyNameWithDelay(string enemyName, float delay)
    {
        yield return new WaitForSeconds(delay);
        enemiesHitNamesList.Remove(enemyName);
    }

    public void ComputeCollision(Collider2D collision, bool isTip)
    {
        float tipFactor = isTip ? 1 : 0.2f;

        // default part, for any weapon
        string enemyName = collision.gameObject.name;
        if (!enemyName.Equals(EnemiesManager.pooledEnemyNameStr) && !enemiesHitNamesList.Contains(enemyName))
        {
            enemiesHitNamesList.Add(enemyName); // to prevent a single tongue to hit the same enemy multiple times
            if (tongueType == TongueType.ROTATING)
            {
                float actualAttackSpeed = attackSpeed * (1 + GameManager.instance.player.GetAttackSpeedBoost());
                StartCoroutine(RemoveEnemyNameWithDelay(enemyName, 1.0f / actualAttackSpeed));
            }

            EnemyInstance enemy = EnemiesManager.instance.GetEnemyInstanceFromGameObjectName(enemyName);
            float actualDamage = tipFactor * ( damage * (1 + GameManager.instance.player.GetAttackDamageBoost()) );

            TongueEffect activeEffect = TongueEffect.NONE;
            switch (tongueType)
            {
                case TongueType.RANDOM:
                    TongueLineRendererBehaviour script = this.GetComponent<TongueLineRendererBehaviour>();
                    activeEffect = script.GetEffectFromTip();
                    if (activeEffect == TongueEffect.CURSE)
                    {
                        // Curse active means no damage
                        actualDamage = 0; 
                    }
                    else if (activeEffect == TongueEffect.POISON)
                    {
                        // Poison active means only poison damage is inflicted
                        actualDamage = poisonDamage * (1 + GameManager.instance.player.GetAttackDamageBoost());
                    }
                    else if (activeEffect == TongueEffect.FREEZE)
                    {
                        // When freeze effect is active, damage is reduced (divided by 10, rounded up to first decimal)
                        actualDamage /= 10;
                        actualDamage = Mathf.CeilToInt(actualDamage * 10) / 10.0f;
                    }
                    break;
                case TongueType.CURSED:
                    activeEffect = TongueEffect.CURSE;
                    break;
                case TongueType.VAMPIRE:
                    activeEffect = TongueEffect.VAMPIRE;
                    break;
                case TongueType.POISON:
                    activeEffect = TongueEffect.POISON;
                    break;
                case TongueType.FREEZE:
                    activeEffect = TongueEffect.FREEZE;
                    break;
            }

            // curse part, increase enemy speed
            bool tongueIsCursedTongueAndCurseIsActive = (tongueType == TongueType.CURSED && isCurseEffectActive);
            bool tongueIsRandomTongueAndEffectIsCurse = (tongueType == TongueType.RANDOM && activeEffect == TongueEffect.CURSE);
            if (tongueIsCursedTongueAndCurseIsActive)
            {
                activeEffect = TongueEffect.CURSE;
                actualDamage = 0;
            }

            bool vampireEffect = (activeEffect == TongueEffect.VAMPIRE);
            bool enemyIsDead = EnemiesManager.instance.DamageEnemy(enemyName, actualDamage, this.transform, 
                applyVampireEffect: vampireEffect, applyFreezeEffect: (activeEffect == TongueEffect.FREEZE), applyCurse: (tongueIsCursedTongueAndCurseIsActive || tongueIsRandomTongueAndEffectIsCurse), poisonSource: (activeEffect == TongueEffect.POISON));

            // vampire part, absorb part of damage done
            if (vampireEffect)
            {
                float healAmount = actualDamage * healthAbsorbRatio;
                GameManager.instance.player.Heal(healAmount, cancelDamage: false);
            }

            float actualStatusDuration = duration * (1 + GameManager.instance.player.GetAttackDurationBoost());
            //float actualStatusDuration = duration * (1 + GameManager.instance.player.GetAttackSpecialDurationBoost());

            // poison part, add poison damage to enemy
            if (activeEffect == TongueEffect.POISON)
            {
                float actualPoisonDamage = poisonDamage * (1 + GameManager.instance.player.GetAttackDamageBoost());
                if (RunManager.instance.currentPlayedCharacter.characterID.Equals("POISONOUS_FROG") && enemy.enemyInfo.enemyData.enemyType == EnemyType.PLANT)
                {
                    // Special: Ribbit does more poison damage to plants
                    actualPoisonDamage *= 3;
                }
                EnemiesManager.instance.AddPoisonDamageToEnemy(enemyName, actualPoisonDamage, actualStatusDuration);
            }

            // freeze part, diminish enemy speed
            if (activeEffect == TongueEffect.FREEZE)
            {
                EnemiesManager.instance.ApplyFreezeEffect(enemyName, actualStatusDuration);
            }

            // curse part, increasing enemy speed
            if (tongueIsCursedTongueAndCurseIsActive)
            {
                // Tongue is the cursed tongue and it's been decided that the curse effect is active now
                // Duration is just overall duration of that tongue at the moment
                EnemiesManager.instance.ApplyCurseEffect(enemyName, actualStatusDuration);
            }
            else if (tongueIsRandomTongueAndEffectIsCurse)
            {
                // Tongue is the random tongue and it hits on a cursed part of the tongue
                // Duration is less than the overall duration (otherwise, that tongue becomes too bad)
                EnemiesManager.instance.ApplyCurseEffect(enemyName, actualStatusDuration / 5);
            }

            if (enemyIsDead)
            {
                //EnemiesManager.instance.SetEnemyDead(enemyName);
                SoundManager.instance.PlayEatBugSound();

                eatenFliesCount++;

                foreach (RunWeaponInfo weaponInfo in RunManager.instance.GetOwnedWeapons())
                {
                    if (weaponInfo.weaponItemData.weaponData.weaponType == tongueType)
                    {
                        weaponInfo.killCount++;
                    }
                }
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Enemy"))
        {
            ComputeCollision(collision, false);
        }
    }
}
