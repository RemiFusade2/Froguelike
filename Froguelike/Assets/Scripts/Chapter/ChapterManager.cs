using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// CharacterCount associate a character name and a number.
/// It is used to store the amount of attempts and completions in a List.
/// </summary>
[System.Serializable]
public class CharacterCount
{
    public string characterIdentifier;
    public int counter;
}

/// <summary>
/// FixedCollectibleFound associate a fixed collectible with a boolean.
/// It is used to store the information about which fixed collectibles have been picked up in a given chapter.
/// </summary>
[System.Serializable]
public class FixedCollectibleFound
{
    public string collectibleIdentifier; // We use tile coordinates "X_Y" as identifier
    public bool hasBeenFoundOnce;

    public static string GetIdentifierFromCoordinates(Vector2Int coordinates)
    {
        return $"{coordinates.x}_{coordinates.y}";
    }
}

/// <summary>
/// Chapter describes a chapter in its current state.
/// It has a reference to ChapterData, the scriptable object that describes the chapter. This is not serialized with the rest.
/// It keeps the chapterID there for serialization. When saving/loading this chapter from a save file, the ID will be used to retrieve the right chapter in the program.
/// The information that can change at runtime are:
/// - unlocked, is the status of the chapter. This value can change when the chapter is unlocked through an achievement.
/// - attemptCountByCharacters is the amount of attempts from every character
/// - completionCountByCharacters is the amount of completions from every character
/// </summary>
[System.Serializable]
public class Chapter
{
    [System.NonSerialized]
    public ChapterData chapterData;

    // Defined at runtime, using ChapterData
    [HideInInspector]
    public string chapterID;

    public bool unlocked;

    public List<CharacterCount> attemptCountByCharacters;
    public List<CharacterCount> completionCountByCharacters;

    public List<FixedCollectibleFound> fixedCollectiblesFoundList;

    [System.NonSerialized]
    public float weight; // used to decide how likely it is that this chapter will show up in the selection

    public Chapter()
    {
        attemptCountByCharacters = new List<CharacterCount>();
        completionCountByCharacters = new List<CharacterCount>();
        fixedCollectiblesFoundList = new List<FixedCollectibleFound>();
    }

    public override bool Equals(object obj)
    {
        bool equal = false;
        if (obj is Chapter)
        {
            equal = this.chapterID.Equals((obj as Chapter).chapterID);
        }
        return equal;
    }

    public override int GetHashCode()
    {
        return chapterID.GetHashCode();
    }
}

/// <summary>
/// ChaptersSaveData contains all information that must be saved about the chapters.
/// - chaptersList is the list of chapters in their current state
/// </summary>
[System.Serializable]
public class ChaptersSaveData : SaveData
{
    public List<Chapter> chaptersList;

    public int chapterCountInSelection;

    public ChaptersSaveData()
    {
        Reset();
    }

    public override void Reset()
    {
        base.Reset();
        chaptersList = new List<Chapter>();
        chapterCountInSelection = 3;
    }
}

[System.Serializable]
public class CollectibleSprites
{
    public CollectibleType collectibleType;
    public SpawnFrequency frequency;
    public Sprite collectibleSprite;
}

/// <summary>
/// ChapterManager keep all information about Chapters, including the current state of the Chapters deck during a Run.
/// It also takes care of displaying the Chapter book from the menu, and the Chapter selection during a Run.
/// </summary>
public class ChapterManager : MonoBehaviour
{
    public static ChapterManager instance;

    [Header("Settings - Debug")]
    public VerboseLevel logsVerboseLevel = VerboseLevel.NONE;

    [Header("Data - Chapters scriptable objects")]
    public List<ChapterData> chaptersScriptableObjectsList;
    public ChapterData tutorialChapterScriptableObject;
    public ChapterData toadEndChapterForSpecialStuff;

    [Header("UI - Chapter Selection screen")]
    public TextMeshProUGUI chapterSelectionTopText;
    public List<ChapterButtonBehaviour> chapterButtonsList;
    public Button backButton;
    public ChapterInfoBehaviour chapterInfoPanel;

    [Header("UI - Chapter Selection screen - Post its")]
    public GameObject rerollInfinitePostIt;
    public GameObject rerollPostIt;
    public TextMeshProUGUI rerollPostItCountTextMesh;
    public Button rerollPostItButton;

    [Header("UI - Chapter Start screen")]
    public Image chapterStartBackground;
    public TextMeshProUGUI chapterStartTopText;
    public TextMeshProUGUI chapterStartBottomText;
    public List<Image> chapterStartIconsImageList;

    [Header("Runtime - Save Data")]
    public ChaptersSaveData chaptersData; // Will be loaded and saved when needed

    [Header("Runtime - UI")]
    public bool chapterChoiceIsVisible;
    public bool isFirstChapter;

    [Header("Runtime - Chapter selection")]
    public List<Chapter> selectionOfNextChaptersList; // The current selection of X chapters to choose from

    private Dictionary<string, Chapter> allChaptersDico; // This dictionary is a handy way to get the Chapter object from its ID

    private Coroutine fadeOutChapterStartScreenCoroutine;

    private int displayedChapterIndex;

