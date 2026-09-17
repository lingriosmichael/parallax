using Parallax.Core;
using Parallax.Gameplay.GravityControl;
using Parallax.Gameplay.Observers;
using Parallax.Gameplay.Player;
using Parallax.Gameplay.Reality;
using Parallax.Gameplay.Transport;
using UnityEditor;
using UnityEngine;

namespace Parallax.Editor.Setup
{
    public static class ControlValidator
    {
        [MenuItem("PARALLAX/Validate/Control")]
        public static int Validate()
        {
            int problems = 0;
            foreach (ControlStation station in Object.FindObjectsByType<ControlStation>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                RealityRoot root = station.GetComponentInParent<RealityRoot>();
                var serialized = new SerializedObject(station);
                MonoBehaviour input = serialized.FindProperty("inputSource").objectReferenceValue as MonoBehaviour;
                Collider2D collider = station.GetComponent<Collider2D>();
                if (root != null && collider != null && collider.isTrigger && input is IGravityControlInput && station.gameObject.layer == LayerMask.NameToLayer(RealitySpace.PhysicsLayerName(root.Id))) continue;
                Debug.LogError($"Control: invalid station '{station.name}'.", station);
                problems++;
            }
            foreach (RealityRoot root in Object.FindObjectsByType<RealityRoot>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            foreach (CatMotor2D cat in root.GetComponentsInChildren<CatMotor2D>(true))
            {
                GravityControlReceiver receiver = cat.GetComponent<GravityControlReceiver>();
                TransportHost host = receiver == null ? null : new SerializedObject(receiver).FindProperty("transportHost").objectReferenceValue as TransportHost;
                if (cat.GetComponent<CatSeat>() != null && receiver != null && host != null) continue;
                Debug.LogError($"Control: cat '{cat.name}' lacks CatSeat or GravityControlReceiver host.", cat);
                problems++;
            }
            if (problems == 0) Debug.Log("Control: OK");
            return problems;
        }
    }
}
