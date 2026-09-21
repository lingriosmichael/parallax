using System.Collections.Generic;
using Parallax.Gameplay.Checkpoints;
using Parallax.Gameplay.Input;
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

            GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefabAsset == null)
            {
                Debug.LogError($"CatPlayerSetup: prefab not found at '{PrefabPath}'. Stopping without saving.");
                return;
            }

            GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
            bool prefabSaved = false;
            bool createdMaterial = false;
            bool createdConfig = false;
            try
            {
                if (root == null)
                {
                    Debug.LogError($"CatPlayerSetup: could not load prefab contents for '{PrefabPath}'. Stopping without saving.");
                    return;
                }

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

                if (root.GetComponent<CatRespawn>() == null)
                {
                    root.AddComponent<CatRespawn>();
                    changes.Add("added CatRespawn");
                }

                var collider = root.GetComponent<CapsuleCollider2D>();
                if (collider == null)
                {
                    Debug.LogError($"CatPlayerSetup: '{PrefabPath}' root has no CapsuleCollider2D. Stopping without saving.");
                    return;
                }

                var motorSO = new SerializedObject(motor);
                if (!TryGetRequiredProperty(motorSO, "config", out SerializedProperty configProp)) return;

                PhysicsMaterial2D noFriction = GetOrCreateNoFrictionMaterial(changes, out createdMaterial);
                CatMotorConfig defaultConfig = GetOrCreateDefaultConfig(changes, out createdConfig);
                if (configProp.objectReferenceValue == null)
                {
                    configProp.objectReferenceValue = defaultConfig;
                    changes.Add("assigned CatMotor2D.config = CatMotorConfig_Default");
                }

                motorSO.ApplyModifiedPropertiesWithoutUndo();

                if (collider.sharedMaterial != noFriction)
                {
                    collider.sharedMaterial = noFriction;
                    changes.Add("assigned CapsuleCollider2D.sharedMaterial = Cat_NoFriction");
                }

                ApplyConfiguredCollider(collider, defaultConfig, changes);

                GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                if (savedPrefab == null)
                {
                    Debug.LogError($"CatPlayerSetup: failed to save '{PrefabPath}'. Stopping without saving the config.");
                    return;
                }

                prefabSaved = true;

                // Persist newly introduced config fields only after the prefab references them successfully.
                EditorUtility.SetDirty(defaultConfig);
                AssetDatabase.SaveAssets();
            }
            catch (System.Exception exception)
            {
                Debug.LogError($"CatPlayerSetup: failed while configuring '{PrefabPath}': {exception.Message}. Stopping without saving the config.");
            }
            finally
            {
                if (root != null) PrefabUtility.UnloadPrefabContents(root);
                if (!prefabSaved)
                {
                    if (createdConfig) AssetDatabase.DeleteAsset(ConfigPath);
                    if (createdMaterial) AssetDatabase.DeleteAsset(MaterialPath);
                }
            }

            if (!prefabSaved) return;
            if (changes.Count == 0)
            {
                Debug.Log("CatPlayerSetup: Cat Player already configured.");
            }
            else
            {
                Debug.Log($"CatPlayerSetup: {string.Join("; ", changes)}.");
            }
        }

        static void ApplyConfiguredCollider(CapsuleCollider2D collider, CatMotorConfig config, List<string> changes)
        {
            if (collider.direction != CapsuleDirection2D.Horizontal)
            {
                collider.direction = CapsuleDirection2D.Horizontal;
                changes.Add("set CapsuleCollider2D.direction = Horizontal");
            }

            if (collider.size != config.ColliderSize)
            {
                collider.size = config.ColliderSize;
                changes.Add($"set CapsuleCollider2D.size = {config.ColliderSize}");
            }

            if (collider.offset != config.ColliderOffset)
            {
                collider.offset = config.ColliderOffset;
                changes.Add($"set CapsuleCollider2D.offset = {config.ColliderOffset}");
            }
        }

        static bool TryGetRequiredProperty(SerializedObject serialized, string propertyName, out SerializedProperty property)
        {
            property = serialized.FindProperty(propertyName);
            if (property != null) return true;

            Debug.LogError($"CatPlayerSetup: {serialized.targetObject.GetType().Name} on '{PrefabPath}' has no serialized '{propertyName}' field. Stopping without saving.");
            return false;
        }

        static PhysicsMaterial2D GetOrCreateNoFrictionMaterial(List<string> changes, out bool created)
        {
            var mat = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(MaterialPath);
            if (mat != null)
            {
                created = false;
                return mat;
            }

            mat = new PhysicsMaterial2D("Cat_NoFriction") { friction = 0f, bounciness = 0f };
            AssetDatabase.CreateAsset(mat, MaterialPath);
            changes.Add("created Cat_NoFriction physics material");
            created = true;
            return mat;
        }

        static CatMotorConfig GetOrCreateDefaultConfig(List<string> changes, out bool created)
        {
            var config = AssetDatabase.LoadAssetAtPath<CatMotorConfig>(ConfigPath);
            if (config != null)
            {
                created = false;
                return config;
            }

            config = ScriptableObject.CreateInstance<CatMotorConfig>();
            AssetDatabase.CreateAsset(config, ConfigPath);
            changes.Add("created CatMotorConfig_Default asset");
            created = true;
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

        [MenuItem("PARALLAX/Setup/Build Fall Reset Test")]
        public static void BuildFallResetTest()
        {
            if (GameObject.Find("FallResetTest") != null)
            {
                Debug.Log("CatPlayerSetup: Fall Reset Test already built.");
                return;
            }

            GameObject ground = GameObject.Find("Ground");
            if (ground == null)
            {
                Debug.LogError("CatPlayerSetup: no GameObject named 'Ground' found in the active scene. Stopping.");
                return;
            }

            GameObject wallRight = GameObject.Find("GravityArena/Wall_Right");
            if (wallRight == null)
            {
                Debug.LogError("CatPlayerSetup: no GameObject named 'GravityArena/Wall_Right' found in the active scene. Stopping.");
                return;
            }

            var root = new GameObject("FallResetTest");
            Undo.RegisterCreatedObjectUndo(root, "Build Fall Reset Test");

            var spawnGO = new GameObject("Spawn_Main");
            Undo.RegisterCreatedObjectUndo(spawnGO, "Build Fall Reset Test");
            spawnGO.transform.SetParent(root.transform);
            spawnGO.transform.position = new Vector3(0f, -3.1f, 0f);
            SpawnPoint spawn = spawnGO.AddComponent<SpawnPoint>();

            var spawnSO = new SerializedObject(spawn);
            spawnSO.FindProperty("gravityDirection").vector2Value = Vector2.down;
            spawnSO.ApplyModifiedPropertiesWithoutUndo();

            CreateKillZone(root.transform, "KillZone_Bottom", new Vector2(0f, -9f), new Vector2(40f, 4f), spawn);
            CreateKillZone(root.transform, "KillZone_Top",    new Vector2(0f, 9f),  new Vector2(40f, 4f), spawn);
            CreateKillZone(root.transform, "KillZone_Left",   new Vector2(-14f, 0f), new Vector2(4f, 22f), spawn);
            CreateKillZone(root.transform, "KillZone_Right",  new Vector2(14f, 0f),  new Vector2(4f, 22f), spawn);

            Undo.RecordObject(wallRight, "Build Fall Reset Test");
            wallRight.SetActive(false);

            EditorSceneManager.MarkSceneDirty(root.scene);
            Debug.Log("CatPlayerSetup: built FallResetTest (Spawn_Main + 4 kill zones) and deactivated Wall_Right. Save the scene to keep it.");
        }

        static void CreateKillZone(Transform parent, string childName, Vector2 center, Vector2 size, SpawnPoint spawn)
        {
            var go = new GameObject(childName);
            Undo.RegisterCreatedObjectUndo(go, "Build Fall Reset Test");
            go.transform.SetParent(parent);
            go.transform.position = center;

            var box = go.AddComponent<BoxCollider2D>();
            box.size = size;
            box.isTrigger = true;

            var volume = go.AddComponent<FallResetVolume>();
            var volumeSO = new SerializedObject(volume);
            volumeSO.FindProperty("spawn").objectReferenceValue = spawn;
            volumeSO.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
