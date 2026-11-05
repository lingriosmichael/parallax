using UnityEngine;

namespace Parallax.App
{
    /// <summary>PAX-127 (D-322): phone apps run at 60 frames a second. Unity's mobile default is 30, which kept every touch waiting up to a
    /// frame and drew the cat in steps. The simulation stays at 50 Hz (D-075); only the screen and touch polling speed up. The Editor,
    /// desktop and the browser keep their own rate: a browser paces frames itself, and a target there makes them less even.</summary>
    public static class FrameRate
    {
        public const int Phone = 60;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Apply()
        {
            if (IsPhoneApp(Application.platform)) Application.targetFrameRate = Phone;
        }

        public static bool IsPhoneApp(RuntimePlatform platform) =>
            platform == RuntimePlatform.Android || platform == RuntimePlatform.IPhonePlayer;
    }
}
