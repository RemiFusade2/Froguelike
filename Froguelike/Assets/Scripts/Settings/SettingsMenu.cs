using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering.Universal;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Audio;

public class SettingsMenu : MonoBehaviour
{
    [Header("Resolution")]
    public Toggle fullscreenToggle;
    public ResolutionsScrollRect resolutionScrollRect;
    public GameObject leftArrow;
    public GameObject rightArrow;
    public PixelPerfectCamera pixelPerfectCamera;

    private Vector2Int biggestResolutionForThisScreen;
    private Vector2 startResolution;
    private List<Vector2Int> allowedResolutions;

    int gameWidth;
    int gameHeight;

    int currentResolutionIndex = -1;

    bool startUpDone = false;
    bool isChangingFullscreen = false;

    CanvasScaler canvasScaler;

    public TextMeshProUGUI text;

    #region Sound

    [Header("Sound")]
    public AudioMixer audioMixer;
    public SoundManager soundManager;
    public Toggle SFXToggle;
    public Slider SFXSlider;
    public Toggle musicToggle;
    public Slider musicSlider;

    private float previousSFXVolume;
    private float previousMusicVolume;

    #endregion Sound

    // Start is called before the first frame update
    void Start()
    {
        canvasScaler = gameObject.GetComponent<CanvasScaler>();

        // Set the fullsccreen toggle to match the current fullscreen mode.
        fullscreenToggle.isOn = Screen.fullScreen;

        // Resolution can't be changed if in full screen.
        // leftArrow.GetComponent<Button>().interactable = !Screen.fullScreen;
        // rightArrow.GetComponent<Button>().interactable = !Screen.fullScreen;

        // Get the intended resolution of the game.
        gameWidth = pixelPerfectCamera.refResolutionX;
        gameHeight = pixelPerfectCamera.refResolutionY;

        Screen.fullScreenMode = FullScreenMode.MaximizedWindow;

        FindAllowedResolutions();

        startUpDone = true;
    }

    private void FindAllowedResolutions()
    {
        allowedResolutions = new List<Vector2Int>();

        biggestResolutionForThisScreen = new Vector2Int(Screen.mainWindowDisplayInfo.workArea.width, Screen.mainWindowDisplayInfo.workArea.height);

        // Find the biggest possible scale.
        int maxWidthScale = 0;
        int maxHeightScale = 0;

        while (biggestResolutionForThisScreen.x - gameWidth * (maxWidthScale + 1) >= 0) maxWidthScale++;
        while (biggestResolutionForThisScreen.y - gameHeight * (maxHeightScale + 1) >= 0) maxHeightScale++;

        int maxGameScale = Mathf.Min(maxWidthScale, maxHeightScale);

        List<string> allowedResolutionsAsStrings = new List<string>();

        for (int scale = 1; scale <= maxGameScale + 1; scale++)
        {
            // Add this resolution to the options.
            Vector2Int thisResolution = new Vector2Int(gameWidth * scale, gameHeight * scale);

            allowedResolutions.Add(thisResolution);
            allowedResolutionsAsStrings.Add(thisResolution.x + "x" + thisResolution.y);

            // If this is the current resolution, set the current resolution index.
            if (thisResolution.x == Screen.width && thisResolution.y == Screen.height)
            {
                currentResolutionIndex = allowedResolutions.Count - 1;
            }
        }

        // If no resolution matched, make sure there still is a resolution index.
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

        // Add the fullscreen resolution, since it might be different from the biggest resolution that fits.
        allowedResolutions.Add(biggestResolutionForThisScreen);

        UpdateResolutionScrollView(allowedResolutionsAsStrings);
    }

    private void UpdateResolutionScrollView(List<string> options)
    {
        // Remove all old resolutions.
        for (int child = 0; child < resolutionScrollRect.content.childCount; child++)
        {
            resolutionScrollRect.content.GetChild(child).gameObject.SetActive(false);
            Destroy(resolutionScrollRect.content.GetChild(child).gameObject);
        }

        resolutionScrollRect.Initialize(options, currentResolutionIndex);
    }

