using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class GameOverScreen : MonoBehaviour
{
    public string countPrefix;
    public TextMeshProUGUI respawnCountText;

    public void UpdateGameOverScreen()
    {
        respawnCountText.SetText(countPrefix + RunManager.instance.player.revivals.ToString());
    }
}
