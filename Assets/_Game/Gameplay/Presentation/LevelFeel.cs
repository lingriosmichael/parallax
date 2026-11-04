using System.Collections.Generic;
using Parallax.Core;
using Parallax.Gameplay.Cameras;
using Parallax.Gameplay.Levels;
using Parallax.Gameplay.Observers;
using Parallax.Gameplay.Player;
using Parallax.Gameplay.Rooms;
using UnityEngine;

namespace Parallax.Gameplay.Presentation
{
    /// <summary>PAX-V06: a level's feel, beside LevelHaptics: dust where the cat runs, skids, takes off, lands and slides down a
    /// wall; an impact ring and a camera shake where a heavy mover stops at once (a falling block, a slam, a crusher); the death
    /// beat (a 3-frame flash on the cat and a shake); a burst where a checkpoint gate lights and over the door at level complete.
    /// Read-only: it subscribes and polls, draws its own pooled sprites and the camera's ShakeOffset, and never writes gameplay
    /// state; removing it changes no test. Reduce motion turns all of it off but the flash. It runs after ObserverSet's tick, as
    /// LevelHaptics does, so it sees a hold on the tick it starts.</summary>
    [DefaultExecutionOrder(1000)]
    [DisallowMultipleComponent]
    public sealed class LevelFeel : MonoBehaviour
    {
        [SerializeField] FeelConfig config;
        [SerializeField] RoomManager rooms;
        [SerializeField] RoomDeath death;
        [SerializeField] ObserverSet observers;
        [SerializeField] LevelCameraFollow cameraFollow;
        [Tooltip("P5.3: an Obsidian Void level (baked by the level build): glass shards and cold sparks, not stone chips.")]
        [SerializeField] bool obsidian;

        sealed class Puff
        {
            public SpriteRenderer Renderer;
            public float Age, Life, Size, Grow, Fall, Spin;
            public Vector2 Drift;
            public Color Tint;
            public bool Swell;   // a burst: it swells in, then fades
        }

        readonly List<Puff> pool = new List<Puff>();
        readonly List<MovingTrap> movers = new List<MovingTrap>();
        // Each solid mover's place, speed (u/s) and heading at the last tick.
        readonly Dictionary<MovingTrap, (Vector2 at, float speed, Vector2 heading)> moved = new Dictionary<MovingTrap, (Vector2, float, Vector2)>();
        CatVisualPresenter cat;
        GravityReceiver gravity;
        SpriteRenderer catBody;
        RoomDoor[] doors = new RoomDoor[0];
        bool holdSeen;
        int next, puffCount, shakeSeed;
        float runTravel, wallTimer, shakeAmplitude, shakeTime, shakeDuration;
        Vector2 lastPaws;

        static bool Calm => LevelProgressStore.ReduceMotion;

        void OnEnable()
        {
            if (config == null || rooms == null || death == null || observers == null)
            {
                Debug.LogError("LevelFeel: FeelConfig, RoomManager, RoomDeath or ObserverSet is missing; the level has no feel effects. Run PARALLAX/Setup/Game Feel (PAX-V06).", this);
                enabled = false;
                return;
            }
            foreach (GameObject root in gameObject.scene.GetRootGameObjects())
                movers.AddRange(root.GetComponentsInChildren<MovingTrap>(true));
            doors = FindObjectsByType<RoomDoor>(FindObjectsSortMode.None);
            rooms.LevelCompleted += OnLevelCompleted;
            rooms.SectionReached += OnSectionReached;
            death.Died += OnDied;
        }

        void OnDisable()
        {
            if (cat != null) cat.StateChanged -= OnCatState;
            cat = null;
            movers.Clear();
            moved.Clear();
            if (rooms != null) { rooms.LevelCompleted -= OnLevelCompleted; rooms.SectionReached -= OnSectionReached; }
            if (death != null) death.Died -= OnDied;
            if (cameraFollow != null) cameraFollow.ShakeOffset = Vector2.zero;
            foreach (Puff p in pool) if (p.Renderer != null) p.Renderer.enabled = false;
            holdSeen = false;
        }

