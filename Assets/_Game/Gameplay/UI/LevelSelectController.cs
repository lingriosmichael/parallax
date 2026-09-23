using System.Collections.Generic;
using Parallax.Core;
using Parallax.Gameplay.Levels;
using UnityEngine;

namespace Parallax.Gameplay.UI
{
    /// <summary>PAX-053 (D-063) §2.3: builds one LevelSelectRow per LevelListConfig entry at
    /// runtime, in list order - never a hand-placed button per level, so this works unchanged for
    /// 50 levels. Reads progress the same way LevelCompleteScreen/NextLevelButton do
    /// (LevelProgressStore, PAX-050); defines no new unlock rules.</summary>
    public sealed class LevelSelectController : MonoBehaviour
    {
        [SerializeField] LevelListConfig levelList;
        [SerializeField] RectTransform rowsContainer;
        [SerializeField] LevelSelectRow rowTemplate;

        readonly List<GameObject> spawnedRows = new List<GameObject>();

        void Start() => BuildRows();

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

            LevelSelectEntry[] entries = LevelSelectModel.Build(levels, progress);
            for (int i = 0; i < entries.Length; i++)
            {
                LevelSelectRow row = Instantiate(rowTemplate, rowsContainer);
                row.gameObject.SetActive(true);
                row.Configure(entries[i]);
                spawnedRows.Add(row.gameObject);
            }
        }
    }
}
