using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

/*
[System.Serializable]
public enum DemoLimitationType
{
    NONE,
    NUMBER_OF_RUNS,
    TIMER
}*/

[System.Serializable]
[Flags] public enum PowerUpType
{
    POWERUP_FREEZEALL = 1,
    POWERUP_POISONALL = 2,
    POWERUP_CURSEALL = 4,
    POWERUP_GODMODE = 8,
    POWERUP_FRIENDSFRENZY = 16,
    POWERUP_MEGAMAGNET = 32,
    POWERUP_LEVELUPBUGS = 64,
    POWERUP_LEVELDOWNBUGS = 128,
    POWERUP_TELEPORT = 256
}

public class BuildManager : MonoBehaviour
{
    // Singleton
    public static BuildManager instance;

    [Header("Settings - Version number")]
    public string versionNumber = "0.0.0";

    [Header("Settings - EA")]
    public bool showEADisclaimer = false;
    public PowerUpType availablePowerUpsInEA;

    [Header("Settings - Demo")]
    public bool demoBuild = false;
    [Space]
    public bool showDemoDisclaimer = false;
    /*public DemoLimitationType demoLimitationType;
    public float demoTimeLimit = 0; // In seconds
    public int demoRunCountLimit = 0;
    public bool demoSaveProgress = false;*/
    [Space]
    public PowerUpType availablePowerUpsInDemo;

    [Header("Settings - Cheats")]
    public bool cheatsAreEnabled = false;
    public bool everythingIsUnlocked = false;

    [Header("Settings - Hide missing sprites")]
    public bool thingsWithMissingSpritesAreHidden = false;


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

    public bool IsCollectibleAvailable(CollectibleType type)
    {
        bool result = true;
        PowerUpType availablePowerUpsMask = demoBuild ? availablePowerUpsInDemo : availablePowerUpsInEA;
        switch (type)
        {
            case CollectibleType.FROINS:
            case CollectibleType.HEALTH:
            case CollectibleType.XP_BONUS:
            case CollectibleType.LEVEL_UP:
                break;
            case CollectibleType.POWERUP_FREEZEALL:
                result = ((availablePowerUpsMask & PowerUpType.POWERUP_FREEZEALL) == PowerUpType.POWERUP_FREEZEALL);
                break;
            case CollectibleType.POWERUP_POISONALL:
                result = ((availablePowerUpsMask & PowerUpType.POWERUP_POISONALL) == PowerUpType.POWERUP_POISONALL);
                break;
            case CollectibleType.POWERUP_CURSEALL:
                result = ((availablePowerUpsMask & PowerUpType.POWERUP_CURSEALL) == PowerUpType.POWERUP_CURSEALL);
                break;
            case CollectibleType.POWERUP_GODMODE:
                result = ((availablePowerUpsMask & PowerUpType.POWERUP_GODMODE) == PowerUpType.POWERUP_GODMODE);
                break;
            case CollectibleType.POWERUP_FRIENDSFRENZY:
                result = ((availablePowerUpsMask & PowerUpType.POWERUP_FRIENDSFRENZY) == PowerUpType.POWERUP_FRIENDSFRENZY);
                break;
            case CollectibleType.POWERUP_MEGAMAGNET:
                result = ((availablePowerUpsMask & PowerUpType.POWERUP_MEGAMAGNET) == PowerUpType.POWERUP_MEGAMAGNET);
                break;
            case CollectibleType.POWERUP_LEVELUPBUGS:
                result = ((availablePowerUpsMask & PowerUpType.POWERUP_LEVELUPBUGS) == PowerUpType.POWERUP_LEVELUPBUGS);
                break;
            case CollectibleType.POWERUP_LEVELDOWNBUGS:
                result = ((availablePowerUpsMask & PowerUpType.POWERUP_LEVELDOWNBUGS) == PowerUpType.POWERUP_LEVELDOWNBUGS);
                break;
        }
        return result;
    }
}