    // Update is called once per frame
    void Update()
    {
        // For debugging resolution settings
         text.SetText("Screen. : " + Screen.width + "x" + Screen.height + ", " + Screen.fullScreenMode + ", display: " + Display.main.renderingWidth + "x" + Display.main.renderingHeight + ", safe area: " + Screen.safeArea.width + "x" + Screen.safeArea.height + ", current res: " + Screen.currentResolution + ", good?: " + Screen.mainWindowDisplayInfo.width + "x" + Screen.mainWindowDisplayInfo.height + ", " + Screen.mainWindowDisplayInfo.name);
        text.SetText("workArea.max.x: " + Screen.mainWindowDisplayInfo.workArea.max.x + "x" + Screen.mainWindowDisplayInfo.workArea.max.y +
                    ", workArea.size.x: " + Screen.mainWindowDisplayInfo.workArea.size.x + "x" + Screen.mainWindowDisplayInfo.workArea.size.y +
                    ", workArea.xMax: " + Screen.mainWindowDisplayInfo.workArea.xMax + "x" + Screen.mainWindowDisplayInfo.workArea.yMax +
                    ", workArea.width: " + Screen.mainWindowDisplayInfo.workArea.width + "x" + Screen.mainWindowDisplayInfo.workArea.height +
                    ", .width: " + Screen.mainWindowDisplayInfo.width + "x" + Screen.mainWindowDisplayInfo.height +
                    ", workArea.min.x: " + Screen.mainWindowDisplayInfo.workArea.min.x + "x" + Screen.mainWindowDisplayInfo.workArea.min.y +
                    ", workArea.x: " + Screen.mainWindowDisplayInfo.workArea.x + "x" + Screen.mainWindowDisplayInfo.workArea.y +
                    ", workArea.xMin: " + Screen.mainWindowDisplayInfo.workArea.xMin + "x" + Screen.mainWindowDisplayInfo.workArea.yMin +
                    ", " + Screen.fullScreenMode);

        // Detect if the biggest available resolution changed and if so set new resolution options.
        if (biggestResolutionForThisScreen.x != Screen.resolutions[Screen.resolutions.Length - 1].width || biggestResolutionForThisScreen.y != Screen.resolutions[Screen.resolutions.Length - 1].height)
        {
            // Debug.Log("Redo resolution options");
            // FindAllowedResolutions();
        }

        if (biggestResolutionForThisScreen.x != Screen.safeArea.width || biggestResolutionForThisScreen.y != Screen.safeArea.height)
        {
            // Debug.Log("Redo resolution options");
             // FindAllowedResolutions();
        }

       // if (biggestResolutionForThisScreen != Screen.mainWindowDisplayInfo.)

        if (Screen.fullScreen && !fullscreenToggle.isOn)
        {
            if (!isChangingFullscreen)
            {
                fullscreenToggle.isOn = true;
            }
        }
        else if (!Screen.fullScreen && fullscreenToggle.isOn)
        {
            if (!isChangingFullscreen)
            {
                fullscreenToggle.isOn = false;
            }
        }

        // Resizes the canvas to match the pixel ratio.
        if (canvasScaler.scaleFactor != pixelPerfectCamera.pixelRatio)
        {
            ResizeCanvas();
            resolutionScrollRect.UpdateScroll(false, pixelPerfectCamera.pixelRatio);
        }
    }

    private void LateUpdate()
    {
        if (isChangingFullscreen)
        {
            isChangingFullscreen = false;
        }
    }

    public void SetFullscreen(bool wantFullscreen)
    {
        isChangingFullscreen = true;

        if (wantFullscreen)
        {
            //SetWindowResolution(allowedResolutions.Count - 1);
            currentResolutionIndex = allowedResolutions.Count - 1;
           // Screen.fullScreenMode = FullScreenMode.MaximizedWindow;
            Screen.fullScreen = true;
        }
        else if (startUpDone)
        {
            // would be nice to use the biggest one that doesnt fill the whole sreen, now I'm using the second biggest one.

            if (allowedResolutions[allowedResolutions.Count - 1] == allowedResolutions[allowedResolutions.Count - 2])
            {
                // SetWindowResolution(Mathf.Max(allowedResolutions.Count - 3, 0));

                Vector2Int res = allowedResolutions[Mathf.Max(allowedResolutions.Count - 3, 0)];
                currentResolutionIndex = Mathf.Max(allowedResolutions.Count - 3, 0);
                Screen.SetResolution(res.x, res.y, false);

            }
            else
            {
                // SetWindowResolution(Mathf.Max(allowedResolutions.Count - 2, 0));

                Vector2Int res = allowedResolutions[Mathf.Max(allowedResolutions.Count - 2, 0)];
                currentResolutionIndex = Mathf.Max(allowedResolutions.Count - 2, 0);
                Screen.SetResolution(res.x, res.y, false);
            }
        }

        // Screen.fullScreen = wantFullscreen;

        // leftArrow.GetComponent<Button>().interactable = !wantFullscreen;
        // rightArrow.GetComponent<Button>().interactable = !wantFullscreen;
        resolutionScrollRect.UpdateScroll(false, currentResolutionIndex);

        fullscreenToggle.isOn = wantFullscreen;
    }

