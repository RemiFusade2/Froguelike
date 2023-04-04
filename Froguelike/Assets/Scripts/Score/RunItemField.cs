using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RunItemField : MonoBehaviour
{
    [Header("References")]
    public TextMeshProUGUI itemNameText;
    public TextMeshProUGUI itemLevelText;
    public Image itemIconImage;
    public Image background;
    [Space]
    public List<Sprite> backgrounds;

    public void Initialize(RunWeaponInfo item, int backgroundIndex)
    {
        itemNameText.text = item.itemName;
        itemLevelText.text = item.level.ToString();
        itemIconImage.sprite = item.weaponItemData.icon;
        background.sprite = backgrounds[backgroundIndex];
    }

    public void Initialize(RunStatItemInfo item, int backgroundIndex)
    {
        itemNameText.text = item.itemName;
        itemLevelText.text = item.level.ToString();
        itemIconImage.sprite = item.itemData.icon;
        background.sprite = backgrounds[backgroundIndex];
    }
}
