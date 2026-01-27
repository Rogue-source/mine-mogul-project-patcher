using UnityEditor;
using NomNom.ProjectPatcher;
using NomNom.ProjectPatcher.BepInEx;
using System.IO;

namespace MineMogul.Patcher {
    public class MineMogulPatcher : ProjectPatcherWindow {
        [MenuItem("Tools/Mine Mogul/Project Patcher")]
        public static void Open() {
            GetWindow<MineMogulPatcher>("Mine Mogul Patcher");
        }

        protected override void OnEnable() {
            base.OnEnable();
            var settings = UPPatcherUserSettings.Instance; 
            
            if (settings != null && !string.IsNullOrEmpty(settings.GameFolder)) {
                this.SetPath(settings.GameFolder);
            } 
            else {
                string myDefault = @"C:\SteamLibrary\steamapps\common\MineMogul";
                if (Directory.Exists(myDefault)) {
                    this.SetPath(myDefault);
                }
            }
        }

        protected override string GetDefaultExecutableName() => "Mine Mogul.exe";
        protected override string GetDefaultProjectName() => "MineMogul_Ripped";

        private void SetPath(string path) {
            var pathField = typeof(ProjectPatcherWindow).GetField("_path", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (pathField != null) pathField.SetValue(this, path);
        }
    }
}