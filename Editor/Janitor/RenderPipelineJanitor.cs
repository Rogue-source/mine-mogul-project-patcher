using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine;
using System.IO;
using System.Linq;
using System.Collections.Generic;

[InitializeOnLoad]
public static class RenderPipelineJanitor
{
    static RenderPipelineJanitor()
    {
        AutomateSetup();
    }

    private static void AutomateSetup()
    {
        MoveDLLs();
        MoveMeshes();
        FixMetaSettings("Assets/Plugins/DOTween");
        FixMetaSettings("Assets/MineMogul/Plugins");
        DeleteConflictingScripts();
        CleanManifest();
        ResetRenderPipeline();
        FixTextShaders();
        RelinkMissingAssets();

        if (EditorApplication.isUpdating) return;
        AssetDatabase.Refresh();
    }

    private static void MoveMeshes()
    {
        string targetDir = "Assets/MineMogul/Meshes";
        if (!Directory.Exists(targetDir)) Directory.CreateDirectory(targetDir);

        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        string[] extensions = { "*.fbx", "*.obj", "*.asset" };
        
        foreach (var ext in extensions)
        {
            string[] files = Directory.GetFiles(projectRoot, ext, SearchOption.AllDirectories);
            foreach (string fullPath in files)
            {
                if (fullPath.Contains("\\Assets\\") || fullPath.Contains("\\Library\\") || fullPath.Contains("\\Packages\\")) 
                    continue;

                string fileName = Path.GetFileName(fullPath);
                string destination = Path.Combine(targetDir, fileName);

                if (!File.Exists(destination))
                {
                    File.Copy(fullPath, destination, true);
                    if (File.Exists(fullPath + ".meta")) File.Copy(fullPath + ".meta", destination + ".meta", true);
                    Debug.Log($"[Patcher] Imported Mesh: {fileName}");
                }
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

            var filters = prefab.GetComponentsInChildren<MeshFilter>(true);
            foreach (var filter in filters)
            {
                if (filter.sharedMesh == null)
                {
                    Mesh foundMesh = FindAssetByName<Mesh>(filter.gameObject.name);
                    if (foundMesh != null)
                    {
                        filter.sharedMesh = foundMesh;
                        changed = true;
                    }
                }
            }

            if (changed)
            {
                PrefabUtility.SaveAsPrefabAsset(prefab, path);
            }
            PrefabUtility.UnloadPrefabContents(prefab);
        }
    }

    private static T FindAssetByName<T>(string name) where T : Object
    {
        string typeFilter = typeof(T) == typeof(Mesh) ? "t:Mesh" : "t:Material";
        string[] guids = AssetDatabase.FindAssets($"{name} {typeFilter}");
        if (guids.Length > 0)
        {
            return AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }
        return null;
    }

    private static void MoveDLLs()
    {
        string targetDir = "Assets/Plugins/DOTween";
        string[] targets = { "DOTween.dll", "DOTweenPro.dll" };
        if (!Directory.Exists(targetDir)) Directory.CreateDirectory(targetDir);
        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        string[] allFiles = Directory.GetFiles(projectRoot, "*.dll", SearchOption.AllDirectories);
        foreach (string fullPath in allFiles)
        {
            string fileName = Path.GetFileName(fullPath);
            if (targets.Contains(fileName) && !fullPath.Contains("Assets/Plugins/DOTween"))
            {
                string destination = Path.Combine(targetDir, fileName);
                if (!File.Exists(destination))
                {
                    File.Copy(fullPath, destination, true);
                    if (File.Exists(fullPath + ".meta")) File.Copy(fullPath + ".meta", destination + ".meta", true);
                }
            }
        }
    }

    private static void FixMetaSettings(string path)
    {
        if (!Directory.Exists(path)) return;
        string[] metas = Directory.GetFiles(path, "*.dll.meta", SearchOption.AllDirectories);
        foreach (var metaPath in metas)
        {
            string content = File.ReadAllText(metaPath);
            bool changed = false;
            if (content.Contains("isPredefined: 0")) { content = content.Replace("isPredefined: 0", "isPredefined: 1"); changed = true; }
            if (content.Contains("validateReferences: 1")) { content = content.Replace("validateReferences: 1", "validateReferences: 0"); changed = true; }
            if (changed) File.WriteAllText(metaPath, content);
        }
    }

    private static void FixTextShaders()
    {
        Shader targetSDF = Shader.Find("TextMeshPro/Mobile/Distance Field");
        if (targetSDF == null) return;
        string[] materialGuids = AssetDatabase.FindAssets("t:Material");
        foreach (string guid in materialGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat != null && mat.shader != null)
            {
                if (mat.shader.name.Contains("Universal Render Pipeline/TextMeshPro") || mat.shader.name == "TextMeshPro/Distance Field" || mat.name.Contains("SDF")) 
                {
                    mat.shader = targetSDF;
                    mat.EnableKeyword("OUTLINE_OFF");
                    EditorUtility.SetDirty(mat);
                }
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
        for (int i = 0; i < QualitySettings.names.Length; i++) {
            QualitySettings.SetQualityLevel(i);
            QualitySettings.renderPipeline = null;
        }
        AssetDatabase.SaveAssets();
    }

    public static void CleanManifest()
    {
        string manifestPath = Path.Combine(Directory.GetCurrentDirectory(), "Packages", "manifest.json");
        if (!File.Exists(manifestPath)) return;
        List<string> lines = File.ReadAllLines(manifestPath).ToList();
        string[] forbidden = { "com.unity.render-pipelines.universal", "com.unity.render-pipelines.core", "com.unity.textmeshpro" };
        lines = lines.Where(l => !forbidden.Any(f => l.Contains(f))).ToList();
        if (!lines.Any(l => l.Contains("com.unity.ugui")))
        {
            int insertIndex = lines.FindIndex(l => l.Contains("dependencies")) + 1;
            lines.Insert(insertIndex, "    \"com.unity.ugui\": \"2.0.0\",");
        }
        File.WriteAllLines(manifestPath, lines);
    }
}