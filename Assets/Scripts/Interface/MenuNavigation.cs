using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using NutriMind.Core;
using NutriMind.Data;

namespace NutriMind.Interface
{
    // Navigation and menu controller for Boot, Login, MainMenu, WorldSelect, and MissionSelect.
    // Handles panel switching, profile info, learning progress, and game settings in MainMenu.
    public class MenuNavigation : MonoBehaviour
    {
        [Header("Login scene only")]
        [SerializeField] TMP_InputField studentIdInput;
        [SerializeField] TMP_InputField passwordInput;
        [SerializeField] TMP_Text messageText;

        [Header("MissionSelect scene only")]
        [Tooltip("World Title TMP_Text component in MissionSelect (e.g. WorldTitle)")]
        [SerializeField] TMP_Text missionWorldTitleText;
        [Tooltip("Lock sprite for locked missions (e.g. Assets/Sprites/lock.png)")]
        [SerializeField] Sprite missionLockSprite;
        [Tooltip("Flag sprite for unlocked missions (e.g. Assets/Sprites/flag.png)")]
        [SerializeField] Sprite missionFlagSprite;

        [Header("MissionSelect - Node UI Images (Inspector Configurable)")]
        [Tooltip("UI Image component for Mission 1 (displays Lock or Flag)")]
        [SerializeField] Image mission1NodeImage;
        [Tooltip("UI Image component for Mission 2 (displays Lock or Flag)")]
        [SerializeField] Image mission2NodeImage;
        [Tooltip("UI Image component for Mission 3 (displays Lock or Flag)")]
        [SerializeField] Image mission3NodeImage;

        [Header("MainMenu scene only")]
        [SerializeField] TMP_Text helloText;
        [SerializeField] TMP_Text pointsText;

        [Header("Panels in MainMenu (auto-detected if null)")]
        [SerializeField] GameObject panelHome;
        [SerializeField] GameObject panelChooseActivity;
        [SerializeField] GameObject panelProfile;
        [SerializeField] GameObject panelProgress;
        [SerializeField] GameObject panelSettings;

        [Header("Profile Panel - Current Badge & Details")]
        [Tooltip("Image component displaying the student's current learner rank badge")]
        [SerializeField] Image currentBadgeImage;

        [Header("Profile Panel - Spinning Shine Effect")]
        [Tooltip("Image component for the shining background behind the badge (assign in Inspector).")]
        [SerializeField] Image badgeShineImage;
        [Tooltip("Continuous spin speed of the shine effect in degrees per second.")]
        [SerializeField] float badgeShineSpinSpeed = 25f;

        [Header("Profile Panel - Rank Badge Sprites (Inspector Configurable)")]
        [Tooltip("Badge sprite for Rank 1 (Apprentice Learner). Set in Inspector to customize.")]
        [SerializeField] Sprite rank1BadgeSprite;
        [Tooltip("Badge sprite for Rank 2 (Curious Explorer). Set in Inspector to customize.")]
        [SerializeField] Sprite rank2BadgeSprite;
        [Tooltip("Badge sprite for Rank 3 (Knowledge Seeker). Set in Inspector to customize.")]
        [SerializeField] Sprite rank3BadgeSprite;
        [Tooltip("Badge sprite for Rank 4 (Honor Scholar). Set in Inspector to customize.")]
        [SerializeField] Sprite rank4BadgeSprite;
        [Tooltip("Badge sprite for Rank 5 (Master Scholar). Set in Inspector to customize.")]
        [SerializeField] Sprite rank5BadgeSprite;
        [Tooltip("Optional array of rank badge sprites (Index 0 = Rank 1, Index 1 = Rank 2, etc.)")]
        [SerializeField] Sprite[] rankBadgeSprites;

        [Header("Profile Panel - Rank Details")]
        [Tooltip("TMP_Text displaying the rank title (e.g. 'Apprentice Learner')")]
        [SerializeField] TMP_Text rankNameText;
        [Tooltip("TMP_Text displaying the rank level number (e.g. '1' or 'Level 1')")]
        [SerializeField] TMP_Text rankNumberText;
        [Tooltip("If true, rankNumberText displays 'Level X'; if false, displays 'X'")]
        [SerializeField] bool prefixLevelWithText = true;
        [Tooltip("(Legacy/Combined) TMP_Text displaying both level and rank title if assigned")]
        [SerializeField] TMP_Text currentBadgeText;
        [Tooltip("Optional TMP_Text for Student ID")]
        [SerializeField] TMP_Text studentIdText;
        [Tooltip("Optional TMP_Text for Grade Level")]
        [SerializeField] TMP_Text gradeLevelText;
        [Tooltip("Optional TMP_Text for Total Score")]
        [SerializeField] TMP_Text totalScoreText;

        [Header("Profile Panel - Academic Statistics")]
        [Tooltip("TMP_Text for Missions Completed (e.g. '0 / 9')")]
        [SerializeField] TMP_Text missionsCompletedText;
        [Tooltip("TMP_Text for Stations Mastered (e.g. '0 / 27')")]
        [SerializeField] TMP_Text stationsMasteredText;
        [Tooltip("TMP_Text for Checkpoints Cleared (e.g. '0 / 27')")]
        [SerializeField] TMP_Text checkpointsCountText;
        [Tooltip("TMP_Text for Quizzes Completed / Taken (e.g. '0')")]
        [SerializeField] TMP_Text quizzesCompletedText;
        [Tooltip("TMP_Text for Quizzes Passed (e.g. '0')")]
        [SerializeField] TMP_Text quizzesPassedText;
        [Tooltip("TMP_Text for Best Quiz Score (e.g. '0 PTS')")]
        [SerializeField] TMP_Text bestQuizScoreText;

        [Header("Profile Panel - Collected Badges")]
        [Tooltip("Container/parent Transform where collected badges are placed (e.g., Collection)")]
        [SerializeField] Transform collectedBadgesContainer;
        [Tooltip("Prefab for a collected badge (consisting of an Image and a TMP_Text)")]
        [SerializeField] GameObject collectedBadgePrefab;

        [Header("Progress Panel - LiteraQuest (English)")]
        [Tooltip("Percentage text for LiteraQuest (e.g. 11%)")]
        [SerializeField] TMP_Text engPercentText;
        [Tooltip("Checkpoint count text for LiteraQuest (e.g. 1 of 9 Checkpoints)")]
        [SerializeField] TMP_Text engCountText;
        [Tooltip("Progress bar fill Image for LiteraQuest")]
        [SerializeField] Image engProgressBar;

        [Header("Progress Panel - SciVenture (Science)")]
        [Tooltip("Percentage text for SciVenture (e.g. 0%)")]
        [SerializeField] TMP_Text sciPercentText;
        [Tooltip("Checkpoint count text for SciVenture (e.g. 0 of 9 Checkpoints)")]
        [SerializeField] TMP_Text sciCountText;
        [Tooltip("Progress bar fill Image for SciVenture")]
        [SerializeField] Image sciProgressBar;

        [Header("Progress Panel - HealthQuest (PE & Health)")]
        [Tooltip("Percentage text for HealthQuest (e.g. 0%)")]
        [SerializeField] TMP_Text pehPercentText;
        [Tooltip("Checkpoint count text for HealthQuest (e.g. 0 of 9 Checkpoints)")]
        [SerializeField] TMP_Text pehCountText;
        [Tooltip("Progress bar fill Image for HealthQuest")]
        [SerializeField] Image pehProgressBar;

        [Header("Progress Panel - Bottom Summary")]
        [Tooltip("Overall curriculum completion text at the bottom")]
        [SerializeField] TMP_Text overallCompletionText;
        [Tooltip("Total earned points text at the bottom")]
        [SerializeField] TMP_Text totalPointsEarnedText;

        [Header("Progress Panel - Animation")]
        [Tooltip("Duration of the ease-out count-up and fill animation in seconds.")]
        [SerializeField] float progressAnimationDuration = 1.0f;

