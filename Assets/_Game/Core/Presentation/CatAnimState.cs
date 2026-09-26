namespace Parallax.Core
{
    public enum CatAnimState : byte
    {
        Idle,
        Walk,
        Rise,
        Fall,
        Land,
        Climb,      // PAX-087 (D-089): appended; shown with the Idle frames until the art ticket's climb sheet
    }
}
