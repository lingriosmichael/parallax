using NUnit.Framework;
using Parallax.Gameplay.UI;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Parallax.Tests.EditMode
{
    /// <summary>PAX-049 review fix: RestartButton sits under LevelCompletePanel, which starts
    /// inactive, so its Awake never runs and the cached rectTransform stays null while it is
    /// hidden -- yet it is registered in TouchStickCatInput.reservedRegions from scene start, so
    /// every touch during normal play (long before the level ever completes) calls
    /// ContainsScreenPoint against that null rect.</summary>
    public sealed class RestartButtonTests
    {
        [Test]
        public void ContainsScreenPoint_UnderInactiveParent_ReturnsFalseWithoutThrowing_ThenTrueOnceActivated()
        {
            var parentGo = new GameObject("Panel", typeof(RectTransform));
            try
            {
                parentGo.SetActive(false); // never activated before RestartButton exists, matching LevelCompletePanel

                var buttonGo = new GameObject("RestartButton", typeof(RectTransform));
                buttonGo.transform.SetParent(parentGo.transform, false);
                var rect = (RectTransform)buttonGo.transform;
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(100f, 100f);
                rect.position = new Vector3(500f, 500f, 0f);

                var restartButton = buttonGo.AddComponent<RestartButton>();
                Vector2 insidePoint = new Vector2(500f, 500f);

                bool result = false;
                Assert.DoesNotThrow(() => result = restartButton.ContainsScreenPoint(insidePoint),
                    "a hidden button must never throw when a touch is checked against it");
                Assert.IsFalse(result, "a hidden button must never reserve screen space");

                parentGo.SetActive(true);
                Assert.IsTrue(restartButton.ContainsScreenPoint(insidePoint),
                    "once active, the same point must register as inside its rect");
            }
            finally
            {
                Object.DestroyImmediate(parentGo);
            }
        }
    }
}
