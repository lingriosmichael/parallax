using UnityEngine;

namespace Parallax.Gameplay.Rooms
{
    // PAX-073 (D-074): the smallest Floor a level layout may author. LevelLayoutValidator
    // .ValidatePlatformSizes reads these limits; nothing reads them at runtime.
    [CreateAssetMenu(menuName = "PARALLAX/Platform Size Config")]
    public sealed class PlatformSizeConfig : ScriptableObject
    {
        [Tooltip("Minimum Floor thickness, in world units. Must be at least the cat's maximum fall per physics step so a Floor can never be tunnelled.")]
        [SerializeField] float minThickness = 0.5f;

        [Tooltip("Minimum Floor width, in world units.")]
        [SerializeField] float minWidth = 1f;

        public float MinThickness => minThickness;
        public float MinWidth => minWidth;
    }
}
