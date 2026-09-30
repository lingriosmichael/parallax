using Parallax.Core;
using Parallax.Gameplay.Observers;
using Parallax.Gameplay.Player;
using Parallax.Gameplay.Rooms;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Parallax.Gameplay.Presentation
{
    /// <summary>PAX-V07 §5: the per-frame signals the cat's animation reads besides the motor: this cat's death hold (and what
    /// killed it), its respawn, and level complete. Read-only. RoomDeath, RoomManager and the cat's ObserverContext are looked
    /// up in the cat's own scene on first use (§11 R1: no singleton, no scene wiring); CatRespawn is on the cat. Resets on
    /// OnDisable.</summary>
    [DisallowMultipleComponent]
    public sealed class CatPresentationSignals : MonoBehaviour
    {
        [SerializeField] CatMotor2D motor;

        RoomDeath death;
        RoomManager rooms;
        ObserverContext observer;
        CatRespawn respawn;
        bool looked;
        bool respawned;

        /// <summary>This cat's room is holding after this cat's death (§11 R3: never another cat's).</summary>
        public bool Holding => Find() && death != null && death.IsHolding && death.HoldObserver != null && death.HoldObserver.Cat == motor;
        /// <summary>The held death's kind (§4): the killer's declared kind, else Pit for a fall, else Default.</summary>
        public CatDeathKind HoldKind => Holding ? CatDeathKinds.Resolve(death.HoldKiller, death.HoldCause) : CatDeathKind.Default;
        /// <summary>The level completed, and this is the solo reality's cat (§11 R3).</summary>
        public bool LevelComplete => Find() && rooms != null && rooms.LevelComplete && observer != null && observer.Id == rooms.SoloReality;

        /// <summary>This cat respawned since the last call (CatRespawn.Respawned), then forgets it.</summary>
        public bool ConsumeRespawned()
        {
            bool r = respawned;
            respawned = false;
            return r;
        }

        void OnEnable()
        {
            respawn = motor != null ? motor.GetComponent<CatRespawn>() : null;
            if (respawn != null) respawn.Respawned += OnRespawned;
        }

        void OnDisable()
        {
            if (respawn != null) respawn.Respawned -= OnRespawned;
            respawn = null;
            death = null; rooms = null; observer = null;
            looked = false;
            respawned = false;
        }

        void OnRespawned() => respawned = true;

        // Once per enable, when the cat's scene is loaded: its RoomDeath and RoomManager (a level or Trap Lab has one each; the
        // frozen co-op sandbox's RoomDeath has no rooms), and the ObserverContext whose cat is this one.
        bool Find()
        {
            if (looked) return true;
            if (motor == null) return false;
            Scene scene = gameObject.scene;
            if (!scene.IsValid() || !scene.isLoaded) return false;
            looked = true;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (death == null) death = root.GetComponentInChildren<RoomDeath>(true);
                if (rooms == null) rooms = root.GetComponentInChildren<RoomManager>(true);
                if (observer == null)
                    foreach (ObserverContext o in root.GetComponentsInChildren<ObserverContext>(true))
                        if (o.Cat == motor) { observer = o; break; }
            }
            return true;
        }

        /// <summary>Item 2: the height against gravity of the face of `bounds` that faces against gravity (a floor's top).</summary>
        public static float FaceAgainstGravity(Bounds bounds, Vector2 down) =>
            -Vector2.Dot(bounds.center, down) + Mathf.Abs(Vector2.Dot(bounds.extents, down));

        /// <summary>Item 2: `paws` lie more than `tolerance` below the face against gravity of the solid whose bounds are
        /// `ground` (a grounded capsule rolling off that solid's corner draws its paws into it). Pure: reads only.</summary>
        public static bool SunkBelow(Bounds ground, Vector2 paws, Vector2 down, float tolerance) =>
            FaceAgainstGravity(ground, down) + Vector2.Dot(paws, down) > tolerance;
    }
}
