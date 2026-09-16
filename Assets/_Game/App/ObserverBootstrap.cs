using Parallax.Core;
using Parallax.Gameplay.Observers;
using UnityEngine;

namespace Parallax.App
{
    public sealed class ObserverBootstrap : MonoBehaviour
    {
        [SerializeField] SoloSwitchController switchController;

        void Start()
        {
            if (switchController == null)
            {
                Debug.LogError($"ObserverBootstrap '{gameObject.name}' has no SoloSwitchController assigned.", this);
                return;
            }

            switchController.Initialize(ObserverId.A);
        }
    }
}
