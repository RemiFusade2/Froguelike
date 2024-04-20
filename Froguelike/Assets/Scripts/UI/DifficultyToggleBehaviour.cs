using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DifficultyToggleBehaviour : MonoBehaviour
{
    [Header("References")]
    public Toggle toggle;
    public Animator toggleAnimator;
    public GameObject staticCheckmark;

    public void SetCheckmarkBehaviourOnSelect()
    {
        staticCheckmark.SetActive(false);
        toggleAnimator.SetBool("Reverse", toggle.isOn);
    }

    public void SetCheckmarkBehaviourOnValueChange()
    {
        staticCheckmark.SetActive(false);
        toggleAnimator.SetBool("Reverse", toggle.isOn);
    }

    public void SetStaticCheckmarkVisibilityOnDeselect()
    {
        staticCheckmark.SetActive(toggle.isOn);
    }
}
