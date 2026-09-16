using System.Collections.Generic;
using Parallax.Core;
using Parallax.Gameplay.Anchors;
using Parallax.Gameplay.Interaction;
using Parallax.Gameplay.Player;
using Parallax.Gameplay.Reality;
using Parallax.Gameplay.Transport;
using UnityEditor;
using UnityEngine;

namespace Parallax.Editor.Setup
{
    public static class AnchorValidator
    {
        [MenuItem("PARALLAX/Validate/Anchors")]
        public static int Validate()
        {
            int problems = ValidateDefinitions();
            var presenterIds = new HashSet<ushort>();
            var manifestationCounts = new Dictionary<RealityManifestation, int>();

            foreach (RealityPresenter presenter in Object.FindObjectsByType<RealityPresenter>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                problems += ValidatePresenter(presenter, presenterIds, manifestationCounts);

            foreach (KeyValuePair<RealityManifestation, int> entry in manifestationCounts)
            {
                if (entry.Value == 1) continue;
                Debug.LogError($"Anchors: manifestation '{entry.Key.name}' is referenced {entry.Value} times.", entry.Key);
                problems++;
            }

            foreach (VineInteractable vine in Object.FindObjectsByType<VineInteractable>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                problems += ValidateVine(vine, presenterIds);

            foreach (RealityRoot root in Object.FindObjectsByType<RealityRoot>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            foreach (CatMotor2D cat in root.GetComponentsInChildren<CatMotor2D>(true))
                problems += ValidateCat(cat);

            if (problems == 0) Debug.Log("Anchors: OK");
            return problems;
        }

        static int ValidateDefinitions()
        {
            int problems = 0;
            var ids = new HashSet<ushort>();
            foreach (string guid in AssetDatabase.FindAssets("t:AnchorDefinition"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                AnchorDefinition definition = AssetDatabase.LoadAssetAtPath<AnchorDefinition>(path);
                if (definition != null && definition.Id.Value != 0 && definition.Id.Value != 65535 && ids.Add(definition.Id.Value)) continue;
                Debug.LogError($"Anchors: invalid or duplicate definition '{(definition == null ? path : definition.name)}'.");
                problems++;
            }
            return problems;
        }

        static int ValidatePresenter(RealityPresenter presenter, HashSet<ushort> ids, Dictionary<RealityManifestation, int> counts)
        {
            SerializedObject serialized = new SerializedObject(presenter);
            AnchorDefinition definition = serialized.FindProperty("definition").objectReferenceValue as AnchorDefinition;
            TransportHost host = serialized.FindProperty("transportHost").objectReferenceValue as TransportHost;
            SerializedProperty manifestations = serialized.FindProperty("manifestations");
            if (definition == null || host == null || manifestations.arraySize == 0 || presenter.GetComponentInParent<RealityRoot>() != null || !ids.Add(definition != null ? definition.Id.Value : (ushort)0))
            {
                Debug.LogError($"Anchors: invalid presenter '{presenter.name}'.", presenter);
                return 1;
            }

            int problems = 0;
            for (int i = 0; i < manifestations.arraySize; i++)
            {
                RealityManifestation manifestation = manifestations.GetArrayElementAtIndex(i).objectReferenceValue as RealityManifestation;
                if (manifestation == null || manifestation.GetComponentInParent<RealityRoot>() == null)
                {
                    Debug.LogError($"Anchors: invalid manifestation on '{presenter.name}'.", presenter);
                    problems++;
                    continue;
                }
                counts[manifestation] = counts.TryGetValue(manifestation, out int count) ? count + 1 : 1;
            }
            return problems;
        }

        static int ValidateVine(VineInteractable vine, HashSet<ushort> presenterIds)
        {
            SerializedObject serialized = new SerializedObject(vine);
            AnchorDefinition definition = serialized.FindProperty("definition").objectReferenceValue as AnchorDefinition;
            RealityRoot root = vine.GetComponentInParent<RealityRoot>();
            Collider2D collider = vine.GetComponent<Collider2D>();
            bool valid = definition != null
                         && root != null
                         && collider != null
                         && collider.isTrigger
                         && vine.gameObject.layer == LayerMask.NameToLayer(RealitySpace.PhysicsLayerName(root.Id))
                         && presenterIds.Contains(definition.Id.Value);
            if (valid) return 0;
            Debug.LogError($"Anchors: invalid vine interactable '{vine.name}'.", vine);
            return 1;
        }

        static int ValidateCat(CatMotor2D cat)
        {
            CatInteractor interactor = cat.GetComponent<CatInteractor>();
            TransportHost host = interactor == null ? null : new SerializedObject(interactor).FindProperty("transportHost").objectReferenceValue as TransportHost;
            if (interactor != null && host != null) return 0;
            Debug.LogError($"Anchors: cat '{cat.name}' has no CatInteractor with a host.", cat);
            return 1;
        }
    }
}
