using System.IO;
using UnityEditor;
using UnityEngine;

public static class NutriMindTools
{
    [MenuItem("NutriMind/Reset All Saves")]
    static void ResetAll()
    {
        string folder = Path.Combine(Application.persistentDataPath, "NutriMind");
        if (Directory.Exists(folder))
        {
            Directory.Delete(folder, true);
            Debug.Log("NutriMind: all saves deleted.");
        }
        else Debug.Log("NutriMind: no saves found.");
    }
    [MenuItem("NutriMind/Open Save Folder")]
    static void OpenFolder()
    {
        string folder = Path.Combine(Application.persistentDataPath, "NutriMind");
        Directory.CreateDirectory(folder);
        EditorUtility.RevealInFinder(folder);
    }
}
