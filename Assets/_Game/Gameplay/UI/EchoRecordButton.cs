using Parallax.Gameplay.Echo;
using Parallax.Gameplay.Input;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Parallax.Gameplay.UI
{
    [RequireComponent(typeof(RectTransform))]
    public sealed class EchoRecordButton : MonoBehaviour, ITouchReservedRegion
    {
        [SerializeField] EchoSession echoSession;
        [SerializeField] Button button;
        [SerializeField] Text label;
        RectTransform rectTransform;
        EchoRecordState displayedState;
        int displayedTenths = -1;

        void Awake() => rectTransform = (RectTransform)transform;
        void OnEnable() { if (button != null) button.onClick.AddListener(OnClick); }
        void OnDisable() { if (button != null) button.onClick.RemoveListener(OnClick); }
        void Update() => RefreshLabel();
        void OnClick()
        {
            if (echoSession != null) echoSession.ToggleRecord();
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        }
        void RefreshLabel()
        {
            if (label == null || echoSession == null) return;
            EchoRecordState state = echoSession.State;
            int tenths = Mathf.FloorToInt(echoSession.RecordedSeconds * 10f);
            if (state == displayedState && tenths == displayedTenths) return;
            displayedState = state;
            displayedTenths = tenths;
            label.text = state == EchoRecordState.Idle ? "REC"
                : state == EchoRecordState.Recording ? $"REC {tenths / 10f:F1}"
                : state == EchoRecordState.Full ? "REC FULL" : "REC --";
        }
        public bool ContainsScreenPoint(Vector2 screenPos) => RectTransformUtility.RectangleContainsScreenPoint(rectTransform, screenPos, null);
    }
}
