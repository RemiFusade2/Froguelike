using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FMODUnity;

public class SoundManager : MonoBehaviour
{
    public static SoundManager instance { get; private set; }

    [Header("Audio Sources")]
    public AudioSource buttonAudioSource;
    public AudioSource deathAudioSource;
    public AudioSource pageLongAudioSource;
    public AudioSource pageShortAudioSource;
    public AudioSource slideBookAudioSource;

    public AudioSource takeDamageAudioSource;
    public AudioSource healAudioSource;

    public AudioSource pickUpFroinsAudioSource;
    public AudioSource pickUpXPAudioSource;
    public AudioSource grabCollectibleAudioSource;
    public AudioSource levelUpAudioSource;

    public AudioSource buyItemInShopAudioSource;
    public AudioSource refundShopAudioSource;

    public AudioSource eatBountyAudioSource;
    public Transform eatBugAudioSourcesParent;

    public AudioSource powerUpAudioSource;

    [field: Header("Audio Clips")]
    [field: SerializeField] public EventReference buttonSound { get; private set; }
    [Range(0, 1)] public float buttonVolume = 1;
    [field: SerializeField] public EventReference deathSound { get; private set; }
    [Range(0, 1)] public float deathVolume = 1;
    [field: SerializeField] public EventReference pageLongSound { get; private set; }
    [Range(0, 1)] public float pageLongVolume = 1;
    [field: SerializeField] public EventReference pageShortSound { get; private set; }
    [Range(0, 1)] public float pageShortVolume = 1;
    [field: SerializeField] public EventReference slideBookSound { get; private set; }
    [Range(0, 1)] public float slideBookVolume = 1;

    public AudioClip takeDamageSound;
    [Range(0, 1)] public float takeDamageVolume = 1;
    [field: SerializeField] public EventReference healSound { get; private set; }
    [Range(0, 1)] public float healVolume = 1;

    [field: SerializeField] public EventReference pickUpFroinsSound { get; private set; }
    [Range(0, 1)] public float pickUpFroinsVolume = 1;
    [field: SerializeField] public EventReference pickUpXPSound { get; private set; }
    [Range(0, 1)] public float pickUpXPVolume = 1;
    [field: SerializeField] public EventReference grabCollectibleSound { get; private set; }
    [Range(0, 1)] public float grabCollectibleVolume = 1;
    [field: SerializeField] public EventReference levelUpSound { get; private set; }
    [Range(0, 1)] public float levelUpVolume = 1;


    [field: SerializeField] public EventReference buyItemInShopSound { get; private set; }
    [Range(0, 1)] public float buyItemInShopVolume = 1;
    [field: SerializeField] public EventReference refundShopSound { get; private set; }
    [Range(0, 1)] public float refundShopVolume = 1;

    public AudioClip rerollSound;
    [Range(0, 1)] public float rerollVolume = 1;
    public AudioClip skipSound;
    [Range(0, 1)] public float skipVolume = 1;

    [field: SerializeField] public EventReference eatBountySound { get; private set; }
    [Range(0, 1)] public float eatBBountyVolume = 1;
    [field: SerializeField] public EventReference eatBugSound { get; private set; }
    [Range(0, 1)] public float eatBugVolume = 1;

    [field: SerializeField] public EventReference powerUpFreezeAllSound { get; private set; }
    [Range(0, 1)] public float powerUpFreezeAllVolume = 1;


    private int eatBugLastUsedAudioSourceIndex;
    private List<AudioSource> eatBugAudioSourcesList;

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

