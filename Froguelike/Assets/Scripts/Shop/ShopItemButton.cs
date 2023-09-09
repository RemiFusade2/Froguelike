using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class ShopItemButton : MonoBehaviour, ISelectHandler
{
    [Header("References")]
    public TextMeshProUGUI itemNameText;
    public TextMeshProUGUI itemDescriptionText;
    public Image itemIconImage;
    [Space]
    public Transform levelPanelParent;
    [Space]
    public Button buyButton;
    public TextMeshProUGUI priceText;

    [Header("Scrollview")]
    public ScrollRect scrollView;
    public RectTransform viewportRT;
    public RectTransform thisRT;
    public Transform itemPanelTransform;

    private const float gap = 10;

    public void Initialize(ShopItem item, bool availableButCantBuy)
    {
        buyButton.interactable = true;
        buyButton.gameObject.SetActive(true);

        if (availableButCantBuy)
        {
            var cantBuyColor = buyButton.colors;
            cantBuyColor.selectedColor = Color.red;
            buyButton.colors = cantBuyColor;
        }


        itemNameText.text = item.itemName;
        itemDescriptionText.text = item.data.description;
        itemIconImage.sprite = item.data.icon;

        int level = 0;
        foreach (Transform levelChild in levelPanelParent)
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

        name = item.itemName;

        scrollView = transform.parent.parent.parent.parent.GetComponent<ScrollRect>();
        viewportRT = transform.parent.parent.parent.GetComponent<RectTransform>();
        thisRT = GetComponent<RectTransform>();
        itemPanelTransform = transform.parent;
    }

    public void OnSelect(BaseEventData eventData)
    {
        // Scroll the button into view.
        StartCoroutine(ScrollButtonIntoView());
    }

    private IEnumerator ScrollButtonIntoView()
    {
        // Wait for layout to recompute before getting this buttons position.
        yield return new WaitForSeconds(0.1f);

        float safeArea = (viewportRT.rect.height - thisRT.rect.height) / 2 - gap;
        float currentY = itemPanelTransform.localPosition.y + thisRT.localPosition.y;
        float newY = Mathf.Clamp(currentY, -safeArea, safeArea);
        itemPanelTransform.localPosition += (newY - currentY) * Vector3.up;
    }
}
