namespace Parallax.Core
{
    public static class SeatCommandFilter
    {
        public static CatCommand Apply(CatCommand command)
        {
            command.Move = 0f;
            command.JumpPressed = false;
            command.JumpHeld = false;
            return command;
        }
    }
}
