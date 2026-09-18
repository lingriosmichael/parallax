using System;

namespace Parallax.Core
{
    // Shared logical ownership for one cat/station pairing. It deliberately has no Unity dependency.
    public sealed class SeatState
    {
        public object Occupant { get; private set; }
        public object Station { get; private set; }
        public bool IsOccupied => Occupant != null;

        public event Action Released;

        public bool TryOccupy(object occupant, object station)
        {
            if (occupant == null) throw new ArgumentNullException(nameof(occupant));
            if (station == null) throw new ArgumentNullException(nameof(station));
            if (IsOccupied) return false;

            Occupant = occupant;
            Station = station;
            return true;
        }

        public bool Release()
        {
            if (!IsOccupied) return false;

            Occupant = null;
            Station = null;
            Released?.Invoke();
            return true;
        }
    }
}
