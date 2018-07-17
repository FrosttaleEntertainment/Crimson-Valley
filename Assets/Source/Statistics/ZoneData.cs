
using System;
using System.Collections.Generic;

public class ZoneData  {

	public Zone ZoneName { get; private set;}
    public Dictionary<GameStatisticsType, StatisticsPair> StatisticValues { get; private set;}

    public ZoneData(Zone name)
    {
        this.ZoneName = name;
        StatisticValues = new Dictionary<GameStatisticsType, StatisticsPair>();

        FillStatistics();
    }

    public float GetStatValue(GameStatisticsType stat)
    {
        return StatisticValues[stat].Value;
    }

    public StatisticsPair GetStatPair(GameStatisticsType stat)
    {
        return StatisticValues[stat];
    }

    public void SetStatValue(GameStatisticsType stat, float value)
    {
        StatisticValues[stat].Value = value;
    }

    public void SetStatPair(GameStatisticsType stat, StatisticsPair pair)
    {
        StatisticValues[stat] = pair;
    }

    public void ModifyStatValue(GameStatisticsType stat, float value)
    {
        StatisticValues[stat].Value += value;
    }

    private void FillStatistics()
    {
        var enumList = Enum.GetValues(typeof(GameStatisticsType));

        foreach (var statPair in enumList)
        {
            StatisticValues.Add((GameStatisticsType)statPair, new StatisticsPair((GameStatisticsType)statPair));
        }
    }
}
