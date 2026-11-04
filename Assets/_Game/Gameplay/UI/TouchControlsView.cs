using Parallax.Gameplay.Input;
using UnityEngine;

namespace Parallax.Gameplay.UI
{
    /// <summary>PAX-A09: draws the floating stick and the jump button. It only reads TouchStickCatInput: the stick still starts wherever a thumb lands
    /// in its zone (D-021, D-049), and rests at a quiet home spot until then. Nothing here takes a touch, so nothing needs a reserved region.</summary>
    public sealed class TouchControlsView : MonoBehaviour
    {
        [SerializeField] TouchStickCatInput input;
        [SerializeField] RectTransform stickBase;
        [SerializeField] RectTransform stickKnob;
        [SerializeField] CanvasGroup stick;
        [SerializeField] RectTransform jump;
        [SerializeField] CanvasGroup jumpGroup;
        [SerializeField] float stickRest = 0.22f;
        [SerializeField] float stickHeld = 0.5f;
        [SerializeField] float jumpRest = 0.62f;
        [SerializeField] float jumpHeld = 1f;
        [SerializeField] float jumpPressedScale = 0.92f;

        Canvas canvas;
        Vector2 home;

        /// <summary>The knob's offset from the base, in canvas units: it follows the thumb, and stops at the ring.</summary>
        public static Vector2 KnobOffset(Vector2 origin, Vector2 current, float radiusPixels, float canvasScale) =>
            canvasScale <= 0f ? Vector2.zero : Vector2.ClampMagnitude(current - origin, radiusPixels) / canvasScale;

        void Awake()
        {
            canvas = GetComponentInParent<Canvas>();
            if (stickBase != null) home = stickBase.anchoredPosition;
        }

        void LateUpdate()
        {
            if (input == null || canvas == null) return;
            float scale = canvas.scaleFactor;
            float radius = input.StickRadiusPixels;
            bool held = input.StickHeld;

            if (stickBase != null)
            {
                if (scale > 0f) stickBase.sizeDelta = Vector2.one * (2f * radius / scale);
                if (held) stickBase.position = input.StickOrigin;
                else stickBase.anchoredPosition = home;
            }
            if (stickKnob != null) stickKnob.anchoredPosition = held ? KnobOffset(input.StickOrigin, input.StickCurrent, radius, scale) : Vector2.zero;
            if (stick != null) stick.alpha = held ? stickHeld : stickRest;

            bool jumping = input.JumpHeld;
            if (jumpGroup != null) jumpGroup.alpha = jumping ? jumpHeld : jumpRest;
            if (jump != null) jump.localScale = Vector3.one * (jumping ? jumpPressedScale : 1f);
        }
    }
}
