using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[System.Serializable]
public enum STAT
{
    MAX_HEALTH, // max HP
    HEALTH_RECOVERY_BOOST, // add a % on health recovery
    ARMOR, // remove armor from every damage taken

    XP_BOOST, // add a % on each XP gain
    CURRENCY_BOOST, // add a % on each currency gain
    CURSE, // a factor that boost the amount of enemies spawned, their health, and the XP they give

    WALK_SPEED_BOOST, // add a % on walking speed (on the ground). -1 is -100%, so impossible to move
    SWIM_SPEED_BOOST, // add a % on swimming speed (on ponds and lakes). -1 is -100%, so impossible to move

    REVIVAL, // number of extra lives
    REROLL, // number of rerolls (upgrades and chapters can be rerolled)
    BANISH, // number of banishes (upgrades and chapters can be banished, a banished one will not show up again for this run)
    SKIP, // number of skips (upgrades can be skipped, in that case no upgrade is applied on this level)

    ATK_DAMAGE_BOOST, // add a % on all weapons damage
    ATK_SPEED_BOOST, // add a % on all weapons attack speed
    ATK_COOLDOWN_BOOST, // add a % on all weapons cooldown recovery speed
    ATK_RANGE_BOOST, // add a % on all weapons range
    ATK_AREA_BOOST, // add a % on all weapons area of damage
    ATK_SPECIAL_STRENGTH_BOOST, // add a % on all weapons special effect strength (speed bonus/malus, vampire, poison damage)
    ATK_SPECIAL_DURATION_BOOST, // add a % on all weapons special effect duration

    MAGNET_RANGE_BOOST // add a % on the range of magnet (ie: radius of magnet collider)
}

[System.Serializable]
public class StatValue : IEquatable<StatValue>, ICloneable
{
    public STAT stat;
    public double value;

    public StatValue(STAT s, double v)
    {
        stat = s;
        value = v;
    }

    public StatValue(StatValue origin)
    {
        stat = origin.stat;
        value = origin.value;
    }

    public object Clone()
    {
        return new StatValue(this);
    }

    // Two StatValue are considered equal if they have the same stat type
    public bool Equals(StatValue other)
    {
        return (other != null && this.stat == other.stat);
    }
}

[System.Serializable]
public class StatsWrapper
{
    public List<StatValue> statsList;

    public StatsWrapper()
    {
        statsList = new List<StatValue>();
    }

    public StatsWrapper(List<StatValue> statValuesList)
    {
        statsList = new List<StatValue>(statValuesList);
    }

    public StatValue GetStatValue(STAT statType)
    {
        StatValue statValue = statsList.FirstOrDefault(x => x.stat.Equals(statType));
        if (statValue == null)
        {
            statValue = new StatValue(statType, 0);
        }
        return statValue;
    }

    public bool GetValueForStat(STAT statType, out float value)
    {
        StatValue statValue = GetStatValue(statType);
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
    public static StatsWrapper JoinLists(StatsWrapper statsWrapper1, StatsWrapper statsWrapper2)
    {
        return StatsWrapper.JoinLists(statsWrapper1.statsList, statsWrapper2.statsList);
    }

    /// <summary>
    /// Static method to merge two lists.
    /// Remove stat duplicates (only one stat of each type max).
    /// </summary>
    /// <param name="statsWrapper1"></param>
    /// <param name="statsList2"></param>
    /// <returns></returns>
    public static StatsWrapper JoinLists(StatsWrapper statsWrapper1, List<StatValue> statsList2)
    {
        return StatsWrapper.JoinLists(statsWrapper1.statsList, statsList2);
    }

    /// <summary>
    /// Static method to merge two lists.
    /// Remove stat duplicates (only one stat of each type max).
    /// </summary>
    /// <param name="statsList1"></param>
    /// <param name="statsList2"></param>
    /// <returns></returns>
    public static StatsWrapper JoinLists(List<StatValue> statsList1, List<StatValue> statsList2)
    {
        StatsWrapper result = new StatsWrapper();

        // Copy values from list 1 to result list
        foreach (StatValue statValue in statsList1)
        {
            result.statsList.Add(new StatValue(statValue));
        }

        // Copy values from list 2 to result list
        // Merge values if needed
        foreach (StatValue statValue in statsList2)
        {
            StatValue statValueInResultList = result.statsList.FirstOrDefault(x => x.stat.Equals(statValue.stat));
            if (statValueInResultList != null)
            {
                // The list already contains a value for this stat, we must add them together
                statValueInResultList.value += statValue.value;
            }
            else
            {
                // The list doesn't contain a value for this stat, we must add that stat to the list
                result.statsList.Add(new StatValue(statValue));
            }
        }

        return result;
    }
}
