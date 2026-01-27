using UnityEditor;
using UnityEngine;
using Nomnom.UnityProjectPatcher.Editor;
using System.IO;

namespace MineMogul.Patcher {
    public class MineMogulPatcher : EditorWindow {
        private string _gamePath = @"C:\SteamLibrary\steamapps\common\MineMogul";

        [MenuItem("Tools/Mine Mogul/Project Patcher")]
        public static void ShowWindow() {
            GetWindow<MineMogulPatcher>("Mine Mogul Patcher");
        }

        private void OnGUI() {
            GUILayout.Label("Mine Mogul Project Patcher", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            _gamePath = EditorGUILayout.TextField("Game Directory:", _gamePath);
            if (GUILayout.Button("...", GUILayout.Width(30))) {
                _gamePath = EditorUtility.OpenFolderPanel("Select Mine Mogul Folder", _gamePath, "");
            }
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(20);

            if (GUILayout.Button("Run Patcher", GUILayout.Height(40))) {
                RunPatcherLogic();
            }
        }

        private void RunPatcherLogic() {
            if (!Directory.Exists(_gamePath)) {
                EditorUtility.DisplayDialog("Error", "Selected directory does not exist!", "OK");
                return;
            }

            // This sets the settings for the core NomNom patcher manually
            var settings = UPPatcherUserSettings.Instance;
            settings.GameFolder = _gamePath;
            settings.ProjectName = "MineMogul_Ripped";
            
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();

            Debug.Log("Path assigned. You can now use the 'Unity Project Patcher' window to click Patch.");
            // Open the core window for them to finish the job
            EditorApplication.ExecuteMenuItem("Tools/Project Patcher");
        }
    }
}