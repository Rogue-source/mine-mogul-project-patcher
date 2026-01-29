using System.Collections.Generic;
using System.IO;
using Cysharp.Threading.Tasks;
using Nomnom.UnityProjectPatcher.Editor.Steps;
using UnityEditor;
using UnityEngine;

namespace Rogue.MineMogulProjectPatcher.Editor {
    public struct ReplaceShadersStep : IPatcherStep {
        [MenuItem("Tools/R.E.P.O. Project Patcher/Replace Shaders")]
        static void MenuItem() {
            ReplaceShaders();
        }

        static void ReplaceShaders() {
            AssetDatabase.StartAssetEditing();

            var count = 0;
            
            try {
                var gameShadersFolder = "Assets/REPO/Game/Shaders";
                var materialsFolder = "Assets/REPO/Game/Materials";
                var customShadersFolder = $"Packages/{Constants.PackageName}/Shaders";
            
                var shaderReplacements = new Dictionary<Shader, Shader>();
                
                // gather custom written replacement shaders
                var customGuids = AssetDatabase.FindAssets("t:Shader", new[] { customShadersFolder });
                foreach (var guid in customGuids) {
                    var customPath = AssetDatabase.GUIDToAssetPath(guid);
                    var originalPath = customPath.Replace(customShadersFolder, gameShadersFolder);
                
                    var custom = AssetDatabase.LoadAssetAtPath<Shader>(customPath);
                    var original = AssetDatabase.LoadAssetAtPath<Shader>(originalPath);
                    
                    shaderReplacements.Add(original, custom);
                }
                
                Debug.Log($"Found {customGuids.Length} custom shaders");
                
                var materialGuids = AssetDatabase.FindAssets("t:Material", new[] { materialsFolder });
                foreach (var guid in materialGuids) {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    var material = AssetDatabase.LoadAssetAtPath<Material>(path);

                    if (shaderReplacements.TryGetValue(material.shader, out var replacement)) {
                        Debug.Log($"Replacing shader in {material.name}", material);
                        
                        Undo.RecordObject(material, "Replace Shader");
                        material.shader = replacement;
                        EditorUtility.SetDirty(material);

                        count++;
                    }
                }
            }
            finally {
                AssetDatabase.StopAssetEditing();
            }
            
            Debug.Log($"Replaced shader in {count} materials, saving...");
            
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
        
        public UniTask<StepResult> Run() {
            ReplaceShaders();
            return UniTask.FromResult(StepResult.Success);
        }

        public void OnComplete(bool failed) { }
    }
}