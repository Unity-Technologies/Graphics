#if ENABLE_VR && ENABLE_XR_MANAGEMENT
using UnityEditor.XR;
#endif

using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    using CED = CoreEditorDrawer<SerializedHDCamera>;

    static partial class HDCameraUI
    {
        /// <summary>Enum to store know the expanded state of a expandable section on the camera inspector</summary>
        [HDRPHelpURL("hdrp-camera-component-reference")]
        public enum Expandable
        {
            /// <summary> Projection</summary>
            Projection = 1 << 0,
            /// <summary> Physical</summary>
            Physical = 1 << 1,
            /// <summary> Output</summary>
            Output = 1 << 2,
            /// <summary> Orthographic</summary>
            Orthographic = 1 << 3,
            /// <summary> RenderLoop</summary>
            RenderLoop = 1 << 4,
            /// <summary> Rendering</summary>
            Rendering = 1 << 5,
            /// <summary> Environment</summary>
            Environment = 1 << 6,
        }


        static readonly ExpandedState<Expandable, Camera> k_ExpandedState = new ExpandedState<Expandable, Camera>(Expandable.Projection, "HDRP");

#if ENABLE_VR && ENABLE_XR_MANAGEMENT
        private static readonly CED.IDrawer OrthographicXRError = CED.Conditional(
            (serialized, owner) => serialized.xrRendering.boolValue && serialized.baseCameraSettings.orthographic.boolValue, (serialized, owner) =>
            {
                var buildTargetGroup = BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget);
                var buildTargetSettings = XR.Management.XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(buildTargetGroup);
                if(buildTargetSettings != null && buildTargetSettings.AssignedSettings != null && buildTargetSettings.AssignedSettings.activeLoaders.Count > 0)
                    EditorGUILayout.HelpBox("Orthographic projection is not supported in XR. Please change the Camera Projection setting to Perspective to avoid rendering issues", MessageType.Warning);
            });
#endif

        public static readonly CED.IDrawer SectionProjectionSettings = CED.FoldoutGroup(
            CameraUI.Styles.projectionSettingsHeaderContent,
            Expandable.Projection,
            k_ExpandedState,
            FoldoutOption.Indent,
#if ENABLE_VR && ENABLE_XR_MANAGEMENT
            OrthographicXRError,
#endif
            CED.Group(
                CameraUI.Drawer_Projection
                ),
            PhysicalCamera.Drawer
        );

        public static readonly CED.IDrawer[] Inspector = new[]
        {
            SectionProjectionSettings,
            Rendering.Drawer,
            Environment.Drawer,
            Output.Drawer,
        };
    }
}
