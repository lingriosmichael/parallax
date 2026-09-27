using System;
using System.Collections.Generic;
using Parallax.Core;
using Parallax.Gameplay.Checkpoints;
using Parallax.Gameplay.Observers;
using Parallax.Gameplay.Player;
using UnityEngine;

namespace Parallax.Gameplay.Rooms
{
    [Serializable]
    public struct RoomBoundsEntry
    {
        public int RoomId;
        public Vector2 Center;
        public Vector2 Size;

        public RoomBoundsEntry(int roomId, Vector2 center, Vector2 size)
        {
            RoomId = roomId; Center = center; Size = size;
        }
    }

    /// <summary>PAX-090 (D-091): one checkpoint section of a room, baked by SoloRoomBuilder in section order (section 0, the
    /// start, has no gate and no marker). World space: the gate box, and the spawn as a body position with its gravity.</summary>
    [Serializable]
    public struct RoomSectionEntry
    {
        public int RoomId;
        public string Name;
        public Vector2 GateCenter;
        public Vector2 GateSize;
        public Vector2 Spawn;
        public Vector2 Gravity;
        public SpriteRenderer Marker;

        public RoomSectionEntry(int roomId, string name, Vector2 gateCenter, Vector2 gateSize, Vector2 spawn, Vector2 gravity, SpriteRenderer marker)
        {
            RoomId = roomId; Name = name; GateCenter = gateCenter; GateSize = gateSize; Spawn = spawn; Gravity = gravity; Marker = marker;
        }
    }

    public sealed class RoomManager : MonoBehaviour
    {
        [SerializeField] ObserverId soloReality = ObserverId.A;
        [SerializeField] CheckpointManager checkpoints;
        [SerializeField] ObserverSet observers;
        [SerializeField] RoomDeath roomDeath;
        [SerializeField] RoomBoundsEntry[] bounds = Array.Empty<RoomBoundsEntry>();
        [SerializeField] RoomSectionEntry[] sections = Array.Empty<RoomSectionEntry>();
        [SerializeField] Color sectionMarkerLit = Color.white;

        readonly RoomProgress progress = new RoomProgress();
        readonly Dictionary<int, RoomDoor> doors = new Dictionary<int, RoomDoor>();
        readonly List<RoomTrap> traps = new List<RoomTrap>();
        readonly List<Hazard> hazards = new List<Hazard>();
        readonly List<RoomTrap> trapSnapshot = new List<RoomTrap>();
        readonly List<Hazard> hazardSnapshot = new List<Hazard>();

        public int CurrentRoom => checkpoints != null ? checkpoints.Current : 0;
        public ObserverId SoloReality => soloReality;
        public bool LevelComplete => progress.LevelComplete;
        public bool CurrentDoorTouched { get; private set; }
        public int RoomLifeTick { get; private set; }
        int roomLifeRoom = -1;
        bool roomLifeStartsNextStep;
        readonly RoomSections roomSections = new RoomSections();

        public event Action<int> RoomCompleted;
        public event Action LevelCompleted;

        public bool IsLive(int roomId) => !progress.LevelComplete && roomId == CurrentRoom;

        // PAX-090 (D-091): the live room's checkpoint sections (0 with none).
        public int SectionCount => roomSections.For(CurrentRoom, sections).Count;
        public int CurrentSection => roomSections.For(CurrentRoom, sections).Current;
        public int SectionDeaths(int section) => roomSections.For(CurrentRoom, sections).DeathsIn(section);
        public int SectionIndex(string name) => roomSections.IndexOf(CurrentRoom, sections, name);

        // PAX-049 (D-061): level order is the ascending sort of the same registered-door ids
        // RoomSequence.HasNext already reads; this exposes that single source of truth instead
        // of inventing a second one. Room NUMBER (1-based display position) is the caller's job
        // (its index in this list + 1), since it is deliberately not the same as RoomId.
        public IReadOnlyList<int> RoomIdsInOrder()
        {
            var ids = new List<int>(doors.Keys);
            ids.Sort();
            return ids;
        }

        public bool TryGetBounds(int roomId, out Bounds roomBounds)
        {
            for (int i = 0; i < bounds.Length; i++)
            {
                if (bounds[i].RoomId != roomId) continue;
                roomBounds = new Bounds(bounds[i].Center, bounds[i].Size);
                return true;
            }
            roomBounds = default;
            return false;
        }

