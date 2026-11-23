namespace NutriMind.Core
{
    // The three learning worlds. Change the display names here once you decide on them:
    // your Game Structure file says "Science World" and "Wellness World",
    // while your content files say "SciVenture" and "HealthQuest".
    public static class WorldCatalog
    {
        public const string Eng = "ENG";
        public const string Sci = "SCI";
        public const string Peh = "PEH";

        public struct Info
        {
            public string id;          // used in JSON file names and IDs
            public string displayName; // shown to students
            public string sceneName;   // the 3D world scene

            public Info(string id, string displayName, string sceneName)
            {
                this.id = id;
                this.displayName = displayName;
                this.sceneName = sceneName;
            }
        }

        public static readonly Info[] All =
        {
            new Info("ENG", "LiteraQuest (English)",     "World_LiteraQuest"),
            new Info("SCI", "SciVenture (Science)",      "World_Science"),
            new Info("PEH", "HealthQuest (PE & Health)", "World_Wellness"),
        };

        public static bool TryGet(string id, out Info info)
        {
            for (int i = 0; i < All.Length; i++)
            {
                if (All[i].id == id)
                {
                    info = All[i];
                    return true;
                }
            }
            info = default(Info);
            return false;
        }
    }
}
