using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SettingsSliderBehaviour : MonoBehaviour
{
    public Slider slider;

    private bool wantSliderControll = false;

    private Navigation oldNav;
    private Navigation newNav = new Navigation();
    private Animator animator;

    // Start is called before the first frame update
    void Start()
    {
        animator = GetComponent<Animator>();

        newNav.selectOnDown = null;
        newNav.selectOnLeft = null;
        newNav.selectOnRight = null;
        newNav.selectOnUp = null;
    }

    public void OnSubmit(bool forceSliderControllOff)
    {
        if (forceSliderControllOff)
        {
            wantSliderControll = false;
        }
        else
        {
            wantSliderControll = !wantSliderControll;
        }

        // change navigation?
        if (wantSliderControll)
        {
            animator.SetBool("Controlled", true);
            oldNav = slider.navigation;

            slider.navigation = newNav;
        }
        else
        {
            animator.SetBool("Controlled", false);
            if (oldNav.selectOnDown != null)
            {
                slider.navigation = oldNav;
            }
        }
    }
}
