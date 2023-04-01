using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ChapterButtonBehaviour : MonoBehaviour
{
    [Header("References - Title & Description")]
    public TextMeshProUGUI titleTextMesh;
    public TextMeshProUGUI descriptionTextMesh;

    [Header("References - NEW")]
    public GameObject newKeywordGameObject;

    [Header("References - Icons")]
    public Transform chapterIconsParent;
    public List<GameObject> tooltipsList;

    public void Initialize(Chapter chapter)
    {
        titleTextMesh.SetText(chapter.chapterData.chapterTitle);
        descriptionTextMesh.SetText(chapter.chapterData.chapterLore[0]);

        bool isChapterNew = true;
        foreach (CharacterCount count in chapter.attemptCountByCharacters)
        {
            isChapterNew &= (count.counter == 0);
        }
        newKeywordGameObject.SetActive(isChapterNew);

        int iconCount = 0;
        foreach (Transform iconChild in chapterIconsParent)
        {
            Image iconImage = iconChild.GetComponent<Image>();
            bool iconExists = (iconCount < chapter.chapterData.icons.Count && chapter.chapterData.icons[iconCount] != null);
            iconImage.gameObject.SetActive(iconExists && !isChapterNew);
            if (iconExists)
            {
                iconImage.sprite = chapter.chapterData.icons[iconCount];
            }
            UpdateTootipText(iconCount, DataManager.instance.GetTooltipForChapterIcon(chapter.chapterData.icons[iconCount]));
            iconCount++;
        }

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
}
