using System;
using Parallax.Core;
using Parallax.Gameplay.Input;
using Parallax.Gameplay.Levels;
using UnityEngine;
using UnityEngine.UI;

namespace Parallax.Gameplay.UI
{
    /// <summary>PAX-066 (D-279): the choice for a level past the free ones: pay once, restore, or wait. Shown by a locked row in level
    /// select and by Next level on the level-complete screen. It counts the wait down live; when the level opens (paid, restored or
    /// waited out) it loads it if the player has reached it, and otherwise closes. Store trouble shows as a line of text, and Not now
    /// always works. Time here is the phone's real clock (D-279), a menu's, not the game's tick.</summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class UnlockPanel : MonoBehaviour, ITouchReservedRegion
    {
        [SerializeField] StoreHost store;
        [SerializeField] LevelListConfig levelList;
        [SerializeField] Text statusText;
        [SerializeField] Text unlockLabel;
        [SerializeField] Button unlockButton;
        [SerializeField] Button restoreButton;
        [SerializeField] Button closeButton;

        string levelId, sceneName;
        Action closed;
        float nextRefresh;

        /// <summary>Whether a level may be entered now. With nothing wired, as in a test rig or a scene without a store, it may.</summary>
        public bool Allows(string id)
        {
            if (string.IsNullOrEmpty(id) || levelList == null || store == null || store.Config == null) return true;
            LevelProgress progress = LevelProgressStore.Load(levelList.OrderedIds());
            return LevelAccess.Load(levelList, store.Config, progress).CanEnter(progress, id, LevelAccess.UtcNow());
        }

        public void Show(string id, string scene, Action onClosed = null)
        {
            levelId = id;
            sceneName = scene;
            closed = onClosed;
            gameObject.SetActive(true);
            Refresh();
        }

        void OnEnable()
        {
            if (unlockButton != null) unlockButton.onClick.AddListener(Buy);
            if (restoreButton != null) restoreButton.onClick.AddListener(Restore);
            if (closeButton != null) closeButton.onClick.AddListener(Close);
            if (store != null && store.Service != null) store.Service.Changed += Refresh;
        }

        void OnDisable()
        {
            if (unlockButton != null) unlockButton.onClick.RemoveListener(Buy);
            if (restoreButton != null) restoreButton.onClick.RemoveListener(Restore);
            if (closeButton != null) closeButton.onClick.RemoveListener(Close);
            if (store != null && store.Service != null) store.Service.Changed -= Refresh;
        }

        void Update()
        {
            if (Time.unscaledTime < nextRefresh) return;
            nextRefresh = Time.unscaledTime + 0.25f;
            Refresh();
        }

        public void Buy() { if (store != null) store.Service?.Buy(); }

        public void Restore() { if (store != null) store.Service?.Restore(); }

        public void Close()
        {
            gameObject.SetActive(false);
            Action then = closed;
            closed = null;
            then?.Invoke();
        }

        void Refresh()
        {
            if (levelId == null || levelList == null || store == null || store.Config == null) return;
            LevelProgress progress = LevelProgressStore.Load(levelList.OrderedIds());
            Entitlements access = LevelAccess.Load(levelList, store.Config, progress);
            long now = LevelAccess.UtcNow();
            if (access.IsPlayable(levelId, now)) { Open(progress); return; }

            IapService service = store.Service;
            bool canBuy = service != null && service.Price != null && service.Status != IapStatus.Working;
            if (unlockButton != null) unlockButton.interactable = canBuy;
            if (unlockLabel != null) unlockLabel.text = service != null && service.Price != null ? "Unlock all levels: " + service.Price : "Unlock all levels";
            if (statusText == null) return;
            string wait = progress.IsUnlocked(levelId) ? "Or wait: free in " + Entitlements.Clock(access.SecondsLeft(levelId, now)) : "Or keep playing: levels open free, one at a time.";
            string store_ = service != null ? Line(service.Status, service.Price) : "The store isn't available.";
            statusText.text = store_.Length > 0 ? store_ + "\n" + wait : wait;
        }

        void Open(LevelProgress progress)
        {
            string scene = progress.IsUnlocked(levelId) ? sceneName : null;
            levelId = null;
            if (string.IsNullOrEmpty(scene)) { Close(); return; }
            closed = null;
            gameObject.SetActive(false);
            LevelSceneLoader.Load(scene);
        }

        static string Line(IapStatus status, string price) => status switch
        {
            IapStatus.Working => "Talking to the store...",
            IapStatus.Deferred => "Waiting for the store to approve the payment.",
            IapStatus.Failed => "The purchase didn't go through. Nothing was charged.",
            IapStatus.NothingToRestore => "No purchase to restore on this account.",
            IapStatus.Unavailable => "The store isn't available right now.",
            IapStatus.Cancelled => "",
            _ => price == null ? "Connecting to the store..." : "",
        };

        public bool ContainsScreenPoint(Vector2 screenPos) =>
            isActiveAndEnabled && RectTransformUtility.RectangleContainsScreenPoint((RectTransform)transform, screenPos, null);
    }
}
