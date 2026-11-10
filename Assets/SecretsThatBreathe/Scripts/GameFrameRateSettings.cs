using UnityEngine;

namespace SecretsThatBreathe
{
    public static class GameFrameRateSettings
    {
        private const string PreferenceKey = "FrameRateLimit";
        public static readonly int[] Options = { 60, 90, 120, 180, 240, -1 };
        public static int Current { get; private set; } = 60;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            int saved = PlayerPrefs.GetInt(PreferenceKey, 60);
            Apply(System.Array.IndexOf(Options, saved) >= 0 ? saved : 60, false);
        }

        public static void Apply(int fps, bool save = true)
        {
            if (System.Array.IndexOf(Options, fps) < 0) return;
            Current = fps;
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = fps;
            if (!save) return;
            PlayerPrefs.SetInt(PreferenceKey, fps);
            PlayerPrefs.Save();
        }
    }
}
