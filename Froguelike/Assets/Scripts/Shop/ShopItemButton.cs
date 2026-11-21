using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using Unity.VisualScripting;
using Steamworks;

public class ShopItemButton : MonoBehaviour, ISelectHandler, IPointerEnterHandler, IDeselectHandler
{
    [Header("References")]
    public TextMeshProUGUI itemNameText;
    public TextMeshProUGUI itemDescriptionText;
    public Image itemIconImage;
    [Space]
    public Transform levelPanelParent;
    public GameObject levelPrefab;
    public GameObject levelLockedPrefab;
    public Sprite levelBoughtSprite;
    public Sprite levelUnavailableSprite;
    [Space]
    public Button buyButton;
    public TextMeshProUGUI priceText;
    public Animator animator;
    [Space]
    public GameObject soldOutImage;
    public GameObject noRefundImage;

    [Header("Scrollview")]
    public ScrollRect scrollView;
    public RectTransform viewportRT;
    public RectTransform thisRT;
    public Transform itemPanelTransform;

    private const float gap = 10;

    private bool selectUsingMouseCursor = false;

    private Coroutine changeButtonIconCoroutine;

    public void Start()
    {
        if (name == "Eternal")
        {
            if (changeButtonIconCoroutine != null)
            {
                StopCoroutine(changeButtonIconCoroutine);
            }
            changeButtonIconCoroutine = StartCoroutine(ChangeButtonIcon(0.2f));
        }
    }

    #region Permanent upgrade button

    private IEnumerator ChangeButtonIcon(float delay)
    {
        List<RunPermanentUpgradeItemData> permanentUpgradesItemDataList = RunItemManager.instance.allPermanentUpgradeRunItemsScriptableObjects;
        List<CharacterData> characterDataList = CharacterManager.instance.charactersScriptableObjectsList;
        while (true)
        {
            bool characterOrItemIsUnlocked = false;
            bool newIconIsDifferentFromPreviousIcon = false;
            do
            {
                if (true) // We could be using "Random.Range(0,2) == 0" instead, but the frog sprite doesn't fit the image size
                {
                    RunPermanentUpgradeItemData itemData = permanentUpgradesItemDataList[Random.Range(0, permanentUpgradesItemDataList.Count)];
                    characterOrItemIsUnlocked = RunItemManager.instance.IsRunItemUnlocked(itemData.itemName);
                    newIconIsDifferentFromPreviousIcon = (itemIconImage.sprite != itemData.icon);
                    itemIconImage.sprite = itemData.icon;
                }
                else
                {
                    CharacterData characterData = characterDataList[Random.Range(0, characterDataList.Count)];
                    characterOrItemIsUnlocked = CharacterManager.instance.IsCharacterUnlocked(characterData.characterID);
                    itemIconImage.sprite = characterData.characterSprite;
                }
            } while (!characterOrItemIsUnlocked || !newIconIsDifferentFromPreviousIcon);

            yield return new WaitForSecondsRealtime(delay);
        }
    }

    public void InitializeForPermanentUpgradeButton(bool availableButCantBuy, int cost)
    {
        buyButton.interactable = true;
        buyButton.gameObject.SetActive(true);

        if (soldOutImage != null)
        {
            soldOutImage.SetActive(!availableButCantBuy);
        }
        if (noRefundImage != null)
        {
            noRefundImage.SetActive(true);
        }

        if (availableButCantBuy)
        {
            var cantBuyColor = buyButton.colors;
            cantBuyColor.selectedColor = Color.red;
            buyButton.colors = cantBuyColor;
        }

        itemNameText.text = "Eternal";
        itemDescriptionText.text = "1% increase to a random stat for a random frog";

        foreach (Transform levelChild in levelPanelParent)
        {
            levelChild.gameObject.SetActive(false);
            Destroy(levelChild.gameObject);
        }

        priceText.text = Tools.FormatCurrency(cost, DataManager.instance.currencySymbol);

        name = "Eternal";

        scrollView = transform.parent.parent.parent.parent.GetComponent<ScrollRect>();
        viewportRT = transform.parent.parent.parent.GetComponent<RectTransform>();
        thisRT = GetComponent<RectTransform>();
        itemPanelTransform = transform.parent;
    }