        Coroutine m_ProgressAnimCoroutine;

        static float s_ignoreClicksUntil = 0f;

        public static void Cooldown(float seconds = 0.5f)
        {
            s_ignoreClicksUntil = Mathf.Max(s_ignoreClicksUntil, Time.unscaledTime + seconds);
        }

        public static bool CanClick()
        {
            if (Time.unscaledTime < s_ignoreClicksUntil) return false;
            s_ignoreClicksUntil = Time.unscaledTime + 0.35f;
            return true;
        }

        void Awake()
        {
            GameSettings.Initialize();
            s_ignoreClicksUntil = Mathf.Max(s_ignoreClicksUntil, Time.unscaledTime + 0.5f);
        }

        void OnEnable()
        {
            s_ignoreClicksUntil = Mathf.Max(s_ignoreClicksUntil, Time.unscaledTime + 0.4f);
        }

        void OnDisable()
        {
            if (m_ProgressAnimCoroutine != null)
            {
                StopCoroutine(m_ProgressAnimCoroutine);
                m_ProgressAnimCoroutine = null;
            }
        }

        void Start()
        {
            s_ignoreClicksUntil = Mathf.Max(s_ignoreClicksUntil, Time.unscaledTime + 0.5f);

            // Deselect any selected UI element so Enter/Space doesn't re-trigger navigation
            if (UnityEngine.EventSystems.EventSystem.current != null)
            {
                UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
            }

            InitLoginIfPresent();
            InitMainMenuIfPresent();
            InitWorldSelectIfPresent();
            InitMissionSelectIfPresent();

            GameSession session = GameSession.Instance;
            if (session == null || !session.IsSignedIn) return;

            if (helloText != null) helloText.text = "Hello, " + session.StudentId + "!";
            if (pointsText != null) pointsText.text = session.Save.totalPoints.ToString();
        }

        void Update()
        {
            // Continuously and slowly spin the shining image behind the badge in the profile panel
            if (panelProfile != null && panelProfile.activeInHierarchy && badgeShineImage != null)
            {
                badgeShineImage.rectTransform.Rotate(Vector3.forward, -badgeShineSpinSpeed * Time.unscaledDeltaTime);
            }
        }

        void InitMainMenuIfPresent()
        {
            if (SceneManager.GetActiveScene().name != SceneNames.MainMenu) return;

            // Auto-locate MainMenu panels (even if inactive)
            if (panelHome == null) panelHome = FindPanelInScene("Panel_Home");
            if (panelChooseActivity == null) panelChooseActivity = FindPanelInScene("Panel_ChooseActivity");
            if (panelProfile == null) panelProfile = FindPanelInScene("Panel_Profile");
            if (panelProgress == null) panelProgress = FindPanelInScene("Panel_Progress");
            if (panelSettings == null) panelSettings = FindPanelInScene("Panel_Settings");

            // Wire Home screen buttons
            WireButton("Button_Play", ShowChooseActivity);
            WireButton("Button_Profile", ShowProfile);
            WireButton("Button_Progress", ShowProgress);
            WireButton("Button_Settings", ShowSettings);
            WireButton("Button_Logout", Logout);

            // Wire Choose Activity cards (even if panel was inactive at start)
            WireButton("Card_Missions", () => Go(SceneNames.WorldSelect));
            WireButton("Card_OnlineQuiz", () => Go(SceneNames.OnlineQuiz));

            // Wire Back buttons across panels in MainMenu
            WireBackButtons();

            // Setup Settings panel controls
            SetupSettingsControls();
        }

        void InitWorldSelectIfPresent()
        {
            if (SceneManager.GetActiveScene().name != SceneNames.WorldSelect) return;

            WireButton("Card_English", () => PickWorld("ENG"));
            WireButton("Card_Science", () => PickWorld("SCI"));
            WireButton("Card_PEHealth", () => PickWorld("PEH"));
            WireButton("BackButton", () => Go(SceneNames.MainMenu));
        }

        void InitLoginIfPresent()
        {
            if (SceneManager.GetActiveScene().name != SceneNames.Login) return;
            if (messageText != null)
            {
                messageText.text = "";
                messageText.gameObject.SetActive(false);
            }
        }

        void InitMissionSelectIfPresent()
        {
            if (SceneManager.GetActiveScene().name != SceneNames.MissionSelect) return;

            GameSession session = GameSession.Instance;
            string worldId = session != null ? session.SelectedWorldId : "ENG";

            // Update World Title for the chosen subject world
            if (missionWorldTitleText == null)
            {
                var titleGo = GameObject.Find("WorldTitle");
                if (titleGo != null) missionWorldTitleText = titleGo.GetComponent<TMP_Text>();
            }
            if (missionWorldTitleText != null)
            {
                switch (worldId)
                {
                    case "SCI":
                        missionWorldTitleText.text = "SciVenture\n(Science)";
                        break;
                    case "PEH":
                        missionWorldTitleText.text = "HealthQuest\n(PE & Health)";
                        break;
                    case "ENG":
                    default:
                        missionWorldTitleText.text = "LiteraQuest\n(English)";
                        break;
                }
            }

            if (missionLockSprite == null)
            {
                missionLockSprite = Resources.Load<Sprite>("Badges/lock")
                                 ?? FindFallbackSprite("lock");
            }
            if (missionFlagSprite == null)
            {
                missionFlagSprite = Resources.Load<Sprite>("Badges/flag")
                                 ?? FindFallbackSprite("flag");
            }

            SetupMissionNode(1, "Node_Mission1", mission1NodeImage);
            SetupMissionNode(2, "Node_Mission2", mission2NodeImage);
            SetupMissionNode(3, "Node_Mission3", mission3NodeImage);
            WireButton("BackButton", () => Go(SceneNames.WorldSelect));
        }

        void SetupMissionNode(int missionNumber, string nodeName, Image inspectorNodeImage)
        {
            GameSession session = GameSession.Instance;
            string worldId = session != null ? session.SelectedWorldId : "ENG";
            bool unlocked = session != null && session.IsMissionUnlocked(worldId, missionNumber);

            // 1. If assigned in Inspector, update that UI Image component directly
            if (inspectorNodeImage != null)
            {
                inspectorNodeImage.sprite = unlocked ? (missionFlagSprite ?? inspectorNodeImage.sprite) : (missionLockSprite ?? inspectorNodeImage.sprite);
                inspectorNodeImage.preserveAspect = true;
                inspectorNodeImage.gameObject.SetActive(true);
            }

            GameObject node = GameObject.Find(nodeName);
            if (node == null)
            {
                var allTr = Resources.FindObjectsOfTypeAll<Transform>();
                for (int i = 0; i < allTr.Length; i++)
                {
                    if (allTr[i].gameObject.scene == gameObject.scene && allTr[i].gameObject.name == nodeName)
                    {
                        node = allTr[i].gameObject;
                        break;
                    }
                }
            }

            if (node != null)
            {
                // Fallback resolution if inspector slot wasn't assigned
                if (inspectorNodeImage == null)
                {
                    Transform lockChild = node.transform.Find("LockIcon");
                    Transform flagChild = node.transform.Find("Flag");

                    if (lockChild != null && flagChild != null)
                    {
                        lockChild.gameObject.SetActive(!unlocked);
                        flagChild.gameObject.SetActive(unlocked);

                        var lockImg = lockChild.GetComponent<Image>();
                        if (lockImg != null && missionLockSprite != null)
                        {
                            lockImg.sprite = missionLockSprite;
                            lockImg.preserveAspect = true;
                        }
                        var flagImg = flagChild.GetComponent<Image>();
                        if (flagImg != null && missionFlagSprite != null)
                        {
                            flagImg.sprite = missionFlagSprite;
                            flagImg.preserveAspect = true;
                        }
                    }
                    else if (lockChild != null)
                    {
                        var img = lockChild.GetComponent<Image>();
                        if (img != null)
                        {
                            img.sprite = unlocked ? (missionFlagSprite ?? img.sprite) : (missionLockSprite ?? img.sprite);
                            img.preserveAspect = true;
                        }
                        lockChild.gameObject.SetActive(true);
                    }
                    else if (flagChild != null)
                    {
                        var img = flagChild.GetComponent<Image>();
                        if (img != null)
                        {
                            img.sprite = unlocked ? (missionFlagSprite ?? img.sprite) : (missionLockSprite ?? img.sprite);
                            img.preserveAspect = true;
                        }
                        flagChild.gameObject.SetActive(true);
                    }
                }

                var btn = node.GetComponent<Button>();
                if (btn != null)
                {
                    btn.onClick.RemoveAllListeners();
                    if (unlocked)
                    {
                        btn.onClick.AddListener(() => PickMission(missionNumber));
                    }
                    else
                    {
                        btn.onClick.AddListener(() => ShowFloatingMessage("Mission Locked"));
                    }
                }
            }
            else if (inspectorNodeImage != null)
            {
                var btn = inspectorNodeImage.GetComponentInParent<Button>();
                if (btn != null)
                {
                    btn.onClick.RemoveAllListeners();
                    if (unlocked)
                    {
                        btn.onClick.AddListener(() => PickMission(missionNumber));
                    }
                    else
                    {
                        btn.onClick.AddListener(() => ShowFloatingMessage("Mission Locked"));
                    }
                }
            }
        }

