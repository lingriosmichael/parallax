using System.Collections.Generic;
using System.Linq;
using Parallax.Core;
using Parallax.DebugTools;
using Parallax.Gameplay.Checkpoints;
using Parallax.Gameplay.Observers;
using Parallax.Gameplay.Player;
using Parallax.Gameplay.Reality;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace Parallax.Editor.Setup
{
    public static class RealitySetup
    {
        const string CatSourceName = "Cat_Player";
        const string CatAName = "Cat_A";
        const string CatBName = "Cat_B";
        const string GravityArenaName = "GravityArena";
        const string GroundName = "Ground";
        const string RootAName = "RealityRoot_A";
        const string RootBName = "RealityRoot_B";
        const string GeometryFolderName = "Geometry";
        const string LightsFolderName = "Lights";
        const string PillarName = "Pillar_BOnly";

        static readonly Color ColdTint = ParseColor("#6FA8DC");
        static readonly Color WarmCatTint = ParseColor("#E8A15A");
        static readonly Color WarmCameraBg = ParseColor("#2A1E14");
        static readonly Color ColdCameraBg = ParseColor("#0B1420");
        static readonly Color WarmLightColor = ParseColor("#FFD9A6");
        static readonly Color ColdLightColor = ParseColor("#A6CCFF");

        [MenuItem("PARALLAX/Setup/Realities (Sandbox)")]
        public static void Configure()
        {
            var changes = new List<string>();

            if (!ValidatePrerequisites()) return;

            RealityRoot rootA = EnsureRoot(ObserverId.A, changes);
            RealityRoot rootB = EnsureRoot(ObserverId.B, changes);
            if (rootA == null || rootB == null) return;

            PopulateA(rootA, changes);

            var aToB = new Dictionary<GameObject, GameObject>();
            PopulateB(rootA, rootB, aToB, changes);

            RewireDanglingReferences(rootA, rootB, aToB, changes);

            CatMotor2D catA = FindUnder(rootA.transform, CatAName)?.GetComponent<CatMotor2D>();
            if (catA == null)
            {
                Debug.LogError($"RealitySetup: '{CatAName}' not found under {RootAName} after population. Stopping.");
                return;
            }

            CatMotor2D catB = EnsureCatB(rootA, rootB, catA, changes);
            if (catB == null) return;

            // Runs after Cat_B exists so both cats' layers and default sorting layers
            // (RealityA/RealityB, A_Gameplay/B_Gameplay) are covered by one pass.
            ApplyRealityLayerAndDefaultSorting(rootA, changes);
            ApplyRealityLayerAndDefaultSorting(rootB, changes);

            TintSpriteRenderers(catA.GetComponentsInChildren<SpriteRenderer>(true), WarmCatTint, "Cat_A", changes);
            TintSpriteRenderers(catB.GetComponentsInChildren<SpriteRenderer>(true), ColdTint, "Cat_B", changes);

            (Camera camA, Camera camB) = EnsureCameras(catA, catB, changes);

            EnsureLight(rootA, WarmLightColor, changes);
            EnsureLight(rootB, ColdLightColor, changes);
            DisableStrayGlobalLights(rootA, rootB, changes);

            WireObservers(rootA, rootB, catA, catB, camA, camB, changes);

            if (changes.Count == 0)
            {
                Debug.Log("RealitySetup: no changes.");
            }
            else
            {
                Debug.Log($"RealitySetup: {string.Join("; ", changes)}.");
                EditorSceneManager.MarkSceneDirty(rootA.gameObject.scene);
            }

            RealityIsolationValidator.Validate();
        }

        // ---- Step 0: validate ----

        static bool ValidatePrerequisites()
        {
            var missing = new List<string>();

            if (LayerMask.NameToLayer("RealityA") < 0) missing.Add("physics layer 'RealityA'");
            if (LayerMask.NameToLayer("RealityB") < 0) missing.Add("physics layer 'RealityB'");

            var sortingNames = new HashSet<string>(SortingLayer.layers.Select(l => l.name));
            string[] expectedSorting =
            {
                "A_Background", "A_Middle", "A_Gameplay", "A_Foreground",
                "B_Background", "B_Middle", "B_Gameplay", "B_Foreground",
                "UI",
            };
            foreach (string s in expectedSorting)
            {
                if (!sortingNames.Contains(s)) missing.Add($"sorting layer '{s}'");
            }

            if (missing.Count == 0)
            {
                int layerA = LayerMask.NameToLayer("RealityA");
                int layerB = LayerMask.NameToLayer("RealityB");
                if (layerA >= 0 && layerB >= 0 && !Physics2D.GetIgnoreLayerCollision(layerA, layerB))
                {
                    missing.Add("Physics2D ignore collision RealityA x RealityB");
                }
            }

            if (missing.Count > 0)
            {
                Debug.LogError($"RealitySetup: Step 0 prerequisites missing, stopping without changing the scene: {string.Join("; ", missing)}.");
                return false;
            }

            return true;
        }

        // ---- Step 1: roots ----

        static RealityRoot EnsureRoot(ObserverId id, List<string> changes)
        {
            string name = id == ObserverId.A ? RootAName : RootBName;
            Vector2 origin = RealitySpace.Origin(id);

            GameObject go = GameObject.Find(name);
            if (go == null)
            {
                go = new GameObject(name);
                Undo.RegisterCreatedObjectUndo(go, "Setup Realities");
                go.transform.position = origin;
                changes.Add($"created {name}");
            }

            var root = go.GetComponent<RealityRoot>();
            if (root == null)
            {
                root = go.AddComponent<RealityRoot>();
                changes.Add($"added RealityRoot to {name}");
            }

            var rootSO = new SerializedObject(root);
            var idProp = rootSO.FindProperty("id");
            if ((ObserverId)idProp.enumValueIndex != id)
            {
                idProp.enumValueIndex = (int)id;
                rootSO.ApplyModifiedPropertiesWithoutUndo();
                changes.Add($"set {name}.id = {id}");
            }

            EnsureChildFolder(go.transform, GeometryFolderName, changes);
            EnsureChildFolder(go.transform, LightsFolderName, changes);

            return root;
        }

        static Transform EnsureChildFolder(Transform parent, string name, List<string> changes)
        {
            Transform existing = parent.Find(name);
            if (existing != null) return existing;

            var folder = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(folder, "Setup Realities");
            folder.transform.SetParent(parent, false);
            changes.Add($"created {parent.name}/{name}");
            return folder.transform;
        }

        // ---- Step 2: populate A ----

        static void PopulateA(RealityRoot rootA, List<string> changes)
        {
            Transform geometryA = rootA.transform.Find(GeometryFolderName);

            // Cat_Player -> Cat_A, directly under the root.
            GameObject catPlayer = GameObject.Find(CatSourceName);
            if (catPlayer != null && !catPlayer.transform.IsChildOf(rootA.transform))
            {
                Undo.SetTransformParent(catPlayer.transform, rootA.transform, "Setup Realities");
                catPlayer.name = CatAName;
                changes.Add($"moved '{CatSourceName}' under {RootAName} and renamed to '{CatAName}'");
            }

            // GravityArena and Ground -> RealityRoot_A/Geometry.
            MoveNamedUnder(GravityArenaName, geometryA, changes);
            MoveNamedUnder(GroundName, geometryA, changes);

            // Every SpawnPoint / FallResetVolume: move their common top-level ancestor
            // under RealityRoot_A directly, so references inside that subtree (e.g. a
            // FallResetVolume's spawn field) stay intact.
            var topLevelHolders = new HashSet<Transform>();
            foreach (SpawnPoint sp in Object.FindObjectsByType<SpawnPoint>(FindObjectsSortMode.None))
            {
                if (sp.transform.IsChildOf(rootA.transform) || sp.transform.IsChildOf(RootBTransformOrNull())) continue;
                topLevelHolders.Add(TopLevelAncestor(sp.transform));
            }
            foreach (FallResetVolume fv in Object.FindObjectsByType<FallResetVolume>(FindObjectsSortMode.None))
            {
                if (fv.transform.IsChildOf(rootA.transform) || fv.transform.IsChildOf(RootBTransformOrNull())) continue;
                topLevelHolders.Add(TopLevelAncestor(fv.transform));
            }

            foreach (Transform holder in topLevelHolders)
            {
                if (holder == null || holder.IsChildOf(rootA.transform)) continue;
                Undo.SetTransformParent(holder, rootA.transform, "Setup Realities");
                changes.Add($"moved '{holder.name}' (spawn/fall-reset content) under {RootAName}");
            }

            // Warn about anything else still sitting at scene root with a Collider2D/Renderer.
            WarnAboutUnmovedRootObjects(rootA, changes);
        }

        static Transform RootBTransformOrNull()
        {
            GameObject go = GameObject.Find(RootBName);
            return go != null ? go.transform : null;
        }

        static Transform TopLevelAncestor(Transform t)
        {
            while (t.parent != null) t = t.parent;
            return t;
        }

        // Uses GameObject.Find only after confirming the destination doesn't already have
        // this name, and only searches scene-root objects: once GravityArena/Ground exist
        // under both RealityRoot_A and RealityRoot_B, they share a name, and Find's result
        // for a shared name is not guaranteed to be the A copy.
        static void MoveNamedUnder(string objectName, Transform newParent, List<string> changes)
        {
            if (FindUnder(newParent, objectName) != null) return;

            GameObject go = FindAtSceneRoot(objectName, newParent);
            if (go == null) return;

            Undo.SetTransformParent(go.transform, newParent, "Setup Realities");
            changes.Add($"moved '{objectName}' under {PathOf(newParent)}");
        }

        static GameObject FindAtSceneRoot(string name, Transform anyTransformInScene)
        {
            foreach (GameObject go in anyTransformInScene.gameObject.scene.GetRootGameObjects())
            {
                if (go.name == name) return go;
            }
            return null;
        }

        static void WarnAboutUnmovedRootObjects(RealityRoot rootA, List<string> changes)
        {
            var known = new HashSet<string> { RootAName, RootBName, "DeviceInput", "Observers" };

            foreach (GameObject go in SceneRootObjects(rootA.gameObject.scene))
            {
                if (known.Contains(go.name)) continue;

                bool hasVisual = go.GetComponentInChildren<Collider2D>(true) != null
                                  || go.GetComponentInChildren<Renderer>(true) != null;
                if (!hasVisual) continue;

                Debug.LogWarning($"RealitySetup: scene-root object '{go.name}' has a Collider2D or Renderer and was not moved into a reality. Move it by hand if it belongs to one.");
            }
        }

        static IEnumerable<GameObject> SceneRootObjects(UnityEngine.SceneManagement.Scene scene)
        {
            foreach (GameObject go in scene.GetRootGameObjects())
            {
                yield return go;
            }
        }

        // ---- Step 4: populate B ----

        static void PopulateB(RealityRoot rootA, RealityRoot rootB, Dictionary<GameObject, GameObject> aToB, List<string> changes)
        {
            Transform geometryA = rootA.transform.Find(GeometryFolderName);
            Transform geometryB = rootB.transform.Find(GeometryFolderName);

            GameObject groundA = FindUnder(geometryA, GroundName);
            GameObject groundB = FindUnder(geometryB, GroundName);
            if (groundB == null && groundA != null)
            {
                groundB = CloneWithMap(groundA, geometryB, aToB);
                changes.Add($"duplicated '{GroundName}' into {RootBName}/{GeometryFolderName}");
            }

            GameObject arenaA = FindUnder(geometryA, GravityArenaName);
            GameObject arenaB = FindUnder(geometryB, GravityArenaName);
            if (arenaB == null && arenaA != null)
            {
                arenaB = CloneWithMap(arenaA, geometryB, aToB);
                changes.Add($"duplicated '{GravityArenaName}' into {RootBName}/{GeometryFolderName}");
            }

            if (groundB == null)
            {
                Debug.LogError($"RealitySetup: no '{GroundName}' found under {RootAName} to duplicate into {RootBName}. B will have no floor.");
            }

            // Spawn/fall-reset content: duplicate every top-level holder found directly under RealityRoot_A
            // (siblings of Geometry) that contains a SpawnPoint or FallResetVolume.
            foreach (Transform child in DirectChildren(rootA.transform))
            {
                if (child == geometryA || child.name == GeometryFolderName || child.name == LightsFolderName) continue;
                if (child.GetComponentInChildren<SpawnPoint>(true) == null
                    && child.GetComponentInChildren<FallResetVolume>(true) == null) continue;

                GameObject existingB = FindUnder(rootB.transform, child.name);
                if (existingB != null) continue;

                CloneWithMap(child.gameObject, rootB.transform, aToB);
                changes.Add($"duplicated '{child.name}' (spawn/fall-reset content) into {RootBName}");
            }

            // Tint B geometry cold. Leave A's tint unchanged.
            if (geometryB != null)
            {
                TintSpriteRenderers(geometryB.GetComponentsInChildren<SpriteRenderer>(true), ColdTint, $"{RootBName}/{GeometryFolderName}", changes);
            }

            EnsurePillar(rootA, rootB, changes);
        }

        static void EnsurePillar(RealityRoot rootA, RealityRoot rootB, List<string> changes)
        {
            Transform geometryB = rootB.transform.Find(GeometryFolderName);
            if (geometryB == null || FindUnder(geometryB, PillarName) != null) return;

            GameObject groundB = FindUnder(geometryB, GroundName);
            SpawnPoint spawnB = rootB.GetComponentInChildren<SpawnPoint>(true);
            CatMotor2D catA = FindUnder(rootA.transform, CatAName)?.GetComponent<CatMotor2D>();

            if (groundB == null || spawnB == null)
            {
                Debug.LogError($"RealitySetup: cannot build '{PillarName}' — missing Ground or SpawnPoint under {RootBName}.");
                return;
            }

            float catHeight = 0.8f;
            var capsule = catA != null ? catA.GetComponentInChildren<CapsuleCollider2D>() : null;
            if (capsule != null) catHeight = capsule.size.y;

            var groundBox = groundB.GetComponent<BoxCollider2D>();
            float floorTopLocalY = groundB.transform.localPosition.y
                + (groundBox != null ? groundBox.size.y * groundB.transform.localScale.y * 0.5f : 0.5f);

            float pillarHeight = catHeight * 2.5f;
            float pillarLocalX = spawnB.transform.localPosition.x + 3f;
            float pillarLocalY = floorTopLocalY + pillarHeight * 0.5f;

            GameObject pillar = Object.Instantiate(groundB, geometryB);
            Undo.RegisterCreatedObjectUndo(pillar, "Setup Realities");
            pillar.name = PillarName;
            pillar.transform.localPosition = new Vector3(pillarLocalX, pillarLocalY, groundB.transform.localPosition.z);
            pillar.transform.localScale = new Vector3(1f, pillarHeight, 1f);

            changes.Add($"created '{PillarName}' under {RootBName}/{GeometryFolderName} (height {pillarHeight:F2}, >= 2x cat height {catHeight:F2})");
        }

        static GameObject CloneWithMap(GameObject original, Transform newParent, Dictionary<GameObject, GameObject> map)
        {
            GameObject clone = Object.Instantiate(original, newParent);
            Undo.RegisterCreatedObjectUndo(clone, "Setup Realities");
            clone.name = original.name;
            MapRecursive(original.transform, clone.transform, map);
            return clone;
        }

        static void MapRecursive(Transform a, Transform b, Dictionary<GameObject, GameObject> map)
        {
            map[a.gameObject] = b.gameObject;
            int count = System.Math.Min(a.childCount, b.childCount);
            for (int i = 0; i < count; i++)
            {
                MapRecursive(a.GetChild(i), b.GetChild(i), map);
            }
        }

        static IEnumerable<Transform> DirectChildren(Transform parent)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                yield return parent.GetChild(i);
            }
        }

        // ---- Step 3: layer + default sorting layer (applied to both roots, after population) ----

        static void ApplyRealityLayerAndDefaultSorting(RealityRoot root, List<string> changes)
        {
            string layerName = RealitySpace.PhysicsLayerName(root.Id);
            int layer = LayerMask.NameToLayer(layerName);
            if (layer < 0) return;

            string gameplayLayer = RealitySpace.SortingLayerName(root.Id, SortingBand.Gameplay);
            string bg = RealitySpace.SortingLayerName(root.Id, SortingBand.Background);
            string mid = RealitySpace.SortingLayerName(root.Id, SortingBand.Middle);
            string fg = RealitySpace.SortingLayerName(root.Id, SortingBand.Foreground);

            int changedLayers = 0;
            int changedSorting = 0;

            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t.gameObject.layer != layer)
                {
                    t.gameObject.layer = layer;
                    changedLayers++;
                }
            }

            foreach (SpriteRenderer sr in root.GetComponentsInChildren<SpriteRenderer>(true))
            {
                string current = sr.sortingLayerName;
                if (current != bg && current != mid && current != gameplayLayer && current != fg)
                {
                    sr.sortingLayerName = gameplayLayer;
                    changedSorting++;
                }
            }

            if (changedLayers > 0) changes.Add($"set {changedLayers} object(s) under {root.gameObject.name} to layer '{layerName}'");
            if (changedSorting > 0) changes.Add($"set {changedSorting} SpriteRenderer(s) under {root.gameObject.name} to sorting layer '{gameplayLayer}'");
        }

        // ---- Cross-reference rewiring / verification ----

        static void RewireDanglingReferences(RealityRoot rootA, RealityRoot rootB, Dictionary<GameObject, GameObject> aToB, List<string> changes)
        {
            int rewired = 0;
            int unresolved = 0;

            foreach (Component c in rootB.GetComponentsInChildren<Component>(true))
            {
                if (c == null || c is Transform) continue;

                var so = new SerializedObject(c);
                SerializedProperty prop = so.GetIterator();
                bool enterChildren = true;
                bool dirty = false;
                while (prop.NextVisible(enterChildren))
                {
                    enterChildren = false;
                    if (prop.propertyType != SerializedPropertyType.ObjectReference) continue;

                    Object value = prop.objectReferenceValue;
                    if (value == null) continue;
                    if (EditorUtility.IsPersistent(value)) continue;

                    GameObject targetGO = RealityIsolationValidator.AsGameObject(value);
                    if (targetGO == null || !targetGO.transform.IsChildOf(rootA.transform)) continue;

                    if (aToB.TryGetValue(targetGO, out GameObject mappedGO))
                    {
                        Object replacement = ResolveSameType(value, mappedGO);
                        if (replacement != null)
                        {
                            prop.objectReferenceValue = replacement;
                            dirty = true;
                            rewired++;
                        }
                        else
                        {
                            Debug.LogError($"RealitySetup: could not resolve a '{value.GetType().Name}' on B counterpart of '{RealityIsolationValidator.Path(targetGO.transform)}' for '{RealityIsolationValidator.Path(c.transform)}'.{prop.propertyPath}.");
                            unresolved++;
                        }
                    }
                    else
                    {
                        Debug.LogError($"RealitySetup: '{RealityIsolationValidator.Path(c.transform)}' ({c.GetType().Name}.{prop.propertyPath}) references '{RealityIsolationValidator.Path(targetGO.transform)}' under {RootAName}, which has no B counterpart.");
                        unresolved++;
                    }
                }

                if (dirty) so.ApplyModifiedPropertiesWithoutUndo();
            }

            if (rewired > 0) changes.Add($"rewired {rewired} cross-root reference(s) from A to their B counterparts");
            if (unresolved > 0) changes.Add($"{unresolved} cross-root reference(s) could not be rewired (see errors above)");
        }

        static Object ResolveSameType(Object original, GameObject mappedGO)
        {
            if (original is GameObject) return mappedGO;
            if (original is Component comp) return mappedGO.GetComponent(comp.GetType());
            return null;
        }

        // ---- Step 5: Cat_B ----

        static CatMotor2D EnsureCatB(RealityRoot rootA, RealityRoot rootB, CatMotor2D catA, List<string> changes)
        {
            GameObject existing = FindUnder(rootB.transform, CatBName);
            if (existing != null) return existing.GetComponent<CatMotor2D>();

            string prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(catA.gameObject);
            if (string.IsNullOrEmpty(prefabPath))
            {
                Debug.LogError($"RealitySetup: '{CatAName}' is not a prefab instance; cannot create {CatBName} via PrefabUtility.InstantiatePrefab.");
                return null;
            }

            GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefabAsset == null)
            {
                Debug.LogError($"RealitySetup: could not load prefab asset at '{prefabPath}' for {CatBName}.");
                return null;
            }

            SpawnPoint spawnB = rootB.GetComponentInChildren<SpawnPoint>(true);
            Vector2 spawnPos = spawnB != null ? spawnB.Position : (Vector2)rootB.transform.position;

            GameObject catB = (GameObject)PrefabUtility.InstantiatePrefab(prefabAsset, rootB.transform);
            Undo.RegisterCreatedObjectUndo(catB, "Setup Realities");
            catB.name = CatBName;
            catB.transform.position = spawnPos;

            var debugControl = catB.GetComponent<GravityDebugControl>();
            if (debugControl != null) Object.DestroyImmediate(debugControl);

            if (catB.GetComponent<CatRespawn>() == null)
            {
                Debug.LogError($"RealitySetup: {CatBName} has no CatRespawn component from the prefab.");
            }

            changes.Add($"created {CatBName} under {RootBName} from '{prefabPath}' at Spawn_B ({spawnPos})");

            return catB.GetComponent<CatMotor2D>();
        }

        static void TintSpriteRenderers(IEnumerable<SpriteRenderer> renderers, Color tint, string label, List<string> changes)
        {
            int changed = 0;
            foreach (SpriteRenderer sr in renderers)
            {
                if (sr.color == tint) continue;
                sr.color = tint;
                changed++;
            }

            if (changed > 0) changes.Add($"tinted {changed} SpriteRenderer(s) under {label}");
        }

        // ---- Step 6: cameras ----

        static (Camera, Camera) EnsureCameras(CatMotor2D catA, CatMotor2D catB, List<string> changes)
        {
            GameObject camAGO = GameObject.Find("Camera_A") ?? GameObject.Find("Main Camera");
            if (camAGO == null)
            {
                Debug.LogError("RealitySetup: no 'Camera_A' or 'Main Camera' found. Cannot set up cameras.");
                return (null, null);
            }

            if (camAGO.name != "Camera_A")
            {
                camAGO.name = "Camera_A";
                changes.Add("renamed 'Main Camera' to 'Camera_A'");
            }

            var camA = camAGO.GetComponent<Camera>();
            int layerA = LayerMask.NameToLayer("RealityA");
            int layerB = LayerMask.NameToLayer("RealityB");
            int layerUI = LayerMask.NameToLayer("UI");

            ConfigureCamera(camA, (1 << layerA) | (1 << layerUI), CameraClearFlags.SolidColor, WarmCameraBg, catA.transform, changes, "Camera_A");

            GameObject camBGO = GameObject.Find("Camera_B");
            bool createdB = camBGO == null;
            if (createdB)
            {
                camBGO = Object.Instantiate(camAGO);
                Undo.RegisterCreatedObjectUndo(camBGO, "Setup Realities");
                camBGO.name = "Camera_B";

                var audioListener = camBGO.GetComponent<AudioListener>();
                if (audioListener != null) Object.DestroyImmediate(audioListener);

                changes.Add("created 'Camera_B' from 'Camera_A'");
            }

            var camB = camBGO.GetComponent<Camera>();
            ConfigureCamera(camB, (1 << layerB) | (1 << layerUI), CameraClearFlags.SolidColor, ColdCameraBg, catB.transform, changes, "Camera_B");

            if (camB.enabled)
            {
                camB.enabled = false;
                changes.Add("disabled 'Camera_B' Camera component");
            }

            Vector3 catBPos = catB.transform.position;
            camBGO.transform.position = new Vector3(catBPos.x, catBPos.y, camAGO.transform.position.z);

            return (camA, camB);
        }

        static void ConfigureCamera(Camera cam, int cullingMask, CameraClearFlags clearFlags, Color background, Transform target, List<string> changes, string label)
        {
            if (cam.cullingMask != cullingMask)
            {
                cam.cullingMask = cullingMask;
                changes.Add($"set {label}.cullingMask");
            }
            if (cam.clearFlags != clearFlags)
            {
                cam.clearFlags = clearFlags;
                changes.Add($"set {label}.clearFlags = SolidColor");
            }
            if (cam.backgroundColor != background)
            {
                cam.backgroundColor = background;
                changes.Add($"set {label}.backgroundColor");
            }

            var follow = cam.GetComponent<Parallax.Gameplay.Cameras.CatCameraFollow>();
            if (follow != null)
            {
                var followSO = new SerializedObject(follow);
                var targetProp = followSO.FindProperty("target");
                if (targetProp.objectReferenceValue != target)
                {
                    targetProp.objectReferenceValue = target;
                    changes.Add($"assigned {label} CatCameraFollow.target");
                }

                var respawnProp = followSO.FindProperty("respawn");
                var respawn = target.GetComponent<CatRespawn>();
                if (respawnProp.objectReferenceValue != respawn)
                {
                    respawnProp.objectReferenceValue = respawn;
                    changes.Add($"assigned {label} CatCameraFollow.respawn");
                }
                followSO.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        // ---- Step 7: lights ----

        static void EnsureLight(RealityRoot root, Color color, List<string> changes)
        {
            Transform lightsFolder = root.transform.Find(LightsFolderName);
            if (lightsFolder == null) return;

            string name = $"GlobalLight_{root.Id}";
            GameObject go = FindUnder(lightsFolder, name);
            bool created = go == null;
            if (created)
            {
                go = new GameObject(name);
                Undo.RegisterCreatedObjectUndo(go, "Setup Realities");
                go.transform.SetParent(lightsFolder, false);
                changes.Add($"created '{name}' under {root.gameObject.name}/{LightsFolderName}");
            }

            var light = go.GetComponent<Light2D>();
            if (light == null)
            {
                light = go.AddComponent<Light2D>();
                changes.Add($"added Light2D to '{name}'");
            }

            if (light.lightType != Light2D.LightType.Global)
            {
                light.lightType = Light2D.LightType.Global;
                changes.Add($"set '{name}'.lightType = Global");
            }
            if (light.color != color)
            {
                light.color = color;
                changes.Add($"set '{name}'.color");
            }
            if (!Mathf.Approximately(light.intensity, 1f))
            {
                light.intensity = 1f;
                changes.Add($"set '{name}'.intensity = 1");
            }

            int[] targetIds =
            {
                SortingLayer.NameToID(RealitySpace.SortingLayerName(root.Id, SortingBand.Background)),
                SortingLayer.NameToID(RealitySpace.SortingLayerName(root.Id, SortingBand.Middle)),
                SortingLayer.NameToID(RealitySpace.SortingLayerName(root.Id, SortingBand.Gameplay)),
                SortingLayer.NameToID(RealitySpace.SortingLayerName(root.Id, SortingBand.Foreground)),
            };

            if (light.targetSortingLayers == null || !light.targetSortingLayers.OrderBy(x => x).SequenceEqual(targetIds.OrderBy(x => x)))
            {
                light.targetSortingLayers = targetIds;
                changes.Add($"set '{name}'.targetSortingLayers to {root.Id}'s four sorting layers");
            }
        }

        static void DisableStrayGlobalLights(RealityRoot rootA, RealityRoot rootB, List<string> changes)
        {
            foreach (Light2D light in Object.FindObjectsByType<Light2D>(FindObjectsSortMode.None))
            {
                if (light.lightType != Light2D.LightType.Global) continue;
                if (light.transform.IsChildOf(rootA.transform) || light.transform.IsChildOf(rootB.transform)) continue;
                if (!light.enabled) continue;

                light.enabled = false;
                changes.Add($"disabled stray Global Light2D '{RealityIsolationValidator.Path(light.transform)}'");
                Debug.LogWarning($"RealitySetup: disabled stray Global Light2D at '{RealityIsolationValidator.Path(light.transform)}'.");
            }
        }

        // ---- Step 8: observers ----

        static void WireObservers(RealityRoot rootA, RealityRoot rootB, CatMotor2D catA, CatMotor2D catB, Camera camA, Camera camB, List<string> changes)
        {
            GameObject observersGO = GameObject.Find("Observers");
            if (observersGO == null)
            {
                Debug.LogError("RealitySetup: no 'Observers' GameObject found. Run 'PARALLAX/Setup/Observers (Sandbox)' first.");
                return;
            }

            var observerSet = observersGO.GetComponent<ObserverSet>();
            if (observerSet == null)
            {
                Debug.LogError("RealitySetup: 'Observers' has no ObserverSet. Run 'PARALLAX/Setup/Observers (Sandbox)' first.");
                return;
            }

            GameObject observerAGO = GameObject.Find("Observer_A");
            if (observerAGO != null)
            {
                WireObserverContext(observerAGO.GetComponent<ObserverContext>(), rootA, catA, camA, "Observer_A", changes);
            }

            GameObject observerBGO = GameObject.Find("Observer_B");
            bool createdB = observerBGO == null;
            if (createdB)
            {
                observerBGO = new GameObject("Observer_B");
                Undo.RegisterCreatedObjectUndo(observerBGO, "Setup Realities");
                observerBGO.transform.SetParent(observersGO.transform);
                changes.Add("created Observer_B");
            }

            var observerB = observerBGO.GetComponent<ObserverContext>();
            if (observerB == null)
            {
                observerB = observerBGO.AddComponent<ObserverContext>();
                changes.Add("added ObserverContext to Observer_B");
            }

            var observerBSO = new SerializedObject(observerB);
            var idProp = observerBSO.FindProperty("id");
            if ((ObserverId)idProp.enumValueIndex != ObserverId.B)
            {
                idProp.enumValueIndex = (int)ObserverId.B;
                changes.Add("set Observer_B.id = B");
            }
            observerBSO.ApplyModifiedPropertiesWithoutUndo();

            WireObserverContext(observerB, rootB, catB, camB, "Observer_B", changes);

            var observerSetSO = new SerializedObject(observerSet);
            var observerBProp = observerSetSO.FindProperty("observerB");
            if (observerBProp.objectReferenceValue != observerB)
            {
                observerBProp.objectReferenceValue = observerB;
                observerSetSO.ApplyModifiedPropertiesWithoutUndo();
                changes.Add("assigned ObserverSet.observerB = Observer_B");
            }

            var toggle = observersGO.GetComponent<RealityViewDebugToggle>();
            if (toggle == null)
            {
                toggle = observersGO.AddComponent<RealityViewDebugToggle>();
                changes.Add("added RealityViewDebugToggle to Observers");
            }

            var toggleSO = new SerializedObject(toggle);
            var toggleObserversProp = toggleSO.FindProperty("observers");
            if (toggleObserversProp.objectReferenceValue != observerSet)
            {
                toggleObserversProp.objectReferenceValue = observerSet;
                toggleSO.ApplyModifiedPropertiesWithoutUndo();
                changes.Add("assigned RealityViewDebugToggle.observers = ObserverSet");
            }
        }

        static void WireObserverContext(ObserverContext observer, RealityRoot reality, CatMotor2D cat, Camera cam, string label, List<string> changes)
        {
            if (observer == null)
            {
                Debug.LogError($"RealitySetup: '{label}' has no ObserverContext.");
                return;
            }

            var so = new SerializedObject(observer);
            bool dirty = false;

            var realityProp = so.FindProperty("reality");
            if (realityProp.objectReferenceValue != reality)
            {
                realityProp.objectReferenceValue = reality;
                dirty = true;
                changes.Add($"assigned {label}.reality");
            }

            var catProp = so.FindProperty("cat");
            if (catProp.objectReferenceValue != cat)
            {
                catProp.objectReferenceValue = cat;
                dirty = true;
                changes.Add($"assigned {label}.cat");
            }

            var camProp = so.FindProperty("observerCamera");
            if (camProp.objectReferenceValue != cam)
            {
                camProp.objectReferenceValue = cam;
                dirty = true;
                changes.Add($"assigned {label}.observerCamera");
            }

            if (dirty) so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ---- helpers ----

        static GameObject FindUnder(Transform parent, string name)
        {
            if (parent == null) return null;
            Transform found = parent.Find(name);
            return found != null ? found.gameObject : null;
        }

        static string PathOf(Transform t) => RealityIsolationValidator.Path(t);

        static Color ParseColor(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out Color c);
            return c;
        }
    }
}
