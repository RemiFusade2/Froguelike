using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DifficultyPanelBehaviour : MonoBehaviour
{
    public Toggle hardToggle;
    public Toggle harderToggle;
    public Toggle hardestToggle;
    public Toggle hardesterToggle;

    public void SetToggleCheckmarks(GameMode gameModes)
    {
        bool hardModeIsOn = (gameModes & GameMode.HARD) == GameMode.HARD;
        hardToggle.isOn = hardModeIsOn;
        hardToggle.GetComponent<DifficultyToggleBehaviour>().staticCheckmark.SetActive(hardModeIsOn);

        bool harderModeIsOn = (gameModes & GameMode.HARDER) == GameMode.HARDER;
        harderToggle.isOn = harderModeIsOn;
        harderToggle.GetComponent<DifficultyToggleBehaviour>().staticCheckmark.SetActive(harderModeIsOn);

        bool hardestModeIsOn = (gameModes & GameMode.HARDEST) == GameMode.HARDEST;
        hardestToggle.isOn = hardestModeIsOn;
        hardestToggle.GetComponent<DifficultyToggleBehaviour>().staticCheckmark.SetActive(hardestModeIsOn);

        bool hardesterModeIsOn = (gameModes & GameMode.HARDESTER) == GameMode.HARDESTER;
        hardesterToggle.isOn = hardesterModeIsOn;
        hardesterToggle.GetComponent<DifficultyToggleBehaviour>().staticCheckmark.SetActive(hardesterModeIsOn);
    }
}
