using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using NutriMind.Core;
using NutriMind.Data;

namespace NutriMind.Interface
{
    // Plays the mini-game that comes after the lesson AND the final challenge at the end of a mission.
    // Both are a list of "steps" read from the content JSON (miniGame.steps / finalChallenge.steps):
    //   sort      tap a card, then tap the label it belongs to
    //   sequence  tap the cards in the correct order (tap again to undo)
    //   choose    tap the correct option(s)
    //   write     type a short answer (saved for the teacher)
    // If a checkpoint has no steps (for example Science or Wellness content not converted yet),
    // it falls back to showing the instructions with a Continue button, like the old placeholder.
    public class ActivityPanel : PanelBase
    {
        [Header("Existing fields")]
        [SerializeField] TMP_Text titleText;
        [SerializeField] TMP_Text instructionsText;
        [SerializeField] TMP_Text buttonLabel;
        [SerializeField] Button doneButton;          // the one big button: Check / Next / Continue

        [Header("New fields")]
        [SerializeField] TMP_Text feedbackText;
        [SerializeField] RectTransform cardArea;     // big empty area where the cards appear
        [SerializeField] RectTransform bucketArea;   // thin empty row below it for the sorting labels
        [SerializeField] Button cardTemplate;        // an INACTIVE button with a TMP_Text child, copied for every card
        [SerializeField] TMP_InputField writeInput;  // used by "write" steps

        const int MaxTries = 3;     // after this many wrong checks the answer is shown
        const float Gap = 12f;

        static readonly Color CardColor = new Color32(255, 249, 232, 255);
        static readonly Color SelectedColor = new Color32(249, 206, 88, 255);
        static readonly Color CorrectColor = new Color32(140, 217, 115, 255);
        static readonly Color WrongColor = new Color32(242, 128, 115, 255);
        static readonly Color BucketColor = new Color32(216, 244, 186, 255);
        static readonly Color GoodTextColor = new Color32(29, 106, 84, 255);
        static readonly Color BadTextColor = new Color32(179, 64, 47, 255);

        string heading;
        string lastLabel;
        List<ActivityStep> steps;
        int stepIndex;
        ActivityStep step;
        bool placeholder;
        bool solved;
        int attempts;

        // Per-step state. Arrays are indexed by ITEM number (the order in the JSON), not by screen position.
        int[] order;        // screen slot -> item number
        int[] assign;       // sort: chosen bucket per item (-1 = none); sequence: chosen position per item (-1 = none)
        bool[] picked;      // choose: which options are selected
        int[] marks;        // 0 = none, 1 = correct, 2 = wrong
        int selected;       // sort: the card waiting for a label (-1 = none)
        int nextRank;       // sequence: the next position number to hand out

        Button[] cards;
        TMP_Text[] cardTexts;
        Button[] bucketButtons;
        TMP_Text[] bucketTexts;
        readonly List<GameObject> spawned = new List<GameObject>();

        void Awake()
        {
            doneButton.onClick.AddListener(OnMainButton);
            if (cardTemplate != null) cardTemplate.gameObject.SetActive(false);
            if (writeInput != null) writeInput.onValueChanged.AddListener(OnWriteChanged);
        }

        // ---------- Called by CheckpointFlow and WorldController ----------

        public void ShowMiniGame(MiniGameData game)
        {
            Open();
            Begin("Practice: " + Safe(game != null ? game.name : null),
                  game != null ? game.steps : null,
                  game != null ? game.instructions : null,
                  "Continue");
        }

        public void ShowFinal(FinalChallengeData challenge)
        {
            Open();
            Begin("Final Challenge: " + Safe(challenge != null ? challenge.title : null),
                  challenge != null ? challenge.steps : null,
                  challenge != null ? challenge.description : null,
                  "Finish");
        }

        // ---------- Flow ----------

        void Begin(string title, List<ActivityStep> list, string fallbackText, string finishLabel)
        {
            heading = title;
            lastLabel = finishLabel;
            steps = list;
            stepIndex = 0;

            if (steps == null || steps.Count == 0)
            {
                placeholder = true;
                ShowPlaceholder(fallbackText);
                return;
            }

            placeholder = false;
            ShowStep();
        }

        void ShowPlaceholder(string text)
        {
            ClearSpawned();
            titleText.text = heading;
            instructionsText.text = Safe(text) + "\n\n(No interactive steps for this activity yet.)";
            SetFeedback("", true);
            if (cardArea != null) cardArea.gameObject.SetActive(false);
            if (bucketArea != null) bucketArea.gameObject.SetActive(false);
            if (writeInput != null) writeInput.gameObject.SetActive(false);
            if (buttonLabel != null) buttonLabel.text = lastLabel;
            doneButton.interactable = true;
        }

