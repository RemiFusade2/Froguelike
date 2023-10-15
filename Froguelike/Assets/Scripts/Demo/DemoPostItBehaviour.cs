using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DemoPostItBehaviour : MonoBehaviour
{
    public bool useSteamOverlay = true;

    public void ClickOnDemoButton()
    {
        if (useSteamOverlay && SteamManager.Initialized)
        {
            Steamworks.AppId_t appId = new Steamworks.AppId_t(2315020);
            Steamworks.SteamFriends.ActivateGameOverlayToStore(appId, Steamworks.EOverlayToStoreFlag.k_EOverlayToStoreFlag_None);
        }
        else
        {
            Application.OpenURL("https://store.steampowered.com/app/2315020/Froguelike/");
        }
    }
}
