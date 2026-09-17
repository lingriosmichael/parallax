namespace Parallax.Core
{
    public readonly struct EchoControlEvent
    {
        public readonly int TickOffset;
        public readonly ControlChannel Channel;
        public readonly ObserverId Target;
        public readonly float Value;

        public EchoControlEvent(int tickOffset, ControlChannel channel, ObserverId target, float value)
        {
            TickOffset = tickOffset;
            Channel = channel;
            Target = target;
            Value = value;
        }
    }
}
