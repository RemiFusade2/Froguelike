using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// This file is used to declare most Enum types

/// <summary>
/// Describe the level of debug logs you want from a class
/// </summary>
[System.Serializable]
public enum VerboseLevel
{
    NONE,
    MINIMAL,
    MAXIMAL
}

/// <summary>
/// WeaponEffect will be used as a mask, as a weapon could have multiple of these effects at the same time.
/// </summary>
[System.Serializable]
public enum WeaponEffect
{
    NONE = 0,
    VAMPIRE = 1,
    POISON = 2,
    FREEZE = 4,
    CURSE = 8
}

[System.Serializable]
public struct WeaponEffectData
{
    public WeaponEffect effect;
    public Color color;
}

[System.Serializable]
public enum RunItemType
{
    CONSUMABLE,
    STAT_BONUS,
    WEAPON
}

/// <summary>
/// An Enum for each weapon in the game.
/// Most weapon work with a "case by case" implementation. 
/// So this type will be used in the code to check how that specific weapon would behave, or to spawn the right weapon prefab.
/// </summary>
[System.Serializable]
public enum WeaponType
{
    CLASSIC, // target nearest, medium range, medium damage, no special
    QUICK, // target nearest, shorter, super fast, low damage, low cooldown. Upgrade to zero cooldown and triple tongue.
    WIDE, // target nearest, wider than usual, shorter, more damage
    ROTATING, // rotates around, medium range, low damage, upgrade to double
    VAMPIRE, // target nearest, medium range, low damage, heal when damage. RED
    POISON, // target nearest, low range, medium damage, poison damage during a delay after hit. GREEN
    FREEZE, // target nearest, high range, low damage, slow down enemies. BLUE
    CURSED, // target nearest, medium range, high damage, high speed, make enemies faster. PURPLE OR ORANGE
    RANDOM, // target random direction, high range, high damage, medium speed. Random effect and random color
    CAT // target nearest, SUPER WIDE, shorter, TONS OF DAMAGE
}

[System.Serializable]
public class FriendSprite
{
    public FriendType friendType;
    public Sprite sprite;
}

[System.Serializable]
public class HatSprite
{
    public HatType hatType;
    public Sprite sprite;
}

[System.Serializable]
public class SpawnProbability
{
    public SpawnFrequency frequency;
    public Vector2 probability;
}

/// <summary>
/// DataManager is a class used to give access to handy methods to get relevant data for the game (weapon types, items, enemy types, etc.)
/// </summary>
public class DataManager : MonoBehaviour
{
    // Singleton
    public static DataManager instance;

    [Header("Weapon effects colors")]
    public List<WeaponEffectData> allWeaponEffectsDataList;
    
    [Header("Currency symbol")]
    public string currencySymbol = "FC";

    [Header("Spawn probabilities")]
    public List<SpawnProbability> rocksSpawnProbabilities;
    public List<SpawnProbability> pondsSpawnProbabilities;
    [Space]
    public List<SpawnProbability> collectibleCurrencySpawnProbabilities;
    public List<SpawnProbability> collectibleHealthSpawnProbabilities;
    public List<SpawnProbability> collectibleLevelUpSpawnProbabilities;

    [Header("Sprites")]
    public Sprite collectibleDefaultSprite;

    [Header("Sprites - Hats")]
    public List<HatSprite> hatsSpritesList;

    [Header("Sprites - Friends")]
    public List<FriendSprite> friendsSpritesList;


    private Dictionary<WeaponEffect, Color> weaponEffectColorDico;

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
        InitializeData();
    }

    public Vector2 GetSpawnProbability(string spawnable, SpawnFrequency frequency)
    {
        Vector2 probability = Vector2.zero;
        SpawnProbability spawnProba = null;
        switch (spawnable)
        {
            case "rock":
                spawnProba = rocksSpawnProbabilities.FirstOrDefault(x => x.frequency == frequency);
                probability = (spawnProba != null) ? spawnProba.probability : Vector2.zero; 
                break;
            case "pond":
                spawnProba = pondsSpawnProbabilities.FirstOrDefault(x => x.frequency == frequency);
                probability = (spawnProba != null) ? spawnProba.probability : Vector2.zero;
                break;
            case "currency":
                spawnProba = collectibleCurrencySpawnProbabilities.FirstOrDefault(x => x.frequency == frequency);
                probability = (spawnProba != null) ? spawnProba.probability : Vector2.zero;
                break;
            case "health":
                spawnProba = collectibleHealthSpawnProbabilities.FirstOrDefault(x => x.frequency == frequency);
                probability = (spawnProba != null) ? spawnProba.probability : Vector2.zero;
                break;
            case "levelUp":
                spawnProba = collectibleLevelUpSpawnProbabilities.FirstOrDefault(x => x.frequency == frequency);
                probability = (spawnProba != null) ? spawnProba.probability : Vector2.zero;
                break;
            default:
                break;
        }

        return probability;
    }

    public Color GetColorForWeaponEffect(WeaponEffect effect)
    {
        return weaponEffectColorDico[effect];
    }

    private void InitializeData()
    {
        weaponEffectColorDico = new Dictionary<WeaponEffect, Color>();
        foreach (WeaponEffectData weaponEffectData in allWeaponEffectsDataList)
        {
            weaponEffectColorDico.Add(weaponEffectData.effect, weaponEffectData.color);
        }
    }

    public Sprite GetSpriteForFriend(FriendType friendType)
    {
        Sprite result = collectibleDefaultSprite;
        FriendSprite friendSprite = friendsSpritesList.FirstOrDefault(x => x.friendType == friendType);
        if (friendSprite != null)
        {
            result = friendSprite.sprite;
        }
        return result;
    }

    public Sprite GetSpriteForHat(HatType hatType)
    {
        Sprite result = collectibleDefaultSprite;
        HatSprite hatSprite = hatsSpritesList.FirstOrDefault(x => x.hatType == hatType);
        if (hatSprite != null)
        {
            result = hatSprite.sprite;
        }
        return result;
    }

    public Sprite GetSpriteForCollectible(FixedCollectible collectible)
    {
        Sprite resultSprite = collectibleDefaultSprite;
        switch (collectible.collectibleType)
        {
            case FixedCollectibleType.FRIEND:
                resultSprite = GetSpriteForFriend(collectible.collectibleFriendType);
                break;
            case FixedCollectibleType.HAT:
                resultSprite = GetSpriteForHat(collectible.collectibleHatType);
                break;
            case FixedCollectibleType.STATS_ITEM:
                RunStatItemData statItem = RunItemManager.instance.allRunStatsItems.FirstOrDefault(x => x.itemName.Equals(collectible.collectibleStatItemData.itemName));
                if (statItem != null)
                {
                    resultSprite = statItem.icon;
                }
                break;
            case FixedCollectibleType.WEAPON_ITEM:
                RunWeaponItemData weaponItem = RunItemManager.instance.allRunWeaponsItems.FirstOrDefault(x => x.itemName.Equals(collectible.collectibleWeaponItemData.itemName));
                if (weaponItem != null)
                {
                    resultSprite = weaponItem.icon;
                }
                break;
            default:
                break;
        }
        return resultSprite;
    }
}
