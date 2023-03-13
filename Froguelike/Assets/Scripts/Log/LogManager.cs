using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

/// <summary>
/// LogManager is a class that will store the Debug.Log() messages in a list and eventually save them in a file.
/// These messages will help us getting data from the playtests.
/// </summary>
public class LogManager : MonoBehaviour
{
    public string logFileName = "GameData_STARTTIME.log";

    public bool saveLogInFile;

    [Space]
    public bool readLogs;
    public List<string> readLogFileNames;

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

        if (readLogs)
            CompileInfoFromLogFile();
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

    private void CompileInfoFromLogFile()
    {
        string log = "";
        int numberOfRuns = 0;
        float totalPlayTime = 0;
        int totalKillCount = 0;
        int maxKillCount = 0;
        Dictionary<string, int> weaponsUsed = new Dictionary<string, int>();
        Dictionary<string, int> frogsPicked = new Dictionary<string, int>();
        int froinsSpent = 0;
        int refundCount = 0;
        Dictionary<string, int> friendsFound = new Dictionary<string, int>();
        int hatFound = 0;
        int partyChapterPlayed = 0;
        int goldenChapterPlayed = 0;

        foreach (string readLogFileName in readLogFileNames)
        {
            string[] allLogLines = File.ReadAllLines(Application.persistentDataPath + "/" + readLogFileName);

            foreach (string str in allLogLines)
            {
                Match m = Regex.Match(str, "Run Manager - Start a new Run with character: (.+)");
                if (m.Success)
                {
                    numberOfRuns++;
                    Group p = m.Groups[1];
                    string characterPlayed = p.Captures[0].Value;
                    if (!frogsPicked.ContainsKey(characterPlayed))
                    {
                        frogsPicked.Add(characterPlayed, 1);
                    }
                    else
                    {
                        frogsPicked[characterPlayed]++;
                    }
                }

                m = Regex.Match(str, "Total time [(]pause included[)] is (.+) seconds");
                if (m.Success)
                {
                    Group p = m.Groups[1];
                    string totalTimePlayed = (p.Captures[0].Value).Replace('.',',');
                    if (float.TryParse(totalTimePlayed, out float result))
                    {
                        totalPlayTime += result;
                    }
                }

                m = Regex.Match(str, "-> Total score: (.+)");
                if (m.Success)
                {
                    Group p = m.Groups[1];
                    string killCount = p.Captures[0].Value;
                    if (int.TryParse(killCount, out int kills))
                    {
                        totalKillCount += kills;
                        if (kills > maxKillCount)
                        {
                            maxKillCount = kills;
                        }
                    }
                }
                
                m = Regex.Match(str, "-> (.*) Lvl (.*) - ate a total of (.*) bugs");
                if (m.Success)
                {
                    Group pWeapon = m.Groups[1];
                    Group plvl = m.Groups[2];
                    string weaponStr = pWeapon.Captures[0].Value;
                    string lvlStr = plvl.Captures[0].Value;
                    if (!weaponsUsed.ContainsKey(weaponStr))
                    {
                        weaponsUsed.Add(weaponStr, int.Parse(lvlStr));
                    }
                    else
                    {
                        weaponsUsed[weaponStr] += int.Parse(lvlStr);
                    }
                }

                m = Regex.Match(str, "Shop - Refund all");
                if (m.Success)
                {
                    refundCount++;
                }
                m = Regex.Match(str, "Shop - Buy item (.*) to level (.*) for a cost of (.*) Froins.");
                if (m.Success)
                {
                    Group p = m.Groups[3];
                    string coinSpent = p.Captures[0].Value;
                    if (int.TryParse(coinSpent, out int coins))
                    {
                        froinsSpent += coins;
                    }
                }

                m = Regex.Match(str, "Player - Add Friend: (.*)");
                if (m.Success)
                {
                    Group p = m.Groups[1];
                    string friendName = p.Captures[0].Value;
                    if (!friendsFound.ContainsKey(friendName))
                    {
                        friendsFound.Add(friendName, 1);
                    }
                    else
                    {
                        friendsFound[friendName] += 1;
                    }
                }

                m = Regex.Match(str, "Collect: HAT");
                if (m.Success)
                {
                    hatFound++;
                }

                m = Regex.Match(str, "Chapter - Start screen - ([[].*[]])");
                if (m.Success)
                {
                    Group p = m.Groups[1];
                    string chapterID = p.Captures[0].Value;
                    if (chapterID.Equals("[PARTY]"))
                    {
                        partyChapterPlayed++;
                    }
                    if (chapterID.Equals("[GOLDEN_FLY]"))
                    {
                        goldenChapterPlayed++;
                    }
                }
            }
        }


        log += $"Number of runs = {numberOfRuns}\n";
        log += $"Total play time = {totalPlayTime}\n";
        log += $"Total kill count = {totalKillCount}\n";
        log += $"Max kill count = {maxKillCount}\n";
        log += $"Froins spent = {froinsSpent}\n";
        log += $"Refunds = {refundCount}\n";
        log += $"Hats found = {hatFound}\n";
        log += $"Chapter PARTY played {partyChapterPlayed} times\n";
        log += $"Chapter GOLDEN FLY played {goldenChapterPlayed} times\n";
        foreach (KeyValuePair<string, int> kp in frogsPicked)
        {
            log += $"Frog played: {kp.Key} - {kp.Value} times\n";
        }
        foreach (KeyValuePair<string, int> kp in weaponsUsed)
        {
            log += $"Weapon: {kp.Key} - total levels {kp.Value}\n";
        }
        foreach (KeyValuePair<string, int> kp in friendsFound)
        {
            log += $"Friend: {kp.Key} - found {kp.Value} times\n";
        }

        Debug.Log(log);
    }


}