        void ShowStep()
        {
            step = steps[stepIndex];
            ClearSpawned();

            solved = false;
            attempts = 0;
            selected = -1;
            nextRank = 0;

            titleText.text = heading + (steps.Count > 1 ? "  (" + (stepIndex + 1) + "/" + steps.Count + ")" : "");
            instructionsText.text = Safe(step.prompt)
                + (string.IsNullOrEmpty(step.context) ? "" : "\n\n" + step.context);
            SetFeedback("", true);

            bool isWrite = step.type == "write";
            bool isSort = step.type == "sort";
            if (cardArea != null) cardArea.gameObject.SetActive(!isWrite);
            if (bucketArea != null) bucketArea.gameObject.SetActive(isSort);
            if (writeInput != null)
            {
                writeInput.text = "";
                writeInput.gameObject.SetActive(isWrite);
            }

            int n = step.items != null ? step.items.Length : 0;
            assign = new int[n];
            for (int i = 0; i < n; i++) assign[i] = -1;
            picked = new bool[n];
            marks = new int[n];

            if (step.type == "sort" || step.type == "sequence" || step.type == "choose")
            {
                BuildCards();
                if (isSort) BuildBuckets();
            }
            else if (!isWrite)
            {
                Debug.LogWarning("NutriMind: unknown activity step type '" + step.type + "' in " + step.id);
                solved = true;
                SetFeedback("This activity step is not available. Tap Continue.", false);
            }

            Refresh();
        }

        void OnMainButton()
        {
            if (placeholder)
            {
                Close();
                return;
            }
            if (!solved)
            {
                Check();
                return;
            }
            if (stepIndex + 1 < steps.Count)
            {
                stepIndex++;
                ShowStep();
            }
            else
            {
                ClearSpawned();
                Close();
            }
        }

        // ---------- Building the cards ----------

        void BuildCards()
        {
            int n = step.items.Length;
            order = new int[n];
            for (int i = 0; i < n; i++) order[i] = i;
            if (step.type != "choose") Shuffle(order, step.type == "sequence");

            Canvas.ForceUpdateCanvases();
            float w = AreaSize(cardArea, true, 900f);
            float h = AreaSize(cardArea, false, 460f);
            int cols = n > 5 ? 2 : 1;
            int rows = Mathf.CeilToInt(n / (float)cols);
            float cellW = (w - Gap * (cols - 1)) / cols;
            float cellH = Mathf.Clamp((h - Gap * (rows - 1)) / rows, 56f, 110f);

            cards = new Button[n];
            cardTexts = new TMP_Text[n];
            for (int slot = 0; slot < n; slot++)
            {
                int item = order[slot];
                Vector2 pos = new Vector2((slot % cols) * (cellW + Gap), -(slot / cols) * (cellH + Gap));
                Button b = MakeButton(cardArea, new Vector2(cellW, cellH), pos, () => OnCardClicked(item));
                cards[item] = b;
                cardTexts[item] = b.GetComponentInChildren<TMP_Text>(true);
            }
        }

        void BuildBuckets()
        {
            int k = step.buckets.Length;
            Canvas.ForceUpdateCanvases();
            float w = AreaSize(bucketArea, true, 900f);

            // Ensure the labels sit in a spacious HorizontalLayoutGroup
            LayoutGroup other = bucketArea.GetComponent<LayoutGroup>();
            if (other != null && !(other is HorizontalLayoutGroup)) DestroyImmediate(other);

            HorizontalLayoutGroup row = bucketArea.GetComponent<HorizontalLayoutGroup>();
            if (row == null) row = bucketArea.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.spacing = 16f;
            row.padding = new RectOffset(10, 10, 4, 4);
            row.childAlignment = TextAnchor.MiddleCenter;
            row.childControlWidth = true;
            row.childControlHeight = true;
            row.childForceExpandWidth = true;
            row.childForceExpandHeight = true;

            float availableWidth = Mathf.Max(w, 850f);
            float perButtonWidth = (availableWidth - (16f * Mathf.Max(0, k - 1))) / Mathf.Max(1, k);

            bucketButtons = new Button[k];
            bucketTexts = new TMP_Text[k];
            for (int b = 0; b < k; b++)
            {
                int bucket = b;
                Button btn = MakeButton(bucketArea, new Vector2(perButtonWidth, 75f), Vector2.zero, () => OnBucketClicked(bucket));
                if (btn.image != null) btn.image.color = BucketColor;

                LayoutElement element = btn.GetComponent<LayoutElement>();
                if (element == null) element = btn.gameObject.AddComponent<LayoutElement>();
                element.minWidth = 140f;
                element.preferredWidth = perButtonWidth;
                element.minHeight = 65f;
                element.preferredHeight = 75f;
                element.flexibleWidth = 1f;
                element.flexibleHeight = 1f;

                bucketButtons[b] = btn;
                TMP_Text txt = btn.GetComponentInChildren<TMP_Text>(true);
                if (txt != null)
                {
                    txt.enableWordWrapping = true;
                    txt.enableAutoSizing = true;
                    txt.fontSizeMin = 13f;
                    txt.fontSizeMax = 26f;
                    txt.alignment = TextAlignmentOptions.Center;
                    txt.margin = new Vector4(6, 4, 6, 4);
                }
                bucketTexts[b] = txt;
            }
        }