    private void Start()
    {
        eatBugAudioSourcesList = new List<AudioSource>();
        foreach (Transform child in eatBugAudioSourcesParent)
        {
            eatBugAudioSourcesList.Add(child.GetComponent<AudioSource>());
        }
        eatBugLastUsedAudioSourceIndex = 0;
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

    private void PauseInGameSounds(bool paused)
    {
        if (paused)
        {
            takeDamageAudioSource.Pause();
        }
        else
        {
            takeDamageAudioSource.UnPause();
        }
    }
    public void PauseInGameSounds()
    {
        PauseInGameSounds(true);
    }
    public void UnpauseInGameSounds()
    {
        PauseInGameSounds(false);
    }

    private float ModifyVolume(float volume)
    {
        float newVolume = volume * volumeModifier;
        return newVolume;
    }

    #region Play loops

    private float currentLoopVolume;
    private float targetLoopVolume;

    private Coroutine volumeChangeCoroutine;

    private IEnumerator FadeVolume(AudioSource source, float newTargetValue, float maxVolumeDeltaPerFrame, System.Action<AudioSource> endOfFadeAction)
    {
        targetLoopVolume = newTargetValue;
        yield return new WaitForEndOfFrame();
        while (currentLoopVolume != targetLoopVolume)
        {
            yield return new WaitForEndOfFrame();

            float deltaVolume = (targetLoopVolume - currentLoopVolume);
            deltaVolume = Mathf.Sign(deltaVolume) * Mathf.Min(maxVolumeDeltaPerFrame, Mathf.Abs(deltaVolume));
            currentLoopVolume += deltaVolume;

            source.volume = ModifyVolume(currentLoopVolume);
        }
        if (endOfFadeAction != null)
        {
            endOfFadeAction(source);
        }
    }

    private void MakeAudioSourcePlay(AudioSource source)
    {
        source.Play();
    }

    private void MakeAudioSourceStop(AudioSource source)
    {
        source.Stop();
    }

    public void PlayTakeDamageLoopSound()
    {
        // Start playing
        if (!takeDamageAudioSource.isPlaying)
        {
            takeDamageAudioSource.clip = takeDamageSound;
            takeDamageAudioSource.Play();
        }

        // Fade in volume
        if (volumeChangeCoroutine != null)
        {
            StopCoroutine(volumeChangeCoroutine);
        }
        volumeChangeCoroutine = StartCoroutine(FadeVolume(takeDamageAudioSource, takeDamageVolume, 0.01f, null));
    }

    public void StopPlayingTakeDamageLoopSound()
    {
        // Fade out volume
        if (volumeChangeCoroutine != null)
        {
            StopCoroutine(volumeChangeCoroutine);
        }
        volumeChangeCoroutine = StartCoroutine(FadeVolume(takeDamageAudioSource, 0, 0.01f, MakeAudioSourceStop)); // Then stop playing
    }

    public void StopAllLoops()
    {
        currentLoopVolume = 0;
        takeDamageAudioSource.volume = ModifyVolume(currentLoopVolume);
        MakeAudioSourceStop(takeDamageAudioSource); // Stop playing
    }

    #endregion

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
        // TODO needs a parameter for the volume multiplier, I guess it is louder if it is more xp?
        // float volumeMultiplier = Mathf.Clamp((xpValue / 20.0f), 0.2f, 1.0f);
        // pickUpXPAudioSource.volume = ModifyVolume(Mathf.Clamp(pickUpXPVolume * volumeMultiplier, 0, 1));

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

    public void PlayRefundShopSound()
    {
        RuntimeManager.PlayOneShot(refundShopSound);
    }

    public void PlayEatBountySound()
    {
        RuntimeManager.PlayOneShot(eatBountySound);
    }

    public void PlayEatBugSound() // TODO
    {
        // TODO randomize pitch.
        /* float pitch = Random.Range(0.7f, 1.3f);
        eatBugLastUsedAudioSourceIndex = (eatBugLastUsedAudioSourceIndex + 1) % eatBugAudioSourcesList.Count;
        AudioSource eatBugAudioSource = eatBugAudioSourcesList[eatBugLastUsedAudioSourceIndex];
        eatBugAudioSource.pitch = pitch;
        eatBugAudioSource.volume = ModifyVolume(eatBugVolume); */

        RuntimeManager.PlayOneShot(eatBugSound);
    }

    /*
    public void PlayRerollSound()
    {
        audioSource.volume = ModifyVolume(rerollVolume);
        audioSource.PlayOneShot(rerollSound);
    }
    public void PlaySkipSound()
    {
        audioSource.volume = ModifyVolume(skipVolume);
        audioSource.PlayOneShot(skipSound);
    }*/

    public void PlayFreezeAllSound()
    {
        RuntimeManager.PlayOneShot(powerUpFreezeAllSound);
    }

    #endregion
}
