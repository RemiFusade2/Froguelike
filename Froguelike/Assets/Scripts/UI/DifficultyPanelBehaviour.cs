using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DifficultyPanelBehaviour : MonoBehaviour
{
    public Toggle hardToggle;
    public Toggle harderToggle;

    public void SetToggleCheckmarks(GameMode gameModes)
    {
        bool hardModeIsOn = (gameModes & GameMode.HARD) == GameMode.HARD;
        hardToggle.isOn = hardModeIsOn;
        hardToggle.GetComponent<DifficultyToggleBehaviour>().staticCheckmark.SetActive(hardModeIsOn);

        bool harderModeIsOn = (gameModes & GameMode.HARDER) == GameMode.HARDER;
        harderToggle.isOn = harderModeIsOn;
        harderToggle.GetComponent<DifficultyToggleBehaviour>().staticCheckmark.SetActive(harderModeIsOn);
    }
}
