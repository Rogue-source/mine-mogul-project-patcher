using UnityEditor;
using UnityEngine.Rendering;

[InitializeOnLoad]
public static class RenderPipelineJanitor
{
    static RenderPipelineJanitor()
    {
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
}