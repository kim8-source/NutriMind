using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using NutriMind.Data;

namespace NutriMind.Interface
{
    // Four-option question panel with instant feedback (your "QuizQuestionPanel" + "FeedbackPanel" in one).
    // One attempt per question; the correct answer is revealed with the feedback text from your documents.
    public class QuizPanel : PanelBase
    {
        [SerializeField] TMP_Text progressText;      // "Question 3 of 5"
        [SerializeField] TMP_Text questionText;
        [SerializeField] Button[] optionButtons;     // 4 buttons, in order A B C D
        [SerializeField] TMP_Text[] optionLabels;    // the 4 button labels, same order
        [SerializeField] GameObject feedbackRoot;    // hidden until an answer is chosen
        [SerializeField] TMP_Text feedbackText;
        [SerializeField] TMP_Text nextLabel;
        [SerializeField] Button nextButton;

        [Header("Answer colours")]
        [SerializeField] Color correctColor = new Color(0.55f, 0.85f, 0.45f);
        [SerializeField] Color wrongColor = new Color(0.95f, 0.50f, 0.45f);

        public List<QuestionResponse> Responses { get; private set; }
        public int CorrectCount { get; private set; }

        IList<QuestionData> questions;
        int index;
        bool answered;
        Color[] normalColors;

        void Awake()
        {
            Responses = new List<QuestionResponse>();
            normalColors = new Color[optionButtons.Length];

            for (int i = 0; i < optionButtons.Length; i++)
            {
                int captured = i;
                normalColors[i] = optionButtons[i].targetGraphic.color;
                optionButtons[i].onClick.AddListener(() => Choose(captured));
            }
            nextButton.onClick.AddListener(Next);
        }

        public void Show(IList<QuestionData> items)
        {
            Open();
            questions = items;
            index = 0;
            CorrectCount = 0;
            Responses = new List<QuestionResponse>();
            ShowQuestion();
        }

        void ShowQuestion()
        {
            QuestionData q = questions[index];
            answered = false;

            progressText.text = "Question " + (index + 1) + " of " + questions.Count;
            questionText.text = q.text;
            feedbackRoot.SetActive(false);

            for (int i = 0; i < optionButtons.Length; i++)
            {
                bool hasOption = q.options != null && i < q.options.Length;
                optionButtons[i].gameObject.SetActive(hasOption);
                if (!hasOption) continue;

                optionLabels[i].text = ((char)('A' + i)).ToString() + ". " + q.options[i];
                optionButtons[i].targetGraphic.color = normalColors[i];
            }
        }

        void Choose(int chosen)
        {
            if (answered) return;
            answered = true;

            QuestionData q = questions[index];
            bool correct = chosen == q.correctIndex;
            if (correct) CorrectCount++;

            Responses.Add(new QuestionResponse
            {
                questionId = q.id,
                selectedIndex = chosen,
                correct = correct
            });

            if (q.correctIndex >= 0 && q.correctIndex < optionButtons.Length)
            {
                optionButtons[q.correctIndex].targetGraphic.color = correctColor;
            }
            if (!correct) optionButtons[chosen].targetGraphic.color = wrongColor;

            string fb = !string.IsNullOrEmpty(q.feedback) ? q.feedback : q.explanation;
            feedbackText.text = (correct ? "Correct! " : "Not quite. ") + fb;
            nextLabel.text = index >= questions.Count - 1 ? "Finish" : "Next";
            feedbackRoot.SetActive(true);
        }

        void Next()
        {
            index++;
            if (index >= questions.Count) Close();
            else ShowQuestion();
        }
    }
}
