namespace Parallax.Core
{
    public struct ControlSample
    {
        public ControlChannel Channel;
        public ObserverId Target;
        public float Value;   // normalized -1..1
        public int Tick;

        public ControlSample(ControlChannel channel, ObserverId target, float value, int tick)
        {
            Channel = channel;
            Target = target;
            Value = value;
            Tick = tick;
        }
    }
}
