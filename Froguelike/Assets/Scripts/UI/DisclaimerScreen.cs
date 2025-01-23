using Steamworks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DisclaimerScreen : MonoBehaviour
{
    [Header("References")]
    public SettingsManager settingsMenu;

    public Animator dontShowThisAgainButtonAnimator;
    public GameObject dontShowThisAgainCheckmark;

    public Button confirmButton;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    private bool ShouldDisclaimerBeShown()
    {
        bool showDisclaimer = false;
        if (settingsMenu != null && BuildManager.instance != null)
        {
            if (BuildManager.instance.demoBuild)
            {
                // Demo disclaimer
                showDisclaimer = settingsMenu.IsDemoDisclaimerOn();
            }
            else
            {
                // EA disclaimer
                showDisclaimer = settingsMenu.IsEADisclaimerOn();
            }
        }
        return showDisclaimer;
    }

    public bool TryShowDisclaimer()
    {
        bool showDisclaimer = ShouldDisclaimerBeShown();

        // If "ShowDisclaimer" is ON, it means the checkmark is not there.
        // Therefore, that button animation is not reversed.
        dontShowThisAgainButtonAnimator.SetBool("Reverse", !showDisclaimer);
        dontShowThisAgainCheckmark.SetActive(!showDisclaimer);

        this.gameObject.SetActive(showDisclaimer);

        if (showDisclaimer)
        {
            // Select Confirm button by default when disclaimer screen appears
            UIManager.instance.SetSelectedButton(confirmButton);
        }

        return showDisclaimer;
    }

    public void HideDisclaimer()
    {
        UIManager.instance.DisableDisclaimerScreen(this.gameObject);
    }

    public void ToggleShowDisclaimerAgain()
    {
        bool showDisclaimer = ShouldDisclaimerBeShown();
        if (settingsMenu != null && BuildManager.instance != null)
        {
            showDisclaimer = !showDisclaimer; // Toggle
            if (BuildManager.instance.demoBuild)
            {
                // Demo disclaimer
                settingsMenu.SetDemoDisclaimerOn(showDisclaimer);
            }
            else
            {
                // EA disclaimer
                settingsMenu.SetEADisclaimerOn(showDisclaimer);
            }
        }

        // If "ShowDisclaimer" is ON, it means the checkmark is not there.
        // Therefore, that button animation is not reversed.
        dontShowThisAgainButtonAnimator.SetBool("Reverse", !showDisclaimer);
        dontShowThisAgainCheckmark.SetActive(!showDisclaimer);

        // Select Confirm button
        if (gameObject.activeInHierarchy)
        {
            UIManager.instance.SetSelectedButton(confirmButton);
        }
    }

    public void TempHideCheckmark()
    {
        dontShowThisAgainCheckmark.SetActive(false);
    }
    public void ResetCheckmark()
    {
        dontShowThisAgainCheckmark.SetActive(!ShouldDisclaimerBeShown());
    }

    public void ClickOnDiscordButton()
    {
        UIManager.instance.OpenDiscordInvitation();
    }

    public void ClickOnSteamButton()
    {
        UIManager.instance.OpenSteamPage();
    }
}
