using System;
using System.IO;
using UnityEngine;
using NutriMind.Data;

namespace NutriMind.Core
{
    // Saves one JSON file per student in Application.persistentDataPath/NutriMind.
    // Works fully offline. Later, a sync script uploads files where needsSync == true.
    public static class SaveSystem
    {
        static string Folder
        {
            get { return Path.Combine(Application.persistentDataPath, "NutriMind"); }
        }

        static string PathFor(string studentId)
        {
            string safe = studentId;
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                safe = safe.Replace(c, '_');
            }
            return Path.Combine(Folder, "save_" + safe + ".json");
        }

        public static StudentSave Load(string studentId, int grade)
        {
            try
            {
                string path = PathFor(studentId);
                if (File.Exists(path))
                {
                    var loaded = JsonUtility.FromJson<StudentSave>(File.ReadAllText(path));
                    if (loaded != null && loaded.studentId == studentId) return loaded;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("NutriMind: could not read save, starting fresh. " + e.Message);
            }
            return new StudentSave { studentId = studentId, grade = grade };
        }

        public static void Save(StudentSave save)
        {
            try
            {
                Directory.CreateDirectory(Folder);
                save.lastSavedAt = DateTime.UtcNow.ToString("o");
                save.needsSync = true;

                string path = PathFor(save.studentId);
                string temp = path + ".tmp";
                File.WriteAllText(temp, JsonUtility.ToJson(save, true));
                if (File.Exists(path)) File.Delete(path);
                File.Move(temp, path);
            }
            catch (Exception e)
            {
                Debug.LogError("NutriMind: SAVE FAILED. " + e.Message);
            }
        }

        // Handy while testing: Debug.Log(SaveSystem.DebugPath("debug-student"));
        public static string DebugPath(string studentId)
        {
            return PathFor(studentId);
        }
    }
}
