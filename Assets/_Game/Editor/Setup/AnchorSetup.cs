using System.Collections.Generic;
using Parallax.Core;
using Parallax.Gameplay.Anchors;
using Parallax.Gameplay.Interaction;
using Parallax.Gameplay.Player;
using Parallax.Gameplay.Reality;
using Parallax.Gameplay.Transport;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Parallax.Editor.Setup
{
    public static class AnchorSetup
    {
        const string DefinitionPath = "Assets/_Game/Data/Anchors/Anchor_Pathway01.asset";

        [MenuItem("PARALLAX/Setup/Anchors (Sandbox)")]
        public static void Configure()
        {
            var changes = new List<string>();
            var rootA = GameObject.Find("RealityRoot_A")?.GetComponent<RealityRoot>();
            var rootB = GameObject.Find("RealityRoot_B")?.GetComponent<RealityRoot>();
            var host = Object.FindAnyObjectByType<LocalTransportHost>(FindObjectsInactive.Include);
            if (rootA == null || rootB == null || host == null)
            {
                Debug.LogError("AnchorSetup: RealityRoot_A, RealityRoot_B, and LocalTransportHost are required.");
                return;
            }

            AnchorDefinition definition = EnsureDefinition(changes);
            float groundTop = GroundTop(rootA);
            float jumpHeight = JumpHeight(rootA);
            float spawnX = SpawnX(rootA);
            Debug.Log($"AnchorSetup: groundTop={groundTop:F2}; jumpHeight={jumpHeight:F2}; spawnX={spawnX:F2}.");

            BuildRealityA(rootA, definition, spawnX, groundTop, changes, out VineManifestation vine);
            BuildRealityB(rootB, spawnX, groundTop, jumpHeight, changes, out ElevatorManifestation elevator);
            BuildPresenter(definition, host, vine, elevator, changes);
            EnsureInteractors(rootA, host, changes);
            EnsureInteractors(rootB, host, changes);

            if (changes.Count == 0)
            {
                Debug.Log("AnchorSetup: no changes.");
            }
            else
            {
                Debug.Log("AnchorSetup: " + string.Join("; ", changes));
                EditorSceneManager.MarkSceneDirty(rootA.gameObject.scene);
            }

            RealityIsolationValidator.Validate();
            AnchorValidator.Validate();
        }

        static void BuildRealityA(RealityRoot root, AnchorDefinition definition, float spawnX, float groundTop, List<string> changes, out VineManifestation vineManifestation)
        {
            int layer = LayerMask.NameToLayer(RealitySpace.PhysicsLayerName(root.Id));
            Transform anchors = EnsureChild(root.transform, "Anchors", layer, changes);
            GameObject vine = EnsureChild(anchors, "Vine_A", layer, changes).gameObject;
            GameObject knot = EnsureChild(vine.transform, "Knot", layer, changes).gameObject;
            SetLocalPosition(knot.transform, new Vector2(spawnX + 3f, groundTop + 2.2f), changes);
            vineManifestation = Ensure<VineManifestation>(vine, changes);
            SetObject(vineManifestation, "knot", knot.transform, changes);
            SetVisual(knot, root, new Vector2(0.3f, 0.3f), Color.white, changes);

            GameObject zone = EnsureChild(anchors, "VineZone_A", layer, changes).gameObject;
            SetLocalPosition(zone.transform, new Vector2(spawnX + 3f, groundTop + 0.8f), changes);
            BoxCollider2D zoneCollider = Ensure<BoxCollider2D>(zone, changes);
            SetTrigger(zoneCollider, true, changes);
            SetColliderSize(zoneCollider, new Vector2(1.2f, 1.6f), changes);
            VineInteractable interactable = Ensure<VineInteractable>(zone, changes);
            GameObject indicator = EnsureChild(zone.transform, "Indicator", layer, changes).gameObject;
            SpriteRenderer indicatorRenderer = SetVisual(indicator, root, new Vector2(1.2f, 1.6f), new Color(1f, 1f, 1f, 0.2f), changes);
            SetObject(interactable, "definition", definition, changes);
            SetObject(interactable, "indicator", indicatorRenderer, changes);
            Debug.Log($"AnchorSetup: vine=({spawnX + 3f:F2},{groundTop + 2.2f:F2}); zone=({spawnX + 3f:F2},{groundTop + 0.8f:F2}).");
        }

        static void BuildRealityB(RealityRoot root, float spawnX, float groundTop, float jumpHeight, List<string> changes, out ElevatorManifestation elevatorManifestation)
        {
            int layer = LayerMask.NameToLayer(RealitySpace.PhysicsLayerName(root.Id));
            Transform anchors = EnsureChild(root.transform, "Anchors", layer, changes);
            Transform geometry = EnsureChild(root.transform, "Geometry", layer, changes);
            float elevatorX = spawnX + 5f;
            float ledgeX = elevatorX + 2.8f;
            BoxCollider2D ground = root.transform.Find("Geometry/Ground")?.GetComponent<BoxCollider2D>();
            if (ground != null)
            {
                float groundRight = root.ToLocal((Vector2)ground.bounds.max).x;
                float excess = ledgeX + 1.5f - groundRight;
                if (excess > 0f)
                {
                    elevatorX -= excess;
                    ledgeX -= excess;
                    Debug.Log($"AnchorSetup: shifted elevator and ledge left by {excess:F2} to fit Ground bounds.");
                }
            }

            float loweredY = groundTop + 0.2f;
            float raisedY = groundTop + 0.7f * jumpHeight - 0.2f;
            GameObject elevator = EnsureChild(anchors, "Elevator_B", layer, changes).gameObject;
            SetLocalPosition(elevator.transform, new Vector2(elevatorX, loweredY), changes);
            elevatorManifestation = Ensure<ElevatorManifestation>(elevator, changes);
            Rigidbody2D body = Ensure<Rigidbody2D>(elevator, changes);
            SetBodyType(body, RigidbodyType2D.Kinematic, changes);
            BoxCollider2D elevatorCollider = Ensure<BoxCollider2D>(elevator, changes);
            SetColliderSize(elevatorCollider, new Vector2(2f, 0.4f), changes);
            SetVector2(elevatorManifestation, "loweredLocal", new Vector2(elevatorX, loweredY), changes);
            SetVector2(elevatorManifestation, "raisedLocal", new Vector2(elevatorX, raisedY), changes);
            SetVisual(elevator, root, new Vector2(2f, 0.4f), Color.white, changes);

            GameObject ledge = EnsureChild(geometry, "Ledge_B", layer, changes).gameObject;
            Vector2 ledgePosition = new Vector2(ledgeX, groundTop + 1.4f * jumpHeight - 0.25f);
            SetLocalPosition(ledge.transform, ledgePosition, changes);
            BoxCollider2D ledgeCollider = Ensure<BoxCollider2D>(ledge, changes);
            SetColliderSize(ledgeCollider, new Vector2(3f, 0.5f), changes);
            SetVisual(ledge, root, new Vector2(3f, 0.5f), Color.white, changes);
            Debug.Log($"AnchorSetup: elevatorLower=({elevatorX:F2},{loweredY:F2}); elevatorRaised=({elevatorX:F2},{raisedY:F2}); ledge=({ledgePosition.x:F2},{ledgePosition.y:F2}).");
        }

        static void BuildPresenter(AnchorDefinition definition, TransportHost host, VineManifestation vine, ElevatorManifestation elevator, List<string> changes)
        {
            GameObject presenterRoot = EnsureSceneRoot("Anchors", changes);
            GameObject presenterObject = EnsureChild(presenterRoot.transform, "Presenter_Pathway01", presenterRoot.layer, changes).gameObject;
            RealityPresenter presenter = Ensure<RealityPresenter>(presenterObject, changes);
            SetObject(presenter, "definition", definition, changes);
            SetObject(presenter, "transportHost", host, changes);
            SetArray(presenter, "manifestations", new Object[] { vine, elevator }, changes);
        }

        static AnchorDefinition EnsureDefinition(List<string> changes)
        {
            AnchorDefinition definition = AssetDatabase.LoadAssetAtPath<AnchorDefinition>(DefinitionPath);
            if (definition == null)
            {
                if (!AssetDatabase.IsValidFolder("Assets/_Game/Data")) AssetDatabase.CreateFolder("Assets/_Game", "Data");
                if (!AssetDatabase.IsValidFolder("Assets/_Game/Data/Anchors")) AssetDatabase.CreateFolder("Assets/_Game/Data", "Anchors");
                definition = ScriptableObject.CreateInstance<AnchorDefinition>();
                AssetDatabase.CreateAsset(definition, DefinitionPath);
                changes.Add("created Anchor_Pathway01 asset");
            }

            SerializedObject serialized = new SerializedObject(definition);
            SetInt(serialized, "id", 1, changes, "Anchor_Pathway01.id");
            SetFloat(serialized, "initialValue", 0f, changes, "Anchor_Pathway01.initialValue");
            SetString(serialized, "displayName", "Pathway01", changes, "Anchor_Pathway01.displayName");
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return definition;
        }

        static float GroundTop(RealityRoot root)
        {
            BoxCollider2D ground = root.transform.Find("Geometry/Ground")?.GetComponent<BoxCollider2D>();
            return ground == null ? 0f : root.ToLocal((Vector2)ground.bounds.max).y;
        }

        static float SpawnX(RealityRoot root)
        {
            CatMotor2D cat = root.GetComponentInChildren<CatMotor2D>(true);
            return cat == null ? 0f : root.ToLocal(cat.transform.position).x;
        }

        static float JumpHeight(RealityRoot root)
        {
            CatMotor2D cat = root.GetComponentInChildren<CatMotor2D>(true);
            CatMotorConfig config = cat == null ? null : new SerializedObject(cat).FindProperty("config").objectReferenceValue as CatMotorConfig;
            if (config == null)
            {
                Debug.LogWarning("AnchorSetup: CatMotorConfig missing; using jumpHeight=2.0.");
                return 2f;
            }
            return config.JumpHeight;
        }

        static Transform EnsureChild(Transform parent, string name, int layer, List<string> changes)
        {
            Transform child = parent.Find(name);
            if (child == null)
            {
                var gameObject = new GameObject(name);
                Undo.RegisterCreatedObjectUndo(gameObject, "Setup Anchors");
                gameObject.transform.SetParent(parent, false);
                child = gameObject.transform;
                changes.Add("created " + RealityIsolationValidator.Path(child));
            }
            SetLayer(child.gameObject, layer, changes);
            return child;
        }

        static GameObject EnsureSceneRoot(string name, List<string> changes)
        {
            foreach (GameObject root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
                if (root.name == name) return root;
            var gameObject = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(gameObject, "Setup Anchors");
            changes.Add("created " + name);
            return gameObject;
        }

        static T Ensure<T>(GameObject gameObject, List<string> changes) where T : Component
        {
            T component = gameObject.GetComponent<T>();
            if (component != null) return component;
            component = gameObject.AddComponent<T>();
            changes.Add("added " + typeof(T).Name + " to " + RealityIsolationValidator.Path(gameObject.transform));
            return component;
        }

        static SpriteRenderer SetVisual(GameObject gameObject, RealityRoot root, Vector2 size, Color color, List<string> changes)
        {
            SpriteRenderer renderer = Ensure<SpriteRenderer>(gameObject, changes);
            SpriteRenderer source = root.transform.Find("Geometry/Ground")?.GetComponent<SpriteRenderer>();
            if (source != null && renderer.sprite != source.sprite)
            {
                renderer.sprite = source.sprite;
                changes.Add("set " + gameObject.name + ".sprite");
            }
            if (renderer.drawMode != SpriteDrawMode.Sliced)
            {
                renderer.drawMode = SpriteDrawMode.Sliced;
                changes.Add("set " + gameObject.name + ".drawMode");
            }
            if (renderer.size != size)
            {
                renderer.size = size;
                changes.Add("set " + gameObject.name + ".size");
            }
            string sortingLayer = RealitySpace.SortingLayerName(root.Id, SortingBand.Gameplay);
            if (renderer.sortingLayerName != sortingLayer)
            {
                renderer.sortingLayerName = sortingLayer;
                changes.Add("set " + gameObject.name + ".sortingLayer");
            }
            if (renderer.color != color)
            {
                renderer.color = color;
                changes.Add("set " + gameObject.name + ".color");
            }
            return renderer;
        }

        static void EnsureInteractors(RealityRoot root, TransportHost host, List<string> changes)
        {
            foreach (CatMotor2D cat in root.GetComponentsInChildren<CatMotor2D>(true))
            {
                CatInteractor interactor = Ensure<CatInteractor>(cat.gameObject, changes);
                SetObject(interactor, "transportHost", host, changes);
            }
        }

        static void SetLayer(GameObject gameObject, int layer, List<string> changes)
        {
            if (gameObject.layer == layer) return;
            gameObject.layer = layer;
            changes.Add("set " + RealityIsolationValidator.Path(gameObject.transform) + ".layer");
        }

        static void SetLocalPosition(Transform transform, Vector2 value, List<string> changes)
        {
            if ((Vector2)transform.localPosition == value) return;
            transform.localPosition = value;
            changes.Add("set " + RealityIsolationValidator.Path(transform) + ".localPosition");
        }

        static void SetTrigger(BoxCollider2D collider, bool value, List<string> changes)
        {
            if (collider.isTrigger == value) return;
            collider.isTrigger = value;
            changes.Add("set " + collider.name + ".isTrigger");
        }

        static void SetColliderSize(BoxCollider2D collider, Vector2 value, List<string> changes)
        {
            if (collider.size == value) return;
            collider.size = value;
            changes.Add("set " + collider.name + ".size");
        }

        static void SetBodyType(Rigidbody2D body, RigidbodyType2D value, List<string> changes)
        {
            if (body.bodyType == value) return;
            body.bodyType = value;
            changes.Add("set " + body.name + ".bodyType");
        }

        static void SetObject(Object target, string propertyName, Object value, List<string> changes)
        {
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null || property.objectReferenceValue == value) return;
            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            changes.Add("wired " + target.name + "." + propertyName);
        }

        static void SetVector2(Object target, string propertyName, Vector2 value, List<string> changes)
        {
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null || property.vector2Value == value) return;
            property.vector2Value = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            changes.Add("set " + target.name + "." + propertyName);
        }

        static void SetArray(Object target, string propertyName, Object[] values, List<string> changes)
        {
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null) return;
            bool matches = property.arraySize == values.Length;
            for (int i = 0; matches && i < values.Length; i++)
                matches = property.GetArrayElementAtIndex(i).objectReferenceValue == values[i];
            if (matches) return;
            property.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            serialized.ApplyModifiedPropertiesWithoutUndo();
            changes.Add("wired " + target.name + "." + propertyName);
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
