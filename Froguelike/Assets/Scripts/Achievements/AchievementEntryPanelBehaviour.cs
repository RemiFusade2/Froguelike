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
    public Image checkboxImage;
    [Space]
    public TextMeshProUGUI achievementCountTextMesh;
    public Slider achievementCountSlider;

    [Header("Settings")]
    public Color darkBkgColor;
    public Color lightBkgColor;
    [Space]
    public Color visibleTextColor;
    public Color hiddenTextColor;
    [Space]
    public Sprite achievedSprite;
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
        if (achievement.unlocked)
        {
            achievementIconImage.sprite = (achievement.achievementData.achievementUnlockedIcon != null) ? achievement.achievementData.achievementUnlockedIcon : DataManager.instance.achievementUnlockedDefaultSprite;
        }
        else
        {
            achievementIconImage.sprite = (achievement.achievementData.achievementLockedIcon != null) ? achievement.achievementData.achievementLockedIcon : DataManager.instance.achievementLockedDefaultSprite;
        }
        achievementIconImage.color = (achievementIconImage.sprite == null) ? new Color(0, 0, 0, 0) : Color.white;
    }

    public void Initialize(Achievement achievement, bool darkerBkg, bool accessible)
    {
        background.color = darkerBkg ? darkBkgColor : lightBkgColor;
        AchievementData achievementData = achievement.achievementData;

        //string achievementTitle = achievement.achievementData.achievementTitle;

        // If current build is Demo build and achievement is not part of demo
        bool isDemoBuildAndAchievementIsNotPartOfDemo = (BuildManager.instance.demoBuild && !achievement.achievementData.partOfDemo);

        if (achievement.unlocked)
        {
            // The achievement has been unlocked already
            SetTextColor(visibleTextColor);
            achievementTitleTextMesh.text = achievement.achievementData.achievementTitle;
            achievementHintTextMesh.text = $"How: {achievement.GetAchievementDescription()}";
            achievementRewardTextMesh.text = $"Reward: {achievement.GetRewardDescription()}";

            // Set count to full.
            if (achievement.achievementData.conditionsList[0].specialKey == AchievementConditionSpecialKey.EAT_20000_BUGS)
            {
                achievementCountTextMesh.text = "20000/20000";
                achievementCountSlider.value = 1;
            }
            else if (achievement.achievementData.conditionsList[0].specialKey == AchievementConditionSpecialKey.DIE_A_BUNCH_OF_TIMES)
            {
                achievementCountTextMesh.text = "10/10";
                achievementCountSlider.value = 1;
            }

            checkboxImage.gameObject.SetActive(true);
            checkboxImage.sprite = achievedSprite;
            SetAchievementIcon(achievement);
        }
        else if (isDemoBuildAndAchievementIsNotPartOfDemo)
        {
            // The achievement is not part of the demo
            SetTextColor(hiddenTextColor);
            achievementTitleTextMesh.text = "";
            achievementHintTextMesh.text = "*Not part of the demo*";
            achievementRewardTextMesh.text = "";

            checkboxImage.gameObject.SetActive(false);
            achievementIconImage.sprite = hiddenAchievementIconSprite;
        }
        else if (!accessible)
        {
            // The achievement can't be unlocked.
            SetTextColor(hiddenTextColor);
            achievementTitleTextMesh.text = "";
            achievementHintTextMesh.text = "*Complete other stuff first*";
            achievementRewardTextMesh.text = "";

            checkboxImage.gameObject.SetActive(false);
            achievementIconImage.sprite = hiddenAchievementIconSprite;
        }
        else if (achievement.achievementData.isSecret)
        {
            // The achievement is secret. Title is visible but description and reward are hidden
            SetTextColor(hiddenTextColor);
            achievementTitleTextMesh.text = "*That's a secret*";
            achievementHintTextMesh.text = "";
            achievementRewardTextMesh.text = "Reward: ???";
            if (achievement.achievementData.isSecret)
            {
                achievementHintTextMesh.text = $"Hint: {achievement.achievementData.achievementTitle}";
            }
            checkboxImage.gameObject.SetActive(true);
            checkboxImage.sprite = notAchievedSprite;
            achievementIconImage.sprite = hiddenAchievementIconSprite;
        }
        else
        {
            // The achievement is not unlocked but is visible
            SetTextColor(visibleTextColor);
            achievementTitleTextMesh.text = achievement.achievementData.achievementTitle;
            achievementHintTextMesh.text = $"How: {achievement.GetAchievementDescription()}";
            achievementRewardTextMesh.text = $"Reward: {achievement.GetRewardDescription()}";

            // Set up count.
            if (achievement.achievementData.conditionsList[0].specialKey == AchievementConditionSpecialKey.EAT_20000_BUGS)
            {
                int eatenBugsCapped = Mathf.Clamp(GameManager.instance.gameData.cumulatedScore, 0, 20000);
                achievementCountTextMesh.text = $"{eatenBugsCapped}/20000";
                // The percentage for this quest is a special case to make sure there is always some progress visible if some bugs have been eaten and that the bar doesn't fill up before the goal is met.
                float eatenBugsPercentage = eatenBugsCapped > 0 ? Mathf.Max(eatenBugsCapped / 20000f, 0.005f) : 0f;
                eatenBugsPercentage = eatenBugsPercentage == 1 ? 0.995f : eatenBugsPercentage;
                achievementCountSlider.value = eatenBugsPercentage;
            }
            else if (achievement.achievementData.conditionsList[0].specialKey == AchievementConditionSpecialKey.DIE_A_BUNCH_OF_TIMES)
            {
                int deathCountCapped = Mathf.Clamp(GameManager.instance.gameData.deathCount, 0, 10);
                achievementCountTextMesh.text = $"{deathCountCapped}/10";
                float deathCountPercentage = deathCountCapped / 10f;
                achievementCountSlider.value = deathCountPercentage;
            }

            checkboxImage.gameObject.SetActive(true);
            checkboxImage.sprite = notAchievedSprite;
            SetAchievementIcon(achievement);
        }
    }

    public void ClickOnDemoButton()
    {
        Application.OpenURL("https://store.steampowered.com/app/2315020/Froguelike/");
    }
}
