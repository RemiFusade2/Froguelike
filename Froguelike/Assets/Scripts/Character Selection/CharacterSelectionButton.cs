using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class CharacterSelectionButton : MonoBehaviour, ISelectHandler, IPointerEnterHandler
{
    [Header("References")]
    public Button characterButton;
    [Space]
    public Image characterBackgroundImage;
    [Space]
    public TextMeshProUGUI characterNameText;
    public TextMeshProUGUI characterDescriptionText;
    public Image characterIconImage;
    public Image tongueIconImage;
    public Image animationImage;

    [Header("UI")]
    public Sprite characterLockedSprite;
    public Sprite characterAvailableSprite;
    public Sprite characterSelectedSprite;
    [Space]
    public Color charactersDefaultTextColor;
    public Color charactersHintTextColor;
    [Space]
    public Color characterLockedAnimationColor;
    public Color characterAvailableAnimationColor;
    public Color characterSelectedAnimationColor;

    [Header("Scrollview")]
    public ScrollRect scrollView;
    public RectTransform viewportRT;
    public RectTransform thisRT;
    public Transform characterPanelGroupTransform;

    [Header("Runtime")]
    public PlayableCharacter character;
    public bool isSelected;

    private const float gap = 22;

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
                characterBackgroundImage.sprite = isSelected ? characterSelectedSprite : characterAvailableSprite; // use the corresponding sprite if the character is selected
                animationImage.color = isSelected ? characterSelectedAnimationColor : characterAvailableAnimationColor; // Set the color for the animation. 
                characterNameText.color = charactersDefaultTextColor;
                characterNameText.text = character.characterData.characterName;
                characterDescriptionText.color = charactersDefaultTextColor;
                characterDescriptionText.text = character.characterData.characterDescription.Replace("\\n", "\n");
                characterIconImage.sprite = character.characterData.characterSprite;
                tongueIconImage.sprite = character.characterData.startingItems[0].icon;
            }
            else
            {
                // character is locked, so display hint to unlock it
                characterBackgroundImage.sprite = characterLockedSprite;
                animationImage.color = characterLockedAnimationColor; // Set the color for the animation.
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

        // Scroll the button into view.
        StartCoroutine(ScrollButtonIntoView());
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        characterButton.Select();
    }


    private IEnumerator ScrollButtonIntoView()
    {
        // Wait for layout to recompute before getting this buttons position.
        yield return new WaitForSeconds(0.1f);

        float safeArea = (viewportRT.rect.height - thisRT.rect.height) / 2 - gap;
        float currentY = characterPanelGroupTransform.localPosition.y + thisRT.localPosition.y;
        float newY = Mathf.Clamp(currentY, -safeArea, safeArea);
        characterPanelGroupTransform.localPosition += (newY - currentY) * Vector3.up;
    }
}
