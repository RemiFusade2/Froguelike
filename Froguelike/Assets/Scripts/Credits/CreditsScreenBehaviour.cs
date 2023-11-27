using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;
using static Rewired.ComponentControls.Effects.RotateAroundAxis;

public class CreditsScreenBehaviour : MonoBehaviour
{
    [Header("References")]
    public GameObject SelectedGameObjectCreditsScreen;
    public ScrollRect scrollRect;
    public RectTransform contentRect;
    public ScrollbarKeepCursorSizeBehaviour keepCursorSizeBehaviour;

    [Header("Frogs")]
    public List<CreditsFrogBehaviour> interactableFrogsList;

    /*
    [Header("Settings")]
    public float autoScrollDelay = 1;
    public float autoScrollSpeed = 1;*/

    private Coroutine autoScrollDown;
    private float currentScrollPosition;

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
