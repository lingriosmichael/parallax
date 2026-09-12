using System.Collections.Generic;
using Parallax.Gameplay.Cameras;
using Parallax.Gameplay.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Parallax.Editor
{
    public static class CameraSetup
    {
        const string CatPlayerName = "Cat_Player";

        [MenuItem("PARALLAX/Setup/Configure Camera Follow")]
        public static void Configure()
        {
            GameObject cameraGO = GameObject.FindWithTag("MainCamera");
            if (cameraGO == null)
            {
                Debug.LogError("CameraSetup: no GameObject tagged 'MainCamera' found in the active scene. Stopping.");
                return;
            }

            var cam = cameraGO.GetComponent<Camera>();
            if (cam == null || !cam.orthographic)
            {
                Debug.LogError($"CameraSetup: '{cameraGO.name}' has no orthographic Camera component. Stopping.");
                return;
            }

            GameObject catGO = GameObject.Find(CatPlayerName);
            if (catGO == null)
            {
                Debug.LogError($"CameraSetup: no GameObject named '{CatPlayerName}' found in the active scene. Stopping.");
                return;
            }

            var changes = new List<string>();

            var follow = cameraGO.GetComponent<CatCameraFollow>();
            if (follow == null)
            {
                follow = cameraGO.AddComponent<CatCameraFollow>();
                changes.Add("added CatCameraFollow");
            }

            var followSO = new SerializedObject(follow);

            var targetProp = followSO.FindProperty("target");
            if (targetProp.objectReferenceValue == null)
            {
                targetProp.objectReferenceValue = catGO.transform;
                changes.Add($"assigned CatCameraFollow.target = {CatPlayerName}");
            }

            var respawnProp = followSO.FindProperty("respawn");
            if (respawnProp.objectReferenceValue == null)
            {
                var respawn = catGO.GetComponent<CatRespawn>();
                if (respawn != null)
                {
                    respawnProp.objectReferenceValue = respawn;
                    changes.Add($"assigned CatCameraFollow.respawn = {CatPlayerName}'s CatRespawn");
                }
            }

            followSO.ApplyModifiedPropertiesWithoutUndo();

            if (changes.Count == 0)
            {
                Debug.Log("CameraSetup: Camera Follow already configured.");
            }
            else
            {
                Debug.Log($"CameraSetup: {string.Join("; ", changes)}.");
                EditorSceneManager.MarkSceneDirty(cameraGO.scene);
            }
        }
    }
}
