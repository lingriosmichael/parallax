using Parallax.Core;
using Parallax.Gameplay.Player;
using Parallax.Gameplay.Reality;
using UnityEngine;

namespace Parallax.Gameplay.Observers
{
    public sealed class ObserverContext : MonoBehaviour
    {
        [SerializeField] ObserverId id;
        [SerializeField] RealityRoot reality;
        [SerializeField] CatMotor2D cat;
        [SerializeField] Camera observerCamera;

        public ObserverId Id => id;
        public RealityRoot Reality => reality;
        public CatMotor2D Cat => cat;
        public Camera Camera => observerCamera;
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

            if (reality != null)
            {
                if (reality.Id != id)
                {
                    Debug.LogError($"ObserverContext '{gameObject.name}': reality.Id ({reality.Id}) does not match this Observer's id ({id}).", this);
                }

                if (cat != null && !cat.transform.IsChildOf(reality.transform))
                {
                    Debug.LogError($"ObserverContext '{gameObject.name}': cat '{cat.name}' is not a descendant of reality '{reality.name}'.", this);
                }
            }
            else
            {
                Debug.LogError($"ObserverContext '{gameObject.name}' has no reality assigned.", this);
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
