using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

/// <summary>
/// One-shot fixes for all three platform scenes.
/// Run via: ZeroDay Platform > Fix All Scenes
/// </summary>
public static class FixScenes
{
    static readonly string[] ScenePaths = new[]
    {
        "Assets/Scenes/LoginScene.unity",
        "Assets/Scenes/LobbyScene.unity",
        "Assets/Scenes/ZeroDaySlots.unity",
    };

    [MenuItem("ZeroDay Platform/Fix All Scenes")]
    public static void FixAll()
    {
        foreach (var path in ScenePaths)
        {
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);

            FixCamera(path);
            FixInputModule(path);

            EditorSceneManager.SaveScene(scene, path);
            Debug.Log($"[ZeroDay] Fixed: {path}");
        }

        AssetDatabase.Refresh();
        Debug.Log("[ZeroDay] All scenes fixed.");
    }

    static void FixCamera(string path)
    {
        // Add a camera if none exists
        var cam = Object.FindAnyObjectByType<Camera>();
        if (cam != null) return;

        var camGO = new GameObject("Main Camera");
        camGO.tag = "MainCamera";
        var c = camGO.AddComponent<Camera>();
        c.clearFlags       = CameraClearFlags.SolidColor;
        c.backgroundColor  = new Color(0.04f, 0.04f, 0.09f, 1f); // dark bg
        c.orthographic     = true;
        c.depth            = -1;
        camGO.AddComponent<AudioListener>();
        Debug.Log($"[ZeroDay] Added camera to {path}");
    }

    static void FixInputModule(string path)
    {
        foreach (var go in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
        {
            var old = go.GetComponentInChildren<StandaloneInputModule>(true);
            if (old == null) continue;

            var esGO = old.gameObject;
            Object.DestroyImmediate(old);

            if (esGO.GetComponent<InputSystemUIInputModule>() == null)
                esGO.AddComponent<InputSystemUIInputModule>();

            Debug.Log($"[ZeroDay] Fixed InputModule in {path}");
        }
    }
}