        Coroutine m_FloatingMsgCoroutine;
        GameObject m_FloatingToastInstance;

        public void ShowFloatingMessage(string message)
        {
            if (m_FloatingMsgCoroutine != null)
            {
                StopCoroutine(m_FloatingMsgCoroutine);
                m_FloatingMsgCoroutine = null;
            }
            m_FloatingMsgCoroutine = StartCoroutine(FloatingMessageRoutine(message));
        }

        IEnumerator FloatingMessageRoutine(string message)
        {
            Canvas canvas = FindAnyObjectByType<Canvas>();
            if (canvas == null) yield break;

            if (m_FloatingToastInstance == null)
            {
                m_FloatingToastInstance = new GameObject("FloatingToast");
                m_FloatingToastInstance.transform.SetParent(canvas.transform, false);

                var rt = m_FloatingToastInstance.AddComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(360f, 60f);

                var bg = m_FloatingToastInstance.AddComponent<Image>();
                bg.color = new Color(0.1f, 0.1f, 0.1f, 0.88f);

                var textGo = new GameObject("Text");
                textGo.transform.SetParent(m_FloatingToastInstance.transform, false);
                var textRt = textGo.AddComponent<RectTransform>();
                textRt.anchorMin = Vector2.zero;
                textRt.anchorMax = Vector2.one;
                textRt.offsetMin = Vector2.zero;
                textRt.offsetMax = Vector2.zero;

                var tmp = textGo.AddComponent<TextMeshProUGUI>();
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.fontSize = 28;
                tmp.fontStyle = FontStyles.Bold;
                tmp.color = new Color(1f, 0.9f, 0.3f, 1f);
            }

            m_FloatingToastInstance.SetActive(true);
            m_FloatingToastInstance.transform.SetAsLastSibling();
            var toastRt = m_FloatingToastInstance.GetComponent<RectTransform>();
            var toastImg = m_FloatingToastInstance.GetComponent<Image>();
            var toastTmp = m_FloatingToastInstance.GetComponentInChildren<TextMeshProUGUI>();

            if (toastTmp != null) toastTmp.text = message;

            Vector2 startPos = new Vector2(0f, -40f);
            Vector2 endPos = new Vector2(0f, 60f);
            float duration = 1.6f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float norm = Mathf.Clamp01(elapsed / duration);

                float moveEase = 1f - Mathf.Pow(1f - norm, 3f);
                toastRt.anchoredPosition = Vector2.Lerp(startPos, endPos, moveEase);

                float alpha = norm > 0.6f ? Mathf.Lerp(1f, 0f, (norm - 0.6f) / 0.4f) : 1f;

                if (toastImg != null) toastImg.color = new Color(0.1f, 0.1f, 0.1f, 0.88f * alpha);
                if (toastTmp != null) toastTmp.color = new Color(1f, 0.9f, 0.3f, alpha);

                yield return null;
            }

            m_FloatingToastInstance.SetActive(false);
            m_FloatingMsgCoroutine = null;
        }

        GameObject FindPanelInScene(string panelName)
        {
            var go = GameObject.Find(panelName);
            if (go != null) return go;

            var allTransforms = Resources.FindObjectsOfTypeAll<Transform>();
            for (int i = 0; i < allTransforms.Length; i++)
            {
                if (allTransforms[i].gameObject.scene == gameObject.scene && allTransforms[i].gameObject.name == panelName)
                {
                    return allTransforms[i].gameObject;
                }
            }
            return null;
        }

        void WireButton(string goName, UnityEngine.Events.UnityAction action)
        {
            var allButtons = Resources.FindObjectsOfTypeAll<Button>();
            for (int i = 0; i < allButtons.Length; i++)
            {
                if (allButtons[i].gameObject.scene == gameObject.scene && allButtons[i].gameObject.name == goName)
                {
                    allButtons[i].onClick.RemoveAllListeners();
                    allButtons[i].onClick.AddListener(action);
                    return;
                }
            }
        }

        void WireBackButtons()
        {
            if (SceneManager.GetActiveScene().name != SceneNames.MainMenu) return;

            var allButtons = Resources.FindObjectsOfTypeAll<Button>();
            for (int i = 0; i < allButtons.Length; i++)
            {
                if (allButtons[i].name == "BackButton" && allButtons[i].gameObject.scene == gameObject.scene)
                {
                    allButtons[i].onClick.RemoveAllListeners();
                    allButtons[i].onClick.AddListener(ShowHome);
                }
            }
        }

        void SetupSettingsControls()
        {
            if (panelSettings == null) return;

            // Music Slider
            var sliderMusic = panelSettings.transform.Find("PanelBackground/Slider_Music")?.GetComponent<Slider>()
                           ?? GameObject.Find("Slider_Music")?.GetComponent<Slider>();
            if (sliderMusic != null)
            {
                sliderMusic.value = GameSettings.MusicVolume;
                sliderMusic.onValueChanged.RemoveAllListeners();
                sliderMusic.onValueChanged.AddListener(v => GameSettings.SetMusicVolume(v));
            }

            // Effects Slider
            var sliderEffects = panelSettings.transform.Find("PanelBackground/Slider_Effects")?.GetComponent<Slider>()
                             ?? GameObject.Find("Slider_Effects")?.GetComponent<Slider>();
            if (sliderEffects != null)
            {
                sliderEffects.value = GameSettings.SfxVolume;
                sliderEffects.onValueChanged.RemoveAllListeners();
                sliderEffects.onValueChanged.AddListener(v => GameSettings.SetSfxVolume(v));
            }

            // Text Size Dropdown
            var dropdownText = panelSettings.transform.Find("PanelBackground/Dropdown_TextSize")?.GetComponent<TMP_Dropdown>()
                            ?? GameObject.Find("Dropdown_TextSize")?.GetComponent<TMP_Dropdown>();
            if (dropdownText != null)
            {
                dropdownText.ClearOptions();
                dropdownText.AddOptions(new List<string> { "Small (80%)", "Normal (100%)", "Large (120%)" });
                dropdownText.value = GameSettings.TextSizeIndex;
                dropdownText.onValueChanged.RemoveAllListeners();
                dropdownText.onValueChanged.AddListener(idx => GameSettings.SetTextSize(idx));
            }
        }

        // ---------- Panel Switchers ----------

