using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ChapterInfoBehaviour : MonoBehaviour
{
    public TextMeshProUGUI infoTitleTextMesh;
    public TextMeshProUGUI infoDescriptionTextMesh;
    public GameObject fixedCollectiblesParent;
    public GameObject powerUpsParent;

    public TextMeshProUGUI infoTitleText;
    public TextMeshProUGUI infoDescriptionText;

    [Header("Only for chapter spread")]
    public GameObject starParent;
    public TextMeshProUGUI chaptersInStoryText;
    public TextMeshProUGUI extraChaptersInStoryText;

    public Color defaultFrameColor;
    public Color goldFrameColor;

    // Private.
    private List<CollectibleSprites> collectibleSpritesFromDataManager;
    private Sprite questionmarkSpriteFromDataManager;
    private bool setUpMaterials = true;

    public void DisplayChapter(Chapter chapterInfo)
    {
        setUpMaterials = DisplayChapterPage(chapterInfo, infoTitleTextMesh, infoDescriptionTextMesh, setUpMaterials, fixedCollectiblesParent, powerUpsParent);
    }

    private bool DisplayFixedCollectibles(Chapter chapterInfo, GameObject fixedCollectiblesParent, bool setUpMarerials, bool useQuestionmarks)
    {
        bool materialsSetUp = false;
        if (setUpMarerials)
        {
            materialsSetUp = SetUpMaterials(fixedCollectiblesParent);
        }
        else
        {
            materialsSetUp = true;
        }

        // If questionmarks are wanted (they are used in the chapter collection book), just show questionmarks if the chapter haven't been played before.
        if (useQuestionmarks)
        {
            if (chapterInfo.attemptCountByCharacters.Count > 0)
            {
                useQuestionmarks = false;
            }
        }

        // Fixed collectibles.
        // Get the chapters fixed collectibles.
        List<FixedCollectible> listOfFixedCollectibles = chapterInfo.chapterData.specialCollectiblesOnTheMap;
        List<Image> fixedCollectibleSlots = fixedCollectiblesParent.GetComponentsInChildren<Image>().ToList();
        fixedCollectibleSlots.RemoveAt(0);
        int slot = 0;

        foreach (FixedCollectible fixedCollectible in listOfFixedCollectibles)
        {
            Image fixedCollectibleSlot = fixedCollectibleSlots[slot];

            // Sprite.
            if (useQuestionmarks)
            {
                fixedCollectibleSlot.sprite = questionmarkSpriteFromDataManager;
                fixedCollectibleSlot.transform.rotation = new Quaternion(0, 0, 0, 0);
            }
            else
            {
                switch (fixedCollectible.collectibleType)
                {
                    case FixedCollectibleType.STATS_ITEM:
                        fixedCollectibleSlot.sprite = fixedCollectible.collectibleStatItemData.icon;
                        fixedCollectibleSlot.transform.rotation = new Quaternion(0, 0, 0, 0);
                        break;
                    case FixedCollectibleType.WEAPON_ITEM:
                        fixedCollectibleSlot.sprite = fixedCollectible.collectibleWeaponItemData.icon;
                        fixedCollectibleSlot.transform.rotation = new Quaternion(0, 0, 0, 0);
                        break;
                    case FixedCollectibleType.HAT:
                        fixedCollectibleSlot.sprite = DataManager.instance.GetSpriteForHat(fixedCollectible.collectibleHatType);
                        fixedCollectibleSlot.transform.rotation = new Quaternion(0, 0, 180, 0);
                        break;
                    case FixedCollectibleType.FRIEND:
                        fixedCollectibleSlot.sprite = DataManager.instance.GetSpriteForFriend(fixedCollectible.collectibleFriendType);
                        fixedCollectibleSlot.transform.rotation = new Quaternion(0, 0, 0, 0);
                        break;
                    default:
                        break;
                }
            }

            slot++;
            fixedCollectibleSlot.SetNativeSize();

            // Found/not found.
            if (useQuestionmarks || chapterInfo.fixedCollectiblesFoundList.Contains(chapterInfo.fixedCollectiblesFoundList.FirstOrDefault(x => x.collectibleIdentifier.Equals(FixedCollectibleFound.GetIdentifierFromCoordinates(fixedCollectible.tileCoordinates)))))
            {
                fixedCollectibleSlot.material.SetInt("_Found", 1); // Found.
            }
            else
            {
                fixedCollectibleSlot.material.SetInt("_Found", 0); // Not found.
            }
        }

        // Empty the unused slots and set shader to found (no overlay).
        while (slot < fixedCollectibleSlots.Count)
        {
            fixedCollectibleSlots[slot].sprite = null;
            fixedCollectibleSlots[slot].material.SetInt("_Found", 1);
            slot++;
        }

        return materialsSetUp;
    }

    private void DisplayCollectiblesAndPowerUps(Chapter chapterInfo, GameObject powerUpsParent, bool useQuestionmarks)
    {
        // If questionmarks are wanted (they are used in the chapter collection book), just show questionmarks if the chapter haven't been played before.
        if (useQuestionmarks)
        {
            if (chapterInfo.attemptCountByCharacters.Count > 0)
            {
                useQuestionmarks = false;
            }
        }

        if (collectibleSpritesFromDataManager == null)
        {
            collectibleSpritesFromDataManager = DataManager.instance.collectibleSprites;
        }

        if (questionmarkSpriteFromDataManager == null)
        {
            questionmarkSpriteFromDataManager = DataManager.instance.questionmarkSprite;
        }

        // Collectibles and power-ups.
        List<CollectibleSpawnFrequency> powerUps = chapterInfo.chapterData.otherCollectibleSpawnFrequenciesList;
        List<Image> powerUpSlots = powerUpsParent.GetComponentsInChildren<Image>().ToList();
        powerUpSlots.RemoveAt(0);
        int slot = 0;

        if (chapterInfo.chapterData.coinsSpawnFrequency != SpawnFrequency.NONE)
        {
            if (useQuestionmarks)
            {
                powerUpSlots[slot].sprite = questionmarkSpriteFromDataManager;
            }
            else
            {
                powerUpSlots[slot].sprite = collectibleSpritesFromDataManager.Find(x => x.collectibleType == CollectibleType.FROINS && x.frequency == chapterInfo.chapterData.coinsSpawnFrequency).collectibleSprite;
            }
            slot++;
        }

        if (chapterInfo.chapterData.levelUpSpawnFrequency != SpawnFrequency.NONE)
        {
            if (useQuestionmarks)
            {
                powerUpSlots[slot].sprite = questionmarkSpriteFromDataManager;
            }
            else
            {
                powerUpSlots[slot].sprite = collectibleSpritesFromDataManager.Find(x => x.collectibleType == CollectibleType.LEVEL_UP && x.frequency == chapterInfo.chapterData.levelUpSpawnFrequency).collectibleSprite;
            }
            slot++;
        }

        if (chapterInfo.chapterData.healthSpawnFrequency != SpawnFrequency.NONE)
        {
            if (useQuestionmarks)
            {
                powerUpSlots[slot].sprite = questionmarkSpriteFromDataManager;
            }
            else
            {
                powerUpSlots[slot].sprite = collectibleSpritesFromDataManager.Find(x => x.collectibleType == CollectibleType.HEALTH && x.frequency == chapterInfo.chapterData.healthSpawnFrequency).collectibleSprite;
            }
            slot++;
        }

        foreach (CollectibleSpawnFrequency powerUp in powerUps)
        {
            Image powerUpSlot = powerUpSlots[slot];

            if (powerUp.Frequency != SpawnFrequency.NONE)
            {
                if (powerUp.Type == CollectibleType.FROINS || powerUp.Type == CollectibleType.LEVEL_UP || powerUp.Type == CollectibleType.HEALTH)
                {
                    if (useQuestionmarks)
                    {
                        powerUpSlot.sprite = questionmarkSpriteFromDataManager;
                    }
                    else
                    {
                        powerUpSlot.sprite = collectibleSpritesFromDataManager.Find(x => x.collectibleType == powerUp.Type && x.frequency == powerUp.Frequency).collectibleSprite;
                    }
                    slot++;
                }
                else if (!BuildManager.instance.demoBuild)
                {
                    if (useQuestionmarks)
                    {
                        powerUpSlot.sprite = questionmarkSpriteFromDataManager;
                    }
                    else
                    {
                        powerUpSlot.sprite = collectibleSpritesFromDataManager.Find(x => x.collectibleType == powerUp.Type).collectibleSprite;
                    }
                    slot++;
                }

                if (slot >= powerUpSlots.Count)
                {
                    break;
                }
            }
        }

        while (slot < powerUpSlots.Count)
        {
            powerUpSlots[slot].sprite = null;
            slot++;
        }
    }

    private void DisplayChapterText(Chapter chapterInfo, TextMeshProUGUI infoTitleText, TextMeshProUGUI infoDescriptionText)
    {
        // Set chapter info
        infoTitleText.SetText(chapterInfo.chapterData.chapterTitle);
        // Find a frog name to use in the description.
        string frogName = "";
        if (RunManager.instance.currentPlayedCharacter.characterData == null) // When looking in the chapter book, look for if the chapter can only be played by a specific characater and use their name.
        {
            foreach (ChapterConditionsChunk chunk in chapterInfo.chapterData.conditions)
            {
                foreach (ChapterCondition condition in chunk.conditionsList)
                {
                    if (condition.conditionType == ChapterConditionType.CHARACTER)
                    {
                        frogName = condition.characterData.characterName;
                        break;
                    }
                }
                if (frogName != "") break;
            }
        }
        else
        {
            // If a character is selected, when starting or in run, use that characters name.
            frogName = RunManager.instance.currentPlayedCharacter.characterData.characterName;
        }

        // If no frog name was found, use "Frog" as default.
        if (frogName == "")
        {
            frogName = "Frog";
        }

        infoDescriptionText.SetText(chapterInfo.chapterData.GetDescription(frogName));
    }

    private void DisplayUnplayedChapterText(TextMeshProUGUI infoTitleText, TextMeshProUGUI infoDescriptionText)
    {
        // Set chapter info
        infoTitleText.SetText("???");
        infoDescriptionText.SetText("???");
    }

    // Used for displaying info when picking chapter and looking at the chapter on the pause screen.
    public bool DisplayChapterPage(Chapter chapterInfo, TextMeshProUGUI infoTitleText, TextMeshProUGUI infoDescriptionText, bool setUpMaterials, GameObject fixedCollectiblesParent, GameObject powerUpsParent)
    {
        DisplayChapterText(chapterInfo, infoTitleText, infoDescriptionText);
        bool materialsSetUp = DisplayFixedCollectibles(chapterInfo, fixedCollectiblesParent, setUpMaterials, false);
        DisplayCollectiblesAndPowerUps(chapterInfo, powerUpsParent, false);

        return materialsSetUp;
    }

    // Used for displaying chapter info in the chapter collection book.
    public bool DisplayChapterSpread(Chapter chapterInfo, bool setUpMaterials)
    {
        if (chapterInfo.attemptCountByCharacters.Count > 0) // This chapter has been played.
        {
            DisplayChapterText(chapterInfo, infoTitleText, infoDescriptionText);
        }
        else // This chapter has not been played.
        {
            DisplayUnplayedChapterText(infoTitleText, infoDescriptionText);
        }

        DisplayChaptersInThisStory(chapterInfo, chaptersInStoryText, extraChaptersInStoryText);
        DisplayStars(chapterInfo, starParent);

        bool materialsSetUp = DisplayFixedCollectibles(chapterInfo, fixedCollectiblesParent, setUpMaterials, true);
        DisplayCollectiblesAndPowerUps(chapterInfo, powerUpsParent, true);

        return materialsSetUp;
    }

    private void DisplayStars(Chapter chapterInfo, GameObject starParent)
    {
        List<CharacterCount> charactersThatCompletedTheChapter = chapterInfo.completionCountByCharacters;
        List<Image> starSlots = starParent.GetComponentsInChildren<Image>().ToList();
        starSlots.RemoveAt(0);
        int slot = 0;

        // Show the the star of each character that have completed the chapter.
        foreach (CharacterCount character in charactersThatCompletedTheChapter)
        {
            Image starSlot = starSlots[slot];

            starSlot.sprite = CharacterManager.instance.GetCharacterData(character.characterIdentifier).characterStarSprite;
            slot++;
        }

        while (slot < starSlots.Count)
        {
            starSlots[slot].sprite = null;
            slot++;
        }

        List<CharacterData> listOfCharactersThatCanPlayThisChapter = new List<CharacterData>();

        // Change the color fo the border if all characters that can play this chapter have completed the chapter.
        foreach (ChapterConditionsChunk chunk in chapterInfo.chapterData.conditions)
        {
            foreach (ChapterCondition condition in chunk.conditionsList)
            {
                if (condition.conditionType == ChapterConditionType.CHARACTER)
                {
                    listOfCharactersThatCanPlayThisChapter.Add(condition.characterData);
                }
            }
        }

        // Turn frame gold.
        if (charactersThatCompletedTheChapter.Count == CharacterManager.instance.charactersScriptableObjectsList.Count || (listOfCharactersThatCanPlayThisChapter.Count > 0 && listOfCharactersThatCanPlayThisChapter.Count == charactersThatCompletedTheChapter.Count) || chapterInfo.chapterID == "[CH_ENDING_TOAD]")
        {
            starParent.GetComponent<Image>().color = goldFrameColor;
        }
        else
        {
            starParent.GetComponent<Image>().color = defaultFrameColor;
        }
    }

    private void DisplayChaptersInThisStory(Chapter chapterInfo, TextMeshProUGUI chaptersInStoryText, TextMeshProUGUI extraChaptersInStoryText)
    {
        GameObject parent = chaptersInStoryText.transform.parent.gameObject;
        Story thisStory = StoryManager.instance.GetTheStoryThatContainsThisChapter(chapterInfo.chapterData);
        string text = "This chapter is part of a story\n\n";


        if (thisStory.listOfChaptersInStory.Count == 1)
        {
            // Hide the note.
            parent.SetActive(false);
        }
        else if (thisStory.nameOfStory == "The riddle") // Because this story has a branch.
        {
            // Show the note.
            parent.SetActive(true);
            // Set size of note game objects.
            chaptersInStoryText.transform.parent.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 214f);

            // Set text for the first chapter and one of the branches.
            for (int chapterIndex = 0; chapterIndex < 3; chapterIndex++)
            {
                Chapter thisChapter = ChapterManager.instance.GetChapterFromID(thisStory.listOfChaptersInStory[chapterIndex].chapterID);
                string displayThisText;
                if (thisChapter.attemptCountByCharacters.Count == 0)
                {
                    displayThisText = "???";
                }
                else
                {
                    displayThisText = thisChapter.chapterData.chapterTitle;
                }

                text += "- " + displayThisText + "\n";

                if (chapterIndex == 0) text += "\n";
            }

            // Show extra text objects and set the text.
            extraChaptersInStoryText.gameObject.SetActive(true);
            string moreChaptersText = "";
            for (int chapterIndex = 3; chapterIndex < 5; chapterIndex++)
            {
                Chapter thisChapter = ChapterManager.instance.GetChapterFromID(thisStory.listOfChaptersInStory[chapterIndex].chapterID);
                string displayThisText;
                if (thisChapter.attemptCountByCharacters.Count == 0)
                {
                    displayThisText = "???";
                }
                else
                {
                    displayThisText = thisChapter.chapterData.chapterTitle;
                }

                moreChaptersText += "- " + displayThisText + "\n";
            }

            extraChaptersInStoryText.SetText(moreChaptersText);
        }
        else
        {
            // Show the note.
            parent.SetActive(true);
            // Set size of note game objects.
            chaptersInStoryText.transform.parent.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 149f);
            // Hide extra text object (used for "the riddle").
            extraChaptersInStoryText.gameObject.SetActive(false);

            // Set text.
            for (int chapterIndex = 0; chapterIndex < thisStory.listOfChaptersInStory.Count; chapterIndex++)
            {
                Chapter thisChapter = ChapterManager.instance.GetChapterFromID(thisStory.listOfChaptersInStory[chapterIndex].chapterID);
                string displayThisText;
                if (thisChapter.attemptCountByCharacters.Count == 0)
                {
                    displayThisText = "???";
                }
                else
                {
                    displayThisText = thisChapter.chapterData.chapterTitle;
                }

                text += "- " + displayThisText + "\n";
            }
        }

        // Update and display list.
        chaptersInStoryText.SetText(text);
    }

    private bool SetUpMaterials(GameObject fixedCollectiblesParent)
    {
        List<Image> fixedCollectibleSlots = fixedCollectiblesParent.GetComponentsInChildren<Image>().ToList();
        foreach (Image fixedCollectible in fixedCollectibleSlots)
        {
            Material mat = Instantiate(fixedCollectible.material);
            fixedCollectible.material = mat;
        }

        return true;
    }
}
