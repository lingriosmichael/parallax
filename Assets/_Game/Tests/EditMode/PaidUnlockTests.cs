using System;
using System.Collections.Generic;
using NUnit.Framework;
using Parallax.Core;
using Parallax.Gameplay.Levels;
using Parallax.Gameplay.UI;
using UnityEditor;
using UnityEngine;

namespace Parallax.Tests.EditMode
{
    /// <summary>PAX-066 (D-279): free and paid against locked and unlocked, the 12-hour wait, purchases through a fake store (success,
    /// cancel, failure, restore, refund), a corrupt cache, and the free set against D-068. Restores every key it touches.</summary>
    public sealed class PaidUnlockTests
    {
        const string PaidKey = "PARALLAX_FULL_UNLOCK";
        const string WaitKey = "PARALLAX_LEVEL_WAIT_T_L011";
        const long Hours12 = 12 * 3600;
        const string LevelListPath = "Assets/_Game/Data/LevelListConfig.asset";

        static readonly string[] Ids = MakeIds();

        static string[] MakeIds()
        {
            var ids = new string[40];
            for (int i = 0; i < ids.Length; i++) ids[i] = "L" + (i + 1).ToString("000");
            return ids;
        }

        sealed class FakeStore : IStoreGateway
        {
            public event Action<string> PriceFetched;
            public event Action<bool> OwnershipChecked;
            public event Action PurchaseSucceeded;
            public event Action<bool> PurchaseFailed;
            public event Action PurchaseDeferred;
            public event Action<string> Unavailable;

            public int Buys, Restores, Connects;
            public bool Disposed;

            public void Connect() => Connects++;
            public void Buy() => Buys++;
            public void Restore() => Restores++;
            public void Dispose() => Disposed = true;

            public void Price(string price) => PriceFetched?.Invoke(price);
            public void Owned(bool owned) => OwnershipChecked?.Invoke(owned);
            public void Succeed() => PurchaseSucceeded?.Invoke();
            public void Fail(bool cancelled) => PurchaseFailed?.Invoke(cancelled);
            public void Defer() => PurchaseDeferred?.Invoke();
            public void Down() => Unavailable?.Invoke("test");
        }

        bool hadPaid;
        string savedPaid;
        FakeStore fake;
        IapService service;

        [SetUp]
        public void SetUp()
        {
            hadPaid = PlayerPrefs.HasKey(PaidKey);
            savedPaid = PlayerPrefs.GetString(PaidKey, "");
            PlayerPrefs.DeleteKey(PaidKey);
            PlayerPrefs.DeleteKey(WaitKey);
            fake = new FakeStore();
            service = new IapService(fake);
        }

        [TearDown]
        public void TearDown()
        {
            service.Dispose();
            if (hadPaid) PlayerPrefs.SetString(PaidKey, savedPaid);
            else PlayerPrefs.DeleteKey(PaidKey);
            PlayerPrefs.DeleteKey(WaitKey);
            PlayerPrefs.Save();
        }

        static Entitlements Access(bool paid, long waitStart = -1) => new Entitlements(Ids, 10, Hours12, paid, id => waitStart);

        [TestCase("L001", false, true)]
        [TestCase("L010", false, true)]
        [TestCase("L011", false, false)]
        [TestCase("L040", false, false)]
        [TestCase("L011", true, true)]
        [TestCase("L040", true, true)]
        [TestCase("NotListed", false, true)]
        public void IsPlayable_FollowsFreeOrPaid(string id, bool paid, bool expected) =>
            Assert.AreEqual(expected, Access(paid).IsPlayable(id, 1000));

        [TestCase(false, false, false)]
        [TestCase(true, false, false)]
        [TestCase(false, true, false)]
        [TestCase(true, true, true)]
        public void Entering_NeedsProgress_AndPlayable(bool reached, bool paid, bool expected)
        {
            var progress = new LevelProgress(Ids);
            if (reached) progress.RecordCompletion("L010", 0);
            Assert.AreEqual(expected, Access(paid).CanEnter(progress, "L011", 1000));
        }

