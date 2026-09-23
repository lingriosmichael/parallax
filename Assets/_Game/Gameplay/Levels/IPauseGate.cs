namespace Parallax.Gameplay.Levels
{
    /// <summary>PAX-054 (D-073): what a tick owner reads to honour the level pause.</summary>
    public interface IPauseGate
    {
        bool IsPaused { get; }
    }
}
