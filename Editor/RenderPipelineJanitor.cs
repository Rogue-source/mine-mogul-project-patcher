using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine;

[InitializeOnLoad]
public static class RenderPipelineJanitor
{
    static RenderPipelineJanitor()
    {
        AutomateSetup();
    }

    private static void AutomateSetup()
    {
        ResetRenderPipeline();
        FixTextShaders();

        if (EditorApplication.isUpdating) return;
        AssetDatabase.Refresh();
    }

    private static void FixTextShaders()
    {
        Shader targetSDF = Shader.Find("TextMeshPro/Mobile/Distance Field");
        if (targetSDF == null) return;

        string[] materialGuids = AssetDatabase.FindAssets("t:Material");
        foreach (string guid in materialGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);

            if (mat != null && mat.shader != null)
            {
                if (mat.shader.name.Contains("Universal Render Pipeline/TextMeshPro") || 
                    mat.shader.name == "TextMeshPro/Distance Field" ||
                    mat.name.Contains("SDF")) 
                {
                    mat.shader = targetSDF;
                    mat.EnableKeyword("OUTLINE_OFF");
                    EditorUtility.SetDirty(mat);
                }
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