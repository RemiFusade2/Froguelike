using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RunModifierManager : MonoBehaviour
{
    // Singleton
    public static RunModifierManager instance;

    [Header("UI References")]
    public GameObject difficultyPanel;
    public TextMeshProUGUI difficultyPanelInfoText;

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
        difficultyPanel.SetActive(true);
        // ResetDifficulty();
        difficultyPanel.GetComponent<DifficultyPanelBehaviour>().SetToggleCheckmarks(GetSelectedGameModes());
        DisplayDifficultyInfo(GetSelectedGameModes());
    }

    public bool IsGameModeUnlocked(GameMode gameMode)
    {
        return (CharacterManager.instance.charactersData.availableGameModes & gameMode) == gameMode;
    }

    public void UnlockGameMode(GameMode gameMode)
    {
        CharacterManager.instance.charactersData.availableGameModes |= gameMode;
        SaveDataManager.instance.isSaveDataDirty = true;
    }

    public GameMode GetSelectedGameModes()
    {
        return selectedGameModes;
    }

    public void ResetModifiers()
    {
        selectedGameModes = GameMode.NONE;
        powerUpsModiferSpawnFrequencies.Clear();
    }

    #region Difficulty

    public void SelectGameModeHard(Toggle thisToggle)
    {
        bool hardModeIsSelected = thisToggle.isOn;

        if (hardModeIsSelected)
        {
            selectedGameModes |= GameMode.HARD; // Add flag
        }
        else
        {
            selectedGameModes &= ~GameMode.HARD; // Remove flag
        }

        DisplayDifficultyInfo(GetSelectedGameModes());
    }

    public void SelectGameModeHarder(Toggle thisToggle)
    {
        bool harderModeIsSelected = thisToggle.isOn;

        if (harderModeIsSelected)
        {
            selectedGameModes |= GameMode.HARDER; // Add flag
        }
        else
        {
            selectedGameModes &= ~GameMode.HARDER; // Remove flag
        }

        DisplayDifficultyInfo(GetSelectedGameModes());
    }

    public void SelectGameModeHardest(Toggle thisToggle)
    {
        bool harderModeIsSelected = thisToggle.isOn;

        if (harderModeIsSelected)
        {
            selectedGameModes |= GameMode.HARDEST; // Add flag
        }
        else
        {
            selectedGameModes &= ~GameMode.HARDEST; // Remove flag
        }

        DisplayDifficultyInfo(GetSelectedGameModes());
    }

    public void SelectGameModeHardester(Toggle thisToggle)
    {
        bool harderModeIsSelected = thisToggle.isOn;

        if (harderModeIsSelected)
        {
            selectedGameModes |= GameMode.HARDESTER; // Add flag
        }
        else
        {
            selectedGameModes &= ~GameMode.HARDESTER; // Remove flag
        }

        DisplayDifficultyInfo(GetSelectedGameModes());
    }

    #endregion Difficulty

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

    #region Power-ups

    public void SelectGameModeFreeze(Toggle thisToggle)
    {
        bool harderModeIsSelected = thisToggle.isOn;

        if (harderModeIsSelected)
        {
            selectedGameModes |= GameMode.POWER_UP_FREEZE; // Add flag
        }
        else
        {
            selectedGameModes &= ~GameMode.POWER_UP_FREEZE; // Remove flag
        }
    }

    public void SelectGameModePoison(Toggle thisToggle)
    {
        bool harderModeIsSelected = thisToggle.isOn;

        if (harderModeIsSelected)
        {
            selectedGameModes |= GameMode.POWER_UP_POISON; // Add flag
        }
        else
        {
            selectedGameModes &= ~GameMode.POWER_UP_POISON; // Remove flag
        }
    }

    public void SelectGameModeCurse(Toggle thisToggle)
    {
        bool harderModeIsSelected = thisToggle.isOn;

        if (harderModeIsSelected)
        {
            selectedGameModes |= GameMode.POWER_UP_CURSE; // Add flag
        }
        else
        {
            selectedGameModes &= ~GameMode.POWER_UP_CURSE; // Remove flag
        }
    }

    public void SelectGameModeSuperfrog(Toggle thisToggle)
    {
        bool harderModeIsSelected = thisToggle.isOn;

        if (harderModeIsSelected)
        {
            selectedGameModes |= GameMode.POWER_UP_SUPERFROG; // Add flag
        }
        else
        {
            selectedGameModes &= ~GameMode.POWER_UP_SUPERFROG; // Remove flag
        }
    }

    public void SelectGameModeParty(Toggle thisToggle)
    {
        bool harderModeIsSelected = thisToggle.isOn;

        if (harderModeIsSelected)
        {
            selectedGameModes |= GameMode.POWER_UP_PARTY; // Add flag
        }
        else
        {
            selectedGameModes &= ~GameMode.POWER_UP_PARTY; // Remove flag
        }
    }

    public void SelectGameModeGoop(Toggle thisToggle)
    {
        bool harderModeIsSelected = thisToggle.isOn;

        if (harderModeIsSelected)
        {
            selectedGameModes |= GameMode.POWER_UP_GOOP; // Add flag
        }
        else
        {
            selectedGameModes &= ~GameMode.POWER_UP_GOOP; // Remove flag
        }
    }

    public void SelectGameModeUp(Toggle thisToggle)
    {
        bool harderModeIsSelected = thisToggle.isOn;

        if (harderModeIsSelected)
        {
            selectedGameModes |= GameMode.POWER_UP_UP; // Add flag
        }
        else
        {
            selectedGameModes &= ~GameMode.POWER_UP_UP; // Remove flag
        }
    }

    public void SelectGameModeDown(Toggle thisToggle)
    {
        bool harderModeIsSelected = thisToggle.isOn;

        if (harderModeIsSelected)
        {
            selectedGameModes |= GameMode.POWER_UP_DOWN; // Add flag
        }
        else
        {
            selectedGameModes &= ~GameMode.POWER_UP_DOWN; // Remove flag
        }
    }

    #endregion Power-ups
}
