using Parallax.Core;
using Parallax.Gameplay.Reality;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace Parallax.Editor.Setup
{
    // Read-only. Changes nothing. Reusable for every future level scene.
    public static class RealityIsolationValidator
    {
        [MenuItem("PARALLAX/Validate/Reality Isolation")]
        public static void ValidateMenuItem() => Validate();

        public static int Validate()
        {
            GameObject rootAGO = GameObject.Find("RealityRoot_A");
            GameObject rootBGO = GameObject.Find("RealityRoot_B");
            RealityRoot rootA = rootAGO != null ? rootAGO.GetComponent<RealityRoot>() : null;
            RealityRoot rootB = rootBGO != null ? rootBGO.GetComponent<RealityRoot>() : null;

            if (rootA == null || rootB == null)
            {
                Debug.LogError("RealityIsolationValidator: RealityRoot_A and/or RealityRoot_B not found. Run 'PARALLAX/Setup/Realities (Sandbox)' first.");
                return -1;
            }

            int problems = 0;
            problems += CheckLayer(rootA);
            problems += CheckLayer(rootB);
            problems += CheckSortingLayers(rootA);
            problems += CheckSortingLayers(rootB);
            problems += CheckCrossReferences(rootA, rootB);
            problems += CheckCrossReferences(rootB, rootA);
            problems += CheckLights(rootA, rootB);
            problems += CheckLights(rootB, rootA);
            problems += CheckCameras();

            if (problems == 0)
            {
                Debug.Log("Reality isolation: OK");
            }
            else
            {
                Debug.LogError($"Reality isolation: {problems} problem(s) found (see warnings/errors above).");
            }

            return problems;
        }

        static int CheckLayer(RealityRoot root)
        {
            int expected = LayerMask.NameToLayer(RealitySpace.PhysicsLayerName(root.Id));
            int problems = 0;

            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t.gameObject.layer != expected)
                {
                    Debug.LogWarning($"Reality isolation: '{Path(t)}' is on layer '{LayerMask.LayerToName(t.gameObject.layer)}', expected '{LayerMask.LayerToName(expected)}'.");
                    problems++;
                }
            }

            return problems;
        }

        static int CheckSortingLayers(RealityRoot root)
        {
            string bg = RealitySpace.SortingLayerName(root.Id, SortingBand.Background);
            string mid = RealitySpace.SortingLayerName(root.Id, SortingBand.Middle);
            string gp = RealitySpace.SortingLayerName(root.Id, SortingBand.Gameplay);
            string fg = RealitySpace.SortingLayerName(root.Id, SortingBand.Foreground);

            int problems = 0;
            foreach (SpriteRenderer sr in root.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (sr.gameObject.layer != root.gameObject.layer)
                {
                    Debug.LogWarning($"Reality isolation: SpriteRenderer '{Path(sr.transform)}' is on layer '{LayerMask.LayerToName(sr.gameObject.layer)}', expected '{LayerMask.LayerToName(root.gameObject.layer)}'.");
                    problems++;
                }
                string layer = sr.sortingLayerName;
                if (layer != bg && layer != mid && layer != gp && layer != fg)
                {
                    Debug.LogWarning($"Reality isolation: SpriteRenderer '{Path(sr.transform)}' is on sorting layer '{layer}', not one of {root.Id}'s sorting layers.");
                    problems++;
                }
            }

            return problems;
        }

        // References under `from` that point at an object under `into`.
        static int CheckCrossReferences(RealityRoot from, RealityRoot into)
        {
            int problems = 0;

            foreach (Component c in from.GetComponentsInChildren<Component>(true))
            {
                if (c == null || c is Transform) continue;

                var so = new SerializedObject(c);
                SerializedProperty prop = so.GetIterator();
                bool enterChildren = true;
                while (prop.NextVisible(enterChildren))
                {
                    enterChildren = false;

                    if (prop.propertyType != SerializedPropertyType.ObjectReference) continue;

                    Object value = prop.objectReferenceValue;
                    if (value == null) continue;
                    if (EditorUtility.IsPersistent(value)) continue; // project asset, not a scene reference

                    GameObject targetGO = AsGameObject(value);
                    if (targetGO == null) continue;

                    if (targetGO.transform.IsChildOf(into.transform))
                    {
                        Debug.LogError($"Reality isolation: '{Path(c.transform)}' ({c.GetType().Name}.{prop.propertyPath}) under {from.Id} references '{Path(targetGO.transform)}' under {into.Id}.");
                        problems++;
                    }
                }
            }

            return problems;
        }

        static int CheckLights(RealityRoot root, RealityRoot other)
        {
            int otherBg = SortingLayer.NameToID(RealitySpace.SortingLayerName(other.Id, SortingBand.Background));
            int otherMid = SortingLayer.NameToID(RealitySpace.SortingLayerName(other.Id, SortingBand.Middle));
            int otherGp = SortingLayer.NameToID(RealitySpace.SortingLayerName(other.Id, SortingBand.Gameplay));
            int otherFg = SortingLayer.NameToID(RealitySpace.SortingLayerName(other.Id, SortingBand.Foreground));

            int problems = 0;
            foreach (Light2D light in root.GetComponentsInChildren<Light2D>(true))
            {
                int[] targets = light.targetSortingLayers;
                if (targets == null) continue;

                foreach (int id in targets)
                {
                    if (id == otherBg || id == otherMid || id == otherGp || id == otherFg)
                    {
                        Debug.LogError($"Reality isolation: Light2D '{Path(light.transform)}' under {root.Id} targets sorting layer '{SortingLayer.IDToName(id)}', which belongs to {other.Id}.");
                        problems++;
                    }
                }
            }

            return problems;
        }

        static int CheckCameras()
        {
            int layerA = LayerMask.NameToLayer(RealitySpace.PhysicsLayerName(ObserverId.A));
            int layerB = LayerMask.NameToLayer(RealitySpace.PhysicsLayerName(ObserverId.B));

            int problems = 0;
            foreach (Camera cam in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
            {
                bool hasA = (cam.cullingMask & (1 << layerA)) != 0;
                bool hasB = (cam.cullingMask & (1 << layerB)) != 0;
                if (hasA && hasB)
                {
                    Debug.LogError($"Reality isolation: camera '{Path(cam.transform)}' culling mask includes both RealityA and RealityB.");
                    problems++;
                }
            }

            return problems;
        }

        internal static GameObject AsGameObject(Object value)
        {
            if (value is GameObject go) return go;
            if (value is Component c) return c.gameObject;
            return null;
        }

        internal static string Path(Transform t)
        {
            string path = t.name;
            while (t.parent != null)
            {
                t = t.parent;
                path = t.name + "/" + path;
            }
            return path;
        }
    }
}
