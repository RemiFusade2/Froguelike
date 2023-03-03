using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CharacterSelectionButton : MonoBehaviour
{
    [Header("References")]
    public Button characterButton;
    [Space]
    public Image characterBackgroundImage;
    [Space]
    public TextMeshProUGUI characterNameText;
    public TextMeshProUGUI characterDescriptionText;
    public Image characterIconImage;

    [Header("UI")]
    public Sprite characterLockedSprite;
    public Sprite characterAvailableSprite;
    public Sprite characterSelectedSprite;
    [Space]
    public Color charactersDefaultTextColor;
    public Color charactersHintTextColor;

    [Header("Runtime")]
    public PlayableCharacter character;
    public bool isSelected;

    // Start is called before the first frame update
    void Start()
    {
        UpdateUI();
    }

    public void Initialize(PlayableCharacter characterData)
    {
        character = characterData;
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (character != null && character.characterData != null && characterButton != null && characterButton.isActiveAndEnabled)
        {
            characterButton.interactable = character.unlocked;
            characterIconImage.enabled = character.unlocked;
            if (character.unlocked)
            {
                // character is unlocked
                characterBackgroundImage.sprite = isSelected ? characterSelectedSprite : characterAvailableSprite; // use the corresponding sprite if the character is selected
                characterNameText.color = charactersDefaultTextColor;
                characterNameText.text = character.characterData.characterName;
                characterDescriptionText.color = charactersDefaultTextColor;
                characterDescriptionText.text = character.characterData.characterDescription.Replace("\\n", "\n");
                characterIconImage.sprite = character.characterData.characterSprite;
            }
            else
            {
                // character is locked, so display hint to unlock it
                characterBackgroundImage.sprite = characterLockedSprite;
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
}
