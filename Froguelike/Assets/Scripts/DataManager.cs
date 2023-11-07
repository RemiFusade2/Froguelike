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
public class FriendInfo
{
    public FriendType friendType;

    public GameObject prefab;

    public Sprite sprite;
    public int style; // for animator

    public Vector2 springDistanceMinMax;
    public Vector2 springDampingMinMax;
    public Vector2 springFrequencyMinMax;
}

[System.Serializable]
public class HatInfo
{
    public HatType hatType;
    public Sprite hatSprite;
    public float hatHeight;
}

[System.Serializable]
public class SpawnProbability
{
    public SpawnFrequency frequency;
    public Vector2 probability;
}

[System.Serializable]
public class CharacterStatNameAndRelevantData
{
    public CharacterStat stat;
    public string shortName;
    public string longName;
    public string unit;
    public bool usePercent;
    public Sprite icon;
}

[System.Serializable]
public class WeaponStatNameAndRelevantData
{
    public WeaponStat stat;
    public string shortName;
    public string longName;
    public string unit;
    public bool usePercent;
}

[System.Serializable]
public class ChapterIconData
{
    public Sprite iconSprite;
    public string tooltip;
}

/// <summary>
/// DataManager is a class used to give access to handy methods to get relevant data for the game (weapon types, items, enemy types, etc.)
/// </summary>
public class DataManager : MonoBehaviour
{
    // Singleton
    public static DataManager instance;

    [Header("Values")]
    public float defaultMaxHP = 100;
    public float defaultHealthRecovery = 0;
    public float defaultWalkSpeed = 6;
    public float defaultSwimSpeed = 4;
    public float defaultMagnetRange = 3.0f;
    public int defaultStatItemSlotCount = 4;
    public int defaultWeaponSlotCount = 4;

    [Header("Weapon effects colors")]
    public List<WeaponEffectData> allWeaponEffectsDataList;

    [Header("Stats info")]
    public List<CharacterStatNameAndRelevantData> characterStatsDataList;
    public List<WeaponStatNameAndRelevantData> weaponStatsDataList;

    [Header("Currency symbol")]
    public string currencySymbol = "FC";

    [Header("Spawn probabilities")]
    public List<SpawnProbability> rocksSpawnProbabilities;
    public List<SpawnProbability> pondsSpawnProbabilities;
    [Space]
    public List<SpawnProbability> collectibleCurrencySpawnProbabilities;
    public List<SpawnProbability> collectibleHealthSpawnProbabilities;
    public List<SpawnProbability> collectibleLevelUpSpawnProbabilities;
    [Space]
    public List<SpawnProbability> collectiblePowerUpsSpawnProbabilities;

    [Header("Power ups")]
    public float powerUpFreezeDuration = 10;
    public float powerUpPoisonDuration = 10;
    public float powerUpCurseDuration = 10;

    [Header("Sprites")]
    public Sprite collectibleDefaultSprite;
    public Sprite achievementLockedDefaultSprite;
    public Sprite achievementUnlockedDefaultSprite;

    [Header("Hats")]
    public List<HatInfo> hatInfoList;

    [Header("Friends")]
    public List<FriendInfo> friendInfoList;

    [Header("Chapter Icons")]
    public List<ChapterIconData> chapterIconsList;


    private Dictionary<WeaponEffect, Color> weaponEffectColorDico;
    private Dictionary<HatType, HatInfo> hatsDictionary;
    private Dictionary<FriendType, FriendInfo> friendsDictionary;
    private Dictionary<Sprite, ChapterIconData> chapterIconsDictionary;

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

