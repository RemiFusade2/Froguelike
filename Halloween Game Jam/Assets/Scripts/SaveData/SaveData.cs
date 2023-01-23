using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;


public class SaveData
{
    public List<int> unlockedCharacterIndexList;
    public List<int> wonTheGameWithCharacterIndexList;

    public List<StatsWrapper> startingStatsForCharactersList;

    public List<ShopItem> shopItems;
    public long currencySpentInShop;

    public int deathCount;
    public int bestScore;
    public int cumulatedScore;

    public int attempts;
    public int wins;

    public long availableCurrency;
    public long totalSpentCurrency;

    public void InitializeCharacterLists()
    {
        wonTheGameWithCharacterIndexList = new List<int>();
        startingStatsForCharactersList = new List<StatsWrapper>();
        for (int i = 0; i < 7; i++)
        {
            wonTheGameWithCharacterIndexList.Add(0);
            startingStatsForCharactersList.Add(new StatsWrapper());
        }

    }

    /// <summary>
    /// Delete save file permanently. Returns true if succeeded.
    /// </summary>
    /// <param name="saveFileName"></param>
    /// <returns></returns>
    public static bool EraseSaveFile(string saveFileName)
    {
        string saveFilePath = GetFilePath(saveFileName);
        Debug.Log("Debug info - Erasing file: " + saveFilePath);
        bool result = false;
        try
        {
            File.Delete(saveFilePath);
            result = true;
            Debug.Log("Debug info - Save file erased successfully");
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning("Exception in EraseSaveFile(): " + ex.Message);
        }
        return result;
    }

    /// <summary>
    /// Constructor, initialize everything at zero
    /// </summary>
    public SaveData()
    {
        unlockedCharacterIndexList = new List<int>();
        shopItems = new List<ShopItem>();
        InitializeCharacterLists();
        deathCount = 0;
        bestScore = 0;
        cumulatedScore = 0;
        attempts = 0;
        wins = 0;
        availableCurrency = 0;
        totalSpentCurrency = 0;
        currencySpentInShop = 0;
    }

    /// <summary>
    /// Try to save data into a save file from a SaveData object.
    /// </summary>
    /// <param name="saveFileName"></param>
    /// <param name="saveDataObject"></param>
    /// <returns>true if file was saved correctly</returns>
    public static bool Save(string saveFileName, SaveData saveDataObject)
    {
        return saveDataObject.Save(saveFileName);
    }

    public bool Save(string saveFileName)
    {
        string saveFilePath = GetFilePath(saveFileName);
        Debug.Log("Debug info - Saving at: " + saveFilePath);
        bool result = false;
        try
        {
            string jsonData = JsonUtility.ToJson(this);
            File.WriteAllText(saveFilePath, jsonData);
            result = true;
            Debug.Log("Debug info - Saved successfully");
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning("Exception in Save(): " + ex.Message);
        }
        return result;
    }



    private void OverwriteFromJsonData(string savedData)
    {
        JsonUtility.FromJsonOverwrite(savedData, this);
    }

    /// <summary>
    /// Try to load a save file and create a SaveData object assuming the save file exists and contains valid JSON info.
    /// </summary>
    /// <param name="saveFileName"></param>
    /// <param name="saveDataObject"></param>
    /// <returns>true if success, false if failed</returns>
    public static bool Load(string saveFileName, out SaveData saveDataObject)
    {
        bool success = false;
        string saveFilePath = GetFilePath(saveFileName);
        Debug.Log("Debug info - Loading: " + saveFilePath);
        saveDataObject = null;
        try
        {
            if (File.Exists(saveFilePath))
            {
                string jsonData = File.ReadAllText(saveFilePath);
                saveDataObject = new SaveData();
                saveDataObject.OverwriteFromJsonData(jsonData);
                if (saveDataObject.wonTheGameWithCharacterIndexList.Count != 7)
                {
                    saveDataObject.InitializeCharacterLists();
                }
                success = true;
                Debug.Log("Debug info - loaded successfully");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning("Exception in Load(): " + ex.Message);
        }
        if (!success)
        {
            saveDataObject = null;
        }
        return success;
    }

    private static string GetFilePath(string saveFileName)
    {
        return Application.persistentDataPath + "/" + saveFileName + ".json";
    }
}
