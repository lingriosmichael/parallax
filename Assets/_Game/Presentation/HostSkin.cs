using System;
using UnityEngine;

namespace Parallax.Presentation
{
    /// <summary>PAX-A13 (§3.1 P10, §11 R4, §12 R8): the look of the fixed geometry a disguised trap sits in or passes for,
    /// copied from the host's own renderer when the level is built. A disguised trap draws exactly this until its reveal
    /// tick, so it stays pixel-identical to its host when the environment art changes: the level rebuild copies the new look.
    /// A world-tiled host (material Parallax/2D/Sprite-Lit-WorldTile) samples its tile in world space; a disguised trap and
    /// each of its crumble shards sample the same tile at their rest pose, so no cut shows and a shard keeps its own pixels.</summary>
    [Serializable]
    public struct HostSkin
    {
        public const string WorldTileShader = "Parallax/2D/Sprite-Lit-WorldTile";
        static readonly int WorldTileId = Shader.PropertyToID("_WorldTile"), RestId = Shader.PropertyToID("_Rest"), UVRectId = Shader.PropertyToID("_UVRect");
        static MaterialPropertyBlock block;

        public string HostName;
        public Sprite Sprite;
        public Material Material;
        public Color Color;
        public SpriteDrawMode DrawMode;
        public string SortingLayer;
        /// <summary>The world point the host's tiling starts from (its lower-left corner). Only a Tiled skin has a phase.</summary>
        public Vector2 TileOrigin;
        /// <summary>World-tiled: the tile's size in world units and the sprite's rectangle in its texture.</summary>
        public bool WorldTiled;
        public Vector2 Tile;
        public Vector4 UVRect;

        public bool IsSet => Sprite != null;
        static MaterialPropertyBlock Block => block ??= new MaterialPropertyBlock();

        public static bool IsWorldTileMaterial(Material m) => m != null && m.shader != null && m.shader.name == WorldTileShader;

        public static HostSkin Of(SpriteRenderer host)
        {
            if (host == null) return default;
            Bounds b = host.bounds;
            var skin = new HostSkin
            {
                HostName = host.gameObject.name, Sprite = host.sprite, Material = host.sharedMaterial, Color = host.color,
                DrawMode = host.drawMode, SortingLayer = host.sortingLayerName, TileOrigin = b.min,
            };
            if (IsWorldTileMaterial(host.sharedMaterial))
            {
                host.GetPropertyBlock(Block);
                skin.WorldTiled = true;
                skin.Tile = Block.GetVector(WorldTileId);
                skin.UVRect = Block.GetVector(UVRectId);
            }
            return skin;
        }

        /// <summary>A world-tiled skin for `sprite`: its tile is the sprite's world size, its rectangle the sprite's place in
        /// its texture (the whole texture wraps by the texture's own Repeat mode).</summary>
        public static HostSkin WorldTiledFor(string hostName, Sprite sprite, Material worldTile, Color color, string sortingLayer)
        {
            Rect r = sprite.textureRect;
            Texture t = sprite.texture;
            bool whole = Mathf.Approximately(r.width, t.width) && Mathf.Approximately(r.height, t.height);
            return new HostSkin
            {
                HostName = hostName, Sprite = sprite, Material = worldTile, Color = color, DrawMode = SpriteDrawMode.Sliced,
                SortingLayer = sortingLayer, WorldTiled = true, Tile = sprite.bounds.size,
                UVRect = whole ? new Vector4(0f, 0f, 1f, 1f) : new Vector4(r.x / t.width, r.y / t.height, r.width / t.width, r.height / t.height),
            };
        }

        /// <summary>Draws `renderer` as this skin, `size` units, keeping its own sorting order and its transform. A world-tiled
        /// skin samples the tile where the renderer is now.</summary>
        public void ApplyTo(SpriteRenderer renderer, Vector2 size)
        {
            if (renderer == null || !IsSet) return;
            if (WorldTiled) { ApplyWorldTiled(renderer, size, renderer.transform.position); return; }
            renderer.sprite = Sprite;
            if (Material != null) renderer.sharedMaterial = Material;
            renderer.color = Color;
            renderer.drawMode = DrawMode;
            if (DrawMode != SpriteDrawMode.Simple) renderer.size = size;
            renderer.sortingLayerName = SortingLayer;
        }

