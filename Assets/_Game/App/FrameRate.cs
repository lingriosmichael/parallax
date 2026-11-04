using UnityEngine;

namespace Parallax.App
{
    /// <summary>PAX-127 (D-322): phones run at 60 frames a second. Unity's mobile default is 30, which kept every touch waiting up to a
    /// frame and drew the cat in steps. The simulation stays at 50 Hz (D-075); only the screen and touch polling speed up. The Editor
    /// and desktop keep their own rate.</summary>
    public static class FrameRate
    {
        public const int Phone = 60;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Apply()
        {
            if (Application.isMobilePlatform) Application.targetFrameRate = Phone;
        }
    }
}