        // The cat is found once it exists (the observer set wires it at its own start).
        bool FindCat()
        {
            if (cat != null) return true;
            ObserverContext solo = observers.Get(rooms.SoloReality);
            if (solo == null || solo.Cat == null) return false;
            cat = solo.Cat.GetComponentInChildren<CatVisualPresenter>(true);
            if (cat == null) return false;
            gravity = solo.Cat.GetComponent<GravityReceiver>();
            catBody = cat.GetComponentInChildren<SpriteRenderer>(true);
            cat.StateChanged += OnCatState;
            lastPaws = cat.transform.position;
            return true;
        }

        Vector2 Up => gravity != null && gravity.Direction.y > 0f ? Vector2.down : Vector2.up;

        void FixedUpdate()
        {
            if (!FindCat()) return;
            if (death.IsHolding)
            {
                if (!holdSeen) DeathBeat();
                holdSeen = true;
                moved.Clear();   // a frozen mover isn't one that stopped
                return;
            }
            foreach (MovingTrap m in movers)
            {
                if (m == null || !m.IsSolid) continue;
                Vector2 at = m.transform.position;
                bool known = moved.TryGetValue(m, out var last);
                Vector2 step = known ? at - last.at : Vector2.zero;
                float speed = step.magnitude / TickTime.SecondsPerTick;
                if (speed > 60f) speed = 0f;   // a reset's jump home, not a move
                if (known && FeelMath.Impact(last.speed, speed, config.ImpactSpeed)) Impact(m, last.heading);
                moved[m] = (at, speed, speed > 0f ? step.normalized : last.heading);
            }
        }

        // A heavy mover stopped at once: a ring of dust along the face it hit, and a shake by how near the cat is.
        void Impact(MovingTrap m, Vector2 dir)
        {
            Collider2D c = m.GetComponent<Collider2D>();
            if (c == null) return;
            if (dir == Vector2.zero) dir = Vector2.down;
            Bounds b = c.bounds;
            bool vertical = Mathf.Abs(dir.y) >= Mathf.Abs(dir.x);
            Vector2 face = vertical ? new Vector2(b.center.x, dir.y < 0f ? b.min.y : b.max.y) : new Vector2(dir.x < 0f ? b.min.x : b.max.x, b.center.y);
            float near = FeelMath.Falloff(Vector2.Distance(face, cat.transform.position), config.ShakeWithin);
            if (near <= 0f || Calm) return;
            float angle = vertical ? (dir.y < 0f ? 0f : 180f) : (dir.x < 0f ? -90f : 90f);
            Spawn(Pick(config.ImpactRings, Mathf.Max(b.size.x, b.size.y) > 1.5f ? 2 : 1), face, Mathf.Max(b.size.x, b.size.y) * 1.1f, angle, config.ImpactLife, Vector2.zero, 0.25f, swell: false);
            Throw(m.name, face, dir, vertical, angle);
            Shake(config.ImpactShake * near, config.ImpactShakeTime);
        }

        // P5.3: what a stop throws off the face it hit: Obsidian's glass shards and cold sparks; otherwise sparks off metal (known
        // by its name: a slam, a piston, a crusher, a press) and stone chips off the rest. Chips fly back and to both sides, spin
        // and fall.
        void Throw(string mover, Vector2 face, Vector2 dir, bool vertical, float angle)
        {
            bool metal = !obsidian && IsMetal(mover);
            Sprite[] flashes = obsidian ? config.ColdSparks : metal ? config.Sparks : null;
            Sprite[] bits = obsidian ? config.Shards : metal ? null : config.Chips;
            if (flashes != null && flashes.Length > 0)
            {
                Sprite s = Pick(flashes, shakeSeed % flashes.Length);
                Spawn(s, face, s.bounds.size.x, angle, 0.2f, Vector2.zero, 0.3f, swell: false, tint: Color.white, order: 1);
            }
            if (bits == null || bits.Length == 0) return;
            Vector2 along = vertical ? Vector2.right : Vector2.up;
            for (int i = 0; i < config.ChipsPerImpact; i++)
            {
                float side = (i % 2 == 0 ? 1f : -1f) * (0.4f + 0.6f * ((i * 37 + shakeSeed * 11) % 10) / 10f);
                Sprite s = Pick(bits, (shakeSeed + i) % bits.Length);
                Vector2 velocity = (-dir * 0.8f + along * side) * config.ChipSpeed;
                Spawn(s, face + along * side * 0.3f, s.bounds.size.x, (i * 73 + shakeSeed * 29) % 360, config.ChipLife, velocity, 0f, swell: false,
                    tint: Color.white, order: 1, fall: config.ChipGravity, spin: 360f * side);
            }
        }

