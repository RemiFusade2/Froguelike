using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
[CreateAssetMenu(fileName = "Character Data", menuName = "ScriptableObjects/Froguelike/Character Data", order = 1)]
public class CharacterData : ScriptableObject
{
    public string characterName;
    public Sprite characterSprite;
    public string characterDescription;

    public string unlockHint;

    public int characterAnimatorValue;
    public float startingLandSpeed;
    public float startingSwimSpeed;
    public float startingMaxHealth;
    public float startingHealthRecovery;

    public float startingArmor;
    public int startingRevivals;

    public Froguelike_ItemScriptableObject startingWeapon;
}
