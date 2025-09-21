using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering.Universal;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Audio;
using FMODUnity;
using FMOD.Studio;
using System;

public class SettingsManager : MonoBehaviour
{
    public static SettingsManager instance;

    [Header("Tabs")]
    public List<GameObject> listOfTabs;
    public List<GameObject> listOfHiders;

    [Header("Resolution")]
    public Toggle fullscreenToggle;
    public ResolutionsScrollRect resolutionScrollRect;
    public GameObject leftArrow;
    public GameObject rightArrow;
    public PixelPerfectCamera pixelPerfectCamera;
    public CanvasScaler canvasScaler;

    #region Disclaimer screens

    [Header("Disclaimer settings")]
    public Toggle disclaimerToggle;
    public DisclaimerScreen eaDisclaimerScreen;
    public DisclaimerScreen demoDisclaimerScreen;
    private string savedShowDemoDisclaimerSettingKey = "Froguelike Demo Disclaimer on";
    private string savedShowEADisclaimerSettingKey = "Froguelike EA Disclaimer on";

    #endregion Disclaimer screens

    #region Damage text

    [Header("Damage text settings")]
    public Toggle damageTextToggle;
    public bool showDamageText { get; private set; }
    private string savedDamageTextSettingKey = "Froguelike damage text visible";

    #endregion Damage text

    #region Sound

    [Header("Sound")]
    public SoundManager soundManager;
    public Toggle SFXToggle;
    public Slider SFXSlider;
    public Toggle musicToggle;
    public Slider musicSlider;
    public Toggle ambienceToggle;
    public Slider ambienceSlider;
    public Toggle bugSoundsToggle;
    public Slider bugSoundsSlider;

    // Remember previous volume when toggling sound off.
    private float previousSFXVolume;
    private float previousMusicVolume;
    private float previousAmbienceVolume;
    private float previousBugSoundsVolume;

    // Save values in player prefs.
    // SFX.
    private bool savedSFXOn;
    private string savedSFXOnKey = "Froguelike SFX on";
    private float savedSFXVolume;
    private string savedSFXVolumeKey = "Froguelike SFX volume";
    // Music.
    private bool savedMusicOn;
    private string savedMusicOnKey = "Froguelike music on";
    private float savedMusicVolume;
    private string savedMusicVolumeKey = "Froguelike music volume";
    // Ambience.
    private bool savedAmbienceOn;
    private string savedAmbienceOnKey = "Froguelike ambience on";
    private float savedAmbienceVolume;
    private string savedAmbienceVolumeKey = "Froguelike ambience volume";
    // Bug sounds.
    private bool savedBugSoundsOn;
    private string savedBugSoundsOnKey = "Froguelike bug sounds on";
    private float savedBugSoundsVolume;
    private string savedBugSoundsVolumeKey = "Froguelike bug sounds volume";

    #endregion Sound

    #region Font

    [Header("Font")]
    public List<TMP_FontAsset> listOfFonts;
    public List<string> listOfFontNames;
    public FontsScrollRect fontsScrollRect;

    [SerializeField] private GameObject pixelTextOnShopNote;
    [SerializeField] private GameObject notPixelTextOnShopNote;
    private UnityEngine.Object[] textObjectsList = new UnityEngine.Object[] { };
    private string savedFontSettingKey = "Froguelike Saved Font";
    private int currentFontIndex;

    #endregion Font

    #region Flashing effects

    [Header("Flashing effects setting")]
    public Toggle flashingEffectsToggle;
    public bool showFlashingEffects { get; private set; }
    private string savedFlashingEffectSettingKey = "Froguelike flashing effect visible";

    #endregion Flashing effects

    [Header("For debugging")]
    public TextMeshProUGUI text;
    private DisplayInfo savedDisplayInfo;

    // Various private variables.
    private Vector2Int biggestResolutionForThisScreen;
    private List<Vector2Int> allowedResolutions;
    private bool resFixDone = false;
    private bool changeResBack = false;

    int gameWidth;
    int gameHeight;

    int currentResolutionIndex = -1;

