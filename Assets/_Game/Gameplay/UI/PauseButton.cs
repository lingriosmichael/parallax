using Parallax.Gameplay.Input;
using Parallax.Gameplay.Levels;
using UnityEngine;
using UnityEngine.UI;

namespace Parallax.Gameplay.UI
{
    /// <summary>PAX-054 (D-073): the HUD pause button, top-right. Implements ITouchReservedRegion
    /// so the stick and jump never claim a touch that starts on it (D-023). Hidden by
    /// LevelCompleteScreen when the level completes; hidden never reserves screen space.</summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class PauseButton : MonoBehaviour, ITouchReservedRegion
    {
        [SerializeField] Button button;
        [SerializeField] LevelPause levelPause;

        void OnEnable() { if (button != null) button.onClick.AddListener(Press); }
        void OnDisable() { if (button != null) button.onClick.RemoveListener(Press); }

        public void Press()
        {
            if (levelPause == null)
            {
                Debug.LogError($"PauseButton '{name}' has no LevelPause assigned.", this);
                return;
            }
            levelPause.Pause();
        }

        public bool ContainsScreenPoint(Vector2 screenPos) =>
            isActiveAndEnabled && RectTransformUtility.RectangleContainsScreenPoint((RectTransform)transform, screenPos, null);
    }
}
