namespace Parallax.Core
{
    /// <summary>PAX-V07 §4: what killed the cat, for its death clip. Append-only (Splash comes with a water hazard).</summary>
    public enum CatDeathKind
    {
        Default,
        Pit,
        Spiked,
        Crushed,
        Zapped,
        Arrow,
    }

    public static class CatDeathKinds
    {
        /// <summary>PAX-V07 §4: a killer that declares a kind gives it; otherwise a Fall or OutOfBounds cause is a Pit; otherwise
        /// Default (the frightened pose). Only the interface is checked, never a trap class.</summary>
        public static CatDeathKind Resolve(object killer, DeathCause cause)
        {
            if (killer is IDeathKindSource source) return source.DeathKind;
            return cause == DeathCause.Fall || cause == DeathCause.OutOfBounds ? CatDeathKind.Pit : CatDeathKind.Default;
        }
    }
}
