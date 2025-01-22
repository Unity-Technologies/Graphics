using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(HDShadowSettings))]
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

        int labelWidthAdjustment => enableOverrides ? 3 : 30;

        public override void OnInspectorGUI()
        {
            HDEditorUtils.EnsureFrameSetting(FrameSettingsField.ShadowMaps);

            PropertyField(m_MaxShadowDistance, EditorGUIUtility.TrTextContent("Max Distance", "In Meter"));

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Directional Light");
            Unit unit;

            int workingUnitDropdownIndentation = 9 /*offset*/ + 14 /*checkbox width*/ + 3 /*vertical spacing*/;
            if (!enableOverrides)
                workingUnitDropdownIndentation = 9 /*offset*/ + 2 /*magic offset*/ + 3 /*vertical spacing*/;
            using (new IndentLevelScope(workingUnitDropdownIndentation))
            {
                EditorGUIUtility.labelWidth -= labelWidthAdjustment; //not sure why the field is decalled. Seams to be a miss in vertical spacing
                Rect shiftedRect = EditorGUILayout.GetControlRect();
                EditorGUI.BeginChangeCheck();
                unit = (Unit)EditorGUI.EnumPopup(shiftedRect, EditorGUIUtility.TrTextContent("Working Unit", "Except Max Distance which will be still in meter"), m_State.value);
                if (EditorGUI.EndChangeCheck())
                {
                    m_State.value = unit;
                    (serializedObject.targetObject as HDShadowSettings).InitNormalized(m_State.value == Unit.Percent);
                }
                EditorGUIUtility.labelWidth += labelWidthAdjustment;
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

            int cascadeCount;
            using (new IndentLevelScope())
            {
                cascadeCount = m_CascadeShadowSplitCount.value.intValue;
                Debug.Assert(cascadeCount <= 4); // If we add support for more than 4 cascades, then we should add new entries in the next line
                string[] cascadeOrder = { "first", "second", "third" };

                for (int i = 0; i < cascadeCount - 1; i++)
                {
                    string tooltipOverride = (unit == Unit.Metric) ?
                        $"Distance from the Camera (in meters) to the {cascadeOrder[i]} cascade split." :
                        $"Distance from the Camera (as a percentage of Max Distance) to the {cascadeOrder[i]} cascade split.";
                    PropertyField(m_CascadeShadowSplits[i], EditorGUIUtility.TrTextContent(string.Format("Split {0}", i + 1), tooltipOverride));
                }

                if (HDRenderPipeline.s_UseCascadeBorders)
                {
                    EditorGUILayout.Space();

                    for (int i = 0; i < cascadeCount; i++)
                    {
                        PropertyField(m_CascadeShadowBorders[i], EditorGUIUtility.TrTextContent(string.Format("Border {0}", i + 1)));
                    }
                }
            }

            EditorGUILayout.Space();
            GUILayout.Label("Cascade splits", GUILayout.Height(EditorGUIUtility.singleLineHeight + 4));
            Rect showCascadesButtonRect = GUILayoutUtility.GetLastRect();
            showCascadesButtonRect.xMin += EditorGUIUtility.labelWidth - labelWidthAdjustment - 1;
            showCascadesButtonRect.yMin += 2;

            DrawShadowCascades(cascadeCount, unit == Unit.Metric, m_MaxShadowDistance.value.floatValue);

            HDRenderPipeline hdrp = UnityEngine.Rendering.RenderPipelineManager.currentPipeline as HDRenderPipeline;
            if (hdrp == null)
                return;

            bool currentCascadeValue = hdrp.showCascade;
            bool newCascadeValue = GUI.Toggle(showCascadesButtonRect, currentCascadeValue, EditorGUIUtility.TrTextContent("Show Cascades"), EditorStyles.miniButton);
            if (currentCascadeValue ^ newCascadeValue)
                hdrp.showCascade = newCascadeValue;
        }

        private void DrawShadowCascades(int cascadeCount, bool useMetric, float baseMetric)
        {
            var cascades = new ShadowCascadeGUI.Cascade[cascadeCount];

            float lastCascadePartitionSplit = 0;
            for (int i = 0; i < cascadeCount - 1; ++i)
            {
                cascades[i] = new ShadowCascadeGUI.Cascade()
                {
                    size = i == 0 ? m_CascadeShadowSplits[i].value.floatValue : m_CascadeShadowSplits[i].value.floatValue - lastCascadePartitionSplit, // Calculate the size of cascade
                    borderSize = m_CascadeShadowBorders[i].value.floatValue,
                    cascadeHandleState = m_CascadeShadowSplits[i].overrideState.boolValue ? ShadowCascadeGUI.HandleState.Enabled : ShadowCascadeGUI.HandleState.Disabled,
                    borderHandleState = m_CascadeShadowBorders[i].overrideState.boolValue ? ShadowCascadeGUI.HandleState.Enabled : ShadowCascadeGUI.HandleState.Disabled,
                };
                lastCascadePartitionSplit = m_CascadeShadowSplits[i].value.floatValue;
            }

            // Last cascade is special
            var lastCascade = cascadeCount - 1;
            cascades[lastCascade] = new ShadowCascadeGUI.Cascade()
            {
                size = lastCascade == 0 ? 1.0f : 1 - m_CascadeShadowSplits[lastCascade - 1].value.floatValue, // Calculate the size of cascade
                borderSize = m_CascadeShadowBorders[lastCascade].value.floatValue,
                cascadeHandleState = ShadowCascadeGUI.HandleState.Hidden,
                borderHandleState = m_CascadeShadowBorders[lastCascade].overrideState.boolValue ? ShadowCascadeGUI.HandleState.Enabled : ShadowCascadeGUI.HandleState.Disabled,
            };

            EditorGUI.BeginChangeCheck();
            ShadowCascadeGUI.DrawCascades(ref cascades, useMetric, baseMetric);
            if (EditorGUI.EndChangeCheck())
            {
                float lastCascadeSize = 0;
                for (int i = 0; i < cascadeCount - 1; ++i)
                {
                    m_CascadeShadowSplits[i].value.floatValue = lastCascadeSize + cascades[i].size;
                    lastCascadeSize = m_CascadeShadowSplits[i].value.floatValue;
                }

                for (int i = 0; i < cascadeCount; ++i)
                {
                    m_CascadeShadowBorders[i].value.floatValue = cascades[i].borderSize;
                }
            }
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

            var lineRect = EditorGUILayout.GetControlRect();
            EditorGUI.BeginProperty(lineRect, title, value);
            modifiableValue = EditorGUI.Slider(lineRect, title, modifiableValue, 0f, max);
            EditorGUI.EndProperty();
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
            var lineRect = EditorGUILayout.GetControlRect();
            EditorGUI.BeginProperty(lineRect, title, value);
            modifiableValue = EditorGUI.Slider(lineRect, title, modifiableValue, 0f, max);
            EditorGUI.EndProperty();
            if (EditorGUI.EndChangeCheck())
                value.floatValue = Mathf.Clamp01(modifiableValue / max);
            return true;
        }
    }
}
