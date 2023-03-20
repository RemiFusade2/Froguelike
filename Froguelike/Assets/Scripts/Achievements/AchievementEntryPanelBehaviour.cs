using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum AchievementState
{
    ACHIEVED,
    NOT_ACHIEVED,
    HIDDEN
}

public class AchievementEntryPanelBehaviour : MonoBehaviour
{
    [Header("References")]
    public Image background;
    [Space]
    public TextMeshProUGUI achievementTitleTextMesh;
    public TextMeshProUGUI achievementRewardTextMesh;
    public TextMeshProUGUI achievementHintTextMesh;
    [Space]
    public Image achievementIcon;
    public Image unlockedIcon;

    [Header("Settings")]
    public Color darkBkgColor;
    public Color lightBkgColor;
    [Space]
    public Color visibleTextColor;
    public Color hiddenTextColor;
    [Space]
    public Sprite achievedSprite;
    public Sprite notAchievedSprite;
    public Sprite hiddenSprite;
    [Space]
    public Sprite hiddenAchievementIconSprite;

    public void Initialize(Achievement achievement, bool darkerBkg, bool hidden = false)
    {
        background.color = darkerBkg ? darkBkgColor : lightBkgColor;

        string achievementTitle = achievement.achievementData.achievementTitle;

        if (hidden)
        {
            // this achievement is secret
            // title is visible but description and reward are hidden
            achievementTitleTextMesh.text = "*That's a secret*";
            achievementRewardTextMesh.text = "Reward: ???";
            achievementHintTextMesh.text = "";
            if (achievement.achievementData.isSecret)
            {
                achievementHintTextMesh.text = $"Hint: {achievementTitle}";
            }

            unlockedIcon.sprite = hiddenSprite;
            achievementIcon.sprite = hiddenAchievementIconSprite;

            achievementTitleTextMesh.color = hiddenTextColor;
            achievementRewardTextMesh.color = hiddenTextColor;
            achievementHintTextMesh.color = hiddenTextColor;
        }
        else
        {
            // this achievement is visible
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

            achievementTitleTextMesh.text = achievementTitle;
            achievementHintTextMesh.text = $"Hint: {achievement.achievementData.achievementDescription}";

            achievementTitleTextMesh.color = visibleTextColor;
            achievementRewardTextMesh.color = visibleTextColor;
            achievementHintTextMesh.color = visibleTextColor;

            if (achievement.unlocked)
            {
                unlockedIcon.sprite = achievedSprite;
                achievementIcon.sprite = achievement.achievementData.achievementUnlockedIcon;
            }
            else
            {
                unlockedIcon.sprite = notAchievedSprite;
                achievementIcon.sprite = achievement.achievementData.achievementLockedIcon;
            }
        }

    }
}