        Button MakeButton(RectTransform parent, Vector2 size, Vector2 position, UnityAction onClick)
        {
            Button b = Instantiate(cardTemplate, parent, false);
            b.gameObject.SetActive(true);
            b.transition = Selectable.Transition.None;
            b.onClick.RemoveAllListeners();
            b.onClick.AddListener(onClick);

            RectTransform rt = (RectTransform)b.transform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.sizeDelta = size;
            rt.anchoredPosition = position;

            TMP_Text t = b.GetComponentInChildren<TMP_Text>(true);
            if (t != null)
            {
                t.enableAutoSizing = true;
                t.fontSizeMin = 16f;
                t.fontSizeMax = 32f;
                t.alignment = TextAlignmentOptions.Center;
            }

            spawned.Add(b.gameObject);
            return b;
        }

        void ClearSpawned()
        {
            for (int i = 0; i < spawned.Count; i++)
            {
                spawned[i].SetActive(false);
                Destroy(spawned[i]);
            }
            spawned.Clear();
            cards = null;
            cardTexts = null;
            bucketButtons = null;
            bucketTexts = null;
        }

        // ---------- Player input ----------

        void OnCardClicked(int item)
        {
            if (solved) return;
            ClearMarks();
            SetFeedback("", true);

            if (step.type == "sort")
            {
                selected = (selected == item) ? -1 : item;
            }
            else if (step.type == "sequence")
            {
                if (assign[item] >= 0)
                {
                    int removed = assign[item];
                    assign[item] = -1;
                    for (int j = 0; j < assign.Length; j++)
                    {
                        if (assign[j] > removed) assign[j]--;
                    }
                    nextRank--;
                }
                else
                {
                    assign[item] = nextRank;
                    nextRank++;
                }
            }
            else if (step.type == "choose")
            {
                ToggleChoice(item);
            }
            Refresh();
        }

        void OnBucketClicked(int bucket)
        {
            if (solved) return;
            if (selected < 0)
            {
                SetFeedback("Tap a card first, then tap the label it belongs to.", false);
                return;
            }
            ClearMarks();
            SetFeedback("", true);
            assign[selected] = bucket;
            selected = -1;
            Refresh();
        }

        void ToggleChoice(int item)
        {
            int need = step.answers.Length;
            if (picked[item])
            {
                picked[item] = false;
            }
            else if (need == 1)
            {
                for (int i = 0; i < picked.Length; i++) picked[i] = false;
                picked[item] = true;
            }
            else if (CountPicked() < need)
            {
                picked[item] = true;
            }
            else
            {
                SetFeedback("You can choose only " + need + ". Tap a chosen card to remove it first.", false);
            }
        }

        void OnWriteChanged(string value)
        {
            if (step != null && step.type == "write" && !solved) UpdateMainButton();
        }

        // ---------- Checking ----------

        void Check()
        {
            if (step.type == "write")
            {
                string text = writeInput != null ? writeInput.text.Trim() : "";
                if (GameSession.Instance != null) GameSession.Instance.RecordWritten(step.id, text);
                solved = true;
                SetFeedback("Saved! Your teacher can read your answer.", true);
                Refresh();
                return;
            }

            int n = step.items.Length;
            int right = 0;
            int total;
            bool allCorrect;

            if (step.type == "choose")
            {
                total = step.answers.Length;
                int pickedCount = 0;
                for (int i = 0; i < n; i++)
                {
                    if (!picked[i]) continue;
                    pickedCount++;
                    if (Contains(step.answers, i))
                    {
                        marks[i] = 1;
                        right++;
                    }
                    else
                    {
                        marks[i] = 2;
                    }
                }
                allCorrect = right == total && pickedCount == right;
            }
            else
            {
                total = n;
                for (int i = 0; i < n; i++)
                {
                    int expected = step.type == "sort" ? step.answers[i] : i;
                    bool ok = assign[i] == expected;
                    marks[i] = ok ? 1 : 2;
                    if (ok) right++;
                }
                allCorrect = right == total;
            }

            if (allCorrect)
            {
                solved = true;
                SetFeedback(string.IsNullOrEmpty(step.success) ? "Great job!" : step.success, true);
            }
            else
            {
                attempts++;
                if (attempts >= MaxTries)
                {
                    RevealAnswer();
                    solved = true;
                    SetFeedback("Good try! Here is the correct answer. Take a look, then continue.", false);
                }
                else if (step.type == "choose" && total == 1)
                {
                    SetFeedback("Not quite. Try another answer.", false);
                }
                else
                {
                    SetFeedback(right + " of " + total + " correct. Fix the red cards and try again.", false);
                }
            }
            Refresh();
        }

