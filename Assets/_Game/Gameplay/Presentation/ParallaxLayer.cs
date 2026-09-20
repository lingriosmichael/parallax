using Parallax.Core;
using Parallax.Core.Presentation;
using Parallax.Gameplay.Reality;
using UnityEngine;

namespace Parallax.Gameplay.Presentation
{
    /// <summary>Presentation-only, camera-relative placement for one tiled background strip.</summary>
    [DefaultExecutionOrder(100)]
    public sealed class ParallaxLayer : MonoBehaviour
    {
        [SerializeField] Camera realityCamera;
        [SerializeField] float screenSpeed = 0.05f;
        [SerializeField] float tileWidth = 1f;
        [SerializeField] float tileOffsetY;
        [SerializeField] Transform[] tiles;

        RealityRoot realityRoot;

        void Awake()
        {
            realityRoot = GetComponentInParent<RealityRoot>();
            if (realityRoot == null)
                Debug.LogError($"ParallaxLayer '{name}' needs a RealityRoot parent.", this);
        }

        void LateUpdate()
        {
            if (realityCamera == null || !realityCamera.isActiveAndEnabled || realityRoot == null)
            {
                return;
            }

            // The calculation is inherently a snap: it never reuses a stale camera position.
            Vector2 origin = RealitySpace.Origin(realityRoot.Id);
            Vector2 layerOffset = ParallaxMath.LayerLocalOffset(realityCamera.transform.position, origin, screenSpeed);
            Apply(layerOffset, ParallaxMath.CameraLocal(realityCamera.transform.position, origin, layerOffset));
        }

        void Apply(Vector2 layerOffset, Vector2 cameraLocal)
        {
            transform.localPosition = new Vector3(layerOffset.x, layerOffset.y, transform.localPosition.z);
            if (tiles == null || tileWidth <= 0f) return;

            int middle = tiles.Length / 2;
            for (int i = 0; i < tiles.Length; i++)
            {
                Transform tile = tiles[i];
                if (tile == null) continue;
                tile.localPosition = new Vector3(ParallaxMath.TileLocalX(cameraLocal.x, tileWidth, i, middle), tileOffsetY, 0f);
            }
        }
    }

}
