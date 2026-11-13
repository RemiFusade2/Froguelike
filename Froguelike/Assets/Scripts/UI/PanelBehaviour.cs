using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PanelBehaviour : MonoBehaviour
{
    public Toggle hardToggle;
    public Toggle harderToggle;
    public Toggle hardestToggle;
    public Toggle hardesterToggle;
    [Space]
    public Toggle halfTimeToggle;
    public Toggle doubleTimeToggle;
    [Space]
    public Toggle upOneToggle;
    public Toggle downOneToggle;
    public Toggle maxToggle;
    public Toggle minToggle;
    [Space]
    public Toggle freezeToggle;
    public Toggle poisonToggle;
    public Toggle curseToggle;
    public Toggle superfrogToggle;
    public Toggle partyToggle;
    public Toggle goopToggle;
    public Toggle upToggle;
    public Toggle downToggle;
    [Space]
    public Toggle eternalsToggle;
    [Space]
    public Toggle hatToggle; // ?

    public void SetAllCheckmarks(GameMode gameModes)
    {
        SetDifficultyCheckmarks(gameModes);
        SetTimeCheckmarks(gameModes);
        SetBugtierCheckmarks(gameModes);
        SetPowerupsCheckmarks(gameModes);
        // SetEternalsCheckmarks(gameModes);
        // SetHatCheckmarks(gameModes);
    }

    public void SetDifficultyCheckmarks(GameMode gameModes)
    {
        // Hard.
        bool modeIsOn = (gameModes & GameMode.HARD) == GameMode.HARD;
        hardToggle.isOn = modeIsOn;
        hardToggle.GetComponent<ModifierToggleBehaviour>().staticCheckmark.SetActive(modeIsOn);
        // Harder.
        modeIsOn = (gameModes & GameMode.HARDER) == GameMode.HARDER;
        harderToggle.isOn = modeIsOn;
        harderToggle.GetComponent<ModifierToggleBehaviour>().staticCheckmark.SetActive(modeIsOn);
        // Hardest.
        modeIsOn = (gameModes & GameMode.HARDEST) == GameMode.HARDEST;
        hardestToggle.isOn = modeIsOn;
        hardestToggle.GetComponent<ModifierToggleBehaviour>().staticCheckmark.SetActive(modeIsOn);
        // Harder.
        modeIsOn = (gameModes & GameMode.HARDESTER) == GameMode.HARDESTER;
        hardesterToggle.isOn = modeIsOn;
        hardesterToggle.GetComponent<ModifierToggleBehaviour>().staticCheckmark.SetActive(modeIsOn);
    }

    public void SetTimeCheckmarks(GameMode gameModes)
    {
        // Half.
        bool modeIsOn = (gameModes & GameMode.TIME_HALF) == GameMode.TIME_HALF;
        halfTimeToggle.isOn = modeIsOn;
        halfTimeToggle.GetComponent<ModifierToggleBehaviour>().staticCheckmark.SetActive(modeIsOn);
        // Double.
        modeIsOn = (gameModes & GameMode.TIME_DOUBLE) == GameMode.TIME_DOUBLE;
        doubleTimeToggle.isOn = modeIsOn;
        doubleTimeToggle.GetComponent<ModifierToggleBehaviour>().staticCheckmark.SetActive(modeIsOn);
    }

    public void SetBugtierCheckmarks(GameMode gameModes)
    {
        // Up one.
        bool modeIsOn = (gameModes & GameMode.BUGS_UP) == GameMode.BUGS_UP;
        upOneToggle.isOn = modeIsOn;
        upOneToggle.GetComponent<ModifierToggleBehaviour>().staticCheckmark.SetActive(modeIsOn);
        // Down one.
        modeIsOn = (gameModes & GameMode.BUGS_DOWN) == GameMode.BUGS_DOWN;
        downOneToggle.isOn = modeIsOn;
        downOneToggle.GetComponent<ModifierToggleBehaviour>().staticCheckmark.SetActive(modeIsOn);
        // Max.
        modeIsOn = (gameModes & GameMode.BUGS_MAX) == GameMode.BUGS_MAX;
        maxToggle.isOn = modeIsOn;
        maxToggle.GetComponent<ModifierToggleBehaviour>().staticCheckmark.SetActive(modeIsOn);
        // Min.
        modeIsOn = (gameModes & GameMode.BUGS_MIN) == GameMode.BUGS_MIN;
        minToggle.isOn = modeIsOn;
        minToggle.GetComponent<ModifierToggleBehaviour>().staticCheckmark.SetActive(modeIsOn);
    }

    public void SetPowerupsCheckmarks(GameMode gameModes)
    {
        // Freeze.
        bool modeIsOn = (gameModes & GameMode.POWER_UP_FREEZE) == GameMode.POWER_UP_FREEZE;
        freezeToggle.isOn = modeIsOn;
        freezeToggle.GetComponent<ModifierToggleBehaviour>().staticCheckmark.SetActive(modeIsOn);
        // Poison.
        modeIsOn = (gameModes & GameMode.POWER_UP_POISON) == GameMode.POWER_UP_POISON;
        poisonToggle.isOn = modeIsOn;
        poisonToggle.GetComponent<ModifierToggleBehaviour>().staticCheckmark.SetActive(modeIsOn);
        // Curse.
        modeIsOn = (gameModes & GameMode.POWER_UP_CURSE) == GameMode.POWER_UP_CURSE;
        curseToggle.isOn = modeIsOn;
        curseToggle.GetComponent<ModifierToggleBehaviour>().staticCheckmark.SetActive(modeIsOn);
        // Super frog.
        modeIsOn = (gameModes & GameMode.POWER_UP_SUPERFROG) == GameMode.POWER_UP_SUPERFROG;
        superfrogToggle.isOn = modeIsOn;
        superfrogToggle.GetComponent<ModifierToggleBehaviour>().staticCheckmark.SetActive(modeIsOn);
        // Party.
        modeIsOn = (gameModes & GameMode.POWER_UP_PARTY) == GameMode.POWER_UP_PARTY;
        partyToggle.isOn = modeIsOn;
        partyToggle.GetComponent<ModifierToggleBehaviour>().staticCheckmark.SetActive(modeIsOn);
        // Goop.
        modeIsOn = (gameModes & GameMode.POWER_UP_GOOP) == GameMode.POWER_UP_GOOP;
        goopToggle.isOn = modeIsOn;
        goopToggle.GetComponent<ModifierToggleBehaviour>().staticCheckmark.SetActive(modeIsOn);
        // Up.
        modeIsOn = (gameModes & GameMode.POWER_UP_UP) == GameMode.POWER_UP_UP;
        upToggle.isOn = modeIsOn;
        upToggle.GetComponent<ModifierToggleBehaviour>().staticCheckmark.SetActive(modeIsOn);
        // Down.
        modeIsOn = (gameModes & GameMode.POWER_UP_DOWN) == GameMode.POWER_UP_DOWN;
        downToggle.isOn = modeIsOn;
        downToggle.GetComponent<ModifierToggleBehaviour>().staticCheckmark.SetActive(modeIsOn);
    }

    public void SetEternalsCheckmarks(GameMode gameModes)
    {
        // Eternals.
        bool modeIsOn = (gameModes & GameMode.IGNORE_ETERNALS) == GameMode.IGNORE_ETERNALS;
        eternalsToggle.isOn = modeIsOn;
        eternalsToggle.GetComponent<ModifierToggleBehaviour>().staticCheckmark.SetActive(modeIsOn);
    }

    public void SetHatCheckmarks(GameMode gameModes)
    {
        // Hats. TODO should be handled seperatly? and differently.
    }
}
