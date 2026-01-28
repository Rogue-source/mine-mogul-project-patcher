using System;
using System.Collections.Generic;
using System.IO;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using Nomnom.UnityProjectPatcher.Editor.Steps;
using UnityEditor;
using UnityEngine;

namespace Rogue.MineMogulProjectPatcher.Editor {

    public struct GeneratePhotonAssembliesStep : IPatcherStep {
        [MenuItem("Tools/Mine Mogul Project Patcher/Generate Photon Assembly Definitions")]
        static void MenuItem() {
            GenerateDefinitions();
            EditorUtility.RequestScriptReload();
        }
        
        static readonly Dictionary<string, string[]> Dependencies = new() {
            { "Photon3Unity3D", Array.Empty<string>() },
            { "PhotonChat", new [] {
                "Photon3Unity3D"
            } },
            { "PhotonRealtime", new [] {
                "Photon3Unity3D"
            } },
            { "PhotonUnityNetworking", new [] {
                "Photon3Unity3D", 
                "PhotonRealtime"
            } },
            { "PhotonUnityNetworking.Utilities", new [] {
                "Photon3Unity3D", 
                "PhotonRealtime", 
                "PhotonUnityNetworking"
            } },
            { "PhotonVoice", new [] {
                "Photon3Unity3D", 
                "PhotonRealtime", 
                "PhotonVoice.API"
            } },
            { "PhotonVoice.API", new [] {
                "Photon3Unity3D", 
                "PhotonRealtime"
            } },
            { "PhotonVoice.PUN", new [] {
                "Photon3Unity3D", 
                "PhotonRealtime", 
                "PhotonUnityNetworking",
                "PhotonVoice", 
                "PhotonVoice.API"
            } },
        };
        
        public UniTask<StepResult> Run() {
            GenerateDefinitions();

            return UniTask.FromResult(StepResult.Recompile);
        }

        static void GenerateDefinitions() {
            var scriptsFolder = Path.Combine(Application.dataPath, "Mine Mogul", "Game", "Scripts");
            
            foreach (var (assembly, dependencies) in Dependencies) {
                var folderPath = Path.Combine(scriptsFolder, assembly);
                var asmdefPath = Path.Combine(folderPath, $"{assembly}.asmdef");

                var asmDef = new Dictionary<string, object> {
                    { "name", assembly },
                    { "references", dependencies }
                };
                
                var json = JsonConvert.SerializeObject(asmDef, Formatting.Indented);
                File.WriteAllText(asmdefPath, json);
                
                Debug.Log($"Generated assembly definition for {assembly}");
            }
            
            Debug.Log($"Successfully generated Photon assembly definitions, recompiling...");
        }

        public void OnComplete(bool failed) { }
    }
}