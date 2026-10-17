using UnityEngine;

namespace Parallax.Presentation
{
    /// <summary>PAX-A18 W3 (2026-10-17, the developer: "sometimes it doesn't match the color of the tile it is attached to"): the
    /// room's stone shadow as a small world-space map, read by the stone's shaders (Env-Sprite-Lit-WorldUV,
    /// Env-Sprite-Lit-EdgeMask, Trap-Sprite-Lit-WorldTile; StoneShade.hlsl), so every piece of stone at one place, a block,
    /// its broken strip or a disguised trap, takes the same shadow. Red: the face shadow under a block's lit lip; green: the
    /// outside masonry's. Baked by the room's skin (SoloRoomSkin); presentation only, never read by gameplay. With none in
    /// the scene the shadows' strengths are zero and the shaders draw the stone as it is.</summary>
    [ExecuteAlways]
    public sealed class StoneShade : MonoBehaviour
    {
        static readonly int MapId = Shader.PropertyToID("_PaxShadeMap"), RectId = Shader.PropertyToID("_PaxShadeRect"),
            FaceId = Shader.PropertyToID("_PaxFaceShade"), OutsideId = Shader.PropertyToID("_PaxOutsideShade");

        [Tooltip("The world rectangle the map covers (clamped past it).")]
        [SerializeField] Rect area;
        [SerializeField] int width, height;
        [Tooltip("Two bytes per texel, rows from the bottom: the face shadow's strength, then the outside's (0-255).")]
        [SerializeField] byte[] texels = new byte[0];
        [Tooltip("Each shadow's colour, and its full strength as alpha.")]
        [SerializeField] Color faceShade, outsideShade;

        Texture2D map;

        public Rect Area => area;

        /// <summary>The face shadow's and the outside's strength (0-1) at a world point, as the shaders read it (nearest texel).</summary>
        public Vector2 StrengthAt(Vector2 world)
        {
            if (width <= 0 || height <= 0 || texels == null || texels.Length != width * height * 2) return Vector2.zero;
            int i = Mathf.Clamp(Mathf.FloorToInt((world.x - area.xMin) / area.width * width), 0, width - 1);
            int j = Mathf.Clamp(Mathf.FloorToInt((world.y - area.yMin) / area.height * height), 0, height - 1);
            int k = 2 * (j * width + i);
            return new Vector2(texels[k] / 255f, texels[k + 1] / 255f);
        }

        public void Configure(Rect worldArea, int texelsWide, int texelsHigh, byte[] data, Color face, Color outside)
        {
            area = worldArea; width = texelsWide; height = texelsHigh; texels = data; faceShade = face; outsideShade = outside;
            if (isActiveAndEnabled) Apply();
        }

        void OnEnable() => Apply();

        void OnDisable()
        {
            // Only if the shaders still read this one's map (another room's may have taken over).
            if (map != null && Shader.GetGlobalTexture(MapId) == map)
            {
                Shader.SetGlobalColor(FaceId, Color.clear);
                Shader.SetGlobalColor(OutsideId, Color.clear);
            }
            Release();
        }

        void Apply()
        {
            if (width == 0 && height == 0) return;   // not baked yet (just added by the room's skin, which configures it next)
            if (width <= 0 || height <= 0 || texels == null || texels.Length != width * height * 2)
            {
                Debug.LogError($"StoneShade on '{name}': its map is missing or the wrong size ({width} x {height}, {texels?.Length ?? 0} bytes); rebuild the level.", this);
                return;
            }
            Release();
            map = new Texture2D(width, height, TextureFormat.RG16, false, true)
            {
                name = "StoneShade", filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.DontSave,
            };
            map.LoadRawTextureData(texels);
            map.Apply(false, false);
            Shader.SetGlobalTexture(MapId, map);
            Shader.SetGlobalVector(RectId, new Vector4(area.xMin, area.yMin, 1f / area.width, 1f / area.height));
            Shader.SetGlobalColor(FaceId, faceShade);
            Shader.SetGlobalColor(OutsideId, outsideShade);
        }

        void Release()
        {
            if (map == null) return;
            if (Application.isPlaying) Destroy(map); else DestroyImmediate(map);
            map = null;
        }
    }
}
