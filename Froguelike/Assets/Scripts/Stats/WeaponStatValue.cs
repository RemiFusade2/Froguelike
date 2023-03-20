using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// WeaponStat is a list of all the stats describing a weapon.
/// </summary>
[System.Serializable]
public enum WeaponStat
{
    DAMAGE, // Base damage
    SPEED, // Base attack speed
    COOLDOWN, // Base cooldown
    RANGE, // Base range
    SIZE, // Base size
    AREA, // Base damage area
    COUNT, // Number of active weapons

    VAMPIRE_RATIO, // % of damage that is absorbed as health

    POISON_DAMAGE, // damage done as poison status is active
    POISON_DURATION, // duration of poison status

    FREEZE_RATIO, // % of speed removed when frozen
    FREEZE_DURATION, // duration of frozen status

    CURSE_RATIO, // % of curse applied (the curse affects the enemy and boosts its speed and damage)
    CURSE_DURATION, // duration of the curse status

    DURATION // Time during which the tongue is out / the effect is active
}

/// <summary>
/// WeaponStatValue is a handy class storing a WeaponStat with its value.
/// Storing a list of WeaponStatValue should be enough to describe a weapon.
/// </summary>
[System.Serializable]
public class WeaponStatValue : IEquatable<WeaponStatValue>, ICloneable
{
    [Tooltip("What kind of stat are we talking about? Damage? Cooldown? Range?")]
    public WeaponStat stat;
    [Tooltip("What is the value of this stat?")]
    public double value;

    /// <summary>
    /// Constructor that just create a default stat (value is zero)
    /// </summary>
    /// <param name="s"></param>
    public WeaponStatValue(WeaponStat s)
    {
        stat = s;
        value = 0;
    }

    /// <summary>
    /// Constructor that copies a WeaponStatValue
    /// </summary>
    /// <param name="origin"></param>
    public WeaponStatValue(WeaponStatValue origin)
    {
        stat = origin.stat;
        value = origin.value;
    }

    /// <summary>
    /// Will clone a WeaponStatValue to be sure to not have multiple references pointing to the same object!
    /// </summary>
    /// <returns></returns>
    public object Clone()
    {
        return new WeaponStatValue(this);
    }
    
    /// <summary>
    /// Two WeaponStatValue are considered equal if they have the same stat type
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    public bool Equals(WeaponStatValue other)
    {
        return (other != null && this.stat == other.stat);
    }
}

/// <summary>
/// WeaponStatsWrapper is a handy class to store a bunch of WeaponStatValue. 
/// It is mostly used to be able to serialize a list of WeaponStatValue (useful for saving/loading data).
/// </summary>
[System.Serializable]
public class WeaponStatsWrapper
{
    [Tooltip("A list of weapon stats and their values")]
    public List<WeaponStatValue> statsList;

    /// <summary>
    /// Default constructor that initialize the list
    /// </summary>
    public WeaponStatsWrapper()
    {
        statsList = new List<WeaponStatValue>();
    }

    /// <summary>
    /// Constructor that create a new WeaponStatsWrapper with all the stats and values from a given list.
    /// </summary>
    /// <param name="statValuesList"></param>
    public WeaponStatsWrapper(List<WeaponStatValue> statValuesList)
    {
        statsList = new List<WeaponStatValue>(statValuesList);
    }

    /// <summary>
    /// Handy method that returns the WeaponStatValue object for a given weapon stat type.
    /// Would return a default one (with value of zero) if the list doesn't contain any information for that weapon stat type.
    /// </summary>
    /// <param name="statType"></param>
    /// <returns></returns>
    public WeaponStatValue GetStatValue(WeaponStat statType)
    {
        WeaponStatValue statValue = statsList.FirstOrDefault(x => x.stat.Equals(statType));
        if (statValue == null)
        {
            statValue = new WeaponStatValue(statType);
        }
        return statValue;
    }

    /// <summary>
    /// Will attempt to get a value for the given weapon stat type.
    /// If the stat is not defined or its value is zero, the method will return false.
    /// If the stat is defined and has a non-zero value, the method will return true.
    /// </summary>
    /// <param name="statType"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public bool GetValueForStat(WeaponStat statType, out float value)
    {
        WeaponStatValue statValue = GetStatValue(statType);
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
    public static WeaponStatsWrapper JoinLists(WeaponStatsWrapper statsWrapper1, WeaponStatsWrapper statsWrapper2)
    {
        return WeaponStatsWrapper.JoinLists(statsWrapper1.statsList, statsWrapper2.statsList);
    }

    /// <summary>
    /// Static method to merge two lists.
    /// Remove stat duplicates (only one stat of each type max).
    /// </summary>
    /// <param name="statsWrapper1"></param>
    /// <param name="statsList2"></param>
    /// <returns></returns>
    public static WeaponStatsWrapper JoinLists(WeaponStatsWrapper statsWrapper1, List<WeaponStatValue> statsList2)
    {
        return WeaponStatsWrapper.JoinLists(statsWrapper1.statsList, statsList2);
    }

    /// <summary>
    /// Static method to merge two lists.
    /// Remove stat duplicates (only one stat of each type max).
    /// </summary>
    /// <param name="statsList1"></param>
    /// <param name="statsList2"></param>
    /// <returns></returns>
    public static WeaponStatsWrapper JoinLists(List<WeaponStatValue> statsList1, List<WeaponStatValue> statsList2)
    {
        WeaponStatsWrapper result = new WeaponStatsWrapper();

        // Copy values from list 1 to result list
        foreach (WeaponStatValue statValue in statsList1)
        {
            result.statsList.Add(new WeaponStatValue(statValue));
        }

        // Copy values from list 2 to result list
        // Merge values if needed
        foreach (WeaponStatValue statValue in statsList2)
        {
            WeaponStatValue statValueInResultList = result.statsList.FirstOrDefault(x => x.stat.Equals(statValue.stat));
            if (statValueInResultList != null)
            {
                // The list already contains a value for this stat, we must add them together
                statValueInResultList.value += statValue.value;
            }
            else
            {
                // The list doesn't contain a value for this stat, we must add that stat to the list
                result.statsList.Add(new WeaponStatValue(statValue));
            }
        }

        return result;
    }
}
