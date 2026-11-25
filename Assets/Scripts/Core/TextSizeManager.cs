using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NutriMind.Core
{
    /// <summary>
    /// Zero-setup global text size applier.
    /// Scales every TMP_Text proportionally from its original fontSize (and auto-size min/max)
    /// according to GameSettings.TextSizeScale (0.8 / 1.0 / 1.2), preserving font style,
    /// typographical emphasis, and relative hierarchy between titles and body text.
    /// </summary>
    public class TextSizeManager : MonoBehaviour
    {
        struct BaseMetrics
        {
            public float fontSize;
            public float fontSizeMin;
            public float fontSizeMax;
        }

        static TextSizeManager instance;
        static readonly Dictionary<TMP_Text, BaseMetrics> recorded = new Dictionary<TMP_Text, BaseMetrics>();
        static readonly List<TMP_Text> staleKeys = new List<TMP_Text>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            EnsureInstance();
            RefreshAll();
        }

        static void EnsureInstance()
        {
            if (instance != null) return;
            var go = new GameObject("NutriMind_TextSizeManager");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<TextSizeManager>();
        }

        void OnEnable()
        {
            GameSettings.OnSettingsChanged += OnSettingsChanged;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        void OnDisable()
        {
            GameSettings.OnSettingsChanged -= OnSettingsChanged;
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        void OnSettingsChanged()
        {
            RefreshAll();
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            PruneDestroyed();
            RefreshAll();
        }

        public static void RefreshAll()
        {
            EnsureInstance();
            PruneDestroyed();

            float scale = GameSettings.TextSizeScale;
            var allTexts = FindObjectsOfType<TMP_Text>(true);
            for (int i = 0; i < allTexts.Length; i++)
            {
                ApplyTo(allTexts[i], scale);
            }
        }

        public static void ApplyToHierarchy(Transform root)
        {
            if (root == null) return;
            EnsureInstance();

            float scale = GameSettings.TextSizeScale;
            var texts = root.GetComponentsInChildren<TMP_Text>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                ApplyTo(texts[i], scale);
            }
        }

        static void ApplyTo(TMP_Text txt, float scale)
        {
            if (txt == null) return;

            // SweetAlertPopup dynamically sizes its own text per message and applies TextSizeScale directly.
            if (txt.GetComponentInParent<NutriMind.Interface.SweetAlertPopup>(true) != null)
                return;

            if (!recorded.TryGetValue(txt, out BaseMetrics metrics))
            {
                metrics = new BaseMetrics
                {
                    fontSize = txt.fontSize,
                    fontSizeMin = txt.fontSizeMin,
                    fontSizeMax = txt.fontSizeMax
                };
                recorded[txt] = metrics;
            }

            if (txt.enableAutoSizing)
            {
                float scaledMin = Mathf.Max(6f, metrics.fontSizeMin * scale);
                float scaledMax = Mathf.Max(scaledMin, metrics.fontSizeMax * scale);
                txt.fontSizeMin = scaledMin;
                txt.fontSizeMax = scaledMax;
                txt.fontSize = Mathf.Clamp(metrics.fontSize * scale, scaledMin, scaledMax);
            }
            else
            {
                txt.fontSize = Mathf.Max(6f, metrics.fontSize * scale);
            }
        }

        static void PruneDestroyed()
        {
            if (recorded.Count == 0) return;
            staleKeys.Clear();
            foreach (var kvp in recorded)
            {
                if (kvp.Key == null)
                    staleKeys.Add(kvp.Key);
            }
            for (int i = 0; i < staleKeys.Count; i++)
            {
                recorded.Remove(staleKeys[i]);
            }
        }
    }
}
