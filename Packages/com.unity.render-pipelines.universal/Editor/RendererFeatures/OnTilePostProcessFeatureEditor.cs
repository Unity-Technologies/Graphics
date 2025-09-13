using UnityEngine;
using UnityEngine.Rendering.Universal;
#if XR_MANAGEMENT_4_0_1_OR_NEWER
using UnityEditor.XR.Management;
#endif

namespace UnityEditor.Rendering.Universal
{
    [CustomEditor(typeof(OnTilePostProcessFeature))]
    internal class OnTilePostProcessFeatureEditor : Editor
    {
        #region Serialized Properties
        private SerializedProperty m_UseFallbackProperty;
        #endregion

        static class Styles
        {
            public static readonly string k_NoSettingsHelpBox = L10n.Tr("This feature performs post-processing operation on tiled memory for Android based untethered XR platforms. There are currently no available settings, they might be added later.");
            public static readonly string k_NonUntetheredXRBuildTarget = L10n.Tr("On Tile PostProcessing feature is not fully supported on the current build target. Please switch to an untethered XR platform as the build target, and enable XR provider through XR Plug-in management. The render feature would fallback to texture read mode(slow off-tile rendering)");
        }

        private void OnEnable()
        {
        }

        bool IsBuildTargetUntetheredXR()
        {
            bool isBuildTargetUntetheredXR = false;
#if XR_MANAGEMENT_4_0_1_OR_NEWER
            var buildTargetGroup = BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget);
            var buildTargetSettings = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(buildTargetGroup);
            if (buildTargetSettings != null && buildTargetSettings.AssignedSettings != null && buildTargetSettings.AssignedSettings.activeLoaders.Count > 0)
            {
                isBuildTargetUntetheredXR = buildTargetGroup == BuildTargetGroup.Android;
            }
#endif
            return isBuildTargetUntetheredXR;
        }

        public override void OnInspectorGUI()
        {
            if (!IsBuildTargetUntetheredXR())
            {
                EditorGUILayout.HelpBox(
                    Styles.k_NonUntetheredXRBuildTarget,
                    MessageType.Error
                );
            }

            EditorGUILayout.HelpBox(Styles.k_NoSettingsHelpBox, MessageType.Info);
        }
    }
}