        /// <summary>A world-tiled skin `size` units, sampling the tile as if it stood at `restPivot` (its pivot's world point
        /// at rest), wherever it is drawn: a shard keeps its own pixels as it moves.</summary>
        public void ApplyWorldTiled(SpriteRenderer renderer, Vector2 size, Vector2 restPivot)
        {
            if (renderer == null || !IsSet) return;
            renderer.sprite = Sprite;
            renderer.sharedMaterial = Material;
            renderer.color = Color;
            // Sliced to its size, its transform untouched (a host's transform carries its collider): the shader replaces the
            // mesh's UVs with world ones, so the mesh only has to cover the area.
            renderer.drawMode = SpriteDrawMode.Sliced;
            renderer.size = size;
            renderer.sortingLayerName = SortingLayer;
            SetSampling(renderer, restPivot, renderer.transform.lossyScale);
        }

        /// <summary>Points a world-tiled renderer's sampling at a rest pose (its pivot's world point and scale).</summary>
        public void SetSampling(SpriteRenderer renderer, Vector2 restPivot, Vector2 scale)
        {
            renderer.GetPropertyBlock(Block);
            Block.SetVector(WorldTileId, new Vector4(Tile.x, Tile.y, 0f, 0f));
            Block.SetVector(RestId, new Vector4(restPivot.x, restPivot.y, scale.x, scale.y));
            Block.SetVector(UVRectId, UVRect);
            renderer.SetPropertyBlock(Block);
        }

        /// <summary>Null when `renderer` draws as this skin (same sprite, material, colour, draw mode, sorting layer and, for a
        /// Tiled skin, the same tiling phase; for a world-tiled one, the same tile, sampled where it stands); otherwise what
        /// differs.</summary>
        public string Difference(SpriteRenderer renderer)
        {
            if (renderer == null) return "no renderer";
            if (renderer.sprite != Sprite) return $"sprite '{Name(renderer.sprite)}', host '{Name(Sprite)}'";
            if (Material != null && renderer.sharedMaterial != Material) return $"material '{Name(renderer.sharedMaterial)}', host '{Name(Material)}'";
            if (renderer.color != Color) return $"colour {renderer.color}, host {Color}";
            if (renderer.sortingLayerName != SortingLayer) return $"sorting layer '{renderer.sortingLayerName}', host '{SortingLayer}'";
            if (WorldTiled)
            {
                renderer.GetPropertyBlock(Block);
                Vector4 tile = Block.GetVector(WorldTileId), rest = Block.GetVector(RestId);
                if ((Vector2)tile != Tile) return $"world tile {(Vector2)tile}, host {Tile}";
                if (Block.GetVector(UVRectId) != UVRect) return "a different sprite rectangle from the host's";
                Vector3 p = renderer.transform.position, s = renderer.transform.lossyScale;
                if (Mathf.Abs(rest.x - p.x) > 1e-4f || Mathf.Abs(rest.y - p.y) > 1e-4f || Mathf.Abs(rest.z - s.x) > 1e-4f || Mathf.Abs(rest.w - s.y) > 1e-4f)
                    return $"samples the tile at {rest}, not where it stands ({p}, scale {s})";
                return null;
            }
            if (renderer.drawMode != DrawMode) return $"draw mode {renderer.drawMode}, host {DrawMode}";
            if (DrawMode == SpriteDrawMode.Tiled && Sprite != null)
            {
                Vector2 tile = Sprite.bounds.size;
                Vector2 d = (Vector2)renderer.bounds.min - TileOrigin;
                float px = Phase(d.x, tile.x), py = Phase(d.y, tile.y);
                if (px > 1f / 128f || py > 1f / 128f) return $"tiling phase ({px:F4}, {py:F4}) units off the host's";
            }
            return null;
        }

        static float Phase(float d, float tile)
        {
            if (tile <= 0f) return 0f;
            float r = Mathf.Repeat(d, tile);
            return Mathf.Min(r, tile - r);
        }

        static string Name(UnityEngine.Object o) => o == null ? "none" : o.name;
    }
}
