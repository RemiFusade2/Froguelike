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
    public Image achievementIconImage;
    [Space]
    public Image unlockedFrameIconImage;
    public Image unlockedIconImage;
    [Space]
    public GameObject demoMessage;

    [Header("Settings")]
    public Color darkBkgColor;
    public Color lightBkgColor;
    [Space]
    public Color visibleTextColor;
    public Color hiddenTextColor;
    [Space]
    public Sprite achievedFrameSprite;
    public Sprite achievedSprite;
    public Sprite notAchievedFrameSprite;
    public Sprite notAchievedSprite;
    [Space]
    public Sprite hiddenAchievementIconSprite;

    private void SetTextColor(Color color)
    {
        achievementTitleTextMesh.color = color;
        achievementRewardTextMesh.color = color;
        achievementHintTextMesh.color = color;
    }

    private void SetAchievementIcon(Achievement achievement)
    {
        achievementIconImage.sprite = achievement.achievementData.achievementLockedIcon;
        achievementIconImage.color = (achievement.achievementData.achievementLockedIcon == null) ? new Color(0, 0, 0, 0) : Color.white;
    }

    public void Initialize(Achievement achievement, bool darkerBkg, bool accessible)
    {
        background.color = darkerBkg ? darkBkgColor : lightBkgColor;
        AchievementData achievementData = achievement.achievementData;

        //string achievementTitle = achievement.achievementData.achievementTitle;

        // If current build is Demo build and achievement is not part of demo
        bool isDemoBuildAndAchievementIsNotPartOfDemo = (GameManager.instance.demoBuild && !achievement.achievementData.partOfDemo);
        demoMessage.SetActive(isDemoBuildAndAchievementIsNotPartOfDemo);

        if (achievement.unlocked)
        {
            // The achievement has been unlocked already
            SetTextColor(visibleTextColor);
            achievementTitleTextMesh.text = achievement.achievementData.achievementTitle;            
            achievementRewardTextMesh.text = $"Reward: {achievement.GetRewardDescription()}";
            achievementHintTextMesh.text = $"Hint: {achievement.GetAchievementDescription()}";
            unlockedFrameIconImage.gameObject.SetActive(true);
            unlockedFrameIconImage.sprite = achievedFrameSprite;
            unlockedIconImage.sprite = achievedSprite;
            SetAchievementIcon(achievement);
        }
        else if (!accessible)
        {
            // The achievement can't be unlocked.
            SetTextColor(hiddenTextColor);
            achievementTitleTextMesh.text = "";
            achievementRewardTextMesh.text = "*Complete other stuff first*"; ;
            achievementHintTextMesh.text = "";

            unlockedFrameIconImage.gameObject.SetActive(false);
            achievementIconImage.sprite = hiddenAchievementIconSprite;
        }
        else if (achievement.achievementData.isSecret)
        {
            // The achievement is secret. Title is visible but description and reward are hidden
            SetTextColor(hiddenTextColor);
            achievementTitleTextMesh.text = "*That's a secret*";
            achievementRewardTextMesh.text = "Reward: ???";
            achievementHintTextMesh.text = "";
            if (achievement.achievementData.isSecret)
            {
                achievementHintTextMesh.text = $"Hint: {achievement.achievementData.achievementTitle}";
            }
            unlockedFrameIconImage.gameObject.SetActive(true);
            unlockedFrameIconImage.sprite = notAchievedFrameSprite;
            unlockedIconImage.sprite = notAchievedSprite;
            achievementIconImage.sprite = hiddenAchievementIconSprite;
        }
        else
        {
            // The achievement is not unlocked but is visible
            SetTextColor(visibleTextColor);
            achievementTitleTextMesh.text = achievement.achievementData.achievementTitle;
            achievementRewardTextMesh.text = $"Reward: {achievement.GetRewardDescription()}";
            achievementHintTextMesh.text = $"Hint: {achievement.GetAchievementDescription()}";
            unlockedFrameIconImage.gameObject.SetActive(true);
            unlockedFrameIconImage.sprite = notAchievedFrameSprite;
            unlockedIconImage.sprite = notAchievedSprite;
            SetAchievementIcon(achievement);
        }
    }

    public void ClickOnDemoButton()
    {
        Application.OpenURL("https://store.steampowered.com/app/2315020/Froguelike/");
    }
}
