using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Steamworks;
using UnityEngine.SocialPlatforms.Impl;
using UnityEngine.SocialPlatforms;

public class ScoreAchievementPanelBehaviour : MonoBehaviour
{
    public TextMeshProUGUI achievementConditionTextMesh;
    public TextMeshProUGUI achievementRewardTextMesh;

    public Image achievementIcon;

    public void Initialize(Achievement achievement)
    {
        achievementConditionTextMesh.text = achievement.GetAchievementDescription();
        
        achievementRewardTextMesh.text = $"Reward: {achievement.GetRewardDescription()}";
        
        achievementIcon.sprite = (achievement.achievementData.achievementUnlockedIcon != null) ? achievement.achievementData.achievementUnlockedIcon : DataManager.instance.achievementUnlockedDefaultSprite;
        achievementIcon.SetNativeSize();
    }

    public void Initialize(string conditionText, string rewardText, Sprite sprite)
    {
        achievementConditionTextMesh.text = conditionText;
        achievementRewardTextMesh.text = $"Reward: {rewardText}";
        achievementIcon.sprite = sprite;

    }

}
