using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class CharacterBookmarkInRunInfoBehaviour : MonoBehaviour
{
    [Header("Character bookmark")]
    public Image characterImage;
    public TextMeshProUGUI characterNameText;
    [Tooltip("Should be none for pause screen - only used for score screen")]public TextMeshProUGUI characterLevelText;
    public Transform thingSlotsParent;

    public void UpdateInRunBookmark()
    {
        try
        {
            // Character image.
            characterImage.sprite = RunManager.instance.currentPlayedCharacter.characterData.characterSprite;
            characterImage.SetNativeSize();
        }
        catch (Exception e)
        {
            Debug.LogError($"Exception in UpdatePauseScreen() - updating character image: {e.Message}");
        }

        try
        {
            // Character name.
            characterNameText.SetText(RunManager.instance.currentPlayedCharacter.characterData.characterName);
        }
        catch (Exception e)
        {
            Debug.LogError($"Exception in UpdatePauseScreen() - updating character name: {e.Message}");
        }

        try
        {
            if (characterLevelText != null)
            {
                // Character level.
                characterLevelText.SetText("Level " + RunManager.instance.level.ToString());
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Exception in UpdatePauseScreen() - updating character level: {e.Message}");
        }

        try
        {
            // Hats + friends
            int thingSlot = 0;
            Image thingSlotImage;

            int nrOfHats = RunManager.instance.player.hatsParent.childCount;
            for (int hat = 0; hat < nrOfHats; hat++)
            {
                if (thingSlot >= thingSlotsParent.childCount) break;

                thingSlotImage = thingSlotsParent.GetChild(thingSlot).GetComponentInChildren<Image>();
                SpriteRenderer[] hatsSpriteRenderersArray = RunManager.instance.player.hatsParent.GetComponentsInChildren<SpriteRenderer>();
                thingSlotImage.sprite = hatsSpriteRenderersArray[hat].sprite;
                thingSlotImage.SetNativeSize();

                thingSlotImage.gameObject.SetActive(true);
                thingSlotImage.enabled = true;

                thingSlot++;
            }

            int nrOfFriends = FriendsManager.instance.transform.childCount;
            foreach (FriendInstance friend in FriendsManager.instance.permanentFriendsList)
            {
                if (thingSlot >= thingSlotsParent.childCount) break;

                thingSlotImage = thingSlotsParent.GetChild(thingSlot).GetComponentInChildren<Image>();
                thingSlotImage.sprite = friend.data.sprite;
                thingSlotImage.SetNativeSize();

                thingSlotImage.gameObject.SetActive(true);
                thingSlotImage.enabled = true;

                thingSlot++;
            }

            while (thingSlot < thingSlotsParent.childCount)
            {
                // Hide unused slots.
                thingSlotImage = thingSlotsParent.GetChild(thingSlot).GetComponentInChildren<Image>();
                if (thingSlotImage != null)
                {
                    thingSlotImage.GetComponent<Image>().enabled = false;
                }
                else
                {
                    break;
                }

                thingSlot++;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Exception in UpdatePauseScreen() - updating hats and friends: {e.Message}");
        }
    }
}
