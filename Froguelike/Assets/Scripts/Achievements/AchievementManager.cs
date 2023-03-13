using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


/// <summary>
/// Achievement describes an achievement in its current state.
/// 
/// It has a reference to ChapterData, the scriptable object that describes the chapter. This is not serialized with the rest.
/// It keeps the chapterID there for serialization. When saving/loading this chapter from a save file, the ID will be used to retrieve the right chapter in the program.
/// The information that can change at runtime are:
/// - unlocked, is the status of the chapter. This value can change when the chapter is unlocked through an achievement.
/// - attemptCountByCharacters is the amount of attempts from every character
/// - completionCountByCharacters is the amount of completions from every character
/// </summary>
[System.Serializable]
public class Achievement
{
    [System.NonSerialized]
    public AchievementData achievementData;

    // Defined at runtime, using AchievementData
    [HideInInspector]
    public string achievementID;

    public bool unlocked;
    
    public override bool Equals(object obj)
    {
        bool equal = false;
        if (obj is Achievement)
        {
            equal = this.achievementID.Equals((obj as Achievement).achievementID);
        }
        return equal;
    }

    public override int GetHashCode()
    {
        return achievementID.GetHashCode();
    }
}

/// <summary>
/// AchievementsSaveData contains all information that must be saved about the achievements.
/// - achievementsList is the list of achievements in their current state
/// </summary>
[System.Serializable]
public class AchievementsSaveData : SaveData
{
    public List<Achievement> achievementsList;

    public AchievementsSaveData()
    {
        Reset();
    }

    public override void Reset()
    {
        base.Reset();
        achievementsList = new List<Achievement>();
    }
}

public class AchievementManager : MonoBehaviour
{
    public static AchievementManager instance;

    [Header("Settings - logs")]
    public VerboseLevel logsVerboseLevel = VerboseLevel.NONE;

    [Header("Data")]
    public List<AchievementData> achievementsScriptableObjectsList;

    [Header("Runtime")]
    public AchievementsSaveData achievementsData; // Load from save file

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

    #region Load Data

    /// <summary>
    /// Update the achievements data using a AchievementsSaveData object, that was probably loaded from a file by the SaveDataManager.
    /// </summary>
    /// <param name="saveData"></param>
    public void SetAchievementsData(AchievementsSaveData saveData)
    {
        foreach (Achievement achievement in achievementsData.achievementsList)
        {
            Achievement achievementFromSave = saveData.achievementsList.FirstOrDefault(x => x.achievementID.Equals(achievement.achievementID));
            if (achievementFromSave != null)
            {
                achievement.unlocked = achievementFromSave.unlocked;
            }
        }
    }

    #endregion

    public void ResetAchievements()
    {
        achievementsData.achievementsList.Clear();
        foreach (AchievementData achievementData in achievementsScriptableObjectsList)
        {
            Achievement newAchievement = new Achievement()
            {
                achievementData = achievementData,
                unlocked = false,
                achievementID = achievementData.achievementID
            };
            achievementsData.achievementsList.Add(newAchievement);
        }
    }



    public void TestClearSteamAchievement()
    {
        if (SteamManager.Initialized)
        {
            Steamworks.SteamUserStats.ClearAchievement("ACH_PLAY_ONE_GAME");
        }
    }
    public void TestSteamAchievement()
    {
        if (SteamManager.Initialized)
        {
            Steamworks.SteamUserStats.SetAchievement("ACH_PLAY_ONE_GAME");
        }
    }
    public void TestCheckSteamAchievement()
    {
        if (SteamManager.Initialized)
        {
            if (Steamworks.SteamUserStats.GetAchievement("ACH_PLAY_ONE_GAME", out bool achieved))
            {
                Debug.Log("Achievement exist and is achieved = " + achieved);
            }
            if (Steamworks.SteamUserStats.GetAchievement("ACH_PLAY_ONE_GAMEsdkjhflsdiqjgflj", out bool achieved2))
            {
                Debug.Log("Achievement exist and is achieved = " + achieved2);
            }
            else
            {
                Debug.Log("Achievement doesn't exist");
            }
        }
    }



    public List<Achievement> GetUnlockedAchievementsForCurrentRun()
    {
        List<Achievement> unlockedAchievementsList = new List<Achievement>();
        // Gather useful data about the current Run
        //bool runIsWon = RunManager.instance.IsCurrentRunWon();

        foreach (Achievement achievement in achievementsData.achievementsList)
        {
            if (!achievement.unlocked)
            {
                // This achievement is not unlocked yet
            }
        }

        return unlockedAchievementsList;
    }


    public List<string> CheckForUnlockingCharacters()
    {
        List<string> unlockedCharacterNames = new List<string>();

        bool gameIsWon = RunManager.instance.completedChaptersList.Count >= 5;
        
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
