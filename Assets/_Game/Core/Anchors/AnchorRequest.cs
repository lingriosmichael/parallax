namespace Parallax.Core
{
    public readonly struct AnchorRequest
    {
        public readonly AnchorId Anchor;
        public readonly float TargetValue;   // ABSOLUTE. Never a delta.
        public readonly EventOrigin Origin;
        public readonly uint Sequence;       // per-origin counter -> event ID

        public AnchorRequest(AnchorId anchor, float targetValue, EventOrigin origin, uint sequence)
        {
            Anchor = anchor;
            TargetValue = targetValue;
            Origin = origin;
            Sequence = sequence;
        }
    }
}
