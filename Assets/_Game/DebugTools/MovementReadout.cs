using Parallax.Core;
using Parallax.Gameplay.Observers;
using Parallax.Gameplay.Player;
using UnityEngine;

namespace Parallax.DebugTools
{
    /// <summary>PAX-082: what the live motor numbers mean, from JumpReach (the validator's own math),
    /// so the readout and LevelLayoutValidator.ValidateReach can't disagree. Flat ground, full speed.</summary>
    public readonly struct MovementReadoutValues
    {
        public readonly float Apex, AirtimeSeconds, AirtimeTicks, Reach, AllowedReach, RunPerSecond, RunPerTick;

        public MovementReadoutValues(float maxSpeed, float acceleration, float jumpHeight, float gravity)
        {
            float vy = JumpReach.LaunchSpeed(gravity, jumpHeight);
            float vx = JumpReach.TakeoffSpeed(maxSpeed, acceleration, float.MaxValue);
            Apex = jumpHeight;
            AirtimeSeconds = gravity > 0f ? JumpReach.Flight(vy, gravity, 0f) : 0f;
            AirtimeTicks = TickTime.ToTicks(AirtimeSeconds);
            Reach = vx * AirtimeSeconds;
            AllowedReach = JumpReach.RequiredFraction * Reach;
            RunPerSecond = maxSpeed;
            RunPerTick = maxSpeed * TickTime.SecondsPerTick;
        }

        public static MovementReadoutValues From(CatMotorConfig config, float gravity) =>
            new(config.MaxSpeed, config.Acceleration, config.JumpHeight, gravity);
    }

    /// <summary>PAX-082: on-screen tuning readout for Observer A's cat, derived from the live
    /// CatMotorConfig and GravityReceiver every frame. Dev-only (this assembly is excluded from
    /// release builds). References are optional; it finds the scene's ObserverSet and the loaded
    /// CatMotorConfig itself, so it can be added to any object during Play.</summary>
    public sealed class MovementReadout : MonoBehaviour
    {
        [SerializeField] ObserverSet observers;
        [SerializeField] CatMotorConfig config;

        // PAX-082 §12 R1: PARALLAX/Debug/Movement Readout toggles this EditorPrefs bool (off by default).
        public const string EnabledPrefKey = "Parallax.PAX082.MovementReadout";

#if UNITY_EDITOR
        // The pref read, swappable so tests never touch the developer's EditorPrefs (the TickTime pattern).
        public static System.Func<bool> IsEnabledSource = () => UnityEditor.EditorPrefs.GetBool(EnabledPrefKey, false);

        // Editor Play only: with the pref on, one hidden readout that survives level loads. No scene changes.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        public static void SpawnIfEnabled()
        {
            if (!IsEnabledSource()) return;
            var go = new GameObject("MovementReadout (PAX-082)") { hideFlags = HideFlags.HideInHierarchy };
            go.AddComponent<MovementReadout>();
            if (Application.isPlaying) DontDestroyOnLoad(go); // it only runs in Play; the guard lets tests call it
        }
#endif

        // Bottom-left: clear of the HUD's FLIP and pause buttons (top-right), the DEV buttons (top) and the debug legend.
        const float PanelWidth = 330f, PanelHeight = 150f;
        bool warned;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        void OnGUI()
        {
            if (!TryResolve(out CatMotorConfig motor, out float gravity)) return;
            MovementReadoutValues v = MovementReadoutValues.From(motor, gravity);
            var panel = new Rect(10f, Screen.height - PanelHeight - 10f, PanelWidth, PanelHeight);
            GUI.Box(panel, GUIContent.none);
            GUI.Label(new Rect(panel.x + 8f, panel.y + 6f, panel.width - 16f, panel.height - 12f),
                "Movement (PAX-082)\n" +
                $"Apex height: {v.Apex:F2} u\n" +
                $"Airtime: {v.AirtimeSeconds:F3} s = {v.AirtimeTicks:F1} ticks\n" +
                $"Max jump reach: {v.Reach:F2} u\n" +
                $"Allowed ({JumpReach.RequiredFraction:F2}): {v.AllowedReach:F2} u\n" +
                $"Run: {v.RunPerSecond:F2} u/s = {v.RunPerTick:F3} u/tick\n" +
                $"Gravity: {gravity:F1} u/s²");
        }
#endif

        bool TryResolve(out CatMotorConfig motor, out float gravity)
        {
            motor = null; gravity = 0f;
            if (observers == null) observers = FindFirstObjectByType<ObserverSet>();
            if (config == null)
            {
                CatMotorConfig[] loaded = Resources.FindObjectsOfTypeAll<CatMotorConfig>();
                if (loaded.Length == 1) config = loaded[0];
                else Warn($"MovementReadout: found {loaded.Length} loaded CatMotorConfig assets; assign one in the Inspector.");
            }
            ObserverContext observer = observers != null ? observers.Get(ObserverId.A) : null;
            if (observer == null || observer.Gravity == null) { Warn("MovementReadout: no ObserverSet with Observer A's gravity in this scene."); return false; }
            if (config == null) return false;
            motor = config;
            gravity = observer.Gravity.Strength;
            return true;
        }

        void Warn(string message)
        {
            if (warned) return;
            warned = true;
            Debug.LogWarning(message, this);
        }
    }
}
