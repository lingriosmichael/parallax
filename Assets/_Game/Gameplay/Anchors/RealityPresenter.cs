using Parallax.Core;
using Parallax.Gameplay.Transport;
using UnityEngine;

namespace Parallax.Gameplay.Anchors
{
    public sealed class RealityPresenter : MonoBehaviour
    {
        [SerializeField] AnchorDefinition definition;
        [SerializeField] TransportHost transportHost;
        [SerializeField] RealityManifestation[] manifestations;

        void OnEnable()
        {
            if (definition == null || transportHost == null || manifestations == null || manifestations.Length == 0)
            {
                Debug.LogError($"RealityPresenter '{gameObject.name}' is missing its definition, transport host, or manifestations.", this);
                return;
            }
            for (int i = 0; i < manifestations.Length; i++)
            {
                if (manifestations[i] == null)
                {
                    Debug.LogError($"RealityPresenter '{gameObject.name}' has a missing manifestation at index {i}.", this);
                    return;
                }
            }

            AnchorRegistry registry = transportHost.Registry;
            if (registry == null) return;
            registry.Register(definition.Id, definition.InitialValue);
            if (registry.TryGet(definition.Id, out AnchorState state)) Apply(state.Value, true);
            registry.Changed += OnChanged;
        }

        void OnDisable()
        {
            if (transportHost != null && transportHost.Registry != null)
                transportHost.Registry.Changed -= OnChanged;
        }

        void OnChanged(AnchorId id, AnchorState state)
        {
            if (definition != null && id.Equals(definition.Id)) Apply(state.Value, false);
        }

        void Apply(float value, bool snap)
        {
            for (int i = 0; i < manifestations.Length; i++) manifestations[i].SetTarget(value, snap);
        }
    }
}
