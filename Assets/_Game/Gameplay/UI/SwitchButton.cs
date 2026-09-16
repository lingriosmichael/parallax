using Parallax.Core;
using Parallax.Gameplay.Input;
using Parallax.Gameplay.Observers;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Parallax.Gameplay.UI
{
    [RequireComponent(typeof(RectTransform))]
    public sealed class SwitchButton : MonoBehaviour, ITouchReservedRegion
    {
        [SerializeField] SoloSwitchController switchController;
        [SerializeField] Button button;
        [SerializeField] Text label;

        RectTransform rectTransform;

        void Awake()
        {
            rectTransform = (RectTransform)transform;
        }

        void OnEnable()
        {
            if (button != null) button.onClick.AddListener(OnClick);
            if (switchController != null) switchController.Switched += OnSwitched;

            RefreshLabel();
        }

        void OnDisable()
        {
            if (button != null) button.onClick.RemoveListener(OnClick);
            if (switchController != null) switchController.Switched -= OnSwitched;
        }

        void OnClick()
        {
            if (switchController != null) switchController.Toggle();

            // Clear UI selection so Space/Enter (UI Submit) can't re-trigger SWITCH.
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        }

        void OnSwitched(ObserverId from, ObserverId to)
        {
            RefreshLabel();
        }

        void RefreshLabel()
        {
            if (label == null || switchController == null) return;

            label.text = switchController.Active == ObserverId.A ? "<b>A</b>  |  b" : "a  |  <b>B</b>";
        }

        public bool ContainsScreenPoint(Vector2 screenPos)
        {
            return RectTransformUtility.RectangleContainsScreenPoint(rectTransform, screenPos, null);
        }
    }
}
