using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;
using NutriMind.Core;
using NutriMind.Data;

namespace NutriMind.Interface
{
    // Full offline online quiz controller supporting:
    // 1. Panel_QuizList: View and select available quizzes/assessments
    // 2. Panel_QuizDetails: View quiz coverage, total items, time limit
    // 3. Panel_QuizPlay: Interactive multiple-choice questions with toggles & timer
    // 4. Panel_QuizSummary: Pre-submission overview (answered items, time, review vs submit)
    // 5. Panel_QuizResult: Score summary, passing badge, review answers mode, and retry
    public class OnlineQuizController : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] GameObject panelList;
        [SerializeField] GameObject panelDetails;
        [SerializeField] GameObject panelPlay;
        [SerializeField] GameObject panelSummary;
        [SerializeField] GameObject panelResult;
        [SerializeField] GameObject panelOffline;

        [Header("List Screen")]
        [SerializeField] Transform listContent;
        [SerializeField] Button listBackButton;

        [Header("Details Screen")]
        [SerializeField] TMP_Text coverageText;
        [SerializeField] TMP_Text totalItemsText;
        [SerializeField] TMP_Text timeLimitText;
        [SerializeField] Button startButton;
        [SerializeField] Button detailsBackButton;

        [Header("Play Screen")]
        [SerializeField] TMP_Text questionText;
        [SerializeField] TMP_Text counterText;
        [SerializeField] TMP_Text timerText;
        [SerializeField] ToggleGroup optionsToggleGroup;
        [SerializeField] Toggle[] optionToggles;
        [SerializeField] TMP_Text[] optionTexts;
        [SerializeField] Button previousButton;
        [SerializeField] Button nextButton;
        [SerializeField] TMP_Text nextButtonText;
        [SerializeField] Image questionImage;
        [SerializeField] VideoPlayer questionVideoPlayer;

        [Header("Summary Screen")]
        [SerializeField] TMP_Text summaryAnsweredText;
        [SerializeField] TMP_Text summaryTimeUsedText;
        [SerializeField] Button summarySubmitButton;
        [SerializeField] Button summaryReviewButton;

        [Header("Result Screen")]
        [SerializeField] TMP_Text scoreText;
        [SerializeField] TMP_Text percentText;
        [SerializeField] TMP_Text resultTimeUsedText;
        [SerializeField] TMP_Text passedText;
        [SerializeField] GameObject passedBadge;
        [SerializeField] Button retryButton;
        [SerializeField] Button reviewButton;
        [SerializeField] Button backToListButton;
        [SerializeField] Button resultBackButton;

        [Header("Colours")]
        [SerializeField] Color normalOptionColor = new Color(1f, 1f, 1f, 1f);
        [Tooltip("Selected and correct option color (#93ff7f)")]
        [SerializeField] Color selectedOptionColor = new Color(0.5765f, 1f, 0.498f, 1f); // #93ff7f
        [Tooltip("Correct option color in review mode (#93ff7f)")]
        [SerializeField] Color correctColor = new Color(0.5765f, 1f, 0.498f, 1f); // #93ff7f
        [Tooltip("Wrong option color (#ff8f80)")]
        [SerializeField] Color wrongColor = new Color(1f, 0.5608f, 0.502f, 1f); // #ff8f80

        const int QuizQuestionCount = 10;
        const float DefaultTimeLimitSeconds = 600f; // 10 minutes

        [System.Serializable]
        public struct QuizDef
        {
            public string id;
            public string title;
            public string subject;
            public string coverage;
            public int questionCount;
            public float timeLimitSeconds;
        }

        List<QuizDef> availableQuizzes = new List<QuizDef>();
        QuizDef currentQuizDef;

        List<QuestionData> questions = new List<QuestionData>();
        int[] userSelectedAnswers;
        int currentIndex;
        float timeRemaining;
        float timeSpent;
        bool isQuizActive;
        bool isReviewMode;
        bool updatingToggles;

        void Awake()
        {
            AutoWireElements();
            BindEvents();
        }

        void Start()
        {
            // Enter directly at the Quiz List screen first
            ShowListScreen();
        }

        void AutoWireElements()
        {
            // Find panels (including inactive ones in the scene)
            if (panelList == null) panelList = FindPanelInScene("Panel_QuizList");
            if (panelDetails == null) panelDetails = FindPanelInScene("Panel_QuizDetails");
            if (panelPlay == null) panelPlay = FindPanelInScene("Panel_QuizPlay");
            if (panelSummary == null) panelSummary = FindPanelInScene("Panel_QuizSummary");
            if (panelResult == null) panelResult = FindPanelInScene("Panel_QuizResult");
            if (panelOffline == null) panelOffline = FindPanelInScene("Panel_Offline");

            // List screen
            if (listContent == null && panelList != null)
            {
                var sr = panelList.GetComponentInChildren<ScrollRect>(true);
                if (sr != null && sr.content != null)
                {
                    listContent = sr.content;
                }
                else
                {
                    listContent = panelList.transform.Find("Scroll View/Viewport/Content");
                }

                if (listContent == null)
                {
                    var allT = panelList.GetComponentsInChildren<Transform>(true);
                    for (int i = 0; i < allT.Length; i++)
                    {
                        if (allT[i].name == "Content")
                        {
                            listContent = allT[i];
                            break;
                        }
                    }
                }
            }
            if (listBackButton == null && panelList != null)
            {
                listBackButton = panelList.transform.Find("BackButton")?.GetComponent<Button>()
                              ?? panelList.GetComponentInChildren<Button>(true);
            }

            // Details screen
            if (panelDetails != null)
            {
                if (coverageText == null) coverageText = panelDetails.transform.Find("CoverageText")?.GetComponent<TMP_Text>();
                if (totalItemsText == null) totalItemsText = panelDetails.transform.Find("TotalItemsText")?.GetComponent<TMP_Text>();
                if (timeLimitText == null) timeLimitText = panelDetails.transform.Find("TimeLimitText")?.GetComponent<TMP_Text>();
                if (startButton == null) startButton = panelDetails.transform.Find("StartButton")?.GetComponent<Button>();
                if (detailsBackButton == null) detailsBackButton = panelDetails.transform.Find("BackButton")?.GetComponent<Button>();
            }

            // Play screen
            if (panelPlay != null)
            {
                if (questionText == null) questionText = panelPlay.transform.Find("QuestionText")?.GetComponent<TMP_Text>();
                if (counterText == null) counterText = panelPlay.transform.Find("CounterText")?.GetComponent<TMP_Text>();
                if (timerText == null) timerText = panelPlay.transform.Find("TimerText")?.GetComponent<TMP_Text>();

                if (optionsToggleGroup == null)
                {
                    var optTrans = panelPlay.transform.Find("Options");
                    if (optTrans != null) optionsToggleGroup = optTrans.GetComponent<ToggleGroup>();
                }

                if (optionToggles == null || optionToggles.Length == 0)
                {
                    var optTrans = panelPlay.transform.Find("Options");
                    if (optTrans != null)
                    {
                        var tA = optTrans.Find("Option_A")?.GetComponent<Toggle>();
                        var tB = optTrans.Find("Option_B")?.GetComponent<Toggle>();
                        var tC = optTrans.Find("Option_C")?.GetComponent<Toggle>();
                        var tD = optTrans.Find("Option_D")?.GetComponent<Toggle>();
                        optionToggles = new Toggle[] { tA, tB, tC, tD };
                    }
                }

                if (optionsToggleGroup != null)
                {
                    optionsToggleGroup.allowSwitchOff = true;
                }

                if (optionTexts == null || optionTexts.Length == 0)
                {
                    optionTexts = new TMP_Text[optionToggles != null ? optionToggles.Length : 4];
                    if (optionToggles != null)
                    {
                        for (int i = 0; i < optionToggles.Length; i++)
                        {
                            if (optionToggles[i] != null)
                            {
                                optionTexts[i] = optionToggles[i].GetComponentInChildren<TMP_Text>(true);
                            }
                        }
                    }
                }

                if (previousButton == null) previousButton = panelPlay.transform.Find("PreviousButton")?.GetComponent<Button>();
                if (nextButton == null) nextButton = panelPlay.transform.Find("NextButton")?.GetComponent<Button>();
                if (nextButtonText == null && nextButton != null) nextButtonText = nextButton.GetComponentInChildren<TMP_Text>(true);
            }

            // Summary screen
            if (panelSummary != null)
            {
                if (summaryAnsweredText == null) summaryAnsweredText = panelSummary.transform.Find("AnsweredText")?.GetComponent<TMP_Text>();
                if (summaryTimeUsedText == null) summaryTimeUsedText = panelSummary.transform.Find("TimeUsedText")?.GetComponent<TMP_Text>();
                if (summarySubmitButton == null) summarySubmitButton = panelSummary.transform.Find("SubmitButton")?.GetComponent<Button>();
                if (summaryReviewButton == null) summaryReviewButton = panelSummary.transform.Find("ReviewButton")?.GetComponent<Button>();
            }

            // Result screen
            if (panelResult != null)
            {
                if (scoreText == null) scoreText = panelResult.transform.Find("ScoreText")?.GetComponent<TMP_Text>();
                if (percentText == null) percentText = panelResult.transform.Find("PercentText")?.GetComponent<TMP_Text>();
                if (resultTimeUsedText == null) resultTimeUsedText = panelResult.transform.Find("TimeUsedText")?.GetComponent<TMP_Text>();
                if (passedText == null) passedText = panelResult.transform.Find("PassedText")?.GetComponent<TMP_Text>();
                if (passedBadge == null) passedBadge = panelResult.transform.Find("Header/PassedBadge")?.gameObject;
                if (retryButton == null) retryButton = panelResult.transform.Find("RetryButton")?.GetComponent<Button>();
                if (reviewButton == null) reviewButton = panelResult.transform.Find("ViewAnswersButton")?.GetComponent<Button>();
                if (backToListButton == null) backToListButton = panelResult.transform.Find("BackToListButton")?.GetComponent<Button>();
                if (resultBackButton == null) resultBackButton = panelResult.transform.Find("BackButton")?.GetComponent<Button>();
            }
        }

        void BindEvents()
        {
            if (listBackButton != null)
            {
                listBackButton.onClick.RemoveAllListeners();
                listBackButton.onClick.AddListener(() =>
                {
                    if (MenuNavigation.CanClick()) ExitToMainMenu();
                });
            }

            if (detailsBackButton != null)
            {
                detailsBackButton.onClick.RemoveAllListeners();
                detailsBackButton.onClick.AddListener(() =>
                {
                    if (MenuNavigation.CanClick()) ShowListScreen();
                });
            }

            if (startButton != null)
            {
                startButton.onClick.RemoveAllListeners();
                startButton.onClick.AddListener(() =>
                {
                    if (MenuNavigation.CanClick()) StartQuiz();
                });
            }

            if (previousButton != null)
            {
                previousButton.onClick.RemoveAllListeners();
                previousButton.onClick.AddListener(OnPrevious);
            }

            if (nextButton != null)
            {
                nextButton.onClick.RemoveAllListeners();
                nextButton.onClick.AddListener(OnNext);
            }

            if (summaryReviewButton != null)
            {
                summaryReviewButton.onClick.RemoveAllListeners();
                summaryReviewButton.onClick.AddListener(() =>
                {
                    if (MenuNavigation.CanClick()) ReturnToPlayFromSummary();
                });
            }

            if (summarySubmitButton != null)
            {
                summarySubmitButton.onClick.RemoveAllListeners();
                summarySubmitButton.onClick.AddListener(() =>
                {
                    if (MenuNavigation.CanClick()) SubmitQuiz();
                });
            }

            if (retryButton != null)
            {
                retryButton.onClick.RemoveAllListeners();
                retryButton.onClick.AddListener(() =>
                {
                    if (MenuNavigation.CanClick()) StartQuiz();
                });
            }

            if (reviewButton != null)
            {
                reviewButton.onClick.RemoveAllListeners();
                reviewButton.onClick.AddListener(() =>
                {
                    if (MenuNavigation.CanClick()) StartReview();
                });
            }

            if (backToListButton != null)
            {
                backToListButton.onClick.RemoveAllListeners();
                backToListButton.onClick.AddListener(() =>
                {
                    if (MenuNavigation.CanClick()) ShowListScreen();
                });
            }

            if (resultBackButton != null)
            {
                resultBackButton.onClick.RemoveAllListeners();
                resultBackButton.onClick.AddListener(() =>
                {
                    if (MenuNavigation.CanClick()) ExitToMainMenu();
                });
            }

            // Wire Option Toggles
            if (optionToggles != null)
            {
                for (int i = 0; i < optionToggles.Length; i++)
                {
                    if (optionToggles[i] != null)
                    {
                        int index = i;
                        optionToggles[i].onValueChanged.RemoveAllListeners();
                        optionToggles[i].onValueChanged.AddListener((isOn) =>
                        {
                            if (isOn && !updatingToggles)
                            {
                                OnOptionSelected(index);
                            }
                        });
                    }
                }
            }
        }

        GameObject FindPanelInScene(string name)
        {
            var go = GameObject.Find(name);
            if (go != null) return go;

            var all = Resources.FindObjectsOfTypeAll<Transform>();
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i].gameObject.scene == gameObject.scene && all[i].gameObject.name == name)
                {
                    return all[i].gameObject;
                }
            }
            return null;
        }

        void Update()
        {
            if (!isQuizActive || isReviewMode) return;

            timeRemaining -= Time.deltaTime;
            timeSpent += Time.deltaTime;

            if (timeRemaining <= 0f)
            {
                timeRemaining = 0f;
                SubmitQuiz();
                return;
            }

            UpdateTimerDisplay();
        }

        void UpdateTimerDisplay()
        {
            if (timerText == null) return;
            int minutes = Mathf.FloorToInt(timeRemaining / 60f);
            int seconds = Mathf.FloorToInt(timeRemaining % 60f);
            timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
        }

        // ==========================================
        // PHASE 1: QUIZ LIST SCREEN
        // ==========================================

        public void ShowListScreen()
        {
            isQuizActive = false;
            isReviewMode = false;

            SetPanelActive(panelList, true);
            SetPanelActive(panelDetails, false);
            SetPanelActive(panelPlay, false);
            SetPanelActive(panelSummary, false);
            SetPanelActive(panelResult, false);
            SetPanelActive(panelOffline, false);

            PopulateQuizList();
        }

        void PopulateQuizList()
        {
            int grade = GameSession.Instance != null ? GameSession.Instance.Grade : 5;

            availableQuizzes = new List<QuizDef>
            {
                new QuizDef {
                    id = "ENG",
                    title = "LiteraQuest: English Mastery Assessment",
                    subject = "LiteraQuest",
                    coverage = "Grade " + grade + " English: Context Clues, Sequencing & Story Comprehension",
                    questionCount = QuizQuestionCount,
                    timeLimitSeconds = DefaultTimeLimitSeconds
                },
                new QuizDef {
                    id = "SCI",
                    title = "SciVenture: Science & Nature Assessment",
                    subject = "SciVenture",
                    coverage = "Grade " + grade + " Science: Matter & Changes, Living Things & Earth Systems",
                    questionCount = QuizQuestionCount,
                    timeLimitSeconds = DefaultTimeLimitSeconds
                },
                new QuizDef {
                    id = "PEH",
                    title = "HealthQuest: Wellness & PE Assessment",
                    subject = "HealthQuest",
                    coverage = "Grade " + grade + " PE & Health: Nutrition, Fitness & Personal Safety",
                    questionCount = QuizQuestionCount,
                    timeLimitSeconds = DefaultTimeLimitSeconds
                },
                new QuizDef {
                    id = "ALL",
                    title = "NutriMind: Core Mastery Challenge",
                    subject = "Core Mastery",
                    coverage = "Grade " + grade + " All Subjects: English, Science and PE & Health Review",
                    questionCount = QuizQuestionCount,
                    timeLimitSeconds = DefaultTimeLimitSeconds
                }
            };

            if (listContent == null) return;

            // Template row is the first child of listContent
            Transform templateRow = listContent.childCount > 0 ? listContent.GetChild(0) : null;
            if (templateRow == null) return;

            // Ensure we have enough child rows for each quiz
            for (int i = 0; i < availableQuizzes.Count; i++)
            {
                Transform row;
                if (i < listContent.childCount)
                {
                    row = listContent.GetChild(i);
                }
                else
                {
                    row = Instantiate(templateRow.gameObject, listContent).transform;
                }

                row.gameObject.SetActive(true);
                QuizDef def = availableQuizzes[i];

                var titleTmp = row.Find("TitleText")?.GetComponent<TMP_Text>();
                var itemsTmp = row.Find("ItemsText")?.GetComponent<TMP_Text>();
                var btn = row.Find("TakeQuizButton")?.GetComponent<Button>() ?? row.GetComponentInChildren<Button>();

                if (titleTmp != null) titleTmp.text = def.title;
                if (itemsTmp != null) itemsTmp.text = $"{def.questionCount} Questions  •  {Mathf.RoundToInt(def.timeLimitSeconds / 60f)}:00 Mins";

                if (btn != null)
                {
                    btn.interactable = true;
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() => SelectQuiz(def));
                }

                var rowBtn = row.GetComponent<Button>();
                if (rowBtn != null && rowBtn != btn)
                {
                    rowBtn.interactable = true;
                    rowBtn.onClick.RemoveAllListeners();
                    rowBtn.onClick.AddListener(() => SelectQuiz(def));
                }
            }

            // Hide any extra rows beyond our available quizzes
            for (int i = availableQuizzes.Count; i < listContent.childCount; i++)
            {
                listContent.GetChild(i).gameObject.SetActive(false);
            }
        }

        void SelectQuiz(QuizDef def)
        {
            if (!MenuNavigation.CanClick()) return;

            currentQuizDef = def;
            ShowDetailsScreen();
        }

        // ==========================================
        // PHASE 2: QUIZ DETAILS SCREEN
        // ==========================================

        public void ShowDetailsScreen()
        {
            isQuizActive = false;
            isReviewMode = false;

            SetPanelActive(panelList, false);
            SetPanelActive(panelDetails, true);
            SetPanelActive(panelPlay, false);
            SetPanelActive(panelSummary, false);
            SetPanelActive(panelResult, false);
            SetPanelActive(panelOffline, false);

            if (coverageText != null)
            {
                coverageText.text = !string.IsNullOrEmpty(currentQuizDef.coverage)
                    ? currentQuizDef.coverage
                    : "Core Grade Curriculum Assessment";
            }
            if (totalItemsText != null)
            {
                int count = currentQuizDef.questionCount > 0 ? currentQuizDef.questionCount : QuizQuestionCount;
                totalItemsText.text = count + " Questions";
            }
            if (timeLimitText != null)
            {
                float secs = currentQuizDef.timeLimitSeconds > 0 ? currentQuizDef.timeLimitSeconds : DefaultTimeLimitSeconds;
                int mins = Mathf.RoundToInt(secs / 60f);
                timeLimitText.text = mins + ":00 Minutes";
            }
        }

        // ==========================================
        // PHASE 3: QUIZ PLAY SCREEN
        // ==========================================

        public void StartQuiz()
        {
            LoadQuestionsForCurrentQuiz();

            if (questions.Count == 0)
            {
                Debug.LogWarning("NutriMind: No questions found for online quiz.");
                return;
            }

            userSelectedAnswers = new int[questions.Count];
            for (int i = 0; i < userSelectedAnswers.Length; i++) userSelectedAnswers[i] = -1;

            currentIndex = 0;
            timeRemaining = currentQuizDef.timeLimitSeconds > 0 ? currentQuizDef.timeLimitSeconds : DefaultTimeLimitSeconds;
            timeSpent = 0f;
            isQuizActive = true;
            isReviewMode = false;

            SetPanelActive(panelList, false);
            SetPanelActive(panelDetails, false);
            SetPanelActive(panelPlay, true);
            SetPanelActive(panelSummary, false);
            SetPanelActive(panelResult, false);
            SetPanelActive(panelOffline, false);

            RenderQuestion();
        }

        void LoadQuestionsForCurrentQuiz()
        {
            questions.Clear();
            int grade = GameSession.Instance != null ? GameSession.Instance.Grade : 5;
            string targetWorld = string.IsNullOrEmpty(currentQuizDef.id) ? "ENG" : currentQuizDef.id;

            if (targetWorld == "ALL")
            {
                // Pull questions across all 3 subjects
                AddQuestionsFromWorld("ENG", grade);
                AddQuestionsFromWorld("SCI", grade);
                AddQuestionsFromWorld("PEH", grade);
            }
            else
            {
                AddQuestionsFromWorld(targetWorld, grade);
            }

            // Fallback if target subject yielded no questions
            if (questions.Count == 0)
            {
                AddQuestionsFromWorld("ENG", grade);
            }

            // Shuffle questions
            for (int i = 0; i < questions.Count; i++)
            {
                int r = UnityEngine.Random.Range(i, questions.Count);
                QuestionData tmp = questions[i];
                questions[i] = questions[r];
                questions[r] = tmp;
            }

            // Clamp to desired question count
            int count = currentQuizDef.questionCount > 0 ? currentQuizDef.questionCount : QuizQuestionCount;
            if (questions.Count > count)
            {
                questions = questions.GetRange(0, count);
            }
        }

        void AddQuestionsFromWorld(string worldId, int grade)
        {
            ContentFile file = ContentDatabase.Get(worldId, grade);
            if (file != null && file.missions != null)
            {
                for (int m = 0; m < file.missions.Count; m++)
                {
                    var mission = file.missions[m];
                    if (mission.checkpoints == null) continue;
                    for (int c = 0; c < mission.checkpoints.Count; c++)
                    {
                        var cp = mission.checkpoints[c];
                        if (cp.questions != null) questions.AddRange(cp.questions);
                    }
                }
            }
        }

        void RenderQuestion()
        {
            if (currentIndex < 0 || currentIndex >= questions.Count) return;

            QuestionData q = questions[currentIndex];

            if (questionText != null) questionText.text = q.text;
            if (counterText != null) counterText.text = "Question " + (currentIndex + 1) + " of " + questions.Count;

            int selected = userSelectedAnswers[currentIndex];
            updatingToggles = true;

            if (optionToggles != null)
            {
                for (int i = 0; i < optionToggles.Length; i++)
                {
                    if (optionToggles[i] == null) continue;

                    bool hasOption = q.options != null && i < q.options.Length;
                    optionToggles[i].gameObject.SetActive(hasOption);
                    if (!hasOption) continue;

                    if (optionTexts != null && i < optionTexts.Length && optionTexts[i] != null)
                    {
                        optionTexts[i].text = ((char)('A' + i)) + ". " + q.options[i];
                    }

                    // In review mode, show correct answer and selected
                    bool shouldBeOn = isReviewMode ? (i == q.correctIndex || i == selected) : (i == selected);
                    optionToggles[i].SetIsOnWithoutNotify(shouldBeOn);

                    // Option styling
                    var img = optionToggles[i].targetGraphic as Image;
                    if (img != null)
                    {
                        if (isReviewMode)
                        {
                            if (i == q.correctIndex) img.color = correctColor;
                            else if (i == selected && selected != q.correctIndex) img.color = wrongColor;
                            else img.color = normalOptionColor;
                        }
                        else
                        {
                            img.color = (i == selected) ? selectedOptionColor : normalOptionColor;
                        }
                    }

                    // Options disabled color must maintain full alpha (255)
                    var cb = optionToggles[i].colors;
                    cb.disabledColor = new Color(1f, 1f, 1f, 1f);
                    optionToggles[i].colors = cb;
                    optionToggles[i].interactable = !isReviewMode;
                }
            }

            updatingToggles = false;

            if (previousButton != null) previousButton.interactable = currentIndex > 0;
            if (nextButtonText != null)
            {
                if (isReviewMode)
                {
                    nextButtonText.text = (currentIndex >= questions.Count - 1) ? "Done" : "Next";
                }
                else
                {
                    nextButtonText.text = (currentIndex >= questions.Count - 1) ? "Summary" : "Next";
                }
            }
        }

        void OnOptionSelected(int optionIndex)
        {
            if (!isQuizActive || isReviewMode) return;
            userSelectedAnswers[currentIndex] = optionIndex;

            if (optionToggles != null)
            {
                for (int i = 0; i < optionToggles.Length; i++)
                {
                    if (optionToggles[i] != null && optionToggles[i].targetGraphic is Image img)
                    {
                        img.color = (i == optionIndex) ? selectedOptionColor : normalOptionColor;
                    }
                }
            }
        }

        void OnPrevious()
        {
            if (currentIndex > 0)
            {
                currentIndex--;
                RenderQuestion();
            }
        }

        void OnNext()
        {
            if (currentIndex < questions.Count - 1)
            {
                currentIndex++;
                RenderQuestion();
            }
            else
            {
                if (isReviewMode)
                {
                    ShowResultScreen();
                }
                else
                {
                    // Transition to Quiz Summary first before final submission
                    ShowSummaryScreen();
                }
            }
        }

        // ==========================================
        // PHASE 4: QUIZ SUMMARY SCREEN
        // ==========================================

        public void ShowSummaryScreen()
        {
            SetPanelActive(panelList, false);
            SetPanelActive(panelDetails, false);
            SetPanelActive(panelPlay, false);
            SetPanelActive(panelSummary, true);
            SetPanelActive(panelResult, false);
            SetPanelActive(panelOffline, false);

            int answeredCount = 0;
            List<int> unanswered = new List<int>();
            for (int i = 0; i < userSelectedAnswers.Length; i++)
            {
                if (userSelectedAnswers[i] >= 0) answeredCount++;
                else unanswered.Add(i + 1);
            }

            int total = questions.Count;
            int unansweredCount = total - answeredCount;

            if (summaryAnsweredText != null)
            {
                if (unansweredCount == 0)
                {
                    summaryAnsweredText.text = $"<color=#8CE68C><b>{answeredCount} of {total} Answered</b></color>\n(All questions answered!)";
                }
                else
                {
                    string list = string.Join(", #", unanswered);
                    summaryAnsweredText.text = $"<b>{answeredCount} of {total} Answered</b>\n<color=#FF9E9E>Unanswered ({unansweredCount}): #{list}</color>";
                }
            }

            if (summaryTimeUsedText != null)
            {
                int minUsed = Mathf.FloorToInt(timeSpent / 60f);
                int secUsed = Mathf.FloorToInt(timeSpent % 60f);
                int minRem = Mathf.FloorToInt(timeRemaining / 60f);
                int secRem = Mathf.FloorToInt(timeRemaining % 60f);

                summaryTimeUsedText.text = $"Time Used: <b>{minUsed:00}:{secUsed:00}</b>   •   Remaining: <b>{minRem:00}:{secRem:00}</b>";
            }
        }

        public void ReturnToPlayFromSummary()
        {
            SetPanelActive(panelSummary, false);
            SetPanelActive(panelPlay, true);

            RenderQuestion();
        }

        // ==========================================
        // PHASE 5: QUIZ RESULT SCREEN
        // ==========================================

        public void SubmitQuiz()
        {
            isQuizActive = false;

            int correctCount = 0;
            for (int i = 0; i < questions.Count; i++)
            {
                if (userSelectedAnswers[i] == questions[i].correctIndex)
                {
                    correctCount++;
                }
            }

            int total = questions.Count;
            int pct = total > 0 ? Mathf.RoundToInt((float)correctCount / total * 100f) : 0;
            int pointsEarned = correctCount * 10;

            if (GameSession.Instance != null)
            {
                string subjectTag = !string.IsNullOrEmpty(currentQuizDef.id) ? currentQuizDef.id : "Quiz";
                GameSession.Instance.RecordQuizResult(subjectTag, correctCount, total, pointsEarned);
            }

            ShowResultScreen(correctCount, total, pct);
        }

        void ShowResultScreen(int correctCount = -1, int total = -1, int pct = -1)
        {
            if (correctCount < 0)
            {
                correctCount = 0;
                for (int i = 0; i < questions.Count; i++)
                {
                    if (userSelectedAnswers[i] == questions[i].correctIndex) correctCount++;
                }
                total = questions.Count;
                pct = total > 0 ? Mathf.RoundToInt((float)correctCount / total * 100f) : 0;
            }

            SetPanelActive(panelList, false);
            SetPanelActive(panelDetails, false);
            SetPanelActive(panelPlay, false);
            SetPanelActive(panelSummary, false);
            SetPanelActive(panelResult, true);
            SetPanelActive(panelOffline, false);

            if (scoreText != null) scoreText.text = correctCount + " / " + total;
            if (percentText != null) percentText.text = "Percentage: " + pct + "%";

            if (resultTimeUsedText != null)
            {
                int min = Mathf.FloorToInt(timeSpent / 60f);
                int sec = Mathf.FloorToInt(timeSpent % 60f);
                resultTimeUsedText.text = "Time Used: " + string.Format("{0:00}:{1:00}", min, sec);
            }

            bool passed = pct >= 75;
            if (passedText != null)
            {
                passedText.text = passed ? "Quiz Passed! Excellent Work!" : "Completed! Keep Reviewing!";
            }
            if (passedBadge != null)
            {
                passedBadge.SetActive(passed);
            }
        }

        void StartReview()
        {
            isReviewMode = true;
            currentIndex = 0;

            SetPanelActive(panelResult, false);
            SetPanelActive(panelPlay, true);

            RenderQuestion();
        }

        void SetPanelActive(GameObject panel, bool active)
        {
            if (panel != null) panel.SetActive(active);
        }

        public void ExitToMainMenu()
        {
            if (Application.CanStreamedLevelBeLoaded(SceneNames.MainMenu))
            {
                MenuNavigation.Cooldown(0.6f);
                SceneManager.LoadScene(SceneNames.MainMenu);
            }
        }
    }
}
