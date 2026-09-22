using System;
using System.Collections.Generic;
using Parallax.Core;
using Parallax.Gameplay.Checkpoints;
using Parallax.Gameplay.Observers;
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

    public sealed class RoomManager : MonoBehaviour
    {
        [SerializeField] ObserverId soloReality = ObserverId.A;
        [SerializeField] CheckpointManager checkpoints;
        [SerializeField] ObserverSet observers;
        [SerializeField] RoomDeath roomDeath;
        [SerializeField] RoomBoundsEntry[] bounds = Array.Empty<RoomBoundsEntry>();

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

        public event Action<int> RoomCompleted;
        public event Action LevelCompleted;

        public bool IsLive(int roomId) => !progress.LevelComplete && roomId == CurrentRoom;

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
            if (RoomCompleted != null) RoomCompleted.Invoke(room);

            if (RoomSequence.HasNext(room, doors.Keys))
            {
                checkpoints.Activate(room + 1);
                checkpoints.Respawn(observer);
            }
            else
            {
                progress.MarkLevelComplete();
                Debug.Log("Level complete");
                if (LevelCompleted != null) LevelCompleted.Invoke();
            }
        }

        void OnDied(DeathInfo death)
        {
            if (death.Room == CurrentRoom) roomLifeStartsNextStep = true;
        }
    }
}
