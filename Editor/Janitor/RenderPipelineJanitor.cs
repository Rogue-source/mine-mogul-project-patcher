using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine;
using System.IO;
using System.Linq;
using System.Collections.Generic;

[InitializeOnLoad]
public static class RenderPipelineJanitor
{
    private static int lastFileCount = -1;
    private static float stabilityTimer = 0;
    private const float WAIT_TIME = 4.0f;

    private static string GetRipperPath() 
    {
        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        return Path.Combine(projectRoot, "AssetRipperOutput");
    }
    
    static RenderPipelineJanitor()
    {
        EditorApplication.update += WatchForRipperOutput;
    }

    private static void WatchForRipperOutput()
    {
        if (EditorApplication.isUpdating || EditorApplication.isCompiling) return;

        string targetPath = GetRipperPath();
        if (!Directory.Exists(targetPath)) return;

        string[] currentFiles = Directory.GetFiles(targetPath, "*.*", SearchOption.AllDirectories);
        
        if (currentFiles.Length == 0) return;

        if (currentFiles.Length != lastFileCount)
        {
            lastFileCount = currentFiles.Length;
            stabilityTimer = (float)EditorApplication.timeSinceStartup;
            return;
        }

        if (EditorApplication.timeSinceStartup - stabilityTimer < WAIT_TIME) return;

        Debug.Log($"[Patcher] Ripper folder stable with {currentFiles.Length} files. Starting automated import...");
        
        EditorApplication.update -= WatchForRipperOutput;
        RunFullCleanup(targetPath);
    }

    private static void RunFullCleanup(string ripperSource)
    {
        MoveDLLs();
        MoveMeshes(ripperSource);
        FixMetaSettings("Assets/Plugins/DOTween");
        FixMetaSettings("Assets/MineMogul/Plugins");
        DeleteConflictingScripts();
        CleanManifest();
        ResetRenderPipeline();
        FixTextShaders();
        RelinkMissingAssets();

        AssetDatabase.Refresh();
        Debug.Log("[Patcher] Automated Setup Complete. Meshes and Materials should now be linked.");
    }

    private static void MoveMeshes(string searchRoot)
    {
        string targetDir = "Assets/MineMogul/Meshes";
        if (!Directory.Exists(targetDir)) Directory.CreateDirectory(targetDir);

        string[] extensions = { "*.fbx", "*.obj", "*.asset" };
        foreach (var ext in extensions)
        {
            string[] files = Directory.GetFiles(searchRoot, ext, SearchOption.AllDirectories);
            foreach (string fullPath in files)
            {
                if (fullPath.Contains(Application.dataPath.Replace("/", "\\"))) continue;
                
                string fileName = Path.GetFileName(fullPath);
                string destination = Path.Combine(targetDir, fileName);

                if (!File.Exists(destination)) File.Copy(fullPath, destination, true);
            }
        }
    }

    private static void RelinkMissingAssets()
    {
        string[] allPrefabGuids = AssetDatabase.FindAssets("t:Prefab");
        foreach (string guid in allPrefabGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = PrefabUtility.LoadPrefabContents(path);
            bool changed = false;

            foreach (var filter in prefab.GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter.sharedMesh == null)
                {
                    Mesh foundMesh = FindAssetByName<Mesh>(filter.gameObject.name);
                    if (foundMesh != null) { filter.sharedMesh = foundMesh; changed = true; }
                }
            }

            foreach (var renderer in prefab.GetComponentsInChildren<MeshRenderer>(true))
            {
                Material[] sharedMats = renderer.sharedMaterials;
                for (int i = 0; i < sharedMats.Length; i++)
                {
                    if (sharedMats[i] == null)
                    {
                        Material foundMat = FindAssetByName<Material>(renderer.gameObject.name);
                        if (foundMat != null) { sharedMats[i] = foundMat; changed = true; }
                    }
                }
                if (changed) renderer.sharedMaterials = sharedMats;
            }

            if (changed) PrefabUtility.SaveAsPrefabAsset(prefab, path);
            PrefabUtility.UnloadPrefabContents(prefab);
        }
    }

    private static T FindAssetByName<T>(string name) where T : UnityEngine.Object
    {
        string typeFilter = typeof(T) == typeof(Mesh) ? "t:Mesh" : "t:Material";
        string[] guids = AssetDatabase.FindAssets($"{name} {typeFilter}");
        if (guids.Length > 0) return AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guids[0]));
        return null;
    }

    private static void MoveDLLs()
    {
        string targetDir = "Assets/Plugins/DOTween";
        if (!Directory.Exists(targetDir)) Directory.CreateDirectory(targetDir);
        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        foreach (string fullPath in Directory.GetFiles(projectRoot, "*.dll", SearchOption.AllDirectories))
        {
            string fileName = Path.GetFileName(fullPath);
            if ((fileName == "DOTween.dll" || fileName == "DOTweenPro.dll") && !fullPath.Contains("Assets/Plugins/DOTween"))
                File.Copy(fullPath, Path.Combine(targetDir, fileName), true);
        }
    }

    private static void FixMetaSettings(string path)
    {
        if (!Directory.Exists(path)) return;
        foreach (var metaPath in Directory.GetFiles(path, "*.dll.meta", SearchOption.AllDirectories))
        {
            string content = File.ReadAllText(metaPath);
            content = content.Replace("isPredefined: 0", "isPredefined: 1").Replace("validateReferences: 1", "validateReferences: 0");
            File.WriteAllText(metaPath, content);
        }
    }

    private static void FixTextShaders()
    {
        Shader targetSDF = Shader.Find("TextMeshPro/Mobile/Distance Field");
        if (targetSDF == null) return;
        foreach (string guid in AssetDatabase.FindAssets("t:Material"))
        {
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
            if (mat != null && mat.shader != null && (mat.shader.name.Contains("Universal") || mat.name.Contains("SDF")))
            {
                mat.shader = targetSDF;
                EditorUtility.SetDirty(mat);
            }
        }
    }

    private static void DeleteConflictingScripts()
    {
        string[] paths = { "Assets/MineMogul/Game/Plugins/Assembly-CSharp-firstpass/DG/Tweening", "Assets/TutorialInfo" };
        foreach (var path in paths) if (Directory.Exists(path)) AssetDatabase.DeleteAsset(path);
    }

    private static void ResetRenderPipeline()
    {
        GraphicsSettings.defaultRenderPipeline = null;
        for (int i = 0; i < QualitySettings.names.Length; i++) { QualitySettings.SetQualityLevel(i); QualitySettings.renderPipeline = null; }
    }

    private static void CleanManifest()
    {
        string manifestPath = Path.Combine(Directory.GetCurrentDirectory(), "Packages", "manifest.json");
        if (!File.Exists(manifestPath)) return;
        var forbidden = new[] { "com.unity.render-pipelines.universal", "com.unity.render-pipelines.core" };
        var lines = File.ReadAllLines(manifestPath).Where(l => !forbidden.Any(f => l.Contains(f))).ToList();
        File.WriteAllLines(manifestPath, lines);
    }
}