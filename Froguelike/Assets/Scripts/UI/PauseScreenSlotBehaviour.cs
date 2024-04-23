using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PauseScreenSlotBehaviour : MonoBehaviour
{
    public GameObject usedSlot;
    public Image itemIcon;
    public GameObject markerParent;
    public GameObject markerPrefab;
    public GameObject confetti;
    public GameObject overlevelBG;
    public TextMeshProUGUI overlevelText;

    public void UpdateSlot(RunItemInfo runItem)
    {
        usedSlot.SetActive(true);
        itemIcon.sprite = runItem.GetRunItemData().icon;
        int maxLevel = runItem.GetRunItemData().GetMaxLevel();

        // Confetti.
        confetti.SetActive(runItem.level >= maxLevel);

        if (runItem.level > maxLevel)
        {
            // Overlevel.
            overlevelBG.SetActive(true);
            overlevelText.SetText(runItem.level.ToString());
        }
        else
        {
            // Level indicators.
            while (markerParent.transform.childCount < runItem.level)
            {
                Instantiate(markerPrefab, markerParent.transform);
            }
        }
    }
}
