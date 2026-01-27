using System.IO;
using UnityEditor;
using UnityEngine;

namespace MineMogul.Patcher
{
    [Nomnom.UnityProjectPatcher.Editor.UPPatcherAttribute("Mine Mogul")]
    public class MineMogulPatcher : Nomnom.UnityProjectPatcher.Editor.PatcherSteps
    {
        public override string DefaultProjectName => "MineMogul_Ripped";
        public override string GameExecutableName => "Mine Mogul.exe";
        public override int SteamAppId => 3846120;

        public override void OnBeginPatcher()
        {
            base.OnBeginPatcher();
        }
    }
}