using Parallax.Core;
using UnityEngine;

namespace Parallax.Gameplay.Rooms
{
    public sealed class RoomAnchorBinding : MonoBehaviour
    {
        [SerializeField] int roomId;
        [SerializeField] ushort anchorId;
        [SerializeField] float initialValue;
        [SerializeField] RoomDeath roomDeath;
        bool bound;

        void OnEnable()
        {
            if (roomDeath == null)
            {
                Debug.LogError($"RoomAnchorBinding '{name}' has no RoomDeath assigned.", this);
                return;
            }
            if (bound) return;
            AnchorId id = new AnchorId(anchorId);
            if (!roomDeath.ResetRegistry.RegisterAnchor(roomId, id, initialValue))
                Debug.LogError($"RoomAnchorBinding '{name}' could not bind anchor {id} to room {roomId}; it is already bound.", this);
            else bound = true;
        }
    }
}
