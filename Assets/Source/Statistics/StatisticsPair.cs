using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StatisticsPair
{

    public GameStatisticsType Stat { get; set; }

    public float Value { get; set; }

    public StatisticsPair(GameStatisticsType type, float initValue = 0)
    {
        this.Stat = type;
        this.Value = initValue;
    }

    public static StatisticsPair operator +(StatisticsPair stat, float value)
    {
        stat.Value += value;

        return stat;
    }

    public static StatisticsPair operator -(StatisticsPair stat, float value)
    {
        stat.Value -= value;

        return stat;
    }

    public override string ToString()
    {
        return string.Format("{0} = {1}", Stat, Value);
    }

}
