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
        MoveAndFixDLLs();
        DeleteConflictingScripts();
        CleanManifest();
        ResetRenderPipeline();
        
        if (EditorApplication.isUpdating) return;
        AssetDatabase.Refresh();
    }

    private static void MoveAndFixDLLs()
    {
        string sourceDir = "Assets/MineMogul/Game/AuxiliaryFiles/GameAssemblies";
        string pluginDir = "Assets/Plugins/DOTween";
        string gameDllDir = "Assets/MineMogul/Plugins"; 

        if (!Directory.Exists(pluginDir)) Directory.CreateDirectory(pluginDir);

        if (Directory.Exists(sourceDir))
        {
            string[] targets = { "DOTween.dll", "DOTweenPro.dll" };
            foreach (var dll in targets)
            {
                string oldPath = Path.Combine(sourceDir, dll);
                string newPath = Path.Combine(pluginDir, dll);
                if (File.Exists(oldPath) && !File.Exists(newPath))
                {
                    FileUtil.MoveFileOrDirectory(oldPath, newPath);
                    if (File.Exists(oldPath + ".meta")) FileUtil.MoveFileOrDirectory(oldPath + ".meta", newPath + ".meta");
                }
            }
        }

        FixMetaSettings(pluginDir);
        if (Directory.Exists(gameDllDir)) FixMetaSettings(gameDllDir);
    }

    private static void FixMetaSettings(string path)
    {
        string[] metas = Directory.GetFiles(path, "*.dll.meta", SearchOption.AllDirectories);
        foreach (var metaPath in metas)
        {
            string content = File.ReadAllText(metaPath);
            bool changed = false;

            if (content.Contains("isPredefined: 0")) { content = content.Replace("isPredefined: 0", "isPredefined: 1"); changed = true; }
            if (content.Contains("validateReferences: 1")) { content = content.Replace("validateReferences: 1", "validateReferences: 0"); changed = true; }

            if (changed) 
            {
                File.WriteAllText(metaPath, content);
            }
        }
    }

    private static void DeleteConflictingScripts()
    {
        string conflictingPath = "Assets/MineMogul/Game/Plugins/Assembly-CSharp-firstpass/DG/Tweening";
        if (Directory.Exists(conflictingPath)) AssetDatabase.DeleteAsset(conflictingPath);
    }

    private static void ResetRenderPipeline()
    {
        if (GraphicsSettings.defaultRenderPipeline == null) return;
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
        string[] forbidden = { "com.unity.render-pipelines.universal", "com.unity.render-pipelines.core" };
        var lines = File.ReadAllLines(manifestPath).Where(l => !forbidden.Any(f => l.Contains(f))).ToList();
        File.WriteAllLines(manifestPath, lines);
    }
}