using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// TongueStat is a list of all the stats describing a tongue.
/// </summary>
[System.Serializable]
public enum TongueStat
{
    DAMAGE, // Base damage
    SPEED, // Base attack speed
    COOLDOWN, // Base cooldown
    RANGE, // Base range
    SIZE, // Base size
    AREA, // Base damage area
    COUNT, // Number of active weapons

    VAMPIRE_RATIO, // % of damage that is absorbed as health

    POISON_DAMAGE, // Damage done as poison status is active

    CURSE_EFFECT, // % of curse applied (the curse affects the enemy and boosts its speed and damage)
    FREEZE_EFFECT, // Freeze effect (either 0 or 1)

    DURATION, // Time during which the tongue is out / the effect is active

    KNOCKBACK // Knockback force
}

/// <summary>
/// TongueStatValue is a handy class storing a TongueStat with its value.
/// Storing a list of TongueStatValue should be enough to describe a tongue.
/// </summary>
[System.Serializable]
public class TongueStatValue : IEquatable<TongueStatValue>, ICloneable
{
    [Tooltip("What kind of stat are we talking about? Damage? Cooldown? Range?")]
    public TongueStat stat;
    [Tooltip("What is the value of this stat?")]
    public double value;

    /// <summary>
    /// Constructor that just create a default stat (value is zero)
    /// </summary>
    /// <param name="s"></param>
    public TongueStatValue(TongueStat s)
    {
        stat = s;
        value = 0;
    }

    /// <summary>
    /// Constructor that copies a TongueStatValue
    /// </summary>
    /// <param name="origin"></param>
    public TongueStatValue(TongueStatValue origin)
    {
        stat = origin.stat;
        value = origin.value;
    }

    /// <summary>
    /// Will clone a TongueStatValue to be sure to not have multiple references pointing to the same object!
    /// </summary>
    /// <returns></returns>
    public object Clone()
    {
        return new TongueStatValue(this);
    }
    
    /// <summary>
    /// Two TongueStatValue are considered equal if they have the same stat type
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    public bool Equals(TongueStatValue other)
    {
        return (other != null && this.stat == other.stat);
    }
}

/// <summary>
/// TongueStatsWrapper is a handy class to store a bunch of TongueStatValue. 
/// It is mostly used to be able to serialize a list of TongueStatValue (useful for saving/loading data).
/// </summary>
[System.Serializable]
public class TongueStatsWrapper
{
    [Tooltip("A list of tongue stats and their values")]
    public List<TongueStatValue> statsList;

    /// <summary>
    /// Default constructor that initialize the list
    /// </summary>
    public TongueStatsWrapper()
    {
        statsList = new List<TongueStatValue>();
    }

    /// <summary>
    /// Constructor that create a new TongueStatsWrapper with all the stats and values from a given list.
    /// </summary>
    /// <param name="statValuesList"></param>
    public TongueStatsWrapper(List<TongueStatValue> statValuesList)
    {
        statsList = new List<TongueStatValue>(statValuesList);
    }

    /// <summary>
    /// Handy method that returns the TongueStatValue object for a given weapon stat type.
    /// Would return a default one (with value of zero) if the list doesn't contain any information for that weapon stat type.
    /// </summary>
    /// <param name="statType"></param>
    /// <returns></returns>
    public TongueStatValue GetStatValue(TongueStat statType)
    {
        TongueStatValue statValue = statsList.FirstOrDefault(x => x.stat.Equals(statType));
        if (statValue == null)
        {
            statValue = new TongueStatValue(statType);
        }
        return statValue;
    }

    /// <summary>
    /// Will attempt to get a value for the given tongue stat type.
    /// If the stat is not defined or its value is zero, the method will return false.
    /// If the stat is defined and has a non-zero value, the method will return true.
    /// </summary>
    /// <param name="statType"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public bool GetValueForStat(TongueStat statType, out float value)
    {
        TongueStatValue statValue = GetStatValue(statType);
        value = (float)statValue.value; // value can be zero
        return (statValue.value != 0);
    }

    /// <summary>
    /// Static method to merge two lists.
    /// Remove stat duplicates (only one stat of each type max).
    /// </summary>
    /// <param name="statsWrapper1"></param>
    /// <param name="statsWrapper2"></param>
    /// <returns></returns>
    public static TongueStatsWrapper JoinLists(TongueStatsWrapper statsWrapper1, TongueStatsWrapper statsWrapper2)
    {
        return TongueStatsWrapper.JoinLists(statsWrapper1.statsList, statsWrapper2.statsList);
    }

    /// <summary>
    /// Static method to merge two lists.
    /// Remove stat duplicates (only one stat of each type max).
    /// </summary>
    /// <param name="statsWrapper1"></param>
    /// <param name="statsList2"></param>
    /// <returns></returns>
    public static TongueStatsWrapper JoinLists(TongueStatsWrapper statsWrapper1, List<TongueStatValue> statsList2)
    {
        return TongueStatsWrapper.JoinLists(statsWrapper1.statsList, statsList2);
    }

    /// <summary>
    /// Static method to merge two lists.
    /// Remove stat duplicates (only one stat of each type max).
    /// </summary>
    /// <param name="statsList1"></param>
    /// <param name="statsList2"></param>
    /// <returns></returns>
    public static TongueStatsWrapper JoinLists(List<TongueStatValue> statsList1, List<TongueStatValue> statsList2)
    {
        TongueStatsWrapper result = new TongueStatsWrapper();

        // Copy values from list 1 to result list
        foreach (TongueStatValue statValue in statsList1)
        {
            result.statsList.Add(new TongueStatValue(statValue));
        }

        // Copy values from list 2 to result list
        // Merge values if needed
        foreach (TongueStatValue statValue in statsList2)
        {
            TongueStatValue statValueInResultList = result.statsList.FirstOrDefault(x => x.stat.Equals(statValue.stat));
            if (statValueInResultList != null)
            {
                // The list already contains a value for this stat, we must add them together
                statValueInResultList.value += statValue.value;
            }
            else
            {
                // The list doesn't contain a value for this stat, we must add that stat to the list
                result.statsList.Add(new TongueStatValue(statValue));
            }
        }

        return result;
    }

    public static string GetDescription(List<TongueStatValue> weaponStatsList)
    {
        string result = "";
        foreach (TongueStatValue statValue in weaponStatsList)
        {
            if (DataManager.instance.TryGetStatData(statValue.stat, out string shortName, out string longName, out string unit, out bool usePercent))
            {
                string statNameStr = longName;
                string plusSign = (statValue.value < 0) ? "" : "+";
                string statValueStr = statValue.value.ToString("0");
                if (statValue.stat == TongueStat.DAMAGE || statValue.stat == TongueStat.POISON_DAMAGE)
                {
                    statValueStr = (statValue.value * 10).ToString("0");
                }
                if (usePercent)
                {
                    statValueStr = statValue.value.ToString("P0").Replace(" ٪", "%");
                }
                result += $"{statNameStr} {plusSign}{statValueStr}\n";
            }
        }
        result = result.Substring(0, result.Length - 1); // Remove last character
        return result;
    }

    public string GetDescription()
    {
        return GetDescription(statsList);
    }
}
