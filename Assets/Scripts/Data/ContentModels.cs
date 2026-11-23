using System;
using System.Collections.Generic;

namespace NutriMind.Data
{
    // One JSON file = one world + one grade (example: ENG_G5.json).
    // JsonUtility needs public fields, [Serializable] classes and Lists/arrays (no Dictionaries).

    [Serializable]
    public class ContentFile
    {
        public string worldId;   // "ENG", "SCI" or "PEH"
        public int grade;        // 5 or 6
        public List<MissionData> missions = new List<MissionData>();
    }

    [Serializable]
    public class MissionData
    {
        public string id;        // "G5-ENG-M1"
        public int number;       // 1..3
        public string title;
        public string story;
        public string focus;
        public FinalChallengeData finalChallenge = new FinalChallengeData();
        public List<CheckpointData> checkpoints = new List<CheckpointData>();
    }

    [Serializable]
    public class FinalChallengeData
    {
        public string title;
        public string description;
        public string rewardName;
        public List<ActivityStep> steps = new List<ActivityStep>();   // the real final challenge
    }

    [Serializable]
    public class CheckpointData
    {
        public string id;        // "G5-ENG-M1-CP1"
        public int index;        // 1..3
        public string title;     // "Story Garden - The Missing Lantern"
        public string npcName;
        public string npcOpening;
        public string reading;   // reading passage (LiteraQuest); can be empty
        public string lesson;
        public MiniGameData miniGame = new MiniGameData();
        public string rewardName;
        public List<QuestionData> questions = new List<QuestionData>();
    }

    [Serializable]
    public class MiniGameData
    {
        public string name;          // "Story Detective"
        public string type;          // informational only; the game plays the steps below
        public string instructions;  // shown only when there are no steps yet
        public List<ActivityStep> steps = new List<ActivityStep>();
    }

    // One screen of a mini-game or final challenge. Which fields are used depends on "type":
    //   sort      items = cards, buckets = labels, answers[i] = correct bucket number of items[i]
    //   sequence  items = cards listed in the CORRECT order (the game shuffles them)
    //   choose    items = options, answers = number(s) of the correct option(s), 0 = first
    //   write     minChars = shortest allowed answer (saved for the teacher)
    [Serializable]
    public class ActivityStep
    {
        public string id;            // "G5-ENG-M2-CP3-A1" (write steps: used as the save key)
        public string type;          // sort | sequence | choose | write
        public string prompt;
        public string context;       // optional text shown under the prompt (sources, survey results ...)
        public string[] items;
        public string[] buckets;
        public int[] answers;
        public int minChars;
        public string success;       // optional message when the step is answered correctly
    }

    [Serializable]
    public class QuestionData
    {
        public string id;            // "G5-ENG-M1-CP1-Q01"
        public string skill;
        public string text;
        public string[] options;     // 4 options, order = A, B, C, D
        public int correctIndex;     // 0 = A, 1 = B, 2 = C, 3 = D
        public string feedback;
        public string explanation;
    }
}