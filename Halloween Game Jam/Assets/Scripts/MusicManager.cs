using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MusicManager : MonoBehaviour
{
    public static MusicManager instance;

    [Header("Music")]
    public AudioClip titleMusic;
    [Range(0, 1)] public float titleMusicVolume = 1;
    public AudioClip levelMusic;
    [Range(0, 1)] public float levelMusicVolume = 1;

    private AudioSource audioSource;

    private void Awake()
    {
        instance = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        CheckAudioSource();
    }

    // Update is called once per frame
    void Update()
    {

    }

    private void CheckAudioSource()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
    }

    public void PlayTitleMusic()
    {
        CheckAudioSource();
        audioSource.volume = titleMusicVolume;
        if (audioSource == null || audioSource.clip == null || !audioSource.clip.Equals(titleMusic) || !audioSource.isPlaying)
        {
            audioSource.clip = titleMusic;
            audioSource.Play();
        }
    }

    public void PlayLevelMusic()
    {
        CheckAudioSource();
        audioSource.volume = levelMusicVolume;
        audioSource.clip = levelMusic;
        audioSource.Play();
    }

    public void PauseMusic()
    {
        CheckAudioSource();
        audioSource.Pause();
    }

    public void UnpauseMusic()
    {
        CheckAudioSource();
        audioSource.UnPause();
    }
}
