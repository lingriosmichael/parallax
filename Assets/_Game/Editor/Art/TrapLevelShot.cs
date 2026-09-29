using System.Collections.Generic;
using System.Linq;
using Parallax.Core;
using Parallax.Editor.Levels;
using Parallax.Editor.Routes;
using Parallax.Gameplay.Cameras;
using Parallax.Gameplay.Rooms;
using Parallax.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Parallax.Editor.Art
{
    /// <summary>PAX-A13 (§9.2, §12 R10): PARALLAX/Art/Trap Screenshots. L013, L015, L016 and L020 at phone scale (2400 x 1080,
    /// the Pixel 8a in landscape): each level's camera frame and lights are read from its Level_NNN.unity; its solution is
    /// replayed by the route harness with the trap art (the level's lights are copied; its background layers are not, the
    /// clear colour stands in), and the first time each kind of moment is on screen it's rendered: a geyser's tell or
    /// eruption, a floor crumbling, an arrow or spear in flight, a block falling, a mover moving, a flip's pulse, an
    /// inverter's flare, the cloud charging or striking, a vine snapping. The level camera's rule is followed (the view is at
    /// most MaxViewHeight tall and fits the frame; it centres on the cat, clamped to the frame). Saved to
    /// Docs/Art/Traps/screenshots/. The open scene is restored.</summary>
    public static class TrapLevelShot
    {
        const string OutDir = "Docs/Art/Traps/screenshots/";
        const int Width = 2400, Height = 1080, MaxPerLevel = 6;
        static readonly string[] Levels = { "L013", "L015", "L016", "L020" };

        struct Lamp { public Light2D.LightType Type; public Color Color; public float Intensity, Radius; public Vector2 At; public int[] Targets; }

        [MenuItem("PARALLAX/Art/Trap Screenshots")]
        public static void Make()
        {
            if (!TrapContactSheet.TrapArtReady()) return;
            string previous = SceneManager.GetActiveScene().path;
            var shots = new List<string>();
            try { foreach (string level in Levels) shots.AddRange(Shoot(level)); }
            finally { Restore(previous); }
            Debug.LogWarning("Trap Screenshots: " + (shots.Count == 0 ? "no moment was on screen" : string.Join(", ", shots)));
        }

        static List<string> Shoot(string level)
        {
            var shots = new List<string>();
            EditorSceneManager.OpenScene($"Assets/_Game/Scenes/Levels/Level_{level.Substring(1)}.unity", OpenSceneMode.Single);
            LevelCameraFollow follow = Object.FindObjectsByType<LevelCameraFollow>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault();
            if (follow == null) { Debug.LogError($"Trap Screenshots: Level_{level.Substring(1)} has no LevelCameraFollow."); return shots; }
            var so = new SerializedObject(follow);
            Vector2 frameCentre = so.FindProperty("frameCenter").vector2Value, frameSize = so.FindProperty("frameSize").vector2Value;
            var config = (LevelCameraConfig)so.FindProperty("config").objectReferenceValue;
            var lamps = Object.FindObjectsByType<Light2D>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .Where(l => l.lightType is Light2D.LightType.Global or Light2D.LightType.Point)
                .Select(l => new Lamp { Type = l.lightType, Color = l.color, Intensity = l.intensity, Radius = l.pointLightOuterRadius, At = l.transform.position, Targets = l.targetSortingLayers.ToArray() }).ToList();

            float aspect = Width / (float)Height;
            float view = Mathf.Min(config != null ? config.MaxViewHeight : 16f, frameSize.y, frameSize.x / aspect);
            var taken = new HashSet<string>();
            using (var session = new RouteSession())
            {
                var wanted = new Dictionary<string, System.Func<bool>>();
                Built rig = null;
                var options = new ReplayOptions
                {
                    AfterTick = tick =>
                    {
                        TrapShots.ApplyArt();
                        Transform cat = Object.FindObjectsByType<Parallax.Gameplay.Player.CatMotor2D>(FindObjectsSortMode.None).FirstOrDefault()?.transform;
                        if (cat == null) return;
                        Vector2 centre = Clamp(cat.position, frameCentre, frameSize, view, aspect);
                        var viewRect = new Rect(centre - new Vector2(view * aspect, view) * 0.5f, new Vector2(view * aspect, view));
                        string moment = Moment(viewRect);
                        if (moment == null || taken.Count >= MaxPerLevel || !taken.Add(moment)) return;
                        if (rig == null || !rig.Alive) rig = Build(lamps);
                        Texture2D tex = rig.Rig.Shot(centre, view, Width, Height);
                        TrapShots.Text(tex, $"{level} {moment} T{tick} - 2400X1080 PIXEL 8A LANDSCAPE - HOSTS GREY-BOX (ENV-10 NOT IN YET), LEVEL LIGHTS, BACKGROUND LAYERS OMITTED", 24, 24, 3, Color.white);
                        tex.Apply();
                        string path = $"{OutDir}{level}_{moment.ToLowerInvariant().Replace(' ', '_')}.png";
                        TrapShots.SavePng(tex, path);
                        Object.DestroyImmediate(tex);
                        shots.Add(path);
                    },
                };
                RouteHarness.Replay(session, LevelLayouts.ById[level], LevelRoutes.ById[level].Solution, options);
                rig?.Rig.Dispose();
            }
            return shots;
        }

        // The first time each kind of moment is on screen (the trap's position inside the view).
        static string Moment(Rect view)
        {
            foreach (TrapArt art in Object.FindObjectsByType<TrapArt>(FindObjectsSortMode.None))
            {
                RoomTrap trap = art.Trap;
                if (trap == null) continue;
                Vector2 at = trap is StormCloudTrap storm ? (Vector2)storm.Cloud.transform.position : (Vector2)trap.transform.position;
                if (!view.Contains(at)) continue;
                int s = TrapShots.TicksSinceFire(trap);
                switch (trap)
                {
                    case GeyserTrap g when g.Phase == GeyserPhase.Tell && s == 12: return "GEYSER TELL";
                    case GeyserTrap g when g.Phase == GeyserPhase.Erupt && s == Ticks(g, "tellTicks") + 6: return "GEYSER ERUPTING";
                    case CollapsingFloorTrap when s >= 4 && s <= 10: return "FLOOR CRUMBLING";
                    case ArrowTrap a when s == Ticks(a, "tellTicks") + 4: return (bool)Value(a, "spear") ? "SPEAR IN FLIGHT" : "ARROW IN FLIGHT";
                    case FallingBlockTrap when s == 4: return "BLOCK FALLING";
                    case MovingTrap when s == 6: return "MOVER MOVING";
                    case ShrinkingFloorTrap when s == 20: return "FLOOR SHRINKING";
                    case GravityFlipTrap when s == 3: return "FLIP PULSE";
                    case InverterTrap when s == 3: return "INVERTER FLARE";
                    case StormCloudTrap c when c.Phase == StormCloudPhase.Charge: return "CLOUD CHARGING";
                    case StormCloudTrap c when c.Phase == StormCloudPhase.Strike: return "CLOUD STRIKE";
                    case ClimbVine v when v.Snaps && s == 3: return "VINE SNAP";
                }
            }
            return null;
        }

        static object Value(object o, string field) =>
            o.GetType().GetField(field, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(o);

        static int Ticks(object o, string field) => (int)Value(o, field);

        sealed class Built { public TrapShots.Rig Rig; public GameObject Root; public bool Alive => Root != null; }

        static Built Build(List<Lamp> lamps)
        {
            var rig = new TrapShots.Rig();
            rig.Ensure(new Color(0.9f, 0.8f, 0.62f), -1f);
            var root = new GameObject("__LevelLights");
            rig.Own(root);
            foreach (Lamp l in lamps) rig.Light(l.Type, l.At, l.Intensity, l.Color, l.Radius, targets: l.Targets);
            return new Built { Rig = rig, Root = root };
        }

        static Vector2 Clamp(Vector2 cat, Vector2 frameCentre, Vector2 frameSize, float view, float aspect)
        {
            Vector2 half = new(view * aspect * 0.5f, view * 0.5f), min = frameCentre - frameSize * 0.5f + half, max = frameCentre + frameSize * 0.5f - half;
            return new Vector2(min.x > max.x ? frameCentre.x : Mathf.Clamp(cat.x, min.x, max.x), min.y > max.y ? frameCentre.y : Mathf.Clamp(cat.y, min.y, max.y));
        }

        static void Restore(string previous)
        {
            if (!string.IsNullOrEmpty(previous)) EditorSceneManager.OpenScene(previous, OpenSceneMode.Single);
        }
    }
}
