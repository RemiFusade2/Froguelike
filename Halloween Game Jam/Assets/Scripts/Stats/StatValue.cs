using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
public class StatValue : IEquatable<StatValue>
{
    public STAT stat;
    public double value;

    public StatValue(StatValue origin)
    {
        stat = origin.stat;
        value = origin.value;
    }

    // Two StatValue are considered equal if they have the same stat type
    public bool Equals(StatValue other)
    {
        return this.stat == other.stat;
    }
}

[System.Serializable]
public class StatsWrapper
{
    public List<StatValue> statsList;
}
