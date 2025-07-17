using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class TOCEntryButton : MonoBehaviour, ISelectHandler
{
    public Image iconImage;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI dotsText;
    public TextMeshProUGUI numberText;
    public Button TOCEntryButtonGO;

    public void Initialize(ChapterData chapter, int nrInList, int nrInStory)
    {
        // Set symbol.
        // "Chapter completed" icon if everything has been found in that chapter + the chapter is not needed in any quest
        iconImage.gameObject.SetActive(false);

        bool thereIsSomethingToUnlockInStoryline = ChapterManager.instance.DoesChapterUnlockAnAchievementOrAnUnplayedChapter(ChapterManager.instance.GetChapterFromID(chapter.chapterID), RunManager.instance.GetChapterCount());
        thereIsSomethingToUnlockInStoryline |= ChapterManager.instance.DoesChapterContainFixedItemsThatHaveNeverBeenFound(ChapterManager.instance.GetChapterFromID(chapter.chapterID));
        iconImage.gameObject.SetActive(!thereIsSomethingToUnlockInStoryline);

        // Set title.
        titleText.SetText(chapter.chapterTitle.ToString());
        int titleWidth = (int)titleText.preferredWidth;
        titleWidth += SettingsManager.instance.GetCurrentFontAsset().name.Contains("Liberation") ? 1 : 0;
        titleText.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, titleWidth);

        // Set number in list.
        numberText.SetText(nrInList.ToString());
        numberText.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, (int)numberText.preferredWidth);

        // Set dots.
        int dotsXSize = (int)dotsText.GetComponentInParent<RectTransform>().sizeDelta.x - (int)titleText.rectTransform.sizeDelta.x - Mathf.Abs((int)titleText.rectTransform.localPosition.x) - (int)numberText.rectTransform.sizeDelta.x - Mathf.Abs((int)numberText.rectTransform.localPosition.x);
        int dotsXPosition = -(int)numberText.rectTransform.sizeDelta.x - Mathf.Abs((int)numberText.rectTransform.localPosition.x);
        int correctionForX = (int)numberText.rectTransform.sizeDelta.x % 4;
        correctionForX += correctionForX == 0 ? 2 : correctionForX == 2 ? -2 : 0;
        dotsXPosition -= correctionForX;

        dotsXSize -= correctionForX;
        dotsXSize += 2;

        dotsText.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, dotsXSize);
        dotsText.rectTransform.localPosition = new Vector3(dotsXPosition, dotsText.rectTransform.localPosition.y, dotsText.rectTransform.localPosition.z);
    }

    public void OnSelect(BaseEventData eventData)
    {
        SoundManager.instance.PlayButtonSound(TOCEntryButtonGO);
    }
}
