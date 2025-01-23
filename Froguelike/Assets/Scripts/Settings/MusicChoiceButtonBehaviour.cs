using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MusicChoiceButtonBehaviour : Button
{
    public Button yesButton;
    public Button noButton;

    private bool transitioning = false;

    public override void OnPointerEnter(PointerEventData eventData)
    {
        if (eventData.delta.x != 0 || eventData.delta.y != 0)
        {
            base.OnPointerEnter(eventData);
        }

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

    public override void Select()
    {
        if (!transitioning)
        {
            base.Select();
            transitioning = true;
        }
        else
        {
            transitioning = false;
        }
    }
}
