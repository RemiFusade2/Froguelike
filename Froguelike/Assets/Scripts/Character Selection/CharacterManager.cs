﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

/// <summary>
/// PlayableCharacter describes a character in its current state.
/// It has a reference to CharacterData, the scriptable object that describes the character. This is not serialized with the rest.
/// It keeps the characterID there for serialization. When saving/loading this character from a save file, the name will be used to retrieve the right character in the program.
/// The information that can change at runtime are:
/// - characterStartingStats, is the stats of this character, they can increase through playing and are saved in the save file
/// - unlocked, is the status of the character. Is it possible to play with? This value can change when the character is unlocked through an achievement.
/// - runStartedWith is the number of times a game was started with this character
/// - wonWith is the number of times a game was won with this character
/// </summary>
[System.Serializable]
public class PlayableCharacter
{
    [System.NonSerialized]
    public CharacterData characterData;

    // Defined at runtime, using CharacterData
    public string characterID;

    // All information about current state of the character
    public StatsWrapper characterStatsIncrements;
    public bool unlocked;
    public bool hidden;
    public int runStartedWith;
    public int wonWith;

    public bool storyCompleted;

    public StatsWrapper GetCharacterStartingStats()
    {
        StatsWrapper result = new StatsWrapper(characterData.startingStatsList);
        if (storyCompleted)
        {
            result = StatsWrapper.JoinLists(characterData.startingStatsList, characterData.startingStatsStoryUpgrade);
        }
        if (characterStatsIncrements != null && characterStatsIncrements.statsList != null && characterStatsIncrements.statsList.Count > 0)
        {
            result = StatsWrapper.JoinLists(result.statsList, characterStatsIncrements.statsList);
        }
        return result;
    }

    public bool GetValueForStat(CharacterStat stat, out float value)
    {
        value = 0;
        StatValue statValue = GetCharacterStartingStats().GetStatValue(stat);
        bool statExists = (statValue != null);
        if (statExists)
        {
            value = (float)statValue.value;
        }
        return statExists;
    }
}

/// <summary>
/// CharactersSaveData contains all information that must be saved about the characters.
/// - charactersList is the list of characters in their current state
/// </summary>
[System.Serializable]
public class CharactersSaveData : SaveData
{
    public List<PlayableCharacter> charactersList;

    public GameMode availableGameModes;

    public CharactersSaveData()
    {
        Reset();
    }

    public override void Reset()
    {
        base.Reset();
        charactersList = new List<PlayableCharacter>();
    }
}

/// <summary>
/// CharacterManager is a singleton class that deals with the playable characters.
/// - Keep information about all characters (was it unlocked? what are its starting stats?)
/// - Display a character selection screen and offers to select a character, and display its stats
/// </summary>
public class CharacterManager : MonoBehaviour
{
    // Singleton
    public static CharacterManager instance;

    [Header("Settings")]
    public VerboseLevel logsVerboseLevel = VerboseLevel.NONE;
    [Space]
    public bool showLockedCharacters;

    [Header("Characters scriptable objects data")]
    public List<CharacterData> charactersScriptableObjectsList;

    [Header("UI References")]
    public Button startButton;
    public Button backButton;
    [Space]
    public RectTransform characterListContent;
    public RectTransform characterListGridLayoutGroup;
    public ScrollRect characterListScrollRect;
    public ScrollbarKeepCursorSizeBehaviour characterListScrollbar;
    [Space]
    public TextMeshProUGUI characterName;
    public RectTransform statsListContent;
    public RectTransform statsListGridLayoutGroup;
    public ScrollRect statsListScrollRect;
    public ScrollbarKeepCursorSizeBehaviour statListScrollbar;
    public GameObject hideShopStats;
    [Space]
    public GameObject difficultyPanel1;
    public TextMeshProUGUI difficultyPanel1InfoText;
    public GameObject difficultyPanel2;
    public TextMeshProUGUI difficultyPanel2InfoText;

    [Header("UI Prefab")]
    public GameObject characterPanelPrefab;
    public GameObject statLinePrefab;

    [Header("Runtime")]
    public CharactersSaveData charactersData; // Will be loaded and saved when needed
    public PlayableCharacter currentSelectedCharacter;
    [Space]
    public GameMode selectedGameModes;

