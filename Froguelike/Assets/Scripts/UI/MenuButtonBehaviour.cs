using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MenuButtonBehaviour : MonoBehaviour, ISelectHandler, IDeselectHandler
{
    public GameObject target;
    public MenuTongueTargetingBehaviour menuTongueTargetingBehaviour;

    public void OnDeselect(BaseEventData eventData)
    {
        menuTongueTargetingBehaviour.ReturnFromButton(target);
    }

    public void OnSelect(BaseEventData eventData)
    {
        menuTongueTargetingBehaviour.GoToButton(target, false);
    }
}
