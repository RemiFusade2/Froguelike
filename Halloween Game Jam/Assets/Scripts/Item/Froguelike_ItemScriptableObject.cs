using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Froguelike_ItemLevel
{
    public int level;
    public string description;

    [Header("Character Boosts")]
    public float walkSpeedBoost; // move speed on land
    public float swimSpeedBoost; // move speed on water

    public float maxHealthBoost; // max health
    public float healthRecoveryBoost; // pace at which you recover health
    public float armorBoost; // armor (take less damage from attacks)

    public float experienceBoost; // xp bonus when picking up flies
    public float goldBoost; // gold bonus when picking up gold

    public float curseBoost; // enemies get stronger and there are more of them

    public float revivalBoost; // 1 revival = 1 more chance to play when you die
    public float rerollBoost; // 1 reroll = reroll the selection of items after leveling up, or when choosing next chapter

    public float attackDamageBoost; // damage boost to all attacks
    public float attackSpeedBoost; // attack speed boost to all attacks
    public float attackCooldownBoost; // cooldown boost to all attacks
    public float attackRangeBoost; // range boost to all attacks
    public float attackMaxFliesBoost; // max flies boost to all attacks

    [Header("Weapon Boosts")]
    public float weaponDamageBoost;
    public float weaponSpeedBoost;
    public float weaponCooldownBoost;
    public float weaponRangeBoost;
    public float weaponMaxFliesBoost;

    public int weaponExtraWeapon;
}

[System.Serializable]
[CreateAssetMenu(fileName = "Item Data", menuName = "ScriptableObjects/Froguelike/Item Data", order = 1)]
public class Froguelike_ItemScriptableObject : ScriptableObject
{
    public string itemName;

    public List<Froguelike_ItemLevel> levels;

    public bool isWeapon;
    public GameObject weaponPrefab;

    public Sprite icon;

    public override bool Equals(object other)
    {
        if (other is Froguelike_ItemScriptableObject)
        {
            return itemName.Equals((other as Froguelike_ItemScriptableObject).itemName);
        }
        return base.Equals(other);
    }
}
