using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FMODUnity;
using FMOD.Studio;

public class MusicManager : MonoBehaviour
{
    public static MusicManager instance { get; private set; }

    [field: Header("Music")]
    [field: SerializeField] public EventReference titleMusic { get; private set; }
    [field: SerializeField] public EventReference inRunMusic { get; private set; }

    private EventInstance titleMusicEvent;
    private EventInstance inRunMusicEvent;

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

        titleMusicEvent = RuntimeManager.CreateInstance(titleMusic);
        inRunMusicEvent = RuntimeManager.CreateInstance(inRunMusic);
    }

    // Start is called before the first frame update
    void Start()
    {

    }

    public void PlayTitleMusic()
    {
        UnpauseMusic();
        inRunMusicEvent.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
        titleMusicEvent.start();


        /*
        CheckAudioSource();
        audioSource.volume = titleMusicVolume;
        if (audioSource == null || audioSource.clip == null || !audioSource.clip.Equals(titleMusic) || !audioSource.isPlaying)
        {
            audioSource.clip = titleMusic;
            audioSource.Play();
        }
        */
    }

    public void PlayLevelMusic()
    {
        UnpauseMusic();
        titleMusicEvent.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
        inRunMusicEvent.start();

        /*
        CheckAudioSource();
        audioSource.volume = levelMusicVolume;
        audioSource.clip = levelMusic;
        audioSource.Play();
        */
    }

    public void PauseMusic()
    {
        titleMusicEvent.setPaused(true);
        inRunMusicEvent.setPaused(true);

        /*
        CheckAudioSource();
        audioSource.Pause();
        */
    }

    public void UnpauseMusic()
    {
        titleMusicEvent.setPaused(false);
        inRunMusicEvent.setPaused(false);

        /*
        CheckAudioSource();
        audioSource.UnPause();
        */
    }
}
