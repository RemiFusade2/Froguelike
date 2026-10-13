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

    private bool isSelected = false;

    public void OnDeselect(BaseEventData eventData)
    {
        menuTongueTargetingBehaviour.ReturnFromButton();
    }

    public void OnSelect(BaseEventData eventData)
    {
        menuTongueTargetingBehaviour.GoToButton(target, false);
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    
}
