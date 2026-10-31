using System.Collections.Generic;
using Parallax.Core;
using Parallax.Gameplay.Levels;
using UnityEngine;

namespace Parallax.Gameplay.UI
{
    /// <summary>PAX-053 (D-063) §2.3: builds one LevelSelectRow per LevelListConfig entry at
    /// runtime, in list order - never a hand-placed button per level, so this works unchanged for
    /// 50 levels. Reads progress the same way LevelCompleteScreen/NextLevelButton do
    /// (LevelProgressStore, PAX-050); defines no new unlock rules. PAX-066 (D-279): with an UnlockPanel wired, levels past the free ones
    /// show their lock, and the rows rebuild when the panel closes.</summary>
    public sealed class LevelSelectController : MonoBehaviour
    {
        [SerializeField] LevelListConfig levelList;
        [SerializeField] RectTransform rowsContainer;
        [SerializeField] LevelSelectRow rowTemplate;
        [SerializeField] UnlockPanel unlockPanel;
        [SerializeField] StoreHost store;

        readonly List<GameObject> spawnedRows = new List<GameObject>();

        bool shownPaid;

        void Start()
        {
            BuildRows();
            if (store != null && store.Service != null) store.Service.Changed += OnStoreChanged;
        }

        void OnDestroy()
        {
            if (store != null && store.Service != null) store.Service.Changed -= OnStoreChanged;
        }

        // The launch check answers after the rows are built: a restored purchase or a refund shows at once.
        void OnStoreChanged()
        {
            if (LevelProgressStore.PaidUnlocked != shownPaid) BuildRows();
        }

        void ShowUnlock(LevelSelectEntry entry) => unlockPanel.Show(entry.Id, entry.SceneName, BuildRows);

        void BuildRows()
        {
            for (int i = 0; i < spawnedRows.Count; i++) if (spawnedRows[i] != null) Destroy(spawnedRows[i]);
            spawnedRows.Clear();

            if (levelList == null || rowsContainer == null || rowTemplate == null) return;

            IReadOnlyList<string> orderedIds = levelList.OrderedIds();
            LevelProgress progress = LevelProgressStore.Load(orderedIds);

            var levels = new (string Id, string SceneName, string DisplayName)[levelList.Levels.Count];
            for (int i = 0; i < levelList.Levels.Count; i++)
            {
                LevelEntry entry = levelList.Levels[i];
                levels[i] = (entry.Id, entry.SceneName, entry.DisplayName);
            }

            shownPaid = LevelProgressStore.PaidUnlocked;
            bool gated = unlockPanel != null && store != null && store.Config != null;
            Entitlements access = gated ? LevelAccess.Load(levelList, store.Config, progress) : null;
            LevelSelectEntry[] entries = LevelSelectModel.Build(levels, progress, access, LevelAccess.UtcNow());
            for (int i = 0; i < entries.Length; i++)
            {
                LevelSelectRow row = Instantiate(rowTemplate, rowsContainer);
                row.gameObject.SetActive(true);
                row.Configure(entries[i], gated ? ShowUnlock : null);
                spawnedRows.Add(row.gameObject);
            }
        }
    }
}
