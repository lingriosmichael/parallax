using System;
using System.Collections.Generic;

namespace Parallax.Core
{
    public sealed class AnchorRegistry
    {
        readonly List<AnchorId> order = new List<AnchorId>();
        readonly Dictionary<AnchorId, AnchorState> states = new Dictionary<AnchorId, AnchorState>();
        readonly Dictionary<EventOrigin, uint> lastAppliedSequence = new Dictionary<EventOrigin, uint>();

        public event Action<AnchorId, AnchorState> Changed;

        public bool Register(AnchorId id, float initialValue)
        {
            if (states.ContainsKey(id)) return false;

            states[id] = new AnchorState { Value = initialValue, Revision = 0 };
            order.Add(id);
            return true;
        }

        public bool TryGet(AnchorId id, out AnchorState state) => states.TryGetValue(id, out state);

        // Authority side.
        public CommitResult Commit(in AnchorRequest request)
        {
            if (!states.TryGetValue(request.Anchor, out AnchorState state)) return CommitResult.UnknownAnchor;

            if (float.IsNaN(request.TargetValue) || float.IsInfinity(request.TargetValue)) return CommitResult.InvalidValue;

            uint lastSequence = lastAppliedSequence.TryGetValue(request.Origin, out uint last) ? last : 0;
            if (request.Sequence <= lastSequence) return CommitResult.Duplicate;

            lastAppliedSequence[request.Origin] = request.Sequence;

            if (state.Value == request.TargetValue) return CommitResult.NoChange;

            state.Value = request.TargetValue;
            state.Revision++;
            states[request.Anchor] = state;
            Changed?.Invoke(request.Anchor, state);
            return CommitResult.Applied;
        }

        // Non-authority side.
        public void ApplyReplicated(AnchorId id, AnchorState state)
        {
            if (states.TryGetValue(id, out AnchorState current))
            {
                if (state.Revision <= current.Revision) return;

                states[id] = state;
                Changed?.Invoke(id, state);
                return;
            }

            states[id] = state;
            order.Add(id);
            Changed?.Invoke(id, state);
        }

        public IEnumerable<KeyValuePair<AnchorId, AnchorState>> All
        {
            get
            {
                foreach (AnchorId id in order)
                {
                    yield return new KeyValuePair<AnchorId, AnchorState>(id, states[id]);
                }
            }
        }

        // Must be called whenever EventSequencers are recreated (checkpoint/level reload), or
        // every request from a restarted sequencer is dropped as Duplicate.
        public void ResetSequences()
        {
            lastAppliedSequence.Clear();
        }
    }
}
