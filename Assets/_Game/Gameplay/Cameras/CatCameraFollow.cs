using Parallax.Core.Cameras;
using Parallax.Gameplay.Player;
using UnityEngine;

namespace Parallax.Gameplay.Cameras
{
    [RequireComponent(typeof(Camera))]
    public sealed class CatCameraFollow : MonoBehaviour
    {
        [SerializeField] Transform target;
        [SerializeField] Vector2 deadZoneHalfExtents = new Vector2(2.0f, 1.6f);
        [SerializeField] float smoothTime = 0.18f;
        [SerializeField] float maxSpeed = 40f;
        [SerializeField] bool useBounds = false;
        [SerializeField] Vector2 boundsMin = Vector2.zero;
        [SerializeField] Vector2 boundsMax = Vector2.zero;
        [SerializeField] CatRespawn respawn;

        Camera cam;
        Vector2 velocity;

        Vector2 HalfView => new Vector2(cam.orthographicSize * cam.aspect, cam.orthographicSize);

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
        }

        void Start()
        {
            SnapToTarget();
        }

        void LateUpdate()
        {
            if (target == null) return;

            Vector2 currentCentre = transform.position;
            Vector2 targetPos = target.position;

            Vector2 desired = CameraMath.ResolveDeadZone(currentCentre, targetPos, deadZoneHalfExtents);
            if (useBounds) desired = CameraMath.ClampToBounds(desired, HalfView, boundsMin, boundsMax);

            Vector2 smoothed = Vector2.SmoothDamp(currentCentre, desired, ref velocity, smoothTime, maxSpeed, Time.deltaTime);

            transform.position = new Vector3(smoothed.x, smoothed.y, transform.position.z);
        }

        public void SnapToTarget()
        {
            if (target == null) return;

            Vector2 pos = target.position;
            if (useBounds) pos = CameraMath.ClampToBounds(pos, HalfView, boundsMin, boundsMax);

            transform.position = new Vector3(pos.x, pos.y, transform.position.z);
            velocity = Vector2.zero;
        }

        void OnDrawGizmosSelected()
        {
            Vector3 centre = transform.position;

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(centre, new Vector3(deadZoneHalfExtents.x * 2f, deadZoneHalfExtents.y * 2f, 0f));

            if (useBounds)
            {
                Vector3 boundsCentre = new Vector3((boundsMin.x + boundsMax.x) * 0.5f, (boundsMin.y + boundsMax.y) * 0.5f, 0f);
                Vector3 boundsSize = new Vector3(boundsMax.x - boundsMin.x, boundsMax.y - boundsMin.y, 0f);

                Gizmos.color = Color.cyan;
                Gizmos.DrawWireCube(boundsCentre, boundsSize);
            }
        }
    }
}