        // Room bounds are authored/stored in 2D (Vector2 Center/Size), so their z extent is
        // always [0,0]; Bounds.Contains also tests z, which would reject any point whose z
        // isn't exactly 0. Rooms have no z axis, so the check must ignore it.
        public static bool ContainsXY(Bounds bounds, Vector2 point) =>
            point.x >= bounds.min.x && point.x <= bounds.max.x && point.y >= bounds.min.y && point.y <= bounds.max.y;

        void OnEnable()
        {
            if (observers != null) observers.Stepped += OnStepped;
            if (roomDeath != null) roomDeath.Died += OnDied;
        }

        void OnDisable()
        {
            if (observers != null) observers.Stepped -= OnStepped;
            if (roomDeath != null) roomDeath.Died -= OnDied;
        }

        public void Register(RoomDoor door)
        {
            if (door.Reality != soloReality)
            {
                Debug.LogError($"RoomManager: door {door.RoomId} ('{door.name}') is in reality {door.Reality}, not solo reality {soloReality}; ignored.", door);
                return;
            }
            if (doors.ContainsKey(door.RoomId))
            {
                Debug.LogError($"RoomManager: duplicate door id {door.RoomId} ('{door.name}').", door);
                return;
            }
            doors.Add(door.RoomId, door);
        }

        public void Unregister(RoomDoor door)
        {
            if (doors.TryGetValue(door.RoomId, out RoomDoor registered) && registered == door) doors.Remove(door.RoomId);
        }

        public void RegisterTrap(RoomTrap trap)
        {
            if (trap != null && !traps.Contains(trap)) traps.Add(trap);
        }

        public void UnregisterTrap(RoomTrap trap) => traps.Remove(trap);

        public void RegisterHazard(Hazard hazard)
        {
            if (hazard != null && !hazards.Contains(hazard)) hazards.Add(hazard);
        }

        public void UnregisterHazard(Hazard hazard) => hazards.Remove(hazard);

        // PAX-093 (D-095): after every trap stepped (so a launch this tick has already cleared the grounding) and before the
        // hazards (so a cat carried into spikes dies this tick): a LocalHuman cat grounded on a Carry MovingTrap moves with
        // this tick's displacement (CatMotor2D.ApplyCarry). Nothing for any other surface, so no pin moves without one.
        void CarryOnMovingFloor()
        {
            ObserverContext observer = observers != null ? observers.Get(soloReality) : null;
            if (observer == null || observer.Cat == null || observer.Driver == null || observer.Driver.Kind != InputSourceKind.LocalHuman) return;
            CatMotor2D motor = observer.Cat;
            if (!motor.IsGrounded || motor.GroundCollider == null) return;
            MovingTrap floor = motor.GroundCollider.GetComponent<MovingTrap>();
            if (floor == null || floor.Motion != SurfaceMotion.Carry || floor.DisplacementTick != RoomLifeTick) return;
            motor.ApplyCarry(floor.Displacement, TickTime.SecondsPerTick);
        }

