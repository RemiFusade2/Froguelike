using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
    public WeaponType weaponType;
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
    public List<GameObject> activeWeaponsOfSameTypeList;

    private Color tongueColor;

    private LineRenderer tongueLineRenderer;
    private LineRenderer outlineLineRenderer;

    private Collider2D tongueCollider;

    private bool isTongueGoingOut;

    private int eatenFliesCount;

    private List<string> enemiesHitNamesList;

    private Coroutine sendTongueCoroutine;

    // Start is called before the first frame update
    void Start()
    {
        lastAttackTime = Time.time;
        tongueCollider = GetComponent<Collider2D>();
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
        float actualWidth = tongueWidth * (1 + GameManager.instance.player.attackSizeBoost);        
        actualWidth *= 2;

        TongueLineRendererBehaviour tongueLineScript = this.GetComponent<TongueLineRendererBehaviour>();
        if (tongueLineScript != null)
        {
            tongueLineScript.SetLineRenderersWidth(actualWidth, outlineWeight, 1, weaponType != WeaponType.CAT);
        }
    }

    private void SetTongueScale(float scale)
    {
        float actualRange = range * (1 + GameManager.instance.player.attackRangeBoost);
        float actualWidth = tongueWidth * (1 + GameManager.instance.player.attackSizeBoost);
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
        if (weaponType != WeaponType.ROTATING)
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

        tongueLineRenderer = GetComponent<LineRenderer>();
        outlineLineRenderer = outlineTransform.GetComponent<LineRenderer>();

        this.GetComponent<TongueLineRendererBehaviour>().ResetLine();

        switch (weaponType)
        {
            case WeaponType.VAMPIRE:
                SetTongueColor(DataManager.instance.GetColorForWeaponEffect(WeaponEffect.VAMPIRE));
                break;
            case WeaponType.POISON:
                SetTongueColor(DataManager.instance.GetColorForWeaponEffect(WeaponEffect.POISON));
                break;
            case WeaponType.FREEZE:
                SetTongueColor(DataManager.instance.GetColorForWeaponEffect(WeaponEffect.FREEZE));
                break;
            case WeaponType.CURSED:
                SetTongueColor(DataManager.instance.GetColorForWeaponEffect(WeaponEffect.CURSE));
                break;
            default:
                SetTongueColor(DataManager.instance.GetColorForWeaponEffect(WeaponEffect.NONE));
                break;
        }
        
        eatenFliesCount = 0;

        if (sendTongueCoroutine != null)
        {
            StopCoroutine(sendTongueCoroutine);
        }
    }

    public void Initialize(RunWeaponData tongueData, List<GameObject> activeWeapons)
    {
        weaponType = tongueData.weaponType;

        this.GetComponent<TongueLineRendererBehaviour>().SetCurves(tongueData.frogMovementWeightOnTongue, tongueData.targetMovementWeightOnTongue);
        

        attackSpeed = (float)tongueData.weaponBaseStats.GetStatValue(WeaponStat.SPEED).value;
        cooldown = (float)tongueData.weaponBaseStats.GetStatValue(WeaponStat.COOLDOWN).value;
        damage = (float)tongueData.weaponBaseStats.GetStatValue(WeaponStat.DAMAGE).value;
        range = (float)tongueData.weaponBaseStats.GetStatValue(WeaponStat.RANGE).value;
        duration = (float)tongueData.weaponBaseStats.GetStatValue(WeaponStat.DURATION).value;

        knockbackForce = (float)tongueData.weaponBaseStats.GetStatValue(WeaponStat.KNOCKBACK).value;

        healthAbsorbRatio = (float)tongueData.weaponBaseStats.GetStatValue(WeaponStat.VAMPIRE_RATIO).value;
        poisonDamage = (float)tongueData.weaponBaseStats.GetStatValue(WeaponStat.POISON_DAMAGE).value;
        freezeEffect = (float)tongueData.weaponBaseStats.GetStatValue(WeaponStat.FREEZE_EFFECT).value > 0;
        curseChance = (float)tongueData.weaponBaseStats.GetStatValue(WeaponStat.CURSE_EFFECT).value;

        SetTongueWidth((float)tongueData.weaponBaseStats.GetStatValue(WeaponStat.SIZE).value);

        activeWeaponsOfSameTypeList = activeWeapons;
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
            weapon.activeWeaponsOfSameTypeList = otherWeaponsOfTheSameType;
        }

        TongueLineRendererBehaviour baseWeaponLineScript = weapon.gameObject.GetComponent<TongueLineRendererBehaviour>();
        this.GetComponent<TongueLineRendererBehaviour>().SetCurves(baseWeaponLineScript.frogMovementWeightOnTongue, baseWeaponLineScript.targetMovementWeightOnTongue);
        
        cooldown = weapon.cooldown;
        damage = weapon.damage;
        attackSpeed = weapon.attackSpeed;
        range = weapon.range;
        duration = weapon.duration;

        tongueTypeIndex = tongueTypeCount-1;
        activeWeaponsOfSameTypeList = otherWeaponsOfTheSameType;

        knockbackForce = weapon.knockbackForce;

        lastAttackTime = float.MaxValue;

        SetTongueWidth(weapon.tongueWidth);

        weaponType = weapon.weaponType;

        healthAbsorbRatio = weapon.healthAbsorbRatio;
        poisonDamage = weapon.poisonDamage;
        freezeEffect = weapon.freezeEffect;
        curseChance = weapon.curseChance;

        ResetTongue();

        if (weaponType == WeaponType.ROTATING)
        {
            preventAttack = true; // so this new tongue will not attack before the previous one stops it attack first
        }
    }

    public void LevelUp(RunWeaponItemLevel weaponItemLevel)
    {
        // Damage is additive
        damage += (float)weaponItemLevel.weaponStatUpgrades.GetStatValue(WeaponStat.DAMAGE).value;

        // Cooldown is multiplicative and negative
        cooldown *= (1+(float)weaponItemLevel.weaponStatUpgrades.GetStatValue(WeaponStat.COOLDOWN).value);
        
        attackSpeed *= (1+(float)weaponItemLevel.weaponStatUpgrades.GetStatValue(WeaponStat.SPEED).value);
        range *= (1+(float)weaponItemLevel.weaponStatUpgrades.GetStatValue(WeaponStat.RANGE).value);

        duration *= (1 + (float)weaponItemLevel.weaponStatUpgrades.GetStatValue(WeaponStat.DURATION).value);

        knockbackForce *= (1 + (float)weaponItemLevel.weaponStatUpgrades.GetStatValue(WeaponStat.KNOCKBACK).value);

        // Width
        float newTongueWidth = tongueWidth * (1 + (float)weaponItemLevel.weaponStatUpgrades.GetStatValue(WeaponStat.SIZE).value);
        SetTongueWidth(newTongueWidth);

        // Special attack: Vampire
        if (healthAbsorbRatio == 0)
        {
            // First time we add a Vampire effect
            healthAbsorbRatio = (float)weaponItemLevel.weaponStatUpgrades.GetStatValue(WeaponStat.VAMPIRE_RATIO).value;
        }
        else
        {
            healthAbsorbRatio *= (1 + (float)weaponItemLevel.weaponStatUpgrades.GetStatValue(WeaponStat.VAMPIRE_RATIO).value);
        }

        // Special attack: Poison
        poisonDamage += (float)weaponItemLevel.weaponStatUpgrades.GetStatValue(WeaponStat.POISON_DAMAGE).value;

        // Special attack: Freeze
        if (!freezeEffect)
        {
            // First time we add a Freeze effect
            freezeEffect = (weaponItemLevel.weaponStatUpgrades.GetStatValue(WeaponStat.FREEZE_EFFECT).value > 0);
        }

        // Special attack: Curse        
        if (curseChance <= 0)
        {
            // First time we add a Curse effect
            curseChance = (float)weaponItemLevel.weaponStatUpgrades.GetStatValue(WeaponStat.CURSE_EFFECT).value;
        }
        else
        {
            curseChance *= (1 + (float)weaponItemLevel.weaponStatUpgrades.GetStatValue(WeaponStat.CURSE_EFFECT).value);
        }
    }

    private GameObject GetNearestEnemy(int nearestRank = 1)
    {
        GameObject enemy = null;
        Vector2 weaponOriginPosition = this.transform.position; 
        float actualRange = range * (1 + GameManager.instance.player.attackRangeBoost);
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

    private List<WeaponEffect> GetRandomEffects()
    {
        List<WeaponEffect> result = new List<WeaponEffect>();
        float actualRange = range * (1 + GameManager.instance.player.attackRangeBoost);
        List<WeaponEffect> possibleEffects = new List<WeaponEffect>();
        if (healthAbsorbRatio > 0)
        {
            possibleEffects.Add(WeaponEffect.VAMPIRE);
        }
        if (poisonDamage > 0)
        {
            possibleEffects.Add(WeaponEffect.POISON);
        }
        if (freezeEffect)
        {
            possibleEffects.Add(WeaponEffect.FREEZE);
        }
        if (curseChance > 0)
        {
            possibleEffects.Add(WeaponEffect.CURSE);
        }

        WeaponEffect previousEffect = WeaponEffect.NONE;
        while (result.Count < 4)
        {
            WeaponEffect newEffect = possibleEffects[Random.Range(0, possibleEffects.Count)];
            if (newEffect != previousEffect)
            {
                result.Add(newEffect);
                previousEffect = newEffect;
            }
        }

        return result;
    }

    private List<Vector2> GetPositionsTowardsEnemy(GameObject targetEnemy, LineShape lineShape = LineShape.STRAIGHT_LINE, bool stopAtTarget = false)
    {
        List<Vector2> result = new List<Vector2>();
        
        float actualRange = range * (1 + GameManager.instance.player.attackRangeBoost);
        Vector2 direction = Vector2.up;
        float distanceToTarget = actualRange;

        if (targetEnemy != null)
        {
            direction = (targetEnemy.transform.position - this.transform.position).normalized;
            distanceToTarget = (targetEnemy.transform.position - this.transform.position).magnitude;
        }

        float distanceFromFrog = 0;
        float frequency = 0.6f;
        float amplitude = 0.3f;
        float t = 0;
        Vector2 upVector = new Vector2(-direction.y, direction.x);
        bool switched = false;
        float offsetFromCenter = 0;
        float offsetFromCenterDirection = 1;
        Vector2 lastPosition = Vector2.zero;
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
                amplitude = 0.3f;
                t = 0;
                while (distanceFromFrog < actualRange && (!stopAtTarget || distanceFromFrog < distanceToTarget))
                {
                    result.Add(distanceFromFrog * direction + amplitude * Mathf.Sin(t * 2 * Mathf.PI) * upVector);
                    distanceFromFrog += 0.1f;
                    t += 0.1f * frequency;
                }
                break;
            case LineShape.TRIANGLE_LINE:
                frequency = 1.5f;
                amplitude = 0.5f;
                t = 0;
                offsetFromCenterDirection = 1;
                while (distanceFromFrog < actualRange && (!stopAtTarget || distanceFromFrog < distanceToTarget))
                {
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
                bool lastMoveWasUp = false;
                lastPosition = Vector2.zero;
                t = 0;
                result.Add(lastPosition);
                while (distanceFromFrog < actualRange && (!stopAtTarget || distanceFromFrog < distanceToTarget))
                {
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

                int loopState = 0; // 6 steps
                lastPosition = Vector2.zero;
                t = 0;
                result.Add(lastPosition);
                float loopAngle = 0;

                float loopBigRadius = 1.8f;
                float loopSmallRadius = 0.8f;
                float loopMediumRadius = 1.3f;
                float loopRadius = loopBigRadius;

                while (distanceFromFrog < actualRange && (!stopAtTarget || distanceFromFrog < distanceToTarget))
                {
                    switch(loopState)
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
        float actualRange = range * (1 + GameManager.instance.player.attackRangeBoost);
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
        float actualCooldown = cooldown * (1 + GameManager.instance.player.attackCooldownBoost);
        if (!preventAttack && !isAttacking && Time.time - lastAttackTime > actualCooldown)
        {
            GameObject targetEnemy = null;
            List<Vector2> pos = new List<Vector2>();
            List<WeaponEffect> effects = new List<WeaponEffect>();

            // Pick an enemy to target
            bool tongueDoesTarget = (weaponType == WeaponType.CLASSIC || weaponType == WeaponType.CURSED || weaponType == WeaponType.FREEZE || weaponType == WeaponType.POISON || weaponType == WeaponType.QUICK || weaponType == WeaponType.VAMPIRE || weaponType == WeaponType.WIDE);
            if (tongueDoesTarget)
            {
                targetEnemy = GetNearestEnemy(tongueTypeIndex + 1);
            }

            // Update width 
            SetTongueWidth(tongueWidth);

            if (tongueDoesTarget && targetEnemy != null)
            {
                // Set positions
                if (weaponType == WeaponType.POISON)
                {
                    pos = GetPositionsTowardsEnemy(targetEnemy, LineShape.SINUSOID_LINE, false);
                    effects.Add(WeaponEffect.POISON);
                }
                else if (weaponType == WeaponType.FREEZE)
                {
                    pos = GetPositionsTowardsEnemy(targetEnemy, LineShape.TRIANGLE_LINE, false);
                    effects.Add(WeaponEffect.FREEZE);
                }
                else if (weaponType == WeaponType.VAMPIRE)
                {
                    pos = GetPositionsTowardsEnemy(targetEnemy, LineShape.LOOPS_LINE, false);
                    effects.Add(WeaponEffect.VAMPIRE);
                }
                else if (weaponType == WeaponType.CURSED)
                {
                    pos = GetPositionsTowardsEnemy(targetEnemy, LineShape.SQUARE_LINE, false);
                    effects.Add(WeaponEffect.CURSE);
                }
                else if (weaponType == WeaponType.WIDE)
                {
                    pos = GetPositionsTowardsEnemy(targetEnemy, LineShape.STRAIGHT_LINE, false);
                    effects.Add(WeaponEffect.NONE);
                }
                else
                {
                    pos = GetPositionsTowardsEnemy(targetEnemy, LineShape.STRAIGHT_LINE, true);
                    effects.Add(WeaponEffect.NONE);
                }
                this.GetComponent<TongueLineRendererBehaviour>().InitializeTongue(pos, effects, targetEnemy);
                this.GetComponent<TongueLineRendererBehaviour>().DisplayTongue(0);

                AttackLine(this.GetComponent<TongueLineRendererBehaviour>());
            }

            if (weaponType == WeaponType.RANDOM)
            {
                // Set random positions
                pos = GetRandomPositions();
                effects = GetRandomEffects();
                this.GetComponent<TongueLineRendererBehaviour>().InitializeTongue(pos, effects);
                this.GetComponent<TongueLineRendererBehaviour>().DisplayTongue(0);

                AttackLine(this.GetComponent<TongueLineRendererBehaviour>());
            }

            if (weaponType == WeaponType.ROTATING)
            {
                pos = GetPositionsTowardsEnemy(null, LineShape.SPIRAL_LINE, false);
                effects.Add(WeaponEffect.NONE);
                this.GetComponent<TongueLineRendererBehaviour>().InitializeTongue(pos, effects);
                this.GetComponent<TongueLineRendererBehaviour>().DisplayTongue(0);
                AttackLineRotating(this.GetComponent<TongueLineRendererBehaviour>());
            }

            if (weaponType == WeaponType.CAT)
            {
                pos = GetPositionsTowardsEnemy(null, LineShape.STRAIGHT_LINE, false);
                effects.Add(WeaponEffect.NONE);
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

        float actualAttackDuration = duration * (1 + GameManager.instance.player.attackDurationBoost); // in seconds
        float actualAttackSpeed = attackSpeed * (1 + GameManager.instance.player.attackSpeedBoost);
        float tongueLength = tongueScript.GetTongueLength();

        float angle = 0;
        angle += (tongueTypeIndex * 2 * Mathf.PI / tongueTypeCount);
        
        while (isTongueGoingOut)
        {
            // Set width
            float actualWidth = tongueWidth * (1 + GameManager.instance.player.attackSizeBoost);
            actualWidth *= 2;

            TongueLineRendererBehaviour tongueLineScript = this.GetComponent<TongueLineRendererBehaviour>();
            if (tongueLineScript != null)
            {
                tongueLineScript.SetLineRenderersWidth(actualWidth, outlineWeight, tongueLength * t, weaponType != WeaponType.CAT);
            }

            if (t <= 1)
            {
                tongueScript.DisplayTongue(t);
                t += (Time.fixedDeltaTime * (actualAttackSpeed / tongueLength));
            }
            else
            {
                tongueScript.DisplayTongue(1);
                actualAttackDuration -= Time.fixedDeltaTime;
                if (actualAttackDuration < 0)
                {
                    isTongueGoingOut = false;
                }
            }


            actualAttackSpeed = attackSpeed * (1 + GameManager.instance.player.attackSpeedBoost);
            angle += actualAttackSpeed * Time.fixedDeltaTime;

            SetTongueDirection((Mathf.Cos(angle) * Vector2.right + Mathf.Sin(angle) * Vector2.up).normalized);

            yield return new WaitForFixedUpdate();
        }
        t = 1;
        while (t > 0)
        {
            // Set width
            float actualWidth = tongueWidth * (1 + GameManager.instance.player.attackSizeBoost);
            actualWidth *= 2;

            TongueLineRendererBehaviour tongueLineScript = this.GetComponent<TongueLineRendererBehaviour>();
            if (tongueLineScript != null)
            {
                tongueLineScript.SetLineRenderersWidth(actualWidth, outlineWeight, tongueLength * t, weaponType != WeaponType.CAT);
            }

            tongueScript.DisplayTongue(t);
            t -= (Time.fixedDeltaTime * (actualAttackSpeed / tongueLength));

            actualAttackSpeed = attackSpeed * (1 + GameManager.instance.player.attackSpeedBoost);
            angle += actualAttackSpeed * Time.fixedDeltaTime;

            SetTongueDirection((Mathf.Cos(angle) * Vector2.right + Mathf.Sin(angle) * Vector2.up).normalized);
            yield return new WaitForFixedUpdate();
        }

        tongueScript.DisableTongue();

        isAttacking = false;
        lastAttackTime = Time.time;
        preventAttack = false;

        foreach (GameObject weapon in activeWeaponsOfSameTypeList)
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

        float t = 0;
        isTongueGoingOut = true;

        tongueScript.EnableTongue();

        float actualAttackSpeed = attackSpeed * (1 + GameManager.instance.player.attackSpeedBoost);
        float actualRange = range * (1 + GameManager.instance.player.attackRangeBoost);
        float tongueLength = tongueScript.GetTongueLength();

        while (isTongueGoingOut)
        {
            // Set width
            float actualWidth = tongueWidth * (1 + GameManager.instance.player.attackSizeBoost);
            actualWidth *= 2;

            TongueLineRendererBehaviour tongueLineScript = this.GetComponent<TongueLineRendererBehaviour>();
            if (tongueLineScript != null)
            {
                tongueLineScript.SetLineRenderersWidth(actualWidth, outlineWeight, actualRange * t, weaponType != WeaponType.CAT);
            }

            // Display tongue
            tongueScript.DisplayTongue(t);
            t += (Time.fixedDeltaTime * (actualAttackSpeed / tongueLength));

            // Rotate tongue only if it's cat tongue (it follows character orientation)
            if (weaponType == WeaponType.CAT)
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
            float actualWidth = tongueWidth * (1 + GameManager.instance.player.attackSizeBoost);
            actualWidth *= 2;

            TongueLineRendererBehaviour tongueLineScript = this.GetComponent<TongueLineRendererBehaviour>();
            if (tongueLineScript != null)
            {
                tongueLineScript.SetLineRenderersWidth(actualWidth, outlineWeight, actualRange * t, weaponType != WeaponType.CAT);
            }

            // Display tongue
            tongueScript.DisplayTongue(t);
            t -= (Time.fixedDeltaTime * (actualAttackSpeed / tongueLength));

            // Rotate tongue only if it's cat tongue (it follows character orientation)
            if (weaponType == WeaponType.CAT)
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
        foreach (GameObject weapon in activeWeaponsOfSameTypeList)
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
        tongueCollider.enabled = true;
        float actualAttackSpeed = attackSpeed * (1 + GameManager.instance.player.attackSpeedBoost);
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
        tongueCollider.enabled = false;
        lastAttackTime = Time.time;
        isAttacking = false;
        preventAttack = false;
        enemiesHitNamesList.Clear();

        foreach (GameObject weapon in activeWeaponsOfSameTypeList)
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
        tongueCollider.enabled = true;
        float actualAttackSpeed = attackSpeed * (1+GameManager.instance.player.attackSpeedBoost);
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
        tongueCollider.enabled = false;
        lastAttackTime = Time.time;
        isAttacking = false;        
        preventAttack = false;
        enemiesHitNamesList.Clear();

        foreach (GameObject weapon in activeWeaponsOfSameTypeList)
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
        if (!enemiesHitNamesList.Contains(enemyName))
        {
            enemiesHitNamesList.Add(enemyName); // to prevent a single tongue to hit the same enemy multiple times
            if (weaponType == WeaponType.ROTATING)
            {
                float actualAttackSpeed = attackSpeed * (1 + GameManager.instance.player.attackSpeedBoost);
                StartCoroutine(RemoveEnemyNameWithDelay(enemyName, 1.0f / actualAttackSpeed));
            }

            EnemyInstance enemy = EnemiesManager.instance.GetEnemyInstanceFromGameObjectName(enemyName);
            float actualDamage = tipFactor * ( damage * (1 + GameManager.instance.player.attackDamageBoost) );

            WeaponEffect activeEffect = WeaponEffect.NONE;
            switch (weaponType)
            {
                case WeaponType.RANDOM:
                    TongueLineRendererBehaviour script = this.GetComponent<TongueLineRendererBehaviour>();
                    activeEffect = script.GetEffectFromCollider(collision);
                    break;
                case WeaponType.CURSED:
                    activeEffect = WeaponEffect.CURSE;
                    break;
                case WeaponType.VAMPIRE:
                    activeEffect = WeaponEffect.VAMPIRE;
                    break;
                case WeaponType.POISON:
                    activeEffect = WeaponEffect.POISON;
                    break;
                case WeaponType.FREEZE:
                    activeEffect = WeaponEffect.FREEZE;
                    break;
            }

            // curse part, increase enemy speed
            bool isCursed = false;
            if (activeEffect == WeaponEffect.CURSE)
            {
                // Apply curseFactor as a probability of curse
                float curseProbability = curseChance * (1 + GameManager.instance.player.attackSpecialStrengthBoost);
                if (Random.Range(0.0f, 1.0f) < curseProbability)
                {
                    isCursed = true;
                    actualDamage = 0;
                }
            }

            bool enemyIsDead = EnemiesManager.instance.DamageEnemy(enemyName, actualDamage, this.transform);

            // vampire part, absorb part of damage done
            if (activeEffect == WeaponEffect.VAMPIRE)
            {
                float healAmount = actualDamage * healthAbsorbRatio;
                GameManager.instance.player.Heal(healAmount);
            }

            // poison part, add poison damage to enemy
            if (activeEffect == WeaponEffect.POISON)
            {
                float actualPoisonDamage = poisonDamage * (1 + GameManager.instance.player.attackSpecialStrengthBoost);
                float actualPoisonDuration = duration * (1 + GameManager.instance.player.attackSpecialDurationBoost);
                EnemiesManager.instance.AddPoisonDamageToEnemy(enemyName, actualPoisonDamage, actualPoisonDuration);
            }

            // freeze part, diminish enemy speed
            if (activeEffect == WeaponEffect.FREEZE)
            {
                float enemyFreezeDuration = duration * (1 + GameManager.instance.player.attackSpecialDurationBoost);
                EnemiesManager.instance.ApplyFreezeEffect(enemyName, enemyFreezeDuration);
            }
            if (isCursed)
            {
                float enemyCurseDuration = duration * (1 + GameManager.instance.player.attackSpecialDurationBoost);
                EnemiesManager.instance.ApplyCurseEffect(enemyName, enemyCurseDuration);
            }

            if (enemyIsDead)
            {
                //EnemiesManager.instance.SetEnemyDead(enemyName);
                SoundManager.instance.PlayEatBugSound();

                eatenFliesCount++;

                foreach (RunWeaponInfo weaponInfo in RunManager.instance.GetOwnedWeapons())
                {
                    if (weaponInfo.weaponItemData.weaponData.weaponType == weaponType)
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
