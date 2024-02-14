using Steamworks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text.RegularExpressions;
using Unity.Mathematics;
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

    [Header("Settings - Logs")]
    public VerboseLevel verbose = VerboseLevel.NONE;

    [Header("Save files")]
    public string demoSaveFileName = "FroguelikeSaveFile_demo";
    [Space]
    public string saveFileName = "FroguelikeSaveFile";
    public string backupSaveFileName = "FroguelikeSaveFile_backup";

    [Header("Settings")]
    public SaveMethod saveMethod = SaveMethod.BINARY;
    [Space]
    public bool createBackup = true;
    public double minimumTimeBetweenBackupsInHours_ifSomethingWentWrong = 0.5;
    public double minimumTimeBetweenBackupsInHours_ifEverythingIsAlright = 6;
    [Space]
    public int deletingOldestBackupsMinimumCount = 30;
    public double deletingOldestBackupsMinimumAgeInDays = 31; // 1 month
    [Space]
    public float saveFileAttemptDelay = 1.0f;

    [Header("Runtime")]
    public bool isSaveDataDirty = false;
    private Coroutine saveFileCoroutine = null;

    #region Unity Callback Methods

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
    }

    private void OnDestroy()
    {
        isSaveDataDirty = false;
        //Save();
    }

    #endregion

    #region Erase Save file

    /// <summary>
    /// Delete save file permanently. Returns true if succeeded. Optional parameter to also erase the backup file.
    /// </summary>
    /// <param name="saveFileName"></param>
    /// <returns></returns>
    public bool EraseSaveFile(bool eraseBackupToo = false)
    {
        bool filesErased = true;
        CreateBackupIfNeeded(beforeWipingSave: true);
        if (BuildManager.instance.demoBuild)
        {
            // Erase demo save file
            filesErased &= EraseSaveFile(demoSaveFileName);
        }
        else
        {
            // Erase game save file(s)
            filesErased &= EraseSaveFile(saveFileName);
            if (eraseBackupToo)
            {
                filesErased &= EraseSaveFile(backupSaveFileName);
            }
        }
        return filesErased;
    }

    private bool EraseSaveFile(string fileName)
    {
        string saveFilePath = GetFilePath(fileName);
        if (verbose == VerboseLevel.MAXIMAL)
        {
            Debug.Log("Debug info - Erasing file: " + saveFilePath);
        }
        bool result = false;
        try
        {
            if (File.Exists(saveFilePath))
            {
                File.Delete(saveFilePath);
            }
            result = true;
            if (verbose != VerboseLevel.NONE)
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

    #endregion

    #region Save

    public void RunSavingCoroutine()
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
            CreateBackupIfNeeded();
        }
        saveFileCoroutine = StartCoroutine(AttemptSavingAsync());
    }

    /// <summary>
    /// Attempt to save data to the save file. 
    /// Backup file could also be saved (depending on save manager settings), if the save was successful the first time.
    /// </summary>
    /// <returns></returns>
    public bool Save()
    {
        bool saveSuccessful = false;
        if (BuildManager.instance.demoBuild)
        {
            // Demo build
            saveSuccessful = Save(demoSaveFileName);
        }
        else
        {
            // EA build
            saveSuccessful = Save(saveFileName);
            if (createBackup && saveSuccessful)
            {
                // Create backup
                saveSuccessful = Save(backupSaveFileName);
            }
        }

        return saveSuccessful;
    }

    private bool Save(string fileName)
    {
        bool result = false;
        string saveFilePath = GetFilePath(fileName);
        if (verbose == VerboseLevel.MAXIMAL)
        {
            Debug.Log("Debug info - Saving at: " + saveFilePath);
        }
        try
        {
            CombinedSaveData allSaveData = CombinedSaveData.GetAllSaveData();
            switch (saveMethod)
            {
                case SaveMethod.JSON:
                    // Save the data in JSON format (readable and editable)
                    if (verbose == VerboseLevel.MAXIMAL)
                    {
                        Debug.Log("Debug info - Save in JSON");
                    }
                    string jsonData = JsonUtility.ToJson(allSaveData);
                    File.WriteAllText(saveFilePath, jsonData);
                    break;
                case SaveMethod.BINARY:
                    // Save the data in Binary format (unreadable)
                    if (verbose == VerboseLevel.MAXIMAL)
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
            if (verbose != VerboseLevel.NONE)
            {
                Debug.Log("File " + fileName + " saved successfully");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Exception in Save(): " + ex.Message);
        }
        return result;
    }

    #endregion

    #region Load

    /// <summary>
    /// Attempt to load data from the save file. Use backup file if the main one fails.
    /// </summary>
    /// <returns></returns>
    public bool Load()
    {
        bool saveFileIsLoaded = false;
        if (BuildManager.instance.demoBuild)
        {
            // Demo build
            // We attempt to load the demo save file if it exists
            saveFileIsLoaded = Load(demoSaveFileName);

            if (!saveFileIsLoaded)
            {
                // If it failed, we try to load the regular save file
                // We only go through IF the regular save file was saved from a Demo build
                saveFileIsLoaded = Load(saveFileName);
            }
        }
        else
        {
            // EA build
            // We attempt to load the EA save file
            saveFileIsLoaded = Load(saveFileName);

            if (!saveFileIsLoaded)
            {
                // If it failed, we try to load the backup file
                saveFileIsLoaded = Load(backupSaveFileName);
            }

            if (!saveFileIsLoaded)
            {
                // If it still failed, we create a backup of that save file that we couldn't load
                CreateBackupIfNeeded();

                // Then we try to load the demo save file
                saveFileIsLoaded = Load(demoSaveFileName);
            }
        }

        return saveFileIsLoaded;
    }

    private bool Load(string fileName)
    {
        bool success = false;

        string saveFilePath = GetFilePath(fileName);
        if (verbose == VerboseLevel.MAXIMAL)
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
                        if (verbose == VerboseLevel.MAXIMAL)
                        {
                            Debug.Log("Debug info - Load in JSON");
                        }
                        string jsonData = File.ReadAllText(saveFilePath);
                        loadedData = JsonUtility.FromJson<CombinedSaveData>(jsonData);
                        break;
                    case SaveMethod.BINARY:
                        // Load the data in Binary format (unreadable)
                        if (verbose == VerboseLevel.MAXIMAL)
                        {
                            Debug.Log("Debug info - Load in Binary");
                        }
                        FileStream dataStream = null;
                        try
                        {
                            dataStream = new FileStream(saveFilePath, FileMode.Open);
                            if (verbose == VerboseLevel.MAXIMAL)
                            {
                                Debug.Log("Debug info - dataStream opened");
                            }
                            BinaryFormatter converter = new BinaryFormatter();
                            if (verbose == VerboseLevel.MAXIMAL)
                            {
                                Debug.Log("Debug info - converter created");
                            }
                            object data = converter.Deserialize(dataStream);
                            if (verbose == VerboseLevel.MAXIMAL)
                            {
                                Debug.Log($"Debug info - data deserialized: {data}");
                            }
                            loadedData = data as CombinedSaveData;
                            if (verbose == VerboseLevel.MAXIMAL)
                            {
                                Debug.Log($"Debug info - loadedData deserialized: {loadedData}");
                            }
                        }
                        catch (System.Exception ex)
                        {
                            Debug.LogError("Exception in Load(): " + ex.Message);
                        }
                        finally
                        {
                            dataStream.Close();
                        }
                        break;
                }

                bool loadedDataComeFromFullGame = loadedData.gameSaveData.isFullGame;
                string loadedDataComeFromVersionNumber = loadedData.gameSaveData.versionNumber;

                // Either:
                // - loaded data come from Early Access build and we're currently playing Early Access build: all good, just load that data!
                // - loaded data come from Demo build and we're currently playing Demo build: all good, just load that data!
                // - loaded data come from Early Access build and we're currently playing Demo build: Don't load anything!
                // - loaded data come from Demo build and we're currently playing Early Access build: New game is initialized, we want to unlock all the achievements and reinitialize the shop with current balance (but no items bought)

                if (BuildManager.instance.demoBuild)
                {
                    if (!loadedDataComeFromFullGame)
                    {
                        if (verbose == VerboseLevel.MAXIMAL)
                        {
                            Debug.Log($"Debug info - Playing Demo, loading Demo save file");
                            Debug.Log($"Debug info - Calling CombinedSaveData.SetAllSaveData({loadedData})");
                        }

                        // Save file comes from demo and we're currently playing demo
                        CombinedSaveData.SetAllSaveData(loadedData);

                        if (verbose == VerboseLevel.MAXIMAL)
                        {
                            Debug.Log($"Debug info - Calling ShopManager.instance.ApplyDemoLimitationToRestocks()");
                        }

                        // We need to make sure the shop items are not restocked too much
                        ShopManager.instance.ApplyDemoLimitationToRestocks();

                        // We need to make sure the run items are not unlocked if they are not part of the demo
                        RunItemManager.instance.ApplyDemoLimitationToRunItems();

                        // We need to make sure the chapters are not unlocked if they are not part of the demo
                        ChapterManager.instance.ApplyDemoLimitationToChapters();

                        // We need to make sure the quests are not unlocked if they are not part of the demo
                        AchievementManager.instance.ApplyDemoLimitationToAchievements();

                        success = true;
                    }
                    else
                    {
                        // Save file comes from EA and we're currently playing demo
                        // No loading
                        if (verbose == VerboseLevel.MAXIMAL)
                        {
                            Debug.Log($"Debug info - Playing Demo but there's only EA save file. Prevent loading.");
                        }
                    }
                }
                else
                {
                    if (!loadedDataComeFromFullGame)
                    {
                        if (verbose == VerboseLevel.MAXIMAL)
                        {
                            Debug.Log($"Debug info - Playing EA, loading Demo save file");
                            Debug.Log($"Debug info - Calling CombinedSaveData.SetSaveDataFromDemoToEA({loadedData})");
                        }

                        // Save file comes from demo and we're currently playing EA
                        CombinedSaveData.SetSaveDataFromDemoToEA(loadedData);
                        success = true;

                        if (verbose == VerboseLevel.MAXIMAL)
                        {
                            Debug.Log($"Debug info - CombinedSaveData.SetSaveDataFromDemoToEA({loadedData}) called");
                        }
                    }
                    else
                    {
                        if (verbose == VerboseLevel.MAXIMAL)
                        {
                            Debug.Log($"Debug info - Playing EA, loading EA save file");
                            Debug.Log($"Debug info - Calling CombinedSaveData.SetAllSaveData({loadedData})");
                        }

                        // Save file comes from EA and we're currently playing EA
                        CombinedSaveData.SetAllSaveData(loadedData);
                        success = true;

                        if (verbose == VerboseLevel.MAXIMAL)
                        {
                            Debug.Log($"Debug info - CombinedSaveData.SetAllSaveData({loadedData}) called");
                        }
                    }
                }

                isSaveDataDirty = false;
                if (verbose != VerboseLevel.NONE)
                {
                    Debug.Log("File " + fileName + " loaded successfully");
                }
            }
            else
            {
                if (verbose == VerboseLevel.MAXIMAL)
                {
                    Debug.Log("Debug info - Can't load: " + saveFilePath + " because it doesn't exist");
                    success = false;
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Exception in Load(): " + ex.Message);
            success = false;
        }

        return success;
    }

    #endregion

    /// <summary>
    /// Create empty save file
    /// </summary>
    /// <returns></returns>
    public bool CreateEmptySaveFile()
    {
        ShopManager.instance.shopData.Reset();
        ShopManager.instance.ResetShop(true);
        CharacterManager.instance.ResetCharacters(true);
        EnemiesManager.instance.ResetEnemies();
        GameManager.instance.gameData.Reset();
        ChapterManager.instance.ResetChapters(true);
        RunItemManager.instance.ResetRunItems();
        AchievementManager.instance.ResetAchievements();

        return Save();
    }

    private void CreateDirectoryIfRequired(string directoryPath)
    {
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }
    }

    public string GetSaveFolderPath()
    {
        return Application.persistentDataPath;
    }

    public string GetSaveFilePath()
    {
        return GetFilePath(saveFileName);
    }

    /// <summary>
    /// Get the path to the file
    /// </summary>
    /// <param name="fileName"></param>
    /// <returns></returns>
    private string GetFilePath(string fileName)
    {
        /*
        string steamUserID = "000";
        if (SteamManager.Initialized)
        {
            CSteamID steamID = SteamUser.GetSteamID();
            steamUserID = steamID.m_SteamID.ToString();
        }*/

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

        string folderPath = GetSaveFolderPath(); ///user{steamUserID}";
        //CreateDirectoryIfRequired(folderPath);

        return $"{folderPath}/{fileName}.{fileExtension}";
    }

    public bool DoesFileExist(string fileName)
    {
        return File.Exists(GetFilePath(fileName));
    }

    public bool DoesSaveFileExist()
    {
        bool thereIsASaveFileToLoad = false;

        thereIsASaveFileToLoad |= DoesFileExist(demoSaveFileName); // A save file from the demo should be loaded by either demo and full game

        if (!BuildManager.instance.demoBuild)
        {
            thereIsASaveFileToLoad |= DoesFileExist(saveFileName); // A save file from the full game would not be loaded by the demo
            thereIsASaveFileToLoad |= DoesFileExist(backupSaveFileName);
        }

        return thereIsASaveFileToLoad;
    }

    public void CreateBackupIfNeeded(bool somethingWentWrong = false, bool beforeWipingSave = false)
    {
        string fileName = saveFileName;
        if (BuildManager.instance.demoBuild)
        {
            fileName = demoSaveFileName;
        }
        string filePath = GetFilePath(fileName);

        if (File.Exists(filePath))
        {
            DateTime dateOfLatestBackup = GetDateTimeOfLatestBackup(fileName);
            DateTime now = DateTime.Now;
            double minimumTimeBetweenBackupsInHours = somethingWentWrong ? minimumTimeBetweenBackupsInHours_ifSomethingWentWrong : minimumTimeBetweenBackupsInHours_ifEverythingIsAlright;
            if (beforeWipingSave || (now - dateOfLatestBackup).TotalHours > minimumTimeBetweenBackupsInHours)
            {
                // It's been more than minimum time (or we force a backup creation), let's create a new backup
                string folderPath = GetSaveFolderPath();
                string backupFileName = $"{fileName}Backup_{now.ToString("yyyy-MM-dd_HH-mm-ss")}";
                string backupFileExtension = "bak";
                if (beforeWipingSave)
                {
                    backupFileName += "_BeforeWipe";
                }
                string backupFilePath = $"{folderPath}/{backupFileName}.{backupFileExtension}";
                File.Copy(filePath, backupFilePath);
                if (verbose == VerboseLevel.MAXIMAL)
                {
                    Debug.Log("Debug info - Created a backup: " + backupFilePath);
                }
                DeleteOldestBackups();
            }
        }
        else
        {
            if (verbose != VerboseLevel.NONE)
            {
                Debug.Log("Debug info - Can't created a backup of a file that does not exist: " + filePath);
            }
        }
    }

    private DateTime GetDateTimeOfLatestBackup(string fileName)
    {
        DateTime latestDateTime = DateTime.MinValue;
        string folderPath = GetSaveFolderPath();

        DirectoryInfo dir = new DirectoryInfo(folderPath);
        FileInfo[] info = dir.GetFiles(fileName+ "Backup*.bak");
        foreach (FileInfo backupFile in info)
        {
            DateTime newDateTime = backupFile.CreationTime;
            if (newDateTime > latestDateTime)
            {
                latestDateTime = newDateTime;
            }            
        }
        return latestDateTime;
    }

    private void DeleteOldestBackups()
    {
        if (verbose == VerboseLevel.MAXIMAL)
        {
            Debug.Log("Debug info - Deleting oldest backup files");
        }

        // Get all backup files and sort them by creation date
        string folderPath = GetSaveFolderPath();
        DirectoryInfo dir = new DirectoryInfo(folderPath);
        FileInfo[] backupFiles = dir.GetFiles("*.bak");
        List<FileInfo> backupFilesList = backupFiles.OrderBy(x => x.CreationTime).ToList();
        List<FileInfo> backupFilesToDeleteList = new List<FileInfo>();

        // How many files need to be deleted?
        int filesToDeleteCounter = (backupFilesList.Count - deletingOldestBackupsMinimumCount);

        DateTime now = DateTime.Now;
        foreach (FileInfo backupFile in backupFilesList)
        {
            if (filesToDeleteCounter <= 0)
            {
                break; // No more files need to be deleted
            }

            if ( (now - backupFile.CreationTime).TotalDays > deletingOldestBackupsMinimumAgeInDays )
            {
                // This file is old enough to be deleted
                backupFilesToDeleteList.Add(backupFile);
                filesToDeleteCounter--;
            }
        }

        // Actually delete the files
        foreach (FileInfo backupFile in backupFilesToDeleteList)
        {
            if (verbose != VerboseLevel.NONE)
            {
                Debug.Log("Debug info - Delete file: " + backupFile.FullName);
            }
            File.Delete(backupFile.FullName);
        }
    }
}
