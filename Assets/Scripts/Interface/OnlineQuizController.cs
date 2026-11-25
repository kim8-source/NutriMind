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
using NutriMind.Network;

namespace NutriMind.Interface
{
    // SERVER QUIZZES (teachers' quizzes from the web app)
    // When the learner signed in through the server, the list shows the quizzes their teachers assigned
    // (GET /api/v1/game/quizzes), followed by the game's built-in quizzes marked "Practice:".
    //   - Server quiz: questions come from the server WITHOUT answers; on submit the SERVER grades the answers
    //     (POST /quizzes/{id}/attempts) and the result screen shows the server's score. Teachers see the attempt.
    //   - Practice quiz: works exactly as before, fully offline, graded on the device.
    // When the server can't be reached, Panel_Offline is shown (MessageText, RetryButton, BackButton).
    // Identification (typed answer) questions need a TMP_InputField named "AnswerInput" inside Panel_QuizPlay;
    // until a designer adds one, those questions show without a way to answer and count as wrong.
    //
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
        [SerializeField] Image progressBar;
        [SerializeField] TMP_Text timerText;
        [SerializeField] Button playBackButton;
        [SerializeField] ToggleGroup optionsToggleGroup;
        [SerializeField] Toggle[] optionToggles;
        [SerializeField] TMP_Text[] optionTexts;
        [SerializeField] Button previousButton;
        [SerializeField] Button nextButton;
        [SerializeField] TMP_Text nextButtonText;
        [SerializeField] Image questionImage;
        [SerializeField] VideoPlayer questionVideoPlayer;
        [Tooltip("Optional: text box for identification (typed answer) questions from teachers. Auto-found as Panel_QuizPlay/AnswerInput.")]
        [SerializeField] TMP_InputField answerInput;

        [Header("Offline Screen (shown when the server can't be reached)")]
        [SerializeField] TMP_Text offlineMessageText;
        [SerializeField] Button offlineRetryButton;
        [SerializeField] Button offlineBackButton;

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
            public float timeLimitSeconds;     // 0 = no time limit (teacher quizzes may have none)

            // Teacher quizzes from the server (fromServer == false means a built-in practice quiz).
            public bool fromServer;
            public int serverId;
            public bool unlimitedAttempts;
            public int attemptsLeft;

            public bool CanStart
            {
                get { return !fromServer || unlimitedAttempts || attemptsLeft > 0; }
            }
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

        // Server quiz state.
        List<ServerQuestion> serverQuestions = new List<ServerQuestion>();
        string[] typedAnswers;            // answers to identification questions
        bool[] serverCorrect;             // per question, as graded by the server
        QuizAttemptData lastServerResult; // the graded attempt shown on the result screen
        DateTime quizStartedUtc;
        bool hasTimeLimit = true;
        bool isWaitingForServer;
        Action offlineRetryAction;        // what RetryButton on Panel_Offline does

