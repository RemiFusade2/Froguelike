using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

/// <summary>
/// CharacterStat is a list of all the stats describing a character.
/// </summary>
[System.Serializable]
public enum CharacterStat
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

/// <summary>
/// StatValue is a handy class storing a CharacterStat with its value.
/// Storing a list of StatValue should be enough to describe a character.
/// </summary>
[System.Serializable]
public class StatValue : IEquatable<StatValue>, ICloneable
{
    [Tooltip("What kind of stat are we talking about? Max HP? Damage bonus? Armor?")]
    public CharacterStat stat;
    [Tooltip("What is the value of this stat?")]
    public double value;

    /// <summary>
    /// Constructor that just create a default stat (value is zero)
    /// </summary>
    /// <param name="s"></param>
    public StatValue(CharacterStat s)
    {
        stat = s;
        value = 0;
    }

    /// <summary>
    /// Standard constructor for a StatValue
    /// </summary>
    /// <param name="s"></param>
    /// <param name="v"></param>
    public StatValue(CharacterStat s, double v)
    {
        stat = s;
        value = v;
    }

    /// <summary>
    /// Constructor that copies a StatValue
    /// </summary>
    /// <param name="origin"></param>
    public StatValue(StatValue origin)
    {
        stat = origin.stat;
        value = origin.value;
    }

    /// <summary>
    /// Will clone a StatValue to be sure to not have multiple references pointing to the same object!
    /// </summary>
    /// <returns></returns>
    public object Clone()
    {
        return new StatValue(this);
    }

    /// <summary>
    /// Two StatValue are considered equal if they have the same stat type
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    public bool Equals(StatValue other)
    {
        return (other != null && this.stat == other.stat);
    }
}

/// <summary>
/// StatsWrapper is a handy class to store a bunch of StatValue. 
/// It is mostly used to be able to serialize a list of StatValue (useful for saving/loading data).
/// </summary>
[System.Serializable]
public class StatsWrapper
{
    [Tooltip("A list of stats and their values")]
    public List<StatValue> statsList;

    /// <summary>
    /// Default constructor that initialize the list
    /// </summary>
    public StatsWrapper()
    {
        statsList = new List<StatValue>();
    }

    /// <summary>
    /// Constructor that create a new StatsWrapper with all the stats and values from a given list.
    /// </summary>
    /// <param name="statValuesList"></param>
    public StatsWrapper(List<StatValue> statValuesList)
    {
        statsList = new List<StatValue>(statValuesList);
    }

    /// <summary>
    /// Handy method that returns the StatValue object for a given character stat type.
    /// Would return a default one (with value of zero) if the list doesn't contain any information for that stat type.
    /// </summary>
    /// <param name="statType"></param>
    /// <returns></returns>
    public StatValue GetStatValue(CharacterStat statType)
    {
        StatValue statValue = statsList.FirstOrDefault(x => x.stat.Equals(statType));
        if (statValue == null)
        {
            statValue = new StatValue(statType, 0);
        }
        return statValue;
    }

    /// <summary>
    /// Will attempt to get a value for the given stat type.
    /// If the stat is not defined or its value is zero, the method will return false.
    /// If the stat is defined and has a non-zero value, the method will return true.
    /// </summary>
    /// <param name="statType"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public bool GetValueForStat(CharacterStat statType, out float value)
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
