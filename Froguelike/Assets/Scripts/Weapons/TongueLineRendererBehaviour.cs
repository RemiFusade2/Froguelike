using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TongueLineRendererBehaviour : MonoBehaviour
{
    [Header("References")]
    public LineRenderer tongueLineRenderer;
    public LineRenderer tongueOutlineLineRenderer;
    public TongueTipBehaviour tongueTip;

    [Header("Settings")]
    public float colliderRadius = 0.1f;

    [Header("Runtime")]
    public AnimationCurve frogMovementWeightOnTongue;
    public AnimationCurve targetMovementWeightOnTongue;

    private List<Collider2D> tongueColliderComponentsList;

    private List<Vector2> tonguePositionsList;
    private List<WeaponEffect> tongueEffectsList;

    private Vector3 originFrogWhenInitialized;

    private bool followTarget;
    private Vector3 originTargetWhenInitialized;
    private GameObject targetGameObject;
    private Vector2 lastTargetMoveVector;

    private void Awake()
    {
        tongueColliderComponentsList = new List<Collider2D>();
    }

    private void Start()
    {
        tongueColliderComponentsList.Add(this.GetComponent<CircleCollider2D>());
        (tongueColliderComponentsList[0] as CircleCollider2D).radius = colliderRadius;
        SetCollidersEnabled(false);
    }

    private void SetCollidersEnabled(bool enable)
    {
        foreach (Collider2D col in tongueColliderComponentsList)
        {
            if (!enable)
            {
                col.offset = Vector2.zero;
            }
            col.enabled = enable;
        }
        tongueTip.SetColliderEnabled(enable);
    }

    public void SetCurves(AnimationCurve frogWeightCurve, AnimationCurve targetWeightCurve)
    {
        frogMovementWeightOnTongue = frogWeightCurve;
        targetMovementWeightOnTongue = targetWeightCurve;
    }

    public void ResetLine()
    {
        tonguePositionsList = new List<Vector2>();
        tongueEffectsList = new List<WeaponEffect>();
        followTarget = false;
        targetGameObject = null;
        originFrogWhenInitialized = Vector3.zero;
        originTargetWhenInitialized = Vector3.zero;
        DisableTongue();
    }

    public void SetLineRenderersWidth(float width, float outlineWeight, float length, bool withFrogTongueTip)
    {
        float tipRatio = (length - 0.5f) / (length+0.01f);
        tipRatio = Mathf.Clamp(tipRatio, 0, 1);

        // The tongue line renderer goes from 0.5 * width for its root and 1 * width for the tip
        Keyframe[] tongueLineRendererWidthKeys = new Keyframe[3];
        if (withFrogTongueTip)
        {
            tongueLineRendererWidthKeys[0] = new Keyframe(0, 0.5f, 0, 0);
            tongueLineRendererWidthKeys[1] = new Keyframe(tipRatio, 0.5f, 0, 3);
            tongueLineRendererWidthKeys[2] = new Keyframe(1, 1, 0, 0);
        }
        else
        {
            tongueLineRendererWidthKeys[0] = new Keyframe(0, 1, 0, 0);
            tongueLineRendererWidthKeys[1] = new Keyframe(0.5f, 1, 0, 0);
            tongueLineRendererWidthKeys[2] = new Keyframe(1, 1, 0, 0);
        }
        AnimationCurve tongueLineRendererWidthCurve = new AnimationCurve(tongueLineRendererWidthKeys);
        tongueLineRenderer.widthCurve = tongueLineRendererWidthCurve;
        tongueLineRenderer.widthMultiplier = width;

        // The outline renderer does about the same thing, except the root width is (0.5 * width + outline * 2) and the tip width is (1 * width + outline * 2)
        Keyframe[] outlineLineRendererWidthKeys = new Keyframe[3];
        float outlineRootWidthRatio = (0.5f * width + outlineWeight * 2) / (width + outlineWeight * 2);
        if (withFrogTongueTip)
        {
            outlineLineRendererWidthKeys[0] = new Keyframe(0, outlineRootWidthRatio, 0, 0);
            outlineLineRendererWidthKeys[1] = new Keyframe(tipRatio, outlineRootWidthRatio, 0, 5);
            outlineLineRendererWidthKeys[2] = new Keyframe(1, 1, 0, 0);
        }
        else
        {
            outlineLineRendererWidthKeys[0] = new Keyframe(0, 1, 0, 0);
            outlineLineRendererWidthKeys[1] = new Keyframe(0.5f, 1, 0, 0);
            outlineLineRendererWidthKeys[2] = new Keyframe(1, 1, 0, 0);
        }
        AnimationCurve outlineLineRendererWidthCurve = new AnimationCurve(outlineLineRendererWidthKeys);
        tongueOutlineLineRenderer.widthCurve = outlineLineRendererWidthCurve;
        tongueOutlineLineRenderer.widthMultiplier = width + outlineWeight * 2;
    }

    /// <summary>
    /// Set the coordinates and colors of the tongue. Prepare for following a target (optional)
    /// </summary>
    /// <param name="newPositions"></param>
    /// <param name="newEffects"></param>
    /// <param name="targetedEnemy"></param>
    public void InitializeTongue(List<Vector2> newPositions, List<WeaponEffect> newEffects, GameObject targetedEnemy = null)
    {
        tonguePositionsList = newPositions;
        tongueEffectsList = newEffects;
        originFrogWhenInitialized = this.transform.position;

        followTarget = (targetedEnemy != null);
        if (followTarget)
        {
            originTargetWhenInitialized = targetedEnemy.transform.position;
            targetGameObject = targetedEnemy;
        }

        DisplayTongue(0);
    }

    public WeaponEffect GetEffectFromCollider(Collider2D collider)
    {
        WeaponEffect effect = WeaponEffect.NONE;
        float colT = 0;
        float minColT = 0;
        float minDistance = float.MaxValue;
        foreach (Collider2D col in tongueColliderComponentsList)
        {
            if (col.offset.x != 0 || col.offset.y != 0)
            {
                float distance = Vector2.Distance(collider.transform.position, col.transform.position + new Vector3(col.offset.x, col.offset.y, 0));
                if (distance < minDistance)
                {
                    minDistance = distance;
                    minColT = colT;
                }
            }
            colT += (1.0f / tongueColliderComponentsList.Count);
        }
        int effectIndex = Mathf.FloorToInt(minColT * tongueEffectsList.Count);
        effectIndex = Mathf.Clamp(effectIndex, 0, tongueEffectsList.Count - 1);
        effect = tongueEffectsList[effectIndex];

        //Debug.Log($"Got effect from collider. minDistance = {minDistance} ; minColT = {minColT} ; effectIndex = {effectIndex}");

        return effect;
    }

    public float GetTongueLength()
    {
        float length = 0;
        Vector2 previousPos = Vector2.zero;
        foreach (Vector2 pos in tonguePositionsList)
        {
            length += Vector2.Distance(pos, previousPos);
            previousPos = pos;
        }
        return length;
    }

    /// <summary>
    /// Display the tongue using saved coordinates. Also place the colliders along the way, and set the colors.
    /// Parameter decides how much of the tongue is displayed (0 for 0%, 1 for 100%)
    /// </summary>
    /// <param name="t"></param>
    public void DisplayTongue(float t = 0)
    {
        Vector2 frogMovementVector = this.transform.position - originFrogWhenInitialized;
        Vector2 targetMovementVector = Vector2.zero;
        if (followTarget)
        {
            // Get enemy info
            EnemyInstance enemyInfo = null;
            if (!targetGameObject.name.Equals(EnemiesManager.pooledEnemyNameStr))
            {
                enemyInfo = EnemiesManager.instance.GetEnemyInstanceFromGameObjectName(targetGameObject.name);
            }

            if (enemyInfo == null || !enemyInfo.active || !enemyInfo.alive)
            {
                targetMovementVector = lastTargetMoveVector;
            }
            else
            {
                targetMovementVector = targetGameObject.transform.position - originTargetWhenInitialized;
                lastTargetMoveVector = targetMovementVector;
            }
        }

        tongueLineRenderer.positionCount = Mathf.RoundToInt(tonguePositionsList.Count * t);
        tongueOutlineLineRenderer.positionCount = Mathf.RoundToInt(tonguePositionsList.Count * t);

        int colliderIndex = 0;
        Collider2D previousCollider = tongueColliderComponentsList[colliderIndex];
        previousCollider.offset = Vector2.zero;

        Gradient gradient = new Gradient();
        List<GradientColorKey> colorKeys = new List<GradientColorKey>();
        List<GradientAlphaKey> alphaKeys = new List<GradientAlphaKey>();
        int effectIndex = 0;
        WeaponEffect previousEffect = tongueEffectsList[effectIndex];
        colorKeys.Add(new GradientColorKey(DataManager.instance.GetColorForWeaponEffect(previousEffect), 0));
        alphaKeys.Add(new GradientAlphaKey(1, 0));

        float actualColliderRadius = colliderRadius * (GetComponent<WeaponBehaviour>().tongueWidth * (1 + GameManager.instance.player.GetAttackSizeBoost())) * 10;

        float epsilon = 0.001f;

        int index = 0;
        Vector2 lastPos = Vector2.zero;
        foreach (Vector2 pos in tonguePositionsList)
        {
            if (index < tongueLineRenderer.positionCount)
            {
                float currentT = (index * 1.0f / tonguePositionsList.Count);
                currentT = Mathf.Clamp(currentT, 0, 1);

                float frogMovementWeight = frogMovementWeightOnTongue.Evaluate(currentT);
                Vector2 actualPos = pos - frogMovementVector * (1-frogMovementWeight);

                if (followTarget)
                {
                    float targetMovementWeight = targetMovementWeightOnTongue.Evaluate(currentT);
                    actualPos = actualPos + targetMovementVector * targetMovementWeight;
                }

                // Set position on the line renderers
                Vector3 linePointPosition = new Vector3(actualPos.x, actualPos.y, 0);
                tongueLineRenderer.SetPosition(index, linePointPosition);
                tongueOutlineLineRenderer.SetPosition(index, linePointPosition);

                // Set gradient on the tongue line renderer
                int newEffectIndex = Mathf.FloorToInt(currentT * tongueEffectsList.Count);
                if (newEffectIndex > effectIndex && t > 0)
                {
                    effectIndex = newEffectIndex;
                    colorKeys.Add(new GradientColorKey(DataManager.instance.GetColorForWeaponEffect(previousEffect), (currentT - epsilon) / t));
                    alphaKeys.Add(new GradientAlphaKey(1, (currentT - epsilon)/t));
                    previousEffect = tongueEffectsList[effectIndex];
                    colorKeys.Add(new GradientColorKey(DataManager.instance.GetColorForWeaponEffect(previousEffect), currentT/t));
                    alphaKeys.Add(new GradientAlphaKey(1, currentT/t));
                }
                
                // Eventually add a collider (trigger) on the way
                /*
                float distanceToPreviousCollider = Vector2.Distance(previousCollider.offset, actualPos);
                if (distanceToPreviousCollider > (actualColliderRadius * 2))
                {
                    colliderIndex++;
                    if (colliderIndex < tongueColliderComponentsList.Count)
                    {
                        previousCollider = tongueColliderComponentsList[colliderIndex];
                    }
                    else
                    {
                        previousCollider = this.gameObject.AddComponent<CircleCollider2D>();
                        (previousCollider as CircleCollider2D).radius = colliderRadius;
                        previousCollider.isTrigger = true;
                        tongueColliderComponentsList.Add(previousCollider);
                    }
                    (previousCollider as CircleCollider2D).radius = actualColliderRadius;
                    previousCollider.offset = actualPos;
                }*/
                lastPos = actualPos;
            }
            index++;
        }

        // Add last collider (tip of tongue)
        tongueTip.SetTipPositionAndRadius(lastPos, actualColliderRadius + 0.1f);
        /*
        if (colliderIndex < tongueColliderComponentsList.Count)
        {
            previousCollider = tongueColliderComponentsList[colliderIndex];
        }
        else
        {
            previousCollider = this.gameObject.AddComponent<CircleCollider2D>();
            (previousCollider as CircleCollider2D).radius = colliderRadius;
            previousCollider.isTrigger = true;
            tongueColliderComponentsList.Add(previousCollider);
        }
        (previousCollider as CircleCollider2D).radius = actualColliderRadius * 2;
        previousCollider.offset = lastPos;*/

        // All remaining colliders are set to zero (unused)
        (previousCollider as CircleCollider2D).radius = 0;
        previousCollider.offset = Vector2.zero;
        for (int i = colliderIndex+1; i < tongueColliderComponentsList.Count; i++)
        {
            (tongueColliderComponentsList[i] as CircleCollider2D).radius = 0;
            tongueColliderComponentsList[i].offset = Vector2.zero;
        }

        // Set gradient
        colorKeys.Add(new GradientColorKey(DataManager.instance.GetColorForWeaponEffect(previousEffect), 1));
        alphaKeys.Add(new GradientAlphaKey(1, 1));
        gradient.SetKeys(colorKeys.ToArray(), alphaKeys.ToArray());
        tongueLineRenderer.colorGradient = gradient; 
    }

    public void EnableTongue()
    {
        tongueLineRenderer.enabled = true;
        tongueOutlineLineRenderer.enabled = true;
        SetCollidersEnabled(true);
    }

    public void DisableTongue()
    {
        tongueLineRenderer.enabled = false;
        tongueOutlineLineRenderer.enabled = false;
        SetCollidersEnabled(false);
    }
}
