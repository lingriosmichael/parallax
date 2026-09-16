namespace Parallax.Core
{
    public sealed class AnchorToggle
    {
        public float LastRequested { get; private set; }

        public AnchorToggle(float initialValue) => LastRequested = initialValue;

        public float Next()
        {
            LastRequested = LastRequested >= 0.5f ? 0f : 1f;
            return LastRequested;
        }
    }
}
