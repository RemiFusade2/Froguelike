using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Froguelike_SoundManager : MonoBehaviour
{
    [Header("Audio Clips")]
    public AudioClip buttonSound;
    [Range(0, 1)] public float buttonVolume = 1;
    public AudioClip deathSound;
    [Range(0, 1)] public float deathVolume = 1;
    public AudioClip pageSound;
    [Range(0, 1)] public float pageVolume = 1;

    private AudioSource audioSource;

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

    public void PlayPageSound()
    {
        audioSource.volume = pageVolume;
        audioSource.PlayOneShot(pageSound);
    }

    public void PlayDeathSound()
    {
        audioSource.volume = deathVolume;
        audioSource.PlayOneShot(deathSound);
    }
}
