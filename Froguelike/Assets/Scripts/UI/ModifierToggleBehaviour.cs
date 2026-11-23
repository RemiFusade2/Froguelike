using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class ModifierToggleBehaviour : MonoBehaviour, ISelectHandler
{
    [Header("References")]
    public Toggle toggle;
    public GameObject staticCheckmark;
    public GameObject modifierIcon;
    public GameObject questionmarkIcon;
    public GameObject modifierNameText;
    public GameObject unknownText;

    public void OnSelect(BaseEventData eventData)
    {
        if (toggle.interactable)
        {
            toggle.Select();
        }
    }

    public void SetInteractability(bool interactable)
    {
        toggle.interactable = interactable;
        modifierIcon.SetActive(interactable);
        questionmarkIcon.SetActive(!interactable);
        modifierNameText.SetActive(interactable);
        unknownText.SetActive(!interactable);
    }

    public void SetCheckmark()
    {
        staticCheckmark.SetActive(toggle.isOn);
    }

    public void ResetToggle()
    {
        toggle.isOn = false;
        SetCheckmark();
    }
}