        void OnStepped(int tick)
        {
            if (progress.LevelComplete || checkpoints == null) return;

            // PAX-047 (D-058): while a death hold is running, the room is frozen — no
            // RoomLifeTick advance, no trap/hazard step, no bounds check, no door check.
            // RoomDeath.StepHold runs the deferred reset itself once the hold ends; that
            // reset's Died event (unchanged, see OnDied) is what starts the next live tick
            // at RoomLifeTick 0, so nothing here needs to react to the hold ending.
            if (roomDeath != null && roomDeath.IsHolding)
            {
                roomDeath.StepHold();
                return;
            }

            if (roomLifeRoom != checkpoints.Current || roomLifeStartsNextStep) { roomLifeRoom = checkpoints.Current; RoomLifeTick = 0; roomLifeStartsNextStep = false; }
            else RoomLifeTick++;

            // The one ordered room tick is traps -> hazards -> bounds -> door. Components
            // never subscribe independently, so a dead cat cannot complete a door on this
            // same tick.
            trapSnapshot.Clear();
            trapSnapshot.AddRange(traps);
            for (int i = 0; i < trapSnapshot.Count; i++)
            {
                trapSnapshot[i].StepIfLive();
                if (roomDeath != null && roomDeath.WasKilledThisTick(soloReality, tick)) return;
            }
            CarryOnMovingFloor();
            hazardSnapshot.Clear();
            hazardSnapshot.AddRange(hazards);
            for (int i = 0; i < hazardSnapshot.Count; i++) hazardSnapshot[i].KillOverlappingCat();

            ObserverContext observer = observers.Get(soloReality);
            if (roomDeath != null && !roomDeath.WasKilledThisTick(soloReality, tick)
                && observer != null && observer.Cat != null && TryGetBounds(checkpoints.Current, out Bounds roomBounds))
            {
                Collider2D catCollider = observer.Cat.GetComponent<Collider2D>();
                // xy only: bounds are authored/stored as 2D (Vector2 Center/Size, z always 0),
                // so a plain Bounds.Contains would fail on any non-zero cat z. Rooms have no z.
                if (catCollider != null && !ContainsXY(roomBounds, catCollider.bounds.center))
                {
                    Debug.LogWarning($"RoomManager: cat left room {checkpoints.Current} bounds at {catCollider.bounds.center}.", this);
                    roomDeath.Kill(soloReality, DeathCause.OutOfBounds);
                }
            }

            // PAX-090 (D-091): the next gate, after every kill check of this tick (a dead cat passes no gate) and before
            // the door. Its snapshot is the room as this tick's traps left it, at this RoomLifeTick.
            if (roomDeath != null && !roomDeath.WasKilledThisTick(soloReality, tick) && observer != null && observer.Driver != null
                && RoomPolicy.CompletesRoom(observer.Driver.Kind) && observer.Cat != null)
                roomSections.TryPassGate(checkpoints.Current, sections, CatBodyCollider.Of(observer.Cat), traps, RoomLifeTick, sectionMarkerLit, this);

            int room = checkpoints.Current;
            if (!doors.TryGetValue(room, out RoomDoor door))
            {
                CurrentDoorTouched = false;
                return;
            }

            if (roomDeath != null && roomDeath.WasKilledThisTick(soloReality, tick))
            {
                CurrentDoorTouched = false;
                return;
            }
            bool touched = observer != null && observer.Driver != null
                && RoomPolicy.CompletesRoom(observer.Driver.Kind)
                && door.IsTouchedBy(observer);
            CurrentDoorTouched = touched;
            if (!touched || !progress.TryComplete(room)) return;

            Debug.Log($"Room {room} complete");
            roomSections.LogCompleted(room, sections);
            if (RoomCompleted != null) RoomCompleted.Invoke(room);

            if (RoomSequence.HasNext(room, doors.Keys))
            {
                checkpoints.Activate(room + 1);
                checkpoints.Respawn(observer);
            }
            else
            {
                progress.MarkLevelComplete();
                // PAX-049 (D-061): replaces the old bare "Level complete" line with the
                // PARALLAX_STATS summary (per-room deaths in level order, plus the total).
                if (roomDeath != null)
                {
                    RoomSummaryRow[] rows = LevelSummary.BuildRows(RoomIdsInOrder(), roomDeath.DeathsIn);
                    Debug.Log(LevelStatsLog.LevelComplete(rows, LevelSummary.Total(rows)));
                }
                else
                {
                    Debug.Log("Level complete");
                }
                if (LevelCompleted != null) LevelCompleted.Invoke();
            }
        }

        void OnDied(DeathInfo death)
        {
            // PAX-090 (D-091) Q5: a rewind already put RoomLifeTick back at the gate tick; the next live tick continues it.
            if (roomSections.ConsumeRewound()) return;
            if (death.Room == CurrentRoom) roomLifeStartsNextStep = true;
        }

        /// <summary>PAX-090 (D-091): asked by RoomDeath's reset. True with a gate passed in this room, with the current section's
        /// spawn; nothing changes yet. Otherwise false, and today's reset stands.</summary>
        public bool TryRewind(int roomId, out Vector2 spawn, out Vector2 gravity)
        {
            spawn = default; gravity = default;
            if (roomId != CurrentRoom || !roomSections.TryGetCheckpoint(roomId, sections, out RoomSectionEntry entry)) return false;
            spawn = entry.Spawn; gravity = entry.Gravity;
            return true;
        }

