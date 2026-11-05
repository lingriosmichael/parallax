using System.Collections.Generic;
using Parallax.DebugTools;
using Parallax.Gameplay.Input;
using Parallax.Gameplay.Levels;
using Parallax.Gameplay.Observers;
using Parallax.Gameplay.Rooms;
using Parallax.Gameplay.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Parallax.Editor.Setup
{
    /// <summary>PAX-A09: one menu for the in-game UI art. Sets the art's import, then rebuilds Level Select and _LevelTemplate's panels and play HUD in
    /// the title's look, and saves both scenes. Rebuild All Levels then carries the template into every Level_NNN (D-070). Idempotent. The play HUD
    /// (portrait, progress line, stick, jump) takes no touches: the stick and jump stay TouchStickCatInput's zones, so no reserved region changes.</summary>
    public static class GameUiSetup
    {
        const string LevelSelectPath = "Assets/_Game/Scenes/LevelSelect.unity";
        const string TemplatePath = "Assets/_Game/Scenes/Levels/_LevelTemplate.unity";
        const string HudCanvasName = "HUD", DeviceInputName = "DeviceInput";
        public const string PlayHudName = "PlayHud";
        // The middle of TouchStickCatInput's stick and jump zones' lower parts, as fractions of the safe area.
        static readonly Vector2 StickHome = new Vector2(0.13f, 0.25f), JumpHome = new Vector2(0.89f, 0.22f);
        // D-324 (PAX-129): up and to the left of the jump button, about 40 px from its ring at 16:9, inside TouchStickCatInput's
        // whisker zone (which stops short of the jump button, and reaches further left and up for the thumb).
        static readonly Vector2 WhiskerHome = new Vector2(0.80f, 0.30f);
        const float JumpSize = 172f, WhiskerSize = 128f, PortraitSize = 96f, LineLength = 300f;

        [MenuItem("PARALLAX/Setup/Levels/Game UI (PAX-A09)")]
        public static void Configure()
        {
            Scene active = EditorSceneManager.GetActiveScene();
            if (active.isDirty) { Debug.LogError("GameUiSetup: '" + active.name + "' has unsaved changes; save or discard them first."); return; }
            string originalPath = active.path;

            var report = new List<string>();
            ImportArt(report);
            ConfigureLevelSelect(report);
            ConfigureTemplate(report);

            if (!string.IsNullOrEmpty(originalPath) && AssetDatabase.LoadAssetAtPath<Object>(originalPath) != null)
                EditorSceneManager.OpenScene(originalPath, OpenSceneMode.Single);
            Debug.Log("GameUiSetup:\n" + string.Join("\n", report) + "\nRun PARALLAX/Setup/Levels/Rebuild All Levels next, so every level gets the template's.");
        }

        static void ConfigureLevelSelect(List<string> report)
        {
            Scene scene = EditorSceneManager.OpenScene(LevelSelectPath, OpenSceneMode.Single);
            LevelSelectSetup.Configure();
            GameObject canvas = GameObject.Find("Canvas");
            if (canvas == null) { Debug.LogError("GameUiSetup: LevelSelect has no Canvas; run its menu first."); return; }
            var changes = new List<string>();
            PaidUnlockSetup.BuildPanel(canvas.transform, false, changes);
            Save(scene, changes, report);
        }

        static void ConfigureTemplate(List<string> report)
        {
            Scene scene = EditorSceneManager.OpenScene(TemplatePath, OpenSceneMode.Single);
            LevelCompleteUISetup.Configure();
            PauseMenuSetup.Configure();

            GameObject canvas = GameObject.Find(HudCanvasName);
            var touch = GameObject.Find(DeviceInputName)?.GetComponent<TouchStickCatInput>();
            var rooms = Object.FindAnyObjectByType<RoomManager>();
            var screen = Object.FindAnyObjectByType<LevelCompleteScreen>(FindObjectsInactive.Include);
            if (canvas == null || touch == null || rooms == null || screen == null) { Debug.LogError("GameUiSetup: the template has no HUD, TouchStickCatInput, RoomManager or LevelCompleteScreen; run the level UI menus first."); return; }

            var changes = new List<string>();
            PaidUnlockSetup.BuildPanel(canvas.transform, true, changes);
            GameObject hud = BuildPlayHud(canvas.transform, changes);
            TrapKitSetup.Write(hud.GetComponent<TouchControlsView>(), changes, ("input", touch), ("rooms", rooms), ("observers", Object.FindAnyObjectByType<ObserverSet>()));
            TrapKitSetup.Write(touch, changes, ("whiskerZone", TouchStickCatInput.LevelWhiskerZone), ("showDebugOverlay", false));   // D-324: the zone the button sits on
            HideDevTools(touch, changes);
            TrapKitSetup.Write(hud.GetComponentInChildren<LevelProgressLine>(true), changes, ("rooms", rooms));
            TrapKitSetup.Write(screen, changes, ("playHud", hud));
            Save(scene, changes, report);
        }

        // The developer, 2026-11-05: "There are a lot of dev tools on the screen ... remove all of that." The frozen co-op dev tools the template
        // inherited (Echo's record button and timeline, the reality switch, the FLIP button, the debug panel and labels) stay in the scene, off,
        // so nothing that references them breaks; their code is untouched. The touch input's debug overlay is off above, and the hidden tools leave
        // its reserved regions, so their old spots on screen take touches again.
        static void HideDevTools(TouchStickCatInput touch, List<string> changes)
        {
            var hidden = new List<GameObject>();
            foreach (string name in new[] { "RecordButton", "EchoTimeline", "SwitchButton" })
            {
                GameObject go = FindIncludingInactive(name);
                if (go == null) continue;
                hidden.Add(go);
                if (go.activeSelf) { go.SetActive(false); EditorUtility.SetDirty(go); changes.Add("hid " + name); }
            }
            var tools = new List<Behaviour>();
            foreach (Behaviour tool in new Behaviour[]
                     {
                         Object.FindAnyObjectByType<GravityDebugControl>(FindObjectsInactive.Include),
                         Object.FindAnyObjectByType<DebugPanel>(FindObjectsInactive.Include),
                         Object.FindAnyObjectByType<WorldLabelOverlay>(FindObjectsInactive.Include),
                     })
            {
                if (tool == null) continue;
                tools.Add(tool);
                if (tool.enabled) { tool.enabled = false; EditorUtility.SetDirty(tool); changes.Add("turned off " + tool.GetType().Name); }
            }

            // Only the dev tools leave: the panels (pause, level complete, unlock) are inactive until they open, and stay reserved.
            var serialized = new SerializedObject(touch);
            SerializedProperty regions = serialized.FindProperty("reservedRegions");
            for (int i = regions.arraySize - 1; i >= 0; i--)
            {
                var region = regions.GetArrayElementAtIndex(i).objectReferenceValue as Behaviour;
                if (region == null || !(tools.Contains(region) || hidden.Exists(go => region.transform.IsChildOf(go.transform)))) continue;
                changes.Add("unreserved " + region.GetType().Name);
                regions.DeleteArrayElementAtIndex(i);
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        static GameObject FindIncludingInactive(string name)
        {
            foreach (Transform t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (t.name == name) return t.gameObject;
            return null;
        }

        static void Save(Scene scene, List<string> changes, List<string> report)
        {
            EditorSceneManager.MarkSceneDirty(scene);   // the menus above report their own changes, and only mark the scene when they made some
            EditorSceneManager.SaveScene(scene);
            report.Add(scene.name + ": " + (changes.Count == 0 ? "no changes" : string.Join(", ", changes)) + " (saved)");
        }

        /// <summary>Builds (or re-checks) the play HUD under `hud`, below every panel, so an open panel covers it. No scene lookups, so a test can build it on
        /// a throwaway parent.</summary>
        public static GameObject BuildPlayHud(Transform hud, List<string> changes)
        {
            GameObject root = PauseMenuSetup.EnsureChild(hud, PlayHudName, changes);
            PauseMenuSetup.SetAnchors((RectTransform)root.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, changes, PlayHudName);
            PauseMenuSetup.EnsureComponent<SafeAreaFit>(root, changes, PlayHudName);
            if (root.transform.GetSiblingIndex() != 0) { root.transform.SetAsFirstSibling(); changes.Add("moved " + PlayHudName + " under the panels"); }

            BuildPortrait(root.transform, changes);
            var center = new Vector2(0.5f, 0.5f);

            GameObject stickGO = PauseMenuSetup.EnsureChild(root.transform, "Stick", changes);
            GameUiStyle.Place((RectTransform)stickGO.transform, StickHome, Vector2.one * 260f, Vector2.zero, changes, "Stick");
            GameUiStyle.Paint(PauseMenuSetup.EnsureComponent<Image>(stickGO, changes, "Stick"), GameUiStyle.Art("GUI_Ring"), Color.white, false, changes, "Stick");
            var stickGroup = PauseMenuSetup.EnsureComponent<CanvasGroup>(stickGO, changes, "Stick");
            Quiet(stickGroup, changes, "Stick");
            Image knob = GameUiStyle.Picture(stickGO.transform, "Knob", center, Vector2.one * 112f, Vector2.zero, GameUiStyle.Art("GUI_Knob"), Color.white, changes);

            GameObject jumpGO = PauseMenuSetup.EnsureChild(root.transform, "JumpButton", changes);
            GameUiStyle.Place((RectTransform)jumpGO.transform, JumpHome, Vector2.one * JumpSize, Vector2.zero, changes, "JumpButton");
            GameUiStyle.Paint(PauseMenuSetup.EnsureComponent<Image>(jumpGO, changes, "JumpButton"), GameUiStyle.Art("GUI_Ring"), Color.white, false, changes, "JumpButton");
            var jumpGroup = PauseMenuSetup.EnsureComponent<CanvasGroup>(jumpGO, changes, "JumpButton");
            Quiet(jumpGroup, changes, "JumpButton");
            // D-324 (the developer: "replace with a cat paw"): the paw takes the chevron's place.
            Transform chevron = jumpGO.transform.Find("Chevron");
            if (chevron != null) { Object.DestroyImmediate(chevron.gameObject); changes.Add("removed JumpButton/Chevron"); }
            GameUiStyle.Picture(jumpGO.transform, "Paw", center, Vector2.one * (JumpSize * 0.6f), Vector2.zero, GameUiStyle.Art("GUI_Paw"), Color.white, changes);

            // D-324 (PAX-129): the whisker button, a ring that refills in gold over the dodge's cooldown, the whiskers inside.
            GameObject whiskerGO = PauseMenuSetup.EnsureChild(root.transform, "WhiskerButton", changes);
            GameUiStyle.Place((RectTransform)whiskerGO.transform, WhiskerHome, Vector2.one * WhiskerSize, Vector2.zero, changes, "WhiskerButton");
            GameUiStyle.Paint(PauseMenuSetup.EnsureComponent<Image>(whiskerGO, changes, "WhiskerButton"), GameUiStyle.Art("GUI_Ring"), Color.white, false, changes, "WhiskerButton");
            var whiskerGroup = PauseMenuSetup.EnsureComponent<CanvasGroup>(whiskerGO, changes, "WhiskerButton");
            Quiet(whiskerGroup, changes, "WhiskerButton");
            Image fill = GameUiStyle.Picture(whiskerGO.transform, "Refill", center, Vector2.one * (WhiskerSize + 8f), Vector2.zero, GameUiStyle.Art("GUI_Ring"), GameUiStyle.Gold, changes);
            if (fill.type != Image.Type.Filled || fill.fillMethod != Image.FillMethod.Radial360 || fill.fillOrigin != (int)Image.Origin360.Top || !fill.fillClockwise)
            {
                fill.type = Image.Type.Filled;
                fill.fillMethod = Image.FillMethod.Radial360;
                fill.fillOrigin = (int)Image.Origin360.Top;
                fill.fillClockwise = true;
                fill.fillAmount = 1f;
                changes.Add("set Refill to fill round from the top");
            }
            GameUiStyle.Picture(whiskerGO.transform, "Whiskers", center, Vector2.one * (WhiskerSize * 0.66f), Vector2.zero, GameUiStyle.Art("GUI_Whiskers"), Color.white, changes);

            var view = PauseMenuSetup.EnsureComponent<TouchControlsView>(root, changes, PlayHudName);
            TrapKitSetup.Write(view, changes, ("stickRest", 0.22f), ("stickHeld", 0.5f), ("stickBase", stickGO.transform), ("stickKnob", knob.transform), ("stick", stickGroup), ("jump", jumpGO.transform), ("jumpGroup", jumpGroup),
                ("whisker", whiskerGO.transform), ("whiskerGroup", whiskerGroup), ("whiskerFill", fill));
            return root;
        }

        static void Quiet(CanvasGroup group, List<string> changes, string label)
        {
            if (!group.blocksRaycasts && !group.interactable) return;
            group.blocksRaycasts = false;
            group.interactable = false;
            changes.Add("set " + label + " to take no touches");
        }

        // The cat's portrait, top left, with the level's line running out of it to a diamond at the door's end.
        static void BuildPortrait(Transform root, List<string> changes)
        {
            var corner = new Vector2(0f, 1f);
            var at = new Vector2(PortraitSize * 0.5f + 44f, -PortraitSize * 0.5f - 24f);
            GameUiStyle.Picture(root, "Portrait", corner, Vector2.one * PortraitSize, at, GameUiStyle.Art("GUI_Portrait"), Color.white, changes);
            GameUiStyle.Picture(root, "PortraitRing", corner, Vector2.one * (PortraitSize + 12f), at, GameUiStyle.Art("GUI_Ring"), Color.white, changes);

            GameObject lineGO = PauseMenuSetup.EnsureChild(root, "ProgressLine", changes);
            float x = at.x + PortraitSize * 0.5f + 22f + LineLength * 0.5f;
            GameUiStyle.Place((RectTransform)lineGO.transform, corner, new Vector2(LineLength, 4f), new Vector2(x, at.y), changes, "ProgressLine");
            var mid = new Vector2(0.5f, 0.5f);
            Image shadow = GameUiStyle.Picture(lineGO.transform, "Shadow", mid, new Vector2(LineLength + 4f, 8f), Vector2.zero, GameUiStyle.Art("GUI_Bar"), new Color(0.08f, 0.04f, 0.02f, 0.4f), changes);
            Image track = GameUiStyle.Picture(lineGO.transform, "Track", mid, new Vector2(LineLength, 3f), Vector2.zero, GameUiStyle.Art("GUI_Bar"), new Color(1f, 0.97f, 0.9f, 0.5f), changes);
            Image lit = GameUiStyle.Picture(lineGO.transform, "Lit", mid, new Vector2(LineLength, 4f), Vector2.zero, GameUiStyle.Art("GUI_Bar"), GameUiStyle.Gold, changes);
            if (lit.type != Image.Type.Filled || lit.fillMethod != Image.FillMethod.Horizontal || lit.fillOrigin != (int)Image.OriginHorizontal.Left)
            {
                lit.type = Image.Type.Filled;
                lit.fillMethod = Image.FillMethod.Horizontal;
                lit.fillOrigin = (int)Image.OriginHorizontal.Left;
                lit.fillAmount = 0f;
                changes.Add("set Lit to fill from the left");
            }
            Image door = GameUiStyle.Picture(lineGO.transform, "End", new Vector2(1f, 0.5f), new Vector2(22f, 22f), Vector2.zero, GameUiStyle.Art("GUI_Diamond"), GameUiStyle.Ink, changes);
            Image gate = GameUiStyle.Picture(lineGO.transform, "GateTemplate", new Vector2(0f, 0.5f), new Vector2(16f, 16f), Vector2.zero, GameUiStyle.Art("GUI_Diamond"), new Color(1f, 0.97f, 0.9f, 0.9f), changes);
            if (gate.gameObject.activeSelf) { gate.gameObject.SetActive(false); changes.Add("hid GateTemplate"); }   // cloned per gate at runtime
            Image dot = GameUiStyle.Picture(lineGO.transform, "Dot", new Vector2(0f, 0.5f), new Vector2(20f, 20f), Vector2.zero, GameUiStyle.Art("GUI_Dot"), GameUiStyle.Gold, changes);

            var progress = PauseMenuSetup.EnsureComponent<LevelProgressLine>(root.gameObject, changes, "PlayHud");
            TrapKitSetup.Write(progress, changes, ("line", lineGO), ("lit", lit), ("dot", dot.transform), ("gateTemplate", gate.transform));
        }

        // Sprites without mips. The lines and the portrait stay exact, as the title's do: thin lines don't survive block compression.
        static void ImportArt(List<string> report)
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { GameUiStyle.ArtFolder.TrimEnd('/') }))
            {
                string file = AssetDatabase.GUIDToAssetPath(guid);
                if (!(AssetImporter.GetAtPath(file) is TextureImporter importer)) continue;
                Vector4 border = file.EndsWith(GameUiStyle.PanelArt + ".png") ? new Vector4(44f, 44f, 44f, 44f) : Vector4.zero;
                if (importer.textureType == TextureImporterType.Sprite && !importer.mipmapEnabled && importer.spriteBorder == border
                    && importer.textureCompression == TextureImporterCompression.Uncompressed) continue;
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.spriteBorder = border;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
                report.Add("import settings: " + System.IO.Path.GetFileName(file));
            }
        }
    }
}