        [Test]
        public void TheWait_RunsTwelveHours_ThenTheLevelStaysOpen()
        {
            Entitlements access = Access(false, 5000);
            Assert.AreEqual(Hours12, access.SecondsLeft("L011", 5000));
            Assert.AreEqual(1, access.SecondsLeft("L011", 5000 + Hours12 - 1));
            Assert.IsFalse(access.IsPlayable("L011", 5000 + Hours12 - 1));
            Assert.IsTrue(access.IsPlayable("L011", 5000 + Hours12));
            Assert.IsTrue(access.IsPlayable("L011", 5000 + Hours12 * 9));
        }

        [Test]
        public void AWaitNotBegun_ShowsTheWholeWait() => Assert.AreEqual(Hours12, Access(false).SecondsLeft("L011", 5000));

        [Test]
        public void AClockMovedBack_NeverMakesTheWaitLonger() => Assert.AreEqual(Hours12, Access(false, 5000).SecondsLeft("L011", 100));

        [Test]
        public void TheClock_ReadsHoursMinutesSeconds() => Assert.AreEqual("11:32:04", Entitlements.Clock(11 * 3600 + 32 * 60 + 4));

        [Test]
        public void TheWaitsStart_IsSavedOnce_AndACorruptOneStartsAgain()
        {
            Assert.AreEqual(100, LevelProgressStore.WaitStartedAt("T_L011", 100));
            Assert.AreEqual(100, LevelProgressStore.WaitStartedAt("T_L011", 999), "the first start stands");
            PlayerPrefs.SetString(WaitKey, "not a time");
            Assert.AreEqual(777, LevelProgressStore.WaitStartedAt("T_L011", 777));
        }

        [Test]
        public void APurchase_Grants_AndTheCacheHoldsIt()
        {
            service.Buy();
            Assert.AreEqual(IapStatus.Working, service.Status);
            Assert.IsFalse(service.IsUnlocked, "nothing until the store says so");
            fake.Succeed();
            Assert.AreEqual(IapStatus.Unlocked, service.Status);
            Assert.IsTrue(LevelProgressStore.PaidUnlocked);
        }

        [TestCase(true, IapStatus.Cancelled)]
        [TestCase(false, IapStatus.Failed)]
        public void ACancelOrFailure_GrantsNothing_AndYouCanBuyAgain(bool cancelled, IapStatus expected)
        {
            service.Buy();
            fake.Fail(cancelled);
            Assert.AreEqual(expected, service.Status);
            Assert.IsFalse(service.IsUnlocked);
            service.Buy();
            Assert.AreEqual(2, fake.Buys, "the UI stays usable");
        }

        [Test]
        public void ASecondTap_WhileWorking_DoesNothing()
        {
            service.Buy();
            service.Buy();
            service.Restore();
            Assert.AreEqual(1, fake.Buys);
            Assert.AreEqual(0, fake.Restores);
        }

        [Test]
        public void ADeferredPayment_GrantsNothing_UntilItSettles()
        {
            service.Buy();
            fake.Defer();
            Assert.AreEqual(IapStatus.Deferred, service.Status);
            Assert.IsFalse(service.IsUnlocked);
            fake.Succeed();
            Assert.IsTrue(service.IsUnlocked);
        }

        [Test]
        public void Restore_WithTheProduct_Grants()
        {
            service.Restore();
            fake.Owned(true);
            Assert.IsTrue(service.IsUnlocked);
            Assert.AreEqual(IapStatus.Unlocked, service.Status);
        }

        [Test]
        public void Restore_WithNone_GrantsNothing()
        {
            service.Restore();
            fake.Owned(false);
            Assert.IsFalse(service.IsUnlocked);
            Assert.AreEqual(IapStatus.NothingToRestore, service.Status);
        }

        [Test]
        public void ClearedData_ThenAStoreCheck_RecoversTheUnlock()
        {
            fake.Owned(true);
            PlayerPrefs.DeleteKey(PaidKey);
            Assert.IsFalse(service.IsUnlocked);
            fake.Owned(true);
            Assert.IsTrue(service.IsUnlocked);
        }

        [Test]
        public void AStoreCheckThatFindsNothing_ClearsTheCache_ARefund()
        {
            LevelProgressStore.PaidUnlocked = true;
            fake.Owned(false);
            Assert.IsFalse(service.IsUnlocked);
            Assert.AreEqual(IapStatus.Idle, service.Status, "a launch check says nothing on screen");
        }

