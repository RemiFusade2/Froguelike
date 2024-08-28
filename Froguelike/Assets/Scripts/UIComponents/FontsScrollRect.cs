using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class FontsScrollRect : ScrollRect
{
    [Header("Fonts References")]
    public TextMeshProUGUI fontsCountTextMesh;
    public GridLayoutGroup fontsScrollViewContentGridLayoutGroup;

    [Header("Fonts Prefabs")]
    public GameObject fontOptionPrefab;

    [Header("Fonts Settings")]
    public bool loopThrough = true;
    [Space]

    public bool autoScroll = false;
    public float autoScrollDelay = 2.0f;

    private float snapLerpDeceleration = 10;

    private int numberOfFonts = 3;
    public int currentDisplayedFont = 1;

    private bool isLerping = false;
    private Vector2 lerpTarget;

    private float lastTimeScroll;

    // Do nothing when using Drag with mouse
    public override void OnBeginDrag(PointerEventData eventData) { }
    public override void OnDrag(PointerEventData eventData) { }
    public override void OnEndDrag(PointerEventData eventData) { }

    private void Update()
    {
        if (isLerping)
        {
            // prevent overshooting with values greater than 1
            float decelerate = Mathf.Min(snapLerpDeceleration * Time.unscaledDeltaTime, 1f);
            this.content.anchoredPosition = Vector2.Lerp(this.content.anchoredPosition, lerpTarget, decelerate);
            // time to stop lerping?
            if (Vector2.SqrMagnitude(this.content.anchoredPosition - lerpTarget) < 0.25f)
            {
                // snap to target and stop lerping
                this.content.anchoredPosition = lerpTarget;
                isLerping = false;
                // clear also any scrollrect move that may interfere with our lerping
                this.velocity = Vector2.zero;
            }
        }
        else if (autoScroll && (Time.unscaledTime - lastTimeScroll) > autoScrollDelay)
        {
            MoveToNextFont();
        }
    }

    private void ClearScrollView()
    {
        foreach (Transform child in this.content)
        {
            Destroy(child.gameObject);
        }
    }

    public void Initialize(List<TMP_FontAsset> fonts, List<string> fontNames, int currentFontIndex)
    {
        ClearScrollView();
        numberOfFonts = 0;
        currentDisplayedFont =  currentFontIndex + 1;
        for (int fontIndex = 0; fontIndex < fonts.Count; fontIndex++)
        {
            GameObject newFontPanel = Instantiate(fontOptionPrefab, this.content);
            newFontPanel.GetComponent<FontPanelBehaviour>().Initialize(fontNames[fontIndex], fonts[fontIndex]);
            numberOfFonts++;
        }

        this.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, numberOfFonts * fontsScrollViewContentGridLayoutGroup.cellSize.x);
        UpdateScroll(false, currentDisplayedFont);
    }

    public void UpdateScroll(bool lerp, int currentFontIndex)
    {
        currentDisplayedFont = currentFontIndex;
        fontsCountTextMesh.text = $"{currentDisplayedFont}/{numberOfFonts}";
        float horizontalScroll = numberOfFonts > 1 ? (currentDisplayedFont - 1) / (1.0f * numberOfFonts - 1) : 0;
        lerpTarget = -Vector2.right * horizontalScroll * this.content.sizeDelta.x;
        isLerping = lerp;
        lastTimeScroll = Time.unscaledTime;
        if (!lerp)
        {
            this.content.anchoredPosition = lerpTarget;
        }
    }

    private void ClampCurrentDisplayedFont()
    {
        if (loopThrough)
        {
            currentDisplayedFont = (((currentDisplayedFont - 1) + numberOfFonts) % numberOfFonts) + 1;
        }
        else
        {
            currentDisplayedFont = Mathf.Clamp(currentDisplayedFont, 1, numberOfFonts);
        }
    }

    public void MoveToPreviousFont()
    {
        currentDisplayedFont = (currentDisplayedFont - 1);
        ClampCurrentDisplayedFont();
        UpdateScroll(true, currentDisplayedFont);
    }

    public void MoveToNextFont()
    {
        currentDisplayedFont = (currentDisplayedFont + 1);
        ClampCurrentDisplayedFont();
        UpdateScroll(true, currentDisplayedFont);
    }
}
