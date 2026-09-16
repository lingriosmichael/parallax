using System;

namespace Parallax.Core
{
    public readonly struct AnchorId : IEquatable<AnchorId>
    {
        public readonly ushort Value;

        public AnchorId(ushort value)
        {
            Value = value;
        }

        public bool Equals(AnchorId other) => Value == other.Value;
        public override bool Equals(object obj) => obj is AnchorId other && Equals(other);
        public override int GetHashCode() => Value;
        public override string ToString() => $"Anchor#{Value}";

        public static bool operator ==(AnchorId a, AnchorId b) => a.Equals(b);
        public static bool operator !=(AnchorId a, AnchorId b) => !a.Equals(b);
    }
}
