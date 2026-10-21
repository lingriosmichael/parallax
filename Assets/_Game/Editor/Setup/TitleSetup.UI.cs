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
    /// <summary>PAX-108 (D-157): the title's canvas. Restrained, per the UI target (DesignImages/15): a letter-spaced
    /// wordmark, a thin rule and the tagline over the world, two framed text buttons below, and a black cover for the
    /// fade-in. Rebuilt whole on every run, so the menu stays idempotent.</summary>
    public static partial class TitleSetup
    {
        const string CanvasName = "TitleCanvas";
        const string Word = "PARALLAX";
        const string FontPath = "Assets/TextMesh Pro/Fonts/LiberationSans.ttf";
        static readonly Color Ink = new(1f, 0.97f, 0.9f, 1f);

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
            Font font = AssetDatabase.LoadAssetAtPath<Font>(FontPath) ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // A soft darkening behind the wordmark, so it reads on a bright sky.
            Image shade = Rect(canvasGO.transform, "Shade", new Vector2(0.5f, 0.62f), new Vector2(1900f, 760f), Vector2.zero).gameObject.AddComponent<Image>();
            Sprite glow = Parallax.Editor.Art.EnvironmentKit.Sprite("ENV_Glow");
            shade.sprite = glow;
            shade.color = new Color(0.08f, 0.04f, 0.02f, glow != null ? 0.75f : 0f);
            shade.raycastTarget = false;

            // The wordmark: one Text per letter, spaced by LetterSpacedText.
            RectTransform wordRect = Rect(canvasGO.transform, "Wordmark", new Vector2(0.5f, 0.68f), new Vector2(1600f, 200f), Vector2.zero);
            var letters = new Text[Word.Length];
            for (int i = 0; i < Word.Length; i++)
            {
                RectTransform l = Rect(wordRect, "Letter_" + i, new Vector2(0.5f, 0.5f), new Vector2(100f, 200f), Vector2.zero);
                letters[i] = Label(l.gameObject, font, Word[i].ToString(), 128, FontStyle.Normal, Ink);
                var edge = l.gameObject.AddComponent<Outline>();
                edge.effectColor = new Color(0.1f, 0.06f, 0.03f, 0.35f);
                edge.effectDistance = new Vector2(1.5f, -1.5f);
                var shadow = l.gameObject.AddComponent<Shadow>();
                shadow.effectColor = new Color(0f, 0f, 0f, 0.5f);
                shadow.effectDistance = new Vector2(0f, -5f);
            }
            var word = wordRect.gameObject.AddComponent<LetterSpacedText>();
            var wordSo = new SerializedObject(word);
            SerializedProperty letterProp = wordSo.FindProperty("letters");
            letterProp.arraySize = letters.Length;
            for (int i = 0; i < letters.Length; i++) letterProp.GetArrayElementAtIndex(i).objectReferenceValue = letters[i];
            wordSo.ApplyModifiedPropertiesWithoutUndo();
            word.Layout();

            RectTransform rule = Rect(canvasGO.transform, "Rule", new Vector2(0.5f, 0.68f), new Vector2(560f, 2f), new Vector2(0f, -112f));
            Image ruleImage = rule.gameObject.AddComponent<Image>();
            ruleImage.color = new Color(Ink.r, Ink.g, Ink.b, 0.75f);
            ruleImage.raycastTarget = false;

            RectTransform taglineRect = Rect(canvasGO.transform, "Tagline", new Vector2(0.5f, 0.68f), new Vector2(1200f, 60f), new Vector2(0f, -160f));
            Text tagline = Label(taglineRect.gameObject, font, config.Tagline, 36, FontStyle.Italic, new Color(Ink.r, Ink.g, Ink.b, 0.9f));
            var tagShadow = taglineRect.gameObject.AddComponent<Shadow>();
            tagShadow.effectColor = new Color(0f, 0f, 0f, 0.5f);
            tagShadow.effectDistance = new Vector2(0f, -2f);

            // The menu.
            // PLAY/CONTINUE, then START GAME (shown once there is progress; TitleScreen moves LEVELS up into its place otherwise).
            RectTransform menuRect = Rect(canvasGO.transform, "Menu", new Vector2(0.5f, 0.24f), new Vector2(600f, 330f), Vector2.zero);
            var menu = menuRect.gameObject.AddComponent<CanvasGroup>();
            (Button play, Text playLabel) = FramedButton(menuRect, "Play", font, "PLAY", 46, new Vector2(380f, 104f), new Vector2(0f, 74f), true);
            (Button start, _) = FramedButton(menuRect, "StartGame", font, "START GAME", 30, new Vector2(300f, 70f), new Vector2(0f, -40f), false);
            (Button levels, _) = FramedButton(menuRect, "Levels", font, "LEVELS", 30, new Vector2(300f, 70f), new Vector2(0f, -124f), false);
            (GameObject confirm, Button confirmYes, Button confirmNo) = ConfirmPanel(canvasGO.transform, font);

            RectTransform coverRect = Rect(canvasGO.transform, "Cover", Vector2.zero, Vector2.zero, Vector2.zero);
            coverRect.anchorMin = Vector2.zero; coverRect.anchorMax = Vector2.one; coverRect.sizeDelta = Vector2.zero;
            Image cover = coverRect.gameObject.AddComponent<Image>();
            cover.color = Color.black;
            cover.raycastTarget = false;

            var screen = canvasGO.AddComponent<TitleScreen>();
            TrapKitSetup.Write(screen, changes, ("config", config), ("levelList", levelList), ("cover", cover), ("wordmark", word), ("shade", shade),
                ("rule", rule), ("tagline", tagline), ("menu", menu), ("playButton", play), ("playLabel", playLabel), ("levelsButton", levels),
                ("startButton", start), ("confirmPanel", confirm), ("confirmButton", confirmYes), ("cancelButton", confirmNo));
            changes.Add("built " + CanvasName);
        }

        // D-157 amendment 1: START GAME asks before it erases the progress. A dimmed screen (it takes every tap) and a box.
        static (GameObject, Button, Button) ConfirmPanel(Transform canvas, Font font)
        {
            RectTransform panel = Rect(canvas, "ConfirmStart", Vector2.zero, Vector2.zero, Vector2.zero);
            panel.anchorMin = Vector2.zero; panel.anchorMax = Vector2.one; panel.sizeDelta = Vector2.zero;
            Image dim = panel.gameObject.AddComponent<Image>();
            dim.color = new Color(0f, 0f, 0f, 0.6f);
            RectTransform box = Rect(panel, "Box", new Vector2(0.5f, 0.5f), new Vector2(900f, 420f), Vector2.zero);
            Image boxImage = box.gameObject.AddComponent<Image>();
            boxImage.color = new Color(0.07f, 0.045f, 0.03f, 1f);   // solid: the wordmark and tagline sit right behind it
            Color line = new(Ink.r, Ink.g, Ink.b, 0.5f);
            Edge(box, "Top", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 2f), line);
            Edge(box, "Bottom", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 2f), line);
            Edge(box, "Left", new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(2f, 0f), line);
            Edge(box, "Right", new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(2f, 0f), line);
            RectTransform title = Rect(box, "Question", new Vector2(0.5f, 0.5f), new Vector2(820f, 70f), new Vector2(0f, 120f));
            Label(title.gameObject, font, "Start over from level 1?", 46, FontStyle.Normal, Ink);
            RectTransform note = Rect(box, "Note", new Vector2(0.5f, 0.5f), new Vector2(820f, 60f), new Vector2(0f, 40f));
            Label(note.gameObject, font, "Your progress and best death counts will be erased.", 28, FontStyle.Italic, new Color(Ink.r, Ink.g, Ink.b, 0.8f));
            (Button yes, _) = FramedButton(box, "StartOver", font, "START OVER", 36, new Vector2(320f, 90f), new Vector2(-180f, -100f), true);
            (Button no, _) = FramedButton(box, "Cancel", font, "CANCEL", 30, new Vector2(260f, 76f), new Vector2(180f, -100f), false);
            panel.gameObject.SetActive(false);
            return (panel.gameObject, yes, no);
        }

        static (Button, Text) FramedButton(Transform parent, string name, Font font, string text, int size, Vector2 box, Vector2 at, bool fill)
        {
            RectTransform rect = Rect(parent, name, new Vector2(0.5f, 0.5f), box, at);
            Image hit = rect.gameObject.AddComponent<Image>();
            hit.color = new Color(0.06f, 0.035f, 0.02f, fill ? 0.5f : 0.35f);
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = hit;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white; colors.highlightedColor = Color.white;
            colors.pressedColor = new Color(0.75f, 0.75f, 0.75f, 1f); colors.selectedColor = Color.white;
            button.colors = colors;
            if (fill)
            {
                // A thin line frame (four 2 px edges), as the UI target's circular buttons are drawn: line, not slab.
                Color line = new(Ink.r, Ink.g, Ink.b, 0.85f);
                Edge(rect, "Top", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 2f), line);
                Edge(rect, "Bottom", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 2f), line);
                Edge(rect, "Left", new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(2f, 0f), line);
                Edge(rect, "Right", new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(2f, 0f), line);
            }
            RectTransform labelRect = Rect(rect, "Label", new Vector2(0.5f, 0.5f), box, Vector2.zero);
            Text label = Label(labelRect.gameObject, font, text, size, FontStyle.Normal, fill ? Ink : new Color(Ink.r, Ink.g, Ink.b, 0.8f));
            var shadow = labelRect.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.5f);
            shadow.effectDistance = new Vector2(0f, -2f);
            return (button, label);
        }

        static void Edge(RectTransform parent, string name, Vector2 min, Vector2 max, Vector2 size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = min; rect.anchorMax = max; rect.sizeDelta = size; rect.anchoredPosition = Vector2.zero;
            Image image = go.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
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
