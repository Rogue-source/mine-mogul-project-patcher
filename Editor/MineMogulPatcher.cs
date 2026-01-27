using UnityEditor;
using UnityEngine;
using System.IO;

namespace MineMogul.Patcher {
    public class MineMogulPatcher : EditorWindow {
        private string _gamePath = @"C:\Program Files (x86)\Steam\steamapps\common\Mine Mogul\Mine Mogul_Data";

        [MenuItem("Tools/Mine Mogul/Project Patcher")]
        public static void ShowWindow() {
            GetWindow<MineMogulPatcher>("Mine Mogul Patcher");
        }

        private void OnGUI() {
            GUILayout.Label("Mine Mogul Setup", EditorStyles.boldLabel);
            GUILayout.Space(10);

            EditorGUILayout.LabelField("Game Data Location:");
            EditorGUILayout.BeginHorizontal();
            _gamePath = EditorGUILayout.TextField(_gamePath);
            if (GUILayout.Button("...", GUILayout.Width(30))) {
                string selected = EditorUtility.OpenFolderPanel("Select Mine Mogul_Data Folder", _gamePath, "");
                if (!string.IsNullOrEmpty(selected)) _gamePath = selected;
            }
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(20);

            if (GUILayout.Button("1. Copy Path & Open Patcher", GUILayout.Height(40))) {
                if (Directory.Exists(_gamePath)) {
                    EditorGUIUtility.systemCopyBuffer = _gamePath;
                    Debug.Log($"Path copied to clipboard: {_gamePath}");
                    
                    EditorApplication.ExecuteMenuItem("Tools/Project Patcher");
                    
                    EditorUtility.DisplayDialog("Ready to Patch", 
                        "The Game Path has been copied to your clipboard!\n\n" +
                        "1. Paste it into the 'Game Root Path' field in the window that just opened.\n" +
                        "2. Click 'Run Patcher'.", "Got it");
                } else {
                    EditorUtility.DisplayDialog("Error", "Directory not found! Please check the path.", "OK");
                }
            }
        }
    }
}