    #region Unity Callback methods

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(this);
        }
        else
        {
            Destroy(this.gameObject);
        }
    }

    #endregion

    /// <summary>
    /// Unlock the given chapter.
    /// </summary>
    /// <param name="chapterData"></param>
    public void UnlockChapter(ChapterData chapterData)
    {
        allChaptersDico[chapterData.chapterID].unlocked = true;
    }

    public Chapter GetChapterFromID(string chapterID)
    {
        Chapter result = null;
        if (allChaptersDico.ContainsKey(chapterID))
        {
            result = allChaptersDico[chapterID];
        }
        return result;
    }

    public int GetUnlockedChaptersCount()
    {
        int unlockedCount = 0;
        foreach (Chapter chapter in chaptersData.chaptersList)
        {
            if (!chapter.chapterData.startingUnlockState && chapter.unlocked)
            {
                unlockedCount++;
            }
        }
        return unlockedCount;
    }

    /// <summary>
    /// Return true if a reroll would show at least one new chapter.
    /// </summary>
    /// <param name="numberOfSimilarChaptersAfterReroll">Return a value that is the number of similar chapters after the reroll</param>
    /// <returns></returns>
    public bool WouldChapterRerollGiveAtLeastOneNewChapter(out int numberOfSimilarChaptersAfterReroll)
    {
        int numberOfNewChaptersInCurrentDeck = GetDeckOfChapters(true).Count;
        int numberOfChaptersInSelection = chaptersData.chapterCountInSelection;

        bool result = (numberOfNewChaptersInCurrentDeck > 0);

        numberOfSimilarChaptersAfterReroll = (numberOfChaptersInSelection - numberOfNewChaptersInCurrentDeck);
        numberOfSimilarChaptersAfterReroll = (numberOfSimilarChaptersAfterReroll < 0) ? 0 : numberOfSimilarChaptersAfterReroll;

        return result;
    }


    /// COUNT ALL WAVES IN CHAPTERS
    /// This is used to check how many times each wave is used (to fill up our spreadsheet)
    /*
    private Dictionary<string, int> allWavesDico;
    */

    /// <summary>
    /// Create the deck of Chapters using the current state of the game
    /// </summary>
    /// <returns></returns>
    private List<Chapter> GetDeckOfChapters(bool forcePreventUsingChaptersFromPreviousSelection)
    {
        List<Chapter> deckOfChapters = new List<Chapter>();

        List<Chapter> completedChapters = RunManager.instance.completedChaptersList;

        // If this is not empty, then we were asked for a reroll, and we don't want to get the same chapters twice
        List<Chapter> previousSelectionOfChapters = selectionOfNextChaptersList;

        /// COUNT ALL WAVES IN CHAPTERS
        /*
        allWavesDico = new Dictionary<string, int>();
        */

        int numberOfChaptersInSelection = chaptersData.chapterCountInSelection;
        bool preventChaptersFromPreviousSelection = true;
        bool deckIsReady = false;
        while (!deckIsReady)
        {
            foreach (KeyValuePair<string, Chapter> chapterKeyValue in allChaptersDico)
            {
                Chapter currentChapter = chapterKeyValue.Value;

                /// COUNT ALL WAVES IN CHAPTERS
                /*
                foreach (Wave wave in currentChapter.chapterData.waves)
                {
                    if (allWavesDico.ContainsKey(wave.name))
                    {
                        allWavesDico[wave.name] = allWavesDico[wave.name] + 1;
                    }
                    else
                    {
                        allWavesDico.Add(wave.name, 1);
                    }
                }
                */

                if (currentChapter.chapterID == toadEndChapterForSpecialStuff.chapterID)
                {
                    if (currentChapter.attemptCountByCharacters.Count > 0)
                    {
                        continue;
                    }
                }

                if (preventChaptersFromPreviousSelection && previousSelectionOfChapters.Contains(currentChapter))
                {
                    // This chapter was already part of previous selection and we asked for a reroll, so let's ignore this one and move on
                    continue;
                }

                List<ChapterConditionsChunk> currentChapterConditionsChunksList = currentChapter.chapterData.conditions;

                if (!currentChapter.chapterData.canBePlayedMultipleTimesInOneRun && completedChapters.Contains(currentChapter))
                {
                    // This chapter has already been played and can't be played more than once
                    continue;
                }

                bool chapterConditionsAreMet = (currentChapterConditionsChunksList.Count == 0); // particular case if there are no conditions

                float playerDistanceFromSpawn = RunManager.instance.player.transform.position.magnitude;
                float playerDotRight = Vector3.Dot(RunManager.instance.player.transform.position, Vector2.right);
                float playerDotUp = Vector3.Dot(RunManager.instance.player.transform.position, Vector2.up);
                DirectionNESW playerDirectionFromSpawn = (Mathf.Abs(playerDotRight) > Mathf.Abs(playerDotUp)) ? (playerDotRight > 0 ? DirectionNESW.EAST : DirectionNESW.WEST) : (playerDotUp > 0 ? DirectionNESW.NORTH : DirectionNESW.SOUTH);

                // Check each chunk of conditions, until at least one is valid (chunk valid = all conditions met)
                foreach (ChapterConditionsChunk conditionChunk in currentChapterConditionsChunksList)
                {
                    bool conditionChunkIsValid = true;
                    foreach (ChapterCondition condition in conditionChunk.conditionsList)
                    {
                        switch (condition.conditionType)
                        {
                            case ChapterConditionType.CHAPTER_COUNT:
                                int currentChapterCount = RunManager.instance.GetChapterCount();
                                conditionChunkIsValid = (currentChapterCount >= condition.minChapterCount) && (currentChapterCount <= condition.maxChapterCount);
                                break;
                            case ChapterConditionType.CHARACTER:
                                conditionChunkIsValid = (RunManager.instance.currentPlayedCharacter.characterID.Equals(condition.characterData.characterID));
                                break;
                            case ChapterConditionType.ENVIRONMENT:
                                conditionChunkIsValid = true; // TODO: use condition.environmentType to compare to the current environment
                                break;
                            case ChapterConditionType.FRIEND:
                                conditionChunkIsValid = FriendsManager.instance.HasPermanentFriend(condition.friendType);
                                break;
                            case ChapterConditionType.HAT:
                                conditionChunkIsValid = GameManager.instance.player.HasHat(condition.hatType);
                                break;
                            case ChapterConditionType.PLAYED_CHAPTER:
                                Chapter c = completedChapters.FirstOrDefault(x => x.chapterData.Equals(condition.chapterData));
                                if (condition.chapterDataMustBeLatestChapterPlayed)
                                {
                                    conditionChunkIsValid = false;
                                    if (completedChapters.Count > 0)
                                    {
                                        Chapter latestChapterPlayed = completedChapters[completedChapters.Count - 1];
                                        conditionChunkIsValid = (latestChapterPlayed.chapterData.Equals(condition.chapterData));
                                    }
                                }
                                else
                                {
                                    conditionChunkIsValid = (c != null);
                                }
                                break;
                            case ChapterConditionType.RUN_ITEM:
                                conditionChunkIsValid = (RunManager.instance.GetLevelForItem(condition.itemName) > 0);
                                break;
                            case ChapterConditionType.UNLOCKED:
                                conditionChunkIsValid = currentChapter.unlocked;
                                break;
                            case ChapterConditionType.FRIEND_COUNT:
                                int friendsCount = FriendsManager.instance.HasPermanentFriendsCount();
                                conditionChunkIsValid = (friendsCount >= condition.minFriendsCount) && (condition.maxFriendsCount == ChapterCondition.MAXFRIENDSCOUNTINCONDITION || friendsCount <= condition.maxFriendsCount);
                                break;
                            case ChapterConditionType.BOUNTIES_EATEN_IN_PREVIOUS_CHAPTER:
                                int count = 0;
                                try
                                {
                                    count = RunManager.instance.GetPreviousChapterBountyEatCount();
                                }
                                catch (Exception)
                                {
                                    Debug.Log("The chapter condition BOUNTIES_EATEN_IN_PREVIOUS_CHAPTER is set on a chapter that appeared as the first chapter, chapter ID: " + currentChapter.chapterID);
                                    throw;
                                }
                                conditionChunkIsValid = (count >= condition.minBountiesEaten);
                                break;
                            case ChapterConditionType.DISTANCE_FROM_SPAWN:
                                conditionChunkIsValid = (condition.minDistanceFromSpawn == 0 || playerDistanceFromSpawn >= condition.minDistanceFromSpawn);
                                conditionChunkIsValid &= (condition.maxDistanceFromSpawn == 0 || playerDistanceFromSpawn <= condition.maxDistanceFromSpawn);
                                break;
                            case ChapterConditionType.DISTANCE_FROM_SPAWN_IN_DIRECTION:
                                conditionChunkIsValid = (condition.minDistanceFromSpawn == 0 || playerDistanceFromSpawn >= condition.minDistanceFromSpawn);
                                conditionChunkIsValid &= (condition.maxDistanceFromSpawn == 0 || playerDistanceFromSpawn <= condition.maxDistanceFromSpawn);
                                conditionChunkIsValid &= (playerDirectionFromSpawn == condition.direction);
                                break;
                        }
                        conditionChunkIsValid = condition.not ? (!conditionChunkIsValid) : (conditionChunkIsValid); // apply a NOT if needed
                        if (!conditionChunkIsValid)
                        {
                            break; // one condition was false so the whole chunk is invalid
                        }
                    }
                    if (conditionChunkIsValid)
                    {
                        chapterConditionsAreMet = true;
                        break; // one chunk was true, it's enough for the chapter to be included in the deck
                    }
                }

                bool chapterCanBeAddedToTheDeck = chapterConditionsAreMet; // conditions are met
                chapterCanBeAddedToTheDeck = chapterCanBeAddedToTheDeck && currentChapter.weight > 0; // weight is positive
                chapterCanBeAddedToTheDeck = chapterCanBeAddedToTheDeck && (!BuildManager.instance.demoBuild || currentChapter.chapterData.partOfDemo); // this chapter is part of the demo, or the current build is not the demo

                if (chapterCanBeAddedToTheDeck && !deckOfChapters.Contains(currentChapter))
                {
                    deckOfChapters.Add(currentChapter);
                }
            }

            if (deckOfChapters.Count >= numberOfChaptersInSelection || !preventChaptersFromPreviousSelection)
            {
                deckIsReady = true;
            }
            if (preventChaptersFromPreviousSelection)
            {
                preventChaptersFromPreviousSelection = false;
                if (forcePreventUsingChaptersFromPreviousSelection)
                {
                    deckIsReady = true;
                }
            }
        }

        /// DISPLAY ALL WAVES COUNT IN CHAPTERS
        /*
        string log = "WAVE\n";
        List<string> waveNamesList = new List<string>();
        foreach (KeyValuePair<string, int> waveKeyValue in allWavesDico)
        {
            waveNamesList.Add(waveKeyValue.Key);
        }
        waveNamesList.Sort();
        foreach (string waveName in waveNamesList)
        {
            log += $"{waveName} : {allWavesDico[waveName]}\n";
        }
        Debug.Log(log);*/

        return deckOfChapters;
    }

    private Chapter GetRandomChapterFromDeck(List<Chapter> deckOfChapters)
    {
        // Compute the total sum of all weights
        float sumWeights = 0;
        foreach (Chapter chapter in deckOfChapters)
        {
            sumWeights += chapter.weight;
        }

        // Pick a random value between 0 and the weights sum
        float randomValue = UnityEngine.Random.Range(0, sumWeights);

        // Find the Chapter from this random value
        Chapter resultChapter = null;
        foreach (Chapter chapter in deckOfChapters)
        {
            randomValue -= chapter.weight;
            if (randomValue <= 0)
            {
                resultChapter = chapter;
                break;
            }
        }

        return resultChapter;
    }

    /// <summary>
    /// Will compute the current deck of chapter then choose a certain amount (passed as parameter) from this deck.
    /// </summary>
    /// <param name="chapterCount"></param>
    private void SelectNextPossibleChapters(int chapterCount, bool acceptChaptersFromPreviousSelection)
    {
        string log = "Chapter selection - ";

        if (acceptChaptersFromPreviousSelection)
        {
            // Previous selection doesn't matter, we erase it before building the deck
            selectionOfNextChaptersList = new List<Chapter>();
        }

        // Create the deck of chapters to choose from
        List<Chapter> deckOfChapters = GetDeckOfChapters(false);
        if (logsVerboseLevel == VerboseLevel.MAXIMAL)
        {
            log += "Deck of Chapters: ";
            foreach (Chapter chapter in deckOfChapters)
            {
                log += chapter.chapterID + " (weight = " + chapter.weight.ToString("0.00") + "); ";
            }
            Debug.Log(log);
        }

        // Choose a number of chapters from the deck
        // If there's not enough, just show the whole deck
        selectionOfNextChaptersList = new List<Chapter>();
        log = "Chapter selection - ";
        int actualChapterCount = (deckOfChapters.Count < chapterCount) ? deckOfChapters.Count : chapterCount;
        while (selectionOfNextChaptersList.Count < actualChapterCount)
        {
            Chapter selectedChapter = GetRandomChapterFromDeck(deckOfChapters);
            deckOfChapters.Remove(selectedChapter);
            selectionOfNextChaptersList.Add(selectedChapter);
            log += selectedChapter.chapterID + ", ";
        }

        if (logsVerboseLevel == VerboseLevel.MAXIMAL)
        {
            Debug.Log(log);
        }
    }

    private void UpdateRerollPostit()
    {
        bool rerollWouldGiveAtLeastOneNewChapter = WouldChapterRerollGiveAtLeastOneNewChapter(out int numberOfSimilarChaptersAfterReroll);
        bool hasAtLeastOneReroll = (GameManager.instance.player.rerolls > 0);

        rerollInfinitePostIt.SetActive(isFirstChapter && rerollWouldGiveAtLeastOneNewChapter && GameManager.instance.gameData.attempts > 0);
        rerollInfinitePostIt.GetComponent<CanvasGroup>().blocksRaycasts = rerollInfinitePostIt.activeSelf;


        rerollPostIt.SetActive(!isFirstChapter && hasAtLeastOneReroll);
        rerollPostItCountTextMesh.SetText($"x{GameManager.instance.player.rerolls}");
        rerollPostItButton.interactable = hasAtLeastOneReroll && rerollWouldGiveAtLeastOneNewChapter;
        if (rerollPostIt.activeSelf)
        {
            rerollPostIt.GetComponent<CanvasGroup>().blocksRaycasts = !isFirstChapter && hasAtLeastOneReroll && rerollWouldGiveAtLeastOneNewChapter;
        }
    }

    public void SetChapterCountInSelection(int newChapterCount)
    {
        chaptersData.chapterCountInSelection = newChapterCount;
        SaveDataManager.instance.isSaveDataDirty = true;
    }

    private void NewSelectionOfChapters(bool acceptChaptersFromPreviousSelection)
    {
        int numberOfChaptersInSelection = chaptersData.chapterCountInSelection;

        if (GameManager.instance.gameData.attempts == 0 && isFirstChapter)
        {
            // This is the very first chapter of the very first run, so the only available chapter is the tutorial
            selectionOfNextChaptersList = new List<Chapter>();
            selectionOfNextChaptersList.Add(allChaptersDico[tutorialChapterScriptableObject.chapterID]);
        }
        else
        {
            // Choose a number of chapters from the list of available chapters
            SelectNextPossibleChapters(numberOfChaptersInSelection, acceptChaptersFromPreviousSelection);
        }

        // Show the chapters selection
        foreach (ChapterButtonBehaviour chapterButton in chapterButtonsList) // Set up automatic navigation.
        {
            chapterButton.gameObject.SetActive(false);
            Navigation chapterButtonNav = chapterButton.GetComponent<Button>().navigation;
            chapterButtonNav.mode = Navigation.Mode.Automatic;
            chapterButton.GetComponent<Button>().navigation = chapterButtonNav;
        }

        for (int i = 0; i < selectionOfNextChaptersList.Count; i++)
        {
            Chapter chapter = selectionOfNextChaptersList[i];
            ChapterButtonBehaviour chapterButton = chapterButtonsList[i];

            #region Chapter button navigation (not used, uses automatic navigation instead)

            /*
            // Rubber Frog approved code below!

            // Set button navigation.
            Navigation chapterButtonNav = chapterButton.GetComponent<Button>().navigation;
            chapterButtonNav.mode = Navigation.Mode.Explicit;
            Button rerollButton = null;
            if (rerollInfinitePostIt.activeSelf)
            {
                rerollButton = rerollInfinitePostIt.GetComponentInChildren<Button>().interactable ? rerollInfinitePostIt.GetComponentInChildren<Button>() : null;
            }
            else if (rerollPostIt.activeSelf)
            {
                rerollButton = rerollPostIt.GetComponentInChildren<Button>().interactable ? rerollPostIt.GetComponentInChildren<Button>() : null;
            }

            #region Navigation set up helpers
            Button GetChapterButton(int chapterButtonNr)
            {
                return chapterButtonsList[chapterButtonNr - 1].GetComponent<Button>();
            }

            Button GetChapterButtonOrBackButtonOrSelf(int chapterButtonNr, int selfButtonNr)
            {
                if (selectionOfNextChaptersList.Count >= chapterButtonNr || (selectionOfNextChaptersList.Count == 4 && chapterButtonNr == 5))
                {
                    return GetChapterButton(chapterButtonNr);
                }
                else if (backButton.gameObject.activeSelf)
                {
                    return backButton;
                }
                else
                {
                    return GetChapterButton(selfButtonNr);
                }
            }

            Button GetChapterButtonOrBackButtonOrRerollButtonOrSelf(int chapterButtonNr, int selfButtonNr)
            {
                if (selectionOfNextChaptersList.Count >= chapterButtonNr || (selectionOfNextChaptersList.Count == 4 && chapterButtonNr == 5))
                {
                    return GetChapterButton(chapterButtonNr);
                }
                else if (backButton.gameObject.activeSelf)
                {
                    return backButton;
                }
                else if (rerollButton != null)
                {
                    return rerollButton;
                }
                else
                {
                    return GetChapterButton(selfButtonNr);
                }
            }

            Button GetBackButtonOrChapterButtonOrRerollButtonOrSelf(int chapterButtonNr, int selfButtonNr)
            {
                if (backButton.gameObject.activeSelf)
                {
                    return backButton;
                }
                else if (selectionOfNextChaptersList.Count >= chapterButtonNr || (selectionOfNextChaptersList.Count == 4 && chapterButtonNr == 5))
                {
                    return GetChapterButton(chapterButtonNr);
                }
                else if (rerollButton != null)
                {
                    return rerollButton;
                }
                else
                {
                    return GetChapterButton(selfButtonNr);
                }
            }

            Button GetBackButtonOrChapterButton(int chapterButtonNr)
            {
                return backButton.gameObject.activeSelf ? backButton : GetChapterButton(chapterButtonNr);
            }

            Button GetRerollButtonOrChapterButton(int chapterButtonNr)
            {
                return rerollButton ?? GetChapterButton(chapterButtonNr);
            }

            Button GetRerollButtonOrBackButtonOrChapterButton(int chapterButtonNr)
            {
                // If there is no back or reroll button navigate to the given button instead. Might be the same button as navigated from.
                if (rerollButton == null && !backButton.gameObject.activeSelf)
                {
                    return GetChapterButton(chapterButtonNr);
                }
                else
                {
                    return rerollButton ?? backButton;
                }
            }

            Button GetBackButtonOrRerollButtonOrChapterButton(int chapterButtonNr)
            {
                // If there is no back or reroll button navigate to the given button instead. Might be the same button as navigated from.
                if (rerollButton == null && !backButton.gameObject.activeSelf)
                {
                    return GetChapterButton(chapterButtonNr);
                }
                else
                {
                    return backButton.gameObject.activeSelf ? backButton : rerollButton;
                }
            }
            #endregion Navigation set up helpers

            // Depending on which button is being initilaized and how many buttons are going to be showed the navigation between all buttons on this screen are different.
            int thisButtonNr = chapterButtonIndex + 1;
            // This int will be five if there is 4 or 5 chapters, because if there is four buttons chapter button 2 - 5 is used and I need the number of the last button.
            int indexOfLastUsedChapterButton = selectionOfNextChaptersList.Count == 4 ? 5 : selectionOfNextChaptersList.Count;
            switch (thisButtonNr)
            {
                case 1:
                    // OLD chapterButtonNav.selectOnUp = GetChapterButton(indexOfLastUsedChapterButton >= 4 ? 4 : indexOfLastUsedChapterButton == 3 ? 3 : indexOfLastUsedChapterButton == 2 ? 2 : 1); // DONE 4, 3, 2, back, self.
                    chapterButtonNav.selectOnUp = GetRerollButtonOrChapterButton(indexOfLastUsedChapterButton >= 3 ? 3 : indexOfLastUsedChapterButton == 2 ? 2 : 1); // DONE reroll, 3, 2, self.
                    // OLD (and probably wrong?) chapterButtonNav.selectOnDown = GetChapterButton(indexOfLastUsedChapterButton >= 2 ? 2 : 1); // DONE 2, back, self. (Had reroll between back and self but removed it after testing).
                    chapterButtonNav.selectOnDown = indexOfLastUsedChapterButton > 1 ? GetChapterButton(2) : GetRerollButtonOrChapterButton(1); // DONE 2, reroll, self.
                    chapterButtonNav.selectOnLeft = GetBackButtonOrChapterButtonOrRerollButtonOrSelf(5, 1); // DONE back, 5, reroll, self.
                    chapterButtonNav.selectOnRight = GetRerollButtonOrBackButtonOrChapterButton(indexOfLastUsedChapterButton == 5 ? 5 : 1); // DONE reroll, back, 5, self.
                    break;

                case 2: // Has special case for 4 buttons
                    // OLD chapterButtonNav.selectOnUp = GetChapterButton(selectionOfNextChaptersList.Count != 4 ? 1 : 4); // DONE if 4 buttons = 4, else 1. <- SPECIAL
                    chapterButtonNav.selectOnUp = selectionOfNextChaptersList.Count != 4 ? GetChapterButton(1) : GetRerollButtonOrChapterButton(3); // DONE if 4 buttons = reroll, 3, else 1. <- SPECIAL
                    // OLD chapterButtonNav.selectOnDown = GetChapterButton(indexOfLastUsedChapterButton >= 3 ? 3 : 1); // DONE 3, 1.
                    chapterButtonNav.selectOnDown = indexOfLastUsedChapterButton >= 3 ? GetChapterButton(3) : GetRerollButtonOrChapterButton(1); // DONE 3, reroll, 1.
                    chapterButtonNav.selectOnLeft = GetChapterButtonOrBackButtonOrRerollButtonOrSelf(5, 2); // DONE 5, back, reroll, self.
                    chapterButtonNav.selectOnRight = GetRerollButtonOrBackButtonOrChapterButton(indexOfLastUsedChapterButton == 5 ? 5 : 2); // DONE reroll, back, 5, self.
                    break;

                case 3:
                    // OLD chapterButtonNav.selectOnUp = GetChapterButton(2); // DONE 2.
                    chapterButtonNav.selectOnUp = GetChapterButton(2); // DONE 2.
                    // OLD chapterButtonNav.selectOnDown = GetChapterButton(indexOfLastUsedChapterButton == 5 ? 5 : 1); // DONE 5, 1.
                    chapterButtonNav.selectOnDown = GetRerollButtonOrChapterButton(selectionOfNextChaptersList.Count != 4 ? 1 : 2); // DONE reroll, 1, 2.
                    chapterButtonNav.selectOnLeft = GetChapterButtonOrBackButtonOrRerollButtonOrSelf(4, 3); // DONE 4, back, reroll, self.
                    chapterButtonNav.selectOnRight = GetRerollButtonOrBackButtonOrChapterButton(indexOfLastUsedChapterButton < 5 ? 3 : 4); // DONE reroll, back, 4, self.
                    break;

                case 4: // Has special case for 4 buttons.
                    // OLD chapterButtonNav.selectOnUp = GetChapterButton(5); // DONE 5.
                    chapterButtonNav.selectOnUp = GetChapterButton(5); // DONE 5.
                    // OLD chapterButtonNav.selectOnDown = GetChapterButton(selectionOfNextChaptersList.Count != 4 ? 1 : 2); // DONE if 4 buttons = 2, else 1. <- SPECIAL
                    chapterButtonNav.selectOnDown = GetBackButtonOrChapterButton(5); // DONE back, 5.
                    chapterButtonNav.selectOnLeft = GetBackButtonOrRerollButtonOrChapterButton(3); // DONE back, reroll, 3.
                    chapterButtonNav.selectOnRight = GetChapterButton(3); // DONE 3.
                    break;

                case 5:
                    // OLD chapterButtonNav.selectOnUp = GetChapterButton(3); // DONE 3.
                    chapterButtonNav.selectOnUp = GetBackButtonOrChapterButton(4); // DONE back, 4.
                    // OLD chapterButtonNav.selectOnDown = GetChapterButton(4); // DONE 4.
                    chapterButtonNav.selectOnDown = GetChapterButton(4); // DONE 4.
                    chapterButtonNav.selectOnLeft = GetBackButtonOrRerollButtonOrChapterButton(2); // DONE back, reroll, 2. (thoght about making it go to 1 if it exist but decided against.)
                    chapterButtonNav.selectOnRight = GetChapterButton(2); // DONE 2.
                    break;

                default:
                    break;
            }

            chapterButton.GetComponent<Button>().navigation = chapterButtonNav;

            // After last chapter button is set.
            if (chapterButtonIndex + 1 == selectionOfNextChaptersList.Count)
            {
                if (backButton.gameObject.activeSelf)
                {
                    // Back button navigation.
                    Navigation backButtonNav = backButton.navigation;
                    backButtonNav.mode = Navigation.Mode.Explicit;

                    // Has special cases for 4 buttons.
                    // OLD backButtonNav.selectOnUp = GetChapterButton(indexOfLastUsedChapterButton >= 4 ? 4 : indexOfLastUsedChapterButton == 3 ? 3 : indexOfLastUsedChapterButton == 2 ? 2 : 1); // DONE 4, 3, 2, 1.
                    backButtonNav.selectOnUp = GetChapterButton(indexOfLastUsedChapterButton >= 4 ? 4 : indexOfLastUsedChapterButton == 3 ? 3 : indexOfLastUsedChapterButton == 2 ? 2 : 1); // DONE 4, 3, 2, 1.
                    // OLD backButtonNav.selectOnDown = GetChapterButton(indexOfLastUsedChapterButton == 5 ? 5 : 1); // DONE 5, 1.
                    backButtonNav.selectOnDown = GetChapterButton(indexOfLastUsedChapterButton == 5 ? 5 : 1); // DONE 5, 1.
                    backButtonNav.selectOnLeft = GetRerollButtonOrChapterButton(selectionOfNextChaptersList.Count != 4 ? 1 : 2); // DONE reroll, if 4 buttons = 2, else 1. <- SPECIAL
                    backButtonNav.selectOnRight = GetChapterButton(selectionOfNextChaptersList.Count >= 4 ? 5 : 1); // DONE if 4 buttons = 5, else 1. <- SPECIAL

                    backButton.navigation = backButtonNav;
                }

                // Reroll button navigation.
                if (rerollButton != null)
                {
                    Navigation rerollButtonNav = rerollButton.navigation;
                    rerollButtonNav.mode = Navigation.Mode.Explicit;

                    // Has special case for 4 buttons.
                    // OLD rerollButtonNav.selectOnUp = GetBackButtonOrChapterButton(indexOfLastUsedChapterButton >= 3 ? 3 : indexOfLastUsedChapterButton == 2 ? 2 : 1); // DONE back, 3, 2, 1.
                    rerollButtonNav.selectOnUp = GetChapterButton(indexOfLastUsedChapterButton >= 3 ? 3 : indexOfLastUsedChapterButton == 2 ? 2 : 1); // DONE 3, 2, 1.
                    // OLD rerollButtonNav.selectOnDown = GetChapterButton(indexOfLastUsedChapterButton > 1 ? 2 : 1); // DONE 2, 1.
                    rerollButtonNav.selectOnDown = GetChapterButton(selectionOfNextChaptersList.Count != 4 ? 1 : 2); // DONE 1, 2.
                    rerollButtonNav.selectOnLeft = GetChapterButton(selectionOfNextChaptersList.Count != 4 ? 1 : 2); // DONE if 4 buttons = 2, else 1. <- SPECIAL
                    rerollButtonNav.selectOnRight = GetBackButtonOrChapterButton(indexOfLastUsedChapterButton < 5 ? 1 : 5); // DONE back, 5, 1.

                    rerollButton.navigation = rerollButtonNav;
                }
            }
            */

            #endregion Chapter button navigation

            chapterButton.gameObject.SetActive(true);
            chapterButton.Initialize(chapter);
        }
    }

    public void ConfirmRerollChapterSelection()
    {
        if (isFirstChapter)
        {
            NewSelectionOfChapters(false);
        }
        else if (GameManager.instance.player.rerolls > 0)
        {
            GameManager.instance.player.rerolls--;
            NewSelectionOfChapters(false);
            UpdateRerollPostit();

            if (GameManager.instance.player.rerolls < 1)
            {
                // Set new selected button.
                UIManager.instance.SetSelectedButton(chapterButtonsList[0].gameObject);
            }
        }

        displayedChapterIndex = 0;
        DisplayChapter(selectionOfNextChaptersList[0], chapterInfoPanel);
        chapterButtonsList[0].SetDisplayedColor();
        chapterButtonsList[0].SetHighlightedColor(false);
        for (int chapterButtonIndex = 1; chapterButtonIndex < chapterButtonsList.Count; chapterButtonIndex++)
        {
            chapterButtonsList[chapterButtonIndex].SetNormalColor();
        }
    }

    public void RerollChapterSelection()
    {
        bool rerollWouldGiveAtLeastOneNewChapter = WouldChapterRerollGiveAtLeastOneNewChapter(out int numberOfSimilarChaptersAfterReroll);

        if (isFirstChapter || (numberOfSimilarChaptersAfterReroll <= 0))
        {
            ConfirmRerollChapterSelection(); // Do not show a warning, just reroll right away
        }
        else
        {
            UIManager.instance.ShowRerollWarningConfirmationPanel(true); // Show a warning
        }
    }

    public void ShowChapterSelection(int currentChapterCount)
    {
        MusicManager.instance.PlaySuperFrogMusic(false); // in case the previous chapter ended while super frog was active.

        // Update the top text
        int chapterCount = currentChapterCount;
        string chapterIntro = "";
        backButton.gameObject.SetActive((chapterCount == 0));
        if (chapterCount == 0)
        {
            chapterIntro = "How does the story start?";
        }
        else if (chapterCount == 5)
        {
            chapterIntro = "How does that story end?";
        }
        else
        {
            chapterIntro = "What happens in chapter " + chapterCount.ToString() + "?";
        }
        chapterSelectionTopText.text = chapterIntro;

        isFirstChapter = (currentChapterCount == 0);
        NewSelectionOfChapters(true);
        UpdateRerollPostit();
        displayedChapterIndex = 0;
        DisplayChapter(selectionOfNextChaptersList[0], chapterInfoPanel);
        chapterButtonsList[0].SetDisplayedColor();
        chapterButtonsList[0].SetHighlightedColor(true);
        for (int chapterButtonIndex = 1; chapterButtonIndex < chapterButtonsList.Count; chapterButtonIndex++)
        {
            chapterButtonsList[chapterButtonIndex].SetNormalColor();
        }

        // Call the UIManager to display the chapter selection screen
        UIManager.instance.ShowChapterSelectionScreen((chapterCount == 0));
        chapterChoiceIsVisible = true;
    }

    public void PlaySelectedChapter()
    {
        SelectChapter(displayedChapterIndex);
    }

    /// <summary>
    /// Set ups the selected chapters info on a selected info panel.
    /// </summary>
    /// <param name="chapterInfo"></param>
    /// <param name="infoPanel"></param>
    public void DisplayChapter(Chapter chapterInfo, ChapterInfoBehaviour infoPanel)
    {
        infoPanel.DisplayChapter(chapterInfo, infoPanel);
    }

    public void ClickChapterButtonToDisplayInfo(int index)
    {
        displayedChapterIndex = index;
        DisplayChapter(selectionOfNextChaptersList[index], chapterInfoPanel);
        for (int chapterButtonIndex = 0; chapterButtonIndex < chapterButtonsList.Count; chapterButtonIndex++)
        {
            ChapterButtonBehaviour thisChapterButton = chapterButtonsList[chapterButtonIndex];
            if (chapterButtonIndex == displayedChapterIndex)
            {
                thisChapterButton.SetDisplayedColor();
            }
            else
            {
                thisChapterButton.SetNormalColor();
            }
        }
    }

    /// <summary>
    /// Select the Chapter in the selection list from its index.
    /// This method is called by the Button pressed
    /// </summary>
    /// <param name="index"></param>
    public void SelectChapter(int index)
    {
        Chapter chapterInfo = selectionOfNextChaptersList[index];

        if (logsVerboseLevel == VerboseLevel.MAXIMAL)
        {
            Debug.Log("Chapter - Select " + chapterInfo.chapterID);
        }
        StartChapter(chapterInfo);
    }

    public void StartChapter(Chapter chapter)
    {
        // Save information about the current character attempting to play that chapter        
        CharacterCount charCount = chapter.attemptCountByCharacters.FirstOrDefault(x => x.characterIdentifier.Equals(RunManager.instance.currentPlayedCharacter.characterID));
        if (charCount == null)
        {
            // first time this character attempts this chapter
            charCount = new CharacterCount() { characterIdentifier = RunManager.instance.currentPlayedCharacter.characterID, counter = 1 };
            chapter.attemptCountByCharacters.Add(charCount);
        }
        else
        {
            charCount.counter++;
        }
        SaveDataManager.instance.isSaveDataDirty = true;

        // Deal with all weight changes to other chapters
        foreach (ChapterWeightChange weightChange in chapter.chapterData.weightChanges)
        {
            if (allChaptersDico.ContainsKey(weightChange.chapter.chapterID))
            {
                Chapter weightChangeChapter = allChaptersDico[weightChange.chapter.chapterID];
                weightChangeChapter.weight += weightChange.weightChange;
            }
        }

        // Tell the RunManager to start that chapter
        RunManager.instance.StartChapter(chapter);
    }

    public void CompleteChapter(Chapter chapter, PlayableCharacter playedCharacter)
    {
        // Save information about the current character completing that chapter        
        CharacterCount charCount = chapter.completionCountByCharacters.FirstOrDefault(x => x.characterIdentifier.Equals(playedCharacter.characterID));
        if (charCount == null)
        {
            // first time this character attempts this chapter
            charCount = new CharacterCount() { characterIdentifier = playedCharacter.characterID, counter = 1 };
            chapter.completionCountByCharacters.Add(charCount);
        }
        else
        {
            charCount.counter++;
        }
        SaveDataManager.instance.isSaveDataDirty = true;
    }

    public bool IsFixedCollectibleShownByCompassInChapter(Chapter chapter, FixedCollectible collectible, out bool collectibleHasBeenFoundOnce)
    {
        // Get information about the collectible being found
        FixedCollectibleFound collectibleFound = chapter.fixedCollectiblesFoundList.FirstOrDefault(x => x.collectibleIdentifier.Equals(FixedCollectibleFound.GetIdentifierFromCoordinates(collectible.tileCoordinates)));
        collectibleHasBeenFoundOnce = (collectibleFound != null && collectibleFound.hasBeenFoundOnce);
        bool compassShowsCollectible = collectibleHasBeenFoundOnce;
        if (!compassShowsCollectible)
        {
            compassShowsCollectible = (GameManager.instance.GetCompassLevel() >= collectible.compassLevel);
        }

        return compassShowsCollectible;
    }

    public void FoundFixedCollectible(Chapter chapter, FixedCollectible collectible)
    {
        // Save information about the collectible being found
        FixedCollectibleFound collectibleFound = chapter.fixedCollectiblesFoundList.FirstOrDefault(x => x.collectibleIdentifier.Equals(FixedCollectibleFound.GetIdentifierFromCoordinates(collectible.tileCoordinates)));
        if (collectibleFound == null)
        {
            // first time this collectible is found
            collectibleFound = new FixedCollectibleFound() { collectibleIdentifier = FixedCollectibleFound.GetIdentifierFromCoordinates(collectible.tileCoordinates), hasBeenFoundOnce = true };
            chapter.fixedCollectiblesFoundList.Add(collectibleFound);
        }
        else
        {
            // This should never happen
            collectibleFound.hasBeenFoundOnce = true;
        }
        SaveDataManager.instance.isSaveDataDirty = true;
    }

    public bool DoesChapterContainFixedItemsThatHaveNeverBeenFound(Chapter chapter)
    {
        bool thereAreItemsThatWereNeverFound = chapter.fixedCollectiblesFoundList.Count() < chapter.chapterData.specialCollectiblesOnTheMap.Count();

        if (!thereAreItemsThatWereNeverFound)
        {
            // We found more collectibles than the collectibles in that chapter
            // We have to check them one by one
            foreach (FixedCollectible collectible in chapter.chapterData.specialCollectiblesOnTheMap)
            {
                string collectibleIdentifier = FixedCollectibleFound.GetIdentifierFromCoordinates(collectible.tileCoordinates);
                FixedCollectibleFound collectibleFoundInfo = chapter.fixedCollectiblesFoundList.FirstOrDefault(x => x.collectibleIdentifier.Equals(collectibleIdentifier));
                if (collectibleFoundInfo == null || !collectibleFoundInfo.hasBeenFoundOnce)
                {
                    thereAreItemsThatWereNeverFound = true;
                    break;
                }
            }
        }

        if (logsVerboseLevel == VerboseLevel.MAXIMAL)
        {
            Debug.Log($"Chapter Manager - DoesChapterContainFixedItemsThatHaveNeverBeenFound for chapter {chapter.chapterID} returned {thereAreItemsThatWereNeverFound}");
        }

        return thereAreItemsThatWereNeverFound;
    }

    public bool DoesChapterUnlockAnAchievementOrAnUnplayedChapter(Chapter chapter, int chapterCount)
    {
        bool achievementOrUnplayedChapterHasBeenFound = false;

        // check if this chapter is part of a condition for an achievement
        achievementOrUnplayedChapterHasBeenFound = AchievementManager.instance.IsChapterPartOfALockedAchievement(chapter);

        if (logsVerboseLevel == VerboseLevel.MAXIMAL)
        {
            Debug.Log($"Chapter Manager - IsChapterPartOfALockedAchievement for chapter {chapter.chapterID} returned {achievementOrUnplayedChapterHasBeenFound}");
        }

        // if it is not, check any chapter that would be added to the deck by playing this one, and call the same method with this new chapter
        if (!achievementOrUnplayedChapterHasBeenFound && chapterCount < 5)
        {
            foreach (KeyValuePair<string, Chapter> chapterKeyValue in allChaptersDico)
            {
                Chapter testedChapter = chapterKeyValue.Value;
                foreach (ChapterConditionsChunk conditionChunk in testedChapter.chapterData.conditions)
                {
                    foreach (ChapterCondition condition in conditionChunk.conditionsList)
                    {
                        if (condition.conditionType == ChapterConditionType.PLAYED_CHAPTER
                            && !condition.not
                            && condition.chapterData.chapterID.Equals(chapter.chapterID))
                        {
                            achievementOrUnplayedChapterHasBeenFound |= DoesChapterUnlockAnAchievementOrAnUnplayedChapter(testedChapter, chapterCount + 1);

                            achievementOrUnplayedChapterHasBeenFound |= DoesChapterContainFixedItemsThatHaveNeverBeenFound(testedChapter);

                            bool isChapterNew = true;
                            foreach (CharacterCount count in testedChapter.attemptCountByCharacters)
                            {
                                isChapterNew &= (count.counter == 0);
                            }
                            achievementOrUnplayedChapterHasBeenFound |= isChapterNew; // chapter has never been played

                            if (achievementOrUnplayedChapterHasBeenFound)
                            {
                                break;
                            }
                        }
                    }
                    if (achievementOrUnplayedChapterHasBeenFound)
                    {
                        break;
                    }
                }
                if (achievementOrUnplayedChapterHasBeenFound)
                {
                    break;
                }
            }
        }

        if (logsVerboseLevel == VerboseLevel.MAXIMAL)
        {
            Debug.Log($"Chapter Manager - DoesChapterUnlockAnAchievementOrAnUnplayedChapter for chapter {chapter.chapterID} returned {achievementOrUnplayedChapterHasBeenFound}");
        }

        // Stop as soon as we get "true" or through
        return achievementOrUnplayedChapterHasBeenFound;
    }

    #region Chapter Start Screen

    /// <summary>
    /// Display the chapter start screen, a black screen with the chapter main information.
    /// Serves as a breather before the game starts.
    /// </summary>
    /// <param name="chapterCount"></param>
    /// <param name="chapter"></param>
    public void ShowChapterStartScreen(int chapterCount, Chapter chapter)
    {
        // Debug log
        if (logsVerboseLevel == VerboseLevel.MAXIMAL)
        {
            Debug.Log("Chapter - Start screen - " + chapter.chapterID);
        }

        if (fadeOutChapterStartScreenCoroutine != null)
        {
            StopCoroutine(fadeOutChapterStartScreenCoroutine);
        }

        // Set background color
        chapterStartBackground.color = new Color(chapterStartBackground.color.r, chapterStartBackground.color.g, chapterStartBackground.color.b, 1);

        // Display title
        chapterStartTopText.text = "Chapter " + chapterCount.ToString();
        chapterStartTopText.color = new Color(chapterStartTopText.color.r, chapterStartTopText.color.g, chapterStartTopText.color.b, 1);
        chapterStartBottomText.text = chapter.chapterData.chapterTitle;
        chapterStartBottomText.color = new Color(chapterStartBottomText.color.r, chapterStartBottomText.color.g, chapterStartBottomText.color.b, 1);

        // Display icons        
        for (int i = 0; i < chapterStartIconsImageList.Count; i++)
        {
            chapterStartIconsImageList[i].gameObject.SetActive(false);
        }
        for (int i = 0; i < chapter.chapterData.icons.Count; i++)
        {
            chapterStartIconsImageList[i].color = new Color(chapterStartIconsImageList[i].color.r, chapterStartIconsImageList[i].color.g, chapterStartIconsImageList[i].color.b, 1);
            chapterStartIconsImageList[i].gameObject.SetActive(true);
            chapterStartIconsImageList[i].sprite = chapter.chapterData.icons[i];
        }

        UIManager.instance.ShowChapterStart();
    }

    public void FadeOutChapterStartScreen(float delay)
    {
        if (fadeOutChapterStartScreenCoroutine != null)
        {
            StopCoroutine(fadeOutChapterStartScreenCoroutine);
        }
        fadeOutChapterStartScreenCoroutine = StartCoroutine(FadeOutChapterStartScreenAsync(delay));
    }

    private void SetBackgroundAlpha(float alpha)
    {
        Color backgroundColor = new Color(chapterStartBackground.color.r, chapterStartBackground.color.g, chapterStartBackground.color.b, 1);
        Color transparentBackgroundColor = new Color(chapterStartBackground.color.r, chapterStartBackground.color.g, chapterStartBackground.color.b, 0);

        Color topTextColor = new Color(chapterStartTopText.color.r, chapterStartTopText.color.g, chapterStartTopText.color.b, 1);
        Color transparentTopTextColor = new Color(chapterStartTopText.color.r, chapterStartTopText.color.g, chapterStartTopText.color.b, 0);

        Color bottomTextColor = new Color(chapterStartBottomText.color.r, chapterStartBottomText.color.g, chapterStartBottomText.color.b, 1);
        Color transparentBottomTextColor = new Color(chapterStartBottomText.color.r, chapterStartBottomText.color.g, chapterStartBottomText.color.b, 0);

        Color iconColor = new Color(chapterStartIconsImageList[0].color.r, chapterStartIconsImageList[0].color.g, chapterStartIconsImageList[0].color.b, 1);
        Color transparentIconColor = new Color(chapterStartIconsImageList[0].color.r, chapterStartIconsImageList[0].color.g, chapterStartIconsImageList[0].color.b, 0);

        chapterStartBackground.color = Color.Lerp(backgroundColor, transparentBackgroundColor, alpha);
        chapterStartTopText.color = Color.Lerp(topTextColor, transparentTopTextColor, alpha);
        chapterStartBottomText.color = Color.Lerp(bottomTextColor, transparentBottomTextColor, alpha);
        foreach (Image iconImage in chapterStartIconsImageList)
        {
            iconImage.color = Color.Lerp(iconColor, transparentIconColor, alpha);
        }
    }

    private IEnumerator FadeOutChapterStartScreenAsync(float delay)
    {
        SetBackgroundAlpha(0);

        for (float alpha = 0; alpha <= 1; alpha += Time.deltaTime / delay)
        {
            SetBackgroundAlpha(alpha);

            yield return new WaitForEndOfFrame();
        }
    }

    #endregion

    #region Reset Chapters

    public void ResetChaptersWeights()
    {
        foreach (Chapter chapter in chaptersData.chaptersList)
        {
            chapter.weight = chapter.chapterData.startingWeight;
        }
    }

    /// <summary>
    /// Reset all chapters. Hard reset means setting everything back to start game values (locking chapters that were unlocked). 
    /// Soft reset means setting only the character counters back to their original values.
    /// </summary>
    /// <param name="hardReset"></param>
    public void ResetChapters(bool hardReset = false)
    {
        if (hardReset)
        {
            // A hard reset will reset everything to the start game values
            chaptersData.chaptersList.Clear();
            chaptersData.chapterCountInSelection = 3;
            foreach (ChapterData chapterData in chaptersScriptableObjectsList)
            {
                Chapter newChapter = new Chapter()
                {
                    chapterID = chapterData.chapterID,
                    chapterData = chapterData,
                    unlocked = chapterData.startingUnlockState,
                    weight = chapterData.startingWeight
                };
                chaptersData.chaptersList.Add(newChapter);
            }
        }
        else
        {
            // A soft reset will not lock chapters that were unlocked, but it will reset their character counters to start game values
            foreach (Chapter chapter in chaptersData.chaptersList)
            {
                chapter.weight = chapter.chapterData.startingWeight;
                chapter.attemptCountByCharacters.Clear();
                chapter.completionCountByCharacters.Clear();
                chapter.fixedCollectiblesFoundList.Clear();
            }
        }

        // Re-initialize the dictionary
        allChaptersDico = new Dictionary<string, Chapter>();
        foreach (Chapter chapter in chaptersData.chaptersList)
        {
            allChaptersDico.Add(chapter.chapterID, chapter);
        }
    }

    #endregion

    #region Load Data

    /// <summary>
    /// Update the chapters data using a ChaptersSaveData object, that was probably loaded from a file by the SaveDataManager.
    /// </summary>
    /// <param name="saveData"></param>
    public void SetChaptersData(ChaptersSaveData saveData)
    {
        chaptersData.chapterCountInSelection = 3;
        if (saveData.chapterCountInSelection >= 3)
        {
            chaptersData.chapterCountInSelection = saveData.chapterCountInSelection;
        }
        foreach (Chapter chapter in chaptersData.chaptersList)
        {
            Chapter chapterFromSave = saveData.chaptersList.FirstOrDefault(x => x.chapterID.Equals(chapter.chapterID));
            if (chapterFromSave != null)
            {
                chapter.unlocked = chapterFromSave.unlocked;
                chapter.attemptCountByCharacters = new List<CharacterCount>(chapterFromSave.attemptCountByCharacters);
                chapter.completionCountByCharacters = new List<CharacterCount>(chapterFromSave.completionCountByCharacters);
                if (chapterFromSave.fixedCollectiblesFoundList != null)
                {
                    chapter.fixedCollectiblesFoundList = new List<FixedCollectibleFound>(chapterFromSave.fixedCollectiblesFoundList);
                }
                else
                {
                    chapter.fixedCollectiblesFoundList = new List<FixedCollectibleFound>();
                }
            }
        }
    }

    #endregion

    /// <summary>
    /// Lock all chapters that were unlocked but are not part of the demo.
    /// Also make sure the selection of chapters only show 3 chapters at most.
    /// </summary>
    public void ApplyDemoLimitationToChapters()
    {
        SetChapterCountInSelection(3);
        foreach (Chapter chapter in chaptersData.chaptersList)
        {
            if (chapter.unlocked && !chapter.chapterData.partOfDemo)
            {
                chapter.unlocked = false;
            }
        }
    }
}