        static bool IsMetal(string name) =>
            name.IndexOf("Slam", System.StringComparison.OrdinalIgnoreCase) >= 0 || name.IndexOf("Piston", System.StringComparison.OrdinalIgnoreCase) >= 0
            || name.IndexOf("Crush", System.StringComparison.OrdinalIgnoreCase) >= 0 || name.IndexOf("Press", System.StringComparison.OrdinalIgnoreCase) >= 0;

        void DeathBeat()
        {
            // The one cue Reduce motion keeps: 2-3 frames of white on the cat.
            Vector2 at = (Vector2)cat.transform.position + Up * 0.35f;
            Spawn(config.Flash, at, config.FlashSize, 0f, config.FlashTime, Vector2.zero, 0f, swell: false, tint: config.FlashColor, order: 3);
            if (!Calm) Shake(config.DeathShake, config.DeathShakeTime);
        }

        // The reset after a hold; with no hold (HoldTicks 0) this is the only sign of the death.
        void OnDied(DeathInfo info)
        {
            if (!holdSeen && info.Observer == rooms.SoloReality && FindCat()) DeathBeat();
            holdSeen = false;
            moved.Clear();
            runTravel = 0f;
        }

        void OnCatState(CatAnimState before, CatAnimState now)
        {
            if (Calm) return;
            Vector2 paws = cat.transform.position;
            int behind = -cat.Facing;
            switch (now)
            {
                case CatAnimState.Land: Dust(config.LandPuffs, 0, paws, 0.9f); break;
                case CatAnimState.HardLand:
                    Dust(config.LandPuffs, 1, paws, 1.1f);
                    Spawn(Pick(config.ImpactRings, 0), paws, 1.4f, Up.y < 0f ? 180f : 0f, config.ImpactLife, Vector2.zero, 0.2f, swell: false);
                    Shake(config.HardLandShake, config.ImpactShakeTime);
                    break;
                case CatAnimState.Turn: Dust(config.SkidPuffs, 0, paws + Vector2.right * behind * 0.15f, 0.8f, behind); break;
                case CatAnimState.TakeOff: Dust(config.RunPuffs, 1, paws, 0.7f); break;
                case CatAnimState.Rise when before is CatAnimState.Run or CatAnimState.Walk or CatAnimState.Idle: Dust(config.RunPuffs, 1, paws, 0.7f); break;
            }
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (cat != null && !Calm)
            {
                Vector2 paws = cat.transform.position;
                if (cat.State == CatAnimState.Run)
                {
                    runTravel += Mathf.Abs(paws.x - lastPaws.x);
                    if (runTravel >= config.RunPuffEvery) { runTravel = 0f; Dust(config.RunPuffs, puffCount % 2, paws + Vector2.right * -cat.Facing * 0.2f, 0.6f, -cat.Facing); }
                }
                else runTravel = config.RunPuffEvery * 0.5f;   // the first puff comes half a stride into a run
                if (cat.State == CatAnimState.WallSlide && (wallTimer -= dt) <= 0f)
                {
                    wallTimer = config.WallPuffEvery;
                    Dust(config.RunPuffs, 0, paws + Vector2.right * cat.Facing * 0.22f + Up * 0.35f, 0.45f);
                }
                lastPaws = paws;
            }
            foreach (Puff p in pool) Age(p, dt);
            shakeTime += dt;
            if (cameraFollow != null) cameraFollow.ShakeOffset = Calm ? Vector2.zero : FeelMath.Shake(shakeAmplitude, shakeTime, shakeDuration, shakeSeed, config.ShakeFrequency);
        }

        void OnSectionReached(Vector2 gate)
        {
            if (!Calm) Spawn(config.Burst, gate, config.GateBurstSize, 0f, config.BurstTime, Vector2.zero, 0f, swell: true, tint: Color.white, order: 2);
        }