        void RevealAnswer()
        {
            int n = step.items.Length;
            if (step.type == "choose")
            {
                for (int i = 0; i < n; i++)
                {
                    picked[i] = Contains(step.answers, i);
                    marks[i] = picked[i] ? 1 : 0;
                }
            }
            else
            {
                for (int i = 0; i < n; i++)
                {
                    assign[i] = step.type == "sort" ? step.answers[i] : i;
                    marks[i] = 1;
                }
                nextRank = n;
            }
            selected = -1;
        }

        // ---------- Drawing ----------

        void Refresh()
        {
            if (cards != null)
            {
                for (int i = 0; i < cards.Length; i++)
                {
                    if (cards[i] == null) continue;
                    if (cardTexts[i] != null) cardTexts[i].text = CardLabel(i);

                    Color color = CardColor;
                    if (marks[i] == 1) color = CorrectColor;
                    else if (marks[i] == 2) color = WrongColor;
                    else if (step.type == "sort" && selected == i) color = SelectedColor;
                    else if (step.type == "choose" && picked[i]) color = SelectedColor;
                    else if (step.type == "sequence" && assign[i] >= 0) color = SelectedColor;
                    if (cards[i].image != null) cards[i].image.color = color;
                }
            }

            if (bucketButtons != null)
            {
                for (int b = 0; b < bucketButtons.Length; b++)
                {
                    int count = 0;
                    for (int i = 0; i < assign.Length; i++)
                    {
                        if (assign[i] == b) count++;
                    }
                    if (bucketTexts[b] != null) bucketTexts[b].text = step.buckets[b] + "  (" + count + ")";
                }
            }

            UpdateMainButton();
        }

        string CardLabel(int item)
        {
            string text = step.items[item];
            if (step.type == "sort" && assign[item] >= 0)
            {
                return text + "\n<size=75%><b>-> " + step.buckets[assign[item]] + "</b></size>";
            }
            if (step.type == "sequence" && assign[item] >= 0)
            {
                return (assign[item] + 1) + ".  " + text;
            }
            return text;
        }

        void UpdateMainButton()
        {
            if (placeholder) return;
            bool last = stepIndex + 1 >= steps.Count;
            if (buttonLabel != null) buttonLabel.text = solved ? (last ? lastLabel : "Next") : "Check";
            doneButton.interactable = solved || IsReady();
        }

        bool IsReady()
        {
            switch (step.type)
            {
                case "sort":
                    for (int i = 0; i < assign.Length; i++)
                    {
                        if (assign[i] < 0) return false;
                    }
                    return assign.Length > 0;
                case "sequence":
                    return assign.Length > 0 && nextRank == assign.Length;
                case "choose":
                    return CountPicked() == step.answers.Length;
                case "write":
                    return writeInput != null && writeInput.text.Trim().Length >= step.minChars;
                default:
                    return true;
            }
        }

        // ---------- Small helpers ----------

        void ClearMarks()
        {
            for (int i = 0; i < marks.Length; i++) marks[i] = 0;
        }

        int CountPicked()
        {
            int count = 0;
            for (int i = 0; i < picked.Length; i++)
            {
                if (picked[i]) count++;
            }
            return count;
        }

        void SetFeedback(string message, bool good)
        {
            if (feedbackText == null) return;
            feedbackText.text = message;
            feedbackText.color = good ? GoodTextColor : BadTextColor;
        }

        static bool Contains(int[] values, int value)
        {
            for (int i = 0; i < values.Length; i++)
            {
                if (values[i] == value) return true;
            }
            return false;
        }

        static float AreaSize(RectTransform area, bool width, float fallback)
        {
            if (area == null) return fallback;
            float size = width ? area.rect.width : area.rect.height;
            return size > 100f ? size : fallback;
        }

        // Random order; for sequences it makes sure the cards do not start already in the right order.
        static void Shuffle(int[] values, bool avoidSorted)
        {
            for (int attempt = 0; attempt < 10; attempt++)
            {
                for (int i = values.Length - 1; i > 0; i--)
                {
                    int j = Random.Range(0, i + 1);
                    int temp = values[i];
                    values[i] = values[j];
                    values[j] = temp;
                }
                if (!avoidSorted || !IsSorted(values)) return;
            }
        }

        static bool IsSorted(int[] values)
        {
            for (int i = 0; i < values.Length; i++)
            {
                if (values[i] != i) return false;
            }
            return true;
        }

        static string Safe(string value)
        {
            return value ?? "";
        }
    }
}