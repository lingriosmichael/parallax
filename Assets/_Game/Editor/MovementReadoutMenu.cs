using Parallax.DebugTools;
using UnityEditor;

namespace Parallax.Editor
{
    // PAX-082 §12 R1: a checkable menu for the movement readout. It only stores an EditorPrefs bool
    // (off by default); MovementReadout.SpawnIfEnabled reads it when Play starts. No scene is changed.
    static class MovementReadoutMenu
    {
        const string Path = "PARALLAX/Debug/Movement Readout";

        [MenuItem(Path)]
        static void Toggle() => EditorPrefs.SetBool(MovementReadout.EnabledPrefKey, !EditorPrefs.GetBool(MovementReadout.EnabledPrefKey, false));

        [MenuItem(Path, true)]
        static bool ToggleValidate()
        {
            Menu.SetChecked(Path, EditorPrefs.GetBool(MovementReadout.EnabledPrefKey, false));
            return true;
        }
    }
}
