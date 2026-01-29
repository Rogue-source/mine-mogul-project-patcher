using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor.Compilation;
using System;

[InitializeOnLoad]
public static class RenderPipelineJanitor
{
    private const string Version = "1.0.2-Final";

    static RenderPipelineJanitor()
    {
        AutomateSetup();
    }

    [MenuItem("Tools/MineMogul Project Patcher/Fix Errors")]
    public static void ManualTrigger()
    {
        Debug.Log($"Janitor v{Version}: Manual Repair Triggered...");
        AutomateSetup();
        AssetDatabase.Refresh();
        CompilationPipeline.RequestScriptCompilation();
    }

    private static void AutomateSetup()
    {
        Debug.Log($"Janitor v{Version}: Running Cleanup...");
        
        CleanupDrip();   
        MoveDLLs();      
        CleanManifest(); 
        ResetRenderPipeline();
        FixProjectSettings();
        StopTMPPopup();  
        FixTextShaders();
        EnsureUIFunctionality();

        if (EditorApplication.isUpdating) return;
        AssetDatabase.Refresh();
    }

    private static void FixProjectSettings()
    {
        PlayerSettings.graphicsJobs = false;
        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64, false);
        GraphicsDeviceType[] apis = { GraphicsDeviceType.Direct3D12, GraphicsDeviceType.Direct3D11 };
        PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64, apis);
    }

    private static void StopTMPPopup()
    {
        if (!Directory.Exists("Assets/TextMesh Pro"))
        {
            Directory.CreateDirectory("Assets/TextMesh Pro");
        }
    }

    private static void CleanupDrip()
    {
        string[] targets = {
            "Assets/TutorialInfo",
            "Assets/MineMogul/Game/Scripts/Unity.TextMeshPro",
            "Assets/MineMogul/Game/Scripts/UnityEngine.UI",
            "Assets/MineMogul/Game/Scripts/DOTween",
            "Assets/MineMogul/Game/Scripts/DOTweenPro",
            "Assets/MineMogul/Game/Scripts/UnityUIExtensions",
            "Assets/MineMogul/Game/Scripts/UnityUIExtensions.examples",
            "Assets/MineMogul/Game/Plugins/Assembly-CSharp-firstpass/DG",
            "Assets/MineMogul/Game/Scripts/Unity.Animation.Rigging",
            "Assets/MineMogul/Game/Scripts/Unity.Animation.Rigging.DocCodeExamples",
            "Assets/MineMogul/Game/ComputeShader/Lut3DBaker.asset",
            "Assets/MineMogul/Game/ComputeShader/Lut3DBaker.compute"
        };
        
        foreach (string path in targets)
        {
            if (Directory.Exists(path) || File.Exists(path))
            {
                AssetDatabase.DeleteAsset(path);
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
                try 
                {
                    string destination = Path.Combine(targetDir, fileName);
                    File.Copy(fullPath, destination, true);
                    if (File.Exists(fullPath + ".meta")) 
                        File.Copy(fullPath + ".meta", destination + ".meta", true);
                }
                catch (IOException) { }
            }
        }
    }

    public static void CleanManifest()
    {
        string manifestPath = Path.Combine(Directory.GetCurrentDirectory(), "Packages", "manifest.json");
        if (!File.Exists(manifestPath)) return;

        List<string> lines = File.ReadAllLines(manifestPath).ToList();
        
        // Removed the crashing project-patcher and deprecated TMP
        string[] forbidden = { 
            "com.unity.render-pipelines.universal", 
            "com.unity.render-pipelines.core",
            "com.nomnom.unity-project-patcher-bepinex",
            "com.unity.textmeshpro"
        };
        
        lines = lines.Where(l => !forbidden.Any(f => l.Contains(f))).ToList();
        File.WriteAllLines(manifestPath, lines);
    }

    private static void FixTextShaders()
    {
        Shader normalSDF = Shader.Find("TextMeshPro/Mobile/Distance Field");
        Shader overlaySDF = Shader.Find("TextMeshPro/Distance Field Overlay");

        string[] overlayMaterials = {
            "Roboto-ExtraBold SDF Material",
            "Roboto_Condensed-ExtraBold SDF Material",
            "Roboto_Condensed-Regular SDF Material"
        };

        foreach (string guid in AssetDatabase.FindAssets("t:Material"))
        {
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
            if (mat == null) continue;

            if (overlayMaterials.Any(name => mat.name.Contains(name)))
            {
                if (overlaySDF != null && mat.shader != overlaySDF)
                {
                    mat.shader = overlaySDF;
                    EditorUtility.SetDirty(mat);
                }
            }
            else if (mat.shader == null || mat.shader.name.Contains("InternalErrorShader") || mat.name.Contains("SDF"))
            {
                if (normalSDF != null)
                {
                    mat.shader = normalSDF;
                    EditorUtility.SetDirty(mat);
                }
            }
        }
    }

    private static void EnsureUIFunctionality()
    {
        // Force-create EventSystem if missing
        if (UnityEngine.Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.EventSystems.StandaloneInputModule));
        }

        // Clean up Raycasters that often break in ripped projects
        var raycasters = UnityEngine.Object.FindObjectsByType<UnityEngine.UI.GraphicRaycaster>(FindObjectsSortMode.None);
        foreach (var ray in raycasters)
        {
            if (!ray.enabled) ray.enabled = true;
        }
    }

    private static void ResetRenderPipeline()
    {
        GraphicsSettings.defaultRenderPipeline = null;
        for (int i = 0; i < QualitySettings.names.Length; i++) {
            QualitySettings.SetQualityLevel(i);
            QualitySettings.renderPipeline = null;
        }
    }
}