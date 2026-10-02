using System.Collections.Generic;
using Parallax.Gameplay.Cameras;

namespace Parallax.Editor.Levels
{
    /// <summary>D-104: each level's camera. The zoom (D-102's 1.8x where every camera rule passes, else the largest of 1.5x
    /// and 1.2x that does) and the follow target's lift above the cat (1.5 u where every hazard on the solution stays as
    /// visible as without it, else 0). The level build bakes both into LevelCameraFollow; the camera tell, escape and chaos
    /// rules read the same values, so the game and its validators never disagree. Data only: the values were measured
    /// (PAX-A16 gauntlet, the room auditor's check). Levels and rooms not listed use the config's view and no lift.</summary>
    public static class LevelCameras
    {
        public readonly struct Entry
        {
            public readonly float Zoom, Bias;
            public Entry(float zoom, float bias = 0f) { Zoom = zoom; Bias = bias; }
        }

        public const float BaseViewHeight = 16f;

        // Measured 2026-10-02 (Phase 1 checkpoint): zooms from the camera sweep; lifts from the room auditor's ruling.
        public static readonly IReadOnlyDictionary<string, Entry> ById = new Dictionary<string, Entry>
        {
            ["L001"] = new(1.8f, 1.5f), ["L002"] = new(1.8f), ["L003"] = new(1.8f, 1.5f), ["L004"] = new(1.2f), ["L005"] = new(1.2f, 1.5f),
            ["L006"] = new(1.8f), ["L007"] = new(1.8f), ["L008"] = new(1.5f), ["L009"] = new(1.2f, 1.5f), ["L010"] = new(1.2f),
            ["L011"] = new(1.2f), ["L012"] = new(1.5f), ["L013"] = new(1.8f), ["L014"] = new(1.5f), ["L015"] = new(1.2f, 1.5f),
            ["L016"] = new(1.2f), ["L017"] = new(1.2f), ["L018"] = new(1.5f), ["L019"] = new(1.2f), ["L020"] = new(1.5f),
            // Trap Lab rooms (validation only: the sandbox's own camera doesn't read this table).
            ["TrapLab3"] = new(1.5f), ["TrapLab6"] = new(1.5f), ["TrapLab12"] = new(1.5f),
        };

        /// <summary>The level's view height: 16 / its zoom, or the config's MaxViewHeight when it isn't listed.</summary>
        public static float ViewHeight(string id, LevelCameraConfig config) =>
            id != null && ById.TryGetValue(id, out Entry e) ? BaseViewHeight / e.Zoom : config != null ? config.MaxViewHeight : BaseViewHeight;

        public static float Bias(string id) => id != null && ById.TryGetValue(id, out Entry e) ? e.Bias : 0f;
    }
}
