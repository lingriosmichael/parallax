using System.Collections.Generic;
using Parallax.Core;
using Parallax.Gameplay.Checkpoints;
using Parallax.Gameplay.Levels;
using Parallax.Gameplay.Observers;
using Parallax.Gameplay.Rooms;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Parallax.Gameplay.UI
{
    /// <summary>PAX-049 (D-061): reacts to RoomManager's events. Freezes the solo cat and shows
    /// the level-complete summary; logs one PARALLAX_STATS room_clear line per room (the
    /// level_complete summary line itself is logged by RoomManager, in place of its old bare
    /// "Level complete" message). Pure row/total/format math lives in Parallax.Core
    /// (LevelSummary, LevelStatsLog); this script only reacts to events and drives uGUI, so it
    /// stays a thin Parallax.Gameplay MonoBehaviour.
    /// PAX-050 (D-063): also shows a Next level button when LevelListConfig has an entry after
    /// the active scene's level; hidden otherwise (today: always, since only one level exists).
    /// Recording completion into LevelProgress happens on that button's click, not here — see
    /// NextLevelButton.</summary>
    public sealed class LevelCompleteScreen : MonoBehaviour
    {
        [SerializeField] RoomManager rooms;
        [SerializeField] RoomDeath roomDeath;
        [SerializeField] CheckpointManager checkpoints;
        [SerializeField] ObserverSet observers;
        [SerializeField] GameObject panel;
        [SerializeField] RectTransform rowsContainer;
        [SerializeField] Text rowTemplate;
        [SerializeField] Text totalText;
        [SerializeField] RestartButton restartButton;
        [SerializeField] LevelListConfig levelList;
        [SerializeField] NextLevelButton nextLevelButton;

        readonly Dictionary<int, int> roomStartTick = new Dictionary<int, int>();
        readonly List<GameObject> spawnedRows = new List<GameObject>();

        public bool IsShown { get; private set; }
        public RoomSummaryRow[] LastSummary { get; private set; }

        void Start()
        {
            if (rooms != null) roomStartTick[rooms.CurrentRoom] = CurrentTick();
        }

        void OnEnable()
        {
            if (rooms != null) { rooms.RoomCompleted += OnRoomCompleted; rooms.LevelCompleted += OnLevelCompleted; }
            if (checkpoints != null) checkpoints.Activated += OnRoomBecameCurrent;
        }

        void OnDisable()
        {
            if (rooms != null) { rooms.RoomCompleted -= OnRoomCompleted; rooms.LevelCompleted -= OnLevelCompleted; }
            if (checkpoints != null) checkpoints.Activated -= OnRoomBecameCurrent;
        }

        void Update()
        {
#if UNITY_EDITOR
            if (!IsShown || restartButton == null) return;
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame))
                restartButton.Restart();
#endif
        }

        int CurrentTick() => observers != null ? observers.Tick : 0;

        void OnRoomBecameCurrent(int roomId)
        {
            if (!roomStartTick.ContainsKey(roomId)) roomStartTick[roomId] = CurrentTick();
        }

        void OnRoomCompleted(int roomId)
        {
            if (rooms == null || roomDeath == null) return;
            int startTick = roomStartTick.TryGetValue(roomId, out int t) ? t : CurrentTick();
            int ticks = CurrentTick() - startTick;
            Debug.Log(LevelStatsLog.RoomClear(RoomNumberOf(roomId), roomDeath.DeathsIn(roomId), ticks));
        }

        int RoomNumberOf(int roomId)
        {
            IReadOnlyList<int> ids = rooms.RoomIdsInOrder();
            for (int i = 0; i < ids.Count; i++) if (ids[i] == roomId) return i + 1;
            return 0;
        }

        void OnLevelCompleted()
        {
            if (rooms == null) return;

            ObserverContext solo = observers != null ? observers.Get(rooms.SoloReality) : null;
            if (solo != null && solo.Cat != null) solo.Cat.Freeze();

            LastSummary = roomDeath != null
                ? LevelSummary.BuildRows(rooms.RoomIdsInOrder(), roomDeath.DeathsIn)
                : System.Array.Empty<RoomSummaryRow>();
            int total = LevelSummary.Total(LastSummary);

            IsShown = true;
            if (panel != null) panel.SetActive(true);
            PopulateRows(LastSummary, total);
            ConfigureNextLevelButton(total);
        }

        void ConfigureNextLevelButton(int total)
        {
            if (nextLevelButton == null) return;

            bool hasNext = false;
            if (levelList != null
                && levelList.TryGetBySceneName(SceneManager.GetActiveScene().name, out LevelEntry current)
                && levelList.TryGetNext(current.Id, out LevelEntry next))
            {
                nextLevelButton.Configure(current.Id, total, next.SceneName, levelList.OrderedIds());
                hasNext = true;
            }

            nextLevelButton.gameObject.SetActive(hasNext);
        }

        void PopulateRows(RoomSummaryRow[] rowsData, int total)
        {
            for (int i = 0; i < spawnedRows.Count; i++) if (spawnedRows[i] != null) Destroy(spawnedRows[i]);
            spawnedRows.Clear();

            if (rowTemplate != null && rowsContainer != null)
            {
                for (int i = 0; i < rowsData.Length; i++)
                {
                    Text row = Instantiate(rowTemplate, rowsContainer);
                    row.gameObject.SetActive(true);
                    row.text = $"Room {rowsData[i].RoomNumber} — {rowsData[i].Deaths} death{(rowsData[i].Deaths == 1 ? "" : "s")}";
                    spawnedRows.Add(row.gameObject);
                }
            }

            if (totalText != null) totalText.text = $"Total: {total} death{(total == 1 ? "" : "s")}";
        }
    }
}
