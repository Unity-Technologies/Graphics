using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    using CED = CoreEditorDrawer<SerializedHDCamera>;

    static partial class HDCameraUI
    {
        partial class Environment
        {
            public static readonly CED.IDrawer Drawer = CED.FoldoutGroup(
                CameraUI.Environment.Styles.header,
                Expandable.Environment,
                k_ExpandedState,
                FoldoutOption.Indent,
                CED.Group(
                    Drawer_Environment_Background,
                    CameraUI.Environment.Drawer_Environment_VolumeLayerMask,
                    Drawer_Environment_VolumeAnchorOverride,
                    Drawer_Environment_ProbeLayerMask
                )
            );

            static void Drawer_Environment_Background(SerializedHDCamera p, Editor owner)
            {
                EditorGUILayout.PropertyField(p.clearColorMode, Styles.backgroundType);
                if (p.clearColorMode.GetEnumValue<HDAdditionalCameraData.ClearColorMode>() == HDAdditionalCameraData.ClearColorMode.Color)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(p.backgroundColorHDR, Styles.backgroundColor);
                    EditorGUI.indentLevel--;
                }

                if (p.clearDepth.boolValue == false)
                    p.clearDepth.boolValue = true;
            }

            static void Drawer_Environment_VolumeAnchorOverride(SerializedHDCamera p, Editor owner)
            {
                EditorGUILayout.PropertyField(p.volumeAnchorOverride, Styles.volumeAnchorOverride);
            }

            static void Drawer_Environment_ProbeLayerMask(SerializedHDCamera p, Editor owner)
            {
                EditorGUILayout.PropertyField(p.probeLayerMask, Styles.probeLayerMask);
            }
        }
    }
}
