using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    partial class InfluenceVolumeUI
    {
        internal interface IInfluenceUISettingsProvider
        {
            bool drawOffset { get; }
            bool drawNormal { get; }
            bool drawFace { get; }
        }

        public static void Draw<TProvider>(SerializedInfluenceVolume serialized, Editor owner)
            where TProvider : struct, IInfluenceUISettingsProvider
        {
            var provider = new TProvider();

            EditorGUILayout.PropertyField(serialized.shape, shapeContent);
            switch ((InfluenceShape)serialized.shape.intValue)
            {
                case InfluenceShape.Box:
                    Drawer_SectionShapeBox(serialized, owner, provider.drawOffset, provider.drawNormal, provider.drawFace);
                    break;
                case InfluenceShape.Sphere:
                    Drawer_SectionShapeSphere(serialized, owner, provider.drawOffset, provider.drawNormal);
                    break;
            }
        }

        public static void SetInfluenceAdvancedControlSwitch(SerializedInfluenceVolume serialized, Editor owner, bool advancedControl)
        {
            if (advancedControl == serialized.editorAdvancedModeEnabled.boolValue)
                return;

            serialized.editorAdvancedModeEnabled.boolValue = advancedControl;
            if (advancedControl)
            {
                serialized.boxBlendDistancePositive.vector3Value = serialized.editorAdvancedModeBlendDistancePositive.vector3Value;
                serialized.boxBlendDistanceNegative.vector3Value = serialized.editorAdvancedModeBlendDistanceNegative.vector3Value;
                serialized.boxBlendNormalDistancePositive.vector3Value = serialized.editorAdvancedModeBlendNormalDistancePositive.vector3Value;
                serialized.boxBlendNormalDistanceNegative.vector3Value = serialized.editorAdvancedModeBlendNormalDistanceNegative.vector3Value;
                serialized.boxSideFadePositive.vector3Value = serialized.editorAdvancedModeFaceFadePositive.vector3Value;
                serialized.boxSideFadeNegative.vector3Value = serialized.editorAdvancedModeFaceFadeNegative.vector3Value;
            }
            else
            {
                serialized.boxBlendDistanceNegative.vector3Value = serialized.boxBlendDistancePositive.vector3Value = Vector3.one * serialized.editorSimplifiedModeBlendDistance.floatValue;
                serialized.boxBlendNormalDistanceNegative.vector3Value = serialized.boxBlendNormalDistancePositive.vector3Value = Vector3.one * serialized.editorSimplifiedModeBlendNormalDistance.floatValue;
                serialized.boxSideFadeNegative.vector3Value = serialized.boxSideFadePositive.vector3Value = Vector3.one;
            }
            serialized.Apply();
        }

        static void Drawer_SectionShapeBox(SerializedInfluenceVolume serialized, Editor owner, bool drawOffset, bool drawNormal, bool drawFace)
        {
            bool advanced = serialized.editorAdvancedModeEnabled.boolValue;

            //small piece of init logic previously in the removed Drawer_InfluenceAdvancedSwitch
            s_BoxBaseHandle.monoHandle = false;
            s_BoxInfluenceHandle.monoHandle = !advanced;
            s_BoxInfluenceNormalHandle.monoHandle = !advanced;

            var maxFadeDistance = serialized.boxSize.vector3Value * 0.5f;
            var minFadeDistance = Vector3.zero;

            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(serialized.boxSize, boxSizeContent);
            if (EditorGUI.EndChangeCheck())
            {
                Vector3 localSize = serialized.boxSize.vector3Value;
                for (int i = 0; i < 3; ++i)
                {
                    localSize[i] = Mathf.Max(Mathf.Epsilon, localSize[i]);
                }
                serialized.boxSize.vector3Value = localSize;

                Vector3 blendPositive = serialized.boxBlendDistancePositive.vector3Value;
                Vector3 blendNegative = serialized.boxBlendDistanceNegative.vector3Value;
                Vector3 blendNormalPositive = serialized.boxBlendNormalDistancePositive.vector3Value;
                Vector3 blendNormalNegative = serialized.boxBlendNormalDistanceNegative.vector3Value;
                Vector3 size = serialized.boxSize.vector3Value;
                for(int i = 0; i<3; ++i)
                {
                    size[i] = Mathf.Max(0f, size[i]);
                }
                serialized.boxSize.vector3Value = size;
                Vector3 halfSize = size * .5f;
                for (int i = 0; i < 3; ++i)
                {
                    blendPositive[i] = Mathf.Clamp(blendPositive[i], 0f, halfSize[i]);
                    blendNegative[i] = Mathf.Clamp(blendNegative[i], 0f, halfSize[i]);
                    blendNormalPositive[i] = Mathf.Clamp(blendNormalPositive[i], 0f, halfSize[i]);
                    blendNormalNegative[i] = Mathf.Clamp(blendNormalNegative[i], 0f, halfSize[i]);
                }
                serialized.boxBlendDistancePositive.vector3Value = blendPositive;
                serialized.boxBlendDistanceNegative.vector3Value = blendNegative;
                serialized.boxBlendNormalDistancePositive.vector3Value = blendNormalPositive;
                serialized.boxBlendNormalDistanceNegative.vector3Value = blendNormalNegative;
                if (serialized.editorAdvancedModeEnabled.boolValue)
                {
                    serialized.editorAdvancedModeBlendDistancePositive.vector3Value = serialized.boxBlendDistancePositive.vector3Value;
                    serialized.editorAdvancedModeBlendDistanceNegative.vector3Value = serialized.boxBlendDistanceNegative.vector3Value;
                    serialized.editorAdvancedModeBlendNormalDistancePositive.vector3Value = serialized.boxBlendNormalDistancePositive.vector3Value;
                    serialized.editorAdvancedModeBlendNormalDistanceNegative.vector3Value = serialized.boxBlendNormalDistanceNegative.vector3Value;
                }
                else
                {
                    serialized.editorSimplifiedModeBlendDistance.floatValue = Mathf.Min(blendPositive.x, blendPositive.y, blendPositive.z, blendNegative.x, blendNegative.y, blendNegative.z);
                    serialized.boxBlendDistancePositive.vector3Value = serialized.boxBlendDistanceNegative.vector3Value = Vector3.one * serialized.editorSimplifiedModeBlendDistance.floatValue;
                    serialized.editorSimplifiedModeBlendNormalDistance.floatValue = Mathf.Min(blendNormalPositive.x, blendNormalPositive.y, blendNormalPositive.z, blendNormalNegative.x, blendNormalNegative.y, blendNormalNegative.z);
                    serialized.boxBlendNormalDistancePositive.vector3Value = serialized.boxBlendNormalDistanceNegative.vector3Value = Vector3.one * serialized.editorSimplifiedModeBlendNormalDistance.floatValue;
                }
            }
            HDProbeUI.Drawer_ToolBarButton(HDProbeUI.ToolBar.InfluenceShape, owner, GUILayout.Width(28f), GUILayout.MinHeight(22f));
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(EditorGUIUtility.standardVerticalSpacing);

            EditorGUILayout.PropertyField(serialized.editorAdvancedModeEnabled, manipulatonTypeContent);

            EditorGUILayout.BeginHorizontal();
            Drawer_AdvancedBlendDistance(serialized, false, maxFadeDistance, blendDistanceContent);
            HDProbeUI.Drawer_ToolBarButton(HDProbeUI.ToolBar.Blend, owner, GUILayout.ExpandHeight(true), GUILayout.Width(28f), GUILayout.MinHeight(22f), GUILayout.MaxHeight((advanced ? 2 : 1) * (EditorGUIUtility.singleLineHeight + 3)));
            EditorGUILayout.EndHorizontal();

            if (drawNormal)
            {
                EditorGUILayout.BeginHorizontal();
                Drawer_AdvancedBlendDistance(serialized, true, maxFadeDistance, blendNormalDistanceContent);
                HDProbeUI.Drawer_ToolBarButton(HDProbeUI.ToolBar.NormalBlend, owner, GUILayout.ExpandHeight(true), GUILayout.Width(28f), GUILayout.MinHeight(22f), GUILayout.MaxHeight((advanced ? 2 : 1) * (EditorGUIUtility.singleLineHeight + 3)));
                EditorGUILayout.EndHorizontal();
            }

            if (advanced && drawFace)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUI.BeginChangeCheck();
                CoreEditorUtils.DrawVector6(faceFadeContent, serialized.editorAdvancedModeFaceFadePositive, serialized.editorAdvancedModeFaceFadeNegative, Vector3.zero, Vector3.one, k_HandlesColor);
                if (EditorGUI.EndChangeCheck())
                {
                    serialized.boxSideFadePositive.vector3Value = serialized.editorAdvancedModeFaceFadePositive.vector3Value;
                    serialized.boxSideFadeNegative.vector3Value = serialized.editorAdvancedModeFaceFadeNegative.vector3Value;
                }
                GUILayout.Space(30f); //add right margin for alignment
                EditorGUILayout.EndHorizontal();
            }
        }

        static void Drawer_AdvancedBlendDistance(SerializedInfluenceVolume serialized, bool isNormal, Vector3 maxBlendDistance, GUIContent content)
        {
            SerializedProperty blendDistancePositive = isNormal ? serialized.boxBlendNormalDistancePositive : serialized.boxBlendDistancePositive;
            SerializedProperty blendDistanceNegative = isNormal ? serialized.boxBlendNormalDistanceNegative : serialized.boxBlendDistanceNegative;
            SerializedProperty editorAdvancedModeBlendDistancePositive = isNormal ? serialized.editorAdvancedModeBlendNormalDistancePositive : serialized.editorAdvancedModeBlendDistancePositive;
            SerializedProperty editorAdvancedModeBlendDistanceNegative = isNormal ? serialized.editorAdvancedModeBlendNormalDistanceNegative : serialized.editorAdvancedModeBlendDistanceNegative;
            SerializedProperty editorSimplifiedModeBlendDistance = isNormal ? serialized.editorSimplifiedModeBlendNormalDistance : serialized.editorSimplifiedModeBlendDistance;
            Vector3 bdp = blendDistancePositive.vector3Value;
            Vector3 bdn = blendDistanceNegative.vector3Value;

            //resync to be sure prefab revert will keep syncs
            if (serialized.editorAdvancedModeEnabled.boolValue)
            {
                if (!(Mathf.Approximately(Vector3.SqrMagnitude(blendDistancePositive.vector3Value - editorAdvancedModeBlendDistancePositive.vector3Value), 0f)
                    && Mathf.Approximately(Vector3.SqrMagnitude(blendDistanceNegative.vector3Value - editorAdvancedModeBlendDistanceNegative.vector3Value), 0f)))
                {
                    blendDistancePositive.vector3Value = editorAdvancedModeBlendDistancePositive.vector3Value;
                    blendDistanceNegative.vector3Value = editorAdvancedModeBlendDistanceNegative.vector3Value;
                    serialized.Apply();
                    SceneView.RepaintAll(); //update gizmo
                }
            }
            else
            {
                var scalar = Mathf.Min(editorSimplifiedModeBlendDistance.floatValue, Mathf.Min(maxBlendDistance.x, maxBlendDistance.y, maxBlendDistance.z));
                blendDistancePositive.vector3Value = blendDistanceNegative.vector3Value = new Vector3(scalar, scalar, scalar);
                serialized.Apply();
                SceneView.RepaintAll(); //update gizmo
            }

            if (serialized.editorAdvancedModeEnabled.boolValue)
            {
                EditorGUI.BeginChangeCheck();
                CoreEditorUtils.DrawVector6(content, editorAdvancedModeBlendDistancePositive, editorAdvancedModeBlendDistanceNegative, Vector3.zero, maxBlendDistance, k_HandlesColor);
                if (EditorGUI.EndChangeCheck())
                {
                    blendDistancePositive.vector3Value = editorAdvancedModeBlendDistancePositive.vector3Value;
                    blendDistanceNegative.vector3Value = editorAdvancedModeBlendDistanceNegative.vector3Value;
                }
            }
            else
            {
                Rect lineRect = EditorGUILayout.GetControlRect();
                EditorGUI.BeginProperty(lineRect, content, editorSimplifiedModeBlendDistance);
                float distance = editorSimplifiedModeBlendDistance.floatValue;
                EditorGUI.BeginChangeCheck();
                distance = EditorGUI.FloatField(lineRect, content, distance);
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
                EditorGUI.EndProperty();
            }
        }

        static void Drawer_SectionShapeSphere(SerializedInfluenceVolume serialized, Editor owner, bool drawOffset, bool drawNormal)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(serialized.sphereRadius, radiusContent);
            HDProbeUI.Drawer_ToolBarButton(HDProbeUI.ToolBar.InfluenceShape, owner, GUILayout.Width(28f), GUILayout.MinHeight(22f));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
            var maxBlendDistance = serialized.sphereRadius.floatValue;

            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(serialized.sphereBlendDistance, blendDistanceContent);
            if (EditorGUI.EndChangeCheck())
            {
                serialized.sphereBlendDistance.floatValue = Mathf.Clamp(serialized.sphereBlendDistance.floatValue, 0, maxBlendDistance);
            }
            HDProbeUI.Drawer_ToolBarButton(HDProbeUI.ToolBar.Blend, owner, GUILayout.ExpandHeight(true), GUILayout.Width(28f), GUILayout.MinHeight(22f), GUILayout.MaxHeight(EditorGUIUtility.singleLineHeight + 3));
            EditorGUILayout.EndHorizontal();

            if (drawNormal)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(serialized.sphereBlendNormalDistance, blendNormalDistanceContent);
                if (EditorGUI.EndChangeCheck())
                {
                    serialized.sphereBlendNormalDistance.floatValue = Mathf.Clamp(serialized.sphereBlendNormalDistance.floatValue, 0, maxBlendDistance);
                }
                HDProbeUI.Drawer_ToolBarButton(HDProbeUI.ToolBar.NormalBlend, owner, GUILayout.ExpandHeight(true), GUILayout.Width(28f), GUILayout.MinHeight(22f), GUILayout.MaxHeight(EditorGUIUtility.singleLineHeight + 3));
                EditorGUILayout.EndHorizontal();
            }
        }
    }
}
