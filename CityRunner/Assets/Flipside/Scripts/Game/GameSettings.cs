using UnityEngine;

namespace Flipside
{
    /// <summary>Player-facing settings persisted in PlayerPrefs.</summary>
    public static class GameSettings
    {
        const string MusicKey = "flipside.music";
        const string SfxKey = "flipside.sfx";

        public static float MusicVolume
        {
            get => PlayerPrefs.GetFloat(MusicKey, 0.7f);
            set { PlayerPrefs.SetFloat(MusicKey, Mathf.Clamp01(value)); PlayerPrefs.Save(); Changed?.Invoke(); }
        }

        public static float SfxVolume
        {
            get => PlayerPrefs.GetFloat(SfxKey, 0.9f);
            set { PlayerPrefs.SetFloat(SfxKey, Mathf.Clamp01(value)); PlayerPrefs.Save(); Changed?.Invoke(); }
        }

        public static event System.Action Changed;
    }
}