        /// <summary>PAX-090 (D-091) Q5: after RoomDeath's ordinary reset, RoomLifeTick goes back to the gate tick (now, so the
        /// next motor step already reads it) and every trap snapshotted at the gate is restored.</summary>
        public void RestoreRewind()
        {
            RoomLifeTick = roomSections.GateTick;
            roomSections.Rewind();
        }

        /// <summary>PAX-090 (D-091) R6: a death in the live room, counted in its current section.</summary>
        public void RecordSectionDeath(int roomId)
        {
            if (roomId == CurrentRoom) roomSections.For(roomId, sections).RecordDeath();
        }

        // PAX-090 (D-091): the live room's sections, its gate snapshot, and whether the last reset was a rewind. Rebuilt when
        // the live room changes; with no entries for the room it holds nothing and every call is a no-op.
        sealed class RoomSections
        {
            readonly SectionProgress progress = new SectionProgress();
            readonly List<int> entryIndices = new List<int>();
            readonly List<RoomTrap> snapshotTraps = new List<RoomTrap>();
            readonly List<TrapSnapshot> snapshotStates = new List<TrapSnapshot>();
            int room = int.MinValue;
            int gateTick;
            bool rewound;

            public SectionProgress For(int roomId, RoomSectionEntry[] entries)
            {
                Ensure(roomId, entries);
                return progress;
            }

            public int IndexOf(int roomId, RoomSectionEntry[] entries, string name)
            {
                Ensure(roomId, entries);
                for (int i = 0; i < entryIndices.Count; i++) if (entries[entryIndices[i]].Name == name) return i;
                return -1;
            }

            public void TryPassGate(int roomId, RoomSectionEntry[] entries, Collider2D cat, List<RoomTrap> traps, int roomTick, Color lit, UnityEngine.Object context)
            {
                Ensure(roomId, entries);
                int next = progress.Current + 1;
                if (cat == null || next >= entryIndices.Count) return;
                RoomSectionEntry entry = entries[entryIndices[next]];
                Bounds b = cat.bounds;
                Vector2 min = entry.GateCenter - entry.GateSize * .5f, max = entry.GateCenter + entry.GateSize * .5f;
                if (b.max.x <= min.x || b.min.x >= max.x || b.max.y <= min.y || b.min.y >= max.y) return;
                if (!progress.TryEnter(next)) return;

                snapshotTraps.Clear();
                snapshotStates.Clear();
                for (int i = 0; i < traps.Count; i++)
                {
                    if (traps[i] == null || traps[i].RoomId != roomId) continue;
                    snapshotTraps.Add(traps[i]);
                    snapshotStates.Add(traps[i].Capture());
                }
                gateTick = roomTick;
                if (entry.Marker != null) entry.Marker.color = lit;
                Debug.Log($"Room {roomId}: section '{entry.Name}' reached at room tick {roomTick}.", context);
            }

            public bool TryGetCheckpoint(int roomId, RoomSectionEntry[] entries, out RoomSectionEntry entry)
            {
                Ensure(roomId, entries);
                entry = progress.HasCheckpoint ? entries[entryIndices[progress.Current]] : default;
                return progress.HasCheckpoint;
            }

            public int GateTick => gateTick;

            public void Rewind()
            {
                for (int i = 0; i < snapshotTraps.Count; i++) if (snapshotTraps[i] != null) snapshotTraps[i].Restore(snapshotStates[i], gateTick);
                rewound = true;
            }

            public bool ConsumeRewound()
            {
                bool was = rewound;
                rewound = false;
                return was;
            }

            public void LogCompleted(int roomId, RoomSectionEntry[] entries)
            {
                Ensure(roomId, entries);
                if (entryIndices.Count == 0) return;
                var names = new string[entryIndices.Count];
                for (int i = 0; i < names.Length; i++) names[i] = entries[entryIndices[i]].Name;
                Debug.Log(progress.Format(roomId, names));
            }

            void Ensure(int roomId, RoomSectionEntry[] entries)
            {
                if (roomId == room) return;
                room = roomId;
                entryIndices.Clear();
                snapshotTraps.Clear();
                snapshotStates.Clear();
                rewound = false;
                if (entries != null) for (int i = 0; i < entries.Length; i++) if (entries[i].RoomId == roomId) entryIndices.Add(i);
                progress.Begin(entryIndices.Count);
            }
        }
    }
}
