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

    [Header("Runtime")]
    public GameMode selectedGameModes;

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
        ResetDifficulty();
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


    public void ResetDifficulty()
    {
        selectedGameModes = GameMode.NONE;
    }


    public void DisplayDifficultyInfo(GameMode gameMode)
    {
        string text = "";
        TextMeshProUGUI setThisText = difficultyPanelInfoText;

        int modifierAmount = 0;

        if ((gameMode & GameMode.HARD) == GameMode.HARD)
        {
            // Hard mode activated
            modifierAmount += 50;
        }
        if ((gameMode & GameMode.HARDER) == GameMode.HARDER)
        {
            // Harder mode activated
            modifierAmount += 100;
        }
        if ((gameMode & GameMode.HARDEST) == GameMode.HARDEST)
        {
            // Hardest mode activated
            modifierAmount += 250;
        }
        if ((gameMode & GameMode.HARDESTER) == GameMode.HARDESTER)
        {
            // Hardesest mode activated
            modifierAmount += 500;
        }

        text = "+" + modifierAmount + "% bug health\n+" + modifierAmount + "% bug speed\n+" + modifierAmount + "% bug strength\n+" + modifierAmount + "% froins";
        setThisText.text = text;
    }


}
