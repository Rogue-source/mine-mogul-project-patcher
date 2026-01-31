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
    private const string Version = "2.3.0";

    static RenderPipelineJanitor()
    {
        EditorApplication.delayCall += AutomateSetup;
    }

    [MenuItem("Tools/MineMogul Project Patcher/Repair Test")]
    public static void ManualTrigger()
    {
        AutomateSetup();
        AssetDatabase.Refresh();
    }

    private static void AutomateSetup()
    {
		CleanManifest();
		ImportTMP();
        CleanupDrip();   
        CleanupBrokenCode(); 
        MoveDLLs();      
        FixProjectSettings();
        RepairBrokenEventSystem();

        if (EditorApplication.isUpdating) return;
        AssetDatabase.Refresh();
    }

    private static void CleanupBrokenCode()
    {
        if (!PlayerSettings.allowUnsafeCode)
        {
            PlayerSettings.allowUnsafeCode = true;
        }

        string[] brokenPaths = {
            "Assets/MineMogul/Game/Scripts/UnityEngine.UnityConsentModule",
            "Assets/MineMogul/Game/Scripts/System.Runtime.CompilerServices.Unsafe",
            "Assets/MineMogul/Game/Scripts/System.IO.Hashing",
            "Assets/MineMogul/Game/Scripts/System.Memory",
            "Assets/MineMogul/Game/Scripts/System.Buffers",
          //  "Assets/MineMogul/Game/Scripts/SSCC.Runtime",
            "Assets/MineMogul/Game/Scripts/Unity.Animation.Rigging.DocCodeExamples",
            "Assets/MineMogul/Game/Scripts/UnityUIExtensions",
            "AssetRipperOutput/ExportedProject/Assets/Scripts/UnityEngine.UnityConsentModule",
            "AssetRipperOutput/ExportedProject/Assets/Scripts/System.Runtime.CompilerServices.Unsafe"
        };

        bool changesMade = false;
        foreach (string path in brokenPaths)
        {
            if (path.StartsWith("Assets"))
            {
                if (AssetDatabase.IsValidFolder(path) || File.Exists(path))
                {
                    AssetDatabase.DeleteAsset(path);
                    changesMade = true;
                }
            }
            else
            {
                string root = Directory.GetParent(Application.dataPath).FullName;
                string fullPath = Path.Combine(root, path);
                if (Directory.Exists(fullPath)) 
                {
                    Directory.Delete(fullPath, true);
                }
            }
        }

        if (changesMade) AssetDatabase.Refresh();
    }
	
	private static void ImportTMP()
    {
        if (Directory.Exists("Assets/TextMesh Pro/Resources")) return;

        string packagePath = "Packages/com.unity.textmeshpro/Package Resources/TMP Essential Resources.unitypackage";
        
        if (File.Exists(packagePath))
        {
            AssetDatabase.ImportPackage(packagePath, false);
            Debug.Log("Imported TMP Essential Resources.");
        }
        else
        {
            EditorApplication.ExecuteMenuItem("Window/TextMeshPro/Import TMP Essential Resources");
        }
    }
	
	public static void CleanManifest()
    {
        string manifestPath = Path.Combine(Directory.GetCurrentDirectory(), "Packages", "manifest.json");
        if (!File.Exists(manifestPath)) return;

        List<string> lines = File.ReadAllLines(manifestPath).ToList();
        
        string[] forbidden = { 
            "com.unity.render-pipelines.universal", 
            "com.unity.render-pipelines.core",
            "com.unity.textmeshpro",
            "com.unity.inputsystem" 
        };
        
        int initialCount = lines.Count;
        lines = lines.Where(l => !forbidden.Any(f => l.Contains(f))).ToList();

        if (lines.Count != initialCount)
        {
            File.WriteAllLines(manifestPath, lines);
            Debug.Log("Removed official InputSystem package to resolve DLL conflict.");
            AssetDatabase.Refresh();
        }
    }
	
    private static void FixProjectSettings()
    {
        PlayerSettings.graphicsJobs = false;
        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64, false);
        GraphicsDeviceType[] apis = { GraphicsDeviceType.Direct3D11, GraphicsDeviceType.Direct3D12 };
        PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64, apis);

        PlayerSettings.colorSpace = ColorSpace.Linear;

        RenderSettings.ambientMode = AmbientMode.Skybox;
        RenderSettings.subtractiveShadowColor = new Color(0.2f, 0.2f, 0.2f);
        
        Lightmapping.giWorkflowMode = Lightmapping.GIWorkflowMode.OnDemand;
    }

    private static void RepairBrokenEventSystem()
    {
        var es = UnityEngine.Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>();
        if (es == null)
        {
            new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.EventSystems.StandaloneInputModule));
            return;
        }

        GameObject esObj = es.gameObject;
        var components = esObj.GetComponents<Component>();
        foreach (var c in components)
        {
            if (c == null) GameObject.DestroyImmediate(c);
        }

        if (esObj.GetComponent<UnityEngine.EventSystems.StandaloneInputModule>() == null)
        {
            esObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
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
            "Assets/MineMogul/Game/Plugins/Assembly-CSharp-firstpass/DG",
            "Assets/MineMogul/Game/ComputeShader/Lut3DBaker.asset",
            "Assets/MineMogul/Game/ComputeShader/Lut3DBaker.compute",
            "Assets/MineMogul/Game/Scripts/Unity.Animation.Rigging",
            "Assets/MineMogul/Game/Shaders/PostProcessing/Resources/Lut3DBaker.compute"
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
}