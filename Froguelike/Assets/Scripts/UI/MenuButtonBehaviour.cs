using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MenuButtonBehaviour : MonoBehaviour, ISelectHandler
{
    public GameObject target;
    public MenuTongueTargetingBehaviour menuTongueTargetingBehaviour;

    private bool isSelected = false;

    public void OnSelect(BaseEventData eventData)
    {
        menuTongueTargetingBehaviour.Attack(target, false);
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
