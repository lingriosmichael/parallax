using System.Collections.Generic;
using Parallax.Core;
using Parallax.Gameplay.Anchors;
using Parallax.Gameplay.Observers;
using Parallax.Gameplay.Player;
using Parallax.Gameplay.Reality;
using Parallax.Gameplay.Sensors;
using Parallax.Gameplay.Transport;
using Parallax.Gameplay.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Parallax.Editor.Setup
{
    public static class PlateGateSetup
    {
        const string DefinitionPath = "Assets/_Game/Data/Anchors/Anchor_Gate01.asset";

        [MenuItem("PARALLAX/Setup/Plate + Gate (Sandbox)")]
        public static void Configure()
        {
            var changes = new List<string>();
            GameObject rootAObject = GameObject.Find("RealityRoot_A");
            GameObject rootBObject = GameObject.Find("RealityRoot_B");
            RealityRoot rootA = rootAObject == null ? null : rootAObject.GetComponent<RealityRoot>();
            RealityRoot rootB = rootBObject == null ? null : rootBObject.GetComponent<RealityRoot>();
            TransportHost host = Object.FindAnyObjectByType<LocalTransportHost>(FindObjectsInactive.Include);
            ObserverSet observers = Object.FindAnyObjectByType<ObserverSet>(FindObjectsInactive.Include);
            SoloSwitchController switchController = Object.FindAnyObjectByType<SoloSwitchController>(FindObjectsInactive.Include);
            if (rootA == null || rootB == null || host == null || observers == null || switchController == null)
            {
                Debug.LogError("PlateGateSetup: RealityRoot_A, RealityRoot_B, LocalTransportHost, ObserverSet, and SoloSwitchController are required.");
                return;
            }
            float groundTop = GroundTop(rootA);
            float jumpHeight = JumpHeight(rootA);
            float spawnX = SpawnX(rootA);
            float gateHeight = 1.6f * jumpHeight;
            float gateX = spawnX - 2.5f;
            float goalX = spawnX - 4.5f;
            if (!FitRealityB(rootB, spawnX, ref gateX, ref goalX)) return;
            Debug.Log($"PlateGateSetup: groundTop={groundTop:F2}; jumpHeight={jumpHeight:F2}; spawnX={spawnX:F2}; plate=({spawnX - 2.5f:F2},{groundTop:F2}); gate=({gateX:F2},{groundTop + gateHeight / 2f:F2}); goal=({goalX:F2},{groundTop + 0.6f:F2}).");
            AnchorDefinition definition = SetupUtility.EnsureDefinition(DefinitionPath, 2, "Gate01", changes);
            PressurePlateSensor plate = BuildPlate(rootA, definition, host, observers, spawnX, groundTop, changes);
            ElevatorManifestation gate = BuildGate(rootB, gateX, groundTop, gateHeight, changes);
            BuildGoal(rootB, goalX, groundTop, changes);
            BuildPresenter(definition, host, gate, changes);
            BuildTimeline(observers, switchController, changes);
            if (changes.Count == 0) Debug.Log("PlateGateSetup: no changes.");
            else
            {
                Debug.Log("PlateGateSetup: " + string.Join("; ", changes));
                EditorSceneManager.MarkSceneDirty(rootA.gameObject.scene);
            }
            RealityIsolationValidator.Validate();
            AnchorValidator.Validate();
        }

        static PressurePlateSensor BuildPlate(RealityRoot root, AnchorDefinition definition, TransportHost host, ObserverSet observers, float spawnX, float groundTop, List<string> changes)
        {
            int layer = LayerMask.NameToLayer(RealitySpace.PhysicsLayerName(root.Id));
            Transform anchors = SetupUtility.EnsureChild(root.transform, "Anchors", layer, changes);
            GameObject plate = SetupUtility.EnsureChild(anchors, "Plate_A", layer, changes).gameObject;
            SetupUtility.SetLocalPosition(plate.transform, new Vector2(spawnX - 2.5f, groundTop), changes);
            PressurePlateSensor sensor = SetupUtility.Ensure<PressurePlateSensor>(plate, changes);
            GameObject indicator = SetupUtility.EnsureChild(plate.transform, "Indicator", layer, changes).gameObject;
            SetupUtility.SetLocalPosition(indicator.transform, new Vector2(0f, 0.075f), changes);
            SpriteRenderer renderer = SetupUtility.SetVisual(indicator, root, new Vector2(1.2f, 0.15f), new Color(1f, 1f, 1f, 0.25f), changes);
            SetupUtility.SetObject(sensor, "definition", definition, changes);
            SetupUtility.SetObject(sensor, "transportHost", host, changes);
            SetupUtility.SetObject(sensor, "observers", observers, changes);
            SetupUtility.SetObject(sensor, "indicator", renderer, changes);
            return sensor;
        }

        static ElevatorManifestation BuildGate(RealityRoot root, float x, float groundTop, float height, List<string> changes)
        {
            int layer = LayerMask.NameToLayer(RealitySpace.PhysicsLayerName(root.Id));
            Transform anchors = SetupUtility.EnsureChild(root.transform, "Anchors", layer, changes);
            GameObject gate = SetupUtility.EnsureChild(anchors, "Gate_B", layer, changes).gameObject;
            Vector2 lowered = new Vector2(x, groundTop + height / 2f);
            Vector2 raised = new Vector2(x, groundTop + height * 1.5f + 0.2f);
            SetupUtility.SetLocalPosition(gate.transform, lowered, changes);
            ElevatorManifestation manifestation = SetupUtility.Ensure<ElevatorManifestation>(gate, changes);
            Rigidbody2D body = SetupUtility.Ensure<Rigidbody2D>(gate, changes);
            BoxCollider2D collider = SetupUtility.Ensure<BoxCollider2D>(gate, changes);
            SetupUtility.SetBodyType(body, RigidbodyType2D.Kinematic, changes);
            SetupUtility.SetColliderSize(collider, new Vector2(0.5f, height), changes);
            SetupUtility.SetVector2(manifestation, "loweredLocal", lowered, changes);
            SetupUtility.SetVector2(manifestation, "raisedLocal", raised, changes);
            SetupUtility.SetFloat(manifestation, "speed", 4f, changes);
            SetupUtility.SetVisual(gate, root, new Vector2(0.5f, height), Color.white, changes);
            return manifestation;
        }

        static void BuildGoal(RealityRoot root, float x, float groundTop, List<string> changes)
        {
            int layer = LayerMask.NameToLayer(RealitySpace.PhysicsLayerName(root.Id));
            Transform geometry = SetupUtility.EnsureChild(root.transform, "Geometry", layer, changes);
            GameObject goal = SetupUtility.EnsureChild(geometry, "Goal_B", layer, changes).gameObject;
            SetupUtility.SetLocalPosition(goal.transform, new Vector2(x, groundTop + 0.6f), changes);
            SetupUtility.SetVisual(goal, root, new Vector2(0.4f, 1.2f), new Color(0.7f, 1f, 0.7f, 1f), changes);
        }

        static void BuildPresenter(AnchorDefinition definition, TransportHost host, ElevatorManifestation gate, List<string> changes)
        {
            GameObject anchors = SetupUtility.EnsureSceneRoot("Anchors", changes);
            GameObject presenterObject = SetupUtility.EnsureChild(anchors.transform, "Presenter_Gate01", anchors.layer, changes).gameObject;
            RealityPresenter presenter = SetupUtility.Ensure<RealityPresenter>(presenterObject, changes);
            SetupUtility.SetObject(presenter, "definition", definition, changes);
            SetupUtility.SetObject(presenter, "transportHost", host, changes);
            SetupUtility.SetArray(presenter, "manifestations", new Object[] { gate }, changes);
        }

        static void BuildTimeline(ObserverSet observers, SoloSwitchController switchController, List<string> changes)
        {
            GameObject hud = GameObject.Find("HUD");
            if (hud == null)
            {
                Debug.LogError("PlateGateSetup: HUD is required for EchoTimeline.");
                return;
            }
            GameObject timeline = SetupUtility.EnsureChild(hud.transform, "EchoTimeline", hud.layer, changes).gameObject;
            RectTransform rect = EnsureRect(timeline);
            SetRect(rect, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -155f), new Vector2(360f, 36f), changes);
            EchoTimelineView view = SetupUtility.Ensure<EchoTimelineView>(timeline, changes);
            GameObject bar = SetupUtility.EnsureChild(timeline.transform, "Bar", timeline.layer, changes).gameObject;
            RectTransform barRect = EnsureRect(bar);
            SetRect(barRect, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, changes);
            Image background = SetupUtility.Ensure<Image>(bar, changes);
            SetImageSprite(background, changes);
            SetImageColor(background, new Color(0f, 0f, 0f, 0.55f), changes);
            SetRaycast(background, false, changes);
            GameObject fillObject = SetupUtility.EnsureChild(bar.transform, "Fill", bar.layer, changes).gameObject;
            SetSiblingIndex(fillObject.transform, 0, changes);
            RectTransform fillRect = EnsureRect(fillObject);
            SetRect(fillRect, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, changes);
            Image fill = SetupUtility.Ensure<Image>(fillObject, changes);
            SetImageSprite(fill, changes);
            SetImageColor(fill, new Color(0.4f, 0.9f, 1f, 0.9f), changes);
            if (fill.type != Image.Type.Filled || fill.fillMethod != Image.FillMethod.Horizontal || fill.fillOrigin != (int)Image.OriginHorizontal.Left)
            {
                fill.type = Image.Type.Filled;
                fill.fillMethod = Image.FillMethod.Horizontal;
                fill.fillOrigin = (int)Image.OriginHorizontal.Left;
                changes.Add("set EchoTimeline Fill horizontal");
            }
            SetRaycast(fill, false, changes);
            GameObject labelObject = SetupUtility.EnsureChild(bar.transform, "Label", bar.layer, changes).gameObject;
            SetSiblingIndex(labelObject.transform, 1, changes);
            RectTransform labelRect = EnsureRect(labelObject);
            SetRect(labelRect, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, changes);
            Text label = SetupUtility.Ensure<Text>(labelObject, changes);
            if (label.font == null) { label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); changes.Add("set EchoTimeline label font"); }
            if (label.alignment != TextAnchor.MiddleCenter) { label.alignment = TextAnchor.MiddleCenter; changes.Add("set EchoTimeline label alignment"); }
            if (label.color != Color.white) { label.color = Color.white; changes.Add("set EchoTimeline label color"); }
            if (label.fontSize != 20) { label.fontSize = 20; changes.Add("set EchoTimeline label font size"); }
            SetRaycast(label, false, changes);
            SetupUtility.SetObject(view, "observers", observers, changes);
            SetupUtility.SetObject(view, "switchController", switchController, changes);
            SetupUtility.SetObject(view, "bar", bar, changes);
            SetupUtility.SetObject(view, "fill", fill, changes);
            SetupUtility.SetObject(view, "label", label, changes);
        }

        static bool FitRealityB(RealityRoot root, float spawnX, ref float gateX, ref float goalX)
        {
            Transform geometry = root.transform.Find("Geometry");
            if (geometry == null) { Debug.LogError("PlateGateSetup: Reality B Geometry is missing."); return false; }
            Transform arena = geometry.Find("GravityArena");
            if (arena == null) { Debug.LogError("PlateGateSetup: Reality B Geometry/GravityArena is missing."); return false; }
            Transform wallTransform = arena.Find("Wall_Left");
            if (wallTransform == null) { Debug.LogError("PlateGateSetup: Reality B Geometry/GravityArena/Wall_Left is missing."); return false; }
            BoxCollider2D wall = wallTransform.GetComponent<BoxCollider2D>();
            if (wall == null) { Debug.LogError("PlateGateSetup: Reality B left wall is missing."); return false; }
            float wallRight = root.ToLocal((Vector2)wall.bounds.max).x;
            float requiredShift = wallRight - (goalX - 0.2f);
            if (requiredShift <= 0f) return true;
            gateX += requiredShift;
            goalX += requiredShift;
            Debug.Log($"PlateGateSetup: shifted gate and goal right by {requiredShift:F2} to fit the left wall.");
            if (gateX + 0.25f <= spawnX - 1f) return true;
            Debug.LogError("PlateGateSetup: gate and goal cannot fit between Reality B's left wall and spawn point.");
            return false;
        }

        static float GroundTop(RealityRoot root)
        {
            Transform groundTransform = root.transform.Find("Geometry/Ground");
            BoxCollider2D ground = groundTransform == null ? null : groundTransform.GetComponent<BoxCollider2D>();
            if (ground == null)
            {
                Debug.LogError("PlateGateSetup: Reality A Geometry/Ground collider is required.");
                return 0f;
            }
            return root.ToLocal((Vector2)ground.bounds.max).y;
        }
        static float SpawnX(RealityRoot root) => root.ToLocal(root.GetComponentInChildren<CatMotor2D>(true).transform.position).x;
        static float JumpHeight(RealityRoot root)
        {
            CatMotor2D cat = root.GetComponentInChildren<CatMotor2D>(true);
            if (cat == null)
            {
                Debug.LogWarning("PlateGateSetup: CatMotor2D missing; using jumpHeight=2.0.");
                return 2f;
            }
            SerializedProperty property = new SerializedObject(cat).FindProperty("config");
            CatMotorConfig config = property == null ? null : property.objectReferenceValue as CatMotorConfig;
            if (config != null) return config.JumpHeight;
            Debug.LogWarning("PlateGateSetup: CatMotorConfig missing; using jumpHeight=2.0.");
            return 2f;
        }

        static RectTransform EnsureRect(GameObject go)
        {
            RectTransform rect = go.GetComponent<RectTransform>();
            if (rect == null) rect = go.AddComponent<RectTransform>();
            return rect;
        }
        static void SetImageSprite(Image image, List<string> changes)
        {
            Sprite sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            if (image.sprite == sprite) return;
            image.sprite = sprite;
            changes.Add("set " + image.name + ".sprite");
        }
        static void SetImageColor(Image image, Color color, List<string> changes)
        {
            if (image.color == color) return;
            image.color = color;
            changes.Add("set " + image.name + ".color");
        }
        static void SetSiblingIndex(Transform transform, int value, List<string> changes)
        {
            if (transform.GetSiblingIndex() == value) return;
            transform.SetSiblingIndex(value);
            changes.Add("set " + transform.name + ".siblingIndex");
        }
        static void SetRect(RectTransform rect, Vector2 min, Vector2 max, Vector2 pivot, Vector2 position, Vector2 size, List<string> changes)
        {
            if (rect.anchorMin == min && rect.anchorMax == max && rect.pivot == pivot && rect.anchoredPosition == position && rect.sizeDelta == size) return;
            rect.anchorMin = min; rect.anchorMax = max; rect.pivot = pivot; rect.anchoredPosition = position; rect.sizeDelta = size;
            changes.Add("positioned " + rect.name);
        }
        static void SetRaycast(Graphic graphic, bool value, List<string> changes)
        {
            if (graphic.raycastTarget == value) return;
            graphic.raycastTarget = value;
            changes.Add("set " + graphic.name + ".raycastTarget");
        }
    }
}
