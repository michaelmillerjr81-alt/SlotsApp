using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

/// <summary>
/// Replaces StandaloneInputModule with InputSystemUIInputModule in all platform scenes.
/// Run via: ZeroDay Platform > Fix Input Modules
/// </summary>
public static class FixInputModules
{
    static readonly string[] ScenePaths = new[]
    {
        "Assets/Scenes/LoginScene.unity",
        "Assets/Scenes/LobbyScene.unity",
        "Assets/Scenes/ZeroDaySlots.unity",
    };

    [MenuItem("ZeroDay Platform/Fix Input Modules")]
    public static void Fix()
    {
        foreach (var path in ScenePaths)
        {
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);

            foreach (var go in scene.GetRootGameObjects())
            {
                var old = go.GetComponentInChildren<StandaloneInputModule>(true);
                if (old == null) continue;

                var esGO = old.gameObject;
                Object.DestroyImmediate(old);

                if (esGO.GetComponent<InputSystemUIInputModule>() == null)
                    esGO.AddComponent<InputSystemUIInputModule>();

                Debug.Log($"[ZeroDay] Fixed InputModule in {path}");
            }

            EditorSceneManager.SaveScene(scene, path);
        }

        AssetDatabase.Refresh();
        Debug.Log("[ZeroDay] All input modules fixed.");
    }
}
