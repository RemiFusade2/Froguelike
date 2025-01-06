using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Android;
using UnityEngine.UI;

public class CreditsFrogBehaviour : MonoBehaviour
{
    [Header("References")]
    public Animator frogAnimator;
    public Button frogButton;
    public Transform frogTransform;
    public Transform frogPositionsParent;
    public Transform froinsParent;
    public Image hatImage;

    [Header("Prefabs")]
    public GameObject froinPrefab;

    [Header("Settings")]
    public int frogAnimatorIndex = 0;
    public float moveSpeed = 0;
    public bool wearHat;

    private int frogPositionIndex = 0;

    private List<Transform> activeFrogPositionsList;
    private List<Transform> activeFroinsList;

    private void Awake()
    {
        activeFrogPositionsList = new List<Transform>();
        foreach (Transform frogPosition in frogPositionsParent)
        {
            if (frogPosition.gameObject.activeSelf)
            {
                activeFrogPositionsList.Add(frogPosition);
            }
        }
        activeFroinsList = new List<Transform>();
    }

    private void Start()
    {
        hatImage.enabled = wearHat;
        ResetFrog();
    }

    private void Update()
    {
        frogAnimator.SetInteger("FrogIndex", frogAnimatorIndex);
    }

    public void SelectFrog()
    {
        frogAnimator.SetBool("IsSleeping", false);
        StartCoroutine(ScrollFrogIntoViewAsync());
    }

    public void UnselectFrog()
    {
        frogAnimator.SetBool("IsSleeping", true);
    }

    public void ClickOnFrog()
    {
        frogAnimator.SetBool("IsJumping", true);
    }

    public void PlaySound()
    {
        SoundManager.instance.PlayCreditFrogCroakingSound();
    }

    public void ResetFrog()
    {
        UpdateFrogButtonToPositionAtIndex(0);
    }

    public void SpawnFroins(float probability = 1)
    {
        Vector2 frogInitialPosition = activeFrogPositionsList[0].GetComponent<RectTransform>().anchoredPosition;
        foreach (Transform frogPosition in activeFrogPositionsList)
        {
            if (Vector2.Distance(frogPosition.GetComponent<RectTransform>().anchoredPosition, frogInitialPosition) > 5)
            {
                // Position is not too close from frog initial position
                if (!IsFroinAtPosition(frogPosition.GetComponent<RectTransform>().anchoredPosition))
                {
                    // Position doesn't contain a froin already
                    if (Random.Range(0,1.0f) <= probability)
                    {
                        // Random dice roll decided to put a froin on that available spot
                        GameObject froin = Instantiate(froinPrefab, froinsParent);
                        froin.GetComponent<RectTransform>().anchoredPosition = frogPosition.GetComponent<RectTransform>().anchoredPosition;
                        activeFroinsList.Add(froin.transform);
                    }
                }
            }
        }
    }

    private bool IsFroinAtPosition(Vector2 anchoredPosition)
    {
        bool isFroin = false;
        foreach (Transform froin in activeFroinsList)
        {
            if (Vector2.Distance (anchoredPosition, froin.GetComponent<RectTransform>().anchoredPosition) <= 5)
            {
                isFroin = true;
                break;
            }
        }
        return isFroin;
    }


    public void StartJump()
    {
        // Move towards next position
        StartCoroutine(MoveFrogAsync());
    }

    private IEnumerator MoveFrogAsync()
    {
        Vector2 initialPosition = frogButton.GetComponent<RectTransform>().anchoredPosition;
        int indexOfNextPosition = (frogPositionIndex + 1) % activeFrogPositionsList.Count;
        Vector2 targetPosition = activeFrogPositionsList[indexOfNextPosition].GetComponent<RectTransform>().anchoredPosition;

        Vector2 moveDirection = (targetPosition - initialPosition).normalized;

        Vector2 currentPosition = initialPosition;
        while (!currentPosition.Equals(targetPosition))
        {
            yield return new WaitForEndOfFrame();

            currentPosition += moveDirection * moveSpeed;

            CollectFroinsAtPosition(currentPosition);

            if (Vector2.Dot(targetPosition - currentPosition, moveDirection) <= 0)
            {
                currentPosition = targetPosition;
                frogButton.GetComponent<RectTransform>().anchoredPosition = currentPosition;
                break; // just to make sure it doesn't continue due to rounding error
            }

            frogButton.GetComponent<RectTransform>().anchoredPosition = currentPosition;
        }

        frogAnimator.SetBool("IsJumping", false);
        yield return new WaitForEndOfFrame();
        FrogMoveToNextIndex();
    }

    private void CollectFroinsAtPosition(Vector2 frogPosition)
    {
        foreach (Transform froin in froinsParent)
        {
            Vector2 froinPosition = froin.GetComponent<RectTransform>().anchoredPosition;
            if (froin.gameObject.activeSelf && Vector2.Distance(froinPosition, frogPosition) < 5)
            {
                // Collect froin
                froin.gameObject.SetActive(false);
                activeFroinsList.Remove(froin);
                Destroy(froin.gameObject, 0.1f);
                SoundManager.instance.PlayPickUpFroinsSound();
                GameManager.instance.ChangeAvailableCurrency(10);
                AchievementManager.instance.TryUnlockAchievementsOutOfARun(true);
                UIManager.instance.UpdateShopAndQuestButtonsOnTitleScreen();
            }
        }
    }

    public void FrogMoveToNextIndex()
    {
        UpdateFrogButtonToPositionAtIndex((frogPositionIndex + 1) % activeFrogPositionsList.Count);
    }

    private void UpdateFrogButtonToPositionAtIndex(int newIndex)
    {
        if (activeFrogPositionsList != null && newIndex >= 0 && newIndex < activeFrogPositionsList.Count)
        {
            frogPositionIndex = newIndex;

            Transform frogPosition = activeFrogPositionsList[frogPositionIndex];

            frogButton.GetComponent<RectTransform>().anchoredPosition = frogPosition.GetComponent<RectTransform>().anchoredPosition;
            frogButton.GetComponent<RectTransform>().rotation = frogPosition.GetComponent<RectTransform>().rotation;
        }
    }

    private IEnumerator ScrollFrogIntoViewAsync()
    {
        yield return new WaitForSecondsRealtime(0.05f);
        CreditsScreenBehaviour.instance.ScrollFrogIntoView(GetComponent<RectTransform>());
    }
}
