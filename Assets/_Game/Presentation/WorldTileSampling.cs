using UnityEngine;

namespace Parallax.Presentation
{
    /// <summary>PAX-A15: where a world-tiled sprite (Parallax/2D/Sprite-Lit-WorldTile) samples its tile. The shader reads
    /// it from a MaterialPropertyBlock, which scenes don't save, so the level builder stores the values here and they are
    /// applied whenever the renderer is enabled, in edit mode too. One shared material, so the sprites still batch. Trap
    /// art skins don't use this: their presenters set their sampling every frame (HostSkin.SetSampling).</summary>
    [ExecuteAlways]
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class WorldTileSampling : MonoBehaviour
    {
        static readonly int WorldTileId = Shader.PropertyToID("_WorldTile"), RestId = Shader.PropertyToID("_Rest"), UVRectId = Shader.PropertyToID("_UVRect");
        static MaterialPropertyBlock block;

        [Tooltip("The tile's size in world units.")]
        [SerializeField] Vector2 tile = Vector2.one;
        [Tooltip("The sampling origin (xy) and scale (zw): uv = (rest.xy + objectPosition * rest.zw) / tile.")]
        [SerializeField] Vector4 rest = new(0f, 0f, 1f, 1f);
        [SerializeField] Vector4 uvRect = new(0f, 0f, 1f, 1f);

        public Vector2 Tile => tile;
        public Vector4 Rest => rest;

        public void Set(Vector2 newTile, Vector4 newRest, Vector4 newUVRect)
        {
            tile = newTile; rest = newRest; uvRect = newUVRect;
            Apply();
        }

        void OnEnable() => Apply();
#if UNITY_EDITOR
        void OnValidate() => Apply();
#endif

        public void Apply()
        {
            var r = GetComponent<SpriteRenderer>();
            if (r == null) return;
            block ??= new MaterialPropertyBlock();
            r.GetPropertyBlock(block);
            block.SetVector(WorldTileId, new Vector4(tile.x, tile.y, 0f, 0f));
            block.SetVector(RestId, rest);
            block.SetVector(UVRectId, uvRect);
            r.SetPropertyBlock(block);
        }
    }
}
