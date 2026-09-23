using Parallax.Core.Cameras;
using Parallax.Gameplay.Player;
using Parallax.Gameplay.Rooms;
using UnityEngine;

namespace Parallax.Gameplay.Cameras
{
    // PAX-052 (D-071): the level camera. A new component, not an extension of CatCameraFollow -
    // CatCameraFollow is exercised by Sandbox_Realities/Level_Solo01 (frozen-adjacent, D-047) and
    // must not change. Reads only the frame baked by LevelSetup at regeneration time (never a
    // runtime layout read, D-066); the frame is the room's content bounds plus ViewMargin, not
    // RoomManager's own (differently-margined) kill bounds.
    [RequireComponent(typeof(Camera))]
    public sealed class LevelCameraFollow : MonoBehaviour
    {
        [SerializeField] Transform target;
        [SerializeField] RoomDeath roomDeath;
        [SerializeField] CatRespawn respawn;
        [SerializeField] LevelCameraConfig config;

        // Baked by LevelSetup/LevelCameraBuilder from SoloRoomBuilder.ComputeRoomBounds (this
        // level's one room, ViewMargin), the same pattern D-059 already uses for RoomManager.bounds.
        [SerializeField] Vector2 frameCenter;
        [SerializeField] Vector2 frameSize;

        Camera cam;
        Vector2 velocity;
        float lastTargetX;
        float lastDirection;

        void Awake()
        {
            cam = GetComponent<Camera>();
        }

        void OnEnable()
        {
            if (respawn != null) respawn.Respawned += SnapToTarget;
        }

        void OnDisable()
        {
            if (respawn != null) respawn.Respawned -= SnapToTarget;
            // Latched follow state must not survive a disable/enable cycle, same as any other
            // presentation-derived state in this codebase (PAX-049 class of bug): a re-enabled
            // camera must not resume with a stale look-ahead direction or SmoothDamp velocity.
            velocity = Vector2.zero;
            lastDirection = 0f;
            lastTargetX = target != null ? target.position.x : 0f;
        }

        void Start()
        {
            if (target != null) lastTargetX = target.position.x;
            SnapToTarget();
        }

        void LateUpdate() => Step();

        // Also callable directly (tests, or any future manual re-sync) - identical to what
        // LateUpdate runs every frame.
        public void Step()
        {
            if (target == null || config == null) return;
            if (cam == null) cam = GetComponent<Camera>();
            if (cam == null) return;
            // PAX-047 (D-058)/§2.2.3: still camera during the death hold - RoomManager gates its
            // whole tick on the same flag, this gates the whole position/size update the same way.
            if (roomDeath != null && roomDeath.IsHolding) return;

            transform.position = ToVector3(Resolve(transform.position, immediate: false));
            lastTargetX = target.position.x;
        }

        public void SnapToTarget()
        {
            if (target == null || config == null) return;
            if (cam == null) cam = GetComponent<Camera>();
            if (cam == null) return;
            transform.position = ToVector3(Resolve(transform.position, immediate: true));
            velocity = Vector2.zero;
            lastTargetX = target.position.x;
        }

        Vector3 ToVector3(Vector2 xy) => new Vector3(xy.x, xy.y, transform.position.z);

        Vector2 Resolve(Vector2 currentCentre, bool immediate)
        {
            float viewHeight = CameraMath.ResolveViewHeight(frameSize, config.MaxViewHeight, cam.aspect);
            cam.orthographicSize = viewHeight * 0.5f;
            Vector2 halfView = new Vector2(viewHeight * 0.5f * cam.aspect, viewHeight * 0.5f);
            Vector2 frameMin = frameCenter - frameSize * 0.5f;
            Vector2 frameMax = frameCenter + frameSize * 0.5f;

            Vector2 desired;
            if (CameraMath.IsFitMode(frameSize, config.MaxViewHeight, cam.aspect))
            {
                desired = frameCenter;
            }
            else
            {
                Vector2 targetPos = target.position;
                float dx = targetPos.x - lastTargetX;
                float direction = dx > 0f ? 1f : (dx < 0f ? -1f : lastDirection);
                lastDirection = direction;
                bool verticalFollow = frameSize.y > viewHeight + 0.001f;

                desired = CameraMath.ResolveFollowCentre(currentCentre, targetPos, direction,
                    config.DeadZoneHalfExtents, config.LookAhead, halfView, frameCenter, frameMin, frameMax, verticalFollow);
            }

            if (immediate) return desired;
            return Vector2.SmoothDamp(currentCentre, desired, ref velocity, config.SmoothTime, config.MaxSpeed, Time.deltaTime);
        }
    }
}
