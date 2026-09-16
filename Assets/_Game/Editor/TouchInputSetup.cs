using Parallax.Editor.Setup;
using UnityEditor;

namespace Parallax.Editor
{
    public static class TouchInputSetup
    {
        // Superseded by ObserverSetup (PAX-014): input now lives on a device-level
        // DeviceInput rig, not on Cat_Player. Kept as a menu alias so muscle memory
        // and any external references still resolve to the current setup step.
        [MenuItem("PARALLAX/Setup/Configure Touch Input")]
        public static void Configure()
        {
            ObserverSetup.Configure();
        }
    }
}
