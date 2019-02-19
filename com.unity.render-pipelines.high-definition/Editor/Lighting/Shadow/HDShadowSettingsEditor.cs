using UnityEditor;
using UnityEditor.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [CanEditMultipleObjects]
    [VolumeComponentEditor(typeof(HDShadowSettings))]
    public class HDShadowSettingsEditor : VolumeComponentEditor
    {
        SerializedDataParameter m_MaxShadowDistance;

        SerializedDataParameter m_CascadeShadowSplitCount;

        SerializedDataParameter[] m_CascadeShadowSplits = new SerializedDataParameter[3];
        SerializedDataParameter[] m_CascadeShadowBorders = new SerializedDataParameter[4];
        private enum Unit { Metric, Percent }
        private static Unit s_Unit = Unit.Metric;

        public override void OnEnable()
        {
            var o = new PropertyFetcher<HDShadowSettings>(serializedObject);

            m_MaxShadowDistance = Unpack(o.Find(x => x.maxShadowDistance));
            m_CascadeShadowSplitCount = Unpack(o.Find(x => x.cascadeShadowSplitCount));
            m_CascadeShadowSplits[0] = Unpack(o.Find(x => x.cascadeShadowSplit0));
            m_CascadeShadowSplits[1] = Unpack(o.Find(x => x.cascadeShadowSplit1));
            m_CascadeShadowSplits[2] = Unpack(o.Find(x => x.cascadeShadowSplit2));
            m_CascadeShadowBorders[0] = Unpack(o.Find(x => x.cascadeShadowBorder0));
            m_CascadeShadowBorders[1] = Unpack(o.Find(x => x.cascadeShadowBorder1));
            m_CascadeShadowBorders[2] = Unpack(o.Find(x => x.cascadeShadowBorder2));
            m_CascadeShadowBorders[3] = Unpack(o.Find(x => x.cascadeShadowBorder3));
        }

        public override void OnInspectorGUI()
        {
            PropertyField(m_MaxShadowDistance, EditorGUIUtility.TrTextContent("Max Distance"));
            Rect firstLine = GUILayoutUtility.GetLastRect();

            EditorGUILayout.Space();
            PropertyField(m_CascadeShadowSplitCount, EditorGUIUtility.TrTextContent("Cascade Count"));

            if (!m_CascadeShadowSplitCount.value.hasMultipleDifferentValues)
            {
                EditorGUI.indentLevel++;
                int cascadeCount = m_CascadeShadowSplitCount.value.intValue;
                for (int i = 0; i < cascadeCount - 1; i++)
                {
                    PropertyField(m_CascadeShadowSplits[i], EditorGUIUtility.TrTextContent(string.Format("Split {0}", i + 1)));
                }

                if (LightLoop.s_UseCascadeBorders)
                {
                    EditorGUILayout.Space();

                    for (int i = 0; i < cascadeCount; i++)
                    {
                        PropertyField(m_CascadeShadowBorders[i], EditorGUIUtility.TrTextContent(string.Format("Border {0}", i + 1)));
                    }
                }

                EditorGUILayout.Space();

                GUILayout.Label("Cascade splits");
                Rect rect = GUILayoutUtility.GetLastRect();
                rect.x += rect.width - 100;
                rect.width = 60f;
                EditorGUI.LabelField(rect, EditorGUIUtility.TrTextContent("Unit"));
                rect.x += 25;
                rect.width = 75;
                s_Unit = (Unit)EditorGUI.EnumPopup(rect, s_Unit);
                ShadowCascadeGUI.DrawCascadeSplitGUI(m_CascadeShadowSplits, LightLoop.s_UseCascadeBorders ? m_CascadeShadowBorders : null, (uint)cascadeCount, blendLastCascade: true, useMetric: s_Unit == Unit.Metric, baseMetric: m_MaxShadowDistance.value.floatValue);
                EditorGUI.indentLevel--;
            }

            HDRenderPipeline hdrp = UnityEngine.Rendering.RenderPipelineManager.currentPipeline as HDRenderPipeline;
            if (hdrp == null)
                return;

            firstLine.y -= EditorGUIUtility.singleLineHeight;
            firstLine.height -= 2;
            firstLine.x += EditorGUIUtility.labelWidth + 20;
            firstLine.width -= EditorGUIUtility.labelWidth + 20;
            bool currentCascadeValue = hdrp.showCascade;
            bool newCascadeValue = GUI.Toggle(firstLine, currentCascadeValue, EditorGUIUtility.TrTextContent("Visualize Cascades"), EditorStyles.miniButton);
            if (currentCascadeValue ^ newCascadeValue)
                hdrp.showCascade = newCascadeValue;
        }
    }
}