        [Test]
        public void AnUnavailableStore_KeepsTheCache_AndLeavesRestoreUsable()
        {
            LevelProgressStore.PaidUnlocked = true;
            service.Restore();
            fake.Down();
            Assert.IsTrue(service.IsUnlocked, "offline, the cached purchase stands");
            Assert.AreEqual(IapStatus.Unavailable, service.Status);
            service.Restore();
            Assert.AreEqual(2, fake.Restores);
        }

        [TestCase("")]
        [TestCase("1")]
        [TestCase("GRANTED")]
        [TestCase("garbage%%%")]
        public void ACorruptCache_IsNotUnlocked_AndDoesntThrow(string value)
        {
            PlayerPrefs.SetString(PaidKey, value);
            Assert.IsFalse(LevelProgressStore.PaidUnlocked);
            fake.Owned(true);
            Assert.IsTrue(LevelProgressStore.PaidUnlocked, "the store re-check puts it right");
        }

        [Test]
        public void StartGamesClear_KeepsThePurchase_AndDropsTheWaits()
        {
            LevelProgressStore.PaidUnlocked = true;
            LevelProgressStore.WaitStartedAt("T_L011", 100);
            LevelProgressStore.Clear(new[] { "T_L011" });
            Assert.IsTrue(LevelProgressStore.PaidUnlocked);
            Assert.IsFalse(PlayerPrefs.HasKey(WaitKey));
        }

        [Test]
        public void Dispose_LetsTheStoreGo()
        {
            service.Dispose();
            Assert.IsTrue(fake.Disposed);
            fake.Succeed();
            Assert.IsFalse(LevelProgressStore.PaidUnlocked, "a disposed service hears nothing");
            service = new IapService(new FakeStore());
        }

        static IapConfig EditorConfig(EditorPurchaseResult result, bool owned)
        {
            IapConfig config = ScriptableObject.CreateInstance<IapConfig>();
            var serialized = new SerializedObject(config);
            serialized.FindProperty("editorPurchase").enumValueIndex = (int)result;
            serialized.FindProperty("editorOwned").boolValue = owned;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return config;
        }

        [TestCase(EditorPurchaseResult.Succeed, IapStatus.Unlocked, true)]
        [TestCase(EditorPurchaseResult.Cancel, IapStatus.Cancelled, false)]
        [TestCase(EditorPurchaseResult.Fail, IapStatus.Failed, false)]
        [TestCase(EditorPurchaseResult.Defer, IapStatus.Deferred, false)]
        [TestCase(EditorPurchaseResult.StoreDown, IapStatus.Unavailable, false)]
        public void TheEditorStore_AnswersAsConfigured(EditorPurchaseResult result, IapStatus expected, bool unlocked)
        {
            IapConfig config = EditorConfig(result, false);
            using var editor = new IapService(new EditorStoreGateway(config));
            editor.Start();
            editor.Buy();
            Assert.AreEqual(expected, editor.Status);
            Assert.AreEqual(unlocked, editor.IsUnlocked);
            Assert.AreEqual(result != EditorPurchaseResult.StoreDown, editor.Price != null, "the price arrives unless the store is down");
            UnityEngine.Object.DestroyImmediate(config);
        }

        [TestCase(true, IapStatus.Unlocked)]
        [TestCase(false, IapStatus.NothingToRestore)]
        public void TheEditorStore_Restores_WhatTheAccountOwns(bool owned, IapStatus expected)
        {
            IapConfig config = EditorConfig(EditorPurchaseResult.Succeed, owned);
            using var editor = new IapService(new EditorStoreGateway(config));
            editor.Restore();
            Assert.AreEqual(expected, editor.Status);
            UnityEngine.Object.DestroyImmediate(config);
        }

        [Test]
        public void ThePanel_AllowsEverything_WithNothingWired()
        {
            var go = new GameObject("Panel", typeof(RectTransform));
            UnlockPanel panel = go.AddComponent<UnlockPanel>();
            Assert.IsTrue(panel.Allows("L011"));
            UnityEngine.Object.DestroyImmediate(go);
        }

