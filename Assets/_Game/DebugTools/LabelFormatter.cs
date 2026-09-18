using Parallax.Core;

namespace Parallax.DebugTools
{
    public static class LabelFormatter
    {
        public static string AnchorTag(AnchorId anchor) => $"K{anchor.Value}";

        public static string Cat(ObserverId id, InputSourceKind driver, bool seated, float echoSeconds, bool echoHolding)
        {
            string state = driver switch
            {
                InputSourceKind.LocalHuman => "LIVE",
                InputSourceKind.EchoReplay => echoHolding ? "ECHO HOLD" : $"ECHO {echoSeconds:F1}s",
                _ => "IDLE",
            };
            return $"{id} · {state}" + (seated ? " · seated" : string.Empty);
        }

        public static string Anchor(string tag, float value, bool pending) => pending ? $"[{tag}] →{value:F0}" : $"[{tag}] {value:F0}";
        public static string Plate(string tag, bool pressed, bool echo) => $"[{tag}] " + (pressed ? echo ? "pressed (echo)" : "pressed" : "free");
        public static string Gate(string tag, float value) => $"[{tag}] " + (value > 0.5f ? "open" : "closed");
        public static string Station(bool occupied, ObserverId occupant, float value) => occupied ? $"station: {occupant} {value:F2}" : "station: empty";
    }
}
