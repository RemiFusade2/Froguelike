using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class TongueTipBehaviour : MonoBehaviour
{
    public WeaponBehaviour parentTongue;
    public CircleCollider2D tipCollider;

    public void SetColliderEnabled(bool enable)
    {
        tipCollider.enabled = enable;
    }

    public void SetTipPositionAndRadius(Vector2 offset, float radius)
    {
        SetColliderEnabled(true);
        tipCollider.offset = offset;
        tipCollider.radius = radius;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Enemy"))
        {
            parentTongue.ComputeCollision(collision, true);
        }
    }
}
