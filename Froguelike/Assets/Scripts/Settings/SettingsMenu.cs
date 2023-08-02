using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering.Universal;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Audio;

public class SettingsMenu : MonoBehaviour
{
    public Toggle fullscreenToggle;
    public TMP_Dropdown resolutionDropdown;
    public ResolutionsScrollRect resolutionScrollRect;
    public GameObject leftArrow;
    public GameObject rightArrow;
    public PixelPerfectCamera pixelPerfectCamera;
    public AudioMixer audioMixer;

    private Vector2 biggestResolutionForThisScreen;
    private List<Vector2> allowedResolutions;
    // private Vector2 currentResolution;

    int gameWidth;
    int gameHeight;

    int currentResolutionIndex;
    int gameScaler;

    bool startUpDone = false;
    bool isUpdatingDropdownValue = false;
    bool isChangingFullscreen = false;

    CanvasScaler canvasScaler;

    public TextMeshProUGUI currentMaxRes;


    // Sound.
    public SoundManager soundManager;
    public Toggle SFXToggle;
    public Slider SFXSlider;
    public Toggle musicToggle;
    public Slider musicSlider;

    private float previousSFXVolume;
    private float previousMusicVolume;

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
        List<Resolution> availableResolutions = new List<Resolution>();        // (Last one is biggest)
        availableResolutions.AddRange(Screen.resolutions);

        biggestResolutionForThisScreen = new Vector2(availableResolutions[availableResolutions.Count - 1].width, availableResolutions[availableResolutions.Count - 1].height);

        int maxWidthScale = 0;
        int maxHeigthScale = 0;

        while (biggestResolutionForThisScreen.x - gameWidth * (maxWidthScale + 1) >= 0) maxWidthScale++;
        while (biggestResolutionForThisScreen.y - gameHeight * (maxHeigthScale + 1) >= 0) maxHeigthScale++;

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

        allowedResolutions.Add(biggestResolutionForThisScreen);

        // UpdateDropdown(options);
        UpdateResolutionScrollView(options);
    }


    // Update the resolution dropdown and set the marker to the current reolution (without setting a resolution).
    private void UpdateDropdown(List<string> options)
    {
        isUpdatingDropdownValue = true;

        resolutionDropdown.ClearOptions();
        resolutionDropdown.AddOptions(options);
        SetDropdownValue(currentResolutionIndex);
    }

    private void UpdateResolutionScrollView(List<string> options)
    {
        // Remove all old resolutions.
        for (int child = 0; child < resolutionScrollRect.content.childCount; child++)
        {
            resolutionScrollRect.content.GetChild(child).gameObject.SetActive(false);
            Destroy(resolutionScrollRect.content.GetChild(child).gameObject);
        }

        resolutionScrollRect.Initialize(options);
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
        // For debugging resolution settings
        // currentMaxRes.SetText(Screen.resolutions[Screen.resolutions.Length - 1].width + "x" + Screen.resolutions[Screen.resolutions.Length - 1].height);


        // Detect if the biggest available resolution changed and if so set new reolution options.
        if (biggestResolutionForThisScreen.x != Screen.resolutions[Screen.resolutions.Length - 1].width || biggestResolutionForThisScreen.y != Screen.resolutions[Screen.resolutions.Length - 1].height)
        {
            Debug.Log("Redo resolution options");
            FindAllowedResolutions();
        }


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

        if (canvasScaler.scaleFactor != pixelPerfectCamera.pixelRatio * 2)
        {
            ResizeCanvas();
            SetDropdownValue(pixelPerfectCamera.pixelRatio - 1);
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
            SetWindowResolution(allowedResolutions.Count - 1);
            currentResolutionIndex = allowedResolutions.Count - 2;
            SetDropdownValue(currentResolutionIndex);
        }
        else if (startUpDone)
        {
            // would be nice to use the biggest one that doesnt fill the whole sreen, now I'm using the second biggest one.

            if (allowedResolutions[allowedResolutions.Count - 1] == allowedResolutions[allowedResolutions.Count - 2])
            {
                SetWindowResolution(Mathf.Max(allowedResolutions.Count - 3, 0));

            }
            else
            {
                SetWindowResolution(Mathf.Max(allowedResolutions.Count - 2, 0));
            }
        }

        Screen.fullScreen = wantFullscreen;
        resolutionDropdown.interactable = !wantFullscreen;
        SetDropdownValue(currentResolutionIndex);

        leftArrow.GetComponent<Button>().interactable = !wantFullscreen;
        rightArrow.GetComponent<Button>().interactable = !wantFullscreen;

        fullscreenToggle.isOn = wantFullscreen;
    }

    public void SetWindowResolution(int wantedResolutionIndex)
    {
        if (startUpDone && !isUpdatingDropdownValue)
        {
            currentResolutionIndex = wantedResolutionIndex;
            Screen.SetResolution(Mathf.RoundToInt(allowedResolutions[currentResolutionIndex].x), Mathf.RoundToInt(allowedResolutions[currentResolutionIndex].y), Screen.fullScreen);
            SetDropdownValue(currentResolutionIndex);
        }
    }

    public void SetWindowResolution()
    {
        if (startUpDone && !isUpdatingDropdownValue)
        {
            currentResolutionIndex = resolutionScrollRect.currentDisplayedResolution - 1;
            Screen.SetResolution(Mathf.RoundToInt(allowedResolutions[currentResolutionIndex].x), Mathf.RoundToInt(allowedResolutions[currentResolutionIndex].y), Screen.fullScreen);
        }
    }

    private void ResizeCanvas()
    {
        canvasScaler.scaleFactor = pixelPerfectCamera.pixelRatio;
    }




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
}
