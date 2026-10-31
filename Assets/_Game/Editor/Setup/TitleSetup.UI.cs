using System.Collections.Generic;
using Parallax.Gameplay.Levels;
using Parallax.Gameplay.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Parallax.Editor.Setup
{
    /// <summary>PAX-108 (D-157): the title's canvas. D-280: an old-gold inscription over the world: the air (TitleAtmosphere), a turning
    /// sigil, a gilded wordmark, a flourish and the tagline, a row of gold-line buttons on a darkened ground, and a black cover for the
    /// fade-in. Rebuilt whole on every run, so the menu stays idempotent.</summary>
    public static partial class TitleSetup
    {
        const string CanvasName = "TitleCanvas";
        const string Word = "PARALLAX";
        const string FontFolder = "Assets/_Game/Art/UI/Fonts/", ArtFolder = "Assets/_Game/Art/UI/Title/";
        const int MoteCount = 34;
        static readonly Color Ink = new(1f, 0.97f, 0.9f, 1f), Gold = new(1f, 0.87f, 0.58f, 1f);
        // The light shafts: x from the top's centre (the sun's side), tilt, width.
        static readonly Vector3[] RayAt = { new(330f, 7f, 620f), new(470f, -9f, 460f), new(120f, 20f, 520f), new(690f, -20f, 560f) };

        static void BuildCanvas(TitleScreenConfig config, LevelListConfig levelList, List<string> changes)
        {
            EnsureEventSystem(changes);
            GameObject old = GameObject.Find(CanvasName);
            if (old != null) Object.DestroyImmediate(old);

            var canvasGO = new GameObject(CanvasName, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(canvasGO, "Setup Title Screen");
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 1f; // landscape: scale by height, so a 20:9 phone keeps the same text size as 16:9
            canvasGO.AddComponent<GraphicRaycaster>();
            Font display = LoadFont("Cinzel-Bold.ttf"), font = LoadFont("Cinzel-Medium.ttf"), italic = LoadFont("CormorantGaramond-MediumItalic.ttf");

            // D-280: back to front: the air, the darkening that gives the gold something to stand on, the foreground, the sigil, the type.
            var atmosphere = canvasGO.AddComponent<TitleAtmosphere>();
            Image vignette = Stretch(canvasGO.transform, "Vignette", ArtSprite("TUI_Vignette"), Color.white);
            var rays = new Image[RayAt.Length];
            for (int i = 0; i < RayAt.Length; i++)
            {
                RectTransform ray = Rect(canvasGO.transform, "Ray_" + i, new Vector2(0.5f, 1f), new Vector2(RayAt[i].z, 1500f), new Vector2(RayAt[i].x, 40f));
                ray.pivot = new Vector2(0.5f, 1f);
                ray.localRotation = Quaternion.Euler(0f, 0f, RayAt[i].y);
                rays[i] = Picture(ray, ArtSprite("TUI_Ray"), new Color(1f, 0.88f, 0.62f, 0f));
            }
            RectTransform moteField = Rect(canvasGO.transform, "Motes", new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            var motes = new Image[MoteCount];
            for (int i = 0; i < MoteCount; i++)
            {
                float size = 7f + 15f * Mathf.Repeat(i * 0.4142f, 1f);
                motes[i] = Picture(Rect(moteField, "Mote_" + i, new Vector2(0.5f, 0.5f), new Vector2(size, size), Vector2.zero), ArtSprite("TUI_Soft"), new Color(1f, 0.86f, 0.55f, 0f));
            }
            RectTransform leftFern = Foliage(canvasGO.transform, "Foliage_L", "TUI_FoliageR", new Vector2(0f, 0f), new Vector2(720f, 250f), new Vector2(-10f, -24f), false);
            RectTransform rightFern = Foliage(canvasGO.transform, "Foliage_R", "TUI_FoliageR", new Vector2(1f, 0f), new Vector2(900f, 340f), new Vector2(10f, -24f), true);
            RectTransform scrimRect = Rect(canvasGO.transform, "Scrim", new Vector2(0.5f, 0f), new Vector2(0f, 460f), Vector2.zero);
            scrimRect.anchorMin = Vector2.zero; scrimRect.anchorMax = new Vector2(1f, 0f); scrimRect.pivot = new Vector2(0.5f, 0f); scrimRect.sizeDelta = new Vector2(0f, 460f);
            Picture(scrimRect, ArtSprite("TUI_Scrim"), Color.white);

            Vector2 mark = new(0.5f, 0.69f);
            Image shade = Picture(Rect(canvasGO.transform, "Shade", mark, new Vector2(2000f, 720f), Vector2.zero), ArtSprite("TUI_Soft"), new Color(0.07f, 0.035f, 0.015f, 0.8f));
            Image sigilOuter = Picture(Rect(canvasGO.transform, "SigilOuter", mark, new Vector2(820f, 820f), Vector2.zero), ArtSprite("TUI_SigilOuter"), new Color(1f, 0.86f, 0.56f, 0f));
            Image sigilInner = Picture(Rect(canvasGO.transform, "SigilInner", mark, new Vector2(560f, 560f), Vector2.zero), ArtSprite("TUI_SigilInner"), new Color(1f, 0.86f, 0.56f, 0f));
            Image glow = Picture(Rect(canvasGO.transform, "Glow", mark, new Vector2(1500f, 460f), Vector2.zero), ArtSprite("TUI_Soft"), new Color(1f, 0.72f, 0.34f, 0.22f));
            Picture(Rect(canvasGO.transform, "TagShade", mark, new Vector2(1250f, 300f), new Vector2(0f, -176f)), ArtSprite("TUI_Soft"), new Color(0.06f, 0.03f, 0.012f, 0.82f));

            // The wordmark: one Text per letter, spaced by LetterSpacedText.
            RectTransform wordRect = Rect(canvasGO.transform, "Wordmark", mark, new Vector2(1600f, 240f), Vector2.zero);
            var letters = new Text[Word.Length];
            for (int i = 0; i < Word.Length; i++)
            {
                RectTransform l = Rect(wordRect, "Letter_" + i, new Vector2(0.5f, 0.5f), new Vector2(100f, 240f), Vector2.zero);
                letters[i] = Label(l.gameObject, display, Word[i].ToString(), 158, FontStyle.Normal, Color.white);
                l.gameObject.AddComponent<GildedText>();
                var edge = l.gameObject.AddComponent<Outline>();
                edge.effectColor = new Color(0.16f, 0.07f, 0.02f, 0.9f);
                edge.effectDistance = new Vector2(2f, -2f);
                var shadow = l.gameObject.AddComponent<Shadow>();
                shadow.effectColor = new Color(0f, 0f, 0f, 0.65f);
                shadow.effectDistance = new Vector2(0f, -8f);
            }
            var word = wordRect.gameObject.AddComponent<LetterSpacedText>();
            var wordSo = new SerializedObject(word);
            SerializedProperty letterProp = wordSo.FindProperty("letters");
            letterProp.arraySize = letters.Length;
            for (int i = 0; i < letters.Length; i++) letterProp.GetArrayElementAtIndex(i).objectReferenceValue = letters[i];
            wordSo.ApplyModifiedPropertiesWithoutUndo();
            word.Layout();

            RectTransform rule = Rect(canvasGO.transform, "Rule", mark, new Vector2(1000f, 69f), new Vector2(0f, -122f));
            Picture(rule, ArtSprite("TUI_Flourish"), new Color(1f, 0.86f, 0.56f, 0.95f));

            RectTransform taglineRect = Rect(canvasGO.transform, "Tagline", mark, new Vector2(1200f, 70f), new Vector2(0f, -180f));
            Text tagline = Label(taglineRect.gameObject, italic, config.Tagline, 52, FontStyle.Normal, Ink);
            var tagShadow = taglineRect.gameObject.AddComponent<Shadow>();
            tagShadow.effectColor = new Color(0f, 0f, 0f, 0.75f);
            tagShadow.effectDistance = new Vector2(0f, -3f);

            // The menu, one row on the darkened ground: START GAME (shown once there is progress), PLAY/CONTINUE, LEVELS.
            RectTransform menuRect = Rect(canvasGO.transform, "Menu", new Vector2(0.5f, 0.125f), new Vector2(1300f, 160f), Vector2.zero);
            var menu = menuRect.gameObject.AddComponent<CanvasGroup>();
            (Button play, Text playLabel) = FramedButton(menuRect, "Play", font, "PLAY", 46, new Vector2(420f, 122f), Vector2.zero, true);
            (Button start, _) = FramedButton(menuRect, "StartGame", font, "START GAME", 28, new Vector2(320f, 88f), new Vector2(-400f, 0f), false);
            (Button levels, _) = FramedButton(menuRect, "Levels", font, "LEVELS", 28, new Vector2(320f, 88f), new Vector2(400f, 0f), false);
            (GameObject confirm, Button confirmYes, Button confirmNo) = ConfirmPanel(canvasGO.transform, font, italic);

            RectTransform coverRect = Rect(canvasGO.transform, "Cover", Vector2.zero, Vector2.zero, Vector2.zero);
            coverRect.anchorMin = Vector2.zero; coverRect.anchorMax = Vector2.one; coverRect.sizeDelta = Vector2.zero;
            Image cover = coverRect.gameObject.AddComponent<Image>();
            cover.color = Color.black;
            cover.raycastTarget = false;

            var screen = canvasGO.AddComponent<TitleScreen>();
            TrapKitSetup.Write(atmosphere, changes, ("sigilOuter", sigilOuter), ("sigilInner", sigilInner), ("glow", glow));
            SetupUtility.SetArray(atmosphere, "rays", rays, changes);
            SetupUtility.SetArray(atmosphere, "motes", motes, changes);
            SetupUtility.SetArray(atmosphere, "ferns", new[] { leftFern, rightFern }, changes);
            SetupUtility.SetArray(screen, "menuItems", new[] { (RectTransform)play.transform, (RectTransform)start.transform, (RectTransform)levels.transform }, changes);
            TrapKitSetup.Write(screen, changes, ("config", config), ("levelList", levelList), ("cover", cover), ("wordmark", word), ("shade", shade), ("atmosphere", atmosphere),
                ("rule", rule), ("tagline", tagline), ("menu", menu), ("playButton", play), ("playLabel", playLabel), ("levelsButton", levels),
                ("startButton", start), ("confirmPanel", confirm), ("confirmButton", confirmYes), ("cancelButton", confirmNo));
            changes.Add("built " + CanvasName);
        }

        // D-157 amendment 1: START GAME asks before it erases the progress. A dimmed screen (it takes every tap) and a box.
        static (GameObject, Button, Button) ConfirmPanel(Transform canvas, Font font, Font italic)
        {
            RectTransform panel = Rect(canvas, "ConfirmStart", Vector2.zero, Vector2.zero, Vector2.zero);
            panel.anchorMin = Vector2.zero; panel.anchorMax = Vector2.one; panel.sizeDelta = Vector2.zero;
            Image dim = panel.gameObject.AddComponent<Image>();
            dim.color = new Color(0f, 0f, 0f, 0.6f);
            RectTransform box = Rect(panel, "Box", new Vector2(0.5f, 0.5f), new Vector2(900f, 420f), Vector2.zero);
            Image boxImage = Picture(box, ArtSprite("TUI_Plate"), Color.white);
            boxImage.type = Image.Type.Sliced;
            boxImage.raycastTarget = true;
            RectTransform title = Rect(box, "Question", new Vector2(0.5f, 0.5f), new Vector2(820f, 70f), new Vector2(0f, 120f));
            Label(title.gameObject, font, "Start over from level 1?", 44, FontStyle.Normal, Ink);
            RectTransform note = Rect(box, "Note", new Vector2(0.5f, 0.5f), new Vector2(820f, 60f), new Vector2(0f, 40f));
            Label(note.gameObject, italic, "Your progress and best death counts will be erased.", 34, FontStyle.Normal, new Color(Ink.r, Ink.g, Ink.b, 0.85f));
            (Button yes, _) = FramedButton(box, "StartOver", font, "START OVER", 32, new Vector2(340f, 96f), new Vector2(-180f, -100f), true);
            (Button no, _) = FramedButton(box, "Cancel", font, "CANCEL", 28, new Vector2(280f, 84f), new Vector2(180f, -100f), false);
            panel.gameObject.SetActive(false);
            return (panel.gameObject, yes, no);
        }

        // D-280: a gold-line plate, stretched by its corners. PLAY's is at full strength; the others are quieter. Under a finger it gives.
        static (Button, Text) FramedButton(Transform parent, string name, Font font, string text, int size, Vector2 box, Vector2 at, bool fill)
        {
            RectTransform rect = Rect(parent, name, new Vector2(0.5f, 0.5f), box, at);
            Image hit = Picture(rect, ArtSprite("TUI_Plate"), new Color(1f, 1f, 1f, fill ? 1f : 0.92f));
            hit.type = Image.Type.Sliced;
            hit.raycastTarget = true;
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = hit;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white; colors.highlightedColor = Color.white;
            colors.pressedColor = new Color(1f, 0.86f, 0.62f, 1f); colors.selectedColor = Color.white;
            button.colors = colors;
            rect.gameObject.AddComponent<TitleButtonFx>();
            RectTransform labelRect = Rect(rect, "Label", new Vector2(0.5f, 0.5f), box, Vector2.zero);
            Text label = Label(labelRect.gameObject, font, text, size, FontStyle.Normal, fill ? Gold : new Color(Ink.r, Ink.g, Ink.b, 0.86f));
            var shadow = labelRect.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.6f);
            shadow.effectDistance = new Vector2(0f, -2f);
            return (button, label);
        }

        static Image Picture(RectTransform rect, Sprite sprite, Color color)
        {
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite; image.color = color; image.raycastTarget = false;
            return image;
        }

        static Image Stretch(Transform parent, string name, Sprite sprite, Color color)
        {
            RectTransform rect = Rect(parent, name, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.sizeDelta = Vector2.zero;
            return Picture(rect, sprite, color);
        }

        // D-280: soft leaves in the corners (the near ones out of focus), as the key art frames its own. They also hide the ledge's cut.
        static RectTransform Foliage(Transform canvas, string name, string art, Vector2 anchor, Vector2 size, Vector2 at, bool flip)
        {
            RectTransform rect = Rect(canvas, name, anchor, size, at);
            rect.pivot = Vector2.zero;
            if (flip) rect.localScale = new Vector3(-1f, 1f, 1f);
            Picture(rect, ArtSprite(art), Color.white);
            return rect;
        }

        static RectTransform Rect(Transform parent, string name, Vector2 anchor, Vector2 size, Vector2 at)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = anchor; rect.anchorMax = anchor; rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size; rect.anchoredPosition = at;
            return rect;
        }

        static Text Label(GameObject go, Font font, string text, int size, FontStyle style, Color color)
        {
            Text label = go.AddComponent<Text>();
            label.font = font; label.text = text; label.fontSize = size; label.fontStyle = style; label.color = color;
            label.alignment = TextAnchor.MiddleCenter;
            label.horizontalOverflow = HorizontalWrapMode.Overflow; label.verticalOverflow = VerticalWrapMode.Overflow;
            label.raycastTarget = false;
            return label;
        }

        static Font LoadFont(string file) => AssetDatabase.LoadAssetAtPath<Font>(FontFolder + file) ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        static Sprite ArtSprite(string name) => AssetDatabase.LoadAssetAtPath<Sprite>(ArtFolder + name + ".png");

        // D-280: the line art's import settings (Tools/Art/title_ui.py draws it): sprites without mips; the plate stretches by its corners.
        static void ImportArt(List<string> changes)
        {
            foreach (string path in AssetDatabase.FindAssets("t:Texture2D", new[] { ArtFolder.TrimEnd('/') }))
            {
                string file = AssetDatabase.GUIDToAssetPath(path);
                if (!(AssetImporter.GetAtPath(file) is TextureImporter importer)) continue;
                Vector4 border = file.EndsWith("TUI_Plate.png") ? new Vector4(44f, 44f, 44f, 44f) : Vector4.zero;
                // Thin lines don't survive block compression: the flourish and the plate stay exact.
                TextureImporterCompression compression = file.EndsWith("TUI_Plate.png") || file.EndsWith("TUI_Flourish.png")
                    ? TextureImporterCompression.Uncompressed : TextureImporterCompression.CompressedHQ;
                if (importer.textureType == TextureImporterType.Sprite && !importer.mipmapEnabled && importer.spriteBorder == border
                    && importer.textureCompression == compression) continue;
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.spriteBorder = border;
                importer.textureCompression = compression;
                importer.SaveAndReimport();
                changes.Add("import settings: " + System.IO.Path.GetFileName(file));
            }
        }

        static void EnsureEventSystem(List<string> changes)
        {
            var eventSystem = Object.FindAnyObjectByType<EventSystem>();
            GameObject go = eventSystem != null ? eventSystem.gameObject : null;
            if (go == null)
            {
                go = new GameObject("EventSystem");
                Undo.RegisterCreatedObjectUndo(go, "Setup Title Screen");
                go.AddComponent<EventSystem>();
                changes.Add("created EventSystem");
            }
            var legacy = go.GetComponent<StandaloneInputModule>();
            if (legacy != null) Object.DestroyImmediate(legacy);
            if (go.GetComponent<InputSystemUIInputModule>() == null) go.AddComponent<InputSystemUIInputModule>();
        }
    }
}
