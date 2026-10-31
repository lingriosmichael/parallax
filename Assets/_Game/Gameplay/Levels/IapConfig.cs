using UnityEngine;

namespace Parallax.Gameplay.Levels
{
    public enum EditorPurchaseResult { Succeed, Cancel, Fail, Defer, StoreDown }

    /// <summary>PAX-066 (D-279): the paid unlock's product and the free wait. One asset at AssetPath, made by PARALLAX/Setup/Paid Unlock
    /// (PAX-066). The price isn't here: it's set in Play Console and the store reports it, localized. The Editor Store fields choose
    /// what EditorStoreGateway answers, so every flow can be tried in Play Mode.</summary>
    [CreateAssetMenu(menuName = "PARALLAX/IAP Config")]
    public sealed class IapConfig : ScriptableObject
    {
        public const string AssetPath = "Assets/_Game/Data/IapConfig.asset";

        [Tooltip("The non-consumable product's id. It must match Play Console.")]
        [SerializeField] string productId = "full_unlock";
        [Tooltip("Hours a level past the free ones waits before it opens free (D-068).")]
        [SerializeField] float waitHours = 12f;

        [Header("Editor Store (never used on a device)")]
        [Tooltip("What tapping Unlock does in the Editor.")]
        [SerializeField] EditorPurchaseResult editorPurchase = EditorPurchaseResult.Succeed;
        [Tooltip("Whether the account owns the product, for the launch check and Restore. Turn it off to test a refund or an empty restore.")]
        [SerializeField] bool editorOwned;

        public EditorPurchaseResult EditorPurchase => editorPurchase;
        public bool EditorOwned => editorOwned;
        public string ProductId => productId;
        public long WaitSeconds => (long)(waitHours * 3600f);
    }
}
