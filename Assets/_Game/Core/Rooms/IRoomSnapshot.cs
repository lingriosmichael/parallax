using UnityEngine;

namespace Parallax.Core
{
    /// <summary>PAX-090 (D-091): one element's runtime state, by value: its TrapState, its timing, the flip countdown, and one
    /// int, one float and one Vector3 for the few extras a kind has (a geyser's phase, a cloud's x, a retreated door's pose,
    /// an inverter's fire tick, a snapped vine). Plain fields and no references, so it compares by value.</summary>
    public struct TrapSnapshot
    {
        public TrapState State;
        public TrapTimingState Timing;
        public TrapCountdownSnapshot Countdown;
        public int ExtraInt;
        public float ExtraFloat;
        public Vector3 ExtraVector;
    }

    /// <summary>PAX-090 (D-091): an element a checkpoint section can snapshot at its gate and restore after a death. Restore
    /// runs with the room clock already back at the gate tick (roomTick), after the room's ordinary reset.</summary>
    public interface IRoomSnapshot
    {
        int RoomId { get; }
        TrapSnapshot Capture();
        void Restore(in TrapSnapshot snapshot, int roomTick);
    }
}
