using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ItemLevel
{
    public int level;
    public string description;

    [Header("Character Heal & Score")]
    public float recoverHealth; // instantly heal x HP
    public int extraScore; // a few more points

    [Header("Character Boosts")]
    public float walkSpeedBoost; // move speed on land
    public float swimSpeedBoost; // move speed on water
    [Space]
    public float maxHealthBoost; // max health
    public float healthRecoveryBoost; // pace at which you recover health
    public float armorBoost; // armor (take less damage from attacks)
    [Space]
    public float experienceBoost; // xp bonus when picking up flies
    public float goldBoost; // gold bonus when picking up gold
    [Space]
    public float curseBoost; // enemies get stronger and there are more of them
    [Space]
    public int revivalBoost; // 1 revival = 1 more chance to play when you die
    public float rerollBoost; // 1 reroll = reroll the selection of items after leveling up, or when choosing next chapter
    [Space]
    public float attackDamageBoost; // damage boost to all attacks
    public float attackSpeedBoost; // attack speed boost to all attacks
    public float attackCooldownBoost; // cooldown boost to all attacks
    public float attackRangeBoost; // range boost to all attacks
    public float attackMaxFliesBoost; // max flies boost to all attacks
    [Space]
    public float attackSpecialStrengthBoost; // special attack such as poison or freeze are more efficient
    public float attackSpecialDurationBoost; // special attack such as poison or freeze last longer

    [Header("Weapon Boosts")]
    public float weaponDamageBoost;
    public float weaponSpeedBoost;
    public float weaponCooldownBoost;
    public float weaponRangeBoost;
    public float weaponMaxFliesBoost;
    [Space]
    public float weaponHealthAbsorbRatioBoost;
    public float weaponHealthAbsorbMaxBoost;
    [Space]
    public float weaponPoisonDamageBoost;
    public float weaponPoisonDurationBoost;
    [Space]
    public float weaponChangeSpeedFactorBoost;
    public float weaponChangeSpeedDurationBoost;
    [Space]
    public int weaponExtraWeapon;
}

[System.Serializable]
public class WeaponData
{
    [Header("Prefab reference")]
    public GameObject weaponPrefab;

    [Header("Base settings")]
    public float startWidth;
    public WeaponType weaponType;
    [Space]
    public float startCooldown;
    public float startDamage;
    public float startAttackSpeed;
    public float startMaxFlies;
    public float startRange;

    [Header("Special settings")]
    public float startHealthAbsorbRatio;
    public float startHealthAbsorbMax;
    [Space]
    public float startPoisonDamage;
    public float startPoisonDuration;
    [Space]
    public float startChangeSpeedFactor;
    public float startChangeSpeedDuration;
}

[System.Serializable]
[CreateAssetMenu(fileName = "Item Data", menuName = "ScriptableObjects/Froguelike/Item Data", order = 1)]
public class ItemScriptableObject : ScriptableObject
{
    [Header("Base settings")]
    public string itemName;
    public Sprite icon;

    [Header("Levels")]
    public List<ItemLevel> levels;

    [Header("Weapon settings")]
    public bool isWeapon;
    public WeaponData weaponData;

    public override bool Equals(object other)
    {
        if (other is ItemScriptableObject)
        {
            return itemName.Equals((other as ItemScriptableObject).itemName);
        }
        return base.Equals(other);
    }

    public override int GetHashCode()
    {
        return itemName.GetHashCode();
    }
}
