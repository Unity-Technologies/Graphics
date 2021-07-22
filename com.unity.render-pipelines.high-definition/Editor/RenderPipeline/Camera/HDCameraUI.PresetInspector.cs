using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    using CED = CoreEditorDrawer<SerializedHDCamera>;

    static class HDCameraUIPreset
    {
        /// <summary>Enum to store know the expanded state of a expandable section on the camera inspector</summary>
        [HDRPHelpURL("HDRP-Camera")]
        public enum Expandable
        {
            /// <summary> Projection</summary>
            Projection = 1 << 0,
            /// <summary> Physical</summary>
            Physical = 1 << 1,
            /// <summary> Rendering</summary>
            Rendering = 1 << 2,
        }

        static readonly ExpandedState<Expandable, Camera> k_ExpandedState = new ExpandedState<Expandable, Camera>(Expandable.Projection, "HDRP-preset");

        public static readonly CED.IDrawer Inspector = CED.Group(
            CED.FoldoutGroup(
                CameraUI.Styles.projectionSettingsHeaderContent,
                Expandable.Projection,
                k_ExpandedState,
                FoldoutOption.Indent,
                CED.Group(CameraUI.Drawer_Projection), HDCameraUI.PhysicalCamera.DrawerPreset),
            HDCameraUI.Rendering.DrawerPreset
        );
    }
}
