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

    [field: Header("Music - by Brian")]
    [field: SerializeField] public EventReference titleMusicByBrian { get; private set; }
    [field: SerializeField] public EventReference runMusicByBrian { get; private set; }
    [SerializeField] private int downToTension1Limit;
    [SerializeField] private int upToTension2Limit;
    [SerializeField] private int downToTension2Limit;
    [SerializeField] private int upToTension3Limit;

    private int debugBugs;
    private int debugTension;

    private EventInstance titleMusicEvent;
    private EventInstance inRunMusicEvent;
    private EventInstance runMusicByBrianEvent;
    private EventInstance titleMusicByBrianEvent;

    private MusicChoice musicChoice = MusicChoice.ByBrian;

    enum MusicChoice
    {
        ByJohanna,
        ByBrian
    }

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
        runMusicByBrianEvent = RuntimeManager.CreateInstance(runMusicByBrian);
        titleMusicByBrianEvent = RuntimeManager.CreateInstance(titleMusicByBrian);
    }

    // Start is called before the first frame update
    void Start()
    {

    }

    public void PlayTitleMusic()
    {
        if (musicChoice == MusicChoice.ByBrian)
        {
            // TODO will I need to reset all the parameters?
            RuntimeManager.StudioSystem.setParameterByName("Invincible", 0); // reset
            runMusicByBrianEvent.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            titleMusicByBrianEvent.getPlaybackState(out var titleMusicPlaybackState);
            if (titleMusicPlaybackState == PLAYBACK_STATE.STOPPED || titleMusicPlaybackState == PLAYBACK_STATE.STOPPING)
            {
                // If not started, start.
                titleMusicByBrianEvent.start();
            }
            else
            {
                // If started, pause.
                titleMusicByBrianEvent.setPaused(false);
            }
        }
        else
        {
            UnpauseMusic();
            inRunMusicEvent.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            titleMusicEvent.start();
        }

    }

    public void PlayRunMusic()
    {
        if (musicChoice == MusicChoice.ByBrian)
        {
            RuntimeManager.StudioSystem.setParameterByName("Upgrades", 0);
            titleMusicByBrianEvent.setPaused(true);
            runMusicByBrianEvent.start();
        }
        else
        {
            UnpauseMusic();
            titleMusicEvent.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            inRunMusicEvent.start();
        }
    }

    public void PlayLevelUpMusic(bool levelUpIsVisible)
    {
        if (musicChoice == MusicChoice.ByBrian)
        {
            titleMusicByBrianEvent.getPlaybackState(out var isPlaying);
            if (isPlaying == PLAYBACK_STATE.STOPPED || isPlaying == PLAYBACK_STATE.STOPPING)
            {
                titleMusicByBrianEvent.start();
            }

            RuntimeManager.StudioSystem.setParameterByName("Upgrades", levelUpIsVisible ? 1 : 0); // used to "pause" and "unpause" the run music.
            titleMusicByBrianEvent.setPaused(!levelUpIsVisible);
        }
    }

    public void PlaySuperFrogMusic(bool SFIsActive)
    {
       if (musicChoice == MusicChoice.ByBrian)
       {
            RuntimeManager.StudioSystem.setParameterByName("Invincible", SFIsActive ? 1 : 0);
       }
    }

    public void AdjustTensionLevel(int bugs, int from)
    {
        if (musicChoice == MusicChoice.ByBrian)
        {
            debugBugs = bugs;
            // Debug.Log("Bugs: " + bugs);
            RuntimeManager.StudioSystem.getParameterByName("Tension Level", out var tensionLevelOut);
            int tensionLevel = (int)tensionLevelOut;
            Debug.Log("TensionLevel: " + tensionLevel + " - before set up, from: " + from);

            if (tensionLevel == 2)
            {
                if (bugs < downToTension1Limit)
                {
                    tensionLevel = 1;
                }
                else if (bugs > upToTension3Limit)
                {
                    tensionLevel = 3;
                }
            }
            else if ((tensionLevel == 3 && bugs < downToTension2Limit) || (tensionLevel == 1 && bugs > upToTension2Limit))
            {
                tensionLevel = 2;
            }
            else
            {
                Debug.Log("tensionLevel: " + tensionLevel + " - didn't change");
            }

            debugTension = tensionLevel;
            RuntimeManager.StudioSystem.setParameterByName("Tension Level", tensionLevel);
        }
    }

    public void StopMusic()
    {
        if (musicChoice == MusicChoice.ByBrian)
        {

        }
        else
        {
            titleMusicEvent.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            inRunMusicEvent.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
        }
    }

    public void UnpauseMusic()
    {
        if (musicChoice == MusicChoice.ByBrian)
        {

        }
        else
        {
            titleMusicEvent.setPaused(false);
            inRunMusicEvent.setPaused(false);
        }
    }
}
