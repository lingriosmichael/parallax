using Parallax.Core;
using Parallax.Gameplay.Echo;
using Parallax.Gameplay.Observers;
using UnityEngine;
using UnityEngine.UI;

namespace Parallax.Gameplay.UI
{
    public sealed class EchoTimelineView : MonoBehaviour
    {
        [SerializeField] ObserverSet observers;
        [SerializeField] SoloSwitchController switchController;
        [SerializeField] GameObject bar;
        [SerializeField] Image fill;
        [SerializeField] Text label;

        bool displayed;
        float displayedFill = -1f;
        int displayedCursorTenths = -1;
        int displayedTotalTenths = -1;
        bool displayedHolding;

        void Awake()
        {
            displayed = bar != null && bar.activeSelf;
            if (fill != null) fill.raycastTarget = false;
            if (label != null) label.raycastTarget = false;
        }

        void Update()
        {
            EchoReplayDriver replay = null;
            var other = switchController != null && observers != null ? observers.Get(switchController.Active.Other()) : null;
            if (other != null) replay = other.Driver as EchoReplayDriver;
            bool show = replay != null;
            if (bar != null && displayed != show)
            {
                displayed = show;
                bar.SetActive(show);
            }
            if (!show || replay == null) return;
            float value = replay.FrameCount == 0 ? 0f : Mathf.Clamp01(replay.Cursor / (float)replay.FrameCount);
            if (fill != null && !Mathf.Approximately(displayedFill, value))
            {
                displayedFill = value;
                fill.fillAmount = value;
            }
            int cursorTenths = Mathf.FloorToInt(replay.Cursor * Time.fixedDeltaTime * 10f);
            int totalTenths = Mathf.FloorToInt(replay.FrameCount * Time.fixedDeltaTime * 10f);
            if (label != null && (displayedCursorTenths != cursorTenths || displayedTotalTenths != totalTenths || displayedHolding != replay.IsHolding))
            {
                displayedCursorTenths = cursorTenths;
                displayedTotalTenths = totalTenths;
                displayedHolding = replay.IsHolding;
                label.text = replay.IsHolding
                    ? $"ECHO {other.Id} HOLD"
                    : $"ECHO {other.Id} {cursorTenths / 10f:F1}/{totalTenths / 10f:F1}";
            }
        }
    }
}
