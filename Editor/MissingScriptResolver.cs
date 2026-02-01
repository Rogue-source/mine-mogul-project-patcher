using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;
using System.Linq;

public class MissingScriptResolver : EditorWindow
{
    [MenuItem("Tools/Repair Missing Scripts")]
    public static void ShowWindow()
    {
        GetWindow<MissingScriptResolver>("Script Repair");
    }

    private void OnGUI()
    {
        if (GUILayout.Button("Repair All Scenes (Deep Scan)"))
        {
            RepairAllScenes();
        }
    }

    private static void RepairAllScenes()
    {
        string[] scenePaths = EditorBuildSettings.scenes
            .Where(s => s.enabled)
            .Select(s => s.path)
            .ToArray();

        foreach (string path in scenePaths)
        {
            Scene AssetRipperScene = EditorSceneManager.OpenScene(path);
            Debug.Log($"[Repair] Scanning Scene: {path}");
            
            GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>(true);
            foreach (GameObject go in allObjects)
            {
                Component[] components = go.GetComponents<Component>();
                for (int i = 0; i < components.Length; i++)
                {
                    if (components[i] == null)
                    {
                        AttemptRepair(go, i);
                    }
                }
            }
            EditorSceneManager.SaveScene(AssetRipperScene);
        }
        Debug.Log("[Repair] Deep Scan Complete.");
    }

    private static void AttemptRepair(GameObject go, int index)
    {
        Debug.LogWarning($"[Repair] Found missing script on {go.name} at index {index}.");
    }
}