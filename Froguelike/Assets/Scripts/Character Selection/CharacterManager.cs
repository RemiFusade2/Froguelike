using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;


/// <summary>
/// PlayableCharacter describes a character in its current state.
/// It has a reference to CharacterData, the scriptable object that describes the character. This is not serialized with the rest.
/// It keeps the characterName there for serialization. When saving/loading this character from a save file, the name will be used to retrieve the right character in the program.
/// The information that can change at runtime are:
/// - characterStartingStats, is the stats of this character, they can increase through playing and are saved in the save file
/// - unlocked, is the status of the character. Is it possible to play with? This value can change when the character is unlocked through an achievement.
/// - wonWith is the number of times a game was won with this character
/// </summary>
[System.Serializable]
public class PlayableCharacter
{
    [System.NonSerialized]
    public CharacterData characterData;

    // Defined at runtime, using CharacterData
    public string characterName;

    // All information about current state of the character
    public StatsWrapper characterStartingStats;
    public bool unlocked;
    public bool hidden;
    public int wonWith;

    public bool GetValueForStat(STAT stat, out float value)
    {
        value = 0;
        StatValue statValue = characterStartingStats.GetStatValue(stat);
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
/// - shopItems is the list of items in their current state
/// - currencySpentInShop is the amount of money that has been spent in the Shop (can be refunded when Shop is reset)
/// </summary>
[System.Serializable]
public class CharactersSaveData : SaveData
{
    public List<PlayableCharacter> charactersList;

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

    [Header("Characters scriptable objects data")]
    public List<CharacterData> charactersScriptableObjectsList;

    [Header("UI References")]
    public Button startButton;
    [Space]
    public RectTransform characterListContent;
    public RectTransform characterListContainerParent;
    public ScrollRect characterListScrollRect;

    [Header("UI Prefab")]
    public GameObject characterPanelPrefab;

    [Header("Runtime")]
    public CharactersSaveData charactersData; // Will be loaded and saved when needed
    public PlayableCharacter currentSelectedCharacter;

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

    #region UI
    
    /// <summary>
    /// Create buttons for each character and set the appropriate size for the scroll list
    /// </summary>
    public void UpdateCharacterSelectionScreen()
    {
        currentSelectedCharacter = null;

        // Remove previous buttons
        foreach (Transform child in characterListContainerParent)
        {
            Destroy(child.gameObject);
        }

        // Instantiate a button for each character (unless this character is hidden)
        int buttonCount = 0;
        for (int i = 0; i < charactersData.charactersList.Count; i++)
        {
            PlayableCharacter characterInfo = charactersData.charactersList[i];
            if (!characterInfo.hidden)
            {
                GameObject newCharacterPanel = Instantiate(characterPanelPrefab, characterListContainerParent);
                newCharacterPanel.GetComponent<CharacterSelectionButton>().Initialize(characterInfo);
                buttonCount++;
            }
        }

        // Set size of container panel
        GridLayoutGroup characterListContainerParentGridLayout = characterListContainerParent.GetComponent<GridLayoutGroup>();
        float buttonHeight = characterListContainerParentGridLayout.cellSize.y + characterListContainerParentGridLayout.spacing.y;
        float padding = characterListContainerParentGridLayout.padding.top + characterListContainerParentGridLayout.padding.bottom;
        characterListContent.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, buttonCount * buttonHeight + padding);
        
        // Select first character by default
        // SelectCharacter(charactersData.charactersList[0]);

        // Scroll to the top of the list
        characterListScrollRect.normalizedPosition = new Vector2(0, 1);

        // Display the Start button if possible
        startButton.interactable = (currentSelectedCharacter != null);
    }

    #endregion

    /// <summary>
    /// Display stats and Start Run button
    /// </summary>
    /// <param name="selectedCharacter"></param>
    public void SelectCharacter(PlayableCharacter selectedCharacter)
    {
        // Unselect all characters and select the one that was clicked
        foreach (Transform childCharacterButton in characterListContainerParent)
        {
            CharacterSelectionButton characterButton = childCharacterButton.GetComponent<CharacterSelectionButton>();
            if (characterButton != null)
            {
                characterButton.SetSelected(characterButton.character.Equals(selectedCharacter));
            }
        }

        // Display stats of this character and Start Run button
        currentSelectedCharacter = selectedCharacter;

        // These are two handy methods to get the stat bonuses from the Shop.
        /*
        List<StatValue> statBonusesFromShop = ShopManager.instance.statsBonuses;
        ShopManager.instance.GetStatBonus(STAT.MAX_HEALTH);
        */

        // Display the Start button if possible
        startButton.interactable = (currentSelectedCharacter != null);
    }

    public void StartRun()
    {
        GameManager.instance.StartRunWithCharacter(currentSelectedCharacter);
    }

    /// <summary>
    /// Update the characters data using a CharactersSaveData object, that was probably loaded from a file by the SaveDataManager.
    /// </summary>
    /// <param name="saveData"></param>
    public void SetCharactersData(CharactersSaveData saveData)
    {
        foreach (PlayableCharacter character in charactersData.charactersList)
        {
            PlayableCharacter characterFromSave = saveData.charactersList.First(x => x.characterName.Equals(character.characterName));
            if (characterFromSave != null)
            {
                character.unlocked = characterFromSave.unlocked;
                character.hidden = characterFromSave.hidden;
                character.wonWith = characterFromSave.wonWith;
                character.characterStartingStats = characterFromSave.characterStartingStats;
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
            foreach (CharacterData characterData in charactersScriptableObjectsList)
            {
                PlayableCharacter newCharacter = new PlayableCharacter() { characterData = characterData, characterName = characterData.characterName, unlocked = characterData.startingUnlockState, hidden = characterData.startingHiddenState, wonWith = 0 };
                newCharacter.characterStartingStats = new StatsWrapper(characterData.startingStatsList);
                charactersData.charactersList.Add(newCharacter);
            }
        }
        else
        {
            // A soft reset will not lock characters that were unlocked, but it will reset their starting stats to start game values
            foreach (PlayableCharacter character in charactersData.charactersList)
            {
                character.characterStartingStats = new StatsWrapper(character.characterData.startingStatsList);
            }
        }
        
        SaveDataManager.instance.isSaveDataDirty = true;
    }

    /// <summary>
    /// Try to unlock the character wearing this name. Do not do anything if this character does not exist or is already unlocked.
    /// Return true if a new character has been unlocked.
    /// </summary>
    /// <param name="characterName"></param>
    /// <returns></returns>
    public bool UnlockCharacter(string characterName)
    {
        bool characterNewlyUnlocked = false;
        PlayableCharacter unlockedCharacter = charactersData.charactersList.FirstOrDefault(x => x.characterName.Equals(characterName));
        if (unlockedCharacter != null && !unlockedCharacter.unlocked)
        {
            unlockedCharacter.unlocked = true;
            unlockedCharacter.hidden = false;
            characterNewlyUnlocked = true;
            SaveDataManager.instance.isSaveDataDirty = true;
        }
        return characterNewlyUnlocked;
    }

    /// <summary>
    /// Increase the amount of victories with the given character.
    /// </summary>
    /// <param name="character"></param>
    public void WonTheGameWithCharacter(PlayableCharacter character)
    {
        PlayableCharacter characterInCurrentData = charactersData.charactersList.FirstOrDefault(x => x.characterName.Equals(character.characterName));
        characterInCurrentData.wonWith++;
        SaveDataManager.instance.isSaveDataDirty = true;
    }
}