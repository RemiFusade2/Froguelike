using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// CharacterData is a scriptable object that describes a playable character.
/// It contains all information of this character such as: its name, description, unlock hint, sprite, animator value, the items is starts with, and starting stats
/// </summary>
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
    public bool startingUnlockState; // a character may be unlocked from the start
    public bool startingHiddenState; // a character may be hidden from the start

    [Header("Starting Items")]
    public List<ItemScriptableObject> startingItems;
}
