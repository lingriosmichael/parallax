using System;

namespace Parallax.Gameplay.Levels
{
    /// <summary>PAX-066 (D-279): the store in the Editor, answering as IapConfig's Editor Store fields say. It stands in for Unity IAP's
    /// fake store, whose dialog builds a legacy-input EventSystem that throws every frame in this Input System project, so it can't be
    /// clicked. Its launch check keeps a purchase made this session, as the real store would.</summary>
    public sealed class EditorStoreGateway : IStoreGateway
    {
        const string Price = "€4.99 (Editor)";

        readonly IapConfig config;
        bool bought;

        public event Action<string> PriceFetched;
        public event Action<bool> OwnershipChecked;
        public event Action PurchaseSucceeded;
        public event Action<bool> PurchaseFailed;
        public event Action PurchaseDeferred;
        public event Action<string> Unavailable;

        public EditorStoreGateway(IapConfig config) => this.config = config;

        public void Connect()
        {
            if (config.EditorPurchase == EditorPurchaseResult.StoreDown) { Unavailable?.Invoke("the Editor store is set to down"); return; }
            PriceFetched?.Invoke(Price);
            OwnershipChecked?.Invoke(config.EditorOwned);
        }

        public void Buy()
        {
            switch (config.EditorPurchase)
            {
                case EditorPurchaseResult.Succeed: bought = true; PurchaseSucceeded?.Invoke(); break;
                case EditorPurchaseResult.Cancel: PurchaseFailed?.Invoke(true); break;
                case EditorPurchaseResult.Fail: PurchaseFailed?.Invoke(false); break;
                case EditorPurchaseResult.Defer: PurchaseDeferred?.Invoke(); break;
                default: Unavailable?.Invoke("the Editor store is set to down"); break;
            }
        }

        public void Restore()
        {
            if (config.EditorPurchase == EditorPurchaseResult.StoreDown) { Unavailable?.Invoke("the Editor store is set to down"); return; }
            OwnershipChecked?.Invoke(bought || config.EditorOwned);
        }

        public void Dispose() { }
    }
}
