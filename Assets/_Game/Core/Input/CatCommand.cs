namespace Parallax.Core
{
    public struct CatCommand
    {
        public float Move;          // -1..1 along the cat's local "right" (perpendicular to gravity)
        public bool  JumpPressed;   // edge: pressed this tick
        public bool  JumpHeld;
        public bool  InteractPressed;
        public bool  InteractHeld;

        public static CatCommand None => default;
    }
}
