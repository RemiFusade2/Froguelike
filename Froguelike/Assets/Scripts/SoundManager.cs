using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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

    public void PlayButtonSound(Button button)
    {
        if (button.interactable)
        {
            audioSource.volume = buttonVolume;
            audioSource.PlayOneShot(buttonSound);
        }
    }

    public void PlayLongPageSound()
    {
        audioSource.volume = pageLongVolume;
        audioSource.PlayOneShot(pageLongSound);
    }

    public void PlayDeathSound()
    {
        audioSource.volume = deathVolume;
        audioSource.PlayOneShot(deathSound);
    }

    public void PlayShortPageSound()
    {
        audioSource.volume = pageShortVolume;
        audioSource.PlayOneShot(pageShortSound);
    }

    public void PlaySlideBookSound()
    {
        audioSource.volume = slideBookVolume;
        audioSource.PlayOneShot(slideBookSound);
    }
}