    private Dictionary<string, CharacterData> charactersDataFromNameDico;

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

    private void Start()
    {
        charactersDataFromNameDico = new Dictionary<string, CharacterData>();
        foreach (CharacterData character in charactersScriptableObjectsList)
        {
            charactersDataFromNameDico.Add(character.characterID, character);
        }
    }

    public int GetUnlockedCharacterCount()
    {
        return charactersData.charactersList.Count(x => x.unlocked);
    }


    public PlayableCharacter GetPlayableCharacter(string characterID)
    {
        PlayableCharacter result = charactersData.charactersList.FirstOrDefault(x => x.characterID == characterID);
        return result;
    }

    public CharacterData GetCharacterData(string characterID)
    {
        CharacterData result = null;
        if (charactersDataFromNameDico.ContainsKey(characterID))
        {
            result = charactersDataFromNameDico[characterID];
        }
        return result;
    }

    #region UI

    /// <summary>
    /// Create buttons for each character, set the appropriate size for the scroll list and display the stats from the selected character and the shop.
    /// </summary>
    public bool UpdateCharacterSelectionScreen()
    {
        bool isCharacterSelectionScreen = true;
        currentSelectedCharacter = null;

        // Remove previous buttons
        foreach (Transform child in characterListGridLayoutGroup)
        {
            child.gameObject.SetActive(false);
            Destroy(child.gameObject);
        }

        // Instantiate a button for each character (unless this character is hidden)
        int buttonCount = 0;
        List<Button> characterPanels = new List<Button>(); // For setting UI navigation a little later.
        string characterLog = "";
        PlayableCharacter defaultCharacter = null;
        List<PlayableCharacter> orderedList = charactersData.charactersList.Where(x => (x.characterData.partOfDemo || !BuildManager.instance.demoBuild)).OrderBy(x => !x.unlocked).ToList();
        for (int i = 0; i < orderedList.Count; i++)
        {
            PlayableCharacter characterInfo = orderedList[i];
            if (showLockedCharacters || IsCharacterUnlocked(characterInfo.characterID))
            {
                GameObject newCharacterPanel = Instantiate(characterPanelPrefab, characterListGridLayoutGroup);
                newCharacterPanel.GetComponent<CharacterSelectionButton>().Initialize(characterInfo);
                newCharacterPanel.name = characterInfo.characterData.characterName;
                characterPanels.Add(newCharacterPanel.GetComponent<CharacterSelectionButton>().characterButton);

                if (buttonCount == 0)
                {
                    defaultCharacter = characterInfo;
                }

                buttonCount++;
                characterLog += $" {characterInfo.characterID} is " + (characterInfo.unlocked ? "unlocked" : "locked") + " ;";
            }
        }

        #region Button navigation, might not need, might use automatic instead
        /*
        // Set button navigations.
        for (int characterPanel = 0; characterPanel < characterPanels.Count; characterPanel++)
        {
            // The current panel.
            Button thisCharacterPanel = characterPanels[characterPanel];
            Navigation thisCharacterButtonNav = thisCharacterPanel.navigation;

            int up;
            int down;
            // Up  from character panel.
            // First panel.
            if (characterPanel == 0)
            {
                // Start and back navigates to the first character panel on down.
                Navigation startButtonNav = startButton.navigation;
                Navigation backButtonNav = backButton.navigation;
                startButtonNav.selectOnDown = thisCharacterPanel.GetComponentInChildren<Button>();
                backButtonNav.selectOnDown = thisCharacterPanel.GetComponentInChildren<Button>();
                startButton.navigation = startButtonNav;
                backButton.navigation = backButtonNav;

                // First character panel navigates to back button.
                thisCharacterButtonNav.selectOnUp = backButton;
            }
            // Rest panels.
            else
            {
                up = characterPanel - 1;
                thisCharacterButtonNav.selectOnUp = characterPanels[up];
            }

            // Down from character panel.
            // Last panel.
            if (characterPanel == characterPanels.Count - 1)
            {
                // Start and back navigates to the lasr character panel on up.
                Navigation startButtonNav = startButton.navigation;
                Navigation backButtonNav = backButton.navigation;
                startButtonNav.selectOnUp = thisCharacterPanel.GetComponentInChildren<Button>();
                backButtonNav.selectOnUp = thisCharacterPanel.GetComponentInChildren<Button>();
                startButton.navigation = startButtonNav;
                backButton.navigation = backButtonNav;

                // Last character panel navigates to back button.
                thisCharacterButtonNav.selectOnDown = backButton;
            }
            // Rest panels.
            else
            {
                down = characterPanel + 1;
                thisCharacterButtonNav.selectOnDown = characterPanels[down];
            }

            // Left and right from character panel.
            thisCharacterButtonNav.selectOnLeft = backButton;
            thisCharacterButtonNav.selectOnRight = startButton;

            // TODO set stat scroll to navigate to selected character on left
            // same for character scroll on right
            // instead of looping make navigation from characters to back button on up and to steam page or start (?) on down?
            // Figure out how scroll will work

            // start button going left and back button going right to selected character?

            thisCharacterPanel.navigation = thisCharacterButtonNav;
        }
        */ //TODO ^
        #endregion Button navigation

        characterLog = "Display " + buttonCount + " buttons\n" + characterLog;
        if (logsVerboseLevel == VerboseLevel.MAXIMAL)
        {
            Debug.Log("Character selection - " + characterLog);
        }

        // Set size of container panel
        GridLayoutGroup characterListGridLayoutGroupComponent = characterListGridLayoutGroup.GetComponent<GridLayoutGroup>();
        float buttonHeight = characterListGridLayoutGroupComponent.cellSize.y + characterListGridLayoutGroupComponent.spacing.y;
        float padding = characterListGridLayoutGroupComponent.padding.top + characterListGridLayoutGroupComponent.padding.bottom;
        characterListContent.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, buttonCount * buttonHeight + padding);

