using UnityEngine;

namespace Parallax.Core.Presentation
{
    /// <summary>A crumble shard's pose, relative to its authored place, `ticks` after the floor gave way.</summary>
    public struct ShardPose
    {
        public Vector2 Offset;
        public float Rotation;
        public float Alpha;
        public bool Visible;
    }

    /// <summary>A small deterministic particle (dust, grit, spray): its offset from the emitter, scale and alpha.</summary>
    public struct PuffPose
    {
        public Vector2 Offset;
        public float Scale;
        public float Alpha;
        public bool Visible;
    }

    /// <summary>PAX-A13 (§7, §11 R4/R5): trap presentation as pure functions of ticks and seeds, so a frame, a shard or a
    /// puff is the same for the same (state, ticks) in the game, in the route harness and after a checkpoint rewind. Nothing
    /// here reads time or Random; variants come from Seed(name, event count).</summary>
    public static class TrapArtMath
    {
        /// <summary>The play layer's scale (§11 R1): trap art is authored and checked at 128 px per unit.</summary>
        public const float PixelsPerUnit = 128f;
        /// <summary>R5: art bounds may exceed the grey-box's by at most one pixel.</summary>
        public const float BoundsTolerance = 1f / PixelsPerUnit;

        public const int CrumbleTicks = 36;

        // How long each event's effect lasts, in ticks from its event (§11 R4): the presenters draw them and the parity test
        // bounds them by the same numbers, so an effect that lingers past its event, or starts before it, fails.
        /// <summary>A flip's pulse, from its fire.</summary>
        public const int FlipPulseTicks = 14;
        /// <summary>An inverter orb's flare, from its fire.</summary>
        public const int InverterFlareTicks = 14;
        /// <summary>A snapped vine's falling pieces and leaves, from the snap.</summary>
        public const int VineFallTicks = 36;
        /// <summary>A retreating door's scrape dust, after the door last moved.</summary>
        public const int DoorDustTicks = 16;
        /// <summary>A falling block's crack, after it lands.</summary>
        public const int CrackFadeTicks = 20;
        /// <summary>A falling block's landing dust, from the tick it comes to rest.</summary>
        public const int LandDustTicks = 24;
        /// <summary>A moving solid's dust puff (its leading edge while it moves) and a shrinker's chips.</summary>
        public const int SolidDustTicks = 18;

        /// <summary>A stable 32-bit seed (FNV-1a) from an element's name and an event count.</summary>
        public static uint Seed(string name, int eventCount)
        {
            uint h = 2166136261u;
            if (name != null)
                foreach (char c in name) { h ^= (byte)c; h *= 16777619u; if (c > 0xFF) { h ^= (byte)(c >> 8); h *= 16777619u; } }
            return h ^ unchecked((uint)eventCount * 0x9E3779B9u);
        }

        /// <summary>A value in [0, 1) from (seed, index): the same inputs give the same value everywhere.</summary>
        public static float Hash01(uint seed, int index)
        {
            // A 32-bit integer mix (lowbias32); 24 bits give a float in [0, 1).
            uint x = seed ^ unchecked((uint)index * 0x85EBCA6Bu + 0x27D4EB2Fu);
            x ^= x >> 16; x *= 0x7FEB352Du; x ^= x >> 15; x *= 0x846CA68Bu; x ^= x >> 16;
            return (x >> 8) * (1f / 16777216f);
        }

        /// <summary>A value in [-1, 1) from (seed, index).</summary>
        public static float HashSigned(uint seed, int index) => Hash01(seed, index) * 2f - 1f;

        /// <summary>The flipbook frame `ticks` after its event: `frames` played evenly over `durationTicks`, then held on the
        /// last frame (or wrapped when `loop`). -1 before the event.</summary>
        public static int Frame(int ticks, int durationTicks, int frames, bool loop)
        {
            if (ticks < 0 || frames < 1) return -1;
            int duration = Mathf.Max(1, durationTicks);
            if (loop) ticks %= duration;
            else if (ticks >= duration) return frames - 1;
            return Mathf.Min(frames - 1, ticks * frames / duration);
        }

