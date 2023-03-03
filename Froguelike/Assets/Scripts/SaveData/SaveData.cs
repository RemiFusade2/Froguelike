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

    public CombinedSaveData()
    {
        gameSaveData = new GameSaveData();
        shopSaveData = new ShopSaveData();
        charactersSaveData = new CharactersSaveData();
        enemiesSaveData = new EnemiesSaveData();
        chaptersSaveData = new ChaptersSaveData();
    }

    public static CombinedSaveData GetAllSaveData()
    {
        CombinedSaveData saveData = new CombinedSaveData();
        saveData.gameSaveData = GameManager.instance.gameData;
        saveData.shopSaveData = ShopManager.instance.shopData;
        saveData.charactersSaveData = CharacterManager.instance.charactersData;
        saveData.enemiesSaveData = EnemiesManager.instance.enemiesData;
        saveData.chaptersSaveData = ChapterManager.instance.chaptersData;

        return saveData;
    }

    public static void SetAllSaveData(CombinedSaveData saveData)
    {
        GameManager.instance.SetGameData(saveData.gameSaveData);
        ShopManager.instance.SetShopData(saveData.shopSaveData);
        CharacterManager.instance.SetCharactersData(saveData.charactersSaveData);
        EnemiesManager.instance.SetEnemiesData(saveData.enemiesSaveData);
        ChapterManager.instance.SetChaptersData(saveData.chaptersSaveData);
    }
}