    public float GetDefaultValueForStat(CharacterStat stat)
    {
        float value = 0;
        switch (stat)
        {
            case CharacterStat.MAX_HEALTH:
                value = defaultMaxHP;
                break;
            case CharacterStat.HEALTH_RECOVERY:
                value = defaultHealthRecovery;
                break;
            case CharacterStat.WALK_SPEED_BOOST:
                value = defaultWalkSpeed;
                break;
            case CharacterStat.SWIM_SPEED_BOOST:
                value = defaultSwimSpeed;
                break;
            case CharacterStat.MAGNET_RANGE_BOOST:
                value = defaultMagnetRange;
                break;
            case CharacterStat.ITEM_SLOT:
                value = defaultStatItemSlotCount;
                break;
            case CharacterStat.WEAPON_SLOT:
                value = defaultWeaponSlotCount;
                break;
        }
        return value;
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
            case "powerUp":
                spawnProba = collectiblePowerUpsSpawnProbabilities.FirstOrDefault(x => x.frequency == frequency);
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
        hatsDictionary = new Dictionary<HatType, HatInfo>();
        foreach (HatInfo hatInfo in hatInfoList)
        {
            hatsDictionary.Add(hatInfo.hatType, hatInfo);
        }
        friendsDictionary = new Dictionary<FriendType, FriendInfo>();
        foreach (FriendInfo friendInfo in friendInfoList)
        {
            friendsDictionary.Add(friendInfo.friendType, friendInfo);
        }
        chapterIconsDictionary = new Dictionary<Sprite, ChapterIconData>();
        foreach (ChapterIconData iconData in chapterIconsList)
        {
            chapterIconsDictionary.Add(iconData.iconSprite, iconData);
        }
    }

    public string GetTooltipForChapterIcon(Sprite chapterIconSprite)
    {
        ChapterIconData info = null;
        if (chapterIconsDictionary.ContainsKey(chapterIconSprite))
        {
            info = chapterIconsDictionary[chapterIconSprite];
        }
        return info.tooltip;
    }

    public FriendInfo GetInfoForFriend(FriendType friendType)
    {
        FriendInfo info = null;
        if (friendsDictionary.ContainsKey(friendType))
        {
            info = friendsDictionary[friendType];
        }
        return info;
    }

    public Sprite GetSpriteForFriend(FriendType friendType)
    {
        FriendInfo info = GetInfoForFriend(friendType);
        Sprite result = collectibleDefaultSprite;
        if (info != null)
        {
            result = info.sprite;
        }
        return result;
    }

    public HatInfo GetInfoForHat(HatType hatType)
    {
        HatInfo info = null;
        if (hatsDictionary.ContainsKey(hatType))
        {
            info = hatsDictionary[hatType];
        }
        return info;
    }


    public Sprite GetSpriteForHat(HatType hatType)
    {
        HatInfo info = GetInfoForHat(hatType);
        Sprite result = collectibleDefaultSprite;
        if (info != null)
        {
            result = info.hatSprite;
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
                RunStatItemData statItem = RunItemManager.instance.allRunStatsItemsScriptableObjects.FirstOrDefault(x => x.itemName.Equals(collectible.collectibleStatItemData.itemName));
                if (statItem != null && statItem.icon != null)
                {
                    resultSprite = statItem.icon;
                }
                break;
            case FixedCollectibleType.WEAPON_ITEM:
                RunWeaponItemData weaponItem = RunItemManager.instance.allRunWeaponsItemsScriptableObjects.FirstOrDefault(x => x.itemName.Equals(collectible.collectibleWeaponItemData.itemName));
                if (weaponItem != null && weaponItem.icon != null)
                {
                    resultSprite = weaponItem.icon;
                }
                break;
            default:
                break;
        }
        return resultSprite;
    }

    public bool TryGetStatData(WeaponStat stat, out string shortName, out string longName, out string unit, out bool usePercent)
    {
        bool statDataExists = false;
        WeaponStatNameAndRelevantData statData = weaponStatsDataList.FirstOrDefault(x => x.stat.Equals(stat));
        shortName = ""; longName = ""; unit = ""; usePercent = false;
        if (statData != null)
        {
            statDataExists = true;
            shortName = statData.shortName;
            longName = statData.longName;
            unit = statData.unit;
            usePercent = statData.usePercent;
        }
        return statDataExists;
    }

    public bool TryGetStatData(CharacterStat stat, out string shortName, out string longName, out string unit, out bool usePercent, out Sprite icon)
    {
        bool statDataExists = false;
        CharacterStatNameAndRelevantData statData = characterStatsDataList.FirstOrDefault(x => x.stat.Equals(stat));
        shortName = ""; longName = ""; unit = ""; usePercent = false; icon = null;
        if (statData != null)
        {
            statDataExists = true;
            icon = statData.icon;
            shortName = statData.shortName;
            longName = statData.longName;
            unit = statData.unit;
            usePercent = statData.usePercent;
        }
        return statDataExists;
    }

    public Sprite GetStatSprite(CharacterStat stat)
    {
        return characterStatsDataList.FirstOrDefault(x => x.stat.Equals(stat)).icon;
    }
}