        void OnLevelCompleted()
        {
            if (Calm || !FindCat()) return;
            Vector2 at = cat.transform.position;
            RoomDoor door = null;
            float best = float.PositiveInfinity;
            foreach (RoomDoor d in doors)
            {
                if (d == null) continue;
                float dist = Vector2.Distance(d.transform.position, at);
                if (dist < best) { best = dist; door = d; }
            }
            Vector2 centre = door != null ? (Vector2)door.transform.position : at + Up * 0.8f;
            Spawn(config.Burst, centre, config.DoorBurstSize, 0f, config.BurstTime, Vector2.zero, 0f, swell: true, tint: Color.white, order: 2);
        }

        void Shake(float amplitude, float duration)
        {
            float left = shakeTime >= shakeDuration ? 0f : shakeAmplitude * (1f - shakeTime / shakeDuration);
            if (amplitude < left) return;   // a stronger shake is still under way
            shakeAmplitude = amplitude;
            shakeDuration = duration;
            shakeTime = 0f;
            shakeSeed++;
        }

        // A puff of dust at the paws (drawn on the cat's side of gravity), drifting `drift` (-1 back, +1 forward, 0 up) and growing.
        void Dust(Sprite[] sprites, int index, Vector2 at, float size, int drift = 0)
        {
            Sprite s = Pick(sprites, index);
            if (s == null) return;
            float width = s.bounds.size.x * config.PuffScale * size / 0.6f;
            Vector2 centre = at + Up * s.bounds.size.y * config.PuffScale * size * 0.5f;
            Spawn(s, centre, width, Up.y < 0f ? 180f : 0f, config.PuffLife, new Vector2(drift * 0.6f, 0f) + Up * 0.25f, 0.5f, swell: false);
            puffCount++;
        }

        static Sprite Pick(Sprite[] sprites, int index) => sprites == null || sprites.Length == 0 ? null : sprites[Mathf.Clamp(index, 0, sprites.Length - 1)];

        void Spawn(Sprite sprite, Vector2 at, float width, float angle, float life, Vector2 drift, float grow, bool swell, Color? tint = null, int order = -1, float fall = 0f, float spin = 0f)
        {
            if (sprite == null || life <= 0f || catBody == null) return;
            Puff p = Take();
            SpriteRenderer r = p.Renderer;
            r.sprite = sprite;
            r.sortingLayerID = catBody.sortingLayerID;
            r.sortingOrder = catBody.sortingOrder + order;
            r.transform.SetPositionAndRotation(new Vector3(at.x, at.y, catBody.transform.position.z), Quaternion.Euler(0f, 0f, angle));
            p.Size = width / Mathf.Max(0.01f, sprite.bounds.size.x);
            p.Age = 0f; p.Life = life; p.Drift = drift; p.Grow = grow; p.Swell = swell; p.Fall = fall; p.Spin = spin;
            p.Tint = tint ?? config.DustTint;
            r.enabled = true;
            Age(p, 0f);
        }

        Puff Take()
        {
            if (pool.Count < Mathf.Max(1, config.PoolSize))
            {
                var go = new GameObject("Feel_" + pool.Count);
                go.transform.SetParent(transform, false);
                var made = new Puff { Renderer = go.AddComponent<SpriteRenderer>() };
                pool.Add(made);
                return made;
            }
            Puff p = pool[next];   // the oldest
            next = (next + 1) % pool.Count;
            return p;
        }

        static void Age(Puff p, float dt)
        {
            if (p.Renderer == null || !p.Renderer.enabled) return;
            p.Age += dt;
            float u = p.Age / p.Life;
            if (u >= 1f) { p.Renderer.enabled = false; return; }
            p.Drift.y -= p.Fall * dt;
            p.Renderer.transform.position += (Vector3)(p.Drift * dt);
            if (p.Spin != 0f) p.Renderer.transform.Rotate(0f, 0f, p.Spin * dt);
            float scale = p.Swell ? p.Size * (0.4f + 0.6f * Mathf.Sin(Mathf.Min(1f, u * 2f) * Mathf.PI * 0.5f)) : p.Size * (1f + p.Grow * u);
            p.Renderer.transform.localScale = new Vector3(scale, scale, 1f);
            Color c = p.Tint;
            c.a *= p.Swell ? Mathf.Sin(u * Mathf.PI) : (1f - u) * (1f - u);
            p.Renderer.color = c;
        }
    }
}
