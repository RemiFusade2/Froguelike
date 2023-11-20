using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class PauseScreen : MonoBehaviour
{
    public RunManager runManager;

    public SpriteRenderer characterImage;
    public TextMeshProUGUI characterNameText;
    public Transform thingSlotsParent;
    public TextMeshProUGUI chapterText;
    public List<Transform> tongueSlotsParents;
    public GameObject tongueSlotPrefab;
    public List<Transform> runItemSlotsParents;
    public GameObject runItemSlotPrefab;
    public GameObject firstExtraSlots;
    public GameObject secondExtraSlots;
    public GameObject thirdExtraSlots;
    public TextMeshProUGUI livesCountText;


    public void UpdatePauseScreen()
    {
        // Character image.
        characterImage.sprite = runManager.currentPlayedCharacter.characterData.characterSprite;

        // Character name.
        characterNameText.SetText(runManager.currentPlayedCharacter.characterData.characterName);

        // Hats + friends
        int thingSlot = 0;

        int nrOfHats = runManager.player.hatsParent.childCount;
        for (int hat = 0; hat < nrOfHats; hat++)
        {
            if (thingSlot >= thingSlotsParent.childCount) break;
            thingSlotsParent.GetChild(thingSlot).GetComponentInChildren<SpriteRenderer>().sprite = runManager.player.hatsParent.GetComponentsInChildren<SpriteRenderer>()[hat].sprite;
            thingSlot++;

        }

        int nrOfFriends = FriendsManager.instance.transform.childCount;
        foreach (FriendInstance friend in FriendsManager.instance.permanentFriendsList)
        {
            if (thingSlot >= thingSlotsParent.childCount) break;
            thingSlotsParent.GetChild(thingSlot).GetComponentInChildren<SpriteRenderer>().sprite = friend.Info.sprite;
            thingSlot++;
        }

        for (int friend = 0; friend < nrOfFriends; friend++)
        {         
            
        }


        while (thingSlot < thingSlotsParent.childCount)
        {
            thingSlotsParent.GetChild(thingSlot).GetComponentInChildren<SpriteRenderer>().sprite = null;
            thingSlot++;
        }

        // Chapter number + name.
        chapterText.SetText("Chapter " + runManager.GetChapterCount().ToString() + "<br>" + runManager.currentChapter.chapterData.chapterTitle);

        // Tongue slots, sprite + level.
        // Item slots, sprite + level.
        UpdateRunItemSlots();

        // Extra lives count.
        livesCountText.SetText("Extra lives: " + runManager.extraLivesCountText.text);
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
}
