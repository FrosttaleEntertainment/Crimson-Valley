using UnityEngine;

public enum EntityGroup { Player, Zombie, TreeOfLife, Other };
public enum ArmorType { None, Fat };

[System.Serializable]
public class EntityStats
{
    [SerializeField]
    public EntityGroup EntityGroup;
    [SerializeField]
    public ArmorType ArmorType;
    [SerializeField]
    public string Name;
    [SerializeField]
    public int TeamID;
    [SerializeField]
    public float MoveSpeed;
    [SerializeField]
    public float TurnSpeed;
    [SerializeField]
    public GameObject DeathFx;
    [SerializeField]
    public GameObject LevelUpFx;
    [SerializeField]
    public GameObject SpawnFx;

    // Weapon related stats
    [SerializeField]
    public bool UseMecanim;
    [SerializeField]
    public GameObject WeaponMountPoint;
    [SerializeField]
    public GameObject AnimatorHostObj;
    [SerializeField]
    public bool UseWeaponIk;
    [SerializeField]
    public bool UseLeftHandIk;
    [SerializeField]
    public bool UseRightHandIk;
    [SerializeField]
    public float CharacterScale;

    // Real Character Stats
    [SerializeField]
    public Stat Level;
    [SerializeField]
    public Stat Experience;
    [SerializeField]
    public Stat XpReward;

    [SerializeField]
    public Stat Health;
    [SerializeField]
    public Stat Armor;
    [SerializeField]
    public Stat Strength;
    [SerializeField]
    public Stat Agility;
    [SerializeField]
    public Stat Dexterity;
    [SerializeField]
    public Stat Endurance;

}

[System.Serializable]
public class Stat
{
    public Stat(float sBase, float min, float max, float perLevel, float actual)
    {
        Base = sBase;
        Actual = actual;
        Min = min;
        Max = max;
        IncreasePerLevel = perLevel;
    }
    [SerializeField]
    public float Base;
    [SerializeField]
    public float Actual;
    [SerializeField]
    public float Min;
    [SerializeField]
    public float Max;
    [SerializeField]
    public float IncreasePerLevel;
}

