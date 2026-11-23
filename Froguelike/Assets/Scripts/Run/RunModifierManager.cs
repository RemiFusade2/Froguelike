using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RunModifierManager : MonoBehaviour
{
    // Singleton
    public static RunModifierManager instance;

    public PanelBehaviour panelBehaviour;
    public FrogCharacterController frogCharacterController;
    [Header("UI References")] // TODO add all panels, should the be blank/hidden before having one option unlocked? Or just ???? 
    public GameObject difficultyPanel;
    public TextMeshProUGUI difficultyPanelInfoText;
    public Toggle hardToggle;
    public Toggle harderToggle;
    public Toggle hardestToggle;
    public Toggle hardesterToggle;
    [Space]
    public GameObject timePanel;
    public Toggle halfTimeToggle;
    public Toggle doubleTimeToggle;
    public Toggle endlessModeToggle;
    [Space]
    public GameObject bugTierPanel;
    public Toggle bugsUpToggle;
    public Toggle bugsDownToggle;
    public Toggle bugsMaxToggle;
    public Toggle bugsMinToggle;
    [Space]
    public GameObject powerupPanel;
    public Toggle freezeToggle;
    public Toggle poisonToggle;
    public Toggle curseToggle;
    public Toggle superfrogToggle;
    public Toggle partyToggle;
    public Toggle goopToggle;
    public Toggle upbugToggle;
    public Toggle downbugToggle;
    [Space]
    public GameObject eternalsPanel;
    public Toggle eternalsToggle;
    [Space]
    public GameObject hatPanel;
    public List<Toggle> hatToggles;

    [Header("Settings")]
    [Tooltip("1 = 100% bug buff")]
    public float hardModifier;
    [Tooltip("1 = 100% bug buff")]
    public float harderModifier;
    [Tooltip("1 = 100% bug buff")]
    public float hardestModifier;
    [Tooltip("1 = 100% bug buff")]
    public float hardesterModifier;
    public CollectibleSpawnFrequency powerupFreeze;
    public CollectibleSpawnFrequency powerupPoison;
    public CollectibleSpawnFrequency powerupCurse;
    public CollectibleSpawnFrequency powerupSuperfrog;
    public CollectibleSpawnFrequency powerupParty;
    public CollectibleSpawnFrequency powerupGoop;
    public CollectibleSpawnFrequency powerupUp;
    public CollectibleSpawnFrequency powerupDown;


    [Header("Runtime")]
    public GameMode selectedGameModes;
    public List<CollectibleSpawnFrequency> powerUpsModiferSpawnFrequencies;
    public int bugTierModifier = 0;
    public bool ignoreEternals = false;
    public string selectedHatName = "NONE";

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

    public void UpdateModifierScreen()
    {
        selectedGameModes = Enum.Parse<GameMode>(GameManager.instance.GetCurrentSetGameModes());

        selectedHatName = GameManager.instance.GetCurrentSetHatName();

        panelBehaviour.SetVisibility(selectedGameModes);
        panelBehaviour.SetAllCheckmarks(GetSelectedGameModes(), DataManager.instance.GetTypeOfHatFromString(selectedHatName));
    }

    public bool IsGameModeUnlocked(GameMode gameMode)
    {
        return (CharacterManager.instance.charactersData.availableGameModes & gameMode) == gameMode;
    }

    public void UnlockGameMode(GameMode gameMode)
    {
        CharacterManager.instance.charactersData.availableGameModes |= gameMode; // TODO does this work with multiple game modes?
        SaveDataManager.instance.isSaveDataDirty = true;
    }

    public GameMode GetSelectedGameModes()
    {
        return selectedGameModes;
    }

    public void ResetModifiers()
    {
        selectedGameModes = GameMode.NONE;
        selectedHatName = "NONE";
        ResetModifierValues();
    }

    public void ResetModifierValues()
    {
        powerUpsModiferSpawnFrequencies.Clear();
        bugTierModifier = 0;
        ignoreEternals = false;
    }

    #region Difficulty

    public void SelectGameModeHard(Toggle thisToggle)
    {
        bool modeIsSelected = thisToggle.isOn;

        if (modeIsSelected)
        {
            selectedGameModes |= GameMode.HARD; // Add flag
        }
        else
        {
            selectedGameModes &= ~GameMode.HARD; // Remove flag
        }

        DisplayDifficultyInfo(GetSelectedGameModes());
        GameManager.instance.SaveCurrentSetGameMode(selectedGameModes.ToString());
    }

    public void SelectGameModeHarder(Toggle thisToggle)
    {
        bool modeIsSelected = thisToggle.isOn;

        if (modeIsSelected)
        {
            selectedGameModes |= GameMode.HARDER; // Add flag
        }
        else
        {
            selectedGameModes &= ~GameMode.HARDER; // Remove flag
        }

        DisplayDifficultyInfo(GetSelectedGameModes());
        GameManager.instance.SaveCurrentSetGameMode(selectedGameModes.ToString());
    }

    public void SelectGameModeHardest(Toggle thisToggle)
    {
        bool modeIsSelected = thisToggle.isOn;

        if (modeIsSelected)
        {
            selectedGameModes |= GameMode.HARDEST; // Add flag
        }
        else
        {
            selectedGameModes &= ~GameMode.HARDEST; // Remove flag
        }

        DisplayDifficultyInfo(GetSelectedGameModes());
        GameManager.instance.SaveCurrentSetGameMode(selectedGameModes.ToString());
    }

    public void SelectGameModeHardester(Toggle thisToggle)
    {
        bool modeIsSelected = thisToggle.isOn;

        if (modeIsSelected)
        {
            selectedGameModes |= GameMode.HARDESTER; // Add flag
        }
        else
        {
            selectedGameModes &= ~GameMode.HARDESTER; // Remove flag
        }

        DisplayDifficultyInfo(GetSelectedGameModes());
        GameManager.instance.SaveCurrentSetGameMode(selectedGameModes.ToString());
    }

    public void DisplayDifficultyInfo(GameMode gameMode)
    {
        string text = "";
        TextMeshProUGUI setThisText = difficultyPanelInfoText;

        int modifierAmount = 0;

        if ((gameMode & GameMode.HARD) == GameMode.HARD)
        {
            // Hard mode activated
            modifierAmount += (int)(hardModifier * 100);
        }
        if ((gameMode & GameMode.HARDER) == GameMode.HARDER)
        {
            // Harder mode activated
            modifierAmount += (int)(harderModifier * 100);
        }
        if ((gameMode & GameMode.HARDEST) == GameMode.HARDEST)
        {
            // Hardest mode activated
            modifierAmount += (int)(hardestModifier * 100);
        }
        if ((gameMode & GameMode.HARDESTER) == GameMode.HARDESTER)
        {
            // Hardesest mode activated
            modifierAmount += (int)(hardesterModifier * 100);
        }

        text = "+" + modifierAmount + "% bug health\n+" + modifierAmount + "% bug speed\n+" + modifierAmount + "% bug strength\n+" + modifierAmount + "% froins";
        setThisText.text = text;
    }

    #endregion Difficulty

    #region Power-ups

    public void SelectGameModeFreeze(Toggle thisToggle)
    {
        bool modeIsSelected = thisToggle.isOn;

        if (modeIsSelected)
        {
            selectedGameModes |= GameMode.POWER_UP_FREEZE; // Add flag
        }
        else
        {
            selectedGameModes &= ~GameMode.POWER_UP_FREEZE; // Remove flag
        }

        GameManager.instance.SaveCurrentSetGameMode(selectedGameModes.ToString());
    }

    public void SelectGameModePoison(Toggle thisToggle)
    {
        bool modeIsSelected = thisToggle.isOn;

        if (modeIsSelected)
        {
            selectedGameModes |= GameMode.POWER_UP_POISON; // Add flag
        }
        else
        {
            selectedGameModes &= ~GameMode.POWER_UP_POISON; // Remove flag
        }

        GameManager.instance.SaveCurrentSetGameMode(selectedGameModes.ToString());
    }

    public void SelectGameModeCurse(Toggle thisToggle)
    {
        bool modeIsSelected = thisToggle.isOn;

        if (modeIsSelected)
        {
            selectedGameModes |= GameMode.POWER_UP_CURSE; // Add flag
        }
        else
        {
            selectedGameModes &= ~GameMode.POWER_UP_CURSE; // Remove flag
        }

        GameManager.instance.SaveCurrentSetGameMode(selectedGameModes.ToString());
    }

    public void SelectGameModeSuperfrog(Toggle thisToggle)
    {
        bool modeIsSelected = thisToggle.isOn;

        if (modeIsSelected)
        {
            selectedGameModes |= GameMode.POWER_UP_SUPERFROG; // Add flag
        }
        else
        {
            selectedGameModes &= ~GameMode.POWER_UP_SUPERFROG; // Remove flag
        }

        GameManager.instance.SaveCurrentSetGameMode(selectedGameModes.ToString());
    }

    public void SelectGameModeParty(Toggle thisToggle)
    {
        bool modeIsSelected = thisToggle.isOn;

        if (modeIsSelected)
        {
            selectedGameModes |= GameMode.POWER_UP_PARTY; // Add flag
        }
        else
        {
            selectedGameModes &= ~GameMode.POWER_UP_PARTY; // Remove flag
        }

        GameManager.instance.SaveCurrentSetGameMode(selectedGameModes.ToString());
    }

    public void SelectGameModeGoop(Toggle thisToggle)
    {
        bool modeIsSelected = thisToggle.isOn;

        if (modeIsSelected)
        {
            selectedGameModes |= GameMode.POWER_UP_GOOP; // Add flag
        }
        else
        {
            selectedGameModes &= ~GameMode.POWER_UP_GOOP; // Remove flag
        }

        GameManager.instance.SaveCurrentSetGameMode(selectedGameModes.ToString());
    }

    public void SelectGameModeUp(Toggle thisToggle)
    {
        bool modeIsSelected = thisToggle.isOn;

        if (modeIsSelected)
        {
            selectedGameModes |= GameMode.POWER_UP_UP; // Add flag
        }
        else
        {
            selectedGameModes &= ~GameMode.POWER_UP_UP; // Remove flag
        }

        GameManager.instance.SaveCurrentSetGameMode(selectedGameModes.ToString());
    }

    public void SelectGameModeDown(Toggle thisToggle)
    {
        bool modeIsSelected = thisToggle.isOn;

        if (modeIsSelected)
        {
            selectedGameModes |= GameMode.POWER_UP_DOWN; // Add flag
        }
        else
        {
            selectedGameModes &= ~GameMode.POWER_UP_DOWN; // Remove flag
        }

        GameManager.instance.SaveCurrentSetGameMode(selectedGameModes.ToString());
    }

    #endregion Power-ups

    #region Time

    public void SelectGameModeHalfTime(Toggle thisToggle)
    {
        // Turn on half time.
        bool modeIsSelected = thisToggle.isOn;

        if (modeIsSelected)
        {
            selectedGameModes |= GameMode.TIME_HALF; // Add flag
        }
        else
        {
            selectedGameModes &= ~GameMode.TIME_HALF; // Remove flag
        }

        // Turn off double time and endless mode. set toggle
        doubleTimeToggle.SetIsOnWithoutNotify(false);
        endlessModeToggle.SetIsOnWithoutNotify(false);
        selectedGameModes &= ~GameMode.TIME_DOUBLE; // Remove flag
        selectedGameModes &= ~GameMode.TIME_ENDLESS; // Remove flag

        GameManager.instance.SaveCurrentSetGameMode(selectedGameModes.ToString());
    }

    public void SelectGameModeDoubleTime(Toggle thisToggle)
    {
        // Turn on double time.
        bool modeIsSelected = thisToggle.isOn;

        if (modeIsSelected)
        {
            selectedGameModes |= GameMode.TIME_DOUBLE; // Add flag
        }
        else
        {
            selectedGameModes &= ~GameMode.TIME_DOUBLE; // Remove flag
        }

        // Turn off half time and endless mode
        halfTimeToggle.SetIsOnWithoutNotify(false);
        endlessModeToggle.SetIsOnWithoutNotify(false);
        selectedGameModes &= ~GameMode.TIME_HALF; // Remove flag
        selectedGameModes &= ~GameMode.TIME_ENDLESS; // Remove flag

        GameManager.instance.SaveCurrentSetGameMode(selectedGameModes.ToString());
    }


    public void SelectGameModeEndlessTime(Toggle thisToggle)
    {
        // Turn on endless mode.
        bool modeIsSelected = thisToggle.isOn;

        if (modeIsSelected)
        {
            selectedGameModes |= GameMode.TIME_ENDLESS; // Add flag
        }
        else
        {
            selectedGameModes &= ~GameMode.TIME_ENDLESS; // Remove flag
        }

        // Turn off half and double time.
        halfTimeToggle.SetIsOnWithoutNotify(false);
        doubleTimeToggle.SetIsOnWithoutNotify(false);
        selectedGameModes &= ~GameMode.TIME_HALF; // Remove flag
        selectedGameModes &= ~GameMode.TIME_DOUBLE; // Remove flag

        GameManager.instance.SaveCurrentSetGameMode(selectedGameModes.ToString());
    }

    #endregion Time

    #region Bug tier

    public void SelectGameModeBugTierUp(Toggle thisToggle)
    {
        // Turn on bugs up.
        bool modeIsSelected = thisToggle.isOn;

        if (modeIsSelected)
        {
            selectedGameModes |= GameMode.BUGS_UP; // Add flag
        }
        else
        {
            selectedGameModes &= ~GameMode.BUGS_UP; // Remove flag
        }

        // Turn off the other bug tier modifiers.
        bugsDownToggle.SetIsOnWithoutNotify(false);
        bugsMaxToggle.SetIsOnWithoutNotify(false);
        bugsMinToggle.SetIsOnWithoutNotify(false);
        selectedGameModes &= ~GameMode.BUGS_DOWN; // Remove flag
        selectedGameModes &= ~GameMode.BUGS_MAX; // Remove flag
        selectedGameModes &= ~GameMode.BUGS_MIN; // Remove flag

        GameManager.instance.SaveCurrentSetGameMode(selectedGameModes.ToString());
    }

    public void SelectGameModeBugTierDown(Toggle thisToggle)
    {
        // Turn on bugs down.
        bool modeIsSelected = thisToggle.isOn;

        if (modeIsSelected)
        {
            selectedGameModes |= GameMode.BUGS_DOWN; // Add flag
        }
        else
        {
            selectedGameModes &= ~GameMode.BUGS_DOWN; // Remove flag
        }

        // Turn off the other bug tier modifiers.
        bugsUpToggle.SetIsOnWithoutNotify(false);
        bugsMaxToggle.SetIsOnWithoutNotify(false);
        bugsMinToggle.SetIsOnWithoutNotify(false);
        selectedGameModes &= ~GameMode.BUGS_UP; // Remove flag
        selectedGameModes &= ~GameMode.BUGS_MAX; // Remove flag
        selectedGameModes &= ~GameMode.BUGS_MIN; // Remove flag

        GameManager.instance.SaveCurrentSetGameMode(selectedGameModes.ToString());
    }

    public void SelectGameModeBugTierMax(Toggle thisToggle)
    {
        // Turn on bugs max.
        bool modeIsSelected = thisToggle.isOn;

        if (modeIsSelected)
        {
            selectedGameModes |= GameMode.BUGS_MAX; // Add flag
        }
        else
        {
            selectedGameModes &= ~GameMode.BUGS_MAX; // Remove flag
        }

        // Turn off the other bug tier modifiers.
        bugsUpToggle.SetIsOnWithoutNotify(false);
        bugsDownToggle.SetIsOnWithoutNotify(false);
        bugsMinToggle.SetIsOnWithoutNotify(false);
        selectedGameModes &= ~GameMode.BUGS_UP; // Remove flag
        selectedGameModes &= ~GameMode.BUGS_DOWN; // Remove flag
        selectedGameModes &= ~GameMode.BUGS_MIN; // Remove flag

        GameManager.instance.SaveCurrentSetGameMode(selectedGameModes.ToString());
    }

    public void SelectGameModeBugTierMin(Toggle thisToggle)
    {
        // Turn on bugs min.
        bool modeIsSelected = thisToggle.isOn;

        if (modeIsSelected)
        {
            selectedGameModes |= GameMode.BUGS_MIN; // Add flag
        }
        else
        {
            selectedGameModes &= ~GameMode.BUGS_MIN; // Remove flag
        }

        // Turn off the other bug tier modifiers.
        bugsUpToggle.SetIsOnWithoutNotify(false);
        bugsDownToggle.SetIsOnWithoutNotify(false);
        bugsMaxToggle.SetIsOnWithoutNotify(false);
        selectedGameModes &= ~GameMode.BUGS_UP; // Remove flag
        selectedGameModes &= ~GameMode.BUGS_DOWN; // Remove flag
        selectedGameModes &= ~GameMode.BUGS_MAX; // Remove flag

        GameManager.instance.SaveCurrentSetGameMode(selectedGameModes.ToString());
    }

    #endregion Bug tier

    #region Eternals

    public void SelectGameModeIgonoreEternals(Toggle thisToggle)
    {
        // Turn off eternals.
        bool modeIsSelected = thisToggle.isOn;

        if (modeIsSelected)
        {
            selectedGameModes |= GameMode.IGNORE_ETERNALS; // Add flag
        }
        else
        {
            selectedGameModes &= ~GameMode.IGNORE_ETERNALS; // Remove flag
        }

        GameManager.instance.SaveCurrentSetGameMode(selectedGameModes.ToString());
    }

    #endregion Eternals

    #region Hats

    public void SelectHatToWear(HatToggle thisToggle)
    {
        bool modeIsSelected = thisToggle.thisToggle.isOn;

        if (modeIsSelected)
        {
            selectedHatName = DataManager.instance.GetInfoForHat(thisToggle.hatType).hatName;
        }
        else
        {
            selectedHatName = "NONE";
        }

        foreach (Toggle otherToggle in hatToggles)
        {
            if (otherToggle.GetComponent<HatToggle>().hatType != thisToggle.GetComponent<HatToggle>().hatType && otherToggle.isOn)
            {
                otherToggle.SetIsOnWithoutNotify(false);
            }
        }

        frogCharacterController.ClearHats();
        if (modeIsSelected) frogCharacterController.AddHat(thisToggle.hatType);

        GameManager.instance.SaveCurrentSetHat(selectedHatName);
    }

    #endregion Hats
}
