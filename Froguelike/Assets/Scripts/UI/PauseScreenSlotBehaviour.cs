using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PauseScreenSlotBehaviour : MonoBehaviour
{
    public GameObject usedSlot;
    public Image itemIcon;
    public GameObject markerParent;
    public GameObject markerPrefab;

    public void UpdateSlot(RunItemInfo runItem)
    {
        usedSlot.SetActive(true);
        itemIcon.sprite = runItem.GetRunItemData().icon;

        while (markerParent.transform.childCount < runItem.level)
        {
            Instantiate(markerPrefab, markerParent.transform);
        }
    }
}
