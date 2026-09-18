using System;
using System.Collections.Generic;
using Parallax.Core;
using UnityEngine;

namespace Parallax.Gameplay.Transport
{
    // Pure C# — behaves like a network (never synchronous, configurable delay)
    // while running entirely on one device. Always the session authority.
    public sealed class LocalTransport : IRealityTransport
    {
        enum ItemKind { Anchor, Control, Cue }

        struct PendingItem
        {
            public ItemKind Kind;
            public int DeliveryTick;
            public AnchorRequest AnchorRequest;
            public ControlSample ControlSample;
            public SpectacleCue Cue;
        }

        readonly AnchorRegistry registry;
        readonly float fixedDeltaSeconds;
        readonly Func<int> currentTick;
        readonly List<PendingItem> pending = new List<PendingItem>();

        float latencyMs;

        public LocalTransport(AnchorRegistry registry, float fixedDeltaSeconds, Func<int> currentTick)
        {
            this.registry = registry ?? throw new ArgumentNullException(nameof(registry));
            this.fixedDeltaSeconds = fixedDeltaSeconds;
            this.currentTick = currentTick ?? throw new ArgumentNullException(nameof(currentTick));
        }

        public bool IsSessionAuthority => true;
        public int Tick => currentTick();

        public float LatencyMs
        {
            get => latencyMs;
            set => latencyMs = Mathf.Clamp(value, 0f, 400f);
        }

        public int PendingCount => pending.Count;
        public CommitResult LastCommitResult { get; private set; }

        public bool TryGetPendingAnchorTarget(AnchorId anchor, out float target)
        {
            for (int i = pending.Count - 1; i >= 0; i--)
            {
                PendingItem item = pending[i];
                if (item.Kind == ItemKind.Anchor && item.AnchorRequest.Anchor == anchor)
                {
                    target = item.AnchorRequest.TargetValue;
                    return true;
                }
            }

            target = default;
            return false;
        }

        public event Action<ControlSample> ControlReceived;
        public event Action<SpectacleCue> CueReceived;

        public void RequestAnchor(in AnchorRequest request)
        {
            pending.Add(new PendingItem
            {
                Kind = ItemKind.Anchor,
                DeliveryTick = ComputeDeliveryTick(),
                AnchorRequest = request,
            });
        }

        public void PublishControl(in ControlSample sample)
        {
            pending.Add(new PendingItem
            {
                Kind = ItemKind.Control,
                DeliveryTick = ComputeDeliveryTick(),
                ControlSample = sample,
            });
        }

        public void SendCue(in SpectacleCue cue)
        {
            pending.Add(new PendingItem
            {
                Kind = ItemKind.Cue,
                DeliveryTick = ComputeDeliveryTick(),
                Cue = cue,
            });
        }

        int ComputeDeliveryTick()
        {
            int delayTicks = Mathf.Max(1, Mathf.CeilToInt(latencyMs / (fixedDeltaSeconds * 1000f)));
            return currentTick() + delayTicks;
        }

        public void Pump(int tick)
        {
            if (pending.Count == 0) return;

            List<PendingItem> remaining = null;
            for (int i = 0; i < pending.Count; i++)
            {
                PendingItem item = pending[i];
                if (item.DeliveryTick <= tick)
                {
                    Deliver(item);
                }
                else
                {
                    remaining ??= new List<PendingItem>(pending.Count);
                    remaining.Add(item);
                }
            }

            pending.Clear();
            if (remaining != null) pending.AddRange(remaining);
        }

        void Deliver(in PendingItem item)
        {
            switch (item.Kind)
            {
                case ItemKind.Anchor:
                    LastCommitResult = registry.Commit(item.AnchorRequest);
                    break;
                case ItemKind.Control:
                    ControlReceived?.Invoke(item.ControlSample);
                    break;
                case ItemKind.Cue:
                    CueReceived?.Invoke(item.Cue);
                    break;
            }
        }
    }
}
