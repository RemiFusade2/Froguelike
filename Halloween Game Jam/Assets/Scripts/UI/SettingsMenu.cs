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
    public Camera pixelPerfectCamera;
    private Resolution[] resolutions;
    private List<int[]> allowedResolutions;


    // Start is called before the first frame update
    void Start()
    {
        // Set the fullsccreen toggle to match the fullscreen mode.
        fullscreenToggle.isOn = Screen.fullScreen;

        // set resolution

        resolutions = Screen.resolutions;
        resolutionDropdown.ClearOptions();

        int nativeWidth = pixelPerfectCamera.GetComponent<PixelPerfectCamera>().refResolutionX;
        int nativeHeight = pixelPerfectCamera.GetComponent<PixelPerfectCamera>().refResolutionY;

        List<string> options = new List<string>();
        allowedResolutions = new List<int[]>();
        allowedResolutions.Add(new int[] { 640, 360 });
        options.Add(allowedResolutions[0][0] + "x" + allowedResolutions[0][1]);
        /*
        allowedResolutions.Add(new int[] { 640 * 2, 360 * 2 });
        allowedResolutions.Add(new int[] { 640 * 3, 360 * 3 });
        */
        Resolution currentResolution = Screen.currentResolution;
        int currentResolutionIndex = 0;


        for (int i = 1; i < resolutions.Length; i++)
        {
            Resolution resolution = resolutions[i];
            if ((resolution.width % nativeWidth) == 0 && (resolution.height % nativeHeight) == 0)
            {
                string option = resolutions[i].width + "x" + resolutions[i].height;
                options.Add(option);
                int[] thisOption = new int[] { resolutions[i].width, resolutions[i].height };
                allowedResolutions.Add(thisOption);


                if (currentResolution.width == resolution.width && currentResolution.height == resolution.height)
                {
                    currentResolutionIndex = options.Count - 1;
                }

            }
        }

        resolutionDropdown.AddOptions(options);
        resolutionDropdown.value = currentResolutionIndex;
        resolutionDropdown.RefreshShownValue();
    }

    // Update is called once per frame
    void Update()
    {
        if (Screen.fullScreen && resolutionDropdown.interactable)
        {
            resolutionDropdown.interactable = false;
        }
        else if (!Screen.fullScreen && !resolutionDropdown.interactable)
        {
            resolutionDropdown.interactable = true;
        }
    }

    public void SetFullscreen(bool isFullscreen)
    {
        if (isFullscreen)
        {
            SetResolution(allowedResolutions.Count - 1);
        }
        Screen.fullScreen = isFullscreen;

    }

    public void SetResolution(int wantedResolutionIndex)
    {
        int[] wantedResolution = allowedResolutions[wantedResolutionIndex];
        Screen.SetResolution(wantedResolution[0], wantedResolution[1], Screen.fullScreen);
    }
}
