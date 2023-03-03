using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AchievementManager : MonoBehaviour
{
    public static AchievementManager instance;

    [Header("Settings")]
    public VerboseLevel logsVerboseLevel = VerboseLevel.NONE;

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
    
    public List<string> CheckForUnlockingCharacters()
    {
        List<string> unlockedCharacterNames = new List<string>();

        bool gameIsWon = RunManager.instance.completedChaptersList.Count >= 6;
        
        FrogCharacterController player = GameManager.instance.player;
        /*
        // After winning one game
        if (gameIsWon)
        {
            string characterName = "Toad";
            if (CharacterManager.instance.UnlockCharacter(characterName))
            {
                unlockedCharacterNames.Add(characterName);
            }
        }*/
        /*
        // After dying 15 times
        if (GameManager.instance.gameData.deathCount >= 15)
        {
            string characterName = "Ghost";
            if (CharacterManager.instance.UnlockCharacter(characterName))
            {
                unlockedCharacterNames.Add(characterName);
            }
        }*/

        if (gameIsWon)
        {/*
            bool hasMaxedOutCurse = false;
            bool hasMaxedOutCursedTongue = false;
            bool hasMaxedOutPoisonousTongue = false;
            foreach (RunItemInfo item in RunManager.instance.ownedItems)
            {
                if (item is RunStatItemInfo)
                {
                    RunStatItemInfo statItem = (item as RunStatItemInfo);
                    if (statItem.itemData.itemName.Equals("Curse") && statItem.level.Equals(statItem.itemData.statBoostLevels.Count))
                    {
                        hasMaxedOutCurse = true;
                    }
                }
            }

            List<RunWeaponInfo> ownedWeapons = RunManager.instance.GetOwnedWeapons();
            foreach (RunWeaponInfo weapon in ownedWeapons)
            {
                if (weapon.weaponItemData.itemName.Equals("Cursed Tongue") && weapon.level.Equals(weapon.weaponItemData.weaponBoostLevels.Count))
                {
                    hasMaxedOutCursedTongue = true;
                }
                if (weapon.weaponItemData.itemName.Equals("Poisonous Tongue") && weapon.level.Equals(weapon.weaponItemData.weaponBoostLevels.Count))
                {
                    hasMaxedOutPoisonousTongue = true;
                }
            }

            // After winning a game with a maxed out poisonous tongue
            if (hasMaxedOutPoisonousTongue)
            {
                string characterName = "Ribbit";
                if (CharacterManager.instance.UnlockCharacter(characterName))
                {
                    unlockedCharacterNames.Add(characterName);
                }
            }

            // After winning a game with all 3 hats
            if (player.HasHat(HatType.FANCY_HAT) && player.HasHat(HatType.FASHION_HAT) && player.HasHat(HatType.SUN_HAT))
            {
                string characterName = "Kermit";
                if (CharacterManager.instance.UnlockCharacter(characterName))
                {
                    unlockedCharacterNames.Add(characterName);
                }
            }

            // After winning a game with maxed out curse and maxed out cursed tongue
            if (hasMaxedOutCurse && hasMaxedOutCursedTongue)
            {
                string characterName = "Thomas";
                if (CharacterManager.instance.UnlockCharacter(characterName))
                {
                    unlockedCharacterNames.Add(characterName);
                }
            }*/

            // After winning a game with all 4 friends
            if (player.HasActiveFriend(FriendType.FROG) && player.HasActiveFriend(FriendType.TOAD) && player.HasActiveFriend(FriendType.GHOST) && player.HasActiveFriend(FriendType.POISONOUS))
            {
                string characterName = "Stanley";
                if (CharacterManager.instance.UnlockCharacter(characterName))
                {
                    unlockedCharacterNames.Add(characterName);
                }
            }
        }

        if (logsVerboseLevel == VerboseLevel.MAXIMAL)
        {
            string unlockLog = "";
            if (unlockedCharacterNames.Count == 0)
            {
                unlockLog = "Achievements - No character unlocked";
            }
            else
            {
                unlockLog = "Achievements - ";
                foreach (string name in unlockedCharacterNames)
                {
                    unlockLog += name + " has been unlocked; ";
                }
            }
            Debug.Log(unlockLog);
        }

        return unlockedCharacterNames;
    }
}