        [Test]
        public void ThePanel_HoldsThePaidLevel_UntilTheStoreGrants()
        {
            const string UnlockedKey = "PARALLAX_LEVEL_UNLOCKED_L011", RealWaitKey = "PARALLAX_LEVEL_WAIT_L011";
            bool hadUnlocked = PlayerPrefs.HasKey(UnlockedKey), hadWait = PlayerPrefs.HasKey(RealWaitKey);
            int savedUnlocked = PlayerPrefs.GetInt(UnlockedKey, 0);
            string savedWait = PlayerPrefs.GetString(RealWaitKey, "");
            Func<long> savedNow = LevelAccess.UtcNow;
            var host = new GameObject("Store");
            var go = new GameObject("Panel", typeof(RectTransform));
            IapConfig config = ScriptableObject.CreateInstance<IapConfig>();
            try
            {
                LevelAccess.UtcNow = () => 1000;
                PlayerPrefs.SetInt(UnlockedKey, 1);
                PlayerPrefs.DeleteKey(RealWaitKey);
                StoreHost store = host.AddComponent<StoreHost>();
                store.Use(config, service);
                UnlockPanel panel = go.AddComponent<UnlockPanel>();
                var serialized = new SerializedObject(panel);
                serialized.FindProperty("store").objectReferenceValue = store;
                serialized.FindProperty("levelList").objectReferenceValue = AssetDatabase.LoadAssetAtPath<LevelListConfig>(LevelListPath);
                serialized.ApplyModifiedPropertiesWithoutUndo();

                Assert.IsTrue(panel.Allows("L001"), "free, and always reached");
                Assert.IsFalse(panel.Allows("L011"), "reached, unpaid, the wait has just begun");
                LevelAccess.UtcNow = () => 1000 + Hours12;
                Assert.IsTrue(panel.Allows("L011"), "the wait is over");
                LevelAccess.UtcNow = () => 1001;
                Assert.IsFalse(panel.Allows("L011"));
                fake.Succeed();
                Assert.IsTrue(panel.Allows("L011"), "paid");
            }
            finally
            {
                LevelAccess.UtcNow = savedNow;
                if (hadUnlocked) PlayerPrefs.SetInt(UnlockedKey, savedUnlocked); else PlayerPrefs.DeleteKey(UnlockedKey);
                if (hadWait) PlayerPrefs.SetString(RealWaitKey, savedWait); else PlayerPrefs.DeleteKey(RealWaitKey);
                UnityEngine.Object.DestroyImmediate(go);
                UnityEngine.Object.DestroyImmediate(host);
                UnityEngine.Object.DestroyImmediate(config);
            }
        }

        [Test]
        public void TheFreeSet_IsLevelsOneToTen_D068()
        {
            var list = UnityEditor.AssetDatabase.LoadAssetAtPath<LevelListConfig>(LevelListPath);
            Assert.IsNotNull(list);
            Assert.AreEqual(10, list.FreeLevels);
            IReadOnlyList<string> ids = list.OrderedIds();
            var access = new Entitlements(ids, list.FreeLevels, Hours12, false, id => -1);
            for (int i = 0; i < ids.Count; i++) Assert.AreEqual(i < 10, access.IsFree(ids[i]), ids[i]);
        }

        [Test]
        public void LevelSelect_MarksLockedLevels_OnlyWhenGated()
        {
            var progress = new LevelProgress(Ids);
            var levels = new (string, string, string)[Ids.Length];
            for (int i = 0; i < Ids.Length; i++) levels[i] = (Ids[i], "S", "N");
            Assert.IsFalse(LevelSelectModel.Build(levels, progress)[10].IsPaywalled, "ungated, as before");
            LevelSelectEntry[] entries = LevelSelectModel.Build(levels, progress, Access(false), 1000);
            Assert.IsFalse(entries[9].IsPaywalled);
            Assert.IsTrue(entries[10].IsPaywalled);
            Assert.AreEqual("Locked", entries[10].BestDeathsText);
            Assert.IsFalse(LevelSelectModel.Build(levels, progress, Access(true), 1000)[10].IsPaywalled);
        }
    }
}
