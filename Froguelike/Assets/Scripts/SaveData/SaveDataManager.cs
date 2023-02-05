using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;


[System.Serializable]
public enum SaveMethod
{
    JSON,
    BINARY
}

/// <summary>
/// - Gather data from multiple sources and assemble them in one structure to save them
/// - Load a file then read from it multiple data and send them to their respective sources
/// - Deal with Steam Cloud System
/// </summary>
public class SaveDataManager : MonoBehaviour
{
    // Singleton
    public static SaveDataManager instance;

    [Header("Save files")]
    public string saveFileName = "FroguelikeSaveFile";
    public string backupSaveFileName = "FroguelikeSaveFile_backup";

    [Header("Settings")]
    public SaveMethod saveMethod = SaveMethod.BINARY;
    public bool debugInfo = false;
    [Space]
    public bool createBackup = true;
    [Space]
    public float saveFileAttemptDelay = 1.0f;

    [Header("Runtime")]
    public bool isSaveDataDirty = false;
    private Coroutine saveFileCoroutine = null;
     
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
        if (saveFileCoroutine != null)
        {
            StopCoroutine(saveFileCoroutine);
        }
        saveFileCoroutine = StartCoroutine(AttemptSavingAsync());
    }

    private IEnumerator AttemptSavingAsync()
    {
        yield return new WaitForSecondsRealtime(saveFileAttemptDelay);
        if (isSaveDataDirty)
        {
            Save();
        }
        saveFileCoroutine = StartCoroutine(AttemptSavingAsync());
    }

    /// <summary>
    /// Delete save file permanently. Returns true if succeeded. Optional parameter to also erase the backup file.
    /// </summary>
    /// <param name="saveFileName"></param>
    /// <returns></returns>
    public bool EraseSaveFile(bool eraseBackupToo = false)
    {
        bool filesErased = true;
        filesErased &= EraseSaveFile(saveFileName);
        if (eraseBackupToo)
        {
            filesErased &= EraseSaveFile(backupSaveFileName);
        }
        return filesErased;
    }

    private bool EraseSaveFile(string fileName)
    {
        string saveFilePath = GetFilePath(fileName);
        if (debugInfo)
        {
            Debug.Log("Debug info - Erasing file: " + saveFilePath);
        }
        bool result = false;
        try
        {
            File.Delete(saveFilePath);
            result = true;
            if (debugInfo)
            {
                Debug.Log("Debug info - Save file erased successfully");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Exception in EraseSaveFile(): " + ex.Message);
        }
        return result;
    }

    /// <summary>
    /// Attempt to save data to the save file. Backup file could also be saved (depending on save manager settings), if the save was successful the first time.
    /// </summary>
    /// <returns></returns>
    public bool Save()
    {
        bool saveSuccessful = Save(saveFileName);
        if (createBackup && saveSuccessful)
        {
            saveSuccessful = Save(backupSaveFileName);
        }
        return saveSuccessful;
    }

    private bool Save(string fileName)
    {
        string saveFilePath = GetFilePath(fileName);
        if (debugInfo)
        {
            Debug.Log("Debug info - Saving at: " + saveFilePath);
        }
        bool result = false;
        try
        {
            CombinedSaveData allSaveData = CombinedSaveData.GetAllSaveData();
            switch (saveMethod)
            {
                case SaveMethod.JSON:
                    // Save the data in JSON format (readable and editable)
                    if (debugInfo)
                    {
                        Debug.Log("Debug info - Save in JSON");
                    }
                    string jsonData = JsonUtility.ToJson(allSaveData);
                    File.WriteAllText(saveFilePath, jsonData);
                    break;
                case SaveMethod.BINARY:
                    // Save the data in Binary format (unreadable)
                    if (debugInfo)
                    {
                        Debug.Log("Debug info - Save in Binary");
                    }
                    FileStream dataStream = new FileStream(saveFilePath, FileMode.Create);
                    BinaryFormatter converter = new BinaryFormatter();
                    converter.Serialize(dataStream, allSaveData);
                    dataStream.Close();
                    break;
            }
            result = true;
            isSaveDataDirty = false;
            if (debugInfo)
            {
                Debug.Log("Debug info - Saved successfully");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Exception in Save(): " + ex.Message);
        }
        return result;
    }

    /// <summary>
    /// Attempt to load data from the save file. Use backup file if the main one fails.
    /// </summary>
    /// <returns></returns>
    public bool Load()
    {
        bool saveFileIsLoaded = Load(saveFileName);
        if (!saveFileIsLoaded)
        {
            saveFileIsLoaded = Load(backupSaveFileName);
        }
        return saveFileIsLoaded;
    }

    private bool Load(string fileName)
    {
        bool success = false;
        string saveFilePath = GetFilePath(fileName);
        if (debugInfo)
        {
            Debug.Log("Debug info - Loading: " + saveFilePath);
        }
        try
        {
            if (File.Exists(saveFilePath))
            {
                CombinedSaveData loadedData = null;
                switch (saveMethod)
                {
                    case SaveMethod.JSON:
                        // Load the data in JSON format (readable and editable)
                        if (debugInfo)
                        {
                            Debug.Log("Debug info - Load in JSON");
                        }
                        string jsonData = File.ReadAllText(saveFilePath);
                        loadedData = JsonUtility.FromJson<CombinedSaveData>(jsonData);
                        break;
                    case SaveMethod.BINARY:
                        // Load the data in Binary format (unreadable)
                        if (debugInfo)
                        {
                            Debug.Log("Debug info - Load in Binary");
                        }
                        FileStream dataStream = new FileStream(saveFilePath, FileMode.Open);
                        BinaryFormatter converter = new BinaryFormatter();
                        loadedData = converter.Deserialize(dataStream) as CombinedSaveData;
                        dataStream.Close();
                        break;
                }
                CombinedSaveData.SetAllSaveData(loadedData);
                success = true;
                isSaveDataDirty = false;
                if (debugInfo)
                {
                    Debug.Log("Debug info - loaded successfully");
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Exception in Load(): " + ex.Message);
        }
        return success;
    }

    /// <summary>
    /// Create empty save file
    /// </summary>
    /// <returns></returns>
    public bool CreateEmptySaveFile()
    {
        ShopManager.instance.shopData.Reset();
        ShopManager.instance.ResetShop(true);

        CharacterManager.instance.ResetCharacters(true);

        GameManager.instance.gameData.Reset();

        return Save();
    }

    /// <summary>
    /// Get the path to the file
    /// </summary>
    /// <param name="fileName"></param>
    /// <returns></returns>
    private string GetFilePath(string fileName)
    {
        string dataPath = Application.persistentDataPath;
        string fileExtension = "";
        switch(saveMethod)
        {
            case SaveMethod.JSON:
                fileExtension = "json";
                break;
            case SaveMethod.BINARY:
                fileExtension = "bin";
                break;
        }
        return $"{dataPath}/{fileName}.{fileExtension}";
    }
}
