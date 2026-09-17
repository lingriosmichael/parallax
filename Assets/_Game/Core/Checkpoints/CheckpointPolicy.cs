namespace Parallax.Core
{
    public static class CheckpointPolicy
    {
        public static bool Activates(InputSourceKind kind) => kind == InputSourceKind.LocalHuman;

        public static bool FallResets(InputSourceKind kind) => kind != InputSourceKind.EchoReplay;
    }
}
