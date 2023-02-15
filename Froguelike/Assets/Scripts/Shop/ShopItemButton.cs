using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ShopItemButton : MonoBehaviour
{
    [Header("References")]
    public Text itemNameText;
    public Text itemDescriptionText;
    public Image itemIconImage;
    [Space]
    public Transform levelPanelParent;
    [Space]
    public Button buyButton;
    public Text priceText;

    public void Initialize(ShopItem item, bool itemIsAvailable)
    {
        buyButton.interactable = itemIsAvailable;
        buyButton.gameObject.SetActive(true);

        itemNameText.text = item.itemName;
        itemDescriptionText.text = item.data.description;
        itemIconImage.sprite = item.data.icon;

        int level = 0;
        foreach(Transform levelChild in levelPanelParent)
        {
            bool levelIsActive = (level < item.currentLevel);
            bool levelExists = (level < item.maxLevel);
            levelChild.GetChild(0).GetComponent<Image>().enabled = levelIsActive;
            levelChild.GetComponent<Image>().enabled = levelExists;
            level++;
        }

        if (item.currentLevel < item.data.costForEachLevel.Count)
        {
            priceText.text = Tools.FormatCurrency(item.data.costForEachLevel[item.currentLevel], DataManager.instance.currencySymbol);
        }
    }
}
