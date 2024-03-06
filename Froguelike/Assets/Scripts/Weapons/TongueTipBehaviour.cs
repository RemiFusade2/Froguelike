using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class TongueTipBehaviour : MonoBehaviour
{
    [Header("References")]
    public WeaponBehaviour parentTongue;
    public CircleCollider2D tipCollider;
    public EdgeCollider2D tongueEdgeCollider;

    [Header("Settings")]
    public bool useEdgeCollider;

    /// <summary>
    /// Enable/Disable tongue collider. Either tip of whole tongue.
    /// </summary>
    /// <param name="enable"></param>
    public void SetColliderEnabled(bool enable)
    {
        if (useEdgeCollider)
        {
            tongueEdgeCollider.enabled = enable;
        }
        else
        {
            tipCollider.enabled = enable;
        }
    }




    /// <summary>
    /// Set the tongue collider using an edge collider.
    /// Use the line renderer positions to build the edge path.
    /// </summary>
    /// <param name="tongueLineRenderer"></param>
    /// <param name="tipOffset"></param>
    /// <param name="tipRadius"></param>
    public void SetTongueEdgeCollider(LineRenderer tongueLineRenderer, Vector2 tipOffset, float tipRadius)
    {
        SetColliderEnabled(true);
        if (useEdgeCollider)
        {
            List<Vector2> points = new List<Vector2>();
            Vector3 lastPosition = Vector3.zero;
            Vector2 lastPoint = Vector2.zero;
            points.Add(Vector2.zero);
            float minDistanceToAddPointToPath = 0.5f; // this will increase with distance from center so edge collider is more accurate towards center
            for (int i = 0; i < tongueLineRenderer.positionCount; i++)
            {
                Vector3 position = tongueLineRenderer.GetPosition(i);
                float distanceWithLastRecordedPosition = Vector3.Distance(lastPosition, position);
                if (distanceWithLastRecordedPosition >= minDistanceToAddPointToPath)
                {
                    lastPoint = new Vector2(position.x, position.y);
                    points.Add(lastPoint);
                    minDistanceToAddPointToPath += 0.2f;
                    lastPosition = position;
                }
            }
            points.Add(tipOffset + tipRadius * (tipOffset - lastPoint).normalized);
            tongueEdgeCollider.SetPoints(points);
        }
    }

    /// <summary>
    /// Set the tongue collider given by using the tip offset compared to tongue base and its radius (that depends on tongue's width).
    /// </summary>
    /// <param name="offset"></param>
    /// <param name="radius"></param>
    public void SetTipPositionAndRadius(Vector2 offset, float radius)
    {
        SetColliderEnabled(true);
        if (!useEdgeCollider)
        {
            // We're only using the tip so placing the collider is trivial
            tipCollider.offset = offset;
            tipCollider.radius = radius;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Enemy"))
        {
            parentTongue.ComputeCollision(collision, true);
        }
    }
}
