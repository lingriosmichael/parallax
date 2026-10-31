using System;
using System.Collections.Generic;
using Unity.Services.Core;
using UnityEngine;
using UnityEngine.Purchasing;

namespace Parallax.Gameplay.Levels
{
    /// <summary>PAX-066 (D-279): Unity IAP 5, one non-consumable. Connects, fetches the product (its localized price), then the player's
    /// purchases, and reports what it finds. Restore is that same fetch: Google Play simply reports what the account owns, and
    /// RestoreTransactions is refused on platforms that don't override it (iOS needs it, in Phase I). Connection trouble is logged and
    /// reported as Unavailable, never thrown: free levels mustn't notice. Device builds only: see EditorStoreGateway.</summary>
    public sealed class UnityStoreGateway : IStoreGateway
    {
        readonly string productId;
        StoreController store;
        bool connected, disposed;

        public event Action<string> PriceFetched;
        public event Action<bool> OwnershipChecked;
        public event Action PurchaseSucceeded;
        public event Action<bool> PurchaseFailed;
        public event Action PurchaseDeferred;
        public event Action<string> Unavailable;

        public UnityStoreGateway(string productId) => this.productId = productId;

        public async void Connect()
        {
            try
            {
                await UnityServices.InitializeAsync();
                if (disposed) return;
                store = UnityIAPServices.StoreController();
                store.OnStoreDisconnected += OnDisconnected;
                store.OnProductsFetched += OnProductsFetched;
                store.OnProductsFetchFailed += OnProductsFetchFailed;
                store.OnPurchasesFetched += OnPurchasesFetched;
                store.OnPurchasesFetchFailed += OnPurchasesFetchFailed;
                store.OnPurchasePending += OnPending;
                store.OnPurchaseConfirmed += OnConfirmed;
                store.OnPurchaseFailed += OnFailed;
                store.OnPurchaseDeferred += OnDeferred;
                await store.Connect();
                if (disposed) return;
                connected = true;
                store.FetchProducts(new List<ProductDefinition> { new ProductDefinition(productId, ProductType.NonConsumable) });
            }
            catch (Exception e) { Report("connect failed: " + e.Message); }
        }

        public void Buy()
        {
            Product product = connected ? store.GetProductById(productId) : null;
            if (product == null) { Report("no product to buy"); return; }
            store.PurchaseProduct(product);
        }

        public void Restore()
        {
            if (!connected) { Report("not connected, nothing to restore from"); return; }
            store.FetchPurchases();
        }

        public void Dispose()
        {
            disposed = true;
            if (store == null) return;
            store.OnStoreDisconnected -= OnDisconnected;
            store.OnProductsFetched -= OnProductsFetched;
            store.OnProductsFetchFailed -= OnProductsFetchFailed;
            store.OnPurchasesFetched -= OnPurchasesFetched;
            store.OnPurchasesFetchFailed -= OnPurchasesFetchFailed;
            store.OnPurchasePending -= OnPending;
            store.OnPurchaseConfirmed -= OnConfirmed;
            store.OnPurchaseFailed -= OnFailed;
            store.OnPurchaseDeferred -= OnDeferred;
        }

        void OnDisconnected(StoreConnectionFailureDescription failure)
        {
            connected = false;
            Report("disconnected: " + failure.Message);
        }

        void OnProductsFetched(List<Product> products)
        {
            Product product = store.GetProductById(productId);
            if (product == null) { Report("the store has no product " + productId); return; }
            PriceFetched?.Invoke(product.metadata.localizedPriceString);
            store.FetchPurchases();
        }

        void OnProductsFetchFailed(ProductFetchFailed failure) => Report("products: " + failure.FailureReason);

        void OnPurchasesFetchFailed(PurchasesFetchFailureDescription failure) => Report("purchases: " + failure.Message);

        // A pending or deferred order isn't a yes yet, and isn't a no: the cache stays as it is until it settles.
        void OnPurchasesFetched(Orders orders)
        {
            if (Contains(orders.ConfirmedOrders)) { OwnershipChecked?.Invoke(true); return; }
            if (Contains(orders.PendingOrders) || Contains(orders.DeferredOrders)) return;
            OwnershipChecked?.Invoke(false);
        }

        void OnPending(PendingOrder order) => store.ConfirmPurchase(order);

        void OnConfirmed(Order order)
        {
            if (order is ConfirmedOrder && Is(order)) PurchaseSucceeded?.Invoke();
            else PurchaseFailed?.Invoke(false);
        }

        void OnFailed(FailedOrder order) => PurchaseFailed?.Invoke(order.FailureReason == PurchaseFailureReason.UserCancelled);

        void OnDeferred(DeferredOrder order) => PurchaseDeferred?.Invoke();

        bool Contains<T>(IReadOnlyList<T> orders) where T : Order
        {
            foreach (T order in orders) if (Is(order)) return true;
            return false;
        }

        bool Is(Order order)
        {
            foreach (CartItem item in order.CartOrdered.Items())
                if (item.Product != null && item.Product.definition.id == productId) return true;
            return false;
        }

        void Report(string reason)
        {
            Debug.LogWarning("UnityStoreGateway: " + reason + "; the store is unavailable, free levels carry on.");
            Unavailable?.Invoke(reason);
        }
    }
}
