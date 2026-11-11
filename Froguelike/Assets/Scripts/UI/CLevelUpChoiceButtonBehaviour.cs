using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CLevelUpChoiceButtonBehaviour : MonoBehaviour
{
    private UILevelUpChoicePanel uILevelUpChoicePanel;

    public void SetUILevelUpChoicePanel(UILevelUpChoicePanel panel)
    {
        uILevelUpChoicePanel = panel;
        uILevelUpChoicePanel.SetSubStyle("Normal");
    }

    public void SetButtonAnimation(string animationName)
    {
        if (uILevelUpChoicePanel != null)
            uILevelUpChoicePanel.SetSubStyle(animationName);
    }
}
