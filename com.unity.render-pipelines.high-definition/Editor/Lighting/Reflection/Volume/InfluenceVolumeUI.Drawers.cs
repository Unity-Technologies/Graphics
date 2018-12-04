using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    partial class InfluenceVolumeUI
    {
        internal interface IInfluenceUISettingsProvider
        {
            bool drawOffset { get; }
            bool drawNormal { get; }
            bool drawFace { get; }
        }

        public static void Draw<TProvider>(InfluenceVolumeUI s, SerializedInfluenceVolume d, Editor o)
            where TProvider : struct, IInfluenceUISettingsProvider
        {
            var provider = new TProvider();

            Drawer_InfluenceAdvancedSwitch(s, d, o);
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(d.shape, shapeContent);
            switch ((InfluenceShape)d.shape.intValue)
            {
                case InfluenceShape.Box:
                    Drawer_SectionShapeBox(s, d, o, provider.drawOffset, provider.drawNormal, provider.drawFace);
                    break;
                case InfluenceShape.Sphere:
                    Drawer_SectionShapeSphere(s, d, o, provider.drawOffset, provider.drawNormal);
                    break;
            }
        }

        static void Drawer_InfluenceAdvancedSwitch(InfluenceVolumeUI s, SerializedInfluenceVolume d, Editor owner)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();

                if (d.shape.intValue == (int)InfluenceShape.Sphere)
                {
                    GUI.enabled = false;
                }

                bool advanced = d.editorAdvancedModeEnabled.boolValue;
                advanced = !GUILayout.Toggle(!advanced, normalModeContent, EditorStyles.miniButtonLeft, GUILayout.Width(60f), GUILayout.ExpandWidth(false));
                advanced = GUILayout.Toggle(advanced, advancedModeContent, EditorStyles.miniButtonRight, GUILayout.Width(60f), GUILayout.ExpandWidth(false));
                s.boxBaseHandle.monoHandle = false;
                s.boxInfluenceHandle.monoHandle = !advanced;
                s.boxInfluenceNormalHandle.monoHandle = !advanced;
                if (d.editorAdvancedModeEnabled.boolValue ^ advanced)
                {
                    d.editorAdvancedModeEnabled.boolValue = advanced;
                    if (advanced)
                    {
                        d.boxBlendDistancePositive.vector3Value = d.editorAdvancedModeBlendDistancePositive.vector3Value;
                        d.boxBlendDistanceNegative.vector3Value = d.editorAdvancedModeBlendDistanceNegative.vector3Value;
                        d.boxBlendNormalDistancePositive.vector3Value = d.editorAdvancedModeBlendNormalDistancePositive.vector3Value;
                        d.boxBlendNormalDistanceNegative.vector3Value = d.editorAdvancedModeBlendNormalDistanceNegative.vector3Value;
                        d.boxSideFadePositive.vector3Value = d.editorAdvancedModeFaceFadePositive.vector3Value;
                        d.boxSideFadeNegative.vector3Value = d.editorAdvancedModeFaceFadeNegative.vector3Value;
                    }
                    else
                    {
                        d.boxBlendDistanceNegative.vector3Value = d.boxBlendDistancePositive.vector3Value = Vector3.one * d.editorSimplifiedModeBlendDistance.floatValue;
                        d.boxBlendNormalDistanceNegative.vector3Value = d.boxBlendNormalDistancePositive.vector3Value = Vector3.one * d.editorSimplifiedModeBlendNormalDistance.floatValue;
                        d.boxSideFadeNegative.vector3Value = d.boxSideFadePositive.vector3Value = Vector3.one;
                    }
                    d.Apply();
                }

                if (d.shape.intValue == (int)InfluenceShape.Sphere)
                {
                    GUI.enabled = true;
                }
            }
        }

        static void Drawer_SectionShapeBox(InfluenceVolumeUI s, SerializedInfluenceVolume d, Editor o, bool drawOffset, bool drawNormal, bool drawFace)
        {
            bool advanced = d.editorAdvancedModeEnabled.boolValue;
            var maxFadeDistance = d.boxSize.vector3Value * 0.5f;
            var minFadeDistance = Vector3.zero;

            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(d.boxSize, boxSizeContent);
            if (EditorGUI.EndChangeCheck())
            {
                Vector3 blendPositive = d.boxBlendDistancePositive.vector3Value;
                Vector3 blendNegative = d.boxBlendDistanceNegative.vector3Value;
                Vector3 blendNormalPositive = d.boxBlendNormalDistancePositive.vector3Value;
                Vector3 blendNormalNegative = d.boxBlendNormalDistanceNegative.vector3Value;
                Vector3 size = d.boxSize.vector3Value;
                for(int i = 0; i<3; ++i)
                {
                    size[i] = Mathf.Max(0f, size[i]);
                }
                d.boxSize.vector3Value = size;
                Vector3 halfSize = size * .5f;
                for (int i = 0; i < 3; ++i)
                {
                    blendPositive[i] = Mathf.Clamp(blendPositive[i], 0f, halfSize[i]);
                    blendNegative[i] = Mathf.Clamp(blendNegative[i], 0f, halfSize[i]);
                    blendNormalPositive[i] = Mathf.Clamp(blendNormalPositive[i], 0f, halfSize[i]);
                    blendNormalNegative[i] = Mathf.Clamp(blendNormalNegative[i], 0f, halfSize[i]);
                }
                d.boxBlendDistancePositive.vector3Value = blendPositive;
                d.boxBlendDistanceNegative.vector3Value = blendNegative;
                d.boxBlendNormalDistancePositive.vector3Value = blendNormalPositive;
                d.boxBlendNormalDistanceNegative.vector3Value = blendNormalNegative;
                if (d.editorAdvancedModeEnabled.boolValue)
                {
                    d.editorAdvancedModeBlendDistancePositive.vector3Value = d.boxBlendDistancePositive.vector3Value;
                    d.editorAdvancedModeBlendDistanceNegative.vector3Value = d.boxBlendDistanceNegative.vector3Value;
                    d.editorAdvancedModeBlendNormalDistancePositive.vector3Value = d.boxBlendNormalDistancePositive.vector3Value;
                    d.editorAdvancedModeBlendNormalDistanceNegative.vector3Value = d.boxBlendNormalDistanceNegative.vector3Value;
                }
                else
                {
                    d.editorSimplifiedModeBlendDistance.floatValue = Mathf.Max(blendPositive.x, blendPositive.y, blendPositive.z, blendNegative.x, blendNegative.y, blendNegative.z);
                    d.boxBlendDistancePositive.vector3Value = d.boxBlendDistanceNegative.vector3Value = Vector3.one * d.editorSimplifiedModeBlendDistance.floatValue;
                    d.editorSimplifiedModeBlendNormalDistance.floatValue = Mathf.Max(blendNormalPositive.x, blendNormalPositive.y, blendNormalPositive.z, blendNormalNegative.x, blendNormalNegative.y, blendNormalNegative.z);
                    d.boxBlendNormalDistancePositive.vector3Value = d.boxBlendNormalDistanceNegative.vector3Value = Vector3.one * d.editorSimplifiedModeBlendNormalDistance.floatValue;
                }
            }
            HDProbeUI.Drawer_ToolBarButton(HDProbeUI.ToolBar.InfluenceShape, o, GUILayout.Width(28f), GUILayout.MinHeight(22f));
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(EditorGUIUtility.standardVerticalSpacing);
            
            EditorGUILayout.BeginHorizontal();
            Drawer_AdvancedBlendDistance(d, false, maxFadeDistance, blendDistanceContent);
            HDProbeUI.Drawer_ToolBarButton(HDProbeUI.ToolBar.Blend, o, GUILayout.ExpandHeight(true), GUILayout.Width(28f), GUILayout.MinHeight(22f), GUILayout.MaxHeight((advanced ? 2 : 1) * (EditorGUIUtility.singleLineHeight + 3)));
            EditorGUILayout.EndHorizontal();
            
            GUILayout.Space(EditorGUIUtility.standardVerticalSpacing * 2f);

            if (drawNormal)
            {
                EditorGUILayout.BeginHorizontal();
                Drawer_AdvancedBlendDistance(d, true, maxFadeDistance, blendNormalDistanceContent);
                HDProbeUI.Drawer_ToolBarButton(HDProbeUI.ToolBar.NormalBlend, o, GUILayout.ExpandHeight(true), GUILayout.Width(28f), GUILayout.MinHeight(22f), GUILayout.MaxHeight((advanced ? 2 : 1) * (EditorGUIUtility.singleLineHeight + 3)));
                EditorGUILayout.EndHorizontal();
                
                GUILayout.Space(EditorGUIUtility.standardVerticalSpacing * 2f);
            }

            if (advanced && drawFace)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUI.BeginChangeCheck();
                var positive = d.editorAdvancedModeFaceFadePositive.vector3Value;
                var negative = d.editorAdvancedModeFaceFadeNegative.vector3Value;
                CoreEditorUtils.DrawVector6(faceFadeContent, ref positive, ref negative, Vector3.zero, Vector3.one, k_HandlesColor);
                d.editorAdvancedModeFaceFadePositive.vector3Value = positive;
                d.editorAdvancedModeFaceFadeNegative.vector3Value = negative;
                if (EditorGUI.EndChangeCheck())
                {
                    d.boxSideFadePositive.vector3Value = d.editorAdvancedModeFaceFadePositive.vector3Value;
                    d.boxSideFadeNegative.vector3Value = d.editorAdvancedModeFaceFadeNegative.vector3Value;
                }
                GUILayout.Space(28f + 9f); //add right margin for alignment
                EditorGUILayout.EndHorizontal();
                
                GUILayout.Space(EditorGUIUtility.standardVerticalSpacing * 2f);
            }
        }

        static void Drawer_AdvancedBlendDistance(SerializedInfluenceVolume d, bool isNormal, Vector3 maxBlendDistance, GUIContent content)
        {
            SerializedProperty blendDistancePositive = isNormal ? d.boxBlendNormalDistancePositive : d.boxBlendDistancePositive;
            SerializedProperty blendDistanceNegative = isNormal ? d.boxBlendNormalDistanceNegative : d.boxBlendDistanceNegative;
            SerializedProperty editorAdvancedModeBlendDistancePositive = isNormal ? d.editorAdvancedModeBlendNormalDistancePositive : d.editorAdvancedModeBlendDistancePositive;
            SerializedProperty editorAdvancedModeBlendDistanceNegative = isNormal ? d.editorAdvancedModeBlendNormalDistanceNegative : d.editorAdvancedModeBlendDistanceNegative;
            SerializedProperty editorSimplifiedModeBlendDistance = isNormal ? d.editorSimplifiedModeBlendNormalDistance : d.editorSimplifiedModeBlendDistance;
            Vector3 bdp = blendDistancePositive.vector3Value;
            Vector3 bdn = blendDistanceNegative.vector3Value;

            EditorGUILayout.BeginVertical();

            if (d.editorAdvancedModeEnabled.boolValue)
            {
                EditorGUI.BeginChangeCheck();
                blendDistancePositive.vector3Value = editorAdvancedModeBlendDistancePositive.vector3Value;
                blendDistanceNegative.vector3Value = editorAdvancedModeBlendDistanceNegative.vector3Value;
                var positive = blendDistancePositive.vector3Value;
                var negative = blendDistanceNegative.vector3Value;
                CoreEditorUtils.DrawVector6(
                    content,
                    ref positive, ref negative, Vector3.zero, maxBlendDistance, k_HandlesColor);
                blendDistancePositive.vector3Value = positive;
                blendDistanceNegative.vector3Value = negative;
                if (EditorGUI.EndChangeCheck())
                {
                    editorAdvancedModeBlendDistancePositive.vector3Value = blendDistancePositive.vector3Value;
                    editorAdvancedModeBlendDistanceNegative.vector3Value = blendDistanceNegative.vector3Value;
                }
            }
            else
            {
                float distance = editorSimplifiedModeBlendDistance.floatValue;
                EditorGUI.BeginChangeCheck();
                distance = EditorGUILayout.FloatField(content, distance);
                if (EditorGUI.EndChangeCheck())
                {
                    distance = Mathf.Clamp(distance, 0f, Mathf.Max(maxBlendDistance.x, maxBlendDistance.y, maxBlendDistance.z));
                    Vector3 decal = Vector3.one * distance;
                    bdp.x = Mathf.Clamp(decal.x, 0f, maxBlendDistance.x);
                    bdp.y = Mathf.Clamp(decal.y, 0f, maxBlendDistance.y);
                    bdp.z = Mathf.Clamp(decal.z, 0f, maxBlendDistance.z);
                    bdn.x = Mathf.Clamp(decal.x, 0f, maxBlendDistance.x);
                    bdn.y = Mathf.Clamp(decal.y, 0f, maxBlendDistance.y);
                    bdn.z = Mathf.Clamp(decal.z, 0f, maxBlendDistance.z);
                    blendDistancePositive.vector3Value = bdp;
                    blendDistanceNegative.vector3Value = bdn;
                    editorSimplifiedModeBlendDistance.floatValue = distance;
                }
            }

            GUILayout.EndVertical();
        }

        static void Drawer_SectionShapeSphere(InfluenceVolumeUI s, SerializedInfluenceVolume d, Editor o, bool drawOffset, bool drawNormal)
        {

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(d.sphereRadius, radiusContent);
            HDProbeUI.Drawer_ToolBarButton(HDProbeUI.ToolBar.InfluenceShape, o, GUILayout.Width(28f), GUILayout.MinHeight(22f));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
            var maxBlendDistance = d.sphereRadius.floatValue;

            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(d.sphereBlendDistance, blendDistanceContent);
            if (EditorGUI.EndChangeCheck())
            {
                d.sphereBlendDistance.floatValue = Mathf.Clamp(d.sphereBlendDistance.floatValue, 0, maxBlendDistance);
            }
            HDProbeUI.Drawer_ToolBarButton(HDProbeUI.ToolBar.Blend, o, GUILayout.ExpandHeight(true), GUILayout.Width(28f), GUILayout.MinHeight(22f), GUILayout.MaxHeight(EditorGUIUtility.singleLineHeight + 3));
            EditorGUILayout.EndHorizontal();

            if (drawNormal)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(d.sphereBlendNormalDistance, blendNormalDistanceContent);
                if (EditorGUI.EndChangeCheck())
                {
                    d.sphereBlendNormalDistance.floatValue = Mathf.Clamp(d.sphereBlendNormalDistance.floatValue, 0, maxBlendDistance);
                }
                HDProbeUI.Drawer_ToolBarButton(HDProbeUI.ToolBar.NormalBlend, o, GUILayout.ExpandHeight(true), GUILayout.Width(28f), GUILayout.MinHeight(22f), GUILayout.MaxHeight(EditorGUIUtility.singleLineHeight + 3));
                EditorGUILayout.EndHorizontal();
            }
        }
    }
}
