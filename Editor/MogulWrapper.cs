using Nomnom.UnityProjectPatcher.Editor;
using Nomnom.UnityProjectPatcher.Editor.Steps;

namespace Rogue.MineMogulProjectPatcher.Editor {
    [UPPatcher("com.rogue.unity-minemogul-project-patcher")]
    public static class MineMogulWrapper {
        public static void GetSteps(StepPipeline stepPipeline) {
            stepPipeline.SetInputSystem(InputSystemType.Both);
            stepPipeline.SetGameViewResolution("16:9");
            stepPipeline.OpenSceneAtEnd("Main");
            
            stepPipeline.InsertLast(new FixES3Step());
        }
    }
}