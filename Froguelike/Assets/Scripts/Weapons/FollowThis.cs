using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowThis : MonoBehaviour
{

    public Transform followThis;

    private void FixedUpdate()
    {
        transform.position = followThis.position;
    }
}
