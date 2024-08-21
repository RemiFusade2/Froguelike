using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FMODUnity;
using FMOD.Studio;

public class SoundManager : MonoBehaviour
{
    public static SoundManager instance { get; private set; }

    [field: Header("FMOD Events")]
    [field: SerializeField] public EventReference buttonSound { get; private set; }
    [field: SerializeField] public EventReference deathSound { get; private set; }
    [field: SerializeField] public EventReference pageLongSound { get; private set; }
    [field: SerializeField] public EventReference pageShortSound { get; private set; }
    [field: SerializeField] public EventReference slideBookSound { get; private set; }

    [field: SerializeField] public EventReference takeDamageSound { get; private set; }
    [field: SerializeField] public EventReference healSound { get; private set; }

    [field: SerializeField] public EventReference pickUpFroinsSound { get; private set; }
    [field: SerializeField] public EventReference pickUpXPSound { get; private set; }
    [field: SerializeField] public EventReference grabCollectibleSound { get; private set; }
    [field: SerializeField] public EventReference levelUpSound { get; private set; }

    [field: SerializeField] public EventReference buyItemInShopSound { get; private set; }
    [field: SerializeField] public EventReference cantBuyItemInShopSound { get; private set; }
    [field: SerializeField] public EventReference refundShopSound { get; private set; }

    [field: SerializeField] public EventReference rerollSound { get; private set; }
    [field: SerializeField] public EventReference skipSound { get; private set; }

    [field: SerializeField] public EventReference eatBountySound { get; private set; } // Not used yet.
    [field: SerializeField] public EventReference eatBugSound { get; private set; } // Not used yet.

    [field: SerializeField] public EventReference chapterEndSound { get; private set; }

    [field: Header("Power ups")]
    [field: SerializeField] public EventReference powerUpPartySound { get; private set; }
    [field: SerializeField] public EventReference powerUpGoopSound { get; private set; }
    [field: SerializeField] public EventReference powerUpFreezeAllSound { get; private set; }
    [field: SerializeField] public EventReference powerUpPoisonAllSound { get; private set; }
    [field: SerializeField] public EventReference powerUpCurseAllSound { get; private set; }

    private EventInstance takeDamageEvent;
    private EventInstance pickUpXPEvent;

    private Bus musicBus;
    private Bus SFXBus;


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

