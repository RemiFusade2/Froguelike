using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering.Universal;
using UnityEngine.UI;
using TMPro;

public class SettingsMenu : MonoBehaviour
{
    public Toggle fullscreenToggle;
    public TMP_Dropdown resolutionDropdown;
    public PixelPerfectCamera pixelPerfectCamera;


    private Resolution[] resolutions;
    private List<Vector2> allowedResolutions;
    private Vector2 currentResolution;

    int gameWidth;
    int gameHeight;

    int currentResolutionIndex;

    bool startUpDone = false;
    bool isUpdatingDropdown = false; // ?? TODO

    // Start is called before the first frame update
    void Start()
    {

        // Set the fullsccreen toggle to match the current fullscreen mode.
        fullscreenToggle.isOn = Screen.fullScreen;
        resolutionDropdown.interactable = !Screen.fullScreen;

        // Get the intended reolution of the game.
        gameWidth = pixelPerfectCamera.refResolutionX;
        gameHeight = pixelPerfectCamera.refResolutionY;

        // Save the resolution at start.
        currentResolution = new Vector2(Screen.currentResolution.width, Screen.currentResolution.height);

        FindAllowedResolutions();

        startUpDone = true;
    }

    private void FindAllowedResolutions()
    {
        allowedResolutions = new List<Vector2>();
        Resolution[] availableResolutions = Screen.resolutions;        // (Last one is biggest)

        Vector2 biggestScreenResolution = new Vector2(availableResolutions[availableResolutions.Length - 1].width, availableResolutions[availableResolutions.Length - 1].height);

        int maxWidthScale = 0;
        int maxHeigthScale = 0;

        while (biggestScreenResolution.x - gameWidth * maxWidthScale > 0) maxWidthScale++;
        while (biggestScreenResolution.y - gameHeight * maxHeigthScale > 0) maxHeigthScale++;

        int maxGameScale = Mathf.Min(maxWidthScale, maxHeigthScale);

        List<string> options = new List<string>();

        for (int scale = 1; scale <= maxGameScale; scale++)
        {
            // Add this resolution.
            Vector2 thisResolution = new Vector2(gameWidth * scale, gameHeight * scale);
            options.Add(thisResolution.x + "x" + thisResolution.y);
            allowedResolutions.Add(thisResolution);

            // If this resolution matches the current one, update the current resolution index.

            currentResolutionIndex = allowedResolutions.IndexOf(currentResolution);


            if (currentResolutionIndex < 0)
            {
                // TODO probably need some kind of fallback for if no resolution matched?
                if (Screen.fullScreen)
                {
                    currentResolutionIndex = allowedResolutions.Count - 1;
                }
                else
                {
                    currentResolutionIndex = Mathf.Max(allowedResolutions.Count - 2, 0);
                }

                currentResolution = allowedResolutions[currentResolutionIndex];
            }
        }


        UpdateDropdown(options);
    }


    // Update the resolution dropdown and set the marker to the current reolution (without setting a resolution).
    private void UpdateDropdown(List<string> options)
    {
        isUpdatingDropdown = true;

        resolutionDropdown.ClearOptions();
        resolutionDropdown.AddOptions(options);
        SetDropdownValue(currentResolutionIndex);

        isUpdatingDropdown = false;
    }

    private void SetDropdownValue(int value)
    {
        resolutionDropdown.SetValueWithoutNotify(value);
    }

    // Update is called once per frame
    void Update()
    {
        Debug.Log("Number of allowed reolutions: " + allowedResolutions.Count);
        Debug.Log("Current resolution index: " + currentResolutionIndex);
        if (allowedResolutions[currentResolutionIndex].x != currentResolution.x && allowedResolutions[currentResolutionIndex].y != currentResolution.y)
        {
            currentResolutionIndex = allowedResolutions.IndexOf(new Vector2(Screen.currentResolution.width, Screen.currentResolution.height));
            SetDropdownValue(currentResolutionIndex);
            //            SetResolution(currentResolutionIndex);
        }

        if (currentResolutionIndex < 0)
        {
            SetFullscreen(false);
        }
    }

    public void SetFullscreen(bool isFullscreen)
    {
        if (isFullscreen)
        {
            SetResolution(allowedResolutions.Count - 1);
        }
        else if (startUpDone)
        {
            // Should check biggest resolution that fits in window, that is not always rge second biggest one,
            // it could be the biggest allowed reolution in case the screen doesn't match a full integer multuiplier for scale.
            SetDropdownValue(currentResolutionIndex);
            SetResolution(allowedResolutions.Count - 2);
        }


        Screen.fullScreen = isFullscreen;
        resolutionDropdown.interactable = !isFullscreen;
        SetDropdownValue(currentResolutionIndex);

    }

    public void SetResolution(int wantedResolutionIndex)
    {
        if (startUpDone && !isUpdatingDropdown)
        {
            Debug.Log("Set resolution to index: " + wantedResolutionIndex);
            currentResolutionIndex = wantedResolutionIndex;
            currentResolution = allowedResolutions[currentResolutionIndex];
            Screen.SetResolution(Mathf.RoundToInt(allowedResolutions[currentResolutionIndex].x), Mathf.RoundToInt(allowedResolutions[currentResolutionIndex].y), Screen.fullScreen);
        }
    }
}