    bool startUpDone = false;
    bool isChangingFullscreen = false;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(this);
        }
        else
        {
            Destroy(this.gameObject);
            Debug.Log("TEST");
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        // Set the fullsccreen toggle to match the current fullscreen mode.
        fullscreenToggle.isOn = Screen.fullScreen;

        // Get the intended resolution of the game.
        gameWidth = pixelPerfectCamera.refResolutionX;
        gameHeight = pixelPerfectCamera.refResolutionY;

        FindAllowedResolutions();

        startUpDone = true;

        LoadAudioSettings();
        LoadDisclaimerSettings();
        LoadDamageTextSetting();
        LoadFlashingEffectsSetting();

        // Find all text boxes in the game.
        textObjectsList = Resources.FindObjectsOfTypeAll(typeof(TextMeshProUGUI));
        LoadFontSetting();
        UpdateFontScrollView();
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

        // Make sure the full screen toggle is displaying correctly.
        if ((Screen.fullScreen && !fullscreenToggle.isOn) || (!Screen.fullScreen && fullscreenToggle.isOn))
        {
            if (!isChangingFullscreen)
            {
                fullscreenToggle.SetIsOnWithoutNotify(Screen.fullScreen);

                if (Screen.fullScreen)
                {
                    SetWindowResolution(allowedResolutions.Count - 1);
                    resolutionScrollRect.UpdateScroll(false, currentResolutionIndex + 1);
                }
            }
        }

        // Resizes the canvas to match the pixel ratio.
        if (canvasScaler.scaleFactor != pixelPerfectCamera.pixelRatio)
        {
            ResizeCanvas();
            resolutionScrollRect.UpdateScroll(false, pixelPerfectCamera.pixelRatio);
        }

        // Makes sure the thintel font show up when not playing in fullscreen on mac.
        if (Application.platform == RuntimePlatform.OSXPlayer && !resFixDone)
        {
            SetResAtStart();
        }
    }

    private void LateUpdate()
    {
        if (isChangingFullscreen)
        {
            isChangingFullscreen = false;
        }
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
                // If the actual current resolution doesn't match, make it match.
                if (PlayerPrefs.GetInt("Screenmanager Resolution Height") != allowedResolutions[currentResolutionIndex].y || PlayerPrefs.GetInt("Screenmanager Resolution Width") != allowedResolutions[currentResolutionIndex].x)
                {
                    SetWindowResolution(currentResolutionIndex);
                }
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

    // Used if the current resolution doesn't match any of the allowed resolutions.
    public void SetWindowResolution(int wantedResolutionIndex)
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

    // Used when changing resolution with the arrows on the setting screen.
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

        // Ambience.
        savedAmbienceOn = PlayerPrefs.GetInt(savedAmbienceOnKey, 1) == 1;
        if (savedAmbienceOn)
        {
            savedAmbienceVolume = PlayerPrefs.GetFloat(savedAmbienceVolumeKey);
            SetAmbienceVolume(savedAmbienceVolume);
            ambienceSlider.SetValueWithoutNotify(savedAmbienceVolume);
        }
        else
        {
            previousAmbienceVolume = PlayerPrefs.GetFloat(savedAmbienceVolumeKey);
            ambienceSlider.SetValueWithoutNotify(previousAmbienceVolume);
        }
        ambienceToggle.isOn = savedAmbienceOn;

        // Bug sounds.
        savedBugSoundsOn = PlayerPrefs.GetInt(savedBugSoundsOnKey, 1) == 1;
        if (savedBugSoundsOn)
        {
            savedBugSoundsVolume = PlayerPrefs.GetFloat(savedBugSoundsVolumeKey);
            SetBugSoundsVolume(savedBugSoundsVolume);
            bugSoundsSlider.SetValueWithoutNotify(savedBugSoundsVolume);
        }
        else
        {
            previousBugSoundsVolume = PlayerPrefs.GetFloat(savedBugSoundsVolumeKey);
            bugSoundsSlider.SetValueWithoutNotify(previousBugSoundsVolume);
        }
        bugSoundsToggle.isOn = savedBugSoundsOn;
    }

    #region Setters

    // Turns SFX on with true, turns SFX of with false.
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

    // Turns music on with true, turns music of with false.
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

    // Turns ambience on with true, turns ambience of with false.
    public void AmbienceOn(bool on)
    {
        PlayerPrefs.SetInt(savedAmbienceOnKey, on ? 1 : 0);

        if (on)
        {
            SetAmbienceVolume(previousAmbienceVolume);
            ambienceSlider.SetValueWithoutNotify(previousAmbienceVolume);
        }
        else
        {
            if (ambienceSlider.value == ambienceSlider.minValue)
            {
                previousAmbienceVolume = ambienceSlider.maxValue / 2;
            }
            else
            {
                previousAmbienceVolume = ambienceSlider.value;
                ambienceSlider.SetValueWithoutNotify(ambienceSlider.minValue);
            }

            PlayerPrefs.SetFloat(savedAmbienceVolumeKey, previousAmbienceVolume);
        }

        SoundManager.instance.MuteAmbienceBus(!on);
    }

    // Turns bug sounds on with true, turns bug sounds of with false.
    public void BugSoundsOn(bool on)
    {
        PlayerPrefs.SetInt(savedBugSoundsOnKey, on ? 1 : 0);

        if (on)
        {
            SetBugSoundsVolume(previousBugSoundsVolume);
            bugSoundsSlider.SetValueWithoutNotify(previousBugSoundsVolume);
        }
        else
        {
            if (bugSoundsSlider.value == bugSoundsSlider.minValue)
            {
                previousBugSoundsVolume = bugSoundsSlider.maxValue / 2;
            }
            else
            {
                previousBugSoundsVolume = bugSoundsSlider.value;
                bugSoundsSlider.SetValueWithoutNotify(bugSoundsSlider.minValue);
            }

            PlayerPrefs.SetFloat(savedBugSoundsVolumeKey, previousBugSoundsVolume);
        }

        SoundManager.instance.MuteBugSoundsBus(!on);
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

    // Sets volume and updates the check box if necessary.
    public void SetAmbienceVolume(float volume)
    {
        PlayerPrefs.SetFloat(savedAmbienceVolumeKey, volume);

        float newVolume = volume / ambienceSlider.maxValue * 2;

        if (newVolume == ambienceSlider.minValue)
        {
            if (ambienceToggle.isOn)
            {
                ambienceToggle.SetIsOnWithoutNotify(false);
                AmbienceOn(false);
            }
        }
        else if (newVolume > 0)
        {
            if (!ambienceToggle.isOn)
            {
                ambienceToggle.SetIsOnWithoutNotify(true);
                PlayerPrefs.SetInt(savedAmbienceOnKey, 1);
                SoundManager.instance.MuteAmbienceBus(false);
            }
        }

        SoundManager.instance.SetNewAmbienceVolume(newVolume);
    }

    // Sets volume and updates the check box if necessary.
    public void SetBugSoundsVolume(float volume)
    {
        PlayerPrefs.SetFloat(savedBugSoundsVolumeKey, volume);

        float newVolume = volume / bugSoundsSlider.maxValue * 2;

        if (newVolume == bugSoundsSlider.minValue)
        {
            if (bugSoundsToggle.isOn)
            {
                bugSoundsToggle.SetIsOnWithoutNotify(false);
                BugSoundsOn(false);
            }
        }
        else if (newVolume > 0)
        {
            if (!bugSoundsToggle.isOn)
            {
                bugSoundsToggle.SetIsOnWithoutNotify(true);
                PlayerPrefs.SetInt(savedBugSoundsOnKey, 1);
                SoundManager.instance.MuteBugSoundsBus(false);
            }
        }

        SoundManager.instance.SetNewBugSoundsVolume(newVolume);
    }

    #endregion Setters

    #endregion Sound

    #region Disclaimer screens

    public void LoadDisclaimerSettings()
    {
        if (BuildManager.instance.demoBuild)
        {
            disclaimerToggle.SetIsOnWithoutNotify(IsDemoDisclaimerOn());
        }
        else
        {
            disclaimerToggle.SetIsOnWithoutNotify(IsEADisclaimerOn());
        }
    }

    public bool IsDemoDisclaimerOn()
    {
        return PlayerPrefs.GetInt(savedShowDemoDisclaimerSettingKey, 1) == 1;
    }

    public void SetDemoDisclaimerOn(bool on)
    {
        PlayerPrefs.SetInt(savedShowDemoDisclaimerSettingKey, on ? 1 : 0);
        disclaimerToggle.SetIsOnWithoutNotify(on);
    }

    public bool IsEADisclaimerOn()
    {
        return PlayerPrefs.GetInt(savedShowEADisclaimerSettingKey, 1) == 1;
    }

    public void SetEADisclaimerOn(bool on)
    {
        PlayerPrefs.SetInt(savedShowEADisclaimerSettingKey, on ? 1 : 0);
        disclaimerToggle.SetIsOnWithoutNotify(on);
    }

    public void ToggleShowDisclaimer()
    {
        if (BuildManager.instance.demoBuild)
        {
            demoDisclaimerScreen.ToggleShowDisclaimerAgain();
        }
        else
        {
            eaDisclaimerScreen.ToggleShowDisclaimerAgain();
        }
    }

    #endregion Disclaimer screens

    #region Damage text

    private void LoadDamageTextSetting()
    {
        showDamageText = PlayerPrefs.GetInt(savedDamageTextSettingKey, 1) == 1 ? true : false;
        damageTextToggle.SetIsOnWithoutNotify(showDamageText);
    }

    // Called from the toggle in the settings menu.
    public void ToggleShowDamageText()
    {
        // Revert the current status for showing the damage text and then save it.
        showDamageText = !showDamageText;
        PlayerPrefs.SetInt(savedDamageTextSettingKey, showDamageText == true ? 1 : 0);
    }

    #endregion Damage text

    #region Font

    public void LoadFontSetting()
    {
        currentFontIndex = PlayerPrefs.GetInt(savedFontSettingKey, 0);
        SetFont(currentFontIndex);
    }

    public void SaveFontSetting(int fontIndex)
    {
        PlayerPrefs.SetInt(savedFontSettingKey, fontIndex);
    }

    // Used when loading font setting.
    public void SetFont(int fontIndex)
    {
        foreach (TextMeshProUGUI text in textObjectsList)
        {
            text.font = listOfFonts[fontIndex];
        }

        if (listOfFonts[fontIndex].name == "Thintel")
        {
            pixelTextOnShopNote.SetActive(true);
            notPixelTextOnShopNote.SetActive(false);
        }
        else
        {
            notPixelTextOnShopNote.SetActive(true);
            pixelTextOnShopNote.SetActive(false);
        }

        SaveFontSetting(currentFontIndex);
    }

    // Used when changing font with the arrows on the setting screen.
    public void SetFont()
    {
        currentFontIndex = fontsScrollRect.currentDisplayedFont - 1;

        SetFont(currentFontIndex);
    }

    public TMP_FontAsset GetCurrentFontAsset()
    {
        return listOfFonts[currentFontIndex];
    }

    private void UpdateFontScrollView()
    {
        // Remove all old fonts.
        for (int child = 0; child < fontsScrollRect.content.childCount; child++)
        {
            fontsScrollRect.content.GetChild(child).gameObject.SetActive(false);
            Destroy(fontsScrollRect.content.GetChild(child).gameObject);
        }

        fontsScrollRect.Initialize(listOfFonts, listOfFontNames, currentFontIndex);
    }

    // Fixes a bug that made the pixel font dissapear when starting the game not in fullscreen on mac, is only called if the application is running on mac.
    private void SetResAtStart()
    {
        if (!Screen.fullScreen)
        {
            Vector2Int currentRes = allowedResolutions[Mathf.Max(currentResolutionIndex, 0)];

            if (!changeResBack)
            {
                Screen.SetResolution(currentRes.x + 1, currentRes.y + 1, false);
            }
            else
            {
                Screen.SetResolution(currentRes.x, currentRes.y, false);
            }
        }

        if (changeResBack)
        {
            resFixDone = true;
        }
        else
        {
            changeResBack = true;
        }
    }

    #endregion Font

    #region Flashing effects

    private void LoadFlashingEffectsSetting()
    {
        showFlashingEffects = PlayerPrefs.GetInt(savedFlashingEffectSettingKey, 1) == 1 ? true : false;
        flashingEffectsToggle.SetIsOnWithoutNotify(showFlashingEffects);
    }

    public void ToggleFlashingEffects()
    {
        // Revert the current status for using flashing effects and save it.
        showFlashingEffects = !showFlashingEffects;
        PlayerPrefs.SetInt(savedFlashingEffectSettingKey, showFlashingEffects == true ? 1 : 0);
    }

    #endregion Flashing effects

    #region Tabs

    public void ChangeTab(int toTabIndex)
    {
        for (int tabIndex = 0; tabIndex < listOfTabs.Count; tabIndex++)
        {
            listOfTabs[tabIndex].SetActive(false);
            listOfTabs[tabIndex].GetComponent<CanvasGroup>().interactable = false;
            listOfHiders[tabIndex].SetActive(true);
        }

        listOfTabs[toTabIndex].SetActive(true);
        listOfTabs[toTabIndex].GetComponent<CanvasGroup>().interactable = true;
        listOfHiders[toTabIndex].SetActive(false);
    }

    #endregion Tabs
}
