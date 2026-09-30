using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Parallax.Core;
using Parallax.Gameplay.Observers;
using Parallax.Gameplay.Player;
using Parallax.Gameplay.Presentation;
using Parallax.Gameplay.Reality;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Parallax.Editor.Art
{
    // Idempotent prefab and scene wiring for PAX-A03 cat animation clips.
    public static class CatVisualSetup
    {
        const string PrefabPath = "Assets/_Game/Gameplay/Player/Cat_Player.prefab";
        const string ConfigPath = "Assets/_Game/Data/CatA_VisualConfig.asset";
        const string DeathTablePath = "Assets/_Game/Data/CatA_DeathClips.asset";
        const string CatArtPath = "Assets/_Game/Art/Cats/CatA/";
        const string ShaderPath = "Assets/_Game/Art/Shaders/SpriteOutlineUnlit.shader";
        const string MaterialFolderPath = "Assets/_Game/Art/Materials";
        const string MaterialPath = MaterialFolderPath + "/Cat_OutlineUnlit.mat";
        static readonly string[] LegacyPlaceholderChildren = { "Body", "Ear_Front", "Ear_Back" };
        const string ManifestPath = "Art_Source/AutoSprite/Cats/A/_import/manifest.json";

        // PAX-V07 §5: the clip list is data covering every wired A08 slot (manifest `wired: true`; DoorEnter is imported but
        // not wired). Frame counts, cells and pivots come from the manifest; this table adds what the manifest doesn't know:
        // the state, the playback rate and loop flag, and for the locomotion clips each frame's stride.
        readonly struct ClipRow
        {
            public ClipRow(string slot, CatAnimState state, float fps, bool loop, float[] stridePx = null, int[] entry = null, int[] stance = null, int[] landEntry = null,
                int[] switchFrames = null, int[] switchTargets = null)
            {
                Slot = slot; State = state; Fps = fps; Loop = loop; StridePx = stridePx; Entry = entry ?? new int[0]; Stance = stance ?? new int[0];
                LandEntry = landEntry ?? new int[0]; SwitchFrames = switchFrames ?? new int[0]; SwitchTargets = switchTargets ?? new int[0];
            }

            public readonly string Slot;
            public readonly CatAnimState State;
            public readonly float Fps;
            public readonly bool Loop;
            public readonly float[] StridePx;   // sprite px at the manifest PPU; null = played by time
            public readonly int[] Entry;        // the frames a loop may start on from another clip (empty = any)
            public readonly int[] Stance;       // the frames a stop settles on
            public readonly int[] LandEntry;    // the frames a landing from the air enters on (item 2)
            public readonly int[] SwitchFrames; // Walk / Run: the frames that may hand over to the other gait
            public readonly int[] SwitchTargets;// ... and the other gait's frame each enters on
        }

        // Strides (PAX-V07 item 1): how far a planted paw moves back from each frame to the next, in sprite px at the
        // manifest's PPU (143.3036), measured on the imported sheets: each frame's contact pixels (its lowest opaque row,
        // alpha >= 0.5, what stands on the ground) clustered into paws (gaps over 3 px split them) and matched to the next
        // frame's (a planted paw moves 0-30 px back); the step is the median of the matched paws. Where no paw is planted
        // on both frames (Walk 10 -> 0: the fore paw lifts as the hind paw lands beside it; Run 0 -> 1), the step comes from
        // the paws within 2 px of the paw row (Walk: 11.75) or is the mean of the others (Run: 16.6). Walk alternates long
        // and short steps (loop 103.75 px = 0.724 u); Run is a gallop whose body surges (loop 166.4 px = 1.161 u). Played
        // by these strides, a planted paw stays on its print at any speed: fps(speed) = speed / stride, frame by frame.
        static readonly float[] WalkStridePx = { 12f, 7.5f, 12f, 7.25f, 11.75f, 7.5f, 10f, 7f, 9.5f, 7.5f, 11.75f };
        static readonly float[] RunStridePx = { 16.6f, 20f, 21f, 16.5f, 28.5f, 13.75f, 17f, 10.5f, 4.5f, 18f };

        // Entry and stance frames (PAX-V07 item 1, round 2), read off the sheets:
        // - Walk 1 and 7 put the hind paws and one fore paw exactly where Idle's stance has them (within 2 px, 5.5 px for
        //   Walk 7's mirrored legs) with the other fore paw reaching forward: a walk starts by lifting that paw from the
        //   stance and a stop ends by setting it down. Every other Walk frame misses an Idle paw by 16-42 px.
        // - Run 1 and 2 are the push-off: hind paws planted and driving back, fore paws reaching. Run 8 (every paw tucked
        //   together in the air) was the closest silhouette but reads as launching mid-air.
        static readonly int[] WalkStance = { 1, 7 };
        static readonly int[] RunPushOff = { 1, 2 };

        // Item 2 (see the air notes below; declared before Rows, which reads them during static initialization).
        static readonly int[] RunTouchDown = { 4 };
        // HardLand enters on its impact crouch or later (final critic): frame 0 is a tall pre-impact stand, the best centroid
        // match after a fall but a stiff pop-up on screen. The landing's pose match picks among these (ruled 2026-09-30).
        static readonly int[] HardLandEntry = { 1, 2, 3 };

        // Walk↔Run switch frames (ruled 2026-09-30: switch at the matching foot position). Measured on the sheets: the overlap
        // of the legs (the silhouette's lowest 45 px, both frames aligned at the shared pivot) between every Walk and Run frame.
        // The best-matching pairs, all above 0.43: Walk 3/4/9/10 -> Run 5 (0.45 / 0.48 / 0.44 / 0.49), and Run 5/6 -> Walk 10
        // (0.49 / 0.44), Run 3 -> Walk 3 (0.43). Every other pair is 0.19-0.42. The body still drops or rises 7.7-8.8 phone px
        // at the switch: Run's body is drawn about 12 px lower than Walk's (NEEDED_ASSETS: Walk↔Run bridge frames).
        static readonly int[] WalkSwitch = { 3, 4, 9, 10 }, WalkSwitchToRun = { 5, 5, 5, 5 };
        static readonly int[] RunSwitch = { 3, 5, 6 }, RunSwitchToWalk = { 3, 10, 10 };

        // Climb (PAX-V07 item 3): the 25-frame loop (§11 R7) is drawn climbing in place, so the body's climb over each frame
        // is how far a gripping paw moves down the cell to the next frame. Measured on the imported sheet: the vine-side fore
        // paw grips from frame 13 to 6 and the hind paw from 8 to 24; each pair's step is the vertical shift (1/4 px steps)
        // that best overlays the paw's patch on the next frame, the mean where both grip; frames 6 and 7 (the fore paw
        // letting go before the hind paw grips) take the mean. The loop climbs 79.4 px = 0.554 u. Played by these strides, a
        // gripping paw stays on its hold: at the full 4 u/s the loop runs 7.2 times a second (180 frames/s, three per display
        // frame at 60 fps); at the slow Climb 0.1 (0.4 u/s) at 18 frames/s.
        static readonly float[] ClimbStridePx = { 2.25f, 4.25f, 2.75f, 2.5f, 1.75f, 3.5f, 3f, 3f, 2.5f, 4.5f, 2.5f, 2.75f, 2.5f,
            6.6f, 3.25f, 3.75f, 3.1f, 6.1f, 2.6f, 2.25f, 2.5f, 4f, 1.6f, 1.4f, 4.5f };
        // Leap from its frame 1: frame 0 is a head-down dive off the vine with vine residue (NEEDS ART), wrong for a leap that
        // goes up at the jump speed.
        static readonly int[] LeapEntry = { 1 };

        static readonly ClipRow[] Rows =
        {
            new ClipRow("Idle", CatAnimState.Idle, 7f, true),
            new ClipRow("IdleLook", CatAnimState.IdleFidget, 12f, false),
            new ClipRow("IdleEar", CatAnimState.IdleFidget, 12f, false),
            new ClipRow("IdleSit", CatAnimState.IdleFidget, 12f, false),
            // Walk's fps is used only where it's played by time (Climb's interim use, FpsForSpeed at the reference speed).
            new ClipRow("Walk", CatAnimState.Walk, 10f, true, WalkStridePx, WalkStance, WalkStance, WalkStance, WalkSwitch, WalkSwitchToRun),
            new ClipRow("Run", CatAnimState.Run, 10f, true, RunStridePx, RunPushOff, landEntry: RunTouchDown, switchFrames: RunSwitch, switchTargets: RunSwitchToWalk),
            new ClipRow("Turn", CatAnimState.Turn, TurnFps, false),
            new ClipRow("TakeOff", CatAnimState.TakeOff, TakeOffFps, false),
            // Rise, Apex and Fall are played by the cat's velocity along gravity (CatClipSet.AirProgress), not by time.
            new ClipRow("Rise", CatAnimState.Rise, 12f, false),
            new ClipRow("Apex", CatAnimState.Apex, 12f, false),
            new ClipRow("Fall", CatAnimState.Fall, 12f, false),
            new ClipRow("Land", CatAnimState.Land, LandFps, false),
            new ClipRow("HardLand", CatAnimState.HardLand, HardLandFps, false, entry: HardLandEntry),
            new ClipRow("Death", CatAnimState.Death, 10f, false),
            new ClipRow("Death_Pit", CatAnimState.Death, 10f, false),
            new ClipRow("Death_Spiked", CatAnimState.Death, 10f, false),
            new ClipRow("Death_Crushed", CatAnimState.Death, 10f, false),
            new ClipRow("Death_Zapped", CatAnimState.Death, 10f, false),
            new ClipRow("Death_Arrow", CatAnimState.Death, 10f, false),
            new ClipRow("Respawn", CatAnimState.Respawn, 20f, false),
            new ClipRow("Flip", CatAnimState.Flip, 12f, false),
            // Climb is played by the distance climbed (its strides; the fps is unused); Hang is one frame.
            new ClipRow("Climb", CatAnimState.Climb, 21f, true, ClimbStridePx),
            new ClipRow("Hang", CatAnimState.Hang, 1f, false),
            new ClipRow("Leap", CatAnimState.Leap, LeapFps, false, entry: LeapEntry),
            new ClipRow("Door", CatAnimState.Door, 10f, false),
        };

        // Turn: CatA_Turn stays in the table (every A08 slot is wired) but isn't played. Ruled 2026-09-30: the turn flips on the
        // most symmetrical frame of the gait on screen and holds it one frame (turnHoldTime). Its 3 drawings (side, rear, front
        // three-quarter) aren't symmetric (mirror overlap 0.29-0.44) and strobed at 0.1 s (NEEDED_ASSETS: Turn redraw).
        const float TurnFps = 30f, TurnHoldTime = 1f / 60f;

        // CatVisualConfig values the setup writes (PAX-V07 item 1, round 2; see the gauntlet reports). Walk covers 0.724 u a
        // stride (11 frames), Run 1.161 u (10 frames). Played by distance, a clip's cadence is speed / stride:
        // - Run from 0.6 x 6 = 3.6 u/s: 3.1 strides/s at 31 fps, a real gallop. Below about 3 u/s the gallop's suspension
        //   frames hold for 2-3 ticks and read as slow motion (round 1's critic at analog 0.5 = 3 u/s), so Run shows only
        //   above that; back to Walk below 0.52 x 6 = 3.12 u/s (2.7 strides/s), so analog 0.5 (3.0 u/s) always walks.
        // - Walk from 0.6 u/s (walkEnter; 9 fps) to 3.6 u/s (5 strides/s, 55 fps: a brisk trotting walk). Below 0.6 a Walk
        //   frame would hold 6.5+ frames while the body glides (round 1: Walk 4 held 10 frames at 0.1-0.9 u/s); Idle there
        //   creeps at most walkExit^2 / (2 x 5 u/s^2) = 0.025 u (2 phone px) on an analog ramp.
        const float RunFraction = 0.6f, RunExitFraction = 0.52f, WalkEnterSpeed = 0.6f, WalkExitSpeed = 0.5f;

        // No ground state shows for a single frame (round 1: a one-frame Idle between Run and Turn read as a flicker).
        const int MinStateFrames = 2;

        // A digital stop or reversal brakes at the motor's 80 / 60 u/s² (6 u/s in under 0.1 s); an analog ramp over a second
        // or more changes the speed by 6 u/s² or less. 20 u/s² splits them: braking harder, the cat walks the brake out on
        // Walk frames that end on a stance (see CatClip.StanceEntry).
        const float SnapAcceleration = 20f;

        // Air (PAX-V07 item 2). A jump leaves at 9.8 u/s (1.6 u at gravity 30) and is airborne ~0.65 s. Every pose is drawn on
        // the body (ruled 2026-09-30: art defects aren't hidden in code, so no pose is drawn off its registered pivot).
        // - TakeOff: its 2 frames (crouch, push) at 60 fps, one display frame each; the body is 0.3 u up when Rise starts, so
        //   Rise 0's hind legs (0.25 u below the paws) clear the floor.
        // - airThreshold 2 u/s: the apex band lasts 4 / 30 = 0.13 s (8 frames), long enough for the Apex stretch to read
        //   between Rise and Fall; Rise and Fall each run ~0.26 s, their 3 frames following the velocity.
        // - Land 3 frames at 15 fps, HardLand 4 at 12 fps; each enters on its frame matching the fall's last pose (ruled
        //   2026-09-30; after a normal fall that is Land 2, 6 phone px from Fall 2, against 15 for Land 0) and plays to its end.
        //   HardLand recovers through Land, entered on Land's frame matching HardLand's last pose.
        // - §3 (ruled 2026-09-30): any movement input cancels the landing on the same frame; a cat moving on at touchdown goes
        //   straight into its gait. speedLeadTolerance 0.5 u/s: the motor changes the body's speed by 1.2 u/s (accelerating) or
        //   1.6 u/s (braking) in one tick; the drawn speed trails by up to a tick, so a body slower than drawn by more than 0.5
        //   is braking (a release: the landing skids in Land), not input.
        // - hardLandDistance 2.75 u: the 20 levels' solutions land after 0-2.5 u (a normal jump's own fall is 1.6-1.7 u, a jump
        //   onto or off a step 0.2-2.5 u) or after 2.97 u and more (drops off towers and cliffs, 3-14 u); 2.75 sits in that gap.
        // - Run's landing frame: Run 4, fore paws reaching to touch down (a running landing enters the gallop there).
        // - groundSinkTolerance 0.01 u: a standing cat's paws sit 0.005 u above its floor; a capsule rolling off a ledge's corner
        //   (still grounded) draws its paws into the ledge 2 sprite px (0.014 u) deep by 0.02 u of sink, so it's shown off the
        //   ground from 0.01 u.
        const float TakeOffFps = 60f, LandFps = 15f, HardLandFps = 12f;
        const float SpeedLeadTolerance = 0.5f, AirThreshold = 2f, HardLandDistance = 2.75f, AirGraceDrop = 0.03f, GroundSinkTolerance = 0.01f;

        // Climbing (PAX-V07 item 3):
        // - climbStillSpeed 0.02 u/s: the motor sets the climb speed directly (Climb x 4 u/s); still is Climb below 0.005, so
        //   the slowest analog climb (the stick just past the 0.35 dead zone) still climbs.
        // - leapSpeedFraction 0.5: a leap leaves at the jump speed (9.8 u/s at gravity 30) against gravity, a release at most
        //   the climb speed (4 u/s, a snap vine) and a geyser launch at 14: the band 6.9-12.7 u/s is a leap.
        // - Leap 1-2 at 9 fps (0.22 s), about the rise to the apex band (9.8 - 2 u/s at 30 u/s² = 0.26 s), so Apex follows it.
        const float ClimbStillSpeed = 0.02f, LeapSpeedFraction = 0.5f, LeapFps = 9f;

        // Idle fidgets (PAX-V07 item 5): the cycle look around -> ear twitch -> sit down, in the rows' order above (IdleLook,
        // IdleEar, IdleSit, 8 frames each at 12 fps = 0.67 s; the sit then holds its seated frame until input). fidgetDelay 3 s:
        // longer than a player's usual pause before a trap (1-2 s), short enough that a player waiting sees the cat's life.
        const float FidgetDelay = 3f;

        public static string[] SlotNames => Rows.Select(r => r.Slot).ToArray();

        [MenuItem("PARALLAX/Setup/Cat Visual")]
        public static void Configure()
        {
            var changes = new List<string>();
            CatVisualConfig config = GetOrCreateConfig(changes);
            Material outlineMaterial = GetOrCreateOutlineMaterial(changes);
            Manifest manifest = LoadManifest();
            Dictionary<string, Sprite[]> framesBySlot = manifest != null ? LoadFrames(manifest) : null;

            if (manifest == null || !ValidateSheets(manifest, framesBySlot))
            {
                Debug.LogError("CatVisualSetup: sheet validation failed. Stopping without saving.");
                return;
            }

            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) == null)
            {
                Debug.LogError($"CatVisualSetup: prefab not found at '{PrefabPath}'. Stopping without saving.");
                return;
            }

            int changesBeforePrefab = changes.Count;
            GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                Transform visual = root.transform.Find("Visual");
                CatMotor2D motor = root.GetComponent<CatMotor2D>();
                if (visual == null || motor == null)
                {
                    Debug.LogError($"CatVisualSetup: '{PrefabPath}' needs a Visual child and root CatMotor2D. Stopping without saving.");
                    return;
                }

                RemoveLegacyPlaceholderChildren(visual, changes);
                ApplyGroundContactOffset(root, visual, config, changes);

                Sprite initialSprite = framesBySlot["Idle"][0];
                SpriteRenderer body = visual.GetComponent<SpriteRenderer>();
                if (body == null)
                {
                    body = visual.gameObject.AddComponent<SpriteRenderer>();
                    changes.Add("added SpriteRenderer to Visual");
                }
                SetSortingLayer(body, RealitySpace.SortingLayerName(ObserverId.A, SortingBand.Gameplay), changes, "Visual");
                SetSprite(body, initialSprite, changes, "Visual");

                SpriteRenderer outline = GetOrCreateOutlineRenderer(visual, body.sortingLayerName, outlineMaterial, changes);
                SetSprite(outline, initialSprite, changes, "Outline");

                CatVisualPresenter presenter = visual.GetComponent<CatVisualPresenter>();
                if (presenter == null)
                {
                    presenter = visual.gameObject.AddComponent<CatVisualPresenter>();
                    changes.Add("added CatVisualPresenter to Visual");
                }

                var presenterSO = new SerializedObject(presenter);
                AssignObject(presenterSO, "config", config, changes, "CatVisualPresenter.config");
                AssignObject(presenterSO, "bodyRenderer", body, changes, "CatVisualPresenter.bodyRenderer");
                AssignObject(presenterSO, "outlineRenderer", outline, changes, "CatVisualPresenter.outlineRenderer");
                AssignObject(presenterSO, "motor", motor, changes, "CatVisualPresenter.motor");
                AssignClips(presenterSO, manifest, framesBySlot, changes);
                presenterSO.ApplyModifiedPropertiesWithoutUndo();

                // PAX-V07 §4-5: the death clip table (built from the clip table just written) and the scene signals.
                var clipSet = (CatClipSet)typeof(CatVisualPresenter).GetField("clips", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(presenter);
                CatDeathClipTable deathTable = GetOrCreateDeathTable(changes);
                if (!FillDeathTable(deathTable, clipSet, changes)) return;
                CatPresentationSignals signals = visual.GetComponent<CatPresentationSignals>();
                if (signals == null)
                {
                    signals = visual.gameObject.AddComponent<CatPresentationSignals>();
                    changes.Add("added CatPresentationSignals to Visual");
                }
                var signalsSO = new SerializedObject(signals);
                AssignObject(signalsSO, "motor", motor, changes, "CatPresentationSignals.motor");
                signalsSO.ApplyModifiedPropertiesWithoutUndo();
                presenterSO.Update();
                AssignObject(presenterSO, "deathClips", deathTable, changes, "CatVisualPresenter.deathClips");
                AssignObject(presenterSO, "signals", signals, changes, "CatVisualPresenter.signals");
                presenterSO.ApplyModifiedPropertiesWithoutUndo();
                SetConfigDefaults(config, motor, changes);

                if (changes.Count > changesBeforePrefab)
                    PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            // Rewrites the existing config through Unity so removed serialized fields do not linger in YAML.
            AssetDatabase.ForceReserializeAssets(new[] { ConfigPath });
            ConfigureSceneInstances(changes);
            ReportAssignments();

            Debug.Log(changes.Count == 0
                ? "CatVisualSetup: already configured."
                : $"CatVisualSetup: {string.Join("; ", changes)}.");
        }

        [Serializable] sealed class ManifestSlot { public string slot; public string file; public int frames; public int[] cell; public float[] pivotNormalized; public bool wired; }
        [Serializable] sealed class Manifest { public float ppu; public ManifestSlot[] slots; }

        static Manifest LoadManifest()
        {
            string path = Path.Combine(Path.GetDirectoryName(Application.dataPath), ManifestPath);
            if (!File.Exists(path))
            {
                Debug.LogError($"CatVisualSetup: the A08 manifest '{ManifestPath}' is missing.");
                return null;
            }
            var manifest = JsonUtility.FromJson<Manifest>(File.ReadAllText(path));
            if (manifest == null || manifest.ppu <= 0f || manifest.slots == null)
            {
                Debug.LogError($"CatVisualSetup: '{ManifestPath}' has no PPU or no slots.");
                return null;
            }
            return manifest;
        }

        static ManifestSlot SlotOf(Manifest manifest, string slot) => manifest.slots.FirstOrDefault(s => s.slot == slot);

        static string SheetPath(ManifestSlot slot) => CatArtPath + slot.file;

        static Dictionary<string, Sprite[]> LoadFrames(Manifest manifest)
        {
            var result = new Dictionary<string, Sprite[]>();
            foreach (ClipRow row in Rows)
            {
                ManifestSlot slot = SlotOf(manifest, row.Slot);
                string prefix = slot != null ? Path.GetFileNameWithoutExtension(slot.file) + "_" : "";
                result[row.Slot] = slot == null ? new Sprite[0] : AssetDatabase.LoadAllAssetsAtPath(SheetPath(slot))
                    .OfType<Sprite>()
                    .Where(sprite => sprite.name.StartsWith(prefix, StringComparison.Ordinal))
                    .OrderBy(sprite => sprite.name, StringComparer.Ordinal)
                    .ToArray();
            }
            return result;
        }

        // A08's rule (manifest `pivotRule`): every sheet has its own cell, and one world pivot (the paw row) placed in each
        // cell as the manifest records. Every row must be a wired slot, and every wired slot (bar DoorEnter) must have a row.
        static bool ValidateSheets(Manifest manifest, Dictionary<string, Sprite[]> framesBySlot)
        {
            bool valid = true;
            foreach (ManifestSlot slot in manifest.slots.Where(s => s.wired && s.slot != "DoorEnter"))
                if (!Rows.Any(r => r.Slot == slot.slot))
                {
                    Debug.LogError($"CatVisualSetup: the wired A08 slot {slot.slot} has no row in the clip table.");
                    valid = false;
                }
            foreach (ClipRow row in Rows)
            {
                ManifestSlot slot = SlotOf(manifest, row.Slot);
                if (slot == null || !slot.wired)
                {
                    Debug.LogError($"CatVisualSetup: {row.Slot} isn't a wired slot in {ManifestPath}.");
                    valid = false;
                    continue;
                }
                var importer = AssetImporter.GetAtPath(SheetPath(slot)) as TextureImporter;
                Sprite[] frames = framesBySlot[row.Slot];
                if (importer == null || frames.Length != slot.frames)
                {
                    Debug.LogError($"CatVisualSetup: {row.Slot} has {frames.Length} imported sprites at '{SheetPath(slot)}', the manifest {slot.frames}.");
                    valid = false;
                    continue;
                }
                if (Mathf.Abs(importer.spritePixelsPerUnit - manifest.ppu) > 0.01f)
                {
                    Debug.LogError($"CatVisualSetup: {row.Slot} PPU {importer.spritePixelsPerUnit} != the manifest's {manifest.ppu}.");
                    valid = false;
                }
                if (row.StridePx != null && row.StridePx.Length != slot.frames)
                {
                    Debug.LogError($"CatVisualSetup: {row.Slot} has {row.StridePx.Length} strides for {slot.frames} frames.");
                    valid = false;
                }
                var pivot = new Vector2(slot.pivotNormalized[0], slot.pivotNormalized[1]);
                foreach (Sprite frame in frames)
                {
                    Vector2 framePivot = new(frame.pivot.x / frame.rect.width, frame.pivot.y / frame.rect.height);
                    if (!Mathf.Approximately(frame.rect.width, slot.cell[0]) || !Mathf.Approximately(frame.rect.height, slot.cell[1])
                        || Vector2.Distance(framePivot, pivot) > 0.002f)
                    {
                        Debug.LogError($"CatVisualSetup: '{frame.name}' isn't the manifest's {slot.cell[0]}x{slot.cell[1]} cell with pivot {pivot}.");
                        valid = false;
                    }
                }
            }
            return valid;
        }

        // Each frame's silhouette centroid (alpha >= 0.5, as the capture checks count a pixel drawn), relative to the pivot,
        // in world units at facing +1: read from the sheet PNG on disk, so the importer needs no Read/Write.
        static Vector2[] PoseCentroids(Sprite[] frames)
        {
            var result = new Vector2[frames.Length];
            if (frames.Length == 0) return result;
            string path = AssetDatabase.GetAssetPath(frames[0].texture);
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave };
            try
            {
                if (!tex.LoadImage(File.ReadAllBytes(path))) throw new InvalidOperationException($"can't read '{path}'");
                Color32[] pixels = tex.GetPixels32();
                float scale = tex.width / (float)frames[0].texture.width;
                for (int f = 0; f < frames.Length; f++)
                {
                    Rect r = frames[f].rect;
                    int x0 = Mathf.RoundToInt(r.x * scale), y0 = Mathf.RoundToInt(r.y * scale), w = Mathf.RoundToInt(r.width * scale), h = Mathf.RoundToInt(r.height * scale);
                    double sx = 0, sy = 0; int n = 0;
                    for (int y = 0; y < h; y++)
                        for (int x = 0; x < w; x++)
                            if (pixels[(y0 + y) * tex.width + x0 + x].a >= 128) { sx += x + 0.5; sy += y + 0.5; n++; }
                    Vector2 pivot = frames[f].pivot * scale;
                    float ppu = frames[f].pixelsPerUnit * scale;
                    result[f] = n == 0 ? Vector2.zero : new Vector2((float)(sx / n - pivot.x) / ppu, (float)(sy / n - pivot.y) / ppu);
                }
            }
            finally
            {
                Object.DestroyImmediate(tex);
            }
            return result;
        }

        static void AssignClips(SerializedObject presenter, Manifest manifest, Dictionary<string, Sprite[]> framesBySlot, List<string> changes)
        {
            SerializedProperty list = presenter.FindProperty("clips.clips");
            if (list == null)
            {
                Debug.LogError("CatVisualSetup: CatVisualPresenter has no 'clips' table.");
                return;
            }
            bool changed = list.arraySize != Rows.Length;
            list.arraySize = Rows.Length;
            for (int i = 0; i < Rows.Length; i++)
            {
                ClipRow row = Rows[i];
                Sprite[] frames = framesBySlot[row.Slot];
                SerializedProperty clip = list.GetArrayElementAtIndex(i);
                changed |= SetString(clip.FindPropertyRelative("slot"), row.Slot);
                changed |= SetInt(clip.FindPropertyRelative("state"), (int)row.State);
                changed |= SetFloat(clip.FindPropertyRelative("fps"), row.Fps);
                changed |= SetBool(clip.FindPropertyRelative("loop"), row.Loop);
                SerializedProperty sprites = clip.FindPropertyRelative("frames");
                if (!SameSprites(sprites, frames))
                {
                    sprites.arraySize = frames.Length;
                    for (int k = 0; k < frames.Length; k++) sprites.GetArrayElementAtIndex(k).objectReferenceValue = frames[k];
                    changed = true;
                }
                float[] strides = row.StridePx != null ? row.StridePx.Select(px => px / manifest.ppu).ToArray() : new float[0];
                changed |= SetFloats(clip.FindPropertyRelative("strideUnits"), strides);
                changed |= SetInts(clip.FindPropertyRelative("entryFrames"), row.Entry);
                changed |= SetInts(clip.FindPropertyRelative("stanceFrames"), row.Stance);
                changed |= SetInts(clip.FindPropertyRelative("landEntryFrames"), row.LandEntry);
                changed |= SetInts(clip.FindPropertyRelative("switchFrames"), row.SwitchFrames);
                changed |= SetInts(clip.FindPropertyRelative("switchTargets"), row.SwitchTargets);
                Vector2[] centroids = PoseCentroids(frames);
                SerializedProperty poses = clip.FindPropertyRelative("poseCentroids");
                if (poses.arraySize != centroids.Length) { poses.arraySize = centroids.Length; changed = true; }
                for (int k = 0; k < centroids.Length; k++)
                {
                    SerializedProperty p = poses.GetArrayElementAtIndex(k);
                    if ((p.vector2Value - centroids[k]).sqrMagnitude > 1e-12f) { p.vector2Value = centroids[k]; changed = true; }
                }
            }
            if (changed) changes.Add($"assigned the clip table ({Rows.Length} clips)");
        }

        static void SetConfigDefaults(CatVisualConfig config, CatMotor2D motor, List<string> changes)
        {
            var so = new SerializedObject(config);
            Object motorConfig = new SerializedObject(motor).FindProperty("config")?.objectReferenceValue;
            bool changed = AssignObject(so, "motorConfig", motorConfig)
                | SetFloat(so.FindProperty("runFraction"), RunFraction)
                | SetFloat(so.FindProperty("runExitFraction"), RunExitFraction)
                | SetFloat(so.FindProperty("snapAcceleration"), SnapAcceleration)
                | SetFloat(so.FindProperty("idleSpeedThreshold"), WalkEnterSpeed)
                | SetFloat(so.FindProperty("walkExit"), WalkExitSpeed)
                | SetInt(so.FindProperty("minStateFrames"), MinStateFrames)
                | SetFloat(so.FindProperty("airThreshold"), AirThreshold)
                | SetFloat(so.FindProperty("hardLandDistance"), HardLandDistance)
                | SetFloat(so.FindProperty("airGraceDrop"), AirGraceDrop)
                | SetFloat(so.FindProperty("groundSinkTolerance"), GroundSinkTolerance)
                | SetFloat(so.FindProperty("speedLeadTolerance"), SpeedLeadTolerance)
                | SetFloat(so.FindProperty("climbStillSpeed"), ClimbStillSpeed)
                | SetFloat(so.FindProperty("leapSpeedFraction"), LeapSpeedFraction)
                | SetFloat(so.FindProperty("turnHoldTime"), TurnHoldTime)
                | SetFloat(so.FindProperty("fidgetDelay"), FidgetDelay);
            if (!changed) return;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssetIfDirty(config);
            changes.Add($"set CatA_VisualConfig thresholds (run {RunFraction} / {RunExitFraction} x MaxSpeed, walk {WalkEnterSpeed} / {WalkExitSpeed} u/s, {MinStateFrames} frames, air {AirThreshold} u/s, hard land {HardLandDistance} u) and motor config");
        }

        static bool SetString(SerializedProperty p, string v) { if (p.stringValue == v) return false; p.stringValue = v; return true; }
        static bool SetInt(SerializedProperty p, int v) { if (p.intValue == v) return false; p.intValue = v; return true; }
        static bool SetBool(SerializedProperty p, bool v) { if (p.boolValue == v) return false; p.boolValue = v; return true; }
        static bool SetFloat(SerializedProperty p, float v) { if (Mathf.Approximately(p.floatValue, v)) return false; p.floatValue = v; return true; }

        static bool SetInts(SerializedProperty p, int[] values)
        {
            bool changed = false;
            if (p.arraySize != values.Length) { p.arraySize = values.Length; changed = true; }
            for (int i = 0; i < values.Length; i++) changed |= SetInt(p.GetArrayElementAtIndex(i), values[i]);
            return changed;
        }

        static bool SetFloats(SerializedProperty p, float[] values)
        {
            bool changed = false;
            if (p.arraySize != values.Length) { p.arraySize = values.Length; changed = true; }
            for (int i = 0; i < values.Length; i++) changed |= SetFloat(p.GetArrayElementAtIndex(i), values[i]);
            return changed;
        }

        static void ConfigureSceneInstances(List<string> changes)
        {
            for (int sceneIndex = 0; sceneIndex < SceneManager.sceneCount; sceneIndex++)
            {
                Scene scene = SceneManager.GetSceneAt(sceneIndex);
                if (!scene.isLoaded) continue;
                bool sceneChanged = false;
                foreach (GameObject root in scene.GetRootGameObjects())
                foreach (CatMotor2D cat in root.GetComponentsInChildren<CatMotor2D>(true))
                {
                    RealityRoot reality = cat.GetComponentInParent<RealityRoot>();
                    Transform visual = cat.transform.Find("Visual");
                    if (reality == null || visual == null) continue;

                    SpriteRenderer body = visual.GetComponent<SpriteRenderer>();
                    Transform outlineTransform = visual.Find("Outline");
                    SpriteRenderer outline = outlineTransform != null ? outlineTransform.GetComponent<SpriteRenderer>() : null;
                    string sortingLayer = RealitySpace.SortingLayerName(reality.Id, SortingBand.Gameplay);
                    sceneChanged |= ApplyInstanceRenderer(visual, body, reality.gameObject.layer, sortingLayer, changes);
                    sceneChanged |= ApplyInstanceRenderer(outlineTransform, outline, reality.gameObject.layer, sortingLayer, changes);
                    sceneChanged |= AssignSceneReferences(cat, visual, changes);
                    sceneChanged |= RemoveUnusedOverrides(cat.gameObject, changes);
                }
                if (sceneChanged) EditorSceneManager.MarkSceneDirty(scene);
            }
        }

        static bool AssignSceneReferences(CatMotor2D cat, Transform visual, List<string> changes)
        {
            CatVisualPresenter presenter = visual.GetComponent<CatVisualPresenter>();
            if (presenter == null) return false;
            ObserverContext match = Object.FindObjectsByType<ObserverContext>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .FirstOrDefault(context => context.Cat == cat);
            var serialized = new SerializedObject(presenter);
            bool changed = AssignObject(serialized, "motor", cat)
                | AssignObject(serialized, "observer", match);
            if (!changed) return false;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            changes.Add($"assigned {cat.name} presenter references");
            return true;
        }

        static void ReportAssignments()
        {
            var reported = new HashSet<ObserverId>();
            foreach (CatMotor2D cat in Object.FindObjectsByType<CatMotor2D>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                RealityRoot reality = cat.GetComponentInParent<RealityRoot>();
                CatVisualPresenter presenter = cat.transform.Find("Visual")?.GetComponent<CatVisualPresenter>();
                if (reality == null || presenter == null || !reported.Add(reality.Id)) continue;

                var serialized = new SerializedObject(presenter);
                var missing = new List<string>();
                SerializedProperty list = serialized.FindProperty("clips.clips");
                foreach (ClipRow row in Rows)
                {
                    bool found = false;
                    for (int i = 0; list != null && i < list.arraySize; i++)
                    {
                        SerializedProperty clip = list.GetArrayElementAtIndex(i);
                        found |= clip.FindPropertyRelative("slot").stringValue == row.Slot && clip.FindPropertyRelative("frames").arraySize > 0;
                    }
                    if (!found) missing.Add(row.Slot);
                }
                int missingRenderers = serialized.FindProperty("bodyRenderer").objectReferenceValue == null ? 1 : 0;
                if (serialized.FindProperty("outlineRenderer").objectReferenceValue == null) missingRenderers++;
                Debug.Log($"CatVisualSetup: Reality {reality.Id}: {Rows.Length - missing.Count}/{Rows.Length} clips assigned; unassigned slots: {(missing.Count == 0 ? "none" : string.Join(", ", missing))}; unassigned renderers: {missingRenderers}.");
            }
            foreach (ObserverId id in new[] { ObserverId.A, ObserverId.B })
                if (!reported.Contains(id)) Debug.LogWarning($"CatVisualSetup: Reality {id}: 0/{Rows.Length} clips assigned; cat not found in loaded scenes.");
        }

        static void ApplyGroundContactOffset(GameObject root, Transform visual, CatVisualConfig config, List<string> changes)
        {
            CapsuleCollider2D collider = root.GetComponent<CapsuleCollider2D>();
            if (collider == null)
            {
                Debug.LogError($"CatVisualSetup: '{PrefabPath}' has no CapsuleCollider2D; cannot place Visual at ground contact.");
                return;
            }
            var target = new Vector3(collider.offset.x, collider.offset.y - collider.size.y * 0.5f + config.GroundOffset, visual.localPosition.z);
            if (visual.localPosition == target) return;
            visual.localPosition = target;
            changes.Add($"set Visual.localPosition = {target}");
        }

        static void RemoveLegacyPlaceholderChildren(Transform visual, List<string> changes)
        {
            foreach (string childName in LegacyPlaceholderChildren)
            {
                Transform child = visual.Find(childName);
                if (child == null) continue;
                Object.DestroyImmediate(child.gameObject);
                changes.Add($"removed legacy placeholder '{childName}'");
            }
        }

        static SpriteRenderer GetOrCreateOutlineRenderer(Transform visual, string sortingLayer, Material material, List<string> changes)
        {
            Transform child = visual.Find("Outline");
            if (child == null)
            {
                var childObject = new GameObject("Outline");
                childObject.transform.SetParent(visual, false);
                child = childObject.transform;
                changes.Add("created Outline child");
            }
            SpriteRenderer renderer = child.GetComponent<SpriteRenderer>();
            if (renderer == null)
            {
                renderer = child.gameObject.AddComponent<SpriteRenderer>();
                changes.Add("added SpriteRenderer to Outline");
            }
            SetSortingLayer(renderer, sortingLayer, changes, "Outline");
            if (renderer.sortingOrder != -1)
            {
                renderer.sortingOrder = -1;
                changes.Add("set Outline sorting order");
            }
            if (renderer.sharedMaterial != material)
            {
                renderer.sharedMaterial = material;
                changes.Add("assigned Outline material");
            }
            return renderer;
        }

        // PAX-V07 §4: death kind -> slot. Unmapped kinds (Splash, when it comes) fall back to Default, the frightened pose.
        static readonly (CatDeathKind kind, string slot)[] DeathSlots =
        {
            (CatDeathKind.Default, "Death"), (CatDeathKind.Pit, "Death_Pit"), (CatDeathKind.Spiked, "Death_Spiked"),
            (CatDeathKind.Crushed, "Death_Crushed"), (CatDeathKind.Zapped, "Death_Zapped"), (CatDeathKind.Arrow, "Death_Arrow"),
        };

        static CatDeathClipTable GetOrCreateDeathTable(List<string> changes)
        {
            CatDeathClipTable table = AssetDatabase.LoadAssetAtPath<CatDeathClipTable>(DeathTablePath);
            if (table != null) return table;
            table = ScriptableObject.CreateInstance<CatDeathClipTable>();
            AssetDatabase.CreateAsset(table, DeathTablePath);
            changes.Add("created CatA_DeathClips asset");
            return table;
        }

        // Copies each death slot's clip from the clip table into the death table, when it differs. False on a missing slot.
        static bool FillDeathTable(CatDeathClipTable table, CatClipSet clips, List<string> changes)
        {
            bool changed = false;
            foreach ((CatDeathKind kind, string slot) in DeathSlots)
            {
                CatClip source = clips?.ForSlot(slot);
                if (source == null || source.Count == 0)
                {
                    Debug.LogError($"CatVisualSetup: the clip table has no {slot} clip for the {kind} death. Stopping without saving.");
                    return false;
                }
                CatClip have = table.Exact(kind);
                if (have != null && have.Slot == source.Slot && Mathf.Approximately(have.Fps, source.Fps) && have.Frames.SequenceEqual(source.Frames)
                    && have.PoseCentroids.SequenceEqual(source.PoseCentroids)) continue;
                table.Set(kind, new CatClip(source.Slot, CatAnimState.Death, source.Frames.ToArray(), source.Fps, false, null, source.PoseCentroids.ToArray()));
                changed = true;
            }
            if (!changed) return true;
            EditorUtility.SetDirty(table);
            AssetDatabase.SaveAssetIfDirty(table);
            changes.Add($"filled CatA_DeathClips ({DeathSlots.Length} kinds)");
            return true;
        }

        static CatVisualConfig GetOrCreateConfig(List<string> changes)
        {
            CatVisualConfig config = AssetDatabase.LoadAssetAtPath<CatVisualConfig>(ConfigPath);
            if (config != null) return config;
            config = ScriptableObject.CreateInstance<CatVisualConfig>();
            AssetDatabase.CreateAsset(config, ConfigPath);
            changes.Add("created CatA_VisualConfig asset");
            return config;
        }

        static Material GetOrCreateOutlineMaterial(List<string> changes)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material != null) return material;
            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath);
            if (shader == null)
            {
                Debug.LogError($"CatVisualSetup: shader not found at '{ShaderPath}'.");
                return null;
            }
            if (!AssetDatabase.IsValidFolder(MaterialFolderPath)) AssetDatabase.CreateFolder("Assets/_Game/Art", "Materials");
            material = new Material(shader) { name = "Cat_OutlineUnlit" };
            AssetDatabase.CreateAsset(material, MaterialPath);
            changes.Add("created Cat_OutlineUnlit material asset");
            return material;
        }

        static bool ApplyInstanceRenderer(Transform target, SpriteRenderer renderer, int layer, string sortingLayer, List<string> changes)
        {
            if (target == null || renderer == null) return false;
            bool changed = false;
            if (target.gameObject.layer != layer)
            {
                target.gameObject.layer = layer;
                changed = true;
            }
            if (renderer.sortingLayerName != sortingLayer)
            {
                renderer.sortingLayerName = sortingLayer;
                changed = true;
            }
            if (changed) changes.Add($"set {target.name} reality layer/sorting");
            return changed;
        }

        static bool RemoveUnusedOverrides(GameObject instanceRoot, List<string> changes)
        {
            PropertyModification[] modifications = PrefabUtility.GetPropertyModifications(instanceRoot);
            int legacyCount = modifications == null ? 0 : modifications.Count(modification =>
                TargetName(modification.target) == "Body" || TargetName(modification.target) == "Ear_Front" || TargetName(modification.target) == "Ear_Back");
            PrefabUtility.RemoveUnusedOverrides(new[] { instanceRoot }, InteractionMode.AutomatedAction);
            if (legacyCount == 0) return false;
            changes.Add($"removed {legacyCount} unused overrides from {instanceRoot.name}");
            return true;
        }

        static string TargetName(Object target)
        {
            if (target is Component component) return component.gameObject.name;
            if (target is GameObject gameObject) return gameObject.name;
            return target != null ? target.name : string.Empty;
        }

        static void AssignObject(SerializedObject serialized, string propertyName, Object value, List<string> changes, string label)
        {
            if (!AssignObject(serialized, propertyName, value)) return;
            changes.Add($"assigned {label}");
        }

        static bool AssignObject(SerializedObject serialized, string propertyName, Object value)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property.objectReferenceValue == value) return false;
            property.objectReferenceValue = value;
            return true;
        }

        static void SetSortingLayer(SpriteRenderer renderer, string value, List<string> changes, string label)
        {
            if (renderer.sortingLayerName == value) return;
            renderer.sortingLayerName = value;
            changes.Add($"set {label} sorting layer");
        }

        static void SetSprite(SpriteRenderer renderer, Sprite value, List<string> changes, string label)
        {
            if (renderer.sprite == value) return;
            renderer.sprite = value;
            changes.Add($"set {label} sprite");
        }

        static bool SameSprites(SerializedProperty property, Sprite[] frames)
        {
            if (property.arraySize != frames.Length) return false;
            for (int i = 0; i < frames.Length; i++)
                if (property.GetArrayElementAtIndex(i).objectReferenceValue != frames[i]) return false;
            return true;
        }
    }
}