        bool IsServerQuiz
        {
            get { return currentQuizDef.fromServer; }
        }

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
                if (progressBar == null) progressBar = panelPlay.transform.Find("ProgressBar")?.GetComponent<Image>();
                if (timerText == null) timerText = panelPlay.transform.Find("TimerText")?.GetComponent<TMP_Text>();
                if (playBackButton == null) playBackButton = panelPlay.transform.Find("BackButton")?.GetComponent<Button>();

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
                if (answerInput == null) answerInput = panelPlay.transform.Find("AnswerInput")?.GetComponent<TMP_InputField>();
                if (answerInput != null) answerInput.gameObject.SetActive(false);
            }

            // Offline screen
            if (panelOffline != null)
            {
                if (offlineMessageText == null) offlineMessageText = panelOffline.transform.Find("MessageText")?.GetComponent<TMP_Text>();
                if (offlineRetryButton == null) offlineRetryButton = panelOffline.transform.Find("RetryButton")?.GetComponent<Button>();
                if (offlineBackButton == null) offlineBackButton = panelOffline.transform.Find("BackButton")?.GetComponent<Button>();
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

            if (playBackButton != null)
            {
                playBackButton.onClick.RemoveAllListeners();
                playBackButton.onClick.AddListener(() =>
                {
                    if (!MenuNavigation.CanClick()) return;
                    if (isReviewMode) ShowResultScreen();
                    else ShowDetailsScreen();
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

            if (offlineRetryButton != null)
            {
                offlineRetryButton.onClick.RemoveAllListeners();
                offlineRetryButton.onClick.AddListener(() =>
                {
                    if (!MenuNavigation.CanClick()) return;
                    SetPanelActive(panelOffline, false);
                    if (offlineRetryAction != null) offlineRetryAction();
                });
            }

            if (offlineBackButton != null)
            {
                // Closes the offline message and returns to the screen underneath (nothing typed is lost).
                offlineBackButton.onClick.RemoveAllListeners();
                offlineBackButton.onClick.AddListener(() =>
                {
                    if (MenuNavigation.CanClick()) SetPanelActive(panelOffline, false);
                });
            }

            if (answerInput != null)
            {
                answerInput.onValueChanged.RemoveAllListeners();
                answerInput.onValueChanged.AddListener(text =>
                {
                    if (isQuizActive && !isReviewMode && typedAnswers != null && currentIndex < typedAnswers.Length)
                    {
                        typedAnswers[currentIndex] = text;
                    }
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

            if (!hasTimeLimit)
            {
                if (timerText != null) timerText.text = "No limit";
                if (summaryTimeUsedText != null && summaryTimeUsedText.gameObject.activeInHierarchy)
                {
                    summaryTimeUsedText.text = "No limit";
                }
                return;
            }

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
            int minutes = Mathf.FloorToInt(timeRemaining / 60f);
            int seconds = Mathf.FloorToInt(timeRemaining % 60f);
            string formatted = string.Format("{0:00}:{1:00}", minutes, seconds);

            if (timerText != null) timerText.text = formatted;
            if (summaryTimeUsedText != null && summaryTimeUsedText.gameObject.activeInHierarchy)
            {
                summaryTimeUsedText.text = formatted;
            }
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

        // Shows the built-in practice quizzes right away; when the learner signed in through the server, the
        // teachers' quizzes are then loaded and listed first.
        void PopulateQuizList()
        {
            availableQuizzes = BuildPracticeQuizzes(false);
            RenderQuizRows();

            if (ApiSession.HasToken) StartCoroutine(LoadServerQuizzes());
        }

        IEnumerator LoadServerQuizzes()
        {
            ApiResult result = null;
            yield return ApiClient.GetQuizzes(r => result = r);

            if (result.Success)
            {
                var combined = new List<QuizDef>();
                foreach (ServerQuiz quiz in result.Parse<QuizListResponse>().data) combined.Add(ToQuizDef(quiz));
                combined.AddRange(BuildPracticeQuizzes(combined.Count > 0));
                availableQuizzes = combined;
                RenderQuizRows();
            }
            else if (result.IsServerUnreachable)
            {
                ShowOffline("Can't reach the NutriMind server, so your teachers' quizzes can't be loaded right now.\nYou can still take the practice quizzes.", PopulateQuizList);
            }
            else
            {
                Debug.LogWarning("NutriMind: could not load the teachers' quizzes (" + result.StatusCode + "): " + result.ErrorMessage);
            }
        }

        static QuizDef ToQuizDef(ServerQuiz quiz)
        {
            string subjectName = quiz.subject != null ? quiz.subject.name : "Quiz";
            return new QuizDef
            {
                id = "server-" + quiz.id,
                title = quiz.title,
                subject = subjectName,
                coverage = string.IsNullOrEmpty(quiz.instructions) ? subjectName : subjectName + "\n" + quiz.instructions,
                questionCount = quiz.question_count,
                timeLimitSeconds = quiz.time_limit_minutes * 60f,
                fromServer = true,
                serverId = quiz.id,
                unlimitedAttempts = quiz.HasUnlimitedAttempts,
                attemptsLeft = quiz.attempts_left
            };
        }

        // The game's own quizzes, built from the local mission content (always available offline).
        List<QuizDef> BuildPracticeQuizzes(bool markAsPractice)
        {
            int grade = GameSession.Instance != null ? GameSession.Instance.Grade : 5;

            var practiceQuizzes = new List<QuizDef>
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

            if (markAsPractice)
            {
                for (int i = 0; i < practiceQuizzes.Count; i++)
                {
                    QuizDef def = practiceQuizzes[i];
                    def.title = "Practice: " + def.title;
                    practiceQuizzes[i] = def;
                }
            }

            return practiceQuizzes;
        }

        void RenderQuizRows()
        {
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
                if (itemsTmp != null)
                {
                    string time = def.timeLimitSeconds > 0 ? $"{Mathf.RoundToInt(def.timeLimitSeconds / 60f)}:00 Mins" : "No time limit";
                    itemsTmp.text = $"{def.questionCount} Questions  •  {time}";
                }

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

            NutriMind.Core.TextSizeManager.ApplyToHierarchy(listContent);
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
                string text = count + " Questions";
                if (IsServerQuiz && !currentQuizDef.unlimitedAttempts)
                {
                    text += "  •  " + currentQuizDef.attemptsLeft + (currentQuizDef.attemptsLeft == 1 ? " attempt left" : " attempts left");
                }
                totalItemsText.text = text;
            }
            if (timeLimitText != null)
            {
                if (IsServerQuiz && currentQuizDef.timeLimitSeconds <= 0)
                {
                    timeLimitText.text = "No time limit";
                }
                else
                {
                    float secs = currentQuizDef.timeLimitSeconds > 0 ? currentQuizDef.timeLimitSeconds : DefaultTimeLimitSeconds;
                    int mins = Mathf.RoundToInt(secs / 60f);
                    timeLimitText.text = mins + ":00 Minutes";
                }
            }

            // A teacher quiz whose attempts are used up can't be started again.
            if (startButton != null) startButton.interactable = currentQuizDef.CanStart;
        }

        // ==========================================
        // PHASE 3: QUIZ PLAY SCREEN
        // ==========================================

        public void StartQuiz()
        {
            if (IsServerQuiz)
            {
                if (!currentQuizDef.CanStart || isWaitingForServer) return;
                StartCoroutine(LoadServerQuestionsThenBegin());
                return;
            }

            serverQuestions.Clear();
            LoadQuestionsForCurrentQuiz();
            BeginQuiz();
        }

        // Downloads a teacher quiz's questions (without answers) and starts it.
        IEnumerator LoadServerQuestionsThenBegin()
        {
            isWaitingForServer = true;
            if (startButton != null) startButton.interactable = false;
            if (retryButton != null) retryButton.interactable = false;

            ApiResult result = null;
            yield return ApiClient.GetQuiz(currentQuizDef.serverId, r => result = r);

            isWaitingForServer = false;
            if (startButton != null) startButton.interactable = currentQuizDef.CanStart;

            if (!result.Success)
            {
                // 404 = the quiz was closed or unassigned since the list loaded.
                ShowOffline(result.IsServerUnreachable ? "Can't reach the NutriMind server to load this quiz." : result.ErrorMessage,
                            result.IsServerUnreachable ? (Action)StartQuiz : null);
                yield break;
            }

            ServerQuiz quiz = result.Parse<QuizDetailResponse>().data;
            currentQuizDef.timeLimitSeconds = quiz.time_limit_minutes * 60f;
            currentQuizDef.unlimitedAttempts = quiz.HasUnlimitedAttempts;
            currentQuizDef.attemptsLeft = quiz.attempts_left;

            if (!currentQuizDef.CanStart)
            {
                ShowDetailsScreen();
                yield break;
            }

            // The quiz screens show QuestionData, so the server questions are copied into that shape.
            // correctIndex stays -1 (unknown) until the server grades the attempt.
            serverQuestions = quiz.questions ?? new List<ServerQuestion>();
            questions.Clear();
            foreach (ServerQuestion question in serverQuestions)
            {
                questions.Add(new QuestionData
                {
                    id = question.id.ToString(),
                    text = question.prompt,
                    options = question.choices != null ? question.choices.ToArray() : new string[0],
                    correctIndex = -1
                });
            }

            BeginQuiz();
        }

        void BeginQuiz()
        {
            if (questions.Count == 0)
            {
                Debug.LogWarning("NutriMind: No questions found for online quiz.");
                if (IsServerQuiz) ShowOffline("This quiz has no questions yet. Please tell your teacher.", null);
                return;
            }

            userSelectedAnswers = new int[questions.Count];
            for (int i = 0; i < userSelectedAnswers.Length; i++) userSelectedAnswers[i] = -1;
            typedAnswers = new string[questions.Count];
            serverCorrect = null;
            lastServerResult = null;
            quizStartedUtc = DateTime.UtcNow;
            hasTimeLimit = !IsServerQuiz || currentQuizDef.timeLimitSeconds > 0;

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

            bool isTypedAnswer = IsServerQuiz && currentIndex < serverQuestions.Count && serverQuestions[currentIndex].IsTypedAnswer;

            if (questionText != null)
            {
                questionText.text = q.text;
                if (isTypedAnswer && answerInput == null)
                {
                    questionText.text += "\n\n<size=70%>(Type-in answers are not supported on this screen yet.)</size>";
                    Debug.LogWarning("NutriMind: question " + q.id + " needs a typed answer. Add a TMP_InputField named \"AnswerInput\" to Panel_QuizPlay.");
                }
            }
            if (counterText != null) counterText.text = "Question " + (currentIndex + 1) + " of " + questions.Count;
            if (progressBar != null)
            {
                progressBar.type = Image.Type.Filled;
                progressBar.fillMethod = Image.FillMethod.Horizontal;
                progressBar.fillOrigin = (int)Image.OriginHorizontal.Left;
                progressBar.fillAmount = questions.Count > 0 ? (float)(currentIndex + 1) / questions.Count : 0f;
            }

            // Identification questions from teachers: show the text box instead of the choices.
            if (answerInput != null)
            {
                answerInput.gameObject.SetActive(isTypedAnswer);
                if (isTypedAnswer)
                {
                    answerInput.SetTextWithoutNotify(typedAnswers != null ? (typedAnswers[currentIndex] ?? "") : "");
                    answerInput.interactable = !isReviewMode;
                }
            }

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
                            // correctIndex is -1 for a teacher quiz that hides correct answers; the learner
                            // still sees whether their own choice was right (from the server's grading).
                            if (i == q.correctIndex) img.color = correctColor;
                            else if (i == selected) img.color = IsAnswerCorrect(currentIndex) ? correctColor : wrongColor;
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
            AudioManager.PlayClick();
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
            for (int i = 0; i < userSelectedAnswers.Length; i++)
            {
                if (IsAnswered(i)) answeredCount++;
            }

            int total = questions.Count;

            if (summaryAnsweredText != null)
            {
                summaryAnsweredText.text = $"{answeredCount}/{total} Answered";
            }

            if (summaryTimeUsedText != null)
            {
                if (!hasTimeLimit)
                {
                    summaryTimeUsedText.text = "No limit";
                }
                else
                {
                    int minRem = Mathf.FloorToInt(timeRemaining / 60f);
                    int secRem = Mathf.FloorToInt(timeRemaining % 60f);
                    summaryTimeUsedText.text = string.Format("{0:00}:{1:00}", minRem, secRem);
                }
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
            if (IsServerQuiz)
            {
                if (isWaitingForServer) return;
                isQuizActive = false;
                StartCoroutine(SubmitToServer());
                return;
            }

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

        // Sends a teacher quiz's answers; the SERVER grades them and stores the attempt for the teacher.
        // If sending fails, the answers stay on screen and RetryButton sends them again.
        IEnumerator SubmitToServer()
        {
            isWaitingForServer = true;
            if (summarySubmitButton != null) summarySubmitButton.interactable = false;

            // started_at is left empty on purpose: a tablet with a wrong clock would be refused. The server
            // works out the start time from duration_seconds.
            var attempt = new QuizAttemptRequest
            {
                duration_seconds = Mathf.RoundToInt(timeSpent),
                is_offline = false
            };
            for (int i = 0; i < serverQuestions.Count; i++)
            {
                string answer = serverQuestions[i].IsTypedAnswer
                    ? (typedAnswers[i] ?? "").Trim()
                    : (userSelectedAnswers[i] >= 0 ? userSelectedAnswers[i].ToString() : "");
                if (answer.Length > 0) attempt.answers.Add(new QuizAnswer { question_id = serverQuestions[i].id, answer = answer });
            }

            ApiResult result = null;
            yield return ApiClient.SubmitQuizAttempt(currentQuizDef.serverId, attempt, r => result = r);

            isWaitingForServer = false;
            if (summarySubmitButton != null) summarySubmitButton.interactable = true;

            if (!result.Success)
            {
                // 422 here usually means "You have used all N attempts for this quiz."
                ShowOffline(result.IsServerUnreachable
                        ? "Can't reach the NutriMind server, so your answers were not sent yet.\nYour answers are kept. Tap Retry to send them."
                        : result.ErrorMessage,
                    result.IsServerUnreachable ? (Action)SubmitQuiz : null);
                yield break;
            }

            QuizAttemptResponse response = result.Parse<QuizAttemptResponse>();
            lastServerResult = response.data;
            currentQuizDef.attemptsLeft = lastServerResult.attempts_left;

            // Copy the server's grading into the review screen.
            serverCorrect = new bool[questions.Count];
            foreach (QuizResultData graded in lastServerResult.results)
            {
                int index = serverQuestions.FindIndex(q => q.id == graded.question_id);
                if (index < 0) continue;
                serverCorrect[index] = graded.is_correct;
                if (graded.HasCorrectAnswer && !serverQuestions[index].IsTypedAnswer) questions[index].correctIndex = graded.correct_choice;
            }

            // The server's updated points/level (also refreshes anything listening to SyncService.ProfileUpdated).
            SyncService.PublishProfile(response.student);

            ShowResultScreen();
        }

        void ShowServerResultScreen()
        {
            SetPanelActive(panelList, false);
            SetPanelActive(panelDetails, false);
            SetPanelActive(panelPlay, false);
            SetPanelActive(panelSummary, false);
            SetPanelActive(panelResult, true);
            SetPanelActive(panelOffline, false);

            QuizAttemptData attempt = lastServerResult;
            int pct = Mathf.RoundToInt(attempt.percentage);

            // Score is in POINTS (teachers can make a question worth more than 1 point).
            if (scoreText != null) scoreText.text = attempt.score + " / " + attempt.max_score;
            if (percentText != null) percentText.text = "Percentage: " + pct + "%";

            if (resultTimeUsedText != null)
            {
                int min = Mathf.FloorToInt(timeSpent / 60f);
                int sec = Mathf.FloorToInt(timeSpent % 60f);
                resultTimeUsedText.text = "Time Used: " + string.Format("{0:00}:{1:00}", min, sec);
            }

            if (passedText != null) passedText.text = attempt.passed ? "Quiz Passed! Excellent Work!" : "Completed! Keep Reviewing!";
            if (passedBadge != null) passedBadge.SetActive(attempt.passed);
            if (retryButton != null) retryButton.interactable = currentQuizDef.CanStart;
        }

        void ShowResultScreen(int correctCount = -1, int total = -1, int pct = -1)
        {
            if (IsServerQuiz && lastServerResult != null)
            {
                ShowServerResultScreen();
                return;
            }

            if (retryButton != null) retryButton.interactable = true;

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

        // Shows Panel_Offline over the current screen. RetryButton runs retry (hidden when retry is null);
        // BackButton just closes the panel, so whatever the learner was doing is still there.
        void ShowOffline(string message, Action retry)
        {
            offlineRetryAction = retry;

            if (panelOffline == null)
            {
                Debug.LogWarning("NutriMind: " + message);
                return;
            }

            if (offlineMessageText != null) offlineMessageText.text = message;
            if (offlineRetryButton != null) offlineRetryButton.gameObject.SetActive(retry != null);
            panelOffline.SetActive(true);
            panelOffline.transform.SetAsLastSibling(); // draw on top of the other panels
        }

        bool IsAnswered(int index)
        {
            if (userSelectedAnswers != null && userSelectedAnswers[index] >= 0) return true;
            return typedAnswers != null && !string.IsNullOrWhiteSpace(typedAnswers[index]);
        }

        // Teacher quizzes use the server's grading; practice quizzes compare with the local answer key.
        bool IsAnswerCorrect(int index)
        {
            if (serverCorrect != null) return serverCorrect[index];
            return userSelectedAnswers[index] == questions[index].correctIndex;
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
