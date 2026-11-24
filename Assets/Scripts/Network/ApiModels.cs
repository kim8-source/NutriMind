using System;
using System.Collections.Generic;

namespace NutriMind.Network
{
    // =====================================================================================
    //  The JSON the Laravel game API sends and receives, as classes Unity's JsonUtility can read.
    //
    //  Field names must match the JSON keys EXACTLY (snake_case), so they look different from normal C#.
    //  JsonUtility turns a JSON null number into 0 and a null list into an empty list. Comments below say
    //  where that matters.
    //
    //  Backend source of truth: web/NutriMind/app/Http/Resources/Game/*.php
    // =====================================================================================

    // ---------- Shared ----------

    // Every error response has a "message" (for validation errors it is the first error, ready to show).
    [Serializable]
    public class ApiErrorResponse
    {
        public string message;
    }

    // ---------- Profile (login, /me, sync, quiz submit) ----------

    [Serializable]
    public class ProfileData
    {
        public int id;                 // the learner's user ID; the game uses it as GameSession.StudentId
        public string name;
        public string username;
        public int grade;              // 5 or 6 (0 if the web has no learner profile for this account)
        public int total_points;       // the SERVER's points; show these instead of the local save's points
        public int level;
        public int max_level;
        public LevelProgressData level_progress;
        public List<RewardData> rewards = new List<RewardData>();
        public string last_synced_at;  // ISO date, empty if never synced
    }

    [Serializable]
    public class LevelProgressData
    {
        public int points_into_level;
        public int points_per_level;   // 0 at the maximum level
        public float percent;          // 0-100, for an XP bar
    }

    [Serializable]
    public class RewardData
    {
        public string name;
        public string description;
        public string awarded_at;
    }

    [Serializable]
    public class ProfileResponse
    {
        public ProfileData data;
    }

    // ---------- Login ----------

    [Serializable]
    public class LoginRequest
    {
        public string username;        // the username OR the 12-digit LRN (dashes/spaces allowed)
        public string password;
        public string device_name;
    }

    [Serializable]
    public class LoginResponse
    {
        public ProfileData data;
        public string token;           // send as "Authorization: Bearer <token>" (ApiClient does this)
        public string token_type;
    }

    // ---------- Progress sync ----------
    // The request body is the game's own save file (NutriMind.Data.StudentSave) sent unchanged.

    [Serializable]
    public class SyncResponse
    {
        public ProfileData data;
        public SyncIgnoredData ignored;
    }

    // What the server could not match. Admins fix these on the web (Missions > "Game mission ID", Rewards > name).
    [Serializable]
    public class SyncIgnoredData
    {
        public List<string> missions = new List<string>();
        public List<string> rewards = new List<string>();
    }

    // ---------- Quizzes (teachers' quizzes) ----------

    [Serializable]
    public class QuizListResponse
    {
        public List<ServerQuiz> data = new List<ServerQuiz>();
    }

    [Serializable]
    public class QuizDetailResponse
    {
        public ServerQuiz data;
    }

    [Serializable]
    public class ServerQuiz
    {
        public int id;
        public string title;
        public string instructions;
        public SubjectData subject;
        public int grade;
        public int time_limit_minutes;   // 0 = no time limit set by the teacher
        public int passing_score;        // percent
        public int max_attempts;         // 0 = unlimited attempts (the server sends null)
        public int attempts_used;
        public int attempts_left;        // only meaningful when max_attempts > 0
        public int question_count;
        public string due_at;            // ISO date or empty
        public bool show_correct_answers;
        public List<ServerQuestion> questions = new List<ServerQuestion>();   // only in the detail response

        public bool HasUnlimitedAttempts
        {
            get { return max_attempts <= 0; }
        }

        public bool CanStartAttempt
        {
            get { return HasUnlimitedAttempts || attempts_left > 0; }
        }
    }

    [Serializable]
    public class SubjectData
    {
        public string name;
        public string code;
    }

    // A question WITHOUT its correct answer (the server grades the attempt).
    [Serializable]
    public class ServerQuestion
    {
        public int id;
        public string type;              // "multiple_choice", "true_false" or "identification"
        public string prompt;
        public List<string> choices = new List<string>();   // empty for identification (typed answer)
        public int points;
        public string image_url;
        public string video_url;

        public bool IsTypedAnswer
        {
            get { return type == "identification"; }
        }
    }

    // ---------- Quiz attempt (submit answers) ----------

    [Serializable]
    public class QuizAttemptRequest
    {
        public List<QuizAnswer> answers = new List<QuizAnswer>();   // leave out unanswered questions
        public string started_at;        // ISO 8601, e.g. DateTime.UtcNow.ToString("o")
        public int duration_seconds;
        public bool is_offline;
    }

    [Serializable]
    public class QuizAnswer
    {
        public int question_id;
        public string answer;            // choice index as text ("0" = first choice) or the typed answer
    }

    [Serializable]
    public class QuizAttemptResponse
    {
        public QuizAttemptData data;
        public ProfileData student;      // the learner's updated points and level
    }

    [Serializable]
    public class QuizAttemptData
    {
        public int id;
        public int quiz_id;
        public int attempt_number;
        public int attempts_left;        // only meaningful when the quiz has a maximum
        public int score;                // points earned (questions can be worth more than 1 point)
        public int max_score;
        public float percentage;
        public bool passed;
        public string submitted_at;
        public int duration_seconds;
        public List<QuizResultData> results = new List<QuizResultData>();
    }

    [Serializable]
    public class QuizResultData
    {
        public int question_id;
        public string answer;
        public bool is_correct;
        public int points_awarded;

        // Only sent when the teacher allows learners to see correct answers (show_correct_answers).
        // correct_answer_text is empty when hidden; correct_choice is the index of the right choice.
        public int correct_choice;
        public string correct_answer_text;
        public string explanation;

        public bool HasCorrectAnswer
        {
            get { return !string.IsNullOrEmpty(correct_answer_text); }
        }
    }
}
