namespace Parallax.Core
{
    public sealed class SensorLatch
    {
        bool latched;

        public SensorLatch(float initialValue)
        {
            latched = initialValue >= 0.5f;
        }

        public bool Update(bool occupied, out float target)
        {
            target = occupied ? 1f : 0f;
            if (occupied == latched) return false;
            latched = occupied;
            return true;
        }
    }
}
