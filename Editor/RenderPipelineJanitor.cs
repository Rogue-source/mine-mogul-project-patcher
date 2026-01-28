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
        CleanManifest();
        if (GraphicsSettings.defaultRenderPipeline != null)
        {
            UnityEngine.Debug.Log("[Patcher] Detecting URP/HDRP... Automating switch to Built-in Pipeline.");
            
            GraphicsSettings.defaultRenderPipeline = null;
            
            string[] qualityNames = QualitySettings.names;
            for (int i = 0; i < qualityNames.Length; i++)
            {
                QualitySettings.SetQualityLevel(i);
                QualitySettings.renderPipeline = null;
            }

            AssetDatabase.SaveAssets();
            UnityEngine.Debug.Log("[Patcher] Auto-Fix Complete: Project is now using Built-in Render Pipeline.");
        }
    }

    public static void CleanManifest()
    {
        string manifestPath = Path.Combine(Directory.GetCurrentDirectory(), "Packages", "manifest.json");
        
        if (!File.Exists(manifestPath)) return;

        try
        {
            string[] lines = File.ReadAllLines(manifestPath);
            List<string> updatedLines = new List<string>();
            bool modified = false;
			
            string[] forbiddenPackages = {
                "com.unity.render-pipelines.universal",
                "com.unity.render-pipelines.core",
                "com.unity.render-pipelines.high-definition"
            };

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                bool isForbidden = forbiddenPackages.Any(p => line.Contains(p));

                if (isForbidden)
                {
                    modified = true;
                    if (updatedLines.Count > 0 && i == lines.Length - 2) 
                    {
                        string lastLine = updatedLines[updatedLines.Count - 1];
                        if (lastLine.TrimEnd().EndsWith(","))
                        {
                            updatedLines[updatedLines.Count - 1] = lastLine.TrimEnd().TrimEnd(',');
                        }
                    }
                    continue; 
                }
                updatedLines.Add(line);
            }

            if (modified)
            {
                File.WriteAllLines(manifestPath, updatedLines);
                UnityEngine.Debug.Log("[Patcher] Scrubbed URP/HDRP from manifest.json. Unity will now refresh.");
                AssetDatabase.Refresh();
            }
        }
        catch (System.Exception e)
        {
            UnityEngine.Debug.LogError($"[Patcher] Failed to scrub manifest: {e.Message}");
        }
    }
}