using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;


[System.Serializable]
public abstract class SaveData
{
    public virtual void Reset()
    {
    }
}

[System.Serializable]
public class CombinedSaveData
{
    public GameSaveData gameSaveData;

    public ShopSaveData shopSaveData;
    public CharactersSaveData charactersSaveData;
    public EnemiesSaveData enemiesSaveData;
    public ChaptersSaveData chaptersSaveData;
    public RunItemsSaveData runItemsSaveData;
    public AchievementsSaveData achievementsSaveData;

    public CombinedSaveData()
    {
        gameSaveData = new GameSaveData();
        shopSaveData = new ShopSaveData();
        charactersSaveData = new CharactersSaveData();
        enemiesSaveData = new EnemiesSaveData();
        chaptersSaveData = new ChaptersSaveData();
        runItemsSaveData = new RunItemsSaveData();
        achievementsSaveData = new AchievementsSaveData();
    }

    public static CombinedSaveData GetAllSaveData()
    {
        CombinedSaveData saveData = new CombinedSaveData();
        saveData.gameSaveData = GameManager.instance.gameData;
        saveData.shopSaveData = ShopManager.instance.shopData;
        saveData.charactersSaveData = CharacterManager.instance.charactersData;
        saveData.enemiesSaveData = EnemiesManager.instance.enemiesData;
        saveData.chaptersSaveData = ChapterManager.instance.chaptersData;
        saveData.runItemsSaveData = RunItemManager.instance.runItemsData;
        saveData.achievementsSaveData = AchievementManager.instance.achievementsData;

        return saveData;
    }

    public static void SetAllSaveData(CombinedSaveData saveData)
    {
        if (saveData.gameSaveData != null)
        {
            GameManager.instance.SetGameData(saveData.gameSaveData);
        }
        if (saveData.shopSaveData != null)
        {
            ShopManager.instance.SetShopData(saveData.shopSaveData);
        }
        if (saveData.charactersSaveData != null)
        {
            CharacterManager.instance.SetCharactersData(saveData.charactersSaveData);
        }
        if (saveData.enemiesSaveData != null)
        {
            EnemiesManager.instance.SetEnemiesData(saveData.enemiesSaveData);
        }
        if (saveData.chaptersSaveData != null)
        {
            ChapterManager.instance.SetChaptersData(saveData.chaptersSaveData);
        }
        if (saveData.runItemsSaveData != null)
        {
            RunItemManager.instance.SetRunItemsData(saveData.runItemsSaveData);
        }
        if (saveData.achievementsSaveData != null)
        {
            AchievementManager.instance.SetAchievementsData(saveData.achievementsSaveData);
        }
    }
}