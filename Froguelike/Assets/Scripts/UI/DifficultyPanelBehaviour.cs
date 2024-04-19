using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DifficultyPanelBehaviour : MonoBehaviour
{
    public Toggle hardToggle;
    public Toggle harderToggle;

    public void SetToggleCheckmarks(GameMode gameMode)
    {
        if (gameMode == GameMode.HARD)
        {
            hardToggle.isOn = true;
            hardToggle.GetComponent<DifficultyToggleBehaviour>().staticCheckmark.SetActive(true);
        }
        else
        {
            hardToggle.isOn = false;
            hardToggle.GetComponent<DifficultyToggleBehaviour>().staticCheckmark.SetActive(false);
        }

        if (gameMode == GameMode.HARDER)
        {
            harderToggle.isOn = true;
            harderToggle.GetComponent<DifficultyToggleBehaviour>().staticCheckmark.SetActive(true);
        }
        else
        {
            harderToggle.isOn = false;
            harderToggle.GetComponent<DifficultyToggleBehaviour>().staticCheckmark.SetActive(false);
        }

        if (gameMode == (GameMode.HARD | GameMode.HARDER))
        {
            hardToggle.isOn = true;
            hardToggle.GetComponent<DifficultyToggleBehaviour>().staticCheckmark.SetActive(true);
            harderToggle.isOn = true;
            harderToggle.GetComponent<DifficultyToggleBehaviour>().staticCheckmark.SetActive(true);
        }

        // TODO this could probably be neater.
    }
}
