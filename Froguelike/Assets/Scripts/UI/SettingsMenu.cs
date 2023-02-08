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
    // private Vector2 currentResolution;

    int gameWidth;
    int gameHeight;

    int currentResolutionIndex;
    int gameScaler;

    bool startUpDone = false;
    bool isUpdatingDropdownValue = false; // ?? TODO

    CanvasScaler canvasScaler;

    // Start is called before the first frame update
    void Start()
    {
        canvasScaler = gameObject.GetComponent<CanvasScaler>();

        // Set the fullsccreen toggle to match the current fullscreen mode.
        fullscreenToggle.isOn = Screen.fullScreen;
        resolutionDropdown.interactable = !Screen.fullScreen;

        // Get the intended reolution of the game.
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

        for (int scale = 1; scale <= maxGameScale; scale++)
        {
            // Add this resolution.
            Vector2 thisResolution = new Vector2(gameWidth * scale, gameHeight * scale);
            options.Add(thisResolution.x + "x" + thisResolution.y);
            allowedResolutions.Add(thisResolution);

            // If this is the current resolution, set the current resolution index.
            if (thisResolution.x == Screen.width && thisResolution.y == Screen.height)
            {
                currentResolutionIndex = allowedResolutions.Count - 1;
            }
        }

        // If no reolution matched, make sure there still is a resolution index.
        if (currentResolutionIndex < 0)
        {
            // If the game is in fullscreen pick the biggest one, otherwise pick the second biggest one.
            if (Screen.fullScreen)
            {
                currentResolutionIndex = Mathf.Max(allowedResolutions.Count - 1, 0);
            }
            else
            {
                currentResolutionIndex = Mathf.Max(allowedResolutions.Count - 2, 0);
            }
        }

        UpdateDropdown(options);
    }


    // Update the resolution dropdown and set the marker to the current reolution (without setting a resolution).
    private void UpdateDropdown(List<string> options)
    {
        isUpdatingDropdownValue = true;

        resolutionDropdown.ClearOptions();
        resolutionDropdown.AddOptions(options);
        SetDropdownValue(currentResolutionIndex);
    }

    private void SetDropdownValue(int value)
    {
        isUpdatingDropdownValue = true;

        resolutionDropdown.SetValueWithoutNotify(value);

        isUpdatingDropdownValue = false;
    }

    // Update is called once per frame
    void Update()
    {
        /*
        // Check if resolution changed
        if (allowedResolutions[currentResolutionIndex].x != Screen.width && allowedResolutions[currentResolutionIndex].y != Screen.height)
        {
            currentResolutionIndex = allowedResolutions.IndexOf(new Vector2(Screen.width, Screen.height));
            SetDropdownValue(currentResolutionIndex);
            SetResolution(currentResolutionIndex);
        }
        */

        if (resolutionDropdown.value != currentResolutionIndex)
        {

        }

        if (canvasScaler.scaleFactor != pixelPerfectCamera.pixelRatio * 2)
        {
            ResizeCanvas();
            SetDropdownValue(pixelPerfectCamera.pixelRatio - 1);
        }
    }

    public void SetFullscreen(bool wantFullscreen)
    {
        if (wantFullscreen)
        {
            SetResolution(Mathf.Max(allowedResolutions.Count - 1, 0));
        }
        else if (startUpDone)
        {
            SetResolution(Mathf.Max(allowedResolutions.Count - 2, 0));
        }

        Screen.fullScreen = wantFullscreen;
        resolutionDropdown.interactable = !wantFullscreen;
        SetDropdownValue(currentResolutionIndex);
    }

    public void SetResolution(int wantedResolutionIndex)
    {
        if (startUpDone && !isUpdatingDropdownValue)
        {
            currentResolutionIndex = wantedResolutionIndex;
            Screen.SetResolution(Mathf.RoundToInt(allowedResolutions[currentResolutionIndex].x), Mathf.RoundToInt(allowedResolutions[currentResolutionIndex].y), Screen.fullScreen);
            SetDropdownValue(currentResolutionIndex);
        }
    }

    private void ResizeCanvas()
    {
        canvasScaler.scaleFactor = pixelPerfectCamera.pixelRatio * 2; // later when UI is redrawn the * 2 is not needed TODO
    }
}
