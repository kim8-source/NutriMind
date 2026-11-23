using System;
using UnityEngine;

namespace NutriMind.Core
{
    // Persistent settings manager for Audio, Text size, and Controls.
    // Backed by PlayerPrefs so settings persist across play sessions and scene loads.
    public static class GameSettings
    {
        const string KeyMusicVolume = "NutriMind_MusicVolume";
        const string KeySfxVolume = "NutriMind_SfxVolume";
        const string KeyTextSize = "NutriMind_TextSize";
        const string KeySensitivity = "NutriMind_LookSensitivity";

        public static float MusicVolume { get; private set; } = 0.8f;
        public static float SfxVolume { get; private set; } = 0.8f;
        public static int TextSizeIndex { get; private set; } = 1; // 0: Small, 1: Normal, 2: Large
        public static float LookSensitivity { get; private set; } = 1.0f; // 0.5 to 2.0

        public static event Action OnSettingsChanged;

        static bool isInitialized;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Initialize()
        {
            if (isInitialized) return;
            Load();
            Apply();
            isInitialized = true;
        }

        public static void Load()
        {
            MusicVolume = PlayerPrefs.GetFloat(KeyMusicVolume, 0.8f);
            SfxVolume = PlayerPrefs.GetFloat(KeySfxVolume, 0.8f);
            TextSizeIndex = PlayerPrefs.GetInt(KeyTextSize, 1);
            LookSensitivity = PlayerPrefs.GetFloat(KeySensitivity, 1.0f);
        }

        public static void SetMusicVolume(float volume)
        {
            MusicVolume = Mathf.Clamp01(volume);
            PlayerPrefs.SetFloat(KeyMusicVolume, MusicVolume);
            Apply();
            Notify();
        }

        public static void SetSfxVolume(float volume)
        {
            SfxVolume = Mathf.Clamp01(volume);
            PlayerPrefs.SetFloat(KeySfxVolume, SfxVolume);
            Apply();
            Notify();
        }

        public static void SetTextSize(int index)
        {
            TextSizeIndex = Mathf.Clamp(index, 0, 2);
            PlayerPrefs.SetInt(KeyTextSize, TextSizeIndex);
            Notify();
        }

        public static void SetLookSensitivity(float sensitivity)
        {
            LookSensitivity = Mathf.Clamp(sensitivity, 0.2f, 3.0f);
            PlayerPrefs.SetFloat(KeySensitivity, LookSensitivity);
            Notify();
        }

        public static void Save()
        {
            PlayerPrefs.Save();
        }

        public static void Apply()
        {
            // Apply master audio volume to Unity AudioListener
            AudioListener.volume = Mathf.Max(MusicVolume, SfxVolume);
        }

        static void Notify()
        {
            OnSettingsChanged?.Invoke();
        }
    }
}
