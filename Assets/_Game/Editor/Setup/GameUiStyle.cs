using System.Collections.Generic;
using Parallax.Gameplay.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Parallax.Editor.Setup
{
    /// <summary>PAX-A09: the title's look (D-280) for every other screen: the same gold plates and type, as small idempotent steps the level UI menus share.
    /// Each step reports only what it changed, so a menu that finds everything in place reports nothing.</summary>
    internal static class GameUiStyle
    {
        public const string ArtFolder = "Assets/_Game/Art/UI/Game/", PanelArt = "GUI_Panel";
        const string TitleArt = "Assets/_Game/Art/UI/Title/", FontFolder = "Assets/_Game/Art/UI/Fonts/";

        public static readonly Color Ink = new(1f, 0.97f, 0.9f, 1f), Gold = new(1f, 0.87f, 0.58f, 1f), Quiet = new(1f, 0.97f, 0.9f, 0.86f);
        public static readonly Color Dim = new(0.04f, 0.02f, 0.01f, 0.66f);
        static readonly Color Pressed = new(1f, 0.86f, 0.62f, 1f), Disabled = new(0.72f, 0.68f, 0.62f, 1f);

        public static Font Display => LoadFont("Cinzel-Bold.ttf");
        public static Font Body => LoadFont("Cinzel-Medium.ttf");
        public static Font Italic => LoadFont("CormorantGaramond-MediumItalic.ttf");

        public static Sprite Art(string name) => AssetDatabase.LoadAssetAtPath<Sprite>(ArtFolder + name + ".png");
        public static Sprite Title(string name) => AssetDatabase.LoadAssetAtPath<Sprite>(TitleArt + name + ".png");
        static Font LoadFont(string file) => AssetDatabase.LoadAssetAtPath<Font>(FontFolder + file) ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        public static void Place(RectTransform rect, Vector2 anchor, Vector2 size, Vector2 at, List<string> changes, string label)
        {
            var pivot = new Vector2(0.5f, 0.5f);
            if (rect.anchorMin == anchor && rect.anchorMax == anchor && rect.pivot == pivot && rect.sizeDelta == size && rect.anchoredPosition == at) return;
            rect.anchorMin = anchor; rect.anchorMax = anchor; rect.pivot = pivot;
            rect.sizeDelta = size; rect.anchoredPosition = at;
            changes.Add("placed " + label);
        }

        public static Image Paint(Image image, Sprite sprite, Color color, bool takesTaps, List<string> changes, string label)
        {
            if (image.sprite == sprite && image.color == color && image.raycastTarget == takesTaps) return image;
            image.sprite = sprite; image.color = color; image.raycastTarget = takesTaps;
            changes.Add("painted " + label);
            return image;
        }

        public static Image Picture(Transform parent, string name, Vector2 anchor, Vector2 size, Vector2 at, Sprite sprite, Color color, List<string> changes)
        {
            GameObject go = PauseMenuSetup.EnsureChild(parent, name, changes);
            Place((RectTransform)go.transform, anchor, size, at, changes, name);
            return Paint(PauseMenuSetup.EnsureComponent<Image>(go, changes, name), sprite, color, false, changes, name);
        }

        /// <summary>A panel's plate, under everything else in it. Its corners keep their size however far it stretches.</summary>
        public static Image Plate(Transform panel, Vector2 size, List<string> changes)
        {
            var center = new Vector2(0.5f, 0.5f);
            Image plate = Picture(panel, "Plate", center, size, Vector2.zero, Art(PanelArt), Color.white, changes);
            Slice(plate, changes, "Plate");
            if (plate.transform.GetSiblingIndex() != 0) { plate.transform.SetAsFirstSibling(); changes.Add("moved Plate under " + panel.name + "'s content"); }
            return plate;
        }

        public static void Slice(Image image, List<string> changes, string label)
        {
            if (image.type == Image.Type.Sliced) return;
            image.type = Image.Type.Sliced;
            changes.Add("sliced " + label);
        }

        public static Image Flourish(Transform parent, Vector2 anchor, float width, Vector2 at, List<string> changes) =>
            Picture(parent, "Rule", anchor, new Vector2(width, width * 69f / 1000f), at, Title("TUI_Flourish"), new Color(1f, 0.86f, 0.56f, 0.95f), changes);

        public static Text Type(Text text, Font font, List<string> changes, string label)
        {
            bool quiet = text.font == font && !text.raycastTarget;
            text.font = font;
            text.raycastTarget = false;
            if (!quiet) changes.Add("set " + label + " font");
            return text;
        }

        public static void Shade(GameObject go, float strength, Vector2 distance, List<string> changes, string label)
        {
            Shadow shadow = PauseMenuSetup.EnsureComponent<Shadow>(go, changes, label);
            var color = new Color(0f, 0f, 0f, strength);
            if (shadow.effectColor == color && shadow.effectDistance == distance) return;
            shadow.effectColor = color;
            shadow.effectDistance = distance;
            changes.Add("set " + label + " shadow");
        }

        /// <summary>The title's framed button on `go`: a gold-line plate with a Cinzel label, that gives under a finger. Primary is the strong one.</summary>
        public static Button Framed(GameObject go, string label, int fontSize, bool primary, List<string> changes)
        {
            Image plate = Paint(PauseMenuSetup.EnsureComponent<Image>(go, changes, go.name), Title("TUI_Plate"), new Color(1f, 1f, 1f, primary ? 1f : 0.92f), true, changes, go.name);
            Slice(plate, changes, go.name);
            Button button = Tap(go, plate, changes);
            PauseMenuSetup.EnsureComponent<TitleButtonFx>(go, changes, go.name);

            GameObject labelGO = PauseMenuSetup.EnsureChild(go.transform, "Label", changes);
            PauseMenuSetup.SetAnchors((RectTransform)labelGO.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, changes, go.name + "/Label");
            Text text = PauseMenuSetup.EnsureComponent<Text>(labelGO, changes, go.name + "/Label");
            PauseMenuSetup.ConfigureText(text, label, fontSize, primary ? Gold : Quiet, changes, go.name + "/Label");
            Type(text, Body, changes, go.name + "/Label");
            Shade(labelGO, 0.6f, new Vector2(0f, -2f), changes, go.name + "/Label");
            return button;
        }

        /// <summary>The Button itself: never selectable (D-073), and it warms under a finger as the title's do.</summary>
        public static Button Tap(GameObject go, Graphic target, List<string> changes)
        {
            Button button = PauseMenuSetup.EnsureComponent<Button>(go, changes, go.name);
            if (button.navigation.mode != Navigation.Mode.None)
            {
                button.navigation = new Navigation { mode = Navigation.Mode.None };
                changes.Add("set " + go.name + " Button.navigation.mode = None");
            }
            if (button.targetGraphic != target) { button.targetGraphic = target; changes.Add("set " + go.name + " Button.targetGraphic"); }
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white; colors.highlightedColor = Color.white; colors.selectedColor = Color.white;
            colors.pressedColor = Pressed; colors.disabledColor = Disabled;
            if (button.colors != colors) { button.colors = colors; changes.Add("set " + go.name + " Button.colors"); }
            return button;
        }
    }
}