    #endregion

    public void Initialize(ShopItem item, bool availableButCantBuy, int extraFee)
    {
        buyButton.interactable = true;
        buyButton.gameObject.SetActive(true);

        if (soldOutImage != null)
        {
            soldOutImage.SetActive(!availableButCantBuy);
        }
        if (noRefundImage != null)
        {
            noRefundImage.SetActive(false);
        }

        if (availableButCantBuy)
        {
            var cantBuyColor = buyButton.colors;
            cantBuyColor.selectedColor = Color.red;
            buyButton.colors = cantBuyColor;
        }

        itemNameText.text = item.itemName;
        itemDescriptionText.text = item.data.description;
        itemIconImage.sprite = item.data.icon;

        foreach (Transform levelChild in levelPanelParent)
        {
            levelChild.gameObject.SetActive(false);
            Destroy(levelChild.gameObject);
        }

        int futureRestocksCount = AchievementManager.instance.GetLockedRestocksForItem(item.data);

        for (int i = 0; i < item.GetMaxLevel(); i++)
        {
            bool levelIsBought = i < item.currentLevel;

            GameObject levelBox = Instantiate(levelPrefab, levelPanelParent);
            levelBox.transform.GetChild(1).GetComponent<Image>().sprite = levelIsBought ? levelBoughtSprite : null;
            levelBox.transform.GetChild(1).GetComponent<Image>().enabled = levelIsBought;
        }

        for (int i = 0; i < futureRestocksCount; i++)
        {
            // Locked restocks
            GameObject levelBox = Instantiate(levelLockedPrefab, levelPanelParent);
            levelBox.transform.GetChild(1).GetComponent<Image>().sprite = null;
            levelBox.transform.GetChild(1).GetComponent<Image>().enabled = false;
        }

        if (item.currentLevel < item.data.costForEachLevel.Count)
        {
            int cost = item.data.costForEachLevel[item.currentLevel] + extraFee;
            priceText.text = Tools.FormatCurrency(cost, DataManager.instance.currencySymbol);
        }

        name = item.itemName;

        scrollView = transform.parent.parent.parent.parent.GetComponent<ScrollRect>();
        viewportRT = transform.parent.parent.parent.GetComponent<RectTransform>();
        thisRT = GetComponent<RectTransform>();
        itemPanelTransform = transform.parent;
    }

    public void OnSelect(BaseEventData eventData)
    {
        SoundManager.instance.PlayButtonSound(buyButton);
        if (ShopManager.instance.logsVerboseLevel == VerboseLevel.MAXIMAL)
        {
            Debug.Log($"Shop Item Button - OnSelect(). eventData = {eventData}");
        }

        if (!selectUsingMouseCursor)
        {
            StartCoroutine(ScrollButtonIntoViewAsync());
        }
        selectUsingMouseCursor = false;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (eventData.delta.x != 0 || eventData.delta.y != 0)
        {
            selectUsingMouseCursor = true;
            buyButton.Select();
        }
    }

    public void OnDeselect(BaseEventData eventData)
    {
        animator.SetTrigger("Normal");
    }

    private IEnumerator ScrollButtonIntoViewAsync()
    {
        // Wait for layout to recompute before getting this buttons position.
        yield return new WaitForSecondsRealtime(0.1f);
        ScrollButtonIntoView();
    }

    private void ScrollButtonIntoView()
    {
        float safeArea = (viewportRT.rect.height - thisRT.rect.height) / 2 - gap;
        float currentY = itemPanelTransform.localPosition.y + thisRT.localPosition.y;
        float newY = Mathf.Clamp(currentY, -safeArea, safeArea);
        itemPanelTransform.localPosition += (newY - currentY) * Vector3.up;

        if (ShopManager.instance.logsVerboseLevel == VerboseLevel.MAXIMAL)
        {
            Debug.Log($"Shop Item Button - ScrollButtonIntoView(). safeArea = {safeArea}. currentY = {currentY}. newY = {newY}.");
        }
    }
}
