#if UNITY_ANDROID && !UNITY_EDITOR
using System;
using System.Collections.Generic;
using Parallax.Core;
using UnityEngine;

namespace Parallax.Gameplay.Audio
{
    /// <summary>PAX-065 (D-278): the phone's vibrator through Android's Vibrator and VibrationEffect. Each cue's effect is built once.
    /// A phone with amplitude control (API 26 or later) plays the config's patterns; any other plays Strong cues with
    /// Handheld.Vibrate and skips the rest. Referencing Handheld.Vibrate is also what makes Unity add the VIBRATE permission. A
    /// failed call is logged once and switches haptics off: they never get in the way of the game.</summary>
    public sealed class AndroidHaptics : IHaptics
    {
        const int FirstVibrationEffectApi = 26;

        readonly HapticsConfig config;
        readonly Dictionary<HapticCue, AndroidJavaObject> effects = new Dictionary<HapticCue, AndroidJavaObject>();
        AndroidJavaObject vibrator;
        bool amplitude, failed;

        public AndroidHaptics(HapticsConfig config)
        {
            this.config = config;
            try
            {
                using var version = new AndroidJavaClass("android.os.Build$VERSION");
                using var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                using AndroidJavaObject activity = player.GetStatic<AndroidJavaObject>("currentActivity");
                vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
                amplitude = version.GetStatic<int>("SDK_INT") >= FirstVibrationEffectApi && vibrator != null && vibrator.Call<bool>("hasAmplitudeControl");
            }
            catch (Exception e) { Fail(e); }
        }

        public void Play(HapticCue cue)
        {
            if (failed || !config.TryGetPattern(cue, out HapticsConfig.Pattern pattern)) return;
            try
            {
                if (amplitude) vibrator.Call("vibrate", EffectFor(pattern));
                else if (pattern.Strong) Handheld.Vibrate();
            }
            catch (Exception e) { Fail(e); }
        }

        AndroidJavaObject EffectFor(HapticsConfig.Pattern pattern)
        {
            if (effects.TryGetValue(pattern.Cue, out AndroidJavaObject effect)) return effect;
            var timings = new long[pattern.Millis.Length];
            for (int i = 0; i < timings.Length; i++) timings[i] = pattern.Millis[i];
            using var effectClass = new AndroidJavaClass("android.os.VibrationEffect");
            effect = effectClass.CallStatic<AndroidJavaObject>("createWaveform", timings, pattern.Amplitudes, -1);
            effects[pattern.Cue] = effect;
            return effect;
        }

        void Fail(Exception e)
        {
            failed = true;
            Debug.LogError("AndroidHaptics: haptics are off for this session: " + e);
        }
    }
}
#endif
