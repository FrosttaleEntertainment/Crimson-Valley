using UnityEditor;
using UnityEngine;

public class EditorUtils : Editor
{
    //
    // Try not to rename this stuff.
    // This keeps a memory of which foldouts are opened/closed.
    // Add a new bool for each new foldout required.
    //

    // Subject.cs Foldout memory
    public static bool EntityGeneral { get { return EditorPrefs.GetBool("EntityGeneral"); } set { EditorPrefs.SetBool("EntityGeneral", value); } }
    public static bool EntityStats { get { return EditorPrefs.GetBool("EntityStats"); } set { EditorPrefs.SetBool("EntityStats", value); } }
    public static bool EntityWeaponData { get { return EditorPrefs.GetBool("EntityWeaponData"); } set { EditorPrefs.SetBool("EntityWeaponData", value); } }
    public static bool EntityIk { get { return EditorPrefs.GetBool("EntityIk"); } set { EditorPrefs.SetBool("EntityIk", value); } }
    public static bool EntityControls { get { return EditorPrefs.GetBool("EntityControls"); } set { EditorPrefs.SetBool("EntityControls", value); } }

    // Spawner.cs Foldout memory
    public static bool SpawnerBasic { get { return EditorPrefs.GetBool("SpawnerBasic"); } set { EditorPrefs.SetBool("SpawnerBasic", value); } }
    public static bool SpawnerPrefabs { get { return EditorPrefs.GetBool("SpawnerPrefabs"); } set { EditorPrefs.SetBool("SpawnerPrefabs", value); } }
    public static bool SpawnerPoints { get { return EditorPrefs.GetBool("SpawnerPoints"); } set { EditorPrefs.SetBool("SpawnerPoints", value); } }

    // Weapon.cs Foldout memory
    public static bool WeaponStats { get { return EditorPrefs.GetBool("WeaponStats"); } set { EditorPrefs.SetBool("WeaponStats", value); } }
    public static bool WeaponSoundsAndTiming { get { return EditorPrefs.GetBool("WeaponSoundsAndTiming"); } set { EditorPrefs.SetBool("WeaponSoundsAndTiming", value); } }
    public static bool WeaponAttacks { get { return EditorPrefs.GetBool("WeaponAttacks"); } set { EditorPrefs.SetBool("WeaponAttacks", value); } }
    public static bool WeaponSpawns { get { return EditorPrefs.GetBool("WeaponSpawns"); } set { EditorPrefs.SetBool("WeaponSpawns", value); } }
    public static bool WeaponAmmo { get { return EditorPrefs.GetBool("WeaponAmmo"); } set { EditorPrefs.SetBool("WeaponAmmo", value); } }
    public static bool WeaponIk { get { return EditorPrefs.GetBool("WeaponIk"); } set { EditorPrefs.SetBool("WeaponIk", value); } }
    public static bool WeaponImpactTags { get { return EditorPrefs.GetBool("WeaponImpactTags"); } set { EditorPrefs.SetBool("WeaponImpactTags", value); } }


    public static void AddBlackLine()
    {
        GUI.color = Color.black;
        GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1));
        GUI.color = Color.white;
    }
}