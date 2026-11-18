using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal
{
    [CustomEditor(typeof(ShadowsVolumeOverride))]
    sealed class ShadowsVolumeOverrideEditor : VolumeComponentEditor
    {
        static class Styles
        {
            public const string directionalLightHeader = "Directional Light";
            public static readonly GUIContent workingUnit = L10n.TextContent("Working Unit", "The unit in which Unity measures the shadow cascade distances. The exception is Max Distance, which will still be in meters.", null, null);
        }

        // VolumeComponentEditor draws the override checkbox in a column this wide (checkbox 14 + offset 9 + spacing 3)
        // and OverridablePropertyScope shrinks the label by one more spacing, so rows without a checkbox match these to align.
        const float k_OverrideCheckboxColumnWidth = 14f + 9f + 3f;
        const float k_OverridableLabelWidthReduction = k_OverrideCheckboxColumnWidth + 3f;

        SerializedDataParameter m_MaxShadowDistance;
        SerializedDataParameter m_CascadeCount;
        readonly SerializedDataParameter[] m_CascadeSplits = new SerializedDataParameter[3];
        SerializedDataParameter m_LastCascadeBorder;

        GUIContent m_MaxDistanceLabel;
        readonly GUIContent[] m_SplitLabels = new GUIContent[3];
        GUIContent m_LastBorderLabel;

        readonly bool[] m_SplitHandlesEnabled = new bool[3];

        EditorPrefBoolFlags<EditorUtils.Unit> m_State;

        static GUIContent Label(SerializedDataParameter property, string text)
            => L10n.TextContent(text, property.GetAttribute<TooltipAttribute>()?.tooltip, null, null);

        public override void OnEnable()
        {
            var o = new PropertyFetcher<ShadowsVolumeOverride>(serializedObject);

            m_MaxShadowDistance = Unpack(o.Find(x => x.maxShadowDistance));
            m_CascadeCount = Unpack(o.Find(x => x.cascadeCount));
            m_CascadeSplits[0] = Unpack(o.Find(x => x.cascadeSplit0));
            m_CascadeSplits[1] = Unpack(o.Find(x => x.cascadeSplit1));
            m_CascadeSplits[2] = Unpack(o.Find(x => x.cascadeSplit2));
            m_LastCascadeBorder = Unpack(o.Find(x => x.lastCascadeBorder));

            m_MaxDistanceLabel = Label(m_MaxShadowDistance, "Max Distance");
            for (int i = 0; i < m_CascadeSplits.Length; ++i)
                m_SplitLabels[i] = Label(m_CascadeSplits[i], $"Split {i + 1}");
            m_LastBorderLabel = Label(m_LastCascadeBorder, "Last Border");

            m_State = new EditorPrefBoolFlags<EditorUtils.Unit>("URP:ShadowsVolumeOverrideEditor:UI_State");
        }

        public override void OnInspectorGUI()
        {
            PropertyField(m_MaxShadowDistance, m_MaxDistanceLabel);

            DrawHeader(Styles.directionalLightHeader);
            using (new IndentLevelScope())
                DrawDirectionalLightCascades();
        }

        void DrawDirectionalLightCascades()
        {
            bool useMetric = DrawWorkingUnit() == EditorUtils.Unit.Metric;

            EditorGUI.BeginChangeCheck();
            PropertyField(m_CascadeCount);
            bool cascadeCountChanged = EditorGUI.EndChangeCheck();

            int cascadeCount = Mathf.Clamp(m_CascadeCount.value.intValue, UniversalRenderPipelineAsset.k_ShadowCascadeMinCount, UniversalRenderPipelineAsset.k_ShadowCascadeMaxCount);
            int splitCount = cascadeCount - 1;
            float maxDistance = m_MaxShadowDistance.value.floatValue;

            Vector4 splits = ShadowCascadeSplitGUI.SortSplits(new Vector4(
                m_CascadeSplits[0].value.floatValue,
                m_CascadeSplits[1].value.floatValue,
                m_CascadeSplits[2].value.floatValue,
                1f), splitCount);

            if (cascadeCountChanged)
                WriteSplits(splits, splitCount);

            for (int i = 0; i < splitCount; ++i)
                DrawSplit(i, ref splits, splitCount, useMetric, maxDistance);

            DrawLastBorder(splits, splitCount, useMetric, maxDistance);

            for (int i = 0; i < splitCount; ++i)
                m_SplitHandlesEnabled[i] = m_CascadeSplits[i].overrideState.boolValue;

            float border = m_LastCascadeBorder.value.floatValue;
            if (ShadowCascadeSplitGUI.DrawCascades(ref splits, ref border, cascadeCount, useMetric, maxDistance, m_SplitHandlesEnabled, m_LastCascadeBorder.overrideState.boolValue))
            {
                WriteSplits(splits, splitCount);
                if (border != m_LastCascadeBorder.value.floatValue)
                    m_LastCascadeBorder.value.floatValue = border;
            }
        }

        EditorUtils.Unit DrawWorkingUnit()
        {
            Rect rect = EditorGUILayout.GetControlRect();
            if (enableOverrides)
                rect.xMin += k_OverrideCheckboxColumnWidth;

            EditorGUIUtility.labelWidth -= k_OverridableLabelWidthReduction;
            EditorGUI.BeginChangeCheck();
            var unit = (EditorUtils.Unit)EditorGUI.EnumPopup(rect, Styles.workingUnit, m_State.value);
            if (EditorGUI.EndChangeCheck())
                m_State.value = unit;
            EditorGUIUtility.labelWidth += k_OverridableLabelWidthReduction;

            return unit;
        }

        void DrawSplit(int index, ref Vector4 splits, int splitCount, bool useMetric, float maxDistance)
        {
            SerializedDataParameter split = m_CascadeSplits[index];
            using (var scope = new OverridablePropertyScope(split, m_SplitLabels[index], this))
            {
                if (!scope.displayed)
                    return;

                Rect rect = EditorGUILayout.GetControlRect();
                GUIContent label = EditorGUI.BeginProperty(rect, scope.label, split.value);
                if (ShadowCascadeSplitGUI.SplitSlider(rect, label, ref splits, index, splitCount, useMetric, maxDistance))
                    split.value.floatValue = splits[index];
                EditorGUI.EndProperty();
            }
        }

        void DrawLastBorder(Vector4 splits, int splitCount, bool useMetric, float maxDistance)
        {
            using (var scope = new OverridablePropertyScope(m_LastCascadeBorder, m_LastBorderLabel, this))
            {
                if (!scope.displayed)
                    return;

                Rect rect = EditorGUILayout.GetControlRect();
                GUIContent label = EditorGUI.BeginProperty(rect, scope.label, m_LastCascadeBorder.value);
                float border = m_LastCascadeBorder.value.floatValue;
                if (ShadowCascadeSplitGUI.LastBorderSlider(rect, label, ref border, splits, splitCount, useMetric, maxDistance))
                    m_LastCascadeBorder.value.floatValue = border;
                EditorGUI.EndProperty();
            }
        }

        void WriteSplits(Vector4 splits, int splitCount)
        {
            for (int i = 0; i < splitCount; ++i)
            {
                if (m_CascadeSplits[i].value.floatValue != splits[i])
                    m_CascadeSplits[i].value.floatValue = splits[i];
            }
        }
    }
}
