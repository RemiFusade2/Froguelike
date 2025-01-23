using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class GameOverScreen : MonoBehaviour
{
    public string countPrefix;
    public bool addExtraLifeSymbol = true;
    public TextMeshProUGUI respawnCountText;

    public void UpdateGameOverScreen()
    {
        respawnCountText.SetText(countPrefix + (addExtraLifeSymbol ? $"{DataManager.instance.extraLifeSymbol} " : "") + RunManager.instance.player.revivals.ToString());
    }
}
