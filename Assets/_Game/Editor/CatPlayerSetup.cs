using System.Collections.Generic;
using Parallax.Gameplay.Controls;
using Parallax.Gameplay.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Parallax.Editor
{
    public static class CatPlayerSetup
    {
        const string PrefabPath   = "Assets/_Game/Gameplay/Player/Cat_Player.prefab";
        const string MaterialPath = "Assets/_Game/Data/Cat_NoFriction.physicsMaterial2D";
        const string ConfigPath   = "Assets/_Game/Data/CatMotorConfig_Default.asset";

        [MenuItem("PARALLAX/Setup/Configure Cat Player")]
        public static void Configure()
        {
            var changes = new List<string>();

            PhysicsMaterial2D noFriction = GetOrCreateNoFrictionMaterial(changes);
            CatMotorConfig defaultConfig = GetOrCreateDefaultConfig(changes);

            GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefabAsset == null)
            {
                Debug.LogError($"CatPlayerSetup: prefab not found at '{PrefabPath}'. Stopping without saving.");
                return;
            }

            GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                Transform visual = root.transform.Find("Visual");
                if (visual == null)
                {
                    Debug.LogError($"CatPlayerSetup: '{PrefabPath}' has no child named 'Visual'. Stopping without saving.");
                    return;
                }

                var gravityReceiver = root.GetComponent<GravityReceiver>();
                if (gravityReceiver == null)
                {
                    root.AddComponent<GravityReceiver>();
                    changes.Add("added GravityReceiver");
                }

                var motor = root.GetComponent<CatMotor2D>();
                if (motor == null)
                {
                    motor = root.AddComponent<CatMotor2D>();
                    changes.Add("added CatMotor2D");
                }

                if (root.GetComponent<KeyboardCatInput>() == null)
                {
                    root.AddComponent<KeyboardCatInput>();
                    changes.Add("added KeyboardCatInput");
                }

                var motorSO = new SerializedObject(motor);
                var configProp = motorSO.FindProperty("config");
                if (configProp.objectReferenceValue == null)
                {
                    configProp.objectReferenceValue = defaultConfig;
                    changes.Add("assigned CatMotor2D.config = CatMotorConfig_Default");
                }

                var visualProp = motorSO.FindProperty("visual");
                if (visualProp.objectReferenceValue == null)
                {
                    visualProp.objectReferenceValue = visual;
                    changes.Add("assigned CatMotor2D.visual = Visual");
                }
                motorSO.ApplyModifiedPropertiesWithoutUndo();

                var collider = root.GetComponent<CapsuleCollider2D>();
                if (collider == null)
                {
                    Debug.LogError($"CatPlayerSetup: '{PrefabPath}' root has no CapsuleCollider2D. Stopping without saving.");
                    return;
                }

                if (collider.sharedMaterial != noFriction)
                {
                    collider.sharedMaterial = noFriction;
                    changes.Add("assigned CapsuleCollider2D.sharedMaterial = Cat_NoFriction");
                }

                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            if (changes.Count == 0)
            {
                Debug.Log("CatPlayerSetup: Cat Player already configured.");
            }
            else
            {
                Debug.Log($"CatPlayerSetup: {string.Join("; ", changes)}.");
            }
        }

        static PhysicsMaterial2D GetOrCreateNoFrictionMaterial(List<string> changes)
        {
            var mat = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(MaterialPath);
            if (mat != null) return mat;

            mat = new PhysicsMaterial2D("Cat_NoFriction") { friction = 0f, bounciness = 0f };
            AssetDatabase.CreateAsset(mat, MaterialPath);
            changes.Add("created Cat_NoFriction physics material");
            return mat;
        }

        static CatMotorConfig GetOrCreateDefaultConfig(List<string> changes)
        {
            var config = AssetDatabase.LoadAssetAtPath<CatMotorConfig>(ConfigPath);
            if (config != null) return config;

            config = ScriptableObject.CreateInstance<CatMotorConfig>();
            AssetDatabase.CreateAsset(config, ConfigPath);
            changes.Add("created CatMotorConfig_Default asset");
            return config;
        }

        [MenuItem("PARALLAX/Setup/Build Gravity Test Arena")]
        public static void BuildGravityTestArena()
        {
            if (GameObject.Find("GravityArena") != null)
            {
                Debug.Log("CatPlayerSetup: Gravity Test Arena already built.");
                return;
            }

            GameObject ground = GameObject.Find("Ground");
            if (ground == null)
            {
                Debug.LogError("CatPlayerSetup: no GameObject named 'Ground' found in the active scene. Stopping.");
                return;
            }

            var arena = new GameObject("GravityArena");
            Undo.RegisterCreatedObjectUndo(arena, "Build Gravity Test Arena");

            CreateWallClone(ground, arena.transform, "Wall_Left",  new Vector3(-8.5f, 0f, 0f), new Vector3(1f, 9f, 1f));
            CreateWallClone(ground, arena.transform, "Wall_Right", new Vector3(8.5f, 0f, 0f),  new Vector3(1f, 9f, 1f));
            CreateWallClone(ground, arena.transform, "Ceiling",    new Vector3(0f, 4f, 0f),    new Vector3(18f, 1f, 1f));

            EditorSceneManager.MarkSceneDirty(arena.scene);
            Debug.Log("CatPlayerSetup: built GravityArena (Wall_Left, Wall_Right, Ceiling). Save the scene to keep it.");
        }

        static void CreateWallClone(GameObject template, Transform parent, string childName, Vector3 localPosition, Vector3 localScale)
        {
            GameObject clone = Object.Instantiate(template, parent);
            clone.name = childName;
            clone.transform.localPosition = localPosition;
            clone.transform.localScale = localScale;
            Undo.RegisterCreatedObjectUndo(clone, "Build Gravity Test Arena");
        }
    }
}
