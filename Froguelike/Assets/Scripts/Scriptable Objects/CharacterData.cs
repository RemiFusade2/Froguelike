using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// CharacterData is a scriptable object that describes a playable character.
/// It contains all information of this character such as: 
/// - its name
/// - a description
/// - a hint on how to unlock it
/// - its sprite
/// - its animator value (used in the character controller animator)
/// - the items is starts with (starting weapon mostly)
/// - and its starting stats at the start of the game (they may evolve, and the new ones would be saved in the save file)
/// </summary>
[System.Serializable]
[CreateAssetMenu(fileName = "Character Data", menuName = "ScriptableObjects/Froguelike/Character Data", order = 1)]
public class CharacterData : ScriptableObject
{
    [Header("Lore Info")]
    [Tooltip("The name of this character")]
    public string characterName;
    [Tooltip("A short description of this character")]
    public string characterDescription;
    [Space]
    [Tooltip("A hint on how to unlock this character")]
    public string unlockHint;

    [Header("Display Settings")]
    [Tooltip("The sprite used to represent this character in menus")]
    public Sprite characterSprite;
    [Tooltip("The value (integer) used in the character controller animator")]
    public int characterAnimatorValue;

    [Header("Starting Stats")]
    [Tooltip("The stats of this character at the start of the game. Any omitted stat will be set to default value")]
    public List<StatValue> startingStatsList;
    [Space]
    [Tooltip("Is the character unlocked from the start? May be unlocked from an achievement")]
    public bool startingUnlockState;
    [Tooltip("Is the character hidden from the start? May be unlocked from an achievement")]
    public bool startingHiddenState;

    [Header("Starting Items")]
    [Tooltip("The items this character has at the start of a Run. Don't forget to give them at least one weapon.")]
    public List<RunItemData> startingItems;
}