        musicBus = RuntimeManager.GetBus("bus:/Music");
        SFXBus = RuntimeManager.GetBus("bus:/SFX");
    }

    private void Start()
    {
        takeDamageEvent = RuntimeManager.CreateInstance(takeDamageSound);
        pickUpXPEvent = RuntimeManager.CreateInstance(pickUpXPSound);
    }

    #region Settings

    // Settings.
    public void SetNewMusicVolume(float volume)
    {
        musicBus.setVolume(volume);
    }

    public void SetNewSFXVolume(float volume)
    {
        SFXBus.setVolume(volume);
    }

    public void MuteMusicBus(bool mute)
    {
        musicBus.setMute(mute);
    }

    public void MuteSFXBus(bool mute)
    {
        SFXBus.setMute(mute);
    }

    private void PauseInGameLoopedSFX(bool paused)
    {
        takeDamageEvent.setPaused(paused);
    }

    public void PauseInGameLoopedSFX()
    {
        PauseInGameLoopedSFX(true);
    }

    public void UnpauseInGameLoopedSFX()
    {
        PauseInGameLoopedSFX(false);
    }

    #endregion Settings

    #region Play loops

    private void MakeLoopEventStart(EventInstance source)
    {
        source.start();
    }

    private void MakeLoopEventStop(EventInstance source)
    {
        source.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
    }

    public void PlayTakeDamageLoopSound() // TODO
    {
        // TODO fade in.
        // Start playing
        PLAYBACK_STATE playbackState;
        takeDamageEvent.getPlaybackState(out playbackState);
        if (playbackState.Equals(PLAYBACK_STATE.STOPPED))
        {
            takeDamageEvent.start();
        }
    }

    public void StopPlayingTakeDamageLoopSound() // TODO
    {
        // TODO fade out.
        PLAYBACK_STATE playbackState;
        takeDamageEvent.getPlaybackState(out playbackState);
        if (playbackState.Equals(PLAYBACK_STATE.PLAYING))
        {
            takeDamageEvent.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
        }
    }

    public void StopAllLoops()
    {
        MakeLoopEventStop(takeDamageEvent); // Stop playing
    }

    #endregion Play loops

    #region Play "one shot" sound

    public void PlayButtonSound(Selectable selectable)
    {
        if (selectable.interactable)
        {
            RuntimeManager.PlayOneShot(buttonSound);
        }
    }

    public void PlayLongPageSound()
    {
        RuntimeManager.PlayOneShot(pageLongSound);
    }

    public void PlayDeathSound()
    {
        RuntimeManager.PlayOneShot(deathSound);
    }

    public void PlayShortPageSound()
    {
        RuntimeManager.PlayOneShot(pageShortSound);
    }

    public void PlaySlideBookSound()
    {
        RuntimeManager.PlayOneShot(slideBookSound);
    }

    public void PlayHealSound()
    {
        RuntimeManager.PlayOneShot(healSound);
    }

    public void PlayPickUpFroinsSound()
    {
        RuntimeManager.PlayOneShot(pickUpFroinsSound);
    }

    public void PlayPickUpXPSound(float xpValue) // TODO
    {
        // TODO could have a parameter for the volume multiplier, it was set up to be louder if it is more xp with the old system.
        int parameterValue;
        if (xpValue < 6)
        {
            parameterValue = 0;
        }
        else if (xpValue < 11)
        {
            parameterValue = 1;
        }
        else if (xpValue < 20)
        {
            parameterValue = 2;
        }
        else if (xpValue < 30)
        {
            parameterValue = 3;
        }
        else
        {
            parameterValue = 4;
        }
        pickUpXPEvent.setParameterByName("Amount of XP", parameterValue);
        RuntimeManager.PlayOneShot(pickUpXPSound);
    }

    public void PlayPickUpCollectibleSound()
    {
        RuntimeManager.PlayOneShot(grabCollectibleSound);
    }

    public void PlayLevelUpSound()
    {
        RuntimeManager.PlayOneShot(levelUpSound);
    }

    public void PlayBuyItemInShopSound()
    {
        RuntimeManager.PlayOneShot(buyItemInShopSound);
    }

    public void PlayCantBuyItemInShopSound()
    {
        RuntimeManager.PlayOneShot(cantBuyItemInShopSound);
    }

    public void PlayRefundShopSound()
    {
        RuntimeManager.PlayOneShot(refundShopSound);
    }

    public void PlayEatBountySound()
    {
        RuntimeManager.PlayOneShot(eatBountySound);
    }

    public void PlayEatBugSound()
    {
        RuntimeManager.PlayOneShot(eatBugSound);
    }

    public void PlayRerollSound()
    {
        // TODO No sound yet.
    }

    public void PlaySkipSound()
    {
        // TODO No sound yet.
    }

    public void PlayChapterEndSound()
    {
        RuntimeManager.PlayOneShot(chapterEndSound);
    }

    #region Power-ups

    public void PlayPartySound()
    {
        RuntimeManager.PlayOneShot(powerUpPartySound);
    }

    public void PlayGoopSound()
    {
        RuntimeManager.PlayOneShot(powerUpGoopSound);
    }

    public void PlayFreezeAllSound()
    {
        RuntimeManager.PlayOneShot(powerUpFreezeAllSound);
    }

    public void PlayPoisonAllSound()
    {
        RuntimeManager.PlayOneShot(powerUpPoisonAllSound);
    }

    public void PlayCurseAllSound()
    {
        RuntimeManager.PlayOneShot(powerUpCurseAllSound);
    }

    #endregion Power-ups

    public void PlayCreditFrogCroakingSound()
    {
        // TODO: have a specific sound for when you click on a frog on the credits screen?
        RuntimeManager.PlayOneShot(cantBuyItemInShopSound);
    }

    #endregion
}
