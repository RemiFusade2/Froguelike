using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class AchievementsScrollRect : ScrollRect
{
    [Header("Achievements References")]
    public TextMeshProUGUI achievementCountTextMesh;

    [Header("Achievements Prefabs")]
    public GameObject achievementScorePrefab;

    [Header("Achievements Settings")]
    public bool loopThrough = true;
    [Space]
    public bool autoScroll = true;
    public float autoScrollDelay = 2.0f;

    private float snapLerpDeceleration = 10;

    private int numberOfAchievements = 4;
    private int currentDisplayedAchievement = 1;

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
            MoveToNextAchievement();
        }
    }

    private void ClearScrollView()
    {
        foreach (Transform child in this.content)
        {
            Destroy(child.gameObject);
        }
    }

    public void Initialize(List<Achievement> achievements)
    {
        ClearScrollView();
        numberOfAchievements = 0;
        currentDisplayedAchievement = 1;
        foreach (Achievement achievement in achievements)
        {
            GameObject newAchievementPanel = Instantiate(achievementScorePrefab, this.content);
            newAchievementPanel.GetComponent<ScoreAchievementPanelBehaviour>().Initialize(achievement);
            numberOfAchievements++;
        }        
        currentDisplayedAchievement = 1;
                
        this.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, numberOfAchievements * achievementScorePrefab.GetComponent<RectTransform>().sizeDelta.x);
        UpdateScroll(false);
    }

    private void UpdateScroll(bool lerp)
    {
        achievementCountTextMesh.text = $"{currentDisplayedAchievement}/{numberOfAchievements}";
        float horizontalScroll = (currentDisplayedAchievement - 1) / (1.0f * numberOfAchievements);
        lerpTarget = -Vector2.right * horizontalScroll * this.content.sizeDelta.x;
        isLerping = lerp;
        lastTimeScroll = Time.unscaledTime;
        if (!lerp)
        {
            this.content.anchoredPosition = lerpTarget;
        }
    }

    public void MoveToPreviousAchievement()
    {
        currentDisplayedAchievement = (currentDisplayedAchievement - 1);
        if (loopThrough)
        {
            currentDisplayedAchievement = (currentDisplayedAchievement <= 0) ? numberOfAchievements : currentDisplayedAchievement;
        }
        else
        {
            currentDisplayedAchievement = (currentDisplayedAchievement <= 0) ? 1 : currentDisplayedAchievement;
        }
        UpdateScroll(true);
    }

    public void MoveToNextAchievement()
    {
        currentDisplayedAchievement = (currentDisplayedAchievement + 1);
        if (loopThrough)
        {
            currentDisplayedAchievement = (currentDisplayedAchievement > numberOfAchievements) ? 1 : currentDisplayedAchievement;
        }
        else
        {
            currentDisplayedAchievement = (currentDisplayedAchievement > numberOfAchievements) ? numberOfAchievements : currentDisplayedAchievement;
        }
        UpdateScroll(true);
    }
}
