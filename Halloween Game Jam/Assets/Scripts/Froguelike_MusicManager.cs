using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Froguelike_MusicManager : MonoBehaviour
{
    public static Froguelike_MusicManager instance;

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
        audioSource = GetComponent<AudioSource>();
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void PlayTitleMusic()
    {
        audioSource.volume = titleMusicVolume;
        audioSource.clip = titleMusic;
        audioSource.Play();
    }

    public void PlayLevelMusic()
    {
        audioSource.volume = levelMusicVolume;
        audioSource.clip = levelMusic;
        audioSource.Play();
    }
}
