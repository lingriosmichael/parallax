using System;
using NUnit.Framework;
using Parallax.Core;
using Parallax.Gameplay.Presentation;
using Parallax.Gameplay.Rooms;
using UnityEditor;
using UnityEngine;

namespace Parallax.Tests.EditMode
{
    // PAX-V07 §4: the death clip table (CatA_DeathClips.asset, written by PARALLAX/Setup/Cat Visual) and the resolution rule.
    public sealed class CatDeathClipTableTests
    {
        const string TablePath = "Assets/_Game/Data/CatA_DeathClips.asset";
        const string SafetyPath = "Assets/_Game/Data/RoomSafetyConfig.asset";
        const string CatPlayerPath = "Assets/_Game/Gameplay/Player/Cat_Player.prefab";

        static CatDeathClipTable Table()
        {
            var table = AssetDatabase.LoadAssetAtPath<CatDeathClipTable>(TablePath);
            Assert.NotNull(table, TablePath + " (run PARALLAX/Setup/Cat Visual)");
            return table;
        }

        [Test]
        public void TheTable_HasTheDefaultPitSpikedCrushedZappedAndArrowEntries()
        {
            CatDeathClipTable table = Table();
            foreach (CatDeathKind kind in new[] { CatDeathKind.Default, CatDeathKind.Pit, CatDeathKind.Spiked, CatDeathKind.Crushed, CatDeathKind.Zapped, CatDeathKind.Arrow })
                Assert.IsTrue(table.Has(kind), $"no {kind} entry");
        }

        // Every entry reaches its last, held frame within the hold: (frames - 1) / fps <= HoldTicks x the tick length.
        [Test]
        public void EveryEntry_ReachesItsHeldFrame_WithinTheHold()
        {
            var safety = AssetDatabase.LoadAssetAtPath<RoomSafetyConfig>(SafetyPath);
            Assert.NotNull(safety, SafetyPath);
            float hold = safety.HoldTicks * TickTime.SecondsPerTick;
            foreach (CatDeathKind kind in Enum.GetValues(typeof(CatDeathKind)))
            {
                CatClip clip = Table().For(kind);
                Assert.NotNull(clip, $"{kind}: no clip (not even Default)");
                Assert.Greater(clip.Fps, 0f, kind.ToString());
                float held = (clip.Count - 1) / clip.Fps;
                Assert.LessOrEqual(held, hold + 1e-4f, $"{kind}: the held frame shows at {held:F3} s, after the {hold:F3} s hold");
            }
        }

        [Test]
        public void AnUnmappedKind_ResolvesToTheDefaultClip()
        {
            CatDeathClipTable table = ScriptableObject.CreateInstance<CatDeathClipTable>();
            try
            {
                var frightened = new CatClip("Death", CatAnimState.Death, new Sprite[1], 10f, false, null, null);
                table.Set(CatDeathKind.Default, frightened);
                Assert.AreSame(frightened, table.For(CatDeathKind.Zapped));
            }
            finally { UnityEngine.Object.DestroyImmediate(table); }
        }

        [Test]
        public void AKillerWithoutTheInterface_WithAFallCause_ResolvesToPit()
        {
            var go = new GameObject("not a kind source");
            try
            {
                Assert.AreEqual(CatDeathKind.Pit, CatDeathKinds.Resolve(go.transform, DeathCause.Fall));
                Assert.AreEqual(CatDeathKind.Pit, CatDeathKinds.Resolve(null, DeathCause.OutOfBounds));
                Assert.AreEqual(CatDeathKind.Default, CatDeathKinds.Resolve(go.transform, DeathCause.Hazard));
            }
            finally { UnityEngine.Object.DestroyImmediate(go); CatDeathKindTagTests.RecreateUntitledScene(); }
        }

        [Test]
        public void TheRespawnClip_IsAtMostAFifthOfASecond()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CatPlayerPath);
            CatVisualPresenter presenter = prefab.GetComponentInChildren<CatVisualPresenter>(true);
            var set = (CatClipSet)typeof(CatVisualPresenter).GetField("clips", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(presenter);
            CatClip respawn = set.ForState(CatAnimState.Respawn);
            Assert.NotNull(respawn, "no Respawn clip");
            Assert.LessOrEqual(respawn.Duration, 0.2f + 1e-4f, "Respawn clip length (s)");
        }

        [Test]
        public void ThePrefabsPresenter_UsesTheTable()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CatPlayerPath);
            CatVisualPresenter presenter = prefab.GetComponentInChildren<CatVisualPresenter>(true);
            var so = new SerializedObject(presenter);
            Assert.AreEqual(Table(), so.FindProperty("deathClips").objectReferenceValue);
            Assert.NotNull(so.FindProperty("signals").objectReferenceValue, "the presenter's CatPresentationSignals");
        }
    }
}
