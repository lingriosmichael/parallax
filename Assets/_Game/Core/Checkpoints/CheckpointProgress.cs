namespace Parallax.Core
{
    public sealed class CheckpointProgress
    {
        public int Current { get; private set; }

        public bool TryAdvance(int id)
        {
            if (id <= Current) return false;
            Current = id;
            return true;
        }

        public void ResetTo(int id)
        {
            Current = id;
        }
    }
}
