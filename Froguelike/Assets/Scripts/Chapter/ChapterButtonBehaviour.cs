using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ChapterButtonBehaviour : MonoBehaviour
{
    [Header("References - Title & Description")]
    public TextMeshProUGUI titleTextMesh;

    [Header("References - NEW")]
    public GameObject newKeywordGameObject;

    [Header("References - Something to unlock")]
    public GameObject chapterCompletedIcon;
    public GameObject chapterCompletedTooltip;

    [Header("References - Icons")]
    public Transform chapterIconsParent;
    public List<GameObject> tooltipsList;

    [Header("References - Animation")]
    public List<Image> frameImages;
    public List<Image> frameAnimationImages;
    public TextMeshProUGUI newText;
    public Image backgroundImage;
    [Space]
    public Color chapterDisplayedFrameColor;
    public Color chapterDisplayedBackgroundColor;
    public Color chapterDisplayedFrameAnimationColor;
    public Color chapterDisplayedBackgroundAnimationColor;
    public Color chapterNewTextDisplayedColor;
    [Space]
    public Color chapterNormalFrameColor;
    public Color chapterNormalBackgroundColor;
    public Color chapterNormalFrameAnimationColor;
    public Color chapterNormalBackgroundAnimationColor;
    public Color chapterNewTextNormalColor;
    [Space]
    public const string somethingToUnlockTooltipString = "There is still something to unlock in that storyline";

    private bool isDisplayed = false;

    public void Initialize(Chapter chapter)
    {
        // Title and description
        titleTextMesh.SetText(chapter.chapterData.chapterTitle);

        // "New" keyword if chapter has never been played
        bool isChapterNew = true;
        foreach (CharacterCount count in chapter.attemptCountByCharacters)
        {
            isChapterNew &= (count.counter == 0);
        }
        newKeywordGameObject.SetActive(isChapterNew);

        // "Chapter completed" icon if everything has been found in that chapter + the chapter is not needed in any quest
        chapterCompletedIcon.SetActive(false);
        chapterCompletedTooltip.SetActive(false);
        if (!isChapterNew)
        {
            bool thereIsSomethingToUnlockInStoryline = ChapterManager.instance.DoesChapterUnlockAnAchievementOrAnUnplayedChapter(chapter, RunManager.instance.GetChapterCount());
            thereIsSomethingToUnlockInStoryline |= ChapterManager.instance.DoesChapterContainFixedItemsThatHaveNeverBeenFound(chapter);
            chapterCompletedIcon.SetActive(!thereIsSomethingToUnlockInStoryline);
            chapterCompletedTooltip.SetActive(false);
        }

        // Other icons (deactivated for now)
        int iconCount = 0;
        foreach (Transform iconChild in chapterIconsParent)
        {
            Image iconImage = iconChild.GetComponent<Image>();
            bool iconExists = (iconCount < chapter.chapterData.icons.Count && chapter.chapterData.icons[iconCount] != null);
            iconImage.gameObject.SetActive(iconExists && !isChapterNew);
            if (iconExists)
            {
                iconImage.sprite = chapter.chapterData.icons[iconCount];
                UpdateTootipText(iconCount, DataManager.instance.GetTooltipForChapterIcon(chapter.chapterData.icons[iconCount]));
            }
            iconCount++;
        }

        // Make sure all tooltips are off
        foreach (GameObject tooltip in tooltipsList)
        {
            tooltip.SetActive(false);
        }
    }

    private void SetTooltipActive(int index, bool active)
    {
        if (index >= 0 && index < tooltipsList.Count)
        {
            tooltipsList[index].SetActive(active);
        }
        else if (index == -1)
        {
            chapterCompletedTooltip.SetActive(active);
        }
    }

    private void UpdateTootipText(int index, string tooltip)
    {
        if (index >= 0 && index < tooltipsList.Count && tooltipsList[index] != null && tooltipsList[index].GetComponentInChildren<TextMeshProUGUI>() != null)
        {
            tooltipsList[index].GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 5 * tooltip.Length);
            tooltipsList[index].GetComponentInChildren<TextMeshProUGUI>().SetText(tooltip);
        }
    }

    public void DisplayTooltip(int index)
    {
        SetTooltipActive(index, true);
    }

    public void HideTooltip(int index)
    {
        SetTooltipActive(index, false);
    }

    public void SetDisplayedColor()
    {
        backgroundImage.color = chapterDisplayedBackgroundColor;

        foreach (Image frameImage in frameImages)
        {
            frameImage.color = chapterDisplayedFrameColor;
        }

        foreach (Image frameAnimationImage in frameAnimationImages)
        {
            frameAnimationImage.color = chapterDisplayedFrameAnimationColor;
        }

        newText.color = chapterNewTextDisplayedColor;

        isDisplayed = true;
    }

    // Called from select and deselect events on the buttons.
    public void SetHighlightedColor(bool highlight)
    {
        if (highlight)
        {
            if (isDisplayed)
            {
                backgroundImage.color = chapterDisplayedBackgroundAnimationColor;
            }
            else
            {
                backgroundImage.color = chapterNormalBackgroundAnimationColor;
            }
        }
        else
        {
            if (isDisplayed)
            {
                backgroundImage.color = chapterDisplayedBackgroundColor;
            }
            else
            {
                backgroundImage.color = chapterNormalBackgroundColor;
            }
        }
    }

    public void SetNormalColor()
    {
        backgroundImage.color = chapterNormalBackgroundColor;

        foreach (Image frameImage in frameImages)
        {
            frameImage.color = chapterNormalFrameColor;
        }

        foreach (Image frameAnimationImage in frameAnimationImages)
        {
            frameAnimationImage.color = chapterNormalFrameAnimationColor;
        }

        newText.color = chapterNewTextNormalColor;

        isDisplayed = false;
    }
}
