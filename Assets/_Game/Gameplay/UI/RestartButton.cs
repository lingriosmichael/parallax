using Parallax.Gameplay.Input;
using Parallax.Gameplay.Levels;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Parallax.Gameplay.UI
{
    /// <summary>PAX-049 (D-061): reloads the active level scene. Counts reset with it; nothing
    /// is saved. Implements ITouchReservedRegion so the stick and jump never claim a touch that
    /// starts on it. PAX-053 (D-072) §2.2: routes through LevelSceneLoader, the one seam every
    /// level/menu scene change goes through.</summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class RestartButton : MonoBehaviour, ITouchReservedRegion
    {
        [SerializeField] Button button;

        void OnEnable() { if (button != null) button.onClick.AddListener(Restart); }
        void OnDisable() { if (button != null) button.onClick.RemoveListener(Restart); }

        public void Restart() => LevelSceneLoader.Load(SceneManager.GetActiveScene().name);

        // The button lives under a panel that starts inactive, so its own Awake may never have
        // run by the time a touch is checked against it -- reading (RectTransform)transform
        // directly instead of a cached field avoids depending on Awake timing at all. Hidden
        // (or otherwise disabled) never reserves screen space.
        public bool ContainsScreenPoint(Vector2 screenPos) =>
            isActiveAndEnabled && RectTransformUtility.RectangleContainsScreenPoint((RectTransform)transform, screenPos, null);
    }
}
