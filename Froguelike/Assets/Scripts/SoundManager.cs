using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SoundManager : MonoBehaviour
{
    public static SoundManager instance;

    [Header("Audio Clips")]
    public AudioClip buttonSound;
    [Range(0, 1)] public float buttonVolume = 1;
    public AudioClip deathSound;
    [Range(0, 1)] public float deathVolume = 1;
    public AudioClip pageLongSound;
    [Range(0, 1)] public float pageLongVolume = 1;
    public AudioClip pageShortSound;
    [Range(0, 1)] public float pageShortVolume = 1;
    public AudioClip slideBookSound;
    [Range(0, 1)] public float slideBookVolume = 1;

    private AudioSource audioSource;
    private bool isSoundOn;
    private float volumeModifier = 1;

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
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        audioSource = GetComponent<AudioSource>();
    }

    // Update is called once per frame
    void Update()
    {

    }

    /*
    // Turns sound on with true, turns sound of with false.
    public void SoundOn(bool on)
    {
        if (on && audioSource.mute)
        {
            audioSource.mute = false;
        }
        else if (!on && !audioSource.mute)
        {
            audioSource.mute = true;
        }
    }

    public void SetVolumeModifier(float percentage)
    {
        // Sets the percentage for the volume. (0% - 200%)
        volumeModifier = Mathf.Clamp(percentage, 0, 200);
    }
    */

    private float ModifyVolume(float volume)
    {
        float newVolume = volume * volumeModifier;
        return newVolume;
    }


    #region Play sound

    public void PlayButtonSound(Button button)
    {
        if (button.interactable)
        {
            audioSource.volume = ModifyVolume(buttonVolume);
            audioSource.PlayOneShot(buttonSound);
        }
    }

    public void PlayButtonSound(Toggle toggle)
    {
        if (toggle.interactable)
        {
            audioSource.volume = ModifyVolume(buttonVolume);
            audioSource.PlayOneShot(buttonSound);
        }
    }

    public void PlayButtonSound(TMP_Dropdown dropdown)
    {
        if (dropdown.interactable)
        {
            audioSource.volume = ModifyVolume(buttonVolume);
            audioSource.PlayOneShot(buttonSound);
        }
    }

    public void PlayLongPageSound()
    {
        audioSource.volume = ModifyVolume(pageLongVolume);
        audioSource.PlayOneShot(pageLongSound);
    }

    public void PlayDeathSound()
    {
        audioSource.volume = ModifyVolume(deathVolume);
        audioSource.PlayOneShot(deathSound);
    }

    public void PlayShortPageSound()
    {
        audioSource.volume = ModifyVolume(pageShortVolume);
        audioSource.PlayOneShot(pageShortSound);
    }

    public void PlaySlideBookSound()
    {
        audioSource.volume = ModifyVolume(slideBookVolume);
        audioSource.PlayOneShot(slideBookSound);
    }

    #endregion Play sound
}
