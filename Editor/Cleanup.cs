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
		GitCleanup();
        CleanManifest();
        DeleteDupedScripts();

        if (EditorApplication.isUpdating) return;
        AssetDatabase.Refresh();
    }
	
	private static void GitCleanup()
{
    if (SessionState.GetBool("GitCleanupPerformed", false)) return;

    string[] onceTargets = {
        "Assets/InputSystem_Actions.inputactions" 
    };

    bool deleted = false;
    foreach (string path in onceTargets)
    {
        if (AssetDatabase.IsValidFolder(path) || File.Exists(path))
        {
            AssetDatabase.DeleteAsset(path);
            deleted = true;
        }
    }

    if (deleted)
    {
        Debug.Log("Cleaned Project");
        SessionState.SetBool("GitCleanupPerformed", true);
        AssetDatabase.Refresh();
    }
}

    private static void CleanManifest()
    {
        string manifestPath = Path.Combine(Directory.GetCurrentDirectory(), "Packages", "manifest.json");
        if (!File.Exists(manifestPath)) return;

        List<string> lines = File.ReadAllLines(manifestPath).ToList();
        
        string[] forbiddenPackages = { 
            "com.unity.inputsystem",
            "com.unity.textmeshpro",
			"com.unity.ugui",
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
            // Core System & UI
            "Assets/MineMogul/Game/Scripts/Unity.InputSystem",
            "Assets/MineMogul/Game/Scripts/Unity.InputSystem.ForUI",
            "Assets/MineMogul/Game/Scripts/UnityEngine.InputSystem",
            "Assets/MineMogul/Game/Scripts/UnityEngine.UI",
            "Assets/MineMogul/Game/Scripts/Unity.TextMeshPro",
			"Assets/MineMogul/Game/Scripts/Unity.ugui",
            "Assets/MineMogul/Game/Scripts/UnityUIExtensions",
            "Assets/MineMogul/Game/Scripts/UnityUIExtensions.examples",

            // Post Processing & Rendering
            "Assets/MineMogul/Game/Scripts/Unity.Postprocessing.Runtime",
            "Assets/MineMogul/Game/Scripts/UnityEngine.Rendering.PostProcessing",
            "Assets/MineMogul/Game/Scripts/PostProcessing",

            // Mathematics & Performance
            "Assets/MineMogul/Game/Scripts/Unity.Mathematics",
            "Assets/MineMogul/Game/Scripts/Unity.Collections",
            "Assets/MineMogul/Game/Scripts/Unity.Burst",
            "Assets/MineMogul/Game/Scripts/Unity.Burst.Unsafe",
            "Assets/MineMogul/Game/Scripts/Unity.Jobs",

            // Specialized Plugins
            "Assets/MineMogul/Game/Scripts/Unity.AI.Navigation",
            "Assets/MineMogul/Game/Scripts/Unity.Animation.Rigging",
            "Assets/MineMogul/Game/Scripts/Unity.Animation.Rigging.DocCodeExamples",
            "Assets/MineMogul/Game/Scripts/Unity.Recorder",
            "Assets/MineMogul/Game/Scripts/Unity.Recorder.Base",
            "Assets/MineMogul/Game/Scripts/Unity.VisualScripting.Antlr3.Runtime",
            "Assets/MineMogul/Game/Scripts/Unity.VisualScripting.Core",
            "Assets/MineMogul/Game/Scripts/Unity.VisualScripting.DocCodeExamples",
            "Assets/MineMogul/Game/Scripts/Unity.VisualScripting.Flow",
            "Assets/MineMogul/Game/Scripts/Unity.VisualScripting.State",
			"Assets/MineMogul/Game/Scripts/Unity.Collections.LowLevel",
			"Assets/MineMogul/Game/Scripts/Unity.Timeline",
            
            // DOTween & DLL Overlaps
            "Assets/MineMogul/Game/Scripts/DemiLib",
            "Assets/MineMogul/Game/Scripts/DOTween",
            "Assets/MineMogul/Game/Scripts/DOTweenPro",
            "Assets/MineMogul/Game/Scripts/DG", 
            "Assets/Plugins/Assembly-CSharp-firstpass",
            "Assets/MineMogul/Plugins/Assembly-CSharp-firstpass",

            // System Libraries
            "Assets/MineMogul/Game/Scripts/System.IO.Hashing",
            "Assets/MineMogul/Game/Scripts/System.Runtime.CompilerServices.Unsafe",
            "Assets/MineMogul/Game/Scripts/SSCC.Runtime",
            "Assets/MineMogul/Game/Scripts/UnityEngine.UnityConsentModule",
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

}