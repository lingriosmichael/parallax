using Parallax.Core;
using Parallax.Gameplay.Player;
using UnityEngine;

namespace Parallax.Gameplay.Observers
{
    public sealed class ObserverContext : MonoBehaviour
    {
        [SerializeField] ObserverId id;
        [SerializeField] CatMotor2D cat;

        public ObserverId Id => id;
        public CatMotor2D Cat => cat;
        public GravityReceiver Gravity { get; private set; }
        public IObserverDriver Driver { get; private set; }

        void Awake()
        {
            if (cat != null)
            {
                Gravity = cat.GetComponent<GravityReceiver>();
                if (Gravity == null)
                {
                    Debug.LogError($"ObserverContext '{gameObject.name}': cat '{cat.name}' has no GravityReceiver.", this);
                }
            }
            else
            {
                Debug.LogError($"ObserverContext '{gameObject.name}' has no cat assigned.", this);
            }
        }

        public void SetDriver(IObserverDriver next)
        {
            Driver?.Deactivate();
            Driver = next;
            Driver?.Activate(this);
        }

        public void Step(int tick)
        {
            Driver?.FixedTick(tick);
        }
    }
}
