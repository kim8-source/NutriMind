using UnityEngine;

namespace NutriMind.Core
{
    // Create via: Assets > Create > NutriMind > Game Config
    // Save it at Assets/_NutriMind/Resources/GameConfig.asset (the name and the Resources folder matter).
    [CreateAssetMenu(fileName = "GameConfig", menuName = "NutriMind/Game Config")]
    public class GameConfig : ScriptableObject
    {
        [Header("Scoring (placeholders - your documents say teachers decide the real values)")]
        [Tooltip("5 questions x 10 = 50 points per checkpoint, matching the '+50 Points' in your mockup.")]
        public int pointsPerCorrectAnswer = 10;
        [Tooltip("Matches the '+100 Points' in your mission-complete mockup.")]
        public int missionCompletePoints = 100;

        [Header("Editor testing (used only when nobody has logged in)")]
        public string debugStudentId = "debug-student";
        [Range(5, 6)] public int debugGrade = 5;
    }
}
