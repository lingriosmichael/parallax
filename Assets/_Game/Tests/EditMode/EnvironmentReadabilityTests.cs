using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Parallax.Gameplay.Cameras;
using Parallax.Gameplay.Presentation;
using Parallax.Gameplay.Reality;
using Parallax.Gameplay.Rooms;
using Parallax.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Parallax.Tests.EditMode
{
    // PAX-A15 §3: the readability rules, read off every saved level scene (L001–L020) after Rebuild All Levels. A4 the
    // stray FG_01 and the A02 strips are gone; ruling 4 far pieces scale at most 1.5, mid pieces not at all; A2 no flat
    // background line within 0.3 u of a walkable top over the camera's travel; the foreground covers only solids; dressing
    // keeps out of hazards, checkpoints, triggers, flip zones, arrow lanes and the door's 1 u; nothing environmental has a
    // collider; every disguised trap's trims follow its skin (P10); the door is ENV-24's lit frame with its glow. Reads
    // scenes without saving and restores the Test Runner's scene (PAX-075 R21/R22).
    public sealed class EnvironmentReadabilityTests
    {
        const string LevelsFolder = "Assets/_Game/Scenes/Levels";
        static readonly Type Skin = Type.GetType("Parallax.Editor.Setup.SoloRoomSkin, Parallax.Editor");
        static readonly Type Stack = Type.GetType("Parallax.Editor.Setup.EnvironmentStackSetup, Parallax.Editor");
        static readonly Type Looks = Type.GetType("Parallax.Editor.Levels.LevelLooks, Parallax.Editor");
        static readonly Type TravelType = Type.GetType("Parallax.Editor.Setup.EnvironmentStackSetup+Travel, Parallax.Editor");
        static readonly IDictionary Layouts = (IDictionary)Type.GetType("Parallax.Editor.Levels.LevelLayouts, Parallax.Editor").GetField("ById").GetValue(null);
        static readonly string[] Dressing = { "Moss", "Drape", "Banner", "Glyph", "Rubble" };

        static IEnumerable<string> LevelIds() => Enumerable.Range(1, 20).Select(n => "L" + n.ToString("000"));

        [Test]
        public void EveryLevel_FollowsTheEnvironmentRules()
        {
            Assert.NotNull(Skin, "SoloRoomSkin not found");
            var failures = new List<string>();
            int compared = 0;
            foreach (string id in LevelIds())
            {
                string path = $"{LevelsFolder}/Level_{id.Substring(1)}.unity";
                if (AssetDatabase.LoadAssetAtPath<Object>(path) == null || !Layouts.Contains(id)) continue;
                WithScene(path, scene => { Check(id, scene, Layouts[id], failures); compared++; });
            }
            Assert.Greater(compared, 0, "no level scene was checked");
            Assert.IsEmpty(failures, string.Join("\n", failures));
        }

        static void Check(string id, Scene scene, object room, List<string> failures)
        {
            RealityRoot rootA = scene.GetRootGameObjects().Select(g => g.GetComponent<RealityRoot>()).First(r => r != null && r.Id == Parallax.Core.ObserverId.A);
            // A4: the stray foreground piece and the A02 strips are gone.
            foreach (Transform t in rootA.GetComponentsInChildren<Transform>(true))
                if (t.name is "FG_01" or "BG_00_Sky" or "BG_01_Far" or "MG_01_Mid") failures.Add($"{id}: {t.name} still under RealityRoot_A");
            Transform env = rootA.transform.Find("Environment");
            if (env == null) { failures.Add($"{id}: no RealityRoot_A/Environment (run Rebuild All Levels)"); return; }

            // Ruling 4 (D-103): far pieces scale at most 1.5, painted mid pieces not at all. The haze gradients (no texture
            // detail) and the water strip (stretched along its waterline) are exempt.
            foreach (Transform piece in Children(env.Find("Far")))
                if (piece.localScale.x > 1.5f + 1e-4f) failures.Add($"{id}: far piece {piece.name} scaled {piece.localScale.x}");
            foreach (Transform piece in Children(env.Find("Mid")))
            {
                string slot = piece.GetComponent<SpriteRenderer>()?.sprite?.name ?? "";
                if (slot is "ENV_SkyFade" or "ENV_WaterPlane") continue;
                if (Mathf.Abs(piece.localScale.x - 1f) > 1e-4f) failures.Add($"{id}: mid piece {piece.name} scaled {piece.localScale.x}");
            }

            // Nothing environmental has a collider.
            var envRoots = new List<Transform> { env };
            envRoots.AddRange(rootA.GetComponentsInChildren<Transform>(true).Where(t => t.name is "Env" or "Trims"));
            foreach (Transform r in envRoots)
                if (r.GetComponentsInChildren<Collider2D>(true).Length > 0) failures.Add($"{id}: a collider under {r.name}");

            LevelCameraFollow follow = Object.FindObjectsByType<LevelCameraFollow>(FindObjectsInactive.Include, FindObjectsSortMode.None).First();
            var so = new SerializedObject(follow);
            var frame = new Bounds(so.FindProperty("frameCenter").vector2Value, so.FindProperty("frameSize").vector2Value);
            // D-104: the level's own view (baked on its camera; 0 = the config's).
            float baked = so.FindProperty("viewHeight").floatValue;
            float maxView = baked > 0f ? baked : (float)Stack.GetMethod("MaxViewHeight").Invoke(null, null);
            object travel = Activator.CreateInstance(TravelType, frame, maxView);

            // A2: flat background lines keep 0.3 u from walkable tops over the camera's travel.
            object tops = Skin.GetMethod("WalkableTops").Invoke(null, new[] { room });
            MethodInfo flatClear = Stack.GetMethod("FlatClear");
            foreach (string layerName in new[] { "Mid", "Far" })
            {
                Transform layer = env.Find(layerName);
                if (layer == null) continue;
                float speed = (float)new SerializedObject(layer.GetComponent<ParallaxLayer>()).FindProperty("screenSpeed").floatValue;
                foreach (SpriteRenderer r in layer.GetComponentsInChildren<SpriteRenderer>(true))
                {
                    string slot = r.sprite != null ? r.sprite.name : "";
                    var flats = (float[])Looks.GetMethod("FlatTops").Invoke(null, new object[] { slot });
                    if (flats.Length == 0) continue;
                    Vector2 size = r.bounds.size;
                    Vector2 bottom = (Vector2)r.bounds.center - new Vector2(0f, size.y * 0.5f);
                    if (!(bool)flatClear.Invoke(null, new object[] { flats, speed, bottom, size, tops, travel })) failures.Add($"{id}: A2 {r.name} has a flat line within 0.3 u of a walkable top");
                }
            }

            // The foreground covers only solids, over the camera's travel.
            Transform fg = env.Find("Foreground");
            if (fg != null)
            {
                float speed = new SerializedObject(fg.GetComponent<ParallaxLayer>()).FindProperty("screenSpeed").floatValue;
                foreach (SpriteRenderer r in fg.GetComponentsInChildren<SpriteRenderer>(true))
                    if (!(bool)Stack.GetMethod("ForegroundClear").Invoke(null, new object[] { new Rect(r.bounds.min, r.bounds.size), speed, room, travel }))
                        failures.Add($"{id}: foreground {r.name} covers the room's air");
            }

            // Dressing keeps out (D-103): tufts (Moss_, drawn behind the stone and every trap) only of the door and the
            // checkpoints; ivy and drapes of everything the player must see (an invisible trigger is no keep-out: a bare
            // patch over it would mark the trap); rubble, banners and glyphs of every keep-out zone, triggers included.
            var keepOut = (List<Rect>)Skin.GetMethod("KeepOut").Invoke(null, new[] { room });
            var visibleKeepOut = (List<Rect>)Skin.GetMethod("VisibleKeepOut").Invoke(null, new[] { room });
            var tuftKeepOut = (List<Rect>)Skin.GetMethod("TuftKeepOut").Invoke(null, new[] { room });
            foreach (SpriteRenderer r in rootA.GetComponentsInChildren<SpriteRenderer>(true).Where(r => Dressing.Any(d => r.name.StartsWith(d)) && InEnv(r.transform)))
            {
                var box = new Rect(r.bounds.min, r.bounds.size);
                List<Rect> zones = r.name.StartsWith("Moss_") ? tuftKeepOut : r.name.StartsWith("Drape_") || r.name.StartsWith("Ivy_") ? visibleKeepOut : keepOut;
                if (zones.Any(k => k.Overlaps(box))) failures.Add($"{id}: dressing {r.name} in a keep-out zone");
            }

            // P10: every trims container follows its owner (a disguised trap's skin).
            foreach (Transform trims in rootA.GetComponentsInChildren<Transform>(true).Where(t => t.name == "Trims"))
            {
                ShownWith follow2 = trims.GetComponent<ShownWith>();
                if (follow2 == null || follow2.Owner == null || follow2.Renderers.Length == 0) failures.Add($"{id}: {trims.parent?.parent?.name} trims don't follow its skin");
                else if (follow2.Owner.transform != trims.parent) failures.Add($"{id}: {trims.parent?.parent?.name} trims follow the wrong renderer");
            }

            // The door: ENV-24's lit frame with its glow (A6's art).
            foreach (RoomDoor door in rootA.GetComponentsInChildren<RoomDoor>(true))
            {
                SpriteRenderer art = door.transform.Find("Art")?.GetComponent<SpriteRenderer>();
                if (art == null || art.sprite == null || art.sprite.name != "ENV_Door") failures.Add($"{id}: door {door.name} isn't ENV_Door");
                if (door.transform.Find("Art/Glow") == null) failures.Add($"{id}: door {door.name} has no glow");
            }
        }

        static IEnumerable<Transform> Children(Transform t) => t == null ? Enumerable.Empty<Transform>() : t.Cast<Transform>();

        static bool InEnv(Transform t)
        {
            for (Transform p = t.parent; p != null; p = p.parent) if (p.name is "Env" or "Trims") return true;
            return false;
        }

        [Test]
        public void ShownWith_HidesAndShowsWithItsOwner() => InCleanScene(() =>
        {
            var owner = new GameObject("Owner").AddComponent<SpriteRenderer>();
            var child = new GameObject("Trim").AddComponent<SpriteRenderer>();
            child.transform.SetParent(owner.transform);
            var follow = owner.gameObject.AddComponent<ShownWith>();
            var so = new SerializedObject(follow);
            so.FindProperty("owner").objectReferenceValue = owner;
            SerializedProperty list = so.FindProperty("renderers");
            list.arraySize = 1;
            list.GetArrayElementAtIndex(0).objectReferenceValue = child;
            so.ApplyModifiedPropertiesWithoutUndo();
            try
            {
                owner.enabled = false; follow.Apply();
                Assert.IsFalse(child.enabled, "hidden with its owner");
                owner.enabled = true; follow.Apply();
                Assert.IsTrue(child.enabled, "shown with its owner");
            }
            finally { Object.DestroyImmediate(owner.gameObject); }
        });

        [Test]
        public void WorldTileSampling_AppliesItsValuesToTheRenderer() => InCleanScene(() =>
        {
            var r = new GameObject("Tile").AddComponent<SpriteRenderer>();
            try
            {
                var sampling = r.gameObject.AddComponent<WorldTileSampling>();
                sampling.Set(new Vector2(4f, 0.5f), new Vector4(3f, 0.25f, 1f, 1f), new Vector4(0f, 0f, 1f, 1f));
                var block = new MaterialPropertyBlock();
                r.GetPropertyBlock(block);
                Assert.AreEqual(new Vector4(4f, 0.5f, 0f, 0f), block.GetVector("_WorldTile"));
                Assert.AreEqual(new Vector4(3f, 0.25f, 1f, 1f), block.GetVector("_Rest"));
            }
            finally { Object.DestroyImmediate(r.gameObject); }
        });

        // Objects made in the Test Runner's scene leave it dirty; later fixtures' route sessions refuse a dirty scene.
        static void InCleanScene(Action body)
        {
            string[] setup = EditorSceneManager.GetSceneManagerSetup().Select(s => s.path).ToArray();
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            try { body(); }
            finally
            {
                if (setup.Length == 0) Type.GetType("Parallax.Editor.Routes.RouteSession, Parallax.Editor").GetMethod("RecreateUntitledScene").Invoke(null, new object[] { true });
                else EditorSceneManager.RestoreSceneManagerSetup(setup.Select((p, i) => new SceneSetup { path = p, isActive = i == 0, isLoaded = true }).ToArray());
            }
        }

        static void WithScene(string path, Action<Scene> body)
        {
            string[] setup = EditorSceneManager.GetSceneManagerSetup().Select(s => s.path).ToArray();
            try { body(EditorSceneManager.OpenScene(path, OpenSceneMode.Single)); }
            finally
            {
                if (setup.Length == 0) Type.GetType("Parallax.Editor.Routes.RouteSession, Parallax.Editor").GetMethod("RecreateUntitledScene").Invoke(null, new object[] { true });
                else
                {
                    var restore = new SceneSetup[setup.Length];
                    for (int i = 0; i < setup.Length; i++) restore[i] = new SceneSetup { path = setup[i], isActive = i == 0, isLoaded = true };
                    EditorSceneManager.RestoreSceneManagerSetup(restore);
                }
            }
        }
    }
}
