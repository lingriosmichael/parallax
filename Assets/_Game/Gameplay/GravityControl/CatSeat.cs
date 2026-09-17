using UnityEngine;

namespace Parallax.Gameplay.GravityControl
{
    public sealed class CatSeat : MonoBehaviour
    {
        public ControlStation Station { get; private set; }
        public bool IsSeated => Station != null;
        public void SitAt(ControlStation station) { Station = station; }
        public void Release()
        {
            ControlStation station = Station;
            if (station == null) return;
            Station = null;
            station.OnReleased(this);
        }
    }
}
