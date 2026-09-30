using UnityEngine;

namespace Parallax.Gameplay.Presentation
{
    /// <summary>PAX-V07 §5 (skeleton; items 2+ fill it): the per-frame signals the cat's animation reads besides the motor:
    /// this cat's death hold, its respawn, level complete and a gravity flip. It will subscribe to RoomDeath, CatRespawn,
    /// RoomManager.LevelCompleted and GravityReceiver (found in the cat's own scene, §11 R1), expose plain read-only values,
    /// and reset on OnDisable. Item 1 (the ground states) needs none of them, so nothing reads it yet and the setup menu
    /// doesn't add it to the prefab.</summary>
    [DisallowMultipleComponent]
    public sealed class CatPresentationSignals : MonoBehaviour
    {
        /// <summary>This cat's room is holding after its death (item 6).</summary>
        public bool Holding { get; private set; }
        /// <summary>This cat respawned on this frame (item 6).</summary>
        public bool RespawnedThisFrame { get; private set; }
        /// <summary>The level completed for this cat's observer (item 7).</summary>
        public bool LevelComplete { get; private set; }

        void OnDisable()
        {
            Holding = false;
            RespawnedThisFrame = false;
            LevelComplete = false;
        }
    }
}
