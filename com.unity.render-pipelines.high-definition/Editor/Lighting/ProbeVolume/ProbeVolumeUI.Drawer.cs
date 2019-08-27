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
            Volume = 1 << 0
        }

        readonly static ExpandedState<Expandable, ProbeVolume> k_ExpandedState = new ExpandedState<Expandable, ProbeVolume>(Expandable.Volume, "HDRP");

        public static readonly CED.IDrawer Inspector = CED.Group(
            CED.Group(
                Drawer_ToolBar,
                Drawer_PrimarySettings
                ),
            CED.space,
            CED.FoldoutGroup(
                Styles.k_VolumeHeader,
                Expandable.Volume,
                k_ExpandedState,
                Drawer_AdvancedSwitch,
                Drawer_VolumeContent
                )
            );

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

        static void Drawer_PrimarySettings(SerializedProbeVolume serialized, Editor owner)
        {
            EditorGUILayout.PropertyField(serialized.debugColor, Styles.s_DebugColorLabel);
        }

        static void Drawer_AdvancedSwitch(SerializedProbeVolume serialized, Editor owner)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();

                bool advanced = serialized.advancedFade.boolValue;
                advanced = GUILayout.Toggle(advanced, Styles.s_AdvancedModeContent, EditorStyles.miniButton, GUILayout.Width(60f), GUILayout.ExpandWidth(false));
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

            Vector3 s = serialized.size.vector3Value;
            EditorGUI.BeginChangeCheck();
            if (serialized.advancedFade.boolValue)
            {
                EditorGUI.BeginChangeCheck();
                CoreEditorUtils.DrawVector6(Styles.s_BlendLabel, serialized.positiveFade, serialized.negativeFade, Vector3.zero, s, InfluenceVolumeUI.k_HandlesColor, serialized.size);
                if (EditorGUI.EndChangeCheck())
                {
                    //forbid positive/negative box that doesn't intersect in inspector too
                    Vector3 positive = serialized.positiveFade.vector3Value;
                    Vector3 negative = serialized.negativeFade.vector3Value;
                    for (int axis = 0; axis < 3; ++axis)
                    {
                        if (positive[axis] > 1f - negative[axis])
                        {
                            if (positive == serialized.positiveFade.vector3Value)
                            {
                                negative[axis] = 1f - positive[axis];
                            }
                            else
                            {
                                positive[axis] = 1f - negative[axis];
                            }
                        }
                    }

                    serialized.positiveFade.vector3Value = positive;
                    serialized.negativeFade.vector3Value = negative;
                }
            }
            else
            {
                EditorGUI.BeginChangeCheck();
                float distanceMax = Mathf.Min(s.x, s.y, s.z);
                float uniformFadeDistance = serialized.uniformFade.floatValue * distanceMax;
                uniformFadeDistance = EditorGUILayout.FloatField(Styles.s_BlendLabel, uniformFadeDistance);
                if (EditorGUI.EndChangeCheck())
                {
                    serialized.uniformFade.floatValue = Mathf.Clamp(uniformFadeDistance / distanceMax, 0f, 0.5f);
                }
            }
            if (EditorGUI.EndChangeCheck())
            {
                Vector3 posFade = new Vector3();
                posFade.x = Mathf.Clamp01(serialized.positiveFade.vector3Value.x);
                posFade.y = Mathf.Clamp01(serialized.positiveFade.vector3Value.y);
                posFade.z = Mathf.Clamp01(serialized.positiveFade.vector3Value.z);

                Vector3 negFade = new Vector3();
                negFade.x = Mathf.Clamp01(serialized.negativeFade.vector3Value.x);
                negFade.y = Mathf.Clamp01(serialized.negativeFade.vector3Value.y);
                negFade.z = Mathf.Clamp01(serialized.negativeFade.vector3Value.z);

                serialized.positiveFade.vector3Value = posFade;
                serialized.negativeFade.vector3Value = negFade;
            }

            // Distance fade.
            {
                EditorGUI.BeginChangeCheck();

                float distanceFadeStart = EditorGUILayout.FloatField(Styles.s_DistanceFadeStartLabel, serialized.distanceFadeStart.floatValue);
                float distanceFadeEnd   = EditorGUILayout.FloatField(Styles.s_DistanceFadeEndLabel,   serialized.distanceFadeEnd.floatValue);

                if (EditorGUI.EndChangeCheck())
                {
                    distanceFadeStart = Mathf.Max(0, distanceFadeStart);
                    distanceFadeEnd   = Mathf.Max(distanceFadeStart, distanceFadeEnd);

                    serialized.distanceFadeStart.floatValue = distanceFadeStart;
                    serialized.distanceFadeEnd.floatValue   = distanceFadeEnd;
                }
            }
        }
    }
}