        // Select first character by default
        SelectCharacter(defaultCharacter);

        // Set the cursor to the top.
        characterListScrollRect.verticalScrollbar.value = 0;

        statsListScrollRect.verticalScrollbar.value = 0; // Cursor up top on the stat list

        // Scroll to the top of the lists
        characterListScrollRect.normalizedPosition = new Vector2(0, 1);
        statsListScrollRect.normalizedPosition = new Vector2(0, 1);

        UpdateStatsList();

        // Display the right difficulty panel if unlocked.
        if (IsGameModeUnlocked(GameMode.HARD))
        {
            GameObject difficultyPanel = difficultyPanel1;
            difficultyPanel1.SetActive(true);
            difficultyPanel2.SetActive(false);

            if (IsGameModeUnlocked(GameMode.HARDER))
            {
                difficultyPanel = difficultyPanel2;
                difficultyPanel2.SetActive(true);
                difficultyPanel1.SetActive(false);
            }

            ResetDifficulty();
            difficultyPanel.GetComponent<DifficultyPanelBehaviour>().SetToggleCheckmarks(GetSelectedGameModes());
            DisplayDifficultyInfo(GetSelectedGameModes());
        }

        if (buttonCount == 1 && defaultCharacter != null)
        {
            currentSelectedCharacter = defaultCharacter;
            isCharacterSelectionScreen = false;
        }

