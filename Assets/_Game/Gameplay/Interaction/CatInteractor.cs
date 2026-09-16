using System.Collections.Generic;
using System;
using Parallax.Core;
using Parallax.Gameplay.Observers;
using Parallax.Gameplay.Reality;
using Parallax.Gameplay.Transport;
using UnityEngine;

namespace Parallax.Gameplay.Interaction
{
    [RequireComponent(typeof(Collider2D))]
    public sealed class CatInteractor : MonoBehaviour
    {
        [SerializeField] TransportHost transportHost;
        readonly List<Collider2D> overlaps = new List<Collider2D>();
        readonly Dictionary<EventOrigin, IAnchorRequester> requesters = new Dictionary<EventOrigin, IAnchorRequester>();
        readonly Dictionary<EventOrigin, IAnchorRequester> forwardingRequesters = new Dictionary<EventOrigin, IAnchorRequester>();
        Collider2D catCollider;

        public event Action<AnchorId, float> Requested;

        void Awake() => catCollider = GetComponent<Collider2D>();

        public void Step(in CatCommand command, ObserverContext observer)
        {
            if (!command.InteractPressed || observer == null || catCollider == null || transportHost == null) return;
            IRealityTransport transport = transportHost.Transport;
            EventSequencer sequencer = transportHost.Sequencer;
            if (transport == null || sequencer == null || observer.Reality == null) return;

            var filter = new ContactFilter2D { useTriggers = true };
            filter.SetLayerMask(observer.Reality.PhysicsMask);
            overlaps.Clear();
            catCollider.Overlap(filter, overlaps);

            IInteractable closest = null;
            float closestDistance = float.PositiveInfinity;
            Vector2 catPosition = catCollider.bounds.center;
            for (int i = 0; i < overlaps.Count; i++)
            {
                Collider2D candidate = overlaps[i];
                if (candidate == catCollider || candidate.GetComponentInParent<RealityRoot>() != observer.Reality) continue;
                IInteractable interactable = candidate.GetComponent<IInteractable>();
                if (interactable == null) continue;
                float distance = ((Vector2)candidate.bounds.center - catPosition).sqrMagnitude;
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closest = interactable;
                }
            }

            if (closest == null) return;
            EventOrigin origin = EventOrigins.Human(observer.Id);
            if (!requesters.TryGetValue(origin, out IAnchorRequester requester))
            {
                requester = new AnchorRequester(transport, sequencer, origin);
                requesters.Add(origin, requester);
            }
            if (!forwardingRequesters.TryGetValue(origin, out IAnchorRequester forwarding))
            {
                forwarding = new ForwardingRequester(this, requester);
                forwardingRequesters.Add(origin, forwarding);
            }
            closest.Interact(observer.Id, forwarding);
        }

        sealed class ForwardingRequester : IAnchorRequester
        {
            readonly IAnchorRequester inner;
            readonly CatInteractor owner;

            public EventOrigin Origin => inner.Origin;

            public ForwardingRequester(CatInteractor owner, IAnchorRequester inner)
            {
                this.owner = owner;
                this.inner = inner;
            }

            public void Request(AnchorId anchor, float targetValue)
            {
                inner.Request(anchor, targetValue);
                owner.Requested?.Invoke(anchor, targetValue);
            }
        }
    }
}
