using System.Collections.Generic;
using UnityEngine;
using NutriMind.Data;

namespace NutriMind.Core
{
    // Loads Resources/Content/<WORLD>_G<GRADE>.json  (example: ENG_G5.json)
    // Because the file name includes the grade, a Grade 5 student can never load Grade 6 content.
    public static class ContentDatabase
    {
        static readonly Dictionary<string, ContentFile> cache = new Dictionary<string, ContentFile>();

        public static ContentFile Get(string worldId, int grade)
        {
            Debug.Log("NutriMind: loading " + worldId + "_G" + grade);
            
            string key = worldId + "_G" + grade;

            ContentFile cached;
            if (cache.TryGetValue(key, out cached)) return cached;

            TextAsset asset = Resources.Load<TextAsset>("Content/" + key);
            if (asset == null)
            {
                Debug.LogError("NutriMind: missing content file Resources/Content/" + key + ".json");
                return null;
            }

            ContentFile file = JsonUtility.FromJson<ContentFile>(asset.text);
            cache[key] = file;
            return file;
        }

        public static MissionData GetMission(string worldId, int grade, int missionNumber)
        {
            ContentFile file = Get(worldId, grade);
            if (file == null) return null;

            for (int i = 0; i < file.missions.Count; i++)
            {
                if (file.missions[i].number == missionNumber) return file.missions[i];
            }
            Debug.LogError("NutriMind: mission " + missionNumber + " not found in " + worldId + "_G" + grade);
            return null;
        }

        public static void ClearCache()
        {
            cache.Clear();
        }
    }
}
