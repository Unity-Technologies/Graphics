using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEditor.Rendering;

// TODO(Nicholas): deduplicate with LocalVolumetricFogUI.Drawer.cs.
namespace UnityEditor.Experimental.Rendering
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
                Drawer_VolumeContent
            )
        );

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

            var rect = EditorGUILayout.GetControlRect(true);
            EditorGUI.BeginProperty(rect, Styles.s_MinMaxSubdivSlider, serialized.minSubdivisionMultiplier);
            EditorGUI.BeginProperty(rect, Styles.s_MinMaxSubdivSlider, serialized.maxSubdivisionMultiplier);

            // Round min and max subdiv
            float maxSubdiv = ProbeReferenceVolume.instance.GetMaxSubdivision(1) - 1;
            float min = Mathf.Round(serialized.minSubdivisionMultiplier.floatValue * maxSubdiv) / maxSubdiv;
            float max = Mathf.Round(serialized.maxSubdivisionMultiplier.floatValue * maxSubdiv) / maxSubdiv;

            EditorGUILayout.MinMaxSlider(Styles.s_MinMaxSubdivSlider, ref min, ref max, 0, 1);
            serialized.minSubdivisionMultiplier.floatValue = Mathf.Max(0.01f, min);
            serialized.maxSubdivisionMultiplier.floatValue = Mathf.Max(0.01f, max);
            EditorGUI.EndProperty();
            EditorGUI.EndProperty();

            EditorGUILayout.HelpBox($"The probe subdivision will fluctuate between {ProbeReferenceVolume.instance.GetMaxSubdivision(serialized.minSubdivisionMultiplier.floatValue)} and {ProbeReferenceVolume.instance.GetMaxSubdivision(serialized.maxSubdivisionMultiplier.floatValue)}", MessageType.Info);
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
