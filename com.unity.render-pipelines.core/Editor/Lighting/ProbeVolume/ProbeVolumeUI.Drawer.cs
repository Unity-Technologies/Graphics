using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor.Rendering;
using UnityEditorInternal;

// TODO(Nicholas): deduplicate with DensityVolumeUI.Drawer.cs.
namespace UnityEditor.Rendering
{
    using CED = CoreEditorDrawer<SerializedProbeVolume>;

    static partial class ProbeVolumeUI
    {
        [System.Flags]
        enum Expandable
        {
            Volume = 1 << 0,
            Probes = 1 << 1,
            Baking = 1 << 2
        }

        readonly static ExpandedState<Expandable, ProbeVolume> k_ExpandedStateVolume = new ExpandedState<Expandable, ProbeVolume>(Expandable.Volume, "HDRP");
        readonly static ExpandedState<Expandable, ProbeVolume> k_ExpandedStateProbes = new ExpandedState<Expandable, ProbeVolume>(Expandable.Probes, "HDRP");
        readonly static ExpandedState<Expandable, ProbeVolume> k_ExpandedStateBaking = new ExpandedState<Expandable, ProbeVolume>(Expandable.Baking, "HDRP");

        internal static readonly CED.IDrawer Inspector = CED.Group(
            CED.Group(
                Drawer_FeatureWarningMessage
                ),
            CED.Conditional(
                IsFeatureDisabled,
                Drawer_FeatureEnableInfo
                ),
            CED.Conditional(
                IsFeatureEnabled,
                CED.Group(
                    Drawer_VolumeContent
                )
            )
        );

        static bool IsFeatureEnabled(SerializedProbeVolume serialized, Editor owner)
        {
            return true;
        }

        static bool IsFeatureDisabled(SerializedProbeVolume serialized, Editor owner)
        {
            return false;
        }

        static void Drawer_FeatureWarningMessage(SerializedProbeVolume serialized, Editor owner)
        {
            EditorGUILayout.HelpBox(Styles.k_featureWarning, MessageType.Warning);
        }

        static void Drawer_FeatureEnableInfo(SerializedProbeVolume serialized, Editor owner)
        {
            EditorGUILayout.HelpBox(Styles.k_featureEnableInfo, MessageType.Error);
        }

        static void Drawer_BakeToolBar(SerializedProbeVolume serialized, Editor owner)
        {
        }

        static void Drawer_ToolBar(SerializedProbeVolume serialized, Editor owner)
        {
        }

        static void Drawer_VolumeContent(SerializedProbeVolume serialized, Editor owner)
        {
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(serialized.size, Styles.s_Size);
            if (EditorGUI.EndChangeCheck())
            {
                Vector3 tmpClamp = serialized.size.vector3Value;
                tmpClamp.x = Mathf.Max(0f, tmpClamp.x);
                tmpClamp.y = Mathf.Max(0f, tmpClamp.y);
                tmpClamp.z = Mathf.Max(0f, tmpClamp.z);
                serialized.size.vector3Value = tmpClamp;
            }
        }
    }
}
