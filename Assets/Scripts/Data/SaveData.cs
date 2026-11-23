using System;
using System.Collections.Generic;

namespace NutriMind.Data
{
    // What gets saved per student (matches "Appendix A" in your content documents).

    [Serializable]
    public class QuestionResponse
    {
        public string questionId;
        public int selectedIndex;
        public bool correct;
    }

    [Serializable]
    public class CheckpointProgress
    {
        public string checkpointId;
        public bool completed;
        public int correctCount;
        public int pointsEarned;
        public string rewardName;
        public List<QuestionResponse> responses = new List<QuestionResponse>();
    }

    // A short answer typed in a "write" activity step; kept so the teacher can read it later.
    [Serializable]
    public class WrittenResponse
    {
        public string stepId;
        public string text;
    }

    [Serializable]
    public class QuizAttemptRecord
    {
        public string quizId;
        public int score;
        public int totalQuestions;
        public int percentage;
        public bool passed;
        public string timestamp;
    }

    [Serializable]
    public class MissionProgress
    {
        public string missionId;
        public bool finalChallengeComplete;
        public List<CheckpointProgress> checkpoints = new List<CheckpointProgress>();
        public List<WrittenResponse> writtenResponses = new List<WrittenResponse>();

        public CheckpointProgress GetCheckpoint(string checkpointId, bool createIfMissing)
        {
            for (int i = 0; i < checkpoints.Count; i++)
            {
                if (checkpoints[i].checkpointId == checkpointId) return checkpoints[i];
            }
            if (!createIfMissing) return null;

            var created = new CheckpointProgress { checkpointId = checkpointId };
            checkpoints.Add(created);
            return created;
        }
    }

    [Serializable]
    public class StudentSave
    {
        public string studentId;
        public int grade;
        public int totalPoints;
        public List<string> collectedRewards = new List<string>();
        public List<MissionProgress> missions = new List<MissionProgress>();
        public List<QuizAttemptRecord> quizHistory = new List<QuizAttemptRecord>();
        public int totalQuizzesTaken;
        public int totalQuizzesPassed;
        public int highestQuizScore;
        public string lastSavedAt;
        public bool needsSync = true;   // set true on every save; your sync code sets it false after upload

        public MissionProgress GetMission(string missionId, bool createIfMissing)
        {
            for (int i = 0; i < missions.Count; i++)
            {
                if (missions[i].missionId == missionId) return missions[i];
            }
            if (!createIfMissing) return null;

            var created = new MissionProgress { missionId = missionId };
            missions.Add(created);
            return created;
        }
    }
}