        public void ShowHome()
        {
            if (ClickIgnored("ShowHome")) return;
            SetPanel(panelHome, true);
            SetPanel(panelChooseActivity, false);
            SetPanel(panelProfile, false);
            SetPanel(panelProgress, false);
            SetPanel(panelSettings, false);

            // Refresh points on home
            if (pointsText != null && GameSession.Instance != null && GameSession.Instance.Save != null)
            {
                pointsText.text = GameSession.Instance.Save.totalPoints.ToString();
            }
        }

        public void ShowChooseActivity()
        {
            if (ClickIgnored("ShowChooseActivity")) return;
            SetPanel(panelHome, false);
            SetPanel(panelChooseActivity, true);
            SetPanel(panelProfile, false);
            SetPanel(panelProgress, false);
            SetPanel(panelSettings, false);
        }

        public void ShowProfile()
        {
            if (ClickIgnored("ShowProfile")) return;
            SetPanel(panelHome, false);
            SetPanel(panelChooseActivity, false);
            SetPanel(panelProfile, true);
            SetPanel(panelProgress, false);
            SetPanel(panelSettings, false);

            PopulateProfileData();
        }

        public void ShowProgress()
        {
            if (ClickIgnored("ShowProgress")) return;
            SetPanel(panelHome, false);
            SetPanel(panelChooseActivity, false);
            SetPanel(panelProfile, false);
            SetPanel(panelProgress, true);
            SetPanel(panelSettings, false);

            PopulateProgressData();
        }

        public void ShowSettings()
        {
            if (ClickIgnored("ShowSettings")) return;
            SetPanel(panelHome, false);
            SetPanel(panelChooseActivity, false);
            SetPanel(panelProfile, false);
            SetPanel(panelProgress, false);
            SetPanel(panelSettings, true);

            SetupSettingsControls();
        }

        void SetPanel(GameObject panel, bool active)
        {
            if (panel != null) panel.SetActive(active);
        }

        // ---------- Profile View Population ----------

        void InitProfileBindings()
        {
            if (panelProfile == null) return;

            // Remove any runtime card overlay artifact if it was created
            Transform oldRoot = panelProfile.transform.Find("ProfileContentRoot");
            if (oldRoot != null) Destroy(oldRoot.gameObject);

            // Auto-detect current badge Image if not assigned in inspector
            if (currentBadgeImage == null)
            {
                currentBadgeImage = panelProfile.transform.Find("Badge/Image")?.GetComponent<Image>()
                                 ?? panelProfile.transform.Find("Badge")?.GetComponentInChildren<Image>();
            }

            // Auto-detect shining background image behind badge if not assigned in inspector
            if (badgeShineImage == null)
            {
                badgeShineImage = panelProfile.transform.Find("Badge/ShineProp")?.GetComponent<Image>()
                               ?? panelProfile.transform.Find("Badge/Shine")?.GetComponent<Image>()
                               ?? panelProfile.transform.Find("ShineProp")?.GetComponent<Image>();
            }

            // Auto-detect separated rank title & level texts
            if (rankNameText == null)
            {
                rankNameText = panelProfile.transform.Find("RankName/Data")?.GetComponent<TMP_Text>()
                            ?? panelProfile.transform.Find("RankName")?.GetComponent<TMP_Text>()
                            ?? panelProfile.transform.Find("RankTitle/Data")?.GetComponent<TMP_Text>()
                            ?? panelProfile.transform.Find("RankTitle")?.GetComponent<TMP_Text>()
                            ?? panelProfile.transform.Find("Rank/RankName")?.GetComponent<TMP_Text>();
            }
            if (rankNumberText == null)
            {
                rankNumberText = panelProfile.transform.Find("RankNumber/Data")?.GetComponent<TMP_Text>()
                              ?? panelProfile.transform.Find("RankNumber")?.GetComponent<TMP_Text>()
                              ?? panelProfile.transform.Find("RankLevel/Data")?.GetComponent<TMP_Text>()
                              ?? panelProfile.transform.Find("RankLevel")?.GetComponent<TMP_Text>()
                              ?? panelProfile.transform.Find("Rank/RankNumber")?.GetComponent<TMP_Text>();
            }

            // Auto-detect legacy/combined current badge Text Details if not assigned in inspector
            if (currentBadgeText == null && rankNameText == null)
            {
                currentBadgeText = panelProfile.transform.Find("Rank/Data")?.GetComponent<TMP_Text>()
                                ?? panelProfile.transform.Find("Rank")?.GetComponentInChildren<TMP_Text>();
            }

            // Auto-detect Student ID, Grade Level, Total Score texts
            if (studentIdText == null)
            {
                studentIdText = panelProfile.transform.Find("StudentID/Data")?.GetComponent<TMP_Text>()
                             ?? panelProfile.transform.Find("StudentID")?.GetComponentInChildren<TMP_Text>();
            }
            if (gradeLevelText == null)
            {
                gradeLevelText = panelProfile.transform.Find("GradeLvl/Data")?.GetComponent<TMP_Text>()
                              ?? panelProfile.transform.Find("GradeLvl")?.GetComponentInChildren<TMP_Text>();
            }
            if (totalScoreText == null)
            {
                totalScoreText = panelProfile.transform.Find("TotalScore/Data")?.GetComponent<TMP_Text>()
                              ?? panelProfile.transform.Find("TotalScore")?.GetComponentInChildren<TMP_Text>();
            }

            // Auto-detect Academic & Quiz statistics texts
            if (missionsCompletedText == null)
            {
                missionsCompletedText = panelProfile.transform.Find("MissionsCompleted/Data")?.GetComponent<TMP_Text>()
                                     ?? panelProfile.transform.Find("MissionsCompleted")?.GetComponentInChildren<TMP_Text>()
                                     ?? panelProfile.transform.Find("Missions/Data")?.GetComponent<TMP_Text>();
            }
            if (stationsMasteredText == null)
            {
                stationsMasteredText = panelProfile.transform.Find("StationsMastered/Data")?.GetComponent<TMP_Text>()
                                    ?? panelProfile.transform.Find("StationsMastered")?.GetComponentInChildren<TMP_Text>()
                                    ?? panelProfile.transform.Find("Stations/Data")?.GetComponent<TMP_Text>();
            }
            if (checkpointsCountText == null)
            {
                checkpointsCountText = panelProfile.transform.Find("Checkpoints/Data")?.GetComponent<TMP_Text>()
                                    ?? panelProfile.transform.Find("Checkpoints")?.GetComponentInChildren<TMP_Text>();
            }
            if (quizzesCompletedText == null)
            {
                quizzesCompletedText = panelProfile.transform.Find("QuizzesCompleted/Data")?.GetComponent<TMP_Text>()
                                    ?? panelProfile.transform.Find("QuizzesCompleted")?.GetComponentInChildren<TMP_Text>()
                                    ?? panelProfile.transform.Find("QuizzesTaken/Data")?.GetComponent<TMP_Text>()
                                    ?? panelProfile.transform.Find("QuizzesTaken")?.GetComponentInChildren<TMP_Text>();
            }
            if (quizzesPassedText == null)
            {
                quizzesPassedText = panelProfile.transform.Find("QuizzesPassed/Data")?.GetComponent<TMP_Text>()
                                 ?? panelProfile.transform.Find("QuizzesPassed")?.GetComponentInChildren<TMP_Text>();
            }
            if (bestQuizScoreText == null)
            {
                bestQuizScoreText = panelProfile.transform.Find("BestQuizScore/Data")?.GetComponent<TMP_Text>()
                                 ?? panelProfile.transform.Find("BestQuizScore")?.GetComponentInChildren<TMP_Text>()
                                 ?? panelProfile.transform.Find("HighestQuizScore/Data")?.GetComponent<TMP_Text>();
            }

            // Auto-detect collected badges container
            if (collectedBadgesContainer == null)
            {
                collectedBadgesContainer = panelProfile.transform.Find("Collection/BadgesGrid")
                                        ?? panelProfile.transform.Find("Collection/Content")
                                        ?? panelProfile.transform.Find("Collection");
            }
        }

