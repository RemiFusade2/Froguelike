using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;

public class PauseScreen : MonoBehaviour
{
    public RunManager runManager;
    [Header("Character bookmark")]
    public Image characterImage;
    public TextMeshProUGUI characterNameText;
    public Transform thingSlotsParent;
    [Header("Chapter list")]
    public TextMeshProUGUI chapterText;
    [Header("Item slots")]
    public List<Transform> tongueSlotsParents;
    public GameObject tongueSlotPrefab;
    public List<Transform> runItemSlotsParents;
    public GameObject runItemSlotPrefab;
    public GameObject firstExtraSlots;
    public GameObject secondExtraSlots;
    public GameObject thirdExtraSlots;
    public GameObject forthExtraSlots;
    [Header("Lives")]
    public TextMeshProUGUI livesCountText;
    [Header("Panel")]
    public GameObject runInfoPanel;
    public ChapterInfoBehaviour chapterInfoPanel;
    [Header("Tab buttons")]
    public Button runInfoButton;
    public Button chapterInfoButton;

    public void ShowRunInfoPanel()
    {
        chapterInfoPanel.transform.parent.gameObject.SetActive(false);
        runInfoPanel.SetActive(true);
        chapterInfoButton.Select();
    }

    public void UpdatePauseScreen()
    {
        try
        {
            // Character image.
            characterImage.sprite = runManager.currentPlayedCharacter.characterData.characterSprite;
            characterImage.SetNativeSize();
        }
        catch (Exception e)
        {
            Debug.LogError($"Exception in UpdatePauseScreen() - updating character image: {e.Message}");
        }

        try
        {
            // Character name.
            characterNameText.SetText(runManager.currentPlayedCharacter.characterData.characterName);
        }
        catch (Exception e)
        {
            Debug.LogError($"Exception in UpdatePauseScreen() - updating character name: {e.Message}");
        }

        try
        {
            // Hats + friends
            int thingSlot = 0;
            Image thingSlotImage;

            int nrOfHats = runManager.player.hatsParent.childCount;
            for (int hat = 0; hat < nrOfHats; hat++)
            {
                if (thingSlot >= thingSlotsParent.childCount) break;

                thingSlotImage = thingSlotsParent.GetChild(thingSlot).GetComponentInChildren<Image>();
                SpriteRenderer[] hatsSpriteRenderersArray = runManager.player.hatsParent.GetComponentsInChildren<SpriteRenderer>();
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

        try
        {
            string text = "";
            for (int chapterIndex = 0; chapterIndex < runManager.GetChapterCount() - 1; chapterIndex++)
            {
                text += "Chapter " + (chapterIndex + 1) + " - " + runManager.completedChaptersList[chapterIndex].chapterData.chapterTitle + "\n";
            }

            text += "Chapter " + runManager.GetChapterCount().ToString() + " - " + runManager.currentChapter.chapterData.chapterTitle;
            // Chapter number + name.
            chapterText.SetText(text);
        }
        catch (Exception e)
        {
            Debug.LogError($"Exception in UpdatePauseScreen() - updating chapter number and name: {e.Message}");
        }

        try
        {
            // Tongue slots, sprite + level.
            // Item slots, sprite + level.
            UpdateRunItemSlots();
        }
        catch (Exception e)
        {
            Debug.LogError($"Exception in UpdatePauseScreen() - updating tongues and items slots: {e.Message}");
        }

        try
        {
            // Extra lives count.
            livesCountText.SetText("Extra lives: " + runManager.extraLivesCountText.text);
        }
        catch (Exception e)
        {
            Debug.LogError($"Exception in UpdatePauseScreen() - updating extra lives count: {e.Message}");
        }

        ShowRunInfoPanel();
    }

    private void UpdateRunItemSlots()
    {
        // Get owned run items.
        List<RunItemInfo> ownedTongues = new List<RunItemInfo>(runManager.GetOwnedWeapons());
        // Make sure all available slots show up by adding empty ones.
        FillRunItemInfoList(ownedTongues, runManager.player.weaponSlotsCount);

        List<RunItemInfo> ownedRunItems = new List<RunItemInfo>(runManager.GetOwnedStatItems());
        // Make sure all available slots show up by adding empty ones.
        FillRunItemInfoList(ownedRunItems, runManager.player.statItemSlotsCount);

        // Show extra slot space if necessary.
        int maxSlots = Mathf.Max(ownedTongues.Count, ownedRunItems.Count, runManager.player.statItemSlotsCount, runManager.player.weaponSlotsCount);
        firstExtraSlots.SetActive(maxSlots > 6);
        secondExtraSlots.SetActive(maxSlots > 8);
        thirdExtraSlots.SetActive(maxSlots > 10);
        forthExtraSlots.SetActive(maxSlots > 12);

        UpdateSlots(ownedTongues, tongueSlotPrefab, tongueSlotsParents);
        UpdateSlots(ownedRunItems, runItemSlotPrefab, runItemSlotsParents);
    }

    private List<RunItemInfo> FillRunItemInfoList(List<RunItemInfo> list, int count)
    {
        while (list.Count < count)
        {
            list.Add(null);
        }

        return list;
    }

    private void UpdateSlots(List<RunItemInfo> ownedItems, GameObject slotPrefab, List<Transform> parents)
    {
        // Remove previous slots.
        RemoveChildren(parents);

        // Add new slots.
        for (int child = 0; child < ownedItems.Count; child++)
        {
            RunItemInfo runItem = ownedItems[child];
            Transform parent;

            // Instantiate slot in book.
            if (child < 6)
            {
                parent = parents[0];
            }
            // Instantiate slot on first post it.
            else if (child < 8)
            {
                parent = parents[1];
            }
            // Instantiate slot on second post it.
            else if (child < 10)
            {
                parent = parents[2];
            }
            // Instantiate slot on third post it.
            else if (child < 12)
            {
                parent = parents[3];
            }
            // Instantiate slot on forth post it.
            else if (child < 14)
            {
                parent = parents[4];
            }
            else
            {
                return;
            }

            PauseScreenSlotBehaviour slot = Instantiate(slotPrefab, parent).GetComponent<PauseScreenSlotBehaviour>();
            if (runItem != null)
            {
                slot.UpdateSlot(runItem);
            }
        }
    }

    private void RemoveChildren(List<Transform> parents)
    {
        foreach (Transform parent in parents)
        {
            foreach (Transform child in parent)
            {
                Destroy(child.gameObject);
            }
        }
    }

    public void ShowChapterInfoPanel()
    {
        runInfoPanel.SetActive(false);
        chapterInfoPanel.transform.parent.gameObject.SetActive(true);
        runInfoButton.Select();

        chapterInfoPanel.DisplayChapter(RunManager.instance.currentChapter, chapterInfoPanel);
    }
}
