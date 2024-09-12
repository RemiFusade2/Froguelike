using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MusicChoiceButtonBehaviour : Button
{
    public Button yesButton;
    public Button noButton;

    public override void OnPointerEnter(PointerEventData eventData)
    {
        base.OnPointerEnter(eventData);
        if (interactable)
        {
            Select();
        }
    }

    public override void OnDeselect(BaseEventData eventData)
    {
        base.OnDeselect(eventData);
        if (interactable)
        {
            GetComponent<Animator>().SetTrigger("Normal");
        }
    }

    public void SetBoolInteractable()
    {
        GetComponent<Animator>().SetBool("Interactable", interactable);
    }
}
