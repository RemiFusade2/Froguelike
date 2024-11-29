using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class CharacterSelectionButton : MonoBehaviour, ISelectHandler, IPointerEnterHandler, IDeselectHandler
{
    [Header("References")]
    public Button characterButton;
    [Space]
    public Image characterFrameImage;
    public Image characterBackgroundImage;
    public Image characterBackgroundAnimationImage;
    [Space]
    public TextMeshProUGUI characterNameText;
    public TextMeshProUGUI characterDescriptionText;
    public Image characterIconImage;
    public Image tongueIconImage;
    public Image animationImage;

    [Header("UI")]
    public Sprite characterLockedFrameSprite;
    public Sprite characterAvailableFrameSprite;
    public Sprite characterSelectedFrameSprite;
    [Space]
    public Color charactersDefaultTextColor;
    public Color charactersHintTextColor;
    [Space]
    public Color characterLockedFrameAnimationColor;
    public Color characterLockedBackgroundAnimationColor;
    public Color characterLockedBackgroundColor;
    public Color characterAvailableFrameAnimationColor;
    public Color characterAvailableBackgroundAnimationColor;
    public Color characterAvailableBackgroundColor;
    public Color characterSelectedFrameAnimationColor;
    public Color characterSelectedBackgroundAnimationColor;
    public Color characterSelectedBackgroundColor;

    [Header("Scrollview")]
    public ScrollRect scrollView;
    public RectTransform viewportRT;
    public RectTransform thisRT;
    public Transform characterPanelGroupTransform;

    [Header("Runtime")]
    public PlayableCharacter character;
    public bool isSelected;

    private const float gap = 22;

    private bool selectUsingMouseCursor = false;

    // Start is called before the first frame update
    void Start()
    {
        UpdateUI();
    }

    public void Initialize(PlayableCharacter characterData)
    {
        character = characterData;
        UpdateUI();

        scrollView = transform.parent.parent.parent.parent.GetComponent<ScrollRect>();
        viewportRT = transform.parent.parent.parent.GetComponent<RectTransform>();
        thisRT = GetComponent<RectTransform>();
        characterPanelGroupTransform = transform.parent;
    }

    private void UpdateUI()
    {
        if (character != null && character.characterData != null && characterButton != null && characterButton.isActiveAndEnabled)
        {
            characterButton.interactable = character.unlocked;
            characterIconImage.enabled = character.unlocked;
            tongueIconImage.enabled = character.unlocked;
            if (character.unlocked)
            {
                // character is unlocked
                characterFrameImage.sprite = isSelected ? characterSelectedFrameSprite : characterAvailableFrameSprite; // use the corresponding sprite if the character is selected
                characterBackgroundImage.color = isSelected ? characterSelectedBackgroundColor : characterAvailableBackgroundColor;
                characterBackgroundAnimationImage.color = isSelected ? characterSelectedBackgroundAnimationColor : characterAvailableBackgroundAnimationColor;
                animationImage.color = isSelected ? characterSelectedFrameAnimationColor : characterAvailableFrameAnimationColor; // Set the color for the animation. 
                characterNameText.color = charactersDefaultTextColor;
                characterNameText.text = character.characterData.characterName;
                characterDescriptionText.color = charactersDefaultTextColor;
                characterDescriptionText.text = character.characterData.characterDescription.Replace("\\n", "\n");
                characterIconImage.sprite = character.characterData.characterSprite;
                characterIconImage.SetNativeSize();
                tongueIconImage.sprite = character.characterData.startingItems[0].icon;
            }
            else
            {
                // character is locked, so display hint to unlock it
                characterFrameImage.sprite = characterLockedFrameSprite;
                characterBackgroundImage.color = characterLockedBackgroundColor;
                characterBackgroundAnimationImage.color = characterLockedBackgroundAnimationColor;
                animationImage.color = characterLockedFrameAnimationColor; // Set the color for the animation.
                characterNameText.color = charactersHintTextColor;
                characterNameText.text = "???";
                characterDescriptionText.color = charactersHintTextColor;
                if (character.characterData.unlockHint != null)
                {
                    characterDescriptionText.text = $"UNLOCK: {character.characterData.unlockHint.Replace("\\n", "\n")}";
                }
            }
        }
    }

    public void ClickOnButton()
    {
        CharacterManager.instance.SelectCharacter(character);
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected;
        UpdateUI();
    }

    public void OnSelect(BaseEventData eventData)
    {
        SoundManager.instance.PlayButtonSound(characterButton);

        if (!selectUsingMouseCursor)
        {
            // Scroll the button into view.
            StartCoroutine(ScrollButtonIntoViewAsync());
        }
        selectUsingMouseCursor = false;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (eventData.delta.x != 0 || eventData.delta.y != 0)
        {
            selectUsingMouseCursor = true;
            characterButton.Select();
        }
    }

    public void OnDeselect(BaseEventData eventData)
    {
        characterButton.GetComponent<Animator>().SetTrigger("Normal");
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
        float currentY = characterPanelGroupTransform.localPosition.y + thisRT.localPosition.y;
        float newY = Mathf.Clamp(currentY, -safeArea, safeArea);
        characterPanelGroupTransform.localPosition += (newY - currentY) * Vector3.up;
    }
}
