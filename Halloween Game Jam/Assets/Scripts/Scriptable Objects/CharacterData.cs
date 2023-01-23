using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
[CreateAssetMenu(fileName = "Character Data", menuName = "ScriptableObjects/Froguelike/Character Data", order = 1)]
public class CharacterData : ScriptableObject
{
    [Header("Lore Info")]
    public string characterName;
    public string characterDescription;
    [Space]
    public string unlockHint;

    [Header("Display Settings")]
    public Sprite characterSprite;
    public int characterAnimatorValue;

    [Header("Starting Stats")]
    public List<StatValue> startingStatsList; // all omitted stat will be set to default value

    [Header("Starting Weapon")]
    public ItemScriptableObject startingWeapon;
}
