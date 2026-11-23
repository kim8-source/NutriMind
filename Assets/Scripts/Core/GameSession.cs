using System;
using System.Collections.Generic;
using UnityEngine;
using NutriMind.Data;

namespace NutriMind.Core
{
    // The one object that survives scene changes. It is created automatically,
    // so you can press Play in ANY scene while testing (it signs in a debug student).
    public class GameSession : MonoBehaviour
    {
        public static GameSession Instance { get; private set; }

        public GameConfig Config { get; private set; }
        public string StudentId { get; private set; }
        public int Grade { get; private set; }
        public StudentSave Save { get; private set; }

        // Set by the menus before loading a world scene.
        public string SelectedWorldId = "ENG";
        public int SelectedMissionNumber = 1;

        public event Action ProgressChanged;

        public bool IsSignedIn
        {
            get { return Save != null; }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Bootstrap()
        {
            if (Instance != null) return;
            new GameObject("GameSession").AddComponent<GameSession>();
        }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            Config = Resources.Load<GameConfig>("GameConfig");

            if (Config == null)
            {
                Debug.LogWarning("NutriMind: GameConfig asset NOT found in a Resources folder. Using built-in defaults (Grade 5).");
                Config = ScriptableObject.CreateInstance<GameConfig>();
            }

            Debug.Log("NutriMind: debugGrade = " + Config.debugGrade);

            if (!IsSignedIn) SignIn(Config.debugStudentId, Config.debugGrade);
        }

        // ---------- Login / logout (call these from your Login scene) ----------

        public void SignIn(string studentId, int grade)
        {
            StudentId = studentId;
            Grade = grade;
            Save = SaveSystem.Load(studentId, grade);
            Save.grade = grade;
            RaiseProgressChanged();
        }

        public void SignOut()
        {
            Save = null;
            StudentId = null;
            Grade = 0;
        }

        // ---------- Content helpers ----------

        public MissionData CurrentMission
        {
            get { return ContentDatabase.GetMission(SelectedWorldId, Grade, SelectedMissionNumber); }
        }

        public static string MissionId(string worldId, int grade, int number)
        {
            return "G" + grade + "-" + worldId + "-M" + number;
        }

        // ---------- Unlock rules ----------

        public bool IsMissionUnlocked(string worldId, int number)
        {
            if (number <= 1) return true;
            MissionProgress previous = Save.GetMission(MissionId(worldId, Grade, number - 1), false);
            return previous != null && previous.finalChallengeComplete;
        }

        public bool IsCheckpointCompleted(MissionData mission, int index)
        {
            CheckpointData data = mission.checkpoints.Find(c => c.index == index);
            if (data == null) return false;

            MissionProgress mp = Save.GetMission(mission.id, false);
            if (mp == null) return false;

            CheckpointProgress cp = mp.GetCheckpoint(data.id, false);
            return cp != null && cp.completed;
        }

        public bool IsCheckpointUnlocked(MissionData mission, int index)
        {
            return index <= 1 || IsCheckpointCompleted(mission, index - 1);
        }

        public int CompletedCheckpointCount(MissionData mission)
        {
            int count = 0;
            for (int i = 0; i < mission.checkpoints.Count; i++)
            {
                if (IsCheckpointCompleted(mission, mission.checkpoints[i].index)) count++;
            }
            return count;
        }

        public bool IsFinalChallengeComplete(MissionData mission)
        {
            MissionProgress mp = Save.GetMission(mission.id, false);
            return mp != null && mp.finalChallengeComplete;
        }

        // ---------- Saving progress ----------

        // Returns the NEW points awarded (0 when replaying with no better score).
        public int CompleteCheckpoint(MissionData mission, CheckpointData checkpoint, List<QuestionResponse> responses)
        {
            int correct = 0;
            for (int i = 0; i < responses.Count; i++)
            {
                if (responses[i].correct) correct++;
            }
            int points = correct * Config.pointsPerCorrectAnswer;

            MissionProgress mp = Save.GetMission(mission.id, true);
            CheckpointProgress cp = mp.GetCheckpoint(checkpoint.id, true);

            int newPoints = 0;
            if (!cp.completed)
            {
                cp.completed = true;
                cp.correctCount = correct;
                cp.pointsEarned = points;
                cp.rewardName = checkpoint.rewardName;
                cp.responses = responses;
                Save.totalPoints += points;
                newPoints = points;
                AddReward(checkpoint.rewardName);
            }
            else if (points > cp.pointsEarned)
            {
                // Replaying never removes progress; it only improves the best score.
                newPoints = points - cp.pointsEarned;
                cp.correctCount = correct;
                cp.pointsEarned = points;
                cp.responses = responses;
                Save.totalPoints += newPoints;
            }

            Commit();
            return newPoints;
        }

