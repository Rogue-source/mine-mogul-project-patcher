using UnityEditor;
using UnityEngine;
using NomNom.ProjectPatcher;

namespace MineMogul.Patcher
{
    [UPPatcher("Mine Mogul")]
    public class MineMogulPatcher : PatcherSteps
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