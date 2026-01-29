using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor.Compilation;

[InitializeOnLoad]
public static class RenderPipelineJanitor
{
    static RenderPipelineJanitor()
    {
        // This runs automatically every time scripts compile or Unity opens
        AutomateSetup();
    }

    [MenuItem("Tools/Mine Mogul Project Patcher/Force Project Clean & Refresh")]
    public static void ManualTrigger()
    {
        Debug.Log("Janitor: Manual Refresh Triggered...");
        AutomateSetup();
        
        // Forces Unity to scan for file changes and recompile everything
        AssetDatabase.Refresh();
        CompilationPipeline.RequestScriptCompilation();
        
        Debug.Log("Janitor: Refresh Complete. If errors persist, try 'Assets > Reimport All'.");
    }

    private static void AutomateSetup()
    {
        CleanupDrip();   
        MoveDLLs();      
        CleanManifest(); 
        ResetRenderPipeline();
        FixTextShaders();

        if (EditorApplication.isUpdating) return;
        AssetDatabase.Refresh();
    }

    private static void CleanupDrip()
    {
        string[] foldersToDelete = {
            "Assets/TutorialInfo",
            "Assets/MineMogul/Game/Scripts/Unity.TextMeshPro",
            "Assets/MineMogul/Game/Scripts/UnityEngine.UI",
            "Assets/MineMogul/Game/Scripts/DOTween",
            "Assets/MineMogul/Game/Scripts/DOTweenPro",
            "Assets/MineMogul/Game/Scripts/UnityUIExtensions",
            "Assets/MineMogul/Game/Scripts/UnityUIExtensions.examples",
            "Assets/MineMogul/Game/Plugins/Assembly-CSharp-firstpass/DG"
        };
        
        foreach (string path in foldersToDelete)
        {
            if (Directory.Exists(path))
            {
                AssetDatabase.DeleteAsset(path);
                Debug.Log($"Janitor: Removed duplicate assembly folder: {path}");
            }
        }
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
                    if (File.Exists(fullPath + ".meta")) 
                        File.Copy(fullPath + ".meta", destination + ".meta", true);
                }
            }
        }
    }

    public static void CleanManifest()
    {
        string manifestPath = Path.Combine(Directory.GetCurrentDirectory(), "Packages", "manifest.json");
        if (!File.Exists(manifestPath)) return;

        List<string> lines = File.ReadAllLines(manifestPath).ToList();
        string[] forbidden = { "com.unity.render-pipelines.universal", "com.unity.render-pipelines.core" };
        
        int originalCount = lines.Count;
        lines = lines.Where(l => !forbidden.Any(f => l.Contains(f))).ToList();

        if (lines.Count != originalCount)
        {
            File.WriteAllLines(manifestPath, lines);
            Debug.Log("Janitor: Cleaned URP packages from manifest.json");
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

    private static void ResetRenderPipeline()
    {
        GraphicsSettings.defaultRenderPipeline = null;
        for (int i = 0; i < QualitySettings.names.Length; i++) {
            QualitySettings.SetQualityLevel(i);
            QualitySettings.renderPipeline = null;
        }
        AssetDatabase.SaveAssets();
    }
}