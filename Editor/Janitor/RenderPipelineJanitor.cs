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
        FixMetaSettings("Assets/Plugins/DOTween");
        FixMetaSettings("Assets/MineMogul/Plugins");
        DeleteConflictingScripts();
        CleanManifest();
        ResetRenderPipeline();
        FixTextShaders();

        if (EditorApplication.isUpdating) return;
        AssetDatabase.Refresh();
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
                    string metaPath = fullPath + ".meta";
                    if (File.Exists(metaPath)) File.Copy(metaPath, destination + ".meta", true);
                    
                    Debug.Log($"[Patcher] Successfully pulled {fileName} from project root into Plugins.");
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
        string[] materialGuids = AssetDatabase.FindAssets("t:Material");
        Shader builtinSDF = Shader.Find("TextMeshPro/Mobile/Distance Field");

        if (builtinSDF == null) return;

        foreach (string guid in materialGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);

            if (mat != null && mat.shader != null && mat.shader.name.Contains("Universal Render Pipeline/TextMeshPro"))
            {
                mat.shader = builtinSDF;
                EditorUtility.SetDirty(mat);
            }
        }
    }

    private static void DeleteConflictingScripts()
    {
        string path = "Assets/MineMogul/Game/Plugins/Assembly-CSharp-firstpass/DG/Tweening";
        if (Directory.Exists(path)) AssetDatabase.DeleteAsset(path);
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