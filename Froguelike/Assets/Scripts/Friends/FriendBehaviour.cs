using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FriendBehaviour : MonoBehaviour
{
    [Header("References")]
    public GameObject friendGameObject;
    public Transform weaponPositionTransform;
    public WeaponBehaviour weapon;
    public Rigidbody2D friendRigidbody;
    public Animator friendAnimator;
    public SpringJoint2D springJoint;

    [Header("Runtime")]
    public FriendInfo _friendInfo;
    //public Vector2 _startPosition;

    public void Initialize(FriendInfo info, Vector2 position)
    {
        _friendInfo = info;
        //_startPosition = position;
        friendGameObject.SetActive(true);
        SetPosition(position);

        springJoint.connectedBody = GameManager.instance.player.GetRigidbody();        
        springJoint.distance = Random.Range(info.springDistanceMinMax.x, info.springDistanceMinMax.y);
        springJoint.frequency = Random.Range(info.springFrequencyMinMax.x, info.springFrequencyMinMax.y);
        springJoint.dampingRatio = Random.Range(info.springDampingMinMax.x, info.springDampingMinMax.y);

        weapon.gameObject.SetActive(true);
        weapon.ResetTongue();
        friendAnimator.SetInteger("Style", _friendInfo.style);
    }

    public FriendType GetFriendType()
    {
        return _friendInfo.friendType;
    }

    public void TryAttack()
    {
        weapon.TryAttack();
    }

    public void SetPosition(Vector2 position)
    {
        friendGameObject.transform.localPosition = position;
    }

    public void UpdateOrientation()
    {
        float friendOrientationAngle = 90 + 90 * Mathf.RoundToInt((Vector2.SignedAngle(friendRigidbody.velocity.normalized, Vector2.right)) / 90);
        friendGameObject.transform.localRotation = Quaternion.Euler(0, 0, -friendOrientationAngle);
        weapon.SetTonguePosition(weaponPositionTransform);
    }

    public void ClampSpeed()
    {
        float friendSpeed = Mathf.Clamp(friendRigidbody.velocity.magnitude, 0, 3);
        friendAnimator.SetFloat("Speed", friendSpeed);
    }
}
