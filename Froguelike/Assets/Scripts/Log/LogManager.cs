using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// LogManager is a class that will store the Debug.Log() messages in a list and eventually save them in a file.
/// These messages will help us getting data from the playtests.
/// </summary>
public class LogManager : MonoBehaviour
{
    public string logFileName = "GameData_STARTTIME.log";

    public bool saveLogInFile;

    private string logFilePath;

    private string tempLogStr;

    private void OnEnable()
    {
        tempLogStr = "";
        if (saveLogInFile)
        {
            Application.logMessageReceived += Log;
        }
        Debug.Log("Game started");
    }

    private void OnDisable()
    {
        Debug.Log("Game stopped");
        if (saveLogInFile)
        {
            Application.logMessageReceived -= Log;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        string startTimeStr = DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss");
        logFilePath = Application.persistentDataPath + "/" + logFileName.Replace("STARTTIME", startTimeStr);
    }
    
    public void Log(string logString, string stackTrace, LogType type)
    {
        string logPrint = "[" + DateTime.Now + "] " + logString;
        if (type == LogType.Error || type == LogType.Exception)
        {
            logPrint += "\n - Stack trace: " + stackTrace;
        }

        if (string.IsNullOrEmpty(logFilePath))
        {
            // can't save right now
            tempLogStr += logPrint + "\n";
        }
        else
        {
            TextWriter tw = new StreamWriter(logFilePath, true);
            if (!string.IsNullOrEmpty(tempLogStr))
            {
                tw.WriteLine(tempLogStr);
                tempLogStr = "";
            }
            tw.WriteLine(logPrint);
            tw.Close();
        }
    }
}
