using System.Collections.Generic;
using System.IO;
using Parallax.Core;
using Parallax.Gameplay.Anchors;
using Parallax.Gameplay.Reality;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Parallax.Editor.Setup
{
    public static class SetupUtility
    {
        const string GreyboxFolder = "Assets/_Game/Art/Greybox";
        const string GreyboxPath = GreyboxFolder + "/Greybox_Square.png";
        public static Transform EnsureChild(Transform parent, string name, int layer, List<string> changes)
        {
            Transform child = parent.Find(name);
            if (child == null)
            {
                var go = parent is RectTransform
                    ? new GameObject(name, typeof(RectTransform))
                    : new GameObject(name);
                Undo.RegisterCreatedObjectUndo(go, "PARALLAX Setup");
                go.transform.SetParent(parent, false);
                child = go.transform;
                changes.Add("created " + RealityIsolationValidator.Path(child));
            }
            if (child.gameObject.layer != layer)
            {
                child.gameObject.layer = layer;
                changes.Add("set " + RealityIsolationValidator.Path(child) + ".layer");
            }
            return child;
        }

        public static T Ensure<T>(GameObject go, List<string> changes) where T : Component
        {
            T component = go.GetComponent<T>();
            if (component != null) return component;
            component = go.AddComponent<T>();
            changes.Add("added " + typeof(T).Name + " to " + RealityIsolationValidator.Path(go.transform));
            return component;
        }

        public static GameObject EnsureSceneRoot(string name, List<string> changes)
        {
            foreach (GameObject root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
                if (root.name == name) return root;
            var go = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(go, "PARALLAX Setup");
            changes.Add("created " + name);
            return go;
        }

        public static void SetLocalPosition(Transform transform, Vector2 value, List<string> changes)
        {
            if ((Vector2)transform.localPosition == value) return;
            transform.localPosition = value;
            changes.Add("set " + RealityIsolationValidator.Path(transform) + ".localPosition");
        }

        public static void SetColliderSize(BoxCollider2D collider, Vector2 value, List<string> changes)
        {
            if (collider.size == value) return;
            collider.size = value;
            changes.Add("set " + collider.name + ".size");
        }

        public static void SetBodyType(Rigidbody2D body, RigidbodyType2D value, List<string> changes)
        {
            if (body.bodyType == value) return;
            body.bodyType = value;
            changes.Add("set " + body.name + ".bodyType");
        }

        public static void SetObject(Object target, string name, Object value, List<string> changes)
        {
            var serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(name);
            if (property == null || property.objectReferenceValue == value) return;
            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            changes.Add("wired " + target.name + "." + name);
        }

        public static void SetVector2(Object target, string name, Vector2 value, List<string> changes)
        {
            var serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(name);
            if (property == null || property.vector2Value == value) return;
            property.vector2Value = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            changes.Add("set " + target.name + "." + name);
        }

        public static void SetFloat(Object target, string name, float value, List<string> changes)
        {
            var serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(name);
            if (property == null || Mathf.Approximately(property.floatValue, value)) return;
            property.floatValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            changes.Add("set " + target.name + "." + name);
        }

        public static void SetColor(Object target, string name, Color value, List<string> changes)
        {
            var serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(name);
            if (property == null || property.colorValue == value) return;
            property.colorValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            changes.Add("set " + target.name + "." + name);
        }

        public static void SetArray(Object target, string name, Object[] values, List<string> changes)
        {
            var serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(name);
            if (property == null) return;
            bool matches = property.arraySize == values.Length;
            for (int i = 0; matches && i < values.Length; i++) matches = property.GetArrayElementAtIndex(i).objectReferenceValue == values[i];
            if (matches) return;
            property.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            serialized.ApplyModifiedPropertiesWithoutUndo();
            changes.Add("wired " + target.name + "." + name);
        }

        public static SpriteRenderer SetVisual(GameObject go, RealityRoot root, Vector2 size, Color color, List<string> changes)
        {
            Transform ground = root.transform.Find("Geometry/Ground");
            SpriteRenderer source = ground == null ? null : ground.GetComponent<SpriteRenderer>();
            Sprite sprite = source != null ? source.sprite : null;
            if (sprite == null) sprite = GetGreyboxSprite();
            if (sprite == null)
            {
                Debug.LogError($"SetupUtility: no scene sprite at {root.name}/Geometry/Ground and fallback '{GreyboxPath}' is unavailable; '{go.name}' was not given a renderer.", go);
                return go.GetComponent<SpriteRenderer>();
            }

            SpriteRenderer renderer = Ensure<SpriteRenderer>(go, changes);
            if (renderer.sprite != sprite)
            {
                renderer.sprite = sprite;
                changes.Add("set " + go.name + ".sprite");
            }
            if (renderer.drawMode != SpriteDrawMode.Sliced)
            {
                renderer.drawMode = SpriteDrawMode.Sliced;
                changes.Add("set " + go.name + ".drawMode");
            }
            if (renderer.size != size)
            {
                renderer.size = size;
                changes.Add("set " + go.name + ".size");
            }
            string sortingLayer = RealitySpace.SortingLayerName(root.Id, SortingBand.Gameplay);
            if (renderer.sortingLayerName != sortingLayer)
            {
                renderer.sortingLayerName = sortingLayer;
                changes.Add("set " + go.name + ".sortingLayer");
            }
            if (renderer.color != color)
            {
                renderer.color = color;
                changes.Add("set " + go.name + ".color");
            }
            return renderer;
        }

        public static Sprite GetGreyboxSprite()
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(GreyboxPath);
            if (sprite != null) return sprite;

            if (!AssetDatabase.IsValidFolder(GreyboxFolder))
            {
                if (!AssetDatabase.IsValidFolder("Assets/_Game/Art")) return null;
                AssetDatabase.CreateFolder("Assets/_Game/Art", "Greybox");
            }

            if (!File.Exists(Path.Combine(Directory.GetCurrentDirectory(), GreyboxPath)))
            {
                var texture = new Texture2D(16, 16, TextureFormat.RGBA32, false);
                Color[] pixels = new Color[16 * 16];
                for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
                texture.SetPixels(pixels);
                texture.Apply(false, false);
                File.WriteAllBytes(Path.Combine(Directory.GetCurrentDirectory(), GreyboxPath), texture.EncodeToPNG());
                Object.DestroyImmediate(texture);
                AssetDatabase.ImportAsset(GreyboxPath, ImportAssetOptions.ForceUpdate);
            }

            var importer = AssetImporter.GetAtPath(GreyboxPath) as TextureImporter;
            if (importer == null) return null;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
            Texture2D fallbackTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(GreyboxPath);
            importer.spritePixelsPerUnit = fallbackTexture != null ? fallbackTexture.width : 16f;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(GreyboxPath);
        }

        public static AnchorDefinition EnsureDefinition(string path, int id, string displayName, List<string> changes)
        {
            AnchorDefinition definition = AssetDatabase.LoadAssetAtPath<AnchorDefinition>(path);
            if (definition == null)
            {
                if (!AssetDatabase.IsValidFolder("Assets/_Game/Data/Anchors")) AssetDatabase.CreateFolder("Assets/_Game/Data", "Anchors");
                definition = ScriptableObject.CreateInstance<AnchorDefinition>();
                AssetDatabase.CreateAsset(definition, path);
                changes.Add("created " + displayName + " asset");
            }
            var serialized = new SerializedObject(definition);
            SetInt(serialized, "id", id, changes, displayName + ".id");
            SetFloat(serialized, "initialValue", 0f, changes, displayName + ".initialValue");
            SetString(serialized, "displayName", displayName, changes, displayName + ".displayName");
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return definition;
        }

        static void SetInt(SerializedObject serialized, string name, int value, List<string> changes, string label)
        {
            SerializedProperty property = serialized.FindProperty(name);
            if (property.intValue == value) return;
            property.intValue = value;
            changes.Add("set " + label);
        }

        static void SetFloat(SerializedObject serialized, string name, float value, List<string> changes, string label)
        {
            SerializedProperty property = serialized.FindProperty(name);
            if (Mathf.Approximately(property.floatValue, value)) return;
            property.floatValue = value;
            changes.Add("set " + label);
        }

        static void SetString(SerializedObject serialized, string name, string value, List<string> changes, string label)
        {
            SerializedProperty property = serialized.FindProperty(name);
            if (property.stringValue == value) return;
            property.stringValue = value;
            changes.Add("set " + label);
        }
    }
}
