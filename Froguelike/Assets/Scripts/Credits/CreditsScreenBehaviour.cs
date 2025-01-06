using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;
using static Rewired.ComponentControls.Effects.RotateAroundAxis;

public class CreditsScreenBehaviour : MonoBehaviour
{
    public static CreditsScreenBehaviour instance;

    [Header("References")]
    public GameObject SelectedGameObjectCreditsScreen;
    public ScrollRect scrollRect;
    public RectTransform contentRect;
    public ScrollbarKeepCursorSizeBehaviour keepCursorSizeBehaviour;
    [Space]
    public RectTransform viewportRT;
    public RectTransform contentRT;

    [Header("Frogs")]
    public List<CreditsFrogBehaviour> interactableFrogsList;


    /*
    [Header("Settings")]
    public float autoScrollDelay = 1;
    public float autoScrollSpeed = 1;*/

    private const float gap = 22;

    private Coroutine autoScrollDown;
    private float currentScrollPosition;

    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        // Respawn Froins
        RespawnCreditFroins(0.05f);
    }

    public void Reset()
    {
        keepCursorSizeBehaviour.SetCursorCentered(false);

        //  Select button that must be selected by default
        UIManager.instance.SetSelectedButton(SelectedGameObjectCreditsScreen);

        // Scroll up
        //keepCursorSizeBehaviour.SetValue(1); 
        scrollRect.normalizedPosition = new Vector2(0, 1);

        // Reset frogs
        foreach (CreditsFrogBehaviour creditFrog in interactableFrogsList)
        {
            creditFrog.ResetFrog();
        }
    }

    public void RespawnCreditFroins(float probability = 1)
    {
        foreach (CreditsFrogBehaviour creditFrog in interactableFrogsList)
        {
            creditFrog.SpawnFroins(probability);
        }
    }

    public void ScrollFrogIntoView(RectTransform frogRT)
    {
        // The size of viewport will determine if frog is already visible or not
        float viewportHeight = viewportRT.rect.height - frogRT.rect.height;

        // Get current Y position of content panel (origin is on top)
        float currentContentY = contentRT.anchoredPosition.y;

        // Get current Y position of frog
        float currentFrogY = frogRT.anchoredPosition.y;

        if (currentFrogY <= -currentContentY && currentFrogY >= (-currentContentY - viewportHeight))
        {
            // Frog is already visible, we do nothing
            return;
        }

        // Get new content position to center the frog on screen
        float newContentY = -(currentFrogY + viewportHeight/2);
        newContentY = Mathf.Clamp(newContentY, 0, contentRT.rect.height - viewportHeight);

        // Set new content position
        contentRT.anchoredPosition = new Vector2(contentRT.anchoredPosition.x, newContentY);
    }

    /*
    public void StopAutoScroll()
    {
        if (autoScrollDown != null)
        {
            StopCoroutine(autoScrollDown);
        }
    }*/

        /*
        public void StartAutoScroll()
        {
            StopAutoScroll();
            autoScrollDown = StartCoroutine(AutoScrollDown(autoScrollDelay, autoScrollSpeed));
        }*/

        /* TODO: Fix that method (it's not working as is)
        private IEnumerator AutoScrollDown(float delay, float speed)
        {
            float scrollContentHeight = contentRect.rect.height;
            float deltaScroll;
            keepCursorSizeBehaviour.SetValue(1);
            keepCursorSizeBehaviour.SetCursorCentered(false);
            yield return new WaitForSecondsRealtime(delay);
            while (true)
            {
                deltaScroll = (speed * Time.unscaledDeltaTime) / scrollContentHeight;
                keepCursorSizeBehaviour.SetValue(scrollRect.verticalScrollbar.value + deltaScroll);
                yield return new WaitForEndOfFrame();
            }
        }*/
}
