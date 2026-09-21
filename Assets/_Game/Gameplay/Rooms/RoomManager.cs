using System;
using System.Collections.Generic;
using Parallax.Core;
using Parallax.Gameplay.Checkpoints;
using Parallax.Gameplay.Observers;
using UnityEngine;

namespace Parallax.Gameplay.Rooms
{
    public sealed class RoomManager : MonoBehaviour
    {
        [SerializeField] ObserverId soloReality = ObserverId.A;
        [SerializeField] CheckpointManager checkpoints;
        [SerializeField] ObserverSet observers;

        readonly RoomProgress progress = new RoomProgress();
        readonly Dictionary<int, RoomDoor> doors = new Dictionary<int, RoomDoor>();

        public int CurrentRoom => checkpoints != null ? checkpoints.Current : 0;
        public bool LevelComplete => progress.LevelComplete;
        public bool CurrentDoorTouched { get; private set; }

        public event Action<int> RoomCompleted;
        public event Action LevelCompleted;

        void OnEnable()
        {
            if (observers != null) observers.Stepped += OnStepped;
        }

        void OnDisable()
        {
            if (observers != null) observers.Stepped -= OnStepped;
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

        void OnStepped(int tick)
        {
            if (progress.LevelComplete || checkpoints == null) return;

            int room = checkpoints.Current;
            if (!doors.TryGetValue(room, out RoomDoor door))
            {
                CurrentDoorTouched = false;
                return;
            }

            ObserverContext observer = observers.Get(soloReality);
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
    }
}
