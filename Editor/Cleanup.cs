using UnityEditor;
using UnityEngine;
using System.IO;
using System.Linq;
using System.Collections.Generic;

[InitializeOnLoad]
public static class Cleanup
{
    static Cleanup()
    {
        EditorApplication.delayCall += RunCleanup;
    }

    [MenuItem("Tools/Cleanup/Force Run")]
    public static void ManualTrigger()
    {
        RunCleanup();
        AssetDatabase.Refresh();
    }

    private static void RunCleanup()
    {
        CleanManifest();
        ImportTMPEssentials();
        DeleteDupedScripts();

        if (EditorApplication.isUpdating) return;
        AssetDatabase.Refresh();
    }

    private static void CleanManifest()
    {
        string manifestPath = Path.Combine(Directory.GetCurrentDirectory(), "Packages", "manifest.json");
        if (!File.Exists(manifestPath)) return;

        List<string> lines = File.ReadAllLines(manifestPath).ToList();
        
        string[] forbiddenPackages = { 
            "com.unity.inputsystem",
            "com.unity.textmeshpro",
            "com.unity.burst",
            "com.unity.mathematics",
            "com.unity.collections",
            "com.unity.jobs",
            "com.unity.animation.rigging",
            "com.unity.visualscripting",
            "com.unity.postprocessing",
            "com.unity.timeline",
            "com.unity.ai.navigation",
            "com.unity.recorder"
        };
        
        int initialCount = lines.Count;
        lines = lines.Where(l => !forbiddenPackages.Any(f => l.Contains(f))).ToList();

        if (lines.Count != initialCount)
        {
            File.WriteAllLines(manifestPath, lines);
        }
    }

    private static void DeleteDupedScripts()
    {
        string[] pathsToDelete = {
            "Assets/MineMogul/Game/Scripts/Unity.InputSystem",
            "Assets/MineMogul/Game/Scripts/Unity.InputSystem.ForUI",
            "Assets/MineMogul/Game/Scripts/UnityEngine.InputSystem",
            "Assets/MineMogul/Game/Scripts/DOTween",
            "Assets/MineMogul/Game/Scripts/DOTweenPro",
            "Assets/MineMogul/Game/Scripts/DG", 
            "Assets/Plugins/Assembly-CSharp-firstpass",
            "Assets/MineMogul/Plugins/Assembly-CSharp-firstpass",
            "Assets/MineMogul/Game/Scripts/UnityUIExtensions",
            "Assets/MineMogul/Game/Scripts/System.IO.Hashing",
            "Assets/MineMogul/Game/Scripts/System.Runtime.CompilerServices.Unsafe",
            "Assets/MineMogul/Game/Scripts/UnityEngine.UI",
            "Assets/MineMogul/Game/Scripts/Unity.TextMeshPro",
            "Assets/MineMogul/Game/Scripts/SSCC.Runtime",
            "Assets/MineMogul/Game/Scripts/Unity.Postprocessing",
            "Assets/MineMogul/Game/Scripts/UnityEngine.Rendering.PostProcessing",
            "Assets/TutorialInfo"
        };

        foreach (string path in pathsToDelete)
        {
            if (AssetDatabase.IsValidFolder(path) || File.Exists(path))
            {
                AssetDatabase.DeleteAsset(path);
            }
        }
    }

    private static void ImportTMPEssentials()
    {
        if (Directory.Exists("Assets/TextMesh Pro/Resources")) return;

        string packagePath = "Packages/com.unity.textmeshpro/Package Resources/TMP Essential Resources.unitypackage";
        
        if (File.Exists(packagePath))
        {
            AssetDatabase.ImportPackage(packagePath, false);
        }
    }
}