        void PopulateProfileData()
        {
            if (panelProfile == null) return;
            var session = GameSession.Instance;
            if (session == null || !session.IsSignedIn) return;

            InitProfileBindings();

            // 1. Current Badge Image & Separated Rank / Level Details
            int level = session.GetStudentLevel();
            string rankTitle = session.GetStudentRankTitle();

            if (currentBadgeImage != null)
            {
                Sprite rankSprite = GetRankBadgeSprite(level);
                currentBadgeImage.sprite = rankSprite;
                currentBadgeImage.preserveAspect = true;
                currentBadgeImage.gameObject.SetActive(true);
            }

            // Separated Rank Name and Rank Number
            if (rankNameText != null)
            {
                rankNameText.text = rankTitle;
            }
            if (rankNumberText != null)
            {
                rankNumberText.text = prefixLevelWithText ? $"Level {level}" : level.ToString();
            }

            // Combined/Legacy Rank text
            if (currentBadgeText != null)
            {
                currentBadgeText.text = rankTitle;
            }

            // 2. Student Info Details
            if (studentIdText != null) studentIdText.text = session.StudentId;
            if (gradeLevelText != null) gradeLevelText.text = $"Grade {session.Grade}";
            if (totalScoreText != null) totalScoreText.text = $"{session.Save.totalPoints} PTS";

            // 3. Academic & Quiz Statistics
            int missionsDone = session.GetCompletedMissionsCount();
            int checkpointsDone = session.GetTotalCompletedCheckpointsCount();
            int quizzesTaken = session.Save.totalQuizzesTaken;
            int quizzesPassed = session.Save.totalQuizzesPassed;
            int bestScore = session.Save.highestQuizScore;

            if (missionsCompletedText != null) missionsCompletedText.text = $"{missionsDone} / 9";
            if (stationsMasteredText != null) stationsMasteredText.text = $"{checkpointsDone} / 27";
            if (checkpointsCountText != null) checkpointsCountText.text = $"{checkpointsDone} / 27";
            if (quizzesCompletedText != null) quizzesCompletedText.text = $"{quizzesTaken}";
            if (quizzesPassedText != null) quizzesPassedText.text = $"{quizzesPassed}";
            if (bestQuizScoreText != null) bestQuizScoreText.text = $"{bestScore} PTS";

            // 4. Collected Badges (using the user's collectedBadgePrefab)
            if (collectedBadgesContainer != null)
            {
                // Clean up previously instantiated badge clones
                for (int i = collectedBadgesContainer.childCount - 1; i >= 0; i--)
                {
                    Transform child = collectedBadgesContainer.GetChild(i);
                    if (child.name.StartsWith("CollectedBadge_"))
                    {
                        Destroy(child.gameObject);
                    }
                }

                var badges = GetCollectedBadges(session);

                if (collectedBadgePrefab != null)
                {
                    // Hide any static placeholder Image objects in the container so prefab clones show cleanly
                    HidePlaceholderChildren(collectedBadgesContainer);

                    for (int i = 0; i < badges.Count; i++)
                    {
                        string bName = badges[i];
                        GameObject badgeInstance = Instantiate(collectedBadgePrefab, collectedBadgesContainer);
                        badgeInstance.name = "CollectedBadge_" + i;
                        badgeInstance.SetActive(true);

                        // Flexible Image resolution:
                        // 1. Direct child named "Icon", "Image", "BadgeIcon", "Badge"
                        // 2. Child Image components (ignoring root container image if multiple exist)
                        Image img = null;
                        Transform iconTr = badgeInstance.transform.Find("Icon")
                                        ?? badgeInstance.transform.Find("Image")
                                        ?? badgeInstance.transform.Find("BadgeIcon")
                                        ?? badgeInstance.transform.Find("Badge");
                        if (iconTr != null)
                        {
                            img = iconTr.GetComponent<Image>();
                        }
                        if (img == null)
                        {
                            var allImgs = badgeInstance.GetComponentsInChildren<Image>(true);
                            if (allImgs != null && allImgs.Length > 0)
                            {
                                img = (allImgs.Length > 1 && allImgs[0].gameObject == badgeInstance) ? allImgs[1] : allImgs[0];
                            }
                        }

                        if (img != null)
                        {
                            img.sprite = GetBadgeSprite(bName);
                            img.preserveAspect = true;
                        }

                        // Flexible TMP_Text resolution:
                        // 1. Direct child named "Text (TMP)", "Text", "Label", "Name", "Title"
                        // 2. Any TMP_Text in children
                        TMP_Text label = null;
                        Transform textTr = badgeInstance.transform.Find("Text (TMP)")
                                        ?? badgeInstance.transform.Find("Text")
                                        ?? badgeInstance.transform.Find("Label")
                                        ?? badgeInstance.transform.Find("Name")
                                        ?? badgeInstance.transform.Find("Title");
                        if (textTr != null)
                        {
                            label = textTr.GetComponent<TMP_Text>();
                        }
                        if (label == null)
                        {
                            label = badgeInstance.GetComponentInChildren<TMP_Text>(true);
                        }

                        if (label != null)
                        {
                            label.text = FormatBadgeDisplayName(bName);
                        }
                    }
                }
                else
                {
                    // If no prefab assigned yet: populate existing placeholder images in collection
                    PopulateExistingCollectionPlaceholders(collectedBadgesContainer, badges);
                }
            }
        }

        void HidePlaceholderChildren(Transform container)
        {
            for (int i = 0; i < container.childCount; i++)
            {
                Transform c = container.GetChild(i);
                // Do not hide headers or titles
                if (c.GetComponent<TMP_Text>() != null) continue;
                if (c.name.StartsWith("Image"))
                {
                    c.gameObject.SetActive(false);
                }
            }
        }

        void PopulateExistingCollectionPlaceholders(Transform container, List<string> badges)
        {
            int badgeIdx = 0;
            for (int i = 0; i < container.childCount; i++)
            {
                Transform c = container.GetChild(i);
                if (c.GetComponent<TMP_Text>() != null && c.name != "Data") continue;

                var img = c.GetComponent<Image>() ?? c.GetComponentInChildren<Image>();
                if (img != null && c.name.StartsWith("Image"))
                {
                    if (badgeIdx < badges.Count)
                    {
                        string bName = badges[badgeIdx++];
                        img.sprite = GetBadgeSprite(bName);
                        img.preserveAspect = true;
                        img.color = Color.white;
                        c.gameObject.SetActive(true);

                        var txt = c.GetComponentInChildren<TMP_Text>();
                        if (txt != null) txt.text = FormatBadgeDisplayName(bName);
                    }
                    else
                    {
                        img.color = new Color(1f, 1f, 1f, 0.25f);
                    }
                }
            }
        }

        List<string> GetCollectedBadges(GameSession session)
        {
            var badges = new List<string>();
            if (session == null || session.Save == null) return badges;

            // Check save rewards and filter out checkpoint rewards (leaves/clues)
            if (session.Save.collectedRewards != null)
            {
                for (int i = 0; i < session.Save.collectedRewards.Count; i++)
                {
                    string item = session.Save.collectedRewards[i];
                    if (IsBadgeItem(item) && !badges.Contains(item))
                    {
                        badges.Add(item);
                    }
                }
            }

            // Also check completed missions from progress
            if (session.Save.missions != null)
            {
                for (int i = 0; i < session.Save.missions.Count; i++)
                {
                    var mp = session.Save.missions[i];
                    if (mp != null && mp.finalChallengeComplete)
                    {
                        string bName = GetMissionBadgeName(mp.missionId);
                        if (!string.IsNullOrEmpty(bName) && !badges.Contains(bName))
                        {
                            badges.Add(bName);
                        }
                    }
                }
            }

            return badges;
        }

