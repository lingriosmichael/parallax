namespace Parallax.Core
{
    public struct AnchorState
    {
        public float Value;    // logical target, usually 0..1; meaning defined per anchor
        public uint Revision;  // incremented by the authority on every commit
    }
}