        // Saves a typed answer from a "write" step so the teacher can read it later.
        // Replaces the earlier answer for the same step (e.g. when a mission is replayed).
        public void RecordWritten(string stepId, string text)
        {
            MissionData mission = CurrentMission;
            if (mission == null || Save == null) return;

            MissionProgress mp = Save.GetMission(mission.id, true);
            WrittenResponse found = mp.writtenResponses.Find(w => w.stepId == stepId);
            if (found == null) mp.writtenResponses.Add(new WrittenResponse { stepId = stepId, text = text });
            else found.text = text;
            Commit();
        }

        public int CompleteFinalChallenge(MissionData mission)
        {
            MissionProgress mp = Save.GetMission(mission.id, true);
            int awarded = 0;
            if (!mp.finalChallengeComplete)
            {
                mp.finalChallengeComplete = true;
                awarded = Config.missionCompletePoints;
                Save.totalPoints += awarded;
                AddReward(mission.finalChallenge.rewardName);
            }
            Commit();
            return awarded;
        }

        public void RecordQuizResult(string quizId, int score, int totalQuestions, int pointsEarned)
        {
            if (!IsSignedIn) return;

            int pct = totalQuestions > 0 ? Mathf.RoundToInt((float)score / totalQuestions * 100f) : 0;
            bool passed = pct >= 75;

            var record = new QuizAttemptRecord
            {
                quizId = quizId,
                score = score,
                totalQuestions = totalQuestions,
                percentage = pct,
                passed = passed,
                timestamp = DateTime.UtcNow.ToString("o")
            };

            Save.quizHistory.Add(record);
            Save.totalQuizzesTaken++;
            if (passed) Save.totalQuizzesPassed++;
            if (score > Save.highestQuizScore) Save.highestQuizScore = score;

            if (pointsEarned > 0)
            {
                Save.totalPoints += pointsEarned;
            }

            Commit();
        }

        public int GetStudentLevel()
        {
            if (!IsSignedIn) return 1;
            // Level formula: Level 1 at 0 pts, Level 2 at 100 pts, Level 3 at 250 pts, Level 4 at 500 pts, etc.
            int pts = Save.totalPoints;
            if (pts >= 1000) return 5;
            if (pts >= 500) return 4;
            if (pts >= 250) return 3;
            if (pts >= 100) return 2;
            return 1;
        }

        public string GetStudentRankTitle()
        {
            int lvl = GetStudentLevel();
            switch (lvl)
            {
                case 1: return "Apprentice Learner";
                case 2: return "Curious Explorer";
                case 3: return "Knowledge Seeker";
                case 4: return "Honor Scholar";
                case 5: return "Master Scholar";
                default: return "NutriMind Scholar";
            }
        }

        public int GetCompletedMissionsCount()
        {
            if (!IsSignedIn) return 0;
            int count = 0;
            for (int i = 0; i < Save.missions.Count; i++)
            {
                if (Save.missions[i].finalChallengeComplete) count++;
            }
            return count;
        }

        public int GetTotalCompletedCheckpointsCount()
        {
            if (!IsSignedIn) return 0;
            int count = 0;
            for (int i = 0; i < Save.missions.Count; i++)
            {
                for (int j = 0; j < Save.missions[i].checkpoints.Count; j++)
                {
                    if (Save.missions[i].checkpoints[j].completed) count++;
                }
            }
            return count;
        }

        void AddReward(string rewardName)
        {
            if (string.IsNullOrEmpty(rewardName)) return;
            if (!Save.collectedRewards.Contains(rewardName)) Save.collectedRewards.Add(rewardName);
        }

        void Commit()
        {
            SaveSystem.Save(Save);
            RaiseProgressChanged();
        }

        void RaiseProgressChanged()
        {
            if (ProgressChanged != null) ProgressChanged();
        }
    }
}
