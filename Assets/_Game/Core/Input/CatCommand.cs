namespace Parallax.Core
{
    public struct CatCommand
    {
        public float Move;          // -1..1 along the cat's local "right" (perpendicular to gravity)
        public float Climb;         // PAX-087 (D-089): -1..1, screen-up positive, never gravity-relative or inverted
        public bool  JumpPressed;   // edge: pressed this tick
        public bool  JumpHeld;
        public bool  InteractPressed;
        public bool  InteractHeld;
        public bool  WhiskerPressed; // D-324 (PAX-129): edge: the whisker dodge pressed this tick

        public static CatCommand None => default;
    }
}