        /// <summary>The launcher's glint: on exactly over the declared tell, 0 ≤ s &lt; tellTicks (§11 R4).</summary>
        public static bool ArrowGlint(int ticksSinceFire, int tellTicks) => ticksSinceFire >= 0 && ticksSinceFire < tellTicks;

        /// <summary>How many shard columns a floor `width` units wide breaks into.</summary>
        public static int ShardColumns(float width) => Mathf.Clamp(Mathf.RoundToInt(width / 0.5f), 1, 12);

        /// <summary>Shard `index` of a columns x rows cut of `bounds` (room-local): the cuts are jittered by the seed, and at
        /// tick 0 the shards tile `bounds` exactly, with no gap and no overlap.</summary>
        public static Rect ShardRect(Rect bounds, uint seed, int index, int columns, int rows)
        {
            columns = Mathf.Max(1, columns); rows = Mathf.Max(1, rows);
            int column = index / rows, row = index % rows;
            float x0 = ColumnCut(bounds, seed, column, columns), x1 = ColumnCut(bounds, seed, column + 1, columns);
            float y0 = RowCut(bounds, seed, column, row, rows), y1 = RowCut(bounds, seed, column, row + 1, rows);
            return Rect.MinMaxRect(x0, y0, x1, y1);
        }

        // Interior cuts move up to 30% of a column either way; the outer edges stay on the bounds, so the shards tile them.
        static float ColumnCut(Rect b, uint seed, int i, int columns)
        {
            if (i <= 0) return b.xMin;
            if (i >= columns) return b.xMax;
            float w = b.width / columns;
            return b.xMin + w * (i + 0.3f * HashSigned(seed, 1000 + i));
        }

        static float RowCut(Rect b, uint seed, int column, int j, int rows)
        {
            if (j <= 0) return b.yMin;
            if (j >= rows) return b.yMax;
            float h = b.height / rows;
            return b.yMin + h * (j + 0.3f * HashSigned(seed, 2000 + column * 16 + j));
        }

        /// <summary>Shard `index`'s pose `ticks` after the floor gave way: it drops and spins, and fades out by CrumbleTicks.</summary>
        public static ShardPose Shard(uint seed, int index, int ticks)
        {
            if (ticks < 0 || ticks >= CrumbleTicks) return default;
            // Per tick: a small sideways drift and a hop, then gravity (units per tick², at 50 Hz about 20 u/s²).
            float vx = 0.02f * HashSigned(seed, 3 * index), vy = 0.03f * Hash01(seed, 3 * index + 1);
            const float gravity = 0.008f;
            float spin = 9f * HashSigned(seed, 3 * index + 2);
            float t = ticks;
            var pose = new ShardPose
            {
                Offset = new Vector2(vx * t, vy * t - 0.5f * gravity * t * t),
                Rotation = spin * t,
                Alpha = Mathf.Clamp01((CrumbleTicks - t) / 10f),
                Visible = true,
            };
            return pose;
        }

        /// <summary>Puff `index` of an emitter `ticks` after its event, lasting `lifeTicks`, spreading up to `spread` units.</summary>
        public static PuffPose Puff(uint seed, int index, int ticks, int lifeTicks, float spread)
        {
            if (ticks < 0 || ticks >= lifeTicks || lifeTicks < 1) return default;
            float u = (float)ticks / lifeTicks;
            float angle = Mathf.PI * Hash01(seed, 5 * index);              // upward half-plane
            float reach = spread * (0.4f + 0.6f * Hash01(seed, 5 * index + 1));
            float ease = 1f - (1f - u) * (1f - u);
            return new PuffPose
            {
                Offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * (reach * ease),
                Scale = 0.5f + 0.9f * ease * (0.7f + 0.3f * Hash01(seed, 5 * index + 2)),
                Alpha = 1f - u,
                Visible = true,
            };
        }
    }
}
