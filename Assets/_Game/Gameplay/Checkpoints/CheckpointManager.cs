using System;
using System.Collections.Generic;
using Parallax.Core;
using Parallax.Gameplay.GravityControl;
using Parallax.Gameplay.Observers;
using Parallax.Gameplay.Player;
using Parallax.Gameplay.Reality;
using UnityEngine;

namespace Parallax.Gameplay.Checkpoints
{
    public sealed class CheckpointManager : MonoBehaviour
    {
        [SerializeField] RealityRoot rootA;
        [SerializeField] RealityRoot rootB;

        readonly CheckpointProgress progress = new CheckpointProgress();
        readonly CheckpointSpawnTable spawnTable = new CheckpointSpawnTable();
        readonly HashSet<int> registeredEntries = new HashSet<int>();
        bool noSpawnLogged;

        public int Current => progress.Current;

        public event Action<int> Activated;

        public void Register(CheckpointMarker marker)
        {
            int key = (marker.Id * 2) + (int)marker.Reality;
            if (!registeredEntries.Add(key))
            {
                Debug.LogError($"CheckpointManager: duplicate checkpoint id {marker.Id} in reality {marker.Reality} ('{marker.name}').", marker);
                return;
            }
            spawnTable.Set(marker.Id, marker.Reality, marker.LocalPosition, marker.GravityDirection);
        }

        public void Unregister(CheckpointMarker marker)
        {
            registeredEntries.Remove((marker.Id * 2) + (int)marker.Reality);
        }

        public void Activate(int id)
        {
            if (!progress.TryAdvance(id)) return;
            Debug.Log($"Checkpoint {id} reached");
            if (Activated != null) Activated.Invoke(id);
        }

        public bool TryGetSpawn(ObserverId observer, out Vector2 worldPosition, out Vector2 gravity)
        {
            worldPosition = default;
            gravity = default;

            if (!spawnTable.TryGet(progress.Current, observer, out Vector2 localPosition, out gravity)) return false;

            RealityRoot root = observer == ObserverId.A ? rootA : rootB;
            if (root == null)
            {
                Debug.LogError($"CheckpointManager: no RealityRoot assigned for {observer}.", this);
                return false;
            }

            worldPosition = root.ToWorld(localPosition);
            return true;
        }

        public void Respawn(ObserverContext observer)
        {
            if (observer == null || observer.Cat == null) return;

            if (!TryGetSpawn(observer.Id, out Vector2 worldPosition, out Vector2 gravity))
            {
                if (!noSpawnLogged)
                {
                    noSpawnLogged = true;
                    Debug.LogError($"CheckpointManager: no spawn available for {observer.Id} at checkpoint {progress.Current}.", this);
                }
                return;
            }

            CatSeat seat = observer.Cat.GetComponent<CatSeat>();
            if (seat != null && seat.IsSeated) seat.Release();

            CatRespawn respawn = observer.Cat.GetComponent<CatRespawn>();
            if (respawn == null)
            {
                Debug.LogError($"CheckpointManager: cat '{observer.Cat.name}' has no CatRespawn.", this);
                return;
            }

            respawn.RespawnAt(worldPosition, gravity);
        }
    }
}
