using System;

namespace Parallax.Gameplay.Levels
{
    public enum IapStatus { Idle, Working, Deferred, Cancelled, Failed, NothingToRestore, Unavailable, Unlocked }

    /// <summary>PAX-066: the store, as IapService sees it. UnityStoreGateway is the real one; tests use a fake.</summary>
    public interface IStoreGateway : IDisposable
    {
        event Action<string> PriceFetched;
        event Action<bool> OwnershipChecked;
        event Action PurchaseSucceeded;
        event Action<bool> PurchaseFailed;       // true: the player cancelled
        event Action PurchaseDeferred;
        event Action<string> Unavailable;

        void Connect();
        void Buy();
        void Restore();
    }

    /// <summary>PAX-066 (D-279): the paid unlock's one state machine. Only the store grants: a successful purchase or a check that finds
    /// the product sets the local cache, so play works offline. A check that finds nothing clears it (a refund). A cancel, a failure or
    /// an unavailable store grants nothing and leaves the UI usable, and free levels never wait on any of this.</summary>
    public sealed class IapService : IDisposable
    {
        readonly IStoreGateway store;
        bool restoring;

        public string Price { get; private set; }
        public IapStatus Status { get; private set; }
        public bool IsUnlocked => LevelProgressStore.PaidUnlocked;

        public event Action Changed;

        public IapService(IStoreGateway store)
        {
            this.store = store;
            store.PriceFetched += OnPrice;
            store.OwnershipChecked += OnOwnership;
            store.PurchaseSucceeded += Grant;
            store.PurchaseFailed += OnFailed;
            store.PurchaseDeferred += OnDeferred;
            store.Unavailable += OnUnavailable;
        }

        public void Start() => store.Connect();

        public void Buy()
        {
            if (Status == IapStatus.Working) return;
            restoring = false;
            Set(IapStatus.Working);
            store.Buy();
        }

        public void Restore()
        {
            if (Status == IapStatus.Working) return;
            restoring = true;
            Set(IapStatus.Working);
            store.Restore();
        }

        public void Dispose()
        {
            store.PriceFetched -= OnPrice;
            store.OwnershipChecked -= OnOwnership;
            store.PurchaseSucceeded -= Grant;
            store.PurchaseFailed -= OnFailed;
            store.PurchaseDeferred -= OnDeferred;
            store.Unavailable -= OnUnavailable;
            store.Dispose();
        }

        void OnPrice(string price)
        {
            Price = price;
            Changed?.Invoke();
        }

        void OnOwnership(bool owned)
        {
            if (owned) { Grant(); return; }
            LevelProgressStore.PaidUnlocked = false;
            if (restoring) { restoring = false; Set(IapStatus.NothingToRestore); }
            else Changed?.Invoke();
        }

        void Grant()
        {
            restoring = false;
            LevelProgressStore.PaidUnlocked = true;
            Set(IapStatus.Unlocked);
        }

        void OnFailed(bool cancelled) => Set(cancelled ? IapStatus.Cancelled : IapStatus.Failed);

        void OnDeferred() => Set(IapStatus.Deferred);

        void OnUnavailable(string reason)
        {
            restoring = false;
            Set(IapStatus.Unavailable);
        }

        void Set(IapStatus status)
        {
            Status = status;
            Changed?.Invoke();
        }
    }
}
