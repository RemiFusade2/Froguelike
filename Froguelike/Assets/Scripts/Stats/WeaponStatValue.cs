using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
public enum WeaponStat
{
    DAMAGE, // Base damage
    SPEED, // Base attack speed
    COOLDOWN, // Base cooldown
    RANGE, // Base range
    WIDTH, // Base Width
    AREA, // Base damage area
    COUNT, // Number of active weapons

    VAMPIRE_RATIO, // % of damage that is absorbed as health

    POISON_DAMAGE, // damage done as poison status is active
    POISON_DURATION, // duration of poison status

    FREEZE_RATIO, // % of speed removed when frozen
    FREEZE_DURATION, // duration of frozen status

    CURSE_RATIO, // % of curse applied (the curse affects the enemy and boosts its speed and damage)
    CURSE_DURATION // duration of the curse status
}

[System.Serializable]
public class WeaponStatValue : IEquatable<WeaponStatValue>, ICloneable
{
    public WeaponStat stat;
    public double value;

    public WeaponStatValue(WeaponStat s)
    {
        stat = s;
        value = 0;
    }

    public WeaponStatValue(WeaponStatValue origin)
    {
        stat = origin.stat;
        value = origin.value;
    }

    public object Clone()
    {
        return new WeaponStatValue(this);
    }

    // Two StatValue are considered equal if they have the same stat type
    public bool Equals(WeaponStatValue other)
    {
        return (other != null && this.stat == other.stat);
    }
}

[System.Serializable]
public class WeaponStatsWrapper
{
    public List<WeaponStatValue> statsList;

    public WeaponStatsWrapper()
    {
        statsList = new List<WeaponStatValue>();
    }

    public WeaponStatsWrapper(List<WeaponStatValue> statValuesList)
    {
        statsList = new List<WeaponStatValue>(statValuesList);
    }

    public WeaponStatValue GetStatValue(WeaponStat statType)
    {
        WeaponStatValue statValue = statsList.FirstOrDefault(x => x.stat.Equals(statType));
        if (statValue == null)
        {
            statValue = new WeaponStatValue(statType);
        }
        return statValue;
    }

    public bool GetValueForStat(WeaponStat statType, out float value)
    {
        WeaponStatValue statValue = GetStatValue(statType);
        value = 0;
        bool statValueIsDefined = (statValue != null);
        if (statValueIsDefined)
        {
            value = (float)statValue.value;
        }
        return statValueIsDefined;
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
