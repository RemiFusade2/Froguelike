using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SoundManager : MonoBehaviour
{
    public static SoundManager instance;

    [Header("Audio Sources")]
    public AudioSource buttonAudioSource;
    public AudioSource deathAudioSource;
    public AudioSource pageLongAudioSource;
    public AudioSource pageShortAudioSource;
    public AudioSource slideBookAudioSource;

    public AudioSource takeDamageAudioSource;
    public AudioSource healAudioSource;

    public AudioSource pickUpFroinsAudioSource;
    public AudioSource grabCollectibleAudioSource;
    public AudioSource levelUpAudioSource;

    public AudioSource buyItemInShopAudioSource;
    public AudioSource refundShopAudioSource;

    public Transform eatBugAudioSourcesParent;

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
    
    public AudioClip takeDamageSound;
    [Range(0, 1)] public float takeDamageVolume = 1;
    public AudioClip healSound;
    [Range(0, 1)] public float healVolume = 1;

    public AudioClip pickUpFroinsSound;
    [Range(0, 1)] public float pickUpFroinsVolume = 1;
    public AudioClip grabCollectibleSound;
    [Range(0, 1)] public float grabCollectibleVolume = 1;
    public AudioClip levelUpSound;
    [Range(0, 1)] public float levelUpVolume = 1;

    /*
    public AudioClip aTongueEatsABugSound;
    [Range(0, 1)] public float aTongueEatsABugVolume = 1;*/

    public AudioClip buyItemInShopSound;
    [Range(0, 1)] public float buyItemInShopVolume = 1;
    public AudioClip refundShopSound;
    [Range(0, 1)] public float refundShopVolume = 1;

    public AudioClip rerollSound;
    [Range(0, 1)] public float rerollVolume = 1;
    public AudioClip skipSound;
    [Range(0, 1)] public float skipVolume = 1;

    public AudioClip eatBugSound;
    [Range(0, 1)] public float eatBugVolume = 1;

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

    public void PlayButtonSound(Button button)
    {
        if (button.interactable)
        {
            buttonAudioSource.volume = ModifyVolume(buttonVolume);
            buttonAudioSource.PlayOneShot(buttonSound);
        }
    }

    public void PlayButtonSound(Toggle toggle)
    {
        if (toggle.interactable)
        {
            buttonAudioSource.volume = ModifyVolume(buttonVolume);
            buttonAudioSource.PlayOneShot(buttonSound);
        }
    }

    public void PlayButtonSound(TMP_Dropdown dropdown)
    {
        if (dropdown.interactable)
        {
            buttonAudioSource.volume = ModifyVolume(buttonVolume);
            buttonAudioSource.PlayOneShot(buttonSound);
        }
    }

    public void PlayLongPageSound()
    {
        pageLongAudioSource.volume = ModifyVolume(pageLongVolume);
        pageLongAudioSource.PlayOneShot(pageLongSound);
    }

    public void PlayDeathSound()
    {
        deathAudioSource.volume = ModifyVolume(deathVolume);
        deathAudioSource.PlayOneShot(deathSound);
    }

    public void PlayShortPageSound()
    {
        pageShortAudioSource.volume = ModifyVolume(pageShortVolume);
        pageShortAudioSource.PlayOneShot(pageShortSound);
    }

    public void PlaySlideBookSound()
    {
        slideBookAudioSource.volume = ModifyVolume(slideBookVolume);
        slideBookAudioSource.PlayOneShot(slideBookSound);
    }

    public void PlayHealSound()
    {
        healAudioSource.volume = ModifyVolume(healVolume);
        if (healSound != null)
        {
            healAudioSource.PlayOneShot(healSound);
        }
    }

    public void PlayPickUpFroinsSound()
    {
        pickUpFroinsAudioSource.volume = ModifyVolume(pickUpFroinsVolume);
        pickUpFroinsAudioSource.PlayOneShot(pickUpFroinsSound);
    }
    public void PlayPickUpCollectibleSound()
    {
        grabCollectibleAudioSource.volume = ModifyVolume(grabCollectibleVolume);
        grabCollectibleAudioSource.PlayOneShot(grabCollectibleSound);
    }
    public void PlayLevelUpSound()
    {
        levelUpAudioSource.volume = ModifyVolume(levelUpVolume);
        levelUpAudioSource.PlayOneShot(levelUpSound);
    }

    public void PlayBuyItemInShopSound()
    {
        buyItemInShopAudioSource.volume = ModifyVolume(buyItemInShopVolume);
        buyItemInShopAudioSource.PlayOneShot(buyItemInShopSound);
    }
    public void PlayRefundShopSound()
    {
        refundShopAudioSource.volume = ModifyVolume(refundShopVolume);
        refundShopAudioSource.PlayOneShot(refundShopSound);
    }

    public void PlayEatBugSound()
    {
        float pitch = Random.Range(0.7f, 1.3f);
        eatBugLastUsedAudioSourceIndex = (eatBugLastUsedAudioSourceIndex + 1) % eatBugAudioSourcesList.Count;
        AudioSource eatBugAudioSource = eatBugAudioSourcesList[eatBugLastUsedAudioSourceIndex];
        eatBugAudioSource.pitch = pitch;
        eatBugAudioSource.volume = ModifyVolume(eatBugVolume);
        eatBugAudioSource.PlayOneShot(eatBugSound);
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

    #endregion
}
