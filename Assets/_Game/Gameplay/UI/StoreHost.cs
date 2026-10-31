using Parallax.Gameplay.Levels;
using UnityEngine;

namespace Parallax.Gameplay.UI
{
    /// <summary>PAX-066 (D-279): one per LevelSelect and level scene, placed by PARALLAX/Setup/Paid Unlock (PAX-066); not a singleton. Owns
    /// the scene's IapService and connects it on Start, which also re-checks the purchase with the store when online.</summary>
    [DisallowMultipleComponent]
    public sealed class StoreHost : MonoBehaviour
    {
        [SerializeField] IapConfig config;

        public IapConfig Config => config;
        public IapService Service { get; private set; }

        void Awake()
        {
            if (config == null) { Debug.LogError("StoreHost: no IapConfig assigned; the store stays off. Run PARALLAX/Setup/Paid Unlock (PAX-066).", this); return; }
#if UNITY_EDITOR
            Service = new IapService(new EditorStoreGateway(config));
#else
            Service = new IapService(new UnityStoreGateway(config.ProductId));
#endif
        }

        void Start() => Service?.Start();

        void OnDestroy() => Service?.Dispose();

        /// <summary>For tests: a service on a fake store.</summary>
        public void Use(IapConfig fakeConfig, IapService service)
        {
            config = fakeConfig;
            Service = service;
        }
    }
}