    public void SetWindowResolution(int wantedResolutionIndex)
    {
        if (startUpDone)
        {
            currentResolutionIndex = wantedResolutionIndex;
            Vector2Int res = allowedResolutions[Mathf.Max(currentResolutionIndex, 0)];
            if (Screen.fullScreen)
            {
                // Screen.fullScreenMode = FullScreenMode.MaximizedWindow;
                Screen.SetResolution(res.x, res.y, FullScreenMode.MaximizedWindow);
            }
            else
            {
                Screen.SetResolution(res.x, res.y, false);
            }
        }
    }

    public void SetWindowResolution()
    {
        if (startUpDone)
        {
            currentResolutionIndex = resolutionScrollRect.currentDisplayedResolution - 1;
            Vector2Int res = allowedResolutions[Mathf.Max(currentResolutionIndex, 0)];
            if (Screen.fullScreen)
            {
                // Screen.fullScreenMode = FullScreenMode.MaximizedWindow;
                Screen.SetResolution(res.x, res.y, FullScreenMode.MaximizedWindow);
                //Debug.Log(allowedResolutions[currentResolutionIndex]);
                //Screen.SetResolution(allowedResolutions[currentResolutionIndex].x, allowedResolutions[currentResolutionIndex].y, false);
                //Screen.fullScreen = true;
            }
            else
            {
                Screen.SetResolution(res.x, res.y, false);
            }
        }
    }

    private void ResizeCanvas()
    {
        canvasScaler.scaleFactor = pixelPerfectCamera.pixelRatio;
    }


    #region Sound
    // Sound.

    public void OnVolumeValueChange(Slider slider)
    {
        /*
        float percentage = (slider.value / slider.maxValue) * 2; // Does this give the right kind of number? Should it be * 100?, give number between 0 and 2 now

        soundManager.SetVolumeModifier(percentage);
        */

    }

    // Turns sound on with true, turns sound of with false.
    public void SFXOn(bool on)
    {
        if (on)
        {
            SetSFXVolume(previousSFXVolume);
            SFXSlider.SetValueWithoutNotify(previousSFXVolume);
        }
        else if (!on)
        {
            if (SFXSlider.value == SFXSlider.minValue)
            {
                previousSFXVolume = 0;
            }
            else
            {
                previousSFXVolume = SFXSlider.value;
                SFXSlider.SetValueWithoutNotify(SFXSlider.minValue);
            }

            Mute("volumeSFX");
        }
    }

    public void MusicOn(bool on)
    {
        if (on)
        {
            SetMusicVolume(previousMusicVolume);
            musicSlider.SetValueWithoutNotify(previousMusicVolume);
        }
        else if (!on)
        {
            if (musicSlider.value == musicSlider.minValue)
            {
                previousMusicVolume = 0;
            }
            else
            {
                previousMusicVolume = musicSlider.value;
                musicSlider.SetValueWithoutNotify(musicSlider.minValue);
            }

            Mute("volumeMusic");
        }
    }

    private void Mute(string audioGroupVolumeParameter)
    {
        audioMixer.SetFloat(audioGroupVolumeParameter, -80f);
    }

    public void SetSFXVolume(float volume)
    {
        float newVolume = volume;

        if (newVolume < 0)
        {
            newVolume *= 3;
        }

        if (newVolume == -30)
        {
            if (SFXToggle.isOn)
            {
                SFXToggle.SetIsOnWithoutNotify(false);
                SFXOn(false);
            }
        }
        else if (newVolume > -30)
        {
            if (!SFXToggle.isOn)
            {
                SFXToggle.SetIsOnWithoutNotify(true);
            }

            audioMixer.SetFloat("volumeSFX", newVolume);
        }
    }

    public void SetMusicVolume(float volume)
    {
        float newVolume = volume;

        if (newVolume < 0)
        {
            newVolume *= 3;
        }

        if (newVolume == -30)
        {
            if (musicToggle.isOn)
            {
                musicToggle.SetIsOnWithoutNotify(false);
                MusicOn(false);
            }
        }
        else if (newVolume > -30)
        {
            if (!musicToggle.isOn)
            {
                musicToggle.SetIsOnWithoutNotify(true);
            }

            audioMixer.SetFloat("volumeMusic", newVolume);
        }
    }

    private float ModifyVolume(float volume)
    {
        float newVolume = volume * 1;
        return newVolume;
    }

    #endregion Sound
}