        bool IsBadgeItem(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return false;
            string lower = raw.ToLowerInvariant();
            // Checkpoint rewards are leaves, cards, pages, clues, shards
            if (lower.Contains("leaf") || lower.Contains("card") || lower.Contains("clue") ||
                lower.Contains("shard") || lower.Contains("page") || lower.Contains("token") || lower.Contains("lens"))
            {
                return false;
            }
            return true;
        }

        string GetMissionBadgeName(string missionId)
        {
            if (string.IsNullOrEmpty(missionId)) return "Mastery Badge";
            if (missionId.Contains("ENG"))
            {
                if (missionId.Contains("M1")) return "Golden Lantern Badge";
                if (missionId.Contains("M2")) return "Wisdom Crystal";
                if (missionId.Contains("M3")) return "Explorer's Atlas Badge";
                return "LiteraQuest Badge";
            }
            if (missionId.Contains("SCI"))
            {
                if (missionId.Contains("M1")) return "Matter Master Badge";
                if (missionId.Contains("M2")) return "Eco Guardian Badge";
                if (missionId.Contains("M3")) return "Earth Explorer Badge";
                return "SciVenture Badge";
            }
            if (missionId.Contains("PEH"))
            {
                if (missionId.Contains("M1")) return "Fitness Champion Badge";
                if (missionId.Contains("M2")) return "Wellness Pioneer Badge";
                if (missionId.Contains("M3")) return "Active Hero Badge";
                return "HealthQuest Badge";
            }
            return "Mission Mastery Badge";
        }

        string FormatBadgeDisplayName(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return "Badge";
            string s = raw.Trim();
            if (s.IndexOf("lantern", StringComparison.OrdinalIgnoreCase) >= 0)
                return "Golden\nLantern";
            if (s.IndexOf("crystal", StringComparison.OrdinalIgnoreCase) >= 0)
                return "Wisdom\nCrystal";
            if (s.IndexOf("atlas", StringComparison.OrdinalIgnoreCase) >= 0)
                return "Explorer's\nAtlas";
            if (s.IndexOf("reader", StringComparison.OrdinalIgnoreCase) >= 0)
                return "Critical\nReader";
            if (s.IndexOf("investigator", StringComparison.OrdinalIgnoreCase) >= 0)
                return "Persuasion\nBadge";
            if (s.IndexOf("communicator", StringComparison.OrdinalIgnoreCase) >= 0)
                return "Communicator\nBadge";
            if (s.IndexOf("mastery", StringComparison.OrdinalIgnoreCase) >= 0)
                return s.Replace("Badge", "").Trim() + "\nBadge";

            s = s.Replace("Badge", "").Trim();
            return s.Length > 15 ? s.Substring(0, 13) + "..." : s;
        }

        // ---------- Progress View Population ----------

        void InitProgressBindings()
        {
            if (panelProgress == null) return;

            // Remove any old runtime-generated preview artifact
            Transform oldRoot = panelProgress.transform.Find("ProgressContentRoot");
            if (oldRoot != null) Destroy(oldRoot.gameObject);

            // LiteraQuest (English)
            Transform eng = panelProgress.transform.Find("EngPanel");
            if (eng != null)
            {
                if (engPercentText == null) engPercentText = eng.Find("Percent")?.GetComponent<TMP_Text>();
                if (engCountText == null) engCountText = eng.Find("Counter")?.GetComponent<TMP_Text>();
                if (engProgressBar == null)
                {
                    engProgressBar = eng.Find("ProgressBar/Image")?.GetComponent<Image>()
                                  ?? eng.Find("ProgressBar")?.GetComponentInChildren<Image>();
                }
            }

            // SciVenture (Science)
            Transform sci = panelProgress.transform.Find("SciPanel");
            if (sci != null)
            {
                if (sciPercentText == null) sciPercentText = sci.Find("Percent")?.GetComponent<TMP_Text>();
                if (sciCountText == null) sciCountText = sci.Find("Counter")?.GetComponent<TMP_Text>();
                if (sciProgressBar == null)
                {
                    sciProgressBar = sci.Find("ProgressBar/Image")?.GetComponent<Image>()
                                  ?? sci.Find("ProgressBar")?.GetComponentInChildren<Image>();
                }
            }

            // HealthQuest (PE & Health)
            Transform peh = panelProgress.transform.Find("PehPanel");
            if (peh != null)
            {
                if (pehPercentText == null) pehPercentText = peh.Find("Percent")?.GetComponent<TMP_Text>();
                if (pehCountText == null) pehCountText = peh.Find("Counter")?.GetComponent<TMP_Text>();
                if (pehProgressBar == null)
                {
                    pehProgressBar = peh.Find("ProgressBar/Image")?.GetComponent<Image>()
                                  ?? peh.Find("ProgressBar")?.GetComponentInChildren<Image>();
                }
            }

            // Bottom Panel
            Transform btm = panelProgress.transform.Find("BottomPanel");
            if (btm != null)
            {
                if (overallCompletionText == null)
                {
                    overallCompletionText = btm.Find("OverallText")?.GetComponent<TMP_Text>()
                                         ?? btm.Find("Text_Overall")?.GetComponent<TMP_Text>()
                                         ?? btm.Find("Overall")?.GetComponent<TMP_Text>()
                                         ?? btm.GetComponentInChildren<TMP_Text>();
                }
                if (totalPointsEarnedText == null)
                {
                    totalPointsEarnedText = btm.Find("PointsText")?.GetComponent<TMP_Text>()
                                         ?? btm.Find("Text_Points")?.GetComponent<TMP_Text>()
                                         ?? btm.Find("TotalEarned")?.GetComponent<TMP_Text>()
                                         ?? btm.Find("Points")?.GetComponent<TMP_Text>();
                }
            }
        }

        void PopulateProgressData()
        {
            if (panelProgress == null) return;
            var session = GameSession.Instance;
            if (session == null || !session.IsSignedIn) return;

            InitProgressBindings();

            int engCompleted = CountWorldCheckpoints("ENG");
            int sciCompleted = CountWorldCheckpoints("SCI");
            int pehCompleted = CountWorldCheckpoints("PEH");
            int totalStations = engCompleted + sciCompleted + pehCompleted;
            int totalPossible = 27;

            int targetEngPct = Mathf.RoundToInt((float)engCompleted / 9f * 100f);
            int targetSciPct = Mathf.RoundToInt((float)sciCompleted / 9f * 100f);
            int targetPehPct = Mathf.RoundToInt((float)pehCompleted / 9f * 100f);
            int targetOverallPct = Mathf.RoundToInt((float)totalStations / totalPossible * 100f);
            int targetPoints = session.Save.totalPoints;

            // Set static count labels immediately
            if (engCountText != null) engCountText.text = $"{engCompleted} of 9 Checkpoints";
            if (sciCountText != null) sciCountText.text = $"{sciCompleted} of 9 Checkpoints";
            if (pehCountText != null) pehCountText.text = $"{pehCompleted} of 9 Checkpoints";

            // Stop any ongoing animation before starting a new one
            if (m_ProgressAnimCoroutine != null)
            {
                StopCoroutine(m_ProgressAnimCoroutine);
                m_ProgressAnimCoroutine = null;
            }

            if (gameObject.activeInHierarchy)
            {
                m_ProgressAnimCoroutine = StartCoroutine(AnimateProgressBars(
                    targetEngPct, engCompleted / 9f,
                    targetSciPct, sciCompleted / 9f,
                    targetPehPct, pehCompleted / 9f,
                    targetOverallPct, totalStations, totalPossible,
                    targetPoints
                ));
            }
            else
            {
                SetProgressInstant(
                    targetEngPct, engCompleted / 9f,
                    targetSciPct, sciCompleted / 9f,
                    targetPehPct, pehCompleted / 9f,
                    targetOverallPct, totalStations, totalPossible,
                    targetPoints
                );
            }
        }

