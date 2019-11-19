using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    [CanEditMultipleObjects]
    [VolumeComponentEditor(typeof(HDShadowSettings))]
    class HDShadowSettingsEditor : VolumeComponentEditor
    {
        SerializedDataParameter m_MaxShadowDistance;

        SerializedDataParameter m_DirectionalTransmissionMultiplier;
        SerializedDataParameter m_CascadeShadowSplitCount;

        SerializedDataParameter[] m_CascadeShadowSplits = new SerializedDataParameter[3];
        SerializedDataParameter[] m_CascadeShadowBorders = new SerializedDataParameter[4];
        private enum Unit { Metric, Percent }
        EditorPrefBoolFlags<Unit> m_State;

        public HDShadowSettingsEditor()
        {
            string Key = string.Format("{0}:{1}:UI_State", "HDRP", typeof(HDShadowSettingsEditor).Name);
            m_State = new EditorPrefBoolFlags<Unit>(Key);
        }


        public override void OnEnable()
        {
            var o = new PropertyFetcher<HDShadowSettings>(serializedObject);

            m_MaxShadowDistance = Unpack(o.Find(x => x.maxShadowDistance));
            m_DirectionalTransmissionMultiplier = Unpack(o.Find(x => x.directionalTransmissionMultiplier));
            m_CascadeShadowSplitCount = Unpack(o.Find(x => x.cascadeShadowSplitCount));
            m_CascadeShadowSplits[0] = Unpack(o.Find(x => x.cascadeShadowSplit0));
            m_CascadeShadowSplits[1] = Unpack(o.Find(x => x.cascadeShadowSplit1));
            m_CascadeShadowSplits[2] = Unpack(o.Find(x => x.cascadeShadowSplit2));
            m_CascadeShadowBorders[0] = Unpack(o.Find(x => x.cascadeShadowBorder0));
            m_CascadeShadowBorders[1] = Unpack(o.Find(x => x.cascadeShadowBorder1));
            m_CascadeShadowBorders[2] = Unpack(o.Find(x => x.cascadeShadowBorder2));
            m_CascadeShadowBorders[3] = Unpack(o.Find(x => x.cascadeShadowBorder3));

            (serializedObject.targetObject as HDShadowSettings).InitNormalized(m_State.value == Unit.Percent);
        }

        public override void OnInspectorGUI()
        {
            PropertyField(m_MaxShadowDistance, EditorGUIUtility.TrTextContent("Max Distance", "In Meter"));
            Rect firstLine = GUILayoutUtility.GetLastRect();

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Directional Light");

            Rect shiftedRect = EditorGUILayout.GetControlRect();
            shiftedRect.x += 20;
            shiftedRect.width -= 20;
            EditorGUI.BeginChangeCheck();
            Unit unit = (Unit)EditorGUI.EnumPopup(shiftedRect, EditorGUIUtility.TrTextContent("Working Unit", "Except Max Distance which will be still in meter"), m_State.value);
            if (EditorGUI.EndChangeCheck())
            {
                m_State.value = unit;
                (serializedObject.targetObject as HDShadowSettings).InitNormalized(m_State.value == Unit.Percent);
            }

            PropertyField(m_DirectionalTransmissionMultiplier, EditorGUIUtility.TrTextContent("Transmission  Multiplier"));

            EditorGUI.BeginChangeCheck();
            PropertyField(m_CascadeShadowSplitCount, EditorGUIUtility.TrTextContent("Cascade Count"));
            if (EditorGUI.EndChangeCheck())
            {
                //fix newly activated cascade split not respecting ordering
                for (int i = 1; i < m_CascadeShadowSplitCount.value.intValue - 1; i++)
                {
                    if (m_CascadeShadowSplits[i - 1].value.floatValue > m_CascadeShadowSplits[i].value.floatValue)
                        m_CascadeShadowSplits[i].value.floatValue = m_CascadeShadowSplits[i - 1].value.floatValue;
                }
            }

            if (!m_CascadeShadowSplitCount.value.hasMultipleDifferentValues)
            {
                EditorGUI.indentLevel++;
                int cascadeCount = m_CascadeShadowSplitCount.value.intValue;
                for (int i = 0; i < cascadeCount - 1; i++)
                {
                    PropertyField(m_CascadeShadowSplits[i], EditorGUIUtility.TrTextContent(string.Format("Split {0}", i + 1)));
                }

                if (HDRenderPipeline.s_UseCascadeBorders)
                {
                    EditorGUILayout.Space();

                    for (int i = 0; i < cascadeCount; i++)
                    {
                        PropertyField(m_CascadeShadowBorders[i], EditorGUIUtility.TrTextContent(string.Format("Border {0}", i + 1)));
                    }
                }

                EditorGUILayout.Space();

                GUILayout.Label("Cascade splits");
                ShadowCascadeGUI.DrawCascadeSplitGUI(m_CascadeShadowSplits, HDRenderPipeline.s_UseCascadeBorders ? m_CascadeShadowBorders : null, (uint)cascadeCount, blendLastCascade: true, useMetric: unit == Unit.Metric, baseMetric: m_MaxShadowDistance.value.floatValue);
                EditorGUI.indentLevel--;
            }

            HDRenderPipeline hdrp = UnityEngine.Rendering.RenderPipelineManager.currentPipeline as HDRenderPipeline;
            if (hdrp == null)
                return;

            Rect visualizeCascade = firstLine;
            visualizeCascade.y -= (EditorGUIUtility.singleLineHeight - 2);
            visualizeCascade.height -= 2;
            visualizeCascade.x += EditorGUIUtility.labelWidth + 20;
            visualizeCascade.width -= EditorGUIUtility.labelWidth + 20;
            bool currentCascadeValue = hdrp.showCascade;
            bool newCascadeValue = GUI.Toggle(visualizeCascade, currentCascadeValue, EditorGUIUtility.TrTextContent("Visualize Cascades"), EditorStyles.miniButton);
            if (currentCascadeValue ^ newCascadeValue)
                hdrp.showCascade = newCascadeValue;
        }
    }


    [VolumeParameterDrawer(typeof(CascadePartitionSplitParameter))]
    sealed class CascadePartitionSplitParameterDrawer : VolumeParameterDrawer
    {
        public override bool OnGUI(SerializedDataParameter parameter, GUIContent title)
        {
            var value = parameter.value;

            if (value.propertyType != SerializedPropertyType.Float)
                return false;

            var o = parameter.GetObjectRef<CascadePartitionSplitParameter>();
            float max = o.normalized ? 100f : o.representationDistance;
            float modifiableValue = value.floatValue * max;
            EditorGUI.BeginChangeCheck();
            modifiableValue = EditorGUILayout.Slider(title, modifiableValue, 0f, max);
            if (EditorGUI.EndChangeCheck())
            {
                modifiableValue /= max;
                value.floatValue = Mathf.Clamp(modifiableValue, o.min, o.max);
            }
            return true;
        }
    }

    [VolumeParameterDrawer(typeof(CascadeEndBorderParameter))]
    sealed class CascadeEndBorderParameterDrawer : VolumeParameterDrawer
    {
        public override bool OnGUI(SerializedDataParameter parameter, GUIContent title)
        {
            var value = parameter.value;

            if (value.propertyType != SerializedPropertyType.Float)
                return false;

            var o = parameter.GetObjectRef<CascadeEndBorderParameter>();
            float max = o.normalized ? 100f : o.representationDistance;
            float modifiableValue = value.floatValue * max;
            EditorGUI.BeginChangeCheck();
            modifiableValue = EditorGUILayout.Slider(title, modifiableValue, 0f, max);
            if(EditorGUI.EndChangeCheck())
                value.floatValue = Mathf.Clamp01(modifiableValue / max);
            return true;
        }
    }
}
