using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Froguelike_ItemInfo
{
    public Froguelike_ItemScriptableObject item;
    public int level;
}

public class Froguelike_GameManager : MonoBehaviour
{
    public static Froguelike_GameManager instance;

    public Froguelike_CharacterController player;

    public Transform fliesParent;

    public Froguelike_Wave waveInfo;

    public float xp;
    public float nextLevelXp = 5;
    public float xpNeededForNextLevelFactor = 1.3f;
    public int level;

    public int respawns;
    public int currentRespawns;

    public List<Froguelike_ItemScriptableObject> availableItems;

    public List<Froguelike_ItemInfo> ownedItems;

    public bool hasGameStarted;
    public bool isGameRunning;

    private void Awake()
    {
        instance = this;
        hasGameStarted = false;
        isGameRunning = false;
    }

    // Start is called before the first frame update
    void Start()
    {
        level = 1;
        xp = 0;
        Froguelike_UIManager.instance.UpdateXPSlider(0, nextLevelXp);
    }

    public void EatFly(int experiencePoints)
    {
        xp += experiencePoints;

        while (xp >= nextLevelXp)
        {
            LevelUP();
            xp -= nextLevelXp;
            nextLevelXp *= xpNeededForNextLevelFactor;
        }

        Froguelike_UIManager.instance.UpdateXPSlider(xp, nextLevelXp);
    }

    #region Level Up

    public List<Froguelike_ItemScriptableObject> levelUpPossibleItems;

    public void ChooseLevelUpChoice(int index)
    {
        Froguelike_ItemScriptableObject pickedItem = levelUpPossibleItems[index];

        bool itemIsNew = true;

        foreach (Froguelike_ItemInfo itemInfo in ownedItems)
        {
            if (itemInfo.Equals(pickedItem))
            {
                itemIsNew = false;
                itemInfo.level++;
            }
        }

        if (itemIsNew)
        {
            Froguelike_ItemInfo newItem = new Froguelike_ItemInfo();
            newItem.level = 1;
            newItem.item = pickedItem;
            ownedItems.Add(newItem);
        }

        Froguelike_UIManager.instance.HideLevelUpItemSelection();
        Time.timeScale = 1;
    }

    public void LevelUP()
    {
        level++;
        Time.timeScale = 0;

        // Pick possible items from a pool
        List<Froguelike_ItemScriptableObject> possibleItems = new List<Froguelike_ItemScriptableObject>(availableItems);
        levelUpPossibleItems.Clear();
        for (int i = 0; i < 3; i++)
        {
            if (possibleItems.Count == 0)
            {
                break;
            }
            int randomIndex = Random.Range(0, possibleItems.Count);
            levelUpPossibleItems.Add(possibleItems[randomIndex]);
            possibleItems.RemoveAt(randomIndex);
        }
        
        // Find levels for each of these items

        List<int> itemLevels = new List<int>();
        itemLevels.Add(1);
        itemLevels.Add(2);
        itemLevels.Add(3);

        // Show Update level UI
        Froguelike_UIManager.instance.ShowLevelUpItemSelection(levelUpPossibleItems, itemLevels);
        Froguelike_UIManager.instance.UpdateLevel(level);
    }

    #endregion

    public void OpenCharacterSelection()
    {
        Froguelike_UIManager.instance.ShowCharacterSelection();
    }

    public void SelectCharacter(int index)
    {
        Froguelike_UIManager.instance.ShowChapterSelection(1, "THE SWAMP", "BEES");
    }

    public void SelectChapter(int index)
    {
        StartCoroutine(StartGame(1, "THE SWAMP"));
    }

    public IEnumerator StartGame(int chapterCount, string chapterTitle)
    {
        Froguelike_UIManager.instance.ShowChapterStart(chapterCount, chapterTitle);
        yield return new WaitForSecondsRealtime(3.0f);
        Froguelike_UIManager.instance.ShowGameUI();
        hasGameStarted = true;
        isGameRunning = true;
        Froguelike_FliesManager.instance.SetWave(waveInfo);
        Time.timeScale = 1;
        player.Respawn();
        currentRespawns = respawns;
    }

    public void TriggerGameOver()
    {
        Time.timeScale = 0;
        isGameRunning = false;
        Froguelike_UIManager.instance.ShowGameOver((currentRespawns > 0));
    }

    public void Respawn()
    {
        Time.timeScale = 1;
        player.Respawn();
        isGameRunning = true;
        Froguelike_UIManager.instance.ShowGameUI();
        currentRespawns--;
    }

    public void ShowScores()
    {
        Froguelike_UIManager.instance.ShowScoreScreen();
    }

    public void BackToTitleScreen()
    {
        Froguelike_FliesManager.instance.ClearAllEnemies();
        Froguelike_UIManager.instance.ShowTitleScreen();
        hasGameStarted = false;
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
