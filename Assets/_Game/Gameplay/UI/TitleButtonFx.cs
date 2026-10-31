using UnityEngine;
using UnityEngine.EventSystems;

namespace Parallax.Gameplay.UI
{
    /// <summary>D-280: a title button gives a little under the finger, and eases back.</summary>
    public sealed class TitleButtonFx : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        [SerializeField] float pressed = 0.95f;
        [SerializeField] float speed = 16f;

        float target = 1f;

        public void OnPointerDown(PointerEventData eventData) => target = pressed;
        public void OnPointerUp(PointerEventData eventData) => target = 1f;
        public void OnPointerExit(PointerEventData eventData) => target = 1f;

        void Update()
        {
            float scale = Mathf.Lerp(transform.localScale.x, target, Mathf.Clamp01(Time.unscaledDeltaTime * speed));
            transform.localScale = new Vector3(scale, scale, 1f);
        }
    }
}
