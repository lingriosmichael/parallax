namespace Parallax.Core
{
    // Append-only: values are never renumbered (PAX-V07 §2).
    public enum CatAnimState : byte
    {
        Idle,
        Walk,
        Rise,
        Fall,
        Land,
        Climb,      // PAX-087 (D-089): appended; shown with the Idle frames until the art ticket's climb sheet
        // PAX-V07 §2: appended in this order.
        Run,
        Turn,
        TakeOff,
        Apex,
        HardLand,
        Death,
        Respawn,
        Flip,
        Hang,
        Leap,
        Door,
        IdleFidget,
        // PAX-105 (D-110): appended in this order.
        WallCling,
        WallSlide,
        WallJump,
    }
}
