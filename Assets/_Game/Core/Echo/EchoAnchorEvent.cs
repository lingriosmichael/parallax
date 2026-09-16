namespace Parallax.Core
{
    public readonly struct EchoAnchorEvent
    {
        public readonly int TickOffset;
        public readonly AnchorId Anchor;
        public readonly float TargetValue;

        public EchoAnchorEvent(int tickOffset, AnchorId anchor, float targetValue)
        {
            TickOffset = tickOffset;
            Anchor = anchor;
            TargetValue = targetValue;
        }
    }
}
