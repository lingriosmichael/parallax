using Parallax.Core;
using Parallax.Gameplay.Anchors;
using UnityEngine;

namespace Parallax.Gameplay.Interaction
{
    public sealed class VineInteractable : MonoBehaviour, IInteractable
    {
        [SerializeField] AnchorDefinition definition;
        [SerializeField] SpriteRenderer indicator;
        AnchorToggle toggle;
        Color initialColor;
        float pulseUntil;

        void Awake()
        {
            if (definition == null) Debug.LogError($"VineInteractable '{gameObject.name}' has no AnchorDefinition assigned.", this);
            else toggle = new AnchorToggle(definition.InitialValue);
            if (indicator != null) initialColor = indicator.color;
        }

        public void Interact(ObserverId observer, IAnchorRequester requester)
        {
            if (definition == null || toggle == null || requester == null) return;
            requester.Request(definition.Id, toggle.Next());
            pulseUntil = Time.time + 0.15f;
        }

        void Update()
        {
            if (indicator == null) return;
            float t = Mathf.Clamp01((pulseUntil - Time.time) / 0.15f);
            indicator.color = Color.Lerp(initialColor, Color.white, t);
        }
    }
}
