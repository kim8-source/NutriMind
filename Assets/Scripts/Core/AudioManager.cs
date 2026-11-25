using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace NutriMind.Core
{
    // Persistent AudioManager for Background Music and UI Sound Effects.
    // Automatically creates itself on first load and stays alive across all scenes.
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Audio Sources")]
        [SerializeField] AudioSource musicSource;
        [SerializeField] AudioSource sfxSource;

        [Header("Music Clips (Optional Inspector assignment)")]
        [SerializeField] AudioClip bgmMainMenu;
        [SerializeField] AudioClip bgmLiteraQuest;
        [SerializeField] AudioClip bgmScience;
        [SerializeField] AudioClip bgmWellness;

        [Header("SFX Clips")]
        [SerializeField] AudioClip buttonClickClip;

        AudioClip proceduralClickClip;
        Coroutine fadeCoroutine;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoInitialize()
        {
            if (Instance == null)
            {
                var existing = FindAnyObjectByType<AudioManager>();
                if (existing != null)
                {
                    Instance = existing;
                    DontDestroyOnLoad(existing.gameObject);
                }
                else
                {
                    GameObject go = new GameObject("AudioManager");
                    Instance = go.AddComponent<AudioManager>();
                    DontDestroyOnLoad(go);
                }
            }
        }

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                SetupAudioSources();
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
                return;
            }
        }

        void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            GameSettings.OnSettingsChanged += UpdateVolumes;
        }

        void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            GameSettings.OnSettingsChanged -= UpdateVolumes;
        }

        void Start()
        {
            UpdateVolumes();
            AutoHookButtonsInScene();
            UpdateBGMForCurrentScene();
        }

        void SetupAudioSources()
        {
            if (musicSource == null)
            {
                musicSource = gameObject.AddComponent<AudioSource>();
                musicSource.loop = true;
                musicSource.playOnAwake = false;
                musicSource.spatialBlend = 0f; // 2D
            }

            if (sfxSource == null)
            {
                sfxSource = gameObject.AddComponent<AudioSource>();
                sfxSource.loop = false;
                sfxSource.playOnAwake = false;
                sfxSource.spatialBlend = 0f; // 2D
            }

            // Generate procedural crisp button click clip as default fallback
            proceduralClickClip = CreateProceduralClickClip();
        }

        void UpdateVolumes()
        {
            if (musicSource != null) musicSource.volume = GameSettings.MusicVolume;
            if (sfxSource != null) sfxSource.volume = GameSettings.SfxVolume;
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            UpdateVolumes();
            StartCoroutine(HookButtonsDelayed(scene));
            UpdateBGMForCurrentScene();
        }

        IEnumerator HookButtonsDelayed(Scene scene)
        {
            // Immediate hook
            AutoHookButtonsInScene(scene);
            // Wait 1 frame so other scripts (like MenuNavigation) finish Awake/Start
            yield return null;
            AutoHookButtonsInScene(scene);
        }

        // ==========================================
        // BACKGROUND MUSIC (BGM)
        // ==========================================

        public void UpdateBGMForCurrentScene()
        {
            string sceneName = SceneManager.GetActiveScene().name;

            // In MissionSelect or gameplay, choose by selected world
            if (sceneName == SceneNames.MissionSelect || sceneName.StartsWith("Mission") || sceneName.StartsWith("World_"))
            {
                string worldId = GameSession.Instance != null ? GameSession.Instance.SelectedWorldId : "ENG";
                PlayWorldBGM(worldId);
                return;
            }

            // Standard menu screens (Boot, Login, MainMenu, WorldSelect, OnlineQuiz)
            PlayMainMenuBGM();
        }

        public void PlayMainMenuBGM()
        {
            AudioClip clip = bgmMainMenu
                          ?? Resources.Load<AudioClip>("Audio/BGM_MainMenu")
                          ?? Resources.Load<AudioClip>("Audio/Curious Exploration")
                          ?? Resources.Load<AudioClip>("Curious Exploration");

            PlayBGM(clip);
        }

        public void PlayWorldBGM(string worldId)
        {
            AudioClip clip = null;

            switch (worldId)
            {
                case "SCI":
                    clip = bgmScience
                        ?? Resources.Load<AudioClip>("Audio/BGM_Science")
                        ?? Resources.Load<AudioClip>("Audio/BGM_SciVenture");
                    break;

                case "PEH":
                    clip = bgmWellness
                        ?? Resources.Load<AudioClip>("Audio/BGM_Wellness")
                        ?? Resources.Load<AudioClip>("Audio/BGM_HealthQuest")
                        ?? Resources.Load<AudioClip>("Audio/BGM_Health");
                    break;

                case "ENG":
                default:
                    clip = bgmLiteraQuest
                        ?? Resources.Load<AudioClip>("Audio/BGM_LiteraQuest")
                        ?? Resources.Load<AudioClip>("Audio/BGM_English");
                    break;
            }

            // Fallback to Curious Exploration if specific track is not yet added
            if (clip == null)
            {
                clip = Resources.Load<AudioClip>("Audio/Curious Exploration")
                    ?? Resources.Load<AudioClip>("Curious Exploration")
                    ?? bgmMainMenu;
            }

            PlayBGM(clip);
        }

        public void PlayBGM(AudioClip clip, float fadeDuration = 0.5f)
        {
            if (clip == null || musicSource == null) return;
            if (musicSource.clip == clip && musicSource.isPlaying) return;

            if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
            fadeCoroutine = StartCoroutine(CrossfadeMusicRoutine(clip, fadeDuration));
        }

        IEnumerator CrossfadeMusicRoutine(AudioClip newClip, float duration)
        {
            float targetVol = GameSettings.MusicVolume;

            // Fade out current
            if (musicSource.isPlaying && duration > 0f)
            {
                float startVol = musicSource.volume;
                for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
                {
                    musicSource.volume = Mathf.Lerp(startVol, 0f, t / duration);
                    yield return null;
                }
            }

            musicSource.clip = newClip;
            musicSource.Play();

            // Fade in new
            if (duration > 0f)
            {
                for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
                {
                    musicSource.volume = Mathf.Lerp(0f, targetVol, t / duration);
                    yield return null;
                }
            }

            musicSource.volume = targetVol;
            fadeCoroutine = null;
        }

        // ==========================================
        // BUTTON CLICK SFX
        // ==========================================

        public static void PlayClick()
        {
            if (Instance != null)
            {
                Instance.PlayButtonClick();
            }
        }

        public void PlayButtonClick()
        {
            if (sfxSource == null) return;

            if (proceduralClickClip == null)
            {
                proceduralClickClip = CreateProceduralClickClip();
            }

            AudioClip clip = buttonClickClip
                          ?? Resources.Load<AudioClip>("Audio/ButtonClick")
                          ?? Resources.Load<AudioClip>("Audio/UI_Click")
                          ?? proceduralClickClip;

            if (clip != null)
            {
                sfxSource.PlayOneShot(clip, GameSettings.SfxVolume);
            }
        }

        // Automatically wires button clicks to all buttons in loaded scenes (including inactive panels)
        public void AutoHookButtonsInScene(Scene scene = default)
        {
            var buttons = Resources.FindObjectsOfTypeAll<Button>();
            for (int i = 0; i < buttons.Length; i++)
            {
                var btn = buttons[i];
                if (btn == null || btn.gameObject == null) continue;
                // Only hook buttons belonging to an active loaded scene (not prefabs in project assets)
                if (!btn.gameObject.scene.isLoaded) continue;

                // Avoid duplicate listeners
                btn.onClick.RemoveListener(PlayButtonClick);
                btn.onClick.AddListener(PlayButtonClick);
            }
        }

        // Procedural crisp UI click pop (smooth 45ms sine wave at 850Hz with exponential decay)
        static AudioClip CreateProceduralClickClip()
        {
            int sampleRate = 44100;
            int length = (int)(sampleRate * 0.045f);
            float[] samples = new float[length];

            for (int i = 0; i < length; i++)
            {
                float t = (float)i / sampleRate;
                float envelope = Mathf.Exp(-t * 90f);
                float freq = Mathf.Lerp(900f, 350f, t / 0.045f);
                samples[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * envelope * 0.7f;
            }

            AudioClip clip = AudioClip.Create("ProceduralClick", length, 1, sampleRate, false);
            clip.hideFlags = HideFlags.DontSave;
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
