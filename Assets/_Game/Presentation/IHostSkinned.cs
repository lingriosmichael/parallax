namespace Parallax.Presentation
{
    /// <summary>PAX-A13 (§3.1, §12 R8): an art that wears its host's skin until its reveal. Reskin points it at a new host
    /// skin (a level rebuild does this through the builder; the contact sheet and the reveal-frame test do it in place).</summary>
    public interface IHostSkinned
    {
        HostSkin HostSkin { get; }
        void Reskin(HostSkin skin);
    }
}
