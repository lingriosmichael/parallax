using Parallax.Core;
using Parallax.Gameplay.Input;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Parallax.Gameplay.GravityControl
{
    public sealed class DialGravityInput : MonoBehaviour, IGravityControlInput, ITouchReservedRegion, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [SerializeField] RectTransform knob;
        [SerializeField] float maxDialDeg = 135f;
        [SerializeField] GameObject visualRoot;
        float value;

        public bool IsAvailable => true;
        public void Calibrate() { value = 0f; ApplyKnob(); }
        public float ReadNormalized() => value;
        public void SetVisible(bool visible) { if (visualRoot != null) visualRoot.SetActive(visible); }
        public bool ContainsScreenPoint(Vector2 point) => visualRoot != null && visualRoot.activeInHierarchy && RectTransformUtility.RectangleContainsScreenPoint((RectTransform)transform, point, null);
        public void OnPointerDown(PointerEventData data) => ReadPointer(data.position);
        public void OnDrag(PointerEventData data) => ReadPointer(data.position);
        public void OnPointerUp(PointerEventData data) { if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null); }
        void ReadPointer(Vector2 pointer)
        {
            RectTransform rect = (RectTransform)transform;
            Vector2 center = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center));
            float next = DialMath.ValueFromPointer(center, pointer, maxDialDeg);
            if (float.IsNaN(next) || Mathf.Approximately(next, value)) return;
            value = next;
            ApplyKnob();
        }
        void ApplyKnob() { if (knob != null) knob.localRotation = Quaternion.Euler(0f, 0f, -value * maxDialDeg); }
    }
}
