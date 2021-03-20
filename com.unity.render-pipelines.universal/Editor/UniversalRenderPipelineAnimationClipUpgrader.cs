using System.Collections.Generic;

namespace UnityEditor.Rendering.Universal
{
    class UniversalRenderPipelineAnimationClipUpgrader
    {
        [MenuItem("Edit/Rendering/AnimationClips/Convert All AnimationClips to URP Properties")]
        static void UpgradeAllAnimationClipsMenuItem()
        {
            var upgraders = new List<MaterialUpgrader>();
            UniversalRenderPipelineMaterialUpgrader.GetUpgraders(ref upgraders);
            AnimationClipUpgrader.DoUpgradeAllClipsMenuItem(upgraders);
        }

        [MenuItem("Edit/Rendering/AnimationClips/Convert All AnimationClips to URP Properties", true)]
        static bool ValidateUpgradeAllClipsMenuItem() =>
            AnimationClipUpgrader.ValidateUpgradeAllClipsMenuItem();

        [MenuItem("Edit/Rendering/AnimationClips/Convert Selected AnimationClips to URP Properties")]
        static void UpgradeSelectedClipsMenuItem()
        {
            var upgraders = new List<MaterialUpgrader>();
            UniversalRenderPipelineMaterialUpgrader.GetUpgraders(ref upgraders);
            AnimationClipUpgrader.DoUpgradeSelectedClipsMenuItem(upgraders);
        }

        [MenuItem("Edit/Rendering/AnimationClips/Convert Selected AnimationClips to URP Properties", true)]
        static bool ValidateUpgradeSelectedClipsMenuItem() =>
            AnimationClipUpgrader.ValidateUpgradeSelectedClipsMenuItem();
    }
}
