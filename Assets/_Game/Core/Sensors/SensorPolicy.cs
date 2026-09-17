namespace Parallax.Core
{
    public static class SensorPolicy
    {
        public static bool Counts(InputSourceKind kind) =>
            kind == InputSourceKind.LocalHuman || kind == InputSourceKind.EchoReplay;
    }
}
