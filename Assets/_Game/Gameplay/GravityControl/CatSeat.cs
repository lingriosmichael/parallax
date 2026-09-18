using Parallax.Core;
using UnityEngine;

namespace Parallax.Gameplay.GravityControl
{
    public sealed class CatSeat : MonoBehaviour
    {
        SeatState state;

        public ControlStation Station => state != null ? state.Station as ControlStation : null;
        public bool IsSeated => state != null && ReferenceEquals(state.Occupant, this);

        internal void SitAt(SeatState nextState)
        {
            state = nextState;
            state.Released += OnReleased;
        }

        public void Release()
        {
            if (!IsSeated) return;
            state.Release();
        }

        void OnDisable() => Release();
        void OnDestroy() => Release();

        void OnReleased()
        {
            state.Released -= OnReleased;
            state = null;
        }
    }
}