        return isCharacterSelectionScreen;
    }

    private void InstantiateStatLines()
    {
        // Instantiate a button for each character (unless this character is hidden)
        int buttonCount = 0;
        for (int i = 0; i < charactersData.charactersList.Count; i++)
        {
            PlayableCharacter characterInfo = charactersData.charactersList[i];

            GameObject newCharacterPanel = Instantiate(characterPanelPrefab, characterListGridLayoutGroup);
            newCharacterPanel.GetComponent<CharacterSelectionButton>().Initialize(characterInfo);
            buttonCount++;
        }
    }

    #endregion

    /// <summary>
    /// Display character stats, total stats and Start Run button
    /// </summary>
    /// <param name="selectedCharacter"></param>
    public void SelectCharacter(PlayableCharacter selectedCharacter)
    {
        // Unselect all characters and select the one that was clicked
        foreach (Transform childCharacterButton in characterListGridLayoutGroup)
        {
            CharacterSelectionButton characterButton = childCharacterButton.GetComponent<CharacterSelectionButton>();
            if (characterButton != null)
            {
                characterButton.SetSelected(characterButton.character.Equals(selectedCharacter));

                if (characterButton.character.Equals(selectedCharacter))
                {
                    // Set start button and back button navigation.
                    Navigation startButtonNav = startButton.navigation;
                    Navigation backButtonNav = backButton.navigation;
                    startButtonNav.selectOnLeft = characterButton.characterButton;
                    backButtonNav.selectOnRight = characterButton.characterButton;
                    startButton.navigation = startButtonNav;
                    backButton.navigation = backButtonNav;
                }
            }
        }

        // Display stats of this character and Start Run button
        currentSelectedCharacter = selectedCharacter;

        // Update stats
        UpdateStatsList();
    }

    private void UpdateStatsList()
    {
        // Update the name on top of the stat view.
        characterName.SetText(currentSelectedCharacter.characterData.characterName);

        // Get all values for stats.
        List<StatValue> statBonusesFromShop = ShopManager.instance.statsBonuses;
        List<StatValue> currentCharacterStatList = currentSelectedCharacter.GetCharacterStartingStats().statsList;
        List<StatValue> totalStatList = StatsWrapper.JoinLists(currentCharacterStatList, statBonusesFromShop).statsList;
 
        if (logsVerboseLevel == VerboseLevel.MAXIMAL)
        {
            string log = "Character selection - Select " + currentSelectedCharacter.characterID + "\n";
            log += "-> Display Character stats: " + StatsWrapper.StatsListToString(currentCharacterStatList) + "\n";
            log += "-> Display Shop stats: " + StatsWrapper.StatsListToString(statBonusesFromShop);
            Debug.Log(log);
        }

        StatValue[] statValues = new StatValue[Enum.GetValues(typeof(CharacterStat)).Length];
        foreach (Transform child in statsListGridLayoutGroup)
        {
            Destroy(child.gameObject);
        }

        int lineCount = 0;
        for (int i = 0; i < statValues.Length; i++)
        {
            CharacterStat stat = (CharacterStat)i;

            if (stat != CharacterStat.ATK_SPECIAL_DURATION_BOOST && stat != CharacterStat.ATK_SPECIAL_STRENGTH_BOOST && stat != CharacterStat.ATK_DURATION_BOOST) // we don't show these stats anymore
            {
                GameObject statLineGo = Instantiate(statLinePrefab, statsListGridLayoutGroup);
                CharacterStatLine statLineScript = statLineGo.GetComponent<CharacterStatLine>();

                // Show the icon.
                statLineGo.transform.Find("Icon").GetComponent<Image>().sprite = DataManager.instance.GetStatSprite(stat);

                StatValue statValue = null;
                float totalValue = 0;
                float frogValue = 0;
                float shopValue = 0;

                if (currentSelectedCharacter.unlocked)
                {
                    statValue = totalStatList.FirstOrDefault(x => x.stat == stat);

                    if (stat != CharacterStat.WALK_SPEED_BOOST && stat != CharacterStat.SWIM_SPEED_BOOST && stat != CharacterStat.MAGNET_RANGE_BOOST)
                    {
                        totalValue = DataManager.instance.GetDefaultValueForStat(stat); // we show these stats with percentage instead of value
                    }

                    if (statValue != null)
                    {
                        totalValue += (float)statValue.value;
                    }
                }
                else
                {
                    statValue = statBonusesFromShop.FirstOrDefault(x => x.stat == stat);

                    if (stat != CharacterStat.WALK_SPEED_BOOST && stat != CharacterStat.SWIM_SPEED_BOOST && stat != CharacterStat.MAGNET_RANGE_BOOST)
                    {
                        totalValue = DataManager.instance.GetDefaultValueForStat(stat); // we show these stats with percentage instead of value
                    }

                    if (statValue != null)
                    {
                        totalValue += (float)statValue.value;
                    }
                }

                statValue = currentCharacterStatList.FirstOrDefault(x => x.stat == stat);
                if (statValue != null)
                {
                    frogValue += (float)statValue.value;
                }

                statValue = statBonusesFromShop.FirstOrDefault(x => x.stat == stat);
                if (statValue != null)
                {
                    shopValue += (float)statValue.value;
                }

                statLineScript.Initialize(stat, totalValue, frogValue, shopValue, currentSelectedCharacter.unlocked);

                lineCount++;
            }
        }

        // Set size of container panel
        GridLayoutGroup statsListGridLayoutGroupComponent = statsListGridLayoutGroup.GetComponent<GridLayoutGroup>();
        float buttonHeight = statsListGridLayoutGroupComponent.cellSize.y + statsListGridLayoutGroupComponent.spacing.y;
        float padding = statsListGridLayoutGroupComponent.padding.top + statsListGridLayoutGroupComponent.padding.bottom;
        statsListContent.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, lineCount * buttonHeight + padding);

        // Hide shop if it isn't unlocked yet.
        hideShopStats.SetActive(!ShopManager.instance.IsShopUnlocked());

        // Make start button not interactable if a locked character is picked.
        startButton.interactable = currentSelectedCharacter.unlocked;
        startButton.GetComponent<CanvasGroup>().blocksRaycasts = currentSelectedCharacter.unlocked;
    }


    /// <summary>
    /// Takes a list of stat values and returns one string with the value or a "-" for all stats and formats the values properly.
    /// </summary>
    /// <param name="statList"></param>
    /// <param name="isListForTotals"></param>
    /// <returns></returns>
    private string MakeStringFromStats(List<StatValue> statList, bool isListForTotals)
    {
        string valueText = "";

        // An array for all stats where all the available values from the stat list is stored.
        StatValue[] statValues = new StatValue[Enum.GetValues(typeof(CharacterStat)).Length];
        for (int i = 0; i < statList.Count; i++)
        {
            statValues[(int)statList[i].stat] = statList[i];
        }

        // Make the string from the available values.
        for (int i = 0; i < statValues.Length; i++)
        {
            CharacterStat thisStat = (CharacterStat)i;

            // Checks if there is a value to add to the string.
            if (statValues[i] == null)
            {
                // Some should be 
                switch (thisStat)
                {
                    case CharacterStat.REVIVAL:
                    case CharacterStat.REROLL:
                    case CharacterStat.BANISH:
                    case CharacterStat.SKIP:
                        if (isListForTotals)
                        {
                            valueText += "0";
                        }
                        else
                        {
                            valueText += "-";
                        }
                        break;
                    default:
                        valueText += "-";
                        break;
                }
            }
            else
            {
                double thisValue = statValues[i].value;

                // Some values need a unit only for the total list
                string unit = "";
                if (isListForTotals)
                {
                    switch (thisStat)
                    {
                        case CharacterStat.MAX_HEALTH: // 0
                            unit = "HP";
                            break;
                        case CharacterStat.HEALTH_RECOVERY: // 1
                            unit = "HP/s";
                            break;
                        case CharacterStat.ARMOR: // 2
                            unit = "HP";
                            break;
                        default:
                            break;
                    }
                }

                // Checks what kind of value that is going to be added to the string.
                switch (thisStat)
                {
                    // These values are a number.
                    case CharacterStat.MAX_HEALTH: // 0
                    case CharacterStat.HEALTH_RECOVERY: // 1
                    case CharacterStat.ARMOR: // 2
                    case CharacterStat.REVIVAL: // 8
                    case CharacterStat.REROLL: // 9
                    case CharacterStat.BANISH: // 10
                    case CharacterStat.SKIP: // 11
                        // Sign depending on negative or positive number. Only added when the list is not for the total value.
                        if (!isListForTotals && thisValue > 0)
                        {
                            valueText += "+";
                        }
                        valueText += thisValue.ToString() + unit;
                        break;

                    // These values are in percentage.
                    case CharacterStat.XP_BOOST: // 3
                    case CharacterStat.CURRENCY_BOOST: // 4
                    case CharacterStat.CURSE: // 5
                    case CharacterStat.WALK_SPEED_BOOST: // 6
                    case CharacterStat.SWIM_SPEED_BOOST: // 7
                    case CharacterStat.ATK_DAMAGE_BOOST: // 12
                    case CharacterStat.ATK_SPEED_BOOST: // 13
                    case CharacterStat.ATK_COOLDOWN_BOOST: // 14
                    case CharacterStat.ATK_RANGE_BOOST: // 15
                    case CharacterStat.ATK_SIZE_BOOST: // 16
                    case CharacterStat.ATK_SPECIAL_STRENGTH_BOOST: // 17
                    case CharacterStat.ATK_SPECIAL_DURATION_BOOST: // 18
                    case CharacterStat.MAGNET_RANGE_BOOST: // 19
                        if (thisValue > 0)
                        {
                            valueText += "+";
                        }

                        valueText += thisValue.ToString("P0").Replace(" ٪", "%"); // replace the shitty percentage symbol by a proper one and remove the space in front of it
                        //valueText += Mathf.FloorToInt((float)thisValue * 100).ToString() + "%";
                        break;

                    default:
                        break;
                }
            }

            valueText += "\n";
        }

        return valueText;
    }

    public void StartRun()
    {
        GameManager.instance.StartRunWithCharacter(currentSelectedCharacter, GetSelectedGameModes());
    }

    /// <summary>
    /// Update the characters data using a CharactersSaveData object, that was probably loaded from a file by the SaveDataManager.
    /// </summary>
    /// <param name="saveData"></param>
    public void SetCharactersData(CharactersSaveData saveData)
    {
        charactersData.availableGameModes = saveData.availableGameModes;
        foreach (PlayableCharacter character in charactersData.charactersList)
        {
            PlayableCharacter characterFromSave = saveData.charactersList.First(x => x.characterID.Equals(character.characterID));
            if (characterFromSave != null)
            {
                character.unlocked = characterFromSave.unlocked;
                character.hidden = characterFromSave.hidden;
                character.runStartedWith = (characterFromSave.wonWith > characterFromSave.runStartedWith) ? characterFromSave.wonWith : characterFromSave.runStartedWith;
                character.wonWith = characterFromSave.wonWith;
                character.characterStatsIncrements = characterFromSave.characterStatsIncrements;
            }
        }

        // Update the character selection display
        UpdateCharacterSelectionScreen();
    }

    /// <summary>
    /// Reset all characters. Hard reset means setting everything back to start game values (locking characters that were unlocked). 
    /// Soft reset means setting only the stats back to their original values.
    /// </summary>
    /// <param name="hardReset"></param>
    public void ResetCharacters(bool hardReset = false)
    {
        if (hardReset)
        {
            // A hard reset will reset everything to the start game values
            charactersData.charactersList.Clear();
            charactersData.availableGameModes = GameMode.NONE;
            foreach (CharacterData characterData in charactersScriptableObjectsList)
            {
                PlayableCharacter newCharacter = new PlayableCharacter() { characterData = characterData, characterID = characterData.characterID, unlocked = characterData.startingUnlockState, hidden = characterData.startingHiddenState, wonWith = 0, runStartedWith = 0 };
                newCharacter.characterStatsIncrements = new StatsWrapper();
                charactersData.charactersList.Add(newCharacter);
            }
        }
        else
        {
            // A soft reset will not lock characters that were unlocked, but it will reset their starting stats to start game values
            foreach (PlayableCharacter character in charactersData.charactersList)
            {
                character.characterStatsIncrements = new StatsWrapper();
            }
        }

        SaveDataManager.instance.isSaveDataDirty = true;
    }

    public bool IsGameModeUnlocked(GameMode gameMode)
    {
        return (charactersData.availableGameModes & gameMode) == gameMode;
    }

    public void UnlockGameMode(GameMode gameMode)
    {
        charactersData.availableGameModes |= gameMode;
        SaveDataManager.instance.isSaveDataDirty = true;
    }

    public GameMode GetSelectedGameModes()
    {
        return selectedGameModes;
    }

    public void SelectGameModeHard(Toggle thisToggle)
    {
        bool hardModeIsSelected = thisToggle.isOn;

        if (hardModeIsSelected)
        {
            selectedGameModes |= GameMode.HARD; // Add flag
        }
        else
        {
            selectedGameModes &= ~GameMode.HARD; // Remove flag
        }

        DisplayDifficultyInfo(GetSelectedGameModes());
    }

    public void SelectGameModeHarder(Toggle thisToggle)
    {
        bool harderModeIsSelected = thisToggle.isOn;

        if (harderModeIsSelected)
        {
            selectedGameModes |= GameMode.HARDER; // Add flag
        }
        else
        {
            selectedGameModes &= ~GameMode.HARDER; // Remove flag
        }

        DisplayDifficultyInfo(GetSelectedGameModes());
    }

    public void ResetDifficulty()
    {
        selectedGameModes = GameMode.NONE;
    }

    public void DisplayDifficultyInfo(GameMode gameMode)
    {
        string text = "";
        TextMeshProUGUI setThisText = IsGameModeUnlocked(GameMode.HARDER) ? difficultyPanel2InfoText : difficultyPanel1InfoText;
        if (gameMode == GameMode.HARD)
        {
            // 50%
            text = "+50% bug health\n+50% bug speed\n+50% bug strength\n+50% froins";
        }
        else if (gameMode == GameMode.HARDER)
        {
            // 100%
            text = "+100% bug health\n+100% bug speed\n+100% bug strength\n+100% froins";
        }
        else if (gameMode == (GameMode.HARD | GameMode.HARDER))
        {
            // 150%
            text = "+150% bug health\n+150% bug speed\n+150% bug strength\n+150% froins";
        }

        setThisText.SetText(text);
    }

    /// <summary>
    /// Try to unlock the character using its identifier. Do not do anything if this character does not exist or is already unlocked.
    /// Return true if a new character has been unlocked.
    /// </summary>
    /// <param name="characterID"></param>
    /// <returns></returns>
    public bool UnlockCharacter(string characterID)
    {
        bool characterNewlyUnlocked = false;
        PlayableCharacter unlockedCharacter = charactersData.charactersList.FirstOrDefault(x => x.characterID.Equals(characterID));
        if (unlockedCharacter != null && !unlockedCharacter.unlocked)
        {
            unlockedCharacter.unlocked = true;
            unlockedCharacter.hidden = false;
            characterNewlyUnlocked = true;
            SaveDataManager.instance.isSaveDataDirty = true;
        }
        return characterNewlyUnlocked;
    }

    public void SetCharacterStoryCompleted(string characterID)
    {
        PlayableCharacter character = charactersData.charactersList.FirstOrDefault(x => x.characterID.Equals(characterID));
        if (character != null && !character.storyCompleted)
        {
            character.storyCompleted = true;
            SaveDataManager.instance.isSaveDataDirty = true;
        }
    }

    public void IncrementCharacterStats(string characterID, List<StatValue> changedStatsValues)
    {
        PlayableCharacter character = charactersData.charactersList.FirstOrDefault(x => x.characterID.Equals(characterID));
        if (character != null)
        {
            character.characterStatsIncrements = StatsWrapper.JoinLists(character.characterStatsIncrements, changedStatsValues);
            SaveDataManager.instance.isSaveDataDirty = true;
        }
    }

    public bool IsCharacterUnlocked(string characterID)
    {
        bool result = false;
        PlayableCharacter unlockedCharacter = charactersData.charactersList.FirstOrDefault(x => x.characterID.Equals(characterID));
        if (unlockedCharacter != null && unlockedCharacter.unlocked && (!BuildManager.instance.demoBuild || unlockedCharacter.characterData.partOfDemo))
        {
            result = true;
        }
        return result;
    }

    /// <summary>
    /// Increase the amount of attempts with the given character
    /// </summary>
    /// <param name="character"></param>
    public void StartedARunWithCharacter(PlayableCharacter character)
    {
        PlayableCharacter characterInCurrentData = charactersData.charactersList.FirstOrDefault(x => x.characterID.Equals(character.characterID));
        characterInCurrentData.runStartedWith++;
        SaveDataManager.instance.isSaveDataDirty = true;
    }

    /// <summary>
    /// Increase the amount of victories with the given character.
    /// </summary>
    /// <param name="character"></param>
    public void WonTheGameWithCharacter(PlayableCharacter character)
    {
        PlayableCharacter characterInCurrentData = charactersData.charactersList.FirstOrDefault(x => x.characterID.Equals(character.characterID));
        characterInCurrentData.wonWith++;
        SaveDataManager.instance.isSaveDataDirty = true;
    }
}
