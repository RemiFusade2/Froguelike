using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Froguelike_MainCamera : MonoBehaviour
{
    public Transform targetTransform;
    public float zDistance = -10;


    private void Update()
    {
        this.transform.position = targetTransform.position + zDistance*Vector3.forward;
    }
}