        IEnumerator AnimateProgressBars(
            int targetEngPct, float targetEngFill,
            int targetSciPct, float targetSciFill,
            int targetPehPct, float targetPehFill,
            int targetOverallPct, int totalStations, int totalPossible,
            int targetPoints)
        {
            float duration = Mathf.Max(0.1f, progressAnimationDuration);
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                // Cubic ease-out: f(t) = 1 - (1 - t)^3
                float easeOut = 1f - Mathf.Pow(1f - t, 3f);

                // LiteraQuest (English)
                int curEngPct = Mathf.RoundToInt(Mathf.Lerp(0, targetEngPct, easeOut));
                if (engPercentText != null) engPercentText.text = $"{curEngPct}%";
                if (engProgressBar != null) engProgressBar.fillAmount = Mathf.Lerp(0f, targetEngFill, easeOut);

                // SciVenture (Science)
                int curSciPct = Mathf.RoundToInt(Mathf.Lerp(0, targetSciPct, easeOut));
                if (sciPercentText != null) sciPercentText.text = $"{curSciPct}%";
                if (sciProgressBar != null) sciProgressBar.fillAmount = Mathf.Lerp(0f, targetSciFill, easeOut);

                // HealthQuest (PE & Health)
                int curPehPct = Mathf.RoundToInt(Mathf.Lerp(0, targetPehPct, easeOut));
                if (pehPercentText != null) pehPercentText.text = $"{curPehPct}%";
                if (pehProgressBar != null) pehProgressBar.fillAmount = Mathf.Lerp(0f, targetPehFill, easeOut);

                // Overall completion at bottom
                int curOverallPct = Mathf.RoundToInt(Mathf.Lerp(0, targetOverallPct, easeOut));
                if (overallCompletionText != null)
                {
                    overallCompletionText.text = $"{curOverallPct}% ({totalStations} / {totalPossible} Checkpoints Cleared)";
                }

                // Total earned points at bottom
                int curPoints = Mathf.RoundToInt(Mathf.Lerp(0, targetPoints, easeOut));
                if (totalPointsEarnedText != null)
                {
                    totalPointsEarnedText.text = $"{curPoints} PTS";
                }

                yield return null;
            }

