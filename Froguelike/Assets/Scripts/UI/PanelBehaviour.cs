using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class PanelBehaviour : MonoBehaviour
{
    public GameObject difficultyPanelTitleText;
    public GameObject difficultyPanelTitleUnknown;
    public ModifierToggleBehaviour hardToggle;
    public ModifierToggleBehaviour harderToggle;
    public ModifierToggleBehaviour hardestToggle;
    public ModifierToggleBehaviour hardesterToggle;
    [Space]
    public GameObject timePanelTitleText;
    public GameObject timePanelTitleUnknown;
    public ModifierToggleBehaviour halfTimeToggle;
    public ModifierToggleBehaviour doubleTimeToggle;
    public ModifierToggleBehaviour endlessTimeToggle;
    [Space]
    public GameObject bugtierPanelTitleText;
    public GameObject bugtierPanelTitleUnknown;
    public ModifierToggleBehaviour upOneToggle;
    public ModifierToggleBehaviour downOneToggle;
    public ModifierToggleBehaviour maxToggle;
    public ModifierToggleBehaviour minToggle;
    [Space]
    public GameObject powerupsPanelTitleText;
    public GameObject powerupsPanelTitleUnknown;
    public ModifierToggleBehaviour freezeToggle;
    public ModifierToggleBehaviour poisonToggle;
    public ModifierToggleBehaviour curseToggle;
    public ModifierToggleBehaviour superfrogToggle;
    public ModifierToggleBehaviour partyToggle;
    public ModifierToggleBehaviour goopToggle;
    public ModifierToggleBehaviour upBugToggle;
    public ModifierToggleBehaviour downBugToggle;
    [Space]
    public GameObject eternalsPanelTitleText;
    public GameObject eternalsPanelTitleUnknown;
    public ModifierToggleBehaviour eternalsToggle;
    [Space]
    public GameObject hatsPanelTitleText;
    public GameObject hatsPanelTitleUnknown;
    public List<HatToggle> hatToggles;

    public void SetAllCheckmarks(GameMode gameModes, HatType hatType)
    {
        SetDifficultyCheckmarks(gameModes);
        SetTimeCheckmarks(gameModes);
        SetBugtierCheckmarks(gameModes);
        SetPowerupsCheckmarks(gameModes);
        SetEternalsCheckmarks(gameModes);
        SetHatCheckmarks(hatType);
    }

    public void SetDifficultyCheckmarks(GameMode gameModes)
    {
        // Hard.
        bool modeIsOn = (gameModes & GameMode.HARD) == GameMode.HARD;
        hardToggle.toggle.isOn = modeIsOn;
        hardToggle.staticCheckmark.SetActive(modeIsOn);
        // Harder.
        modeIsOn = (gameModes & GameMode.HARDER) == GameMode.HARDER;
        harderToggle.toggle.isOn = modeIsOn;
        harderToggle.staticCheckmark.SetActive(modeIsOn);
        // Hardest.
        modeIsOn = (gameModes & GameMode.HARDEST) == GameMode.HARDEST;
        hardestToggle.toggle.isOn = modeIsOn;
        hardestToggle.staticCheckmark.SetActive(modeIsOn);
        // Harder.
        modeIsOn = (gameModes & GameMode.HARDESTER) == GameMode.HARDESTER;
        hardesterToggle.toggle.isOn = modeIsOn;
        hardesterToggle.staticCheckmark.SetActive(modeIsOn);
    }

    public void SetTimeCheckmarks(GameMode gameModes)
    {
        // Half.
        bool modeIsOn = (gameModes & GameMode.TIME_HALF) == GameMode.TIME_HALF;
        halfTimeToggle.toggle.isOn = modeIsOn;
        // Double.
        modeIsOn = (gameModes & GameMode.TIME_DOUBLE) == GameMode.TIME_DOUBLE;
        doubleTimeToggle.toggle.isOn = modeIsOn;
        // Endless.
        modeIsOn = (gameModes & GameMode.TIME_ENDLESS) == GameMode.TIME_ENDLESS;
        endlessTimeToggle.toggle.isOn = modeIsOn;
    }

    public void SetBugtierCheckmarks(GameMode gameModes)
    {
        // Up one.
        bool modeIsOn = (gameModes & GameMode.BUGS_UP) == GameMode.BUGS_UP;
        upOneToggle.toggle.isOn = modeIsOn;
        upOneToggle.GetComponent<ModifierToggleBehaviour>().staticCheckmark.SetActive(modeIsOn);
        // Down one.
        modeIsOn = (gameModes & GameMode.BUGS_DOWN) == GameMode.BUGS_DOWN;
        downOneToggle.toggle.isOn = modeIsOn;
        downOneToggle.GetComponent<ModifierToggleBehaviour>().staticCheckmark.SetActive(modeIsOn);
        // Max.
        modeIsOn = (gameModes & GameMode.BUGS_MAX) == GameMode.BUGS_MAX;
        maxToggle.toggle.isOn = modeIsOn;
        maxToggle.GetComponent<ModifierToggleBehaviour>().staticCheckmark.SetActive(modeIsOn);
        // Min.
        modeIsOn = (gameModes & GameMode.BUGS_MIN) == GameMode.BUGS_MIN;
        minToggle.toggle.isOn = modeIsOn;
        minToggle.GetComponent<ModifierToggleBehaviour>().staticCheckmark.SetActive(modeIsOn);
    }

    public void SetPowerupsCheckmarks(GameMode gameModes)
    {
        // Freeze.
        bool modeIsOn = (gameModes & GameMode.POWER_UP_FREEZE) == GameMode.POWER_UP_FREEZE;
        freezeToggle.toggle.isOn = modeIsOn;
        freezeToggle.GetComponent<ModifierToggleBehaviour>().staticCheckmark.SetActive(modeIsOn);
        // Poison.
        modeIsOn = (gameModes & GameMode.POWER_UP_POISON) == GameMode.POWER_UP_POISON;
        poisonToggle.toggle.isOn = modeIsOn;
        poisonToggle.GetComponent<ModifierToggleBehaviour>().staticCheckmark.SetActive(modeIsOn);
        // Curse.
        modeIsOn = (gameModes & GameMode.POWER_UP_CURSE) == GameMode.POWER_UP_CURSE;
        curseToggle.toggle.isOn = modeIsOn;
        curseToggle.GetComponent<ModifierToggleBehaviour>().staticCheckmark.SetActive(modeIsOn);
        // Super frog.
        modeIsOn = (gameModes & GameMode.POWER_UP_SUPERFROG) == GameMode.POWER_UP_SUPERFROG;
        superfrogToggle.toggle.isOn = modeIsOn;
        superfrogToggle.GetComponent<ModifierToggleBehaviour>().staticCheckmark.SetActive(modeIsOn);
        // Party.
        modeIsOn = (gameModes & GameMode.POWER_UP_PARTY) == GameMode.POWER_UP_PARTY;
        partyToggle.toggle.isOn = modeIsOn;
        partyToggle.GetComponent<ModifierToggleBehaviour>().staticCheckmark.SetActive(modeIsOn);
        // Goop.
        modeIsOn = (gameModes & GameMode.POWER_UP_GOOP) == GameMode.POWER_UP_GOOP;
        goopToggle.toggle.isOn = modeIsOn;
        goopToggle.GetComponent<ModifierToggleBehaviour>().staticCheckmark.SetActive(modeIsOn);
        // Up.
        modeIsOn = (gameModes & GameMode.POWER_UP_UP) == GameMode.POWER_UP_UP;
        upBugToggle.toggle.isOn = modeIsOn;
        upBugToggle.GetComponent<ModifierToggleBehaviour>().staticCheckmark.SetActive(modeIsOn);
        // Down.
        modeIsOn = (gameModes & GameMode.POWER_UP_DOWN) == GameMode.POWER_UP_DOWN;
        downBugToggle.toggle.isOn = modeIsOn;
        downBugToggle.GetComponent<ModifierToggleBehaviour>().staticCheckmark.SetActive(modeIsOn);
    }

    public void SetEternalsCheckmarks(GameMode gameModes)
    {
        // Eternals.
        bool modeIsOn = (gameModes & GameMode.IGNORE_ETERNALS) == GameMode.IGNORE_ETERNALS;
        eternalsToggle.toggle.isOn = modeIsOn;
        eternalsToggle.GetComponent<ModifierToggleBehaviour>().staticCheckmark.SetActive(modeIsOn);
    }

    public void SetHatCheckmarks(HatType hatType)
    {
        // Hats.
        foreach (HatToggle hatToggle in hatToggles)
        {
            if (hatType == HatType.NO_HAT)
            {
                if (hatToggle.thisToggle.interactable)
                {
                    hatToggle.thisToggle.SetIsOnWithoutNotify(false);
                    continue;
                }
            }

            if (hatToggle.hatType == hatType)
            {
                hatToggle.thisToggle.isOn = true;
                hatToggle.thisToggle.GetComponent<ModifierToggleBehaviour>().staticCheckmark.SetActive(true);
            }
        }
    }

    public void ResetToggles()
    {
        hardToggle.ResetToggle();
        harderToggle.ResetToggle();
        hardestToggle.ResetToggle();
        hardesterToggle.ResetToggle();
        halfTimeToggle.ResetToggle();
        doubleTimeToggle.ResetToggle();
        endlessTimeToggle.ResetToggle();
        upOneToggle.ResetToggle();
        downOneToggle.ResetToggle();
        maxToggle.ResetToggle();
        minToggle.ResetToggle();
        freezeToggle.ResetToggle();
        poisonToggle.ResetToggle();
        curseToggle.ResetToggle();
        superfrogToggle.ResetToggle();
        partyToggle.ResetToggle();
        goopToggle.ResetToggle();
        upBugToggle.ResetToggle();
        downBugToggle.ResetToggle();
        eternalsToggle.ResetToggle();
        foreach (HatToggle hatToggle in hatToggles)
        {
            hatToggle.GetComponent<ModifierToggleBehaviour>().ResetToggle();
        }
    }

    public void SetVisibility(GameMode gameModes)
    {
        // Difficulty.
        if (IsGameModeUnlocked(GameMode.HARD) || IsGameModeUnlocked(GameMode.HARDER) || IsGameModeUnlocked(GameMode.HARDEST) || IsGameModeUnlocked(GameMode.HARDESTER))
        {
            difficultyPanelTitleText.SetActive(true);
            difficultyPanelTitleUnknown.SetActive(false);
            RunModifierManager.instance.DisplayDifficultyInfo(RunModifierManager.instance.GetSelectedGameModes());
        }
        else
        {
            difficultyPanelTitleText.SetActive(false);
            difficultyPanelTitleUnknown.SetActive(true);
            RunModifierManager.instance.difficultyPanelInfoText.SetText("");
        }

        hardToggle.SetInteractability(IsGameModeUnlocked(GameMode.HARD));
        harderToggle.SetInteractability(IsGameModeUnlocked(GameMode.HARDER));
        hardestToggle.SetInteractability(IsGameModeUnlocked(GameMode.HARDEST));
        hardesterToggle.SetInteractability(IsGameModeUnlocked(GameMode.HARDESTER));

        // Time.
        if (IsGameModeUnlocked(GameMode.TIME_HALF) || IsGameModeUnlocked(GameMode.TIME_DOUBLE) || IsGameModeUnlocked(GameMode.TIME_ENDLESS))
        {
            timePanelTitleText.SetActive(true);
            timePanelTitleUnknown.SetActive(false);
        }
        else
        {
            timePanelTitleText.SetActive(false);
            timePanelTitleUnknown.SetActive(true);
        }

        halfTimeToggle.SetInteractability(IsGameModeUnlocked(GameMode.TIME_HALF));
        doubleTimeToggle.SetInteractability(IsGameModeUnlocked(GameMode.TIME_DOUBLE));
        endlessTimeToggle.SetInteractability(IsGameModeUnlocked(GameMode.TIME_ENDLESS));

        // Bug tier.
        if (IsGameModeUnlocked(GameMode.BUGS_UP) || IsGameModeUnlocked(GameMode.BUGS_DOWN) || IsGameModeUnlocked(GameMode.BUGS_MAX) || IsGameModeUnlocked(GameMode.BUGS_MIN))
        {
            bugtierPanelTitleText.SetActive(true);
            bugtierPanelTitleUnknown.SetActive(false);
        }
        else
        {
            bugtierPanelTitleText.SetActive(false);
            bugtierPanelTitleUnknown.SetActive(true);
        }

        upOneToggle.SetInteractability(IsGameModeUnlocked(GameMode.BUGS_UP));
        downOneToggle.SetInteractability(IsGameModeUnlocked(GameMode.BUGS_DOWN));
        maxToggle.SetInteractability(IsGameModeUnlocked(GameMode.BUGS_MAX));
        minToggle.SetInteractability(IsGameModeUnlocked(GameMode.BUGS_MIN));

        // Power-ups.
        if (IsGameModeUnlocked(GameMode.POWER_UP_FREEZE) || IsGameModeUnlocked(GameMode.POWER_UP_POISON) || IsGameModeUnlocked(GameMode.POWER_UP_CURSE) || IsGameModeUnlocked(GameMode.POWER_UP_SUPERFROG) || IsGameModeUnlocked(GameMode.POWER_UP_PARTY) || IsGameModeUnlocked(GameMode.POWER_UP_GOOP) || IsGameModeUnlocked(GameMode.POWER_UP_UP) || IsGameModeUnlocked(GameMode.POWER_UP_DOWN))
        {
            powerupsPanelTitleText.SetActive(true);
            powerupsPanelTitleUnknown.SetActive(false);
        }
        else
        {
            powerupsPanelTitleText.SetActive(false);
            powerupsPanelTitleUnknown.SetActive(true);
        }

        freezeToggle.SetInteractability(IsGameModeUnlocked(GameMode.POWER_UP_FREEZE));
        poisonToggle.SetInteractability(IsGameModeUnlocked(GameMode.POWER_UP_POISON));
        curseToggle.SetInteractability(IsGameModeUnlocked(GameMode.POWER_UP_CURSE));
        superfrogToggle.SetInteractability(IsGameModeUnlocked(GameMode.POWER_UP_SUPERFROG));
        partyToggle.SetInteractability(IsGameModeUnlocked(GameMode.POWER_UP_PARTY));
        goopToggle.SetInteractability(IsGameModeUnlocked(GameMode.POWER_UP_GOOP));
        upBugToggle.SetInteractability(IsGameModeUnlocked(GameMode.POWER_UP_UP));
        downBugToggle.SetInteractability(IsGameModeUnlocked(GameMode.POWER_UP_DOWN));

        // Eternals.
        if (IsGameModeUnlocked(GameMode.IGNORE_ETERNALS))
        {
            eternalsPanelTitleText.SetActive(true);
            eternalsPanelTitleUnknown.SetActive(false);
        }
        else
        {
            eternalsPanelTitleText.SetActive(false);
            eternalsPanelTitleUnknown.SetActive(true);
        }

        eternalsToggle.SetInteractability(IsGameModeUnlocked(GameMode.IGNORE_ETERNALS));

        // Hats.
        List<HatType> foundHats = CollectiblesManager.instance.GetFoundHats();
        if (foundHats.Count == 0)
        {
            hatsPanelTitleText.SetActive(false);
            hatsPanelTitleUnknown.SetActive(true);
        }
        else
        {
            hatsPanelTitleText.SetActive(true);
            hatsPanelTitleUnknown.SetActive(false);
        }

        foreach (HatToggle hatToggle in hatToggles)
        {
            if (foundHats.Contains(hatToggle.GetComponent<HatToggle>().hatType))
            {
                hatToggle.GetComponent<ModifierToggleBehaviour>().SetInteractability(true);
            }
            else
            {
                hatToggle.GetComponent<ModifierToggleBehaviour>().SetInteractability(false);
            }
        }
    }

    private bool IsGameModeUnlocked(GameMode gameMode)
    {
        return (CharacterManager.instance.charactersData.availableGameModes & gameMode) == gameMode;
    }
}
