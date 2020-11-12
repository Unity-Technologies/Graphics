using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEditor.Rendering;
using UnityEditorInternal;

// TODO(Nicholas): deduplicate with DensityVolumeUI.Drawer.cs.
namespace UnityEditor.Rendering.HighDefinition
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
                    Drawer_ToolBar,
                    Drawer_AdvancedSwitch,
                    Drawer_VolumeContent,
                    Drawer_BakeToolBar
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
            //EditorGUILayout.PropertyField(serialized.probeVolumeAsset, Styles.s_DataAssetLabel);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Bake Selected"))
            {

            }
            GUILayout.EndHorizontal();
        }

        static void Drawer_ToolBar(SerializedProbeVolume serialized, Editor owner)
        {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            EditMode.DoInspectorToolbar(new[] { ProbeVolumeEditor.k_EditShape, ProbeVolumeEditor.k_EditBlend }, Styles.s_Toolbar_Contents, () =>
                {
                    var bounds = new Bounds();
                    foreach (Component targetObject in owner.targets)
                    {
                        bounds.Encapsulate(targetObject.transform.position);
                    }
                    return bounds;
                },
                owner);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        static void Drawer_AdvancedSwitch(SerializedProbeVolume serialized, Editor owner)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();

                bool advanced = serialized.advancedFade.boolValue;
                advanced = GUILayout.Toggle(advanced, Styles.s_AdvancedModeContent, EditorStyles.miniButton, GUILayout.Width(70f), GUILayout.ExpandWidth(false));
                foreach (var containedBox in ProbeVolumeEditor.blendBoxes.Values)
                {
                    containedBox.monoHandle = !advanced;
                }
                if (serialized.advancedFade.boolValue ^ advanced)
                {
                    serialized.advancedFade.boolValue = advanced;
                }
            }
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
            
            EditorGUILayout.PropertyField(serialized.debugColor, Styles.s_DebugColorLabel);
        }
    }
}
