using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ScoreAchievementPanelBehaviour : MonoBehaviour
{
    public TextMeshProUGUI achievementConditionTextMesh;
    public TextMeshProUGUI achievementRewardTextMesh;

    public Image achievementIcon;

    public void Initialize(Achievement achievement)
    {
        achievementConditionTextMesh.text = achievement.achievementData.achievementDescription;

        string rewardDescription = achievement.achievementData.reward.rewardDescription;
        switch (achievement.achievementData.reward.rewardType)
        {
            case AchievementRewardType.CHARACTER:
                rewardDescription = rewardDescription.Replace("characterName", achievement.achievementData.reward.character.characterName);
                break;
            case AchievementRewardType.RUN_ITEM:
                rewardDescription = rewardDescription.Replace("itemName", achievement.achievementData.reward.runItem.itemName);
                break;
            case AchievementRewardType.SHOP_ITEM:
                rewardDescription = rewardDescription.Replace("itemName", achievement.achievementData.reward.shopItem.itemName);
                break;
            default:
                break;
        }
        achievementRewardTextMesh.text = $"Reward: {rewardDescription}";

        achievementIcon.sprite = achievement.achievementData.achievementUnlockedIcon;
    }

}
