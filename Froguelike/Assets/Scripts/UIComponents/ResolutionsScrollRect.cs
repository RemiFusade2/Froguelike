using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class ResolutionsScrollRect : ScrollRect
{
    [Header("Achievements References")]
    public TextMeshProUGUI resolutionsCountTextMesh;
    public GridLayoutGroup resolutionsScrollViewContentGridLayoutGroup;

    [Header("Resolutions Prefabs")]
    public GameObject resolutionOptionPrefab;

    [Header("Resolutions Settings")]
    public bool loopThrough = true;
    [Space]
    public bool autoScroll = false;
    public float autoScrollDelay = 2.0f;

    private float snapLerpDeceleration = 10;

    private int numberOfResolutions = 4;
    public int currentDisplayedResolution = 1;

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
            MoveToNextResolution();
        }
    }

    private void ClearScrollView()
    {
        foreach (Transform child in this.content)
        {
            Destroy(child.gameObject);
        }
    }

    public void Initialize(List<string> resolutions)
    {
        ClearScrollView();
        numberOfResolutions = 0;
        currentDisplayedResolution = 1;
        foreach (string resolution in resolutions)
        {
            GameObject newResolutionPanel = Instantiate(resolutionOptionPrefab, this.content);
            newResolutionPanel.GetComponent<ResolutionPanelBehaviour>().Initialize(resolution);
            numberOfResolutions++;
        }
        currentDisplayedResolution = 1;

        this.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, numberOfResolutions * resolutionsScrollViewContentGridLayoutGroup.cellSize.x);
        UpdateScroll(false);
    }

    private void UpdateScroll(bool lerp)
    {
        resolutionsCountTextMesh.text = $"{currentDisplayedResolution}/{numberOfResolutions}";
        float horizontalScroll = numberOfResolutions > 1 ? (currentDisplayedResolution - 1) / (1.0f * numberOfResolutions - 1) : 0;
        lerpTarget = -Vector2.right * horizontalScroll * this.content.sizeDelta.x;
        isLerping = lerp;
        lastTimeScroll = Time.unscaledTime;
        if (!lerp)
        {
            this.content.anchoredPosition = lerpTarget;
        }
    }

    private void ClampCurrentDisplayedResolution()
    {
        if (loopThrough)
        {
            currentDisplayedResolution = (((currentDisplayedResolution - 1) + numberOfResolutions) % numberOfResolutions) + 1;
        }
        else
        {
            currentDisplayedResolution = Mathf.Clamp(currentDisplayedResolution, 1, numberOfResolutions);
        }
    }

    public void MoveToPreviousResolution()
    {
        currentDisplayedResolution = (currentDisplayedResolution - 1);
        ClampCurrentDisplayedResolution();
        UpdateScroll(true);
    }

    public void MoveToNextResolution()
    {
        currentDisplayedResolution = (currentDisplayedResolution + 1);
        ClampCurrentDisplayedResolution();
        UpdateScroll(true);
    }
}
