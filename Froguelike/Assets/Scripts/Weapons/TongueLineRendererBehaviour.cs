using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TongueLineRendererBehaviour : MonoBehaviour
{
    public LineRenderer tongueLineRenderer;
    public LineRenderer tongueOutlineLineRenderer;

    public float colliderRadius = 0.12f;

    private List<Collider2D> tongueColliderComponentsList;

    private List<Vector2> tonguePositionsList;
    private List<WeaponEffect> tongueEffectsList;

    private void Start()
    {
        tongueColliderComponentsList = new List<Collider2D>();
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
    }

    /// <summary>
    /// Set the coordinates, width and colors of the tongue.
    /// </summary>
    /// <param name="newPositions"></param>
    /// <param name="width"></param>
    public void InitializeTongue(List<Vector2> newPositions, List<WeaponEffect> newEffects, float width)
    {
        tonguePositionsList = newPositions;
        tongueEffectsList = newEffects;
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

        return effect;
    }

    /// <summary>
    /// Display the tongue using saved coordinates. Also place the colliders along the way, and set the colors.
    /// Parameter decides how much of the tongue is displayed (0 for 0%, 1 for 100%)
    /// </summary>
    /// <param name="t"></param>
    public void DisplayTongue(float t = 0)
    {
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

        float actualColliderRadius = colliderRadius * (GetComponent<WeaponBehaviour>().tongueWidth * (1 + GameManager.instance.player.attackSizeBoost)) * 10;

        float epsilon = 0.001f;

        int index = 0;
        Vector2 lastPos = Vector2.zero;
        foreach (Vector2 pos in tonguePositionsList)
        {
            if (index < tongueLineRenderer.positionCount)
            {
                // Set position on the line renderers
                tongueLineRenderer.SetPosition(index, new Vector3(pos.x, pos.y, 0));
                tongueOutlineLineRenderer.SetPosition(index, new Vector3(pos.x, pos.y, 0));

                // Set gradient on the tongue line renderer
                float currentT = (index * 1.0f / tonguePositionsList.Count);
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
                float distanceToPreviousCollider = Vector2.Distance(previousCollider.offset, pos);
                if ((distanceToPreviousCollider > (actualColliderRadius * 2)) || (t >= 1 && index == tonguePositionsList.Count - 1))
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
                    previousCollider.offset = pos;
                }
                lastPos = pos;
            }
            index++;
        }

        // Add last collider (tip of tongue)
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
        (previousCollider as CircleCollider2D).radius = actualColliderRadius * 2;
        previousCollider.offset = lastPos;

        // All remaining colliders are set to zero (unused)
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
