using UnityEditor;
using UnityEngine;

public class ClearPlayerPrefs
{
    [MenuItem("Tools/Clear PlayerPrefs %#d")] // Ctrl/Cmd + Shift + D
    public static void ClearAllPlayerPrefs()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        Debug.Log("PlayerPrefs cleared!");
    }
}