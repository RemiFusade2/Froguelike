using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// This file is used to declare most Enum types

/// <summary>
/// Describe the level of debug logs you want from a class
/// </summary>
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
}
