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

    int gameWidth;
    int gameHeight;

    int currentResolutionIndex;

    bool startUpDone = false;
    bool isUpdatingDropdown = false; // ?? TODO

    // Start is called before the first frame update
    void Start()
    {
        // Set the fullsccreen toggle to match the fullscreen mode.
        fullscreenToggle.isOn = Screen.fullScreen;

        gameWidth = pixelPerfectCamera.refResolutionX;
        gameHeight = pixelPerfectCamera.refResolutionY;

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

        // Used to pick the dropdowns marker to match the current reolution.
        currentResolutionIndex = 0;

        for (int scale = 1; scale <= maxGameScale; scale++)
        {
            // Add this resolution.
            Vector2 thisResolution = new Vector2(gameWidth * scale, gameHeight * scale);
            options.Add(thisResolution.x + "x" + thisResolution.y);
            allowedResolutions.Add(thisResolution);

            // If this resolution matches the current one, update the current resolution index.
            if (thisResolution.x == Screen.currentResolution.width && thisResolution.y == Screen.currentResolution.height)
            {
                currentResolutionIndex = options.Count - 1;
                // TODO probably need some kind of fallback for if no resolution matched?
            }
        }

        UpdateDropdown(options);
    }


    // Update the resolution dropdown and set the marker to the current reolution (without setting a resolution).
    private void UpdateDropdown(List<string> options)
    {
        resolutionDropdown.ClearOptions();
        resolutionDropdown.AddOptions(options);
        resolutionDropdown.SetValueWithoutNotify(currentResolutionIndex);
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
        if (allowedResolutions[currentResolutionIndex].x != Screen.currentResolution.width && allowedResolutions[currentResolutionIndex].y != Screen.currentResolution.height)
        {
            currentResolutionIndex = allowedResolutions.IndexOf(new Vector2(Screen.currentResolution.width, Screen.currentResolution.height));
            SetDropdownValue(currentResolutionIndex);
            SetResolution(currentResolutionIndex);
        }

        if (currentResolutionIndex < 0)
        {
            SetFullscreen(false);
        }
    }

    public void SetFullscreen(bool isFullscreen)
    {
        if (isFullscreen && startUpDone)
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
        if (startUpDone)
        {
            currentResolutionIndex = wantedResolutionIndex;
            Screen.SetResolution(Mathf.RoundToInt(allowedResolutions[currentResolutionIndex].x), Mathf.RoundToInt(allowedResolutions[currentResolutionIndex].y), Screen.fullScreen);
        }
    }
}
