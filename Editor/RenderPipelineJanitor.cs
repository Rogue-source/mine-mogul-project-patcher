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
        MoveDLLsToPlugins();
        DeleteConflictingScripts();
        CleanManifest();
        if (GraphicsSettings.defaultRenderPipeline != null)
        {
            GraphicsSettings.defaultRenderPipeline = null;
            string[] qualityNames = QualitySettings.names;
            for (int i = 0; i < qualityNames.Length; i++)
            {
                QualitySettings.SetQualityLevel(i);
                QualitySettings.renderPipeline = null;
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[Patcher] Switched to Built-in Pipeline.");
        }
    }

    private static void MoveDLLsToPlugins()
    {
        string sourceDir = "Assets/MineMogul/Game/AuxiliaryFiles/GameAssemblies";
        string targetDir = "Assets/Plugins/DOTween";

        if (!Directory.Exists(sourceDir)) return;
        if (!Directory.Exists(targetDir)) Directory.CreateDirectory(targetDir);

        string[] targets = { "DOTween.dll", "DOTweenPro.dll" };
        bool movedAnything = false;

        foreach (var dllName in targets)
        {
            string oldPath = Path.Combine(sourceDir, dllName);
            string newPath = Path.Combine(targetDir, dllName);

            if (File.Exists(oldPath) && !File.Exists(newPath))
            {
                FileUtil.MoveFileOrDirectory(oldPath, newPath);
                if (File.Exists(oldPath + ".meta")) 
                    FileUtil.MoveFileOrDirectory(oldPath + ".meta", newPath + ".meta");
                
                movedAnything = true;
            }
        }

        if (movedAnything)
        {
            Debug.Log("[Patcher] Moved DOTween DLLs to Plugins. Fixing Reference Validation...");
            AssetDatabase.Refresh();
            FixDLLValidation(targetDir);
        }
    }

    private static void FixDLLValidation(string folderPath)
    {
        string[] metas = Directory.GetFiles(folderPath, "*.dll.meta");
        foreach (var metaPath in metas)
        {
            string content = File.ReadAllText(metaPath);
            if (content.Contains("validateReferences: 1"))
            {
                content = content.Replace("validateReferences: 1", "validateReferences: 0");
                File.WriteAllText(metaPath, content);
            }
        }
    }

    private static void DeleteConflictingScripts()
    {
        string conflictingPath = "Assets/MineMogul/Game/Plugins/Assembly-CSharp-firstpass/DG/Tweening";
        
        if (Directory.Exists(conflictingPath))
        {
            Debug.Log("[Patcher] Found conflicting DOTween scripts. Deleting directory to resolve Ambiguity (CS0121)...");
            
            AssetDatabase.DeleteAsset(conflictingPath);
            AssetDatabase.Refresh();
        }
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