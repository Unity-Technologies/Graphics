using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal
{
    using CED = CoreEditorDrawer<UniversalRenderPipelineSerializedCamera>;

    static class UniversalRenderPipelineCameraUIPreset
    {
        /// <summary>Enum to store know the expanded state of a expandable section on the camera inspector</summary>
        [URPHelpURL("camera-component-reference")]
        public enum Expandable
        {
            /// <summary> Projection</summary>
            Projection = 1 << 0,
            /// <summary> Physical</summary>
            Physical = 1 << 1,
            /// <summary> Rendering</summary>
            Rendering = 1 << 2,
        }

        static readonly ExpandedState<Expandable, Camera> k_ExpandedState = new(Expandable.Projection, "URP");

        public static readonly CED.IDrawer Inspector = CED.Group(
            CED.FoldoutGroup(
                CameraUI.Styles.projectionSettingsHeaderContent,
                Expandable.Projection,
                k_ExpandedState,
                FoldoutOption.Indent,
                CED.Group(CameraUI.Drawer_Projection), UniversalRenderPipelineCameraUI.PhysicalCamera.Drawer),
            UniversalRenderPipelineCameraUI.Rendering.DrawerPreset
        );
    }
}