            // Snap to final values
            SetProgressInstant(
                targetEngPct, targetEngFill,
                targetSciPct, targetSciFill,
                targetPehPct, targetPehFill,
                targetOverallPct, totalStations, totalPossible,
                targetPoints
            );
            m_ProgressAnimCoroutine = null;
        }

        void SetProgressInstant(
            int targetEngPct, float targetEngFill,
            int targetSciPct, float targetSciFill,
            int targetPehPct, float targetPehFill,
            int targetOverallPct, int totalStations, int totalPossible,
            int targetPoints)
        {
            if (engPercentText != null) engPercentText.text = $"{targetEngPct}%";
            if (engProgressBar != null) engProgressBar.fillAmount = targetEngFill;

            if (sciPercentText != null) sciPercentText.text = $"{targetSciPct}%";
            if (sciProgressBar != null) sciProgressBar.fillAmount = targetSciFill;

            if (pehPercentText != null) pehPercentText.text = $"{targetPehPct}%";
            if (pehProgressBar != null) pehProgressBar.fillAmount = targetPehFill;

            if (overallCompletionText != null)
            {
                overallCompletionText.text = $"{targetOverallPct}% ({totalStations} / {totalPossible} Checkpoints Cleared)";
            }

            if (totalPointsEarnedText != null)
            {
                totalPointsEarnedText.text = $"{targetPoints} PTS";
            }
        }

        int CountWorldCheckpoints(string worldId)
        {
            var session = GameSession.Instance;
            if (session == null || session.Save == null) return 0;
            int count = 0;
            for (int i = 0; i < session.Save.missions.Count; i++)
            {
                var m = session.Save.missions[i];
                if (m.missionId.Contains(worldId))
                {
                    for (int c = 0; c < m.checkpoints.Count; c++)
                    {
                        if (m.checkpoints[c].completed) count++;
                    }
                }
            }
            return count;
        }

        Sprite GetBadgeSprite(string badgeName)
        {
            if (string.IsNullOrEmpty(badgeName))
                return Resources.Load<Sprite>("Badges/medal") ?? FindFallbackSprite("medal", "star");

            // 1. Try loading direct or formatted name from Resources/Badges/
            string rawClean = badgeName.Trim();
            Sprite direct = Resources.Load<Sprite>("Badges/" + rawClean);
            if (direct != null) return direct;

            string slug = rawClean.ToLowerInvariant().Replace(" ", "_").Replace("'", "").Replace("’", "").Replace("-", "_");
            direct = Resources.Load<Sprite>("Badges/" + slug);
            if (direct != null) return direct;

            string noSpaces = rawClean.ToLowerInvariant().Replace(" ", "").Replace("'", "").Replace("’", "").Replace("-", "");
            direct = Resources.Load<Sprite>("Badges/" + noSpaces);
            if (direct != null) return direct;

            string lower = rawClean.ToLowerInvariant();

            // 2. Specific mission badge keywords
            if (lower.Contains("lantern")) direct = Resources.Load<Sprite>("Badges/lantern") ?? Resources.Load<Sprite>("Badges/golden_lantern");
            else if (lower.Contains("crystal")) direct = Resources.Load<Sprite>("Badges/crystal") ?? Resources.Load<Sprite>("Badges/wisdom_crystal");
            else if (lower.Contains("atlas")) direct = Resources.Load<Sprite>("Badges/atlas") ?? Resources.Load<Sprite>("Badges/explorers_atlas");
            if (direct != null) return direct;

            // 3. Subject-based fallback to core sprites
            if (lower.Contains("sci") || lower.Contains("flask") || lower.Contains("matter") || lower.Contains("eco") || lower.Contains("earth"))
                return Resources.Load<Sprite>("Badges/sci") ?? FindFallbackSprite("sciicon", "sci");
            if (lower.Contains("peh") || lower.Contains("health") || lower.Contains("pe") || lower.Contains("fitness") || lower.Contains("wellness") || lower.Contains("hero"))
                return Resources.Load<Sprite>("Badges/peh") ?? FindFallbackSprite("peicon", "peh");
            if (lower.Contains("eng") || lower.Contains("litera") || lower.Contains("reader") || lower.Contains("communicator") || lower.Contains("investigator"))
                return Resources.Load<Sprite>("Badges/eng") ?? FindFallbackSprite("engicon", "eng");
            if (lower.Contains("leaf"))
                return Resources.Load<Sprite>("Badges/leaf") ?? FindFallbackSprite("checklisticon", "leaf");
            if (lower.Contains("star") || lower.Contains("rank"))
                return Resources.Load<Sprite>("Badges/star") ?? FindFallbackSprite("star");

            return Resources.Load<Sprite>("Badges/medal") ?? FindFallbackSprite("medal", "star");
        }

        Sprite GetRankBadgeSprite(int level)
        {
            // 1. Direct Inspector-assigned slots
            switch (level)
            {
                case 1: if (rank1BadgeSprite != null) return rank1BadgeSprite; break;
                case 2: if (rank2BadgeSprite != null) return rank2BadgeSprite; break;
                case 3: if (rank3BadgeSprite != null) return rank3BadgeSprite; break;
                case 4: if (rank4BadgeSprite != null) return rank4BadgeSprite; break;
                case 5: if (rank5BadgeSprite != null) return rank5BadgeSprite; break;
            }

            // 2. Inspector array fallback
            if (rankBadgeSprites != null && level - 1 >= 0 && level - 1 < rankBadgeSprites.Length)
            {
                if (rankBadgeSprites[level - 1] != null) return rankBadgeSprites[level - 1];
            }

            // 3. Try loading from Resources/Badges/ by rank level (e.g. rank1, rank_1, level1)
            Sprite direct = Resources.Load<Sprite>($"Badges/rank{level}")
                         ?? Resources.Load<Sprite>($"Badges/rank_{level}")
                         ?? Resources.Load<Sprite>($"Badges/level{level}")
                         ?? Resources.Load<Sprite>($"Badges/level_{level}");
            if (direct != null) return direct;

            // 4. Try loading from Resources/Badges/ by rank title slug
            var session = GameSession.Instance;
            if (session != null)
            {
                string rankTitle = session.GetStudentRankTitle();
                if (!string.IsNullOrEmpty(rankTitle))
                {
                    string slug = rankTitle.ToLowerInvariant().Replace(" ", "_").Replace("-", "_");
                    direct = Resources.Load<Sprite>($"Badges/{slug}") ?? Resources.Load<Sprite>($"Badges/{rankTitle}");
                    if (direct != null) return direct;
                }
            }

            // 5. Default fallback to core badges
            switch (level)
            {
                case 1:
                    return Resources.Load<Sprite>("Badges/leaf") ?? FindFallbackSprite("checklisticon", "leaf");
                case 2:
                    return Resources.Load<Sprite>("Badges/eng") ?? FindFallbackSprite("engicon", "eng");
                case 3:
                    return Resources.Load<Sprite>("Badges/sci") ?? FindFallbackSprite("sciicon", "sci");
                case 4:
                    return Resources.Load<Sprite>("Badges/peh") ?? FindFallbackSprite("peicon", "peh");
                case 5:
                default:
                    return Resources.Load<Sprite>("Badges/medal") ?? FindFallbackSprite("medal", "star");
            }
        }

        static Sprite FindFallbackSprite(params string[] names)
        {
            var all = Resources.FindObjectsOfTypeAll<Sprite>();
            for (int i = 0; i < all.Length; i++)
            {
                for (int c = 0; c < names.Length; c++)
                {
                    if (all[i].name.Equals(names[c], StringComparison.OrdinalIgnoreCase))
                        return all[i];
                }
            }
            return null;
        }

        // ---------- App Focus & Click Debounce ----------

        void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus) s_ignoreClicksUntil = Mathf.Max(s_ignoreClicksUntil, Time.unscaledTime + 0.5f);
        }

        bool ClickIgnored(string what)
        {
            if (Time.unscaledTime < s_ignoreClicksUntil)
            {
                Debug.Log("NutriMind: ignored '" + what + "' (debounce / cooldown active).");
                return true;
            }
            s_ignoreClicksUntil = Time.unscaledTime + 0.35f;
            return false;
        }

        public void Go(string sceneName)
        {
            if (ClickIgnored("Go " + sceneName)) return;
            s_ignoreClicksUntil = Time.unscaledTime + 0.6f;
            SceneManager.LoadScene(sceneName);
        }

        // ---------- Login scene ----------

        public void Login()
        {
            if (ClickIgnored("Login")) return;
            string id = studentIdInput != null ? studentIdInput.text : "";
            string password = passwordInput != null ? passwordInput.text : "";

            int grade;
            string studentId;
            AuthResult result = TestAccounts.TryLogin(id, password, out studentId, out grade);
            if (result != AuthResult.Ok)
            {
                if (messageText != null)
                {
                    messageText.text = TestAccounts.Describe(result);
                    messageText.color = new Color(1f, 0.45f, 0.45f, 1f); // Warning red tint
                    messageText.gameObject.SetActive(true);
                }
                return;
            }

            // Valid credentials -> Start login timer with pulsing fade animation
            StartCoroutine(LoggingInRoutine(studentId, grade));
        }

        IEnumerator LoggingInRoutine(string studentId, int grade)
        {
            s_ignoreClicksUntil = Time.unscaledTime + 5f; // Block spam clicks

            if (studentIdInput != null) studentIdInput.interactable = false;
            if (passwordInput != null) passwordInput.interactable = false;

            if (messageText != null)
            {
                messageText.text = "Logging in...";
                messageText.gameObject.SetActive(true);
            }

            float timer = 0f;
            float totalDuration = 1.3f; // Login timer before scene transition
            Color baseColor = messageText != null ? messageText.color : Color.white;

            while (timer < totalDuration)
            {
                timer += Time.unscaledDeltaTime;
                if (messageText != null)
                {
                    // Pulsing fade between 0.3 and 1.0 alpha
                    float alpha = 0.65f + 0.35f * Mathf.Sin(timer * 9f);
                    messageText.color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
                }
                yield return null;
            }

            if (messageText != null)
            {
                messageText.color = new Color(baseColor.r, baseColor.g, baseColor.b, 1f);
            }

            GameSession.Instance.SignIn(studentId, grade);
            s_ignoreClicksUntil = Time.unscaledTime + 0.6f;
            SceneManager.LoadScene(SceneNames.MainMenu);
        }

        public void Logout()
        {
            if (ClickIgnored("Logout")) return;
            if (GameSession.Instance != null) GameSession.Instance.SignOut();
            s_ignoreClicksUntil = Time.unscaledTime + 0.6f;
            SceneManager.LoadScene(SceneNames.Login);
        }

        // ---------- WorldSelect / MissionSelect ----------

        public void PickWorld(string worldId)
        {
            if (ClickIgnored("PickWorld " + worldId)) return;
            s_ignoreClicksUntil = Time.unscaledTime + 0.6f;
            GameSession.Instance.SelectedWorldId = worldId;
            SceneManager.LoadScene(SceneNames.MissionSelect);
        }

        public void PickMission(int missionNumber)
        {
            if (ClickIgnored("PickMission " + missionNumber)) return;

            GameSession session = GameSession.Instance;
            if (!session.IsMissionUnlocked(session.SelectedWorldId, missionNumber)) return;

            session.SelectedMissionNumber = missionNumber;
            WorldCatalog.Info info;
            if (WorldCatalog.TryGet(session.SelectedWorldId, out info))
            {
                s_ignoreClicksUntil = Time.unscaledTime + 0.6f;
                SceneManager.LoadScene(info.sceneName);
            }
        }
    }

    public enum AuthResult
    {
        Ok,
        EmptyId,
        EmptyPassword,
        NotFound,
        WrongPassword
    }

    // Hard-coded test accounts for offline validation.
    public static class TestAccounts
    {
        struct Account
        {
            public string studentId;
            public string password;
            public int grade;

            public Account(string studentId, string password, int grade)
            {
                this.studentId = studentId;
                this.password = password;
                this.grade = grade;
            }
        }

        static readonly Account[] All =
        {
            new Account("05-9875661", "student05", 5),
            new Account("06-1667392", "student06", 6),
        };

        public static AuthResult TryLogin(string studentId, string password, out string cleanId, out int grade)
        {
            cleanId = string.IsNullOrEmpty(studentId) ? "" : studentId.Trim();
            grade = 0;

            if (string.IsNullOrEmpty(cleanId)) return AuthResult.EmptyId;
            if (string.IsNullOrEmpty(password)) return AuthResult.EmptyPassword;

            for (int i = 0; i < All.Length; i++)
            {
                if (string.Equals(All[i].studentId, cleanId, StringComparison.OrdinalIgnoreCase))
                {
                    if (All[i].password == password)
                    {
                        cleanId = All[i].studentId;
                        grade = All[i].grade;
                        return AuthResult.Ok;
                    }
                    return AuthResult.WrongPassword;
                }
            }
            return AuthResult.NotFound;
        }

        public static string Describe(AuthResult result)
        {
            switch (result)
            {
                case AuthResult.EmptyId: return "Enter your Student ID.";
                case AuthResult.EmptyPassword: return "Enter your password.";
                case AuthResult.NotFound: return "Student ID not found.";
                case AuthResult.WrongPassword: return "Incorrect password.";
                default: return "";
            }
        }
    }
}
