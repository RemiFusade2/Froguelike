using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HalfBookmarkBehaviour : MonoBehaviour
{
    public Image frogImage;
    public Image tongueImage;
    public TextMeshProUGUI frogNameText;

    public void SetInfo()
    {
        PlayableCharacter character = CharacterManager.instance.currentSelectedCharacter;
        frogImage.sprite = character.characterData.characterSprite;
        frogImage.SetNativeSize();
        tongueImage.sprite = character.characterData.startingItems[0].icon;
        frogNameText.SetText(character.characterData.characterName);
    }
}
