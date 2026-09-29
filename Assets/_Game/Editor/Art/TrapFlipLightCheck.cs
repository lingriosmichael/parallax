using Parallax.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Parallax.Editor.Art
{
    /// <summary>PAX-A13: PARALLAX/Art/Trap Flip Light Check. A new, unsaved scratch scene with the launcher (TRAP-01) facing
    /// right and facing left (flipX) side by side on the trap material (Parallax/2D/Sprite-Lit-Flip), a dim global light and a
    /// point light with normal maps on, selected and framed in the Scene view. Drag the point light left and right: both
    /// launchers must shade toward it. A third launcher on URP's stock Sprite-Lit, facing left, shows the bug the flip
    /// shader fixes (its sideways shading comes from the wrong side). Close the scene without saving when done.</summary>
    static class TrapFlipLightCheck
    {
        [MenuItem("PARALLAX/Art/Trap Flip Light Check")]
        static void Build()
        {
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty) { Debug.LogError("Trap Flip Light Check: save or discard the open scene first."); return; }
            var config = AssetDatabase.LoadAssetAtPath<TrapArtConfig>(TrapArtSetup.ConfigPath);
            if (config == null) { Debug.LogError("Trap Flip Light Check: no TrapArtConfig; run PARALLAX/Art/Import Trap Kit first."); return; }
            var stock = AssetDatabase.LoadAssetAtPath<Material>("Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Lit-Default.mat");
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            Launcher("TRAP-01 facing right (flip shader)", new Vector2(-2.2f, 0f), false, config.ArrowLauncher, config.TrapMaterial);
            Launcher("TRAP-01 facing left (flip shader)", new Vector2(0f, 0f), true, config.ArrowLauncher, config.TrapMaterial);
            Launcher("TRAP-01 facing left (URP stock: wrong side)", new Vector2(2.2f, 0f), true, config.ArrowLauncher, stock);

            var global = new GameObject("Global Light 2D").AddComponent<Light2D>();
            global.lightType = Light2D.LightType.Global;
            global.intensity = 0.3f;

            var pointGo = new GameObject("Point Light 2D (drag me left and right)");
            pointGo.transform.position = new Vector3(-3.5f, 1.2f, 0f);
            var point = pointGo.AddComponent<Light2D>();
            point.lightType = Light2D.LightType.Point;
            point.intensity = 1.6f;
            point.color = new Color(1f, 0.9f, 0.75f);
            point.pointLightOuterRadius = 7f;
            var so = new SerializedObject(point);
            so.FindProperty("m_NormalMapQuality").intValue = (int)Light2D.NormalMapQuality.Accurate;
            so.FindProperty("m_NormalMapDistance").floatValue = 1.5f;
            so.FindProperty("m_UseNormalMap").boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo();

            // §12 R9: a camera framing the three launchers, so the Game view shows them too.
            var camera = new GameObject("Main Camera").AddComponent<Camera>();
            camera.tag = "MainCamera";
            camera.orthographic = true;
            camera.orthographicSize = 2.2f;
            camera.transform.position = new Vector3(0f, 0f, -10f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.93f, 0.82f, 0.62f, 1f);

            Selection.activeGameObject = pointGo;
            if (SceneView.lastActiveSceneView != null) { SceneView.lastActiveSceneView.in2DMode = true; SceneView.lastActiveSceneView.Frame(new Bounds(Vector3.zero, new Vector3(8f, 4f, 1f)), false); }
            Debug.Log("Trap Flip Light Check: drag the point light; both flip-shader launchers must shade toward it. Close without saving.");
        }

        static void Launcher(string name, Vector2 at, bool faceLeft, Sprite sprite, Material material)
        {
            var go = new GameObject(name);
            go.transform.position = at;
            go.transform.localScale = Vector3.one * 3f;
            var r = go.AddComponent<SpriteRenderer>();
            r.sprite = sprite; r.sharedMaterial = material; r.flipX = faceLeft;
        }
    }
}
