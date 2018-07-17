using System.Collections.Generic;

public enum GameStatisticsType
{
    ZombiesKilled,
    DamageDealt,
    CollectiblesFound,
    RepairCount,
    TimesDied
}

//TODO: move this out
public enum Zone
{
    Forest,
    Desert,
    Tropic
}

public static class StatisticsManager
{
    private static Dictionary<Zone, ZoneData> Data = new Dictionary<Zone, ZoneData>();		

    static StatisticsManager()
    {
        LoadData();
    }
    
    public static void AddZoneData(ZoneData data){
        if(data == null){
            return;
        }

        Data.Add(data.ZoneName, data);
    }

    public static float GetStatisticValue(Zone zone, GameStatisticsType stat)
    {
        float result = Data[zone].GetStatValue(stat);

        return result;
    }

    /// <summary>
    /// Sets the statistic value explicitly.
    /// </summary>
    public static void SetStatisticValue(Zone zone,GameStatisticsType stat, float value)
    {
        Data[zone].SetStatValue(stat, value);
    }

    /// <summary>
    /// Modifies the stat value with the given one
    /// Positive number will increase it
    /// Negative will decrease it
    /// </summary>
    public static void ModifyStatValue(Zone zone, GameStatisticsType stat, float value)
    {
        Data[zone].ModifyStatValue(stat, value);
    }

    public static StatisticsPair GetStatisticPair(Zone zone, GameStatisticsType stat)
    {
        StatisticsPair result = Data[zone].GetStatPair(stat);

        return result;
    }

    public static Dictionary<GameStatisticsType, StatisticsPair> GetAllStatPairs(Zone zone)
    {
        var result = Data[zone].StatisticValues;

        return result;
    }

    private static void LoadData()
    {
        //Load data
    }

    private static void SaveData()
    {
        //Save data
    }

}