namespace Parallax.Core
{
    /// <summary>PAX-V07 §4: a kill source that says what kind of death it is (a trap or hazard component).</summary>
    public interface IDeathKindSource
    {
        CatDeathKind DeathKind { get; }
    }
}
