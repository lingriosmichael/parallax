using Parallax.Core;
using Parallax.Gameplay.Input;
using Parallax.Gameplay.Observers;
using UnityEngine;

namespace Parallax.App
{
    public sealed class ObserverBootstrap : MonoBehaviour
    {
        [SerializeField] ObserverSet observers;
        [SerializeField] CatInputRouter router;

        void Start()
        {
            if (observers == null)
            {
                Debug.LogError($"ObserverBootstrap '{gameObject.name}' has no ObserverSet assigned.", this);
                return;
            }

            if (router == null)
            {
                Debug.LogError($"ObserverBootstrap '{gameObject.name}' has no CatInputRouter assigned.", this);
                return;
            }

            ObserverContext a = observers.Get(ObserverId.A);
            if (a != null)
            {
                a.SetDriver(new LocalHumanDriver(router));
            }
            else
            {
                Debug.LogError($"ObserverBootstrap '{gameObject.name}': ObserverSet has no observerA.", this);
            }

            ObserverContext b = observers.Get(ObserverId.B);
            if (b != null)
            {
                b.SetDriver(new InactiveDriver());
            }
        }
    }
}
