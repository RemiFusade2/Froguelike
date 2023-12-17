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
/// TongueEffect will be used as a mask, as a tongue could have multiple of these effects at the same time.
/// </summary>
[System.Serializable]
public enum TongueEffect
{
    NONE = 0,
    VAMPIRE = 1,
    POISON = 2,
    FREEZE = 4,
    CURSE = 8
}

[System.Serializable]
public struct TongueEffectData
{
    public TongueEffect effect;
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
/// An Enum for each tongue in the game.
/// Most tongue work with a "case by case" implementation. 
/// So this type will be used in the code to check how that specific tongue would behave.
/// </summary>
[System.Serializable]
public enum TongueType
{
    CLASSIC, // target nearest, medium range, medium damage, no special
    QUICK, // target nearest, shorter, super fast, low damage, low cooldown. Upgrade to zero cooldown.
    WIDE, // target nearest and continue after first hit, wider than usual, more damage
    ROTATING, // rotates around, medium range, low damage, upgrade to have multiple
    VAMPIRE, // target nearest, medium range, low damage, heal when damage. RED
    POISON, // target nearest, low range, medium damage, poison damage during a delay after hit. GREEN
    FREEZE, // target nearest, high range, low damage, stop enemies. BLUE
    CURSED, // target nearest, medium range, high damage, high speed, make enemies faster. PURPLE OR ORANGE
    RANDOM, // target random direction, high range, high damage, medium speed. Random effects and random colors
    CAT // target in front of frog, SUPER WIDE, shorter, TONS OF DAMAGE
}

/// <summary>
/// An Enum for each magnet collectible in the game
/// </summary>
[System.Serializable]
public enum CollectibleType
{
    FROINS,
    XP_BONUS,
    LEVEL_UP,
    HEALTH,

    POWERUP_FREEZEALL,
    POWERUP_POISONALL,
    POWERUP_CURSEALL,
    POWERUP_GODMODE,
    POWERUP_FRIENDSFRENZY,
    POWERUP_MEGAMAGNET,
    POWERUP_LEVELUPBUGS,
    POWERUP_LEVELDOWNBUGS,
    POWERUP_TELEPORT
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
    public TongueStat stat;
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

[System.Serializable]
public class CollectibleInfo
{
    public string Name;
    public CollectibleType Type;
    public List<Sprite> Icons;
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
    public float baseCurrencyProbabilitySpawnFromBugs = 0.1f; // 10% chance to drop 1 froin when eating a bug
    public float baseCurrencyValueSpawnFromBugs = 1;
    [Space]
    public float capturedCollectiblesSpeed = 12;

    [Header("Weapon effects colors")]
    public List<TongueEffectData> allWeaponEffectsDataList;

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
    public float powerUpGodModeDuration = 10;
    public int powerUpFriendsFrenzyAmount = 100;
    public float powerUpFriendsFrenzyLifespan = 15;
    public float powerUpFriendsFrenzySpawnDistanceFromPlayer = 30;

    [Header("Sprites")]
    public Sprite collectibleDefaultSprite;
    public Sprite achievementLockedDefaultSprite;
    public Sprite achievementUnlockedDefaultSprite;

    [Header("Fixed collectibles UI Strings")]
    public string defaultFoundCollectibleItemTitle = "You found";
    public string defaultFoundCollectibleFriendTitle = "You met";
    [Space]
    public string defaultFoundCollectibleItemName = "A thing";
    public string defaultFoundCollectibleFriendName = "A smol frend";
    [Space]
    public string defaultFoundCollectibleAcceptStr = "Accept";
    public string defaultFoundCollectibleRefuseStr = "Refuse";

    [Header("Magnet collectibles Sprites")]
    public List<CollectibleInfo> magnetCollectiblesIconsList;

    [Header("Hats")]
    public List<HatInfo> hatInfoList;

    [Header("Friends")]
    public List<FriendData> friendsDataList;

    [Header("Chapter Icons")]
    public List<ChapterIconData> chapterIconsList;




    private Vector3 farAwayPosition;

    private Dictionary<TongueEffect, Color> weaponEffectColorDico;
    private Dictionary<HatType, HatInfo> hatsDictionary;
    private Dictionary<FriendType, FriendData> friendsDictionary;
    private Dictionary<Sprite, ChapterIconData> chapterIconsDictionary;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            farAwayPosition = new Vector3(50000, 50000, 50000); // this should be far enough and always out of camera frustum
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

    public Color GetColorForWeaponEffect(TongueEffect effect)
    {
        return weaponEffectColorDico[effect];
    }

    private void InitializeData()
    {
        weaponEffectColorDico = new Dictionary<TongueEffect, Color>();
        foreach (TongueEffectData weaponEffectData in allWeaponEffectsDataList)
        {
            weaponEffectColorDico.Add(weaponEffectData.effect, weaponEffectData.color);
        }
        hatsDictionary = new Dictionary<HatType, HatInfo>();
        foreach (HatInfo hatInfo in hatInfoList)
        {
            hatsDictionary.Add(hatInfo.hatType, hatInfo);
        }
        friendsDictionary = new Dictionary<FriendType, FriendData>();
        foreach (FriendData friendInfo in friendsDataList)
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

    public FriendData GetDataForFriend(FriendType friendType)
    {
        FriendData data = null;
        if (friendsDictionary.ContainsKey(friendType))
        {
            data = friendsDictionary[friendType];
        }
        return data;
    }

    public Sprite GetSpriteForFriend(FriendType friendType)
    {
        FriendData data = GetDataForFriend(friendType);
        Sprite result = collectibleDefaultSprite;
        if (data != null)
        {
            result = data.sprite;
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

    public bool TryGetStatData(TongueStat stat, out string shortName, out string longName, out string unit, out bool usePercent)
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

    public CollectibleInfo GetCollectibleInfoFromType(CollectibleType collectibleType)
    {
        return magnetCollectiblesIconsList.Where(x => x.Type.Equals(collectibleType)).FirstOrDefault();
    }

    public CollectibleInfo GetCollectibleInfoFromName(string collectibleName)
    {
        return magnetCollectiblesIconsList.Where(x => x.Name.Equals(collectibleName)).FirstOrDefault();
    }

    public string GetDefaultFoundCollectibleTitle(FixedCollectible collectibleInfo)
    {
        return (collectibleInfo.collectibleType == FixedCollectibleType.FRIEND) ? defaultFoundCollectibleFriendTitle : defaultFoundCollectibleItemTitle;
    }
    public string GetDefaultFoundCollectibleName(FixedCollectible collectibleInfo)
    {
        return (collectibleInfo.collectibleType == FixedCollectibleType.FRIEND) ? defaultFoundCollectibleFriendName : defaultFoundCollectibleItemName;
    }

    public Vector3 GetFarAwayPosition()
    {
        return farAwayPosition;
    }
}
