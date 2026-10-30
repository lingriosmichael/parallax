using Parallax.Core;
using Parallax.Gameplay.Observers;
using UnityEngine;

namespace Parallax.Gameplay.Rooms
{
    public enum MovingTrapKind { Hazard, Solid }
    // PAX-093 (D-095): how a Solid treats a cat standing on it. Legacy: as before PAX-093 (not carried; every existing element);
    // Carry: the cat rides it (RoomManager applies this tick's Displacement); Slip: it moves out from under the cat (not carried).
    public enum SurfaceMotion { Legacy, Carry, Slip }

    // D-119: a Solid may be a hinge floor: it swings 90 degrees about a corner over hingeTicks (tipping a cat on it, by
    // physics), then moves by offset as any mover, pushing (pushes) a cat in its way. Its pose is a function of the ticks
    // since the fire, so a reset and a rewind restore it exactly. PAX-107 (D-124): a hinge floor may be spiked (its top face,
    // which faces the cat once it stands: a touch kills), and a mover may rise first (an L-shaped move).
    [RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
    public sealed class MovingTrap : RoomTrap
    {
        [SerializeField] ObserverSet observers;
        [SerializeField] BoxCollider2D trigger;
        [SerializeField] MovingTrapKind kind;
        [SerializeField] Vector2 offset;
        [SerializeField] int moveTicks = 1, holdTicks, returnTicks;
        [SerializeField] float crushDepth;
        [SerializeField] CrushConfig crushConfig;
        [Header("PAX-093 floors that move")]
        [SerializeField] SurfaceMotion surfaceMotion;
        [SerializeField] bool pushes;
        [SerializeField] Collider2D crushPartner;
        // A PAX-093 floor's delay from its trigger to its move (a drop-and-return floor is harmless until it goes). The builder
        // writes it only for PAX-093 floors: every MovingTrap before them keeps the 0 it always had (every pin identical).
        [SerializeField] int delayTicks;
        [Header("D-119 the hinge floor (off at hingeTicks 0)")]
        [Tooltip("Ticks to swing 90 degrees about hingePivot before the move (offset) starts.")]
        [SerializeField] int hingeTicks;
        [Tooltip("The hinge, local to the start pose (a top corner of the flat floor).")]
        [SerializeField] Vector2 hingePivot;
        [Tooltip("+1 swings counter-clockwise, -1 clockwise.")]
        [SerializeField] float hingeSign = 1f;
        [Header("PAX-107 (D-124)")]
        [Tooltip("The move follows an L: the vertical part of offset first, then the horizontal (a lift that rises, then carries).")]
        [SerializeField] bool riseFirst;
        [Header("PAX-107 (D-140)")]
        [Tooltip("A slam: coming down, it kills a cat it lands on, even one physics would push aside (SlamHits).")]
        [SerializeField] bool slams;
        [Tooltip("A hinge floor's spiked face (the grey-box strip on its top face, shown from the swing): standing, a touch kills. Null: none.")]
        [SerializeField] SpriteRenderer spikes;
        [Header("PAX-113 (D-178) the scanner (off at warnTicks 0)")]
        [Tooltip("A Hazard mover's warning: from each fire it stays home this many ticks, flickering and harmless, then moves. While it "
            + "rests home unfired it is drawn dim. 0: every mover as before.")]
        [SerializeField] int warnTicks;
        [SerializeField] float idleAlpha = .25f, warnAlpha = .8f, warnDimAlpha = .5f;
        [Header("PAX-114 (D-187) the path mover and the glitch wall (off with no points)")]
        [Tooltip("Offsets from home, in order: the mover follows home, then each point (a closed loop ends on home, 0,0).")]
        [SerializeField] Vector2[] path;
        [Tooltip("A glitch wall: it sits on each point and teleports to the next, never sliding.")]
        [SerializeField] bool glitch;
        [Tooltip("D-250: the path's first leg takes this many of moveTicks, the rest the remainder (0: the whole path at one speed).")]
        [SerializeField] int pathFirstLegTicks;
        [Tooltip("A glitch wall's warning before each jump: it flickers and its ghost shows where it will appear.")]
        [SerializeField] int glitchWarnTicks;
        [SerializeField] SpriteRenderer ghost;
        [Header("PAX-115 (D-187) the flip platform (off at flipTicks 0)")]
        [Tooltip("Ticks to turn 180 degrees about the centre; it holds holdTicks and turns back over returnTicks. Its spiked face (spikes) is its underside, always shown. Not solid while it turns.")]
        [SerializeField] int flipTicks;
        [Tooltip("The flip's tell: from its fire it flickers this many ticks at rest before it turns.")]
        [SerializeField] int flipWarnTicks;
        [Header("PAX-122 (D-201) the phase platform (off at phaseOnTicks 0)")]
        [Tooltip("A Solid that stays put: from each fire it is there phaseOnTicks, flickering its last phaseWarnTicks, then gone (no collider, not drawn) until the next fire.")]
        [SerializeField] int phaseOnTicks;
        [SerializeField] int phaseWarnTicks;
        SpriteRenderer look; Color fullColor; SpriteRenderer[] looks; Color[] lookColors;
        Rigidbody2D body; BoxCollider2D box; ContactFilter2D filter; readonly Collider2D[] results = new Collider2D[8]; Vector2 start; float startAngle;

        // PAX-V07 §4: a Solid mover (push wall, crusher, lift, drop-and-return, falling ceiling) crushes; a Hazard one spikes.
        protected override CatDeathKind DeclaredDeathKind => kind == MovingTrapKind.Solid && spikes == null ? CatDeathKind.Crushed : CatDeathKind.Spiked;

        public SurfaceMotion Motion => surfaceMotion;
        protected override int DelayTicks => delayTicks;
        // PAX-093 (D-095): this room tick's move, pose(t) - pose(t-1); zero on a tick it didn't move. Valid only on
        // DisplacementTick (the RoomLifeTick it was computed on): a room that isn't live doesn't step its traps.
        public Vector2 Displacement { get; private set; }
        public int DisplacementTick { get; private set; } = -1;
        // D-115: the room tick before's move (zero when this trap didn't step then), so a floor that slows or stops can
        // settle the cat riding it (RoomManager, CatMotor2D.MatchSlowingFloor).
        public Vector2 PreviousDisplacement { get; private set; }
        public bool IsSolid => kind == MovingTrapKind.Solid;
        public Collider2D Box => box;

        protected override void Awake()
        {
            base.Awake(); body = GetComponent<Rigidbody2D>(); box = GetComponent<BoxCollider2D>();
            if (body == null || box == null || observers == null || Reality == null || (UsesOverlapSource && !IsPeriodic && trigger == null) || (kind == MovingTrapKind.Solid && crushDepth <= 0f && crushConfig == null))
            { Debug.LogError($"MovingTrap '{name}': missing required body, box, observers, reality, overlap trigger, or crush config.", this); enabled = false; return; }
            body.bodyType = RigidbodyType2D.Kinematic; start = body.position; startAngle = body.rotation;
            // PAX-A14: drawn between its tick poses, like the cat, so a floor moving at 50 Hz doesn't judder on a 60 Hz screen.
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            filter = new ContactFilter2D { useLayerMask = true, layerMask = Reality.PhysicsMask, useTriggers = false };
            ShowSpikes(false);
            look = GetComponent<SpriteRenderer>(); if (look != null) fullColor = look.color;
            ShowScan(-1);
            // PAX-114: every look the glitch flicker dims (the ghost and the spiked face excepted), at its authored colour.
            looks = System.Array.FindAll(GetComponentsInChildren<SpriteRenderer>(true), r => r != ghost && r != spikes);
            lookColors = System.Array.ConvertAll(looks, r => r.color);
            ShowGlitch(-1);
            // PAX-114: a glitch wall teleports; drawn between its tick poses it would slide.
            if (Glitches) body.interpolation = RigidbodyInterpolation2D.None;
            if (Phases) ShowPhase(0, 0);
        }

        bool HasPath => path != null && path.Length > 0;
        bool Glitches => glitch && HasPath;
        bool Flips => flipTicks > 0;
        bool Phases => phaseOnTicks > 0;

        // PAX-122 (D-201): a phase platform's state (TrapPath.PhaseState) `since` ticks after its fire, gone outside its timing's effect.
        int PhaseAt(int since) => TrapPath.PhaseState(IsTimingEffectActive ? since : -1, phaseOnTicks, phaseWarnTicks);

        // Its collider while it's there; its look (which its art mirrors) while it's there, off on odd ticks of the flicker.
        void ShowPhase(int state, int since)
        {
            box.enabled = state > 0;
            if (look != null) look.enabled = state == 1 || (state == 2 && since % 2 == 0);
        }

        // It appears where it stands, killing a cat it appears on (by the crush depth), as a glitch wall's arrival.
        void StepPhase(int since)
        {
            int state = PhaseAt(since);
            bool appearing = state > 0 && !box.enabled;
            ShowPhase(state, since);
            if (appearing && TryGetLocalHumanColliderBounds(observers, out Bounds cat)
                && TrapMotion.Crushes(cat, PoseBounds(body.position, body.rotation), crushDepth > 0f ? crushDepth : crushConfig.DefaultCrushDepth)) KillOwn();
        }

        // PAX-114 (D-187): a glitch wall's warning `since` ticks after its fire (negative: at rest): its looks flicker (full on
        // even ticks, half on odd) and its ghost stands where it will appear; otherwise all full and no ghost.
        void ShowGlitch(int since)
        {
            if (!Glitches) return;
            bool warning = since >= 0 && TrapPath.GlitchWarning(path, since, moveTicks, holdTicks, returnTicks, glitchWarnTicks, out int next);
            for (int i = 0; i < looks.Length; i++)
            {
                if (looks[i] == null) continue;
                Color c = lookColors[i];
                if (warning && since % 2 == 1) c.a *= .45f;
                looks[i].color = c;
            }
            if (ghost == null) return;
            ghost.enabled = warning;
            if (warning) ghost.transform.position = (Vector3)(start + TrapPath.Point(path, NextIndex(since)) + box.offset) + new Vector3(0f, 0f, ghost.transform.position.z);
        }

        int NextIndex(int since) { TrapPath.GlitchWarning(path, since, moveTicks, holdTicks, returnTicks, glitchWarnTicks, out int next); return next; }

        // PAX-115 (D-187): a flip platform's spiked face (its underside at rest) at a body pose: a cat touching it dies.
        bool FaceTouches(Vector2 position, float angle)
        {
            if (spikes == null) return false;
            Vector2 size = box.size;
            Vector2 local = new(0f, -(size.y + MovingTrapSpikeFace) * .5f);
            Vector2 centre = position + (Vector2)(Quaternion.Euler(0f, 0f, angle) * (box.offset + local));
            var face = new Bounds(centre, new Vector3(size.x + 2f * SpikeSkin, MovingTrapSpikeFace + 2f * SpikeSkin));
            return IsLocalHumanOverlapping(face, angle, observers, filter, results, out _);
        }

        // D-207: where a spiked mover kills: a slam's spiked face is the strip under its body (the builder's grey-box strip, as a
        // flip's); a hinge floor's is its whole body (its spikes face the cat it pushes).
        Bounds SpikedFace(Bounds pose) => slams
            ? new Bounds(new Vector3(pose.center.x, pose.min.y - MovingTrapSpikeFace * .5f, pose.center.z), new Vector3(pose.size.x, MovingTrapSpikeFace, pose.size.z))
            : pose;

        /// <summary>PAX-115: the spiked face's height (the builder's grey-box strip).</summary>
        public const float MovingTrapSpikeFace = .3f;

        public bool Scans => warnTicks > 0 && kind == MovingTrapKind.Hazard;

        // PAX-113 (D-178): a scanner's look `since` ticks after its fire (negative = resting home unfired): dim at rest, flickering
        // through the warning (bright on even ticks, half on odd), full once it moves.
        void ShowScan(int since)
        {
            if (!Scans || look == null) return;
            Color c = fullColor;
            if (since < 0) c.a = fullColor.a * idleAlpha;
            // Both warning frames differ from the resting look, so a flicker never reads as being home.
            else if (since < warnTicks) c.a = fullColor.a * (since % 2 == 0 ? warnAlpha : warnDimAlpha);
            look.color = c;
        }

        // PAX-107 (D-124): the spiked face shows from the fire (as the floor starts to swing up) until a reset. PAX-115: a flip
        // platform's underside is always shown.
        // D-189: a spiked slam's underside (a falling spiked ceiling) is always shown too.
        void ShowSpikes(bool shown) { if (spikes != null) spikes.enabled = shown || Flips || slams; }

        protected override void OnLiveRoomStep()
        {
            if (!enabled) return;
            PreviousDisplacement = DisplacementTick == RoomLifeTick - 1 ? Displacement : Vector2.zero;
            Displacement = Vector2.zero; DisplacementTick = RoomLifeTick; descending = false; OwnKill = false;
            bool overlap = trigger != null && IsLocalHumanOverlapping(trigger, observers, filter, results, out _);
            StepTiming(overlap);
            if (Phases) { StepPhase(RoomLifeTick - LatestFireTick); return; }
            // PAX-115: a flip platform's spiked face kills at rest too (a cat jumping up into its underside).
            if (Flips && !IsTimingEffectActive && FaceTouches(body.position, body.rotation)) { KillOwn(); return; }
            // PAX-114: a glitch wall's rearm brings it home: that's a jump too, killing a cat it appears on.
            if (Glitches && !IsTimingEffectActive) { ArriveGlitch(start); return; }
            if (!IsTimingEffectActive) return;
            int since = RoomLifeTick - LatestFireTick;
            if (Flips) { StepFlip(since); return; }
            if (Glitches) { StepGlitch(since); return; }
            ShowSpikes(since >= 0);
            // PAX-113 (D-178): a scanner warns at home first, harmless; its move starts warnTicks after the fire.
            ShowScan(since);
            if (Scans) { if (since < warnTicks) return; since -= warnTicks; }
            Vector2 position = PositionAt(since);
            // Physics has resolved the cat against this pose. Checking it before the next
            // MovePosition distinguishes a pinned cat from one the solid safely pushed clear.
            Bounds pose = PoseBounds(body.position, body.rotation);
            // D-119: a hinge floor swinging up tips the cat by physics; it neither crushes nor pushes until it stands.
            if (Hinges && since <= hingeTicks)
            {
                Displacement = position - body.position;
                body.MovePosition(position);
                body.MoveRotation(AngleAt(since));
                return;
            }
            if (kind == MovingTrapKind.Hazard)
            {
                Bounds nextPose = new(position + box.offset, box.size);
                if (IsLocalHumanOverlapping(nextPose, observers, filter, results, out _)) { Death.Kill(Reality.Id, DeathCause.Hazard, this); return; }
                body.MovePosition(position);
                return;
            }
            float depth = crushDepth > 0f ? crushDepth : crushConfig.DefaultCrushDepth;
            // PAX-107 (D-124): a spiked hinge floor, standing, kills a cat touching it where it is or where it moves to (before
            // any push). D-207: a spiked slam kills only by its spiked face, the strip under it; a cat can stand on its top.
            if (spikes != null && TryGetLocalHumanColliderBounds(observers, out Bounds touched)
                && (TrapMotion.Touches(touched, SpikedFace(pose), SpikeSkin) || TrapMotion.Touches(touched, SpikedFace(PoseBounds(position, body.rotation)), SpikeSkin)))
            { KillOwn(); return; }   // D-254: its own record, so the route validator names it (a slam's face is under its body)
            if (TryGetLocalHumanColliderBounds(observers, out Bounds catBounds) && TrapMotion.Crushes(catBounds, pose, depth)) { Death.Kill(Reality.Id, DeathCause.Hazard, this); return; }
            // PAX-107 (D-140): a slam coming down kills a cat it lands on, before physics can push the cat aside.
            descending = slams && position.y < body.position.y - 1e-4f;
            if (descending) { slamFrom = pose; slamTo = PoseBounds(position, body.rotation); }
            if (descending && TryGetLocalHumanColliderBounds(observers, out catBounds) && SlamHits(catBounds)) { Death.Kill(Reality.Id, DeathCause.Hazard, this); return; }
            // PAX-107 (D-123): a mover that carries the cat into its named crush partner (a floor rising into a ceiling) crushes
            // it there: physics lets a pinned cat sink into the static solid, never into the mover.
            if (crushPartner != null && TryGetLocalHumanColliderBounds(observers, out catBounds) && TrapMotion.Crushes(catBounds, crushPartner.bounds, depth)) { Death.Kill(Reality.Id, DeathCause.Hazard, this); return; }
            if (pushes && Push(position, pose, depth)) return;
            Displacement = position - body.position;
            body.MovePosition(position);
        }

        // PAX-115 (D-187): turn toward this tick's angle, killing a cat its spiked face touches where it is or where it turns to.
        // It isn't solid while it turns (a cat on it drops: a kinematic turn would fling it), and is solid again at rest and
        // flipped (spiked side up).
        void StepFlip(int since)
        {
            float angle = AngleAt(since);
            ShowFlipWarning(since);
            if (FaceTouches(body.position, body.rotation) || FaceTouches(body.position, angle)) { KillOwn(); return; }
            box.enabled = !Turning(angle);
            body.MoveRotation(angle);
        }

        // The flip's tell: its looks flicker (full on even ticks, half on odd) for flipWarnTicks from the fire; otherwise full.
        void ShowFlipWarning(int since)
        {
            if (!Flips || looks == null) return;
            bool warning = since >= 0 && since < flipWarnTicks;
            for (int i = 0; i < looks.Length; i++)
            {
                if (looks[i] == null) continue;
                Color c = lookColors[i];
                if (warning && since % 2 == 1) c.a *= .45f;
                looks[i].color = c;
            }
        }

        bool Turning(float angle) { float turn = Mathf.Abs(angle - startAngle); return turn > .01f && turn < 179.99f; }

        // PAX-114 (D-187): a glitch wall sits on its point, warns before each jump, and teleports: never carrying a rider, and
        // killing a cat it appears on (a Hazard on any overlap, a Solid by the crush depth).
        void StepGlitch(int since)
        {
            ArriveGlitch(PositionAt(since));
            ShowGlitch(since);   // after the jump: the ghost is a child, placed in world space
        }

        // PAX-114: a glitch wall teleports its drawn pose with its body (a reset and a rewind too), so nothing is left drawn where it was.
        void PlaceTransform(Vector2 position) => transform.position = new Vector3(position.x, position.y, transform.position.z);

        void ArriveGlitch(Vector2 position)
        {
            if ((position - body.position).sqrMagnitude < 1e-8f) return;
            Bounds arrival = PoseBounds(position, body.rotation);
            bool hit = TryGetLocalHumanColliderBounds(observers, out Bounds cat)
                && (kind == MovingTrapKind.Hazard ? TrapMotion.Touches(cat, arrival, 0f) : TrapMotion.Crushes(cat, arrival, crushDepth > 0f ? crushDepth : crushConfig.DefaultCrushDepth));
            // It appears where it lands either way: the death hold shows the cat crushed under it, never by a wall still elsewhere.
            body.position = position;
            PlaceTransform(position);
            if (hit) KillOwn();
        }

        /// <summary>PAX-114/115: this step's kill came from a flip's spiked face or a glitch wall's arrival (the route harness's
        /// attribution for the kills it can't recompute from the pose: the turn and the jump never happened).</summary>
        public bool OwnKill { get; private set; }
        void KillOwn() { OwnKill = true; Death.Kill(Reality.Id, DeathCause.Hazard, this); }

        bool Hinges => hingeTicks > 0;
        // PAX-107 (D-124): how near a spiked face counts as touching it (physics keeps a resting cat this close to a solid).
        public const float SpikeSkin = .03f;
        // PAX-107 (D-140): how much of a descending solid's top doesn't count as landing on the cat (a rider), as a falling block's
        // kill shrink (LevelLayoutValidator.BlockKillShrink).
        public const float SlamSkin = .04f;
        bool descending; Bounds slamFrom, slamTo;   // a slam's downward move this step (D-140), for the kill and the route harness
        /// <summary>D-140: a slam whose move this step was down lands on a cat with these bounds (the kill test, also the harness's).</summary>
        public bool SlamHits(Bounds catBounds) => descending && TrapMotion.SlamsOnto(catBounds, slamFrom, slamTo, SlamSkin);

        // D-119: the angle `ticks` after the fire: the start's, swung 90 degrees over hingeTicks (none without a hinge).
        float AngleAt(int ticks) => Flips ? startAngle + TrapPath.FlipAngle(ticks - flipWarnTicks, flipTicks, holdTicks, returnTicks)
            : !Hinges || ticks <= 0 ? startAngle : startAngle + hingeSign * 90f * TrapMotion.Progress(ticks, hingeTicks);

        // The body's position `ticks` after the fire: a hinge floor swings about its hinge, then moves by `offset` as any mover.
        Vector2 PositionAt(int ticks)
        {
            if (Flips) return start;
            if (HasPath) return start + TrapPath.Offset(path, glitch, ticks, moveTicks, holdTicks, returnTicks, pathFirstLegTicks);
            if (!Hinges) return start + (riseFirst ? TrapMotion.RiseFirstOffset(offset, ticks, moveTicks, holdTicks, returnTicks)
                : TrapMotion.MovingOffset(offset, ticks, moveTicks, holdTicks, returnTicks));
            Vector2 pivot = start + hingePivot;
            Vector2 swung = pivot + (Vector2)(Quaternion.Euler(0f, 0f, AngleAt(ticks) - startAngle) * (start - pivot));
            return swung + TrapMotion.MovingOffset(offset, ticks - hingeTicks, moveTicks, holdTicks, returnTicks);
        }

        // The box's world bounds at a body pose (D-119: turned with the body).
        Bounds PoseBounds(Vector2 position, float angle)
        {
            float r = angle * Mathf.Deg2Rad, c = Mathf.Abs(Mathf.Cos(r)), sn = Mathf.Abs(Mathf.Sin(r));
            Vector2 centre = position + (Vector2)(Quaternion.Euler(0f, 0f, angle) * box.offset);
            return new Bounds(centre, new Vector3(c * box.size.x + sn * box.size.y, sn * box.size.x + c * box.size.y));
        }

        // PAX-093 (D-095) Q6: a push wall moves a cat in its way flush against its leading edge (the kit decides the shift, not
        // a physics shove). A cat beside it, not on or under it, and ahead of its centre along the push is in its way. Pushed
        // into the named crush partner by the crush depth or more, the cat dies (D-055 (3)); true when it killed.
        bool Push(Vector2 next, Bounds pose, float depth)
        {
            const float eps = 1e-4f;
            float dx = next.x - body.position.x;
            int sign = dx > eps ? 1 : dx < -eps ? -1 : 0;
            if (sign == 0 || !TryGetLocalHumanColliderBounds(observers, out Bounds cat)) return false;
            if (cat.max.y <= pose.min.y + eps || cat.min.y >= pose.max.y - eps) return false;
            if (sign > 0 ? cat.center.x < pose.center.x : cat.center.x > pose.center.x) return false;
            Bounds nextPose = PoseBounds(next, body.rotation);
            float shift = MovingFloorMath.PushOut(sign > 0 ? nextPose.max.x : nextPose.min.x, sign > 0 ? cat.min.x : cat.max.x, sign);
            if (Mathf.Abs(shift) <= eps) return false;
            observers.Get(Reality.Id).Cat.ApplyPush(new Vector2(shift, 0f));
            cat.center += new Vector3(shift, 0f, 0f);
            if (crushPartner == null || !TrapMotion.Crushes(cat, crushPartner.bounds, depth)) return false;
            Death.Kill(Reality.Id, DeathCause.Hazard, this);
            return true;
        }

        protected override void OnReset() { ShowSpikes(false); ShowScan(-1); ShowGlitch(-1); ShowFlipWarning(-1); descending = false; OwnKill = false; if (box != null) box.enabled = true; if (Phases && box != null) ShowPhase(0, 0); if (body != null) { body.position = start; body.rotation = startAngle; if (Glitches) PlaceTransform(start); } }
        // PAX-114: a glitch wall stays where it is on its rearm; its next step brings it home (ArriveGlitch). PAX-122: a phase
        // platform's next step redraws it.
        protected override void OnTimingRearmed() { ShowSpikes(false); ShowScan(-1); ShowGlitch(-1); ShowFlipWarning(-1); if (box != null && !Phases) box.enabled = true; if (body != null && !Glitches) { body.position = start; body.rotation = startAngle; } }
        // PAX-090 (D-091) Q2: as FallingBlockTrap, the pose at roomTick from its formula.
        protected override void OnRestore(in TrapSnapshot snapshot, int roomTick)
        {
            if (body == null) return;
            int since = roomTick - LatestFireTick;
            ShowSpikes(IsTimingEffectActive && since >= 0);
            ShowScan(IsTimingEffectActive ? since : -1);
            int glitchSince = IsTimingEffectActive ? since : -1;
            if (Scans && IsTimingEffectActive) since = Mathf.Max(0, since - warnTicks);
            Vector2 pose = IsTimingEffectActive ? PositionAt(since) : start;
            float angle = IsTimingEffectActive ? AngleAt(since) : startAngle;
            if (Flips) { box.enabled = !Turning(angle); ShowFlipWarning(IsTimingEffectActive ? since : -1); }
            if (Phases) ShowPhase(PhaseAt(since), since);
            body.position = pose; body.rotation = angle;
            if (Glitches) { PlaceTransform(pose); ShowGlitch(glitchSince); }
            body.MovePosition(pose); body.MoveRotation(angle);
        }
    }
}
