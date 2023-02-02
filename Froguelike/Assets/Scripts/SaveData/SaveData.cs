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
public class CharactersSaveData : SaveData
{
    public List<int> unlockedCharacterIndexList;
    public List<int> wonTheGameWithCharacterIndexList;

    public List<StatsWrapper> startingStatsForCharactersList;

    public CharactersSaveData()
    {
        Reset();
    }

    public override void Reset()
    {
        base.Reset();
        Reset(0);
    }

    public void Reset(int characterCount)
    {
        base.Reset();

        unlockedCharacterIndexList = new List<int>();
        wonTheGameWithCharacterIndexList = new List<int>();
        startingStatsForCharactersList = new List<StatsWrapper>();
        for (int i = 0; i < characterCount; i++)
        {
            wonTheGameWithCharacterIndexList.Add(0);
            startingStatsForCharactersList.Add(new StatsWrapper());
        }
    }
}

[System.Serializable]
public class CombinedSaveData
{
    public GameSaveData gameSaveData;

    public ShopSaveData shopSaveData;
    //public CharactersSaveData charactersSaveData;

    public CombinedSaveData()
    {
        gameSaveData = new GameSaveData();
        shopSaveData = new ShopSaveData();
        //charactersSaveData = new CharactersSaveData();
    }

    public static CombinedSaveData GetAllSaveData()
    {
        CombinedSaveData saveData = new CombinedSaveData();
        saveData.gameSaveData = GameManager.instance.gameData;
        saveData.shopSaveData = ShopManager.instance.shopData;
        //saveData.charactersSaveData = CharacterManager.instance.characterData;

        return saveData;
    }

    public static void SetAllSaveData(CombinedSaveData saveData)
    {
        GameManager.instance.SetGameData(saveData.gameSaveData);
        ShopManager.instance.SetShopData(saveData.shopSaveData);
        //CharacterManager.instance.SetCharacterData(saveData.charactersSaveData);
    }

    public override string ToString()
    {
        string result = "gameSaveData = " + gameSaveData.ToString() + "\n";
        result += "shopSaveData = " + shopSaveData.ToString() + "\n";
        //result += "charactersSaveData = " + charactersSaveData.ToString();
        return result;
    }
}