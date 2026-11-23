using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NutriMind.Core;
using NutriMind.Data;
using NutriMind.Interface;

namespace NutriMind.Gameplay
{
    // Runs one checkpoint from your Game Loop:
    // NPC talks -> Lesson -> Mini-game -> 5 questions with feedback -> Reward -> Save.
    // Every panel is a reusable prefab; only the content changes between missions and grades.
    public class CheckpointFlow : MonoBehaviour
    {
        [SerializeField] NPCDialoguePanel dialoguePanel;
        [SerializeField] LessonPanel lessonPanel;
        [SerializeField] ActivityPanel activityPanel;
        [SerializeField] QuizPanel quizPanel;
        [SerializeField] RewardPanel rewardPanel;

        public IEnumerator Run(MissionData mission, CheckpointData checkpoint)
        {
            // 1. NPC dialogue (opening line + reading passage when the checkpoint has one)
            var lines = new List<string>();
            if (!string.IsNullOrEmpty(checkpoint.npcOpening)) lines.Add(checkpoint.npcOpening);
            if (!string.IsNullOrEmpty(checkpoint.reading)) lines.Add(checkpoint.reading);
            if (lines.Count == 0) lines.Add("Let's begin!");

            dialoguePanel.Show(checkpoint.npcName, lines);
            yield return new WaitUntil(() => dialoguePanel.IsDone);

            // 2. Lesson
            lessonPanel.Show(checkpoint.title, checkpoint.lesson);
            yield return new WaitUntil(() => lessonPanel.IsDone);

            // 3. Mini-game (placeholder until you build the real ones, Step 9 in the guide)
            activityPanel.ShowMiniGame(checkpoint.miniGame);
            yield return new WaitUntil(() => activityPanel.IsDone);

            // 4. Questions
            quizPanel.Show(checkpoint.questions);
            yield return new WaitUntil(() => quizPanel.IsDone);

            // 5. Save, then show the reward
            var responses = new List<QuestionResponse>(quizPanel.Responses);
            int points = GameSession.Instance.CompleteCheckpoint(mission, checkpoint, responses);

            rewardPanel.Show("Checkpoint Complete!", checkpoint.rewardName, points, "Continue");
            yield return new WaitUntil(() => rewardPanel.IsDone);
        }
    }
}
