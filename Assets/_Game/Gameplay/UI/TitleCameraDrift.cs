using Parallax.Core;
using Parallax.Gameplay.Levels;
using UnityEngine;

namespace Parallax.Gameplay.UI
{
    /// <summary>PAX-108 (D-157): pans the title camera slowly left and right about where the setup menu placed it, so the
    /// environment's ParallaxLayers move at their own speeds. Presentation only, on unscaled seconds.</summary>
    [RequireComponent(typeof(Camera))]
    public sealed class TitleCameraDrift : MonoBehaviour
    {
        [SerializeField] TitleScreenConfig config;
        [SerializeField] Vector2 centre;

        float elapsed, size;

        void Awake() => size = GetComponent<Camera>().orthographicSize;

        void LateUpdate()
        {
            if (config == null) return;
            elapsed += Time.unscaledDeltaTime;
            float x = centre.x + TitleIntroTimeline.Drift(elapsed, config.DriftRange, config.DriftPeriod);
            transform.position = new Vector3(x, centre.y, transform.position.z);
            // D-280: the slow push in as the intro plays.
            GetComponent<Camera>().orthographicSize = size * Mathf.Lerp(config.PushFrom, 1f, TitleIntroTimeline.Push(elapsed, config.PushSeconds));
        }
    }
}
