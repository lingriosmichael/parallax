namespace Parallax.Core
{
    public struct SpectacleCue
    {
        public ushort CueId;
        public int StartTick;
        public uint Seed;

        public SpectacleCue(ushort cueId, int startTick, uint seed)
        {
            CueId = cueId;
            StartTick = startTick;
            Seed = seed;
        }
    }
}
