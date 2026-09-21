namespace Parallax.Core
{
    public static class RoomPolicy
    {
        public static bool CompletesRoom(InputSourceKind kind) => kind == InputSourceKind.LocalHuman;
    }
}
