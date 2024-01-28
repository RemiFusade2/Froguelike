using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering.Universal;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Audio;
using FMODUnity;
using FMOD.Studio;

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
    DisplayInfo savedDisplayInfo;

    #region Sound

    [Header("Sound")]
    public SoundManager soundManager;
    public Toggle SFXToggle;
    public Slider SFXSlider;
    public Toggle musicToggle;
    public Slider musicSlider;

    private float previousSFXVolume;
    private float previousMusicVolume;

    private bool savedSFXOn;
    private string savedSFXOnKey = "Froguelike SFX on";
    private float savedSFXVolume;
    private string savedSFXVolumeKey = "Froguelike SFX volume";
    private bool savedMusicOn;
    private string savedMusicOnKey = "Froguelike music on";
    private float savedMusicVolume;
    private string savedMusicVolumeKey = "Froguelike music volume";

    #endregion Sound

    #region Disclaimer screens

    private string savedShowDemoDisclaimerKey = "Froguelike Demo Disclaimer on";
    private string savedShowEADisclaimerKey = "Froguelike EA Disclaimer on";

    #endregion

    // Start is called before the first frame update
    void Start()
    {
        canvasScaler = gameObject.GetComponent<CanvasScaler>();

        // Set the fullsccreen toggle to match the current fullscreen mode.
        fullscreenToggle.isOn = Screen.fullScreen;

        // Get the intended resolution of the game.
        gameWidth = pixelPerfectCamera.refResolutionX;
        gameHeight = pixelPerfectCamera.refResolutionY;

        FindAllowedResolutions();

        startUpDone = true;

        LoadAudioSettings();
    }

    // Update is called once per frame
    void Update()
    {
        // For debugging resolution settings
        // text.SetText("Windows size: " + Screen.width + "x" + Screen.height + ", " + Screen.fullScreenMode + ", display: " + savedDisplayInfo.width + "x" + savedDisplayInfo.height);
        // text.SetText(currentResolutionIndex.ToString());

        // Detect if the screens resolution changed and if so set new resolution options.
        if (savedDisplayInfo.width != Screen.mainWindowDisplayInfo.width || savedDisplayInfo.height != Screen.mainWindowDisplayInfo.height)
        {
            FindAllowedResolutions();
        }

        // Make sure the toggle is displaying correctly.
        if ((Screen.fullScreen && !fullscreenToggle.isOn) || (!Screen.fullScreen && fullscreenToggle.isOn))
        {
            if (!isChangingFullscreen)
            {
                fullscreenToggle.SetIsOnWithoutNotify(Screen.fullScreen);
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

    public void LoadAudioSettings()
    {
        // SFX.
        savedSFXOn = PlayerPrefs.GetInt(savedSFXOnKey, 1) == 1;
        if (savedSFXOn)
        {
            savedSFXVolume = PlayerPrefs.GetFloat(savedSFXVolumeKey, 15);
            SetSFXVolume(savedSFXVolume);
            SFXSlider.SetValueWithoutNotify(savedSFXVolume);
        }
        else
        {
            previousSFXVolume = PlayerPrefs.GetFloat(savedSFXVolumeKey, 15);
            SFXSlider.SetValueWithoutNotify(previousSFXVolume);
        }
        SFXToggle.isOn = savedSFXOn;

        // Music.
        savedMusicOn = PlayerPrefs.GetInt(savedMusicOnKey, 1) == 1;
        if (savedMusicOn)
        {
            savedMusicVolume = PlayerPrefs.GetFloat(savedMusicVolumeKey);
            SetMusicVolume(savedMusicVolume);
            musicSlider.SetValueWithoutNotify(savedMusicVolume);
        }
        else
        {
            previousMusicVolume = PlayerPrefs.GetFloat(savedMusicVolumeKey);
            musicSlider.SetValueWithoutNotify(previousMusicVolume);
        }
        musicToggle.isOn = savedMusicOn;
    }

    #region Resolution

    private void FindAllowedResolutions()
    {
        allowedResolutions = new List<Vector2Int>();
        // Save the mainwindow displayinfo to use to detect if the player changed mscreen for the window, and if so: update the available options. (run this method again)
        savedDisplayInfo = Screen.mainWindowDisplayInfo;
        biggestResolutionForThisScreen = new Vector2Int(savedDisplayInfo.width, savedDisplayInfo.height);

        // Find the biggest possible scale.
        int maxWidthScale = 0;
        int maxHeightScale = 0;

        while (biggestResolutionForThisScreen.x - gameWidth * (maxWidthScale + 1) >= 0) maxWidthScale++;
        while (biggestResolutionForThisScreen.y - gameHeight * (maxHeightScale + 1) >= 0) maxHeightScale++;

        int maxGameScale = Mathf.Min(maxWidthScale, maxHeightScale);

        List<string> allowedResolutionsAsStrings = new List<string>();

        for (int scale = 1; scale <= maxGameScale; scale++)
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
        // allowedResolutions.Add(biggestResolutionForThisScreen);

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

    public void SetFullscreen(bool wantFullscreen)
    {
        isChangingFullscreen = true;

        if (wantFullscreen)
        {
            currentResolutionIndex = Mathf.Max(allowedResolutions.Count - 1, 0);
            Vector2Int res = allowedResolutions[currentResolutionIndex];
            Screen.SetResolution(res.x, res.y, FullScreenMode.MaximizedWindow);
        }
        else if (startUpDone)
        {
            Vector2Int res = allowedResolutions[currentResolutionIndex];
            Screen.SetResolution(res.x, res.y, false);
        }

        resolutionScrollRect.UpdateScroll(false, currentResolutionIndex + 1);
    }

    public void SetWindowResolution(int wantedResolutionIndex)
    {
        if (startUpDone)
        {
            currentResolutionIndex = wantedResolutionIndex;
            Vector2Int res = allowedResolutions[Mathf.Max(currentResolutionIndex, 0)];
            if (Screen.fullScreen)
            {
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
                Screen.SetResolution(res.x, res.y, FullScreenMode.MaximizedWindow);
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

    #endregion Resolution

    #region Sound
    // Sound.

    // Turns SFX on with true, turns sound of with false.
    public void SFXOn(bool on)
    {
        PlayerPrefs.SetInt(savedSFXOnKey, on ? 1 : 0);

        if (on)
        {
            SetSFXVolume(previousSFXVolume);
            SFXSlider.SetValueWithoutNotify(previousSFXVolume);
        }
        else
        {
            if (SFXSlider.value == SFXSlider.minValue)
            {
                previousSFXVolume = SFXSlider.maxValue / 2;
            }
            else
            {
                previousSFXVolume = SFXSlider.value;
                SFXSlider.SetValueWithoutNotify(SFXSlider.minValue);
            }

            PlayerPrefs.SetFloat(savedSFXVolumeKey, previousSFXVolume);
        }

        SoundManager.instance.MuteSFXBus(!on);
    }

    // Turns music on with true, turns sound of with false.
    public void MusicOn(bool on)
    {
        PlayerPrefs.SetInt(savedMusicOnKey, on ? 1 : 0);

        if (on)
        {
            SetMusicVolume(previousMusicVolume);
            musicSlider.SetValueWithoutNotify(previousMusicVolume);
        }
        else
        {
            if (musicSlider.value == musicSlider.minValue)
            {
                previousMusicVolume = musicSlider.maxValue / 2;
            }
            else
            {
                previousMusicVolume = musicSlider.value;
                musicSlider.SetValueWithoutNotify(musicSlider.minValue);
            }

            PlayerPrefs.SetFloat(savedMusicVolumeKey, previousMusicVolume);
        }

        SoundManager.instance.MuteMusicBus(!on);
    }

    // Sets volume and updates the check box if necessary.
    public void SetSFXVolume(float volume)
    {
        PlayerPrefs.SetFloat(savedSFXVolumeKey, volume);

        float newVolume = volume / SFXSlider.maxValue * 2;

        if (newVolume == SFXSlider.minValue)
        {
            if (SFXToggle.isOn)
            {
                SFXToggle.SetIsOnWithoutNotify(false);
                SFXOn(false);
            }
        }
        else if (newVolume > 0)
        {
            if (!SFXToggle.isOn)
            {
                SFXToggle.SetIsOnWithoutNotify(true);
                PlayerPrefs.SetInt(savedSFXOnKey, 1);
                SoundManager.instance.MuteSFXBus(false);
            }
        }

        SoundManager.instance.SetNewSFXVolume(newVolume);
    }

    // Sets volume and updates the check box if necessary.
    public void SetMusicVolume(float volume)
    {
        PlayerPrefs.SetFloat(savedMusicVolumeKey, volume);

        float newVolume = volume / musicSlider.maxValue * 2;

        if (newVolume == musicSlider.minValue)
        {
            if (musicToggle.isOn)
            {
                musicToggle.SetIsOnWithoutNotify(false);
                MusicOn(false);
            }
        }
        else if (newVolume > 0)
        {
            if (!musicToggle.isOn)
            {
                musicToggle.SetIsOnWithoutNotify(true);
                PlayerPrefs.SetInt(savedMusicOnKey, 1);
                SoundManager.instance.MuteMusicBus(false);
            }
        }

        SoundManager.instance.SetNewMusicVolume(newVolume);
    }

    #endregion Sound

    #region Disclaimer screens

    public bool IsDemoDisclaimerOn()
    {
        return PlayerPrefs.GetInt(savedShowDemoDisclaimerKey, 1) == 1;
    }

    public void SetDemoDisclaimerOn(bool on)
    {
        PlayerPrefs.SetInt(savedShowDemoDisclaimerKey, on ? 1 : 0);
    }

    public bool IsEADisclaimerOn()
    {
        return PlayerPrefs.GetInt(savedShowEADisclaimerKey, 1) == 1;
    }

    public void SetEADisclaimerOn(bool on)
    {
        PlayerPrefs.SetInt(savedShowEADisclaimerKey, on ? 1 : 0);
    }

    #endregion
}
