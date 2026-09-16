namespace Parallax.Core
{
    public interface ICatCommandSource
    {
        /// Returns the command for this tick. Implementations MUST clear latched
        /// press-edges (JumpPressed, InteractPressed) as part of this call, so a
        /// single physical press is reported exactly once.
        CatCommand Read();

        /// Clears latched edges, current move, stick vector, and finger/key state —
        /// anything that must not survive a driver handover.
        void ResetTransientState();
    }
}
