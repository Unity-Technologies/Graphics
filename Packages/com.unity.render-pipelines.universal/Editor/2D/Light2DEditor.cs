using System.Collections.Generic;
using System.Linq;
using UnityEditor.EditorTools;
using UnityEditor.Rendering.Universal.Path2D;
using UnityEngine;
using UnityEngine.Rendering.Universal;


namespace UnityEditor.Rendering.Universal
{
    [CustomEditor(typeof(Light2D))]
    [CanEditMultipleObjects]
    internal class Light2DEditor : PathComponentEditor<ScriptablePath>
    {
        [EditorTool("Edit Freeform Shape", typeof(Light2D))]
        class FreeformShapeTool : PathEditorTool<ScriptablePath>
        {
            const string k_ShapePath = "m_ShapePath";

            public override bool IsAvailable()
            {
                var light = target as Light2D;

                if (light == null)
                    return false;
                else
                    return base.IsAvailable() && light.lightType == Light2D.LightType.Freeform;
            }

            protected override IShape GetShape(Object target)
            {
                return (target as Light2D).shapePath.ToPolygon(false);
            }

            protected override void SetShape(ScriptablePath shapeEditor, SerializedObject serializedObject)
            {
                serializedObject.Update();

                var pointsProperty = serializedObject.FindProperty(k_ShapePath);
                pointsProperty.arraySize = shapeEditor.pointCount;

                for (var i = 0; i < shapeEditor.pointCount; ++i)
                    pointsProperty.GetArrayElementAtIndex(i).vector3Value = shapeEditor.GetPoint(i).position;

                ((Light2D)(serializedObject.targetObject)).UpdateMesh();

                // This is untracked right now...
                serializedObject.ApplyModifiedProperties();
            }
        }

        private static class Styles
        {
            public static readonly GUIContent InnerOuterSpotAngle = EditorGUIUtility.TrTextContent("Inner / Outer Spot Angle", "Adjusts the inner / outer angles of this light to change the angle ranges of this Spot Light’s beam.");

            public static Texture lightCapTopRight = Resources.Load<Texture>("LightCapTopRight");
            public static Texture lightCapTopLeft = Resources.Load<Texture>("LightCapTopLeft");
            public static Texture lightCapBottomLeft = Resources.Load<Texture>("LightCapBottomLeft");
            public static Texture lightCapBottomRight = Resources.Load<Texture>("LightCapBottomRight");
            public static Texture lightCapUp = Resources.Load<Texture>("LightCapUp");
            public static Texture lightCapDown = Resources.Load<Texture>("LightCapDown");


            public static GUIContent lightTypeFreeform = new GUIContent("Freeform", Resources.Load("InspectorIcons/FreeformLight") as Texture);
            public static GUIContent lightTypeSprite = new GUIContent("Sprite", Resources.Load("InspectorIcons/SpriteLight") as Texture);
            public static GUIContent lightTypePoint = new GUIContent("Spot", Resources.Load("InspectorIcons/PointLight") as Texture);
            public static GUIContent lightTypeGlobal = new GUIContent("Global", Resources.Load("InspectorIcons/GlobalLight") as Texture);
            public static GUIContent[] lightTypeOptions = new GUIContent[] { lightTypeFreeform, lightTypeSprite, lightTypePoint, lightTypeGlobal };


            public static GUIContent blendingSettingsFoldout = EditorGUIUtility.TrTextContent("Blending", "Options used for blending");
            public static GUIContent shadowsSettingsFoldout = EditorGUIUtility.TrTextContent("Shadows", "Options used for shadows");
            public static GUIContent volumetricSettingsFoldout = EditorGUIUtility.TrTextContent("Volumetric", "Options used for volumetric lighting");
            public static GUIContent normalMapsSettingsFoldout = EditorGUIUtility.TrTextContent("Normal Maps", "Options used for normal maps");

            public static GUIContent generalLightType = EditorGUIUtility.TrTextContent("Light Type", "Select the light type. \n\nGlobal Light: For ambient light. \nSpot Light: For a spot light / point light. \nFreeform Light: For a custom shape light. \nSprite Light: For a custom light cookie using Sprites.");

            public static GUIContent generalFalloffSize = EditorGUIUtility.TrTextContent("Falloff", "Adjusts the falloff area of this light. The higher the falloff value, the larger area the falloff spans.");
            public static GUIContent generalFalloffIntensity = EditorGUIUtility.TrTextContent("Falloff Strength", "Adjusts the falloff curve to control the softness of this light’s edges. The higher the falloff strength, the softer the edges of this light.");
            public static GUIContent generalLightColor = EditorGUIUtility.TrTextContent("Color", "Adjusts this light’s color.");
            public static GUIContent generalLightIntensity = EditorGUIUtility.TrTextContent("Intensity", "Adjusts this light’s color intensity by using multiply to brighten the Sprite beyond its original color.");
            public static GUIContent generalVolumeIntensity = EditorGUIUtility.TrTextContent("Intensity", "Adjusts the intensity of this additional light volume that's additively blended on top of this light. To enable the Volumetric Shadow Strength, increase this Intensity to be greater than 0.");
            public static GUIContent generalBlendStyle = EditorGUIUtility.TrTextContent("Blend Style", "Adjusts how this light blends with the Sprites on the Target Sorting Layers. Different Blend Styles can be customized in the 2D Renderer Data Asset.");
            public static GUIContent generalLightOverlapOperation = EditorGUIUtility.TrTextContent("Overlap Operation", "Determines how this light blends with the other lights either through additive or alpha blending.");
            public static GUIContent generalLightOrder = EditorGUIUtility.TrTextContent("Light Order", "Determines the relative order in which lights of the same Blend Style get rendered. Lights with lower values are rendered first.");
            public static GUIContent generalShadowIntensity = EditorGUIUtility.TrTextContent("Strength", "Adjusts the amount of light occlusion from the Shadow Caster 2D component(s) when blocking this light.The higher the value, the more opaque the shadow becomes.");
            public static GUIContent generalShadowVolumeIntensity = EditorGUIUtility.TrTextContent("Shadow Strength", "Adjusts the amount of volume light occlusion from the Shadow Caster 2D component(s) when blocking this light.");
            public static GUIContent generalSortingLayerPrefixLabel = EditorGUIUtility.TrTextContent("Target Sorting Layers", "Determines which layers this light affects. To optimize performance, minimize the number of layers this light affects.");
            public static GUIContent generalLightNoLightEnabled = EditorGUIUtility.TrTextContentWithIcon("No valid blend styles are enabled.", MessageType.Error);
            public static GUIContent generalNormalMapZDistance = EditorGUIUtility.TrTextContent("Distance", "Adjusts the z-axis distance of this light and the lit Sprite(s). Do note that this distance does not Transform the position of this light in the Scene.");
            public static GUIContent generalNormalMapLightQuality = EditorGUIUtility.TrTextContent("Quality", "Determines the accuracy of the lighting calculations when normal map is used. To optimize for performance, select Fast.");

            public static GUIContent pointLightRadius = EditorGUIUtility.TrTextContent("Radius", "Adjusts the inner / outer radius of this light to change the size of this light.");
            public static GUIContent pointLightInner = EditorGUIUtility.TrTextContent("Inner", "Specify the inner radius of the light");
            public static GUIContent pointLightOuter = EditorGUIUtility.TrTextContent("Outer", "Specify the outer radius of the light");
            public static GUIContent pointLightSprite = EditorGUIUtility.TrTextContent("Sprite", "Specify the sprite (deprecated)");

            public static GUIContent shapeLightSprite = EditorGUIUtility.TrTextContent("Sprite", "Assign a Sprite which acts as a mask to create a light cookie.");

            public static GUIContent deprecatedParametricLightWarningSingle = EditorGUIUtility.TrTextContentWithIcon("Parametic Lights have been deprecated. To continue, upgrade your Parametric Light to a Freeform Light to enjoy similar light functionality.", MessageType.Warning);
            public static GUIContent deprecatedParametricLightWarningMulti = EditorGUIUtility.TrTextContentWithIcon("Parametic Lights have been deprecated. To continue, upgrade your Parametric Lights to Freeform Lights to enjoy similar light functionality.", MessageType.Warning);
            public static GUIContent deprecatedParametricLightInstructions = EditorGUIUtility.TrTextContent("Alternatively, you may choose to upgrade from the menu. Window > Rendering > Render Pipeline Converter > URP 2D Converters");
            public static GUIContent deprecatedParametricLightButtonSingle = EditorGUIUtility.TrTextContent("Upgrade Parametric Light");
            public static GUIContent deprecatedParametricLightButtonMulti = EditorGUIUtility.TrTextContent("Upgrade Parametric Lights");

            public static GUIContent renderPipelineUnassignedWarning = EditorGUIUtility.TrTextContentWithIcon("Universal scriptable renderpipeline asset must be assigned in Graphics Settings or Quality Settings.", MessageType.Warning);
            public static GUIContent asset2DUnassignedWarning = EditorGUIUtility.TrTextContentWithIcon("2D renderer data must be assigned to your universal render pipeline asset or camera.", MessageType.Warning);

            public static string deprecatedParametricLightDialogTextSingle = "The upgrade will convert the selected parametric light into a freeform light. You can't undo this operation.";
            public static string deprecatedParametricLightDialogTextMulti = "The upgrade will convert the selected parametric lights into freeform lights. You can't undo this operation.";
            public static string deprecatedParametricLightDialogTitle = "Parametric Light Upgrader";
            public static string deprecatedParametricLightDialogProceed = "Proceed";
            public static string deprecatedParametricLightDialogCancel = "Cancel";
        }

        const float k_GlobalLightGizmoSize = 1.2f;
        const float k_AngleCapSize = 0.16f * k_GlobalLightGizmoSize;
        const float k_AngleCapOffset = 0.08f * k_GlobalLightGizmoSize;
        const float k_AngleCapOffsetSecondary = -0.05f;
        const float k_RangeCapSize = 0.025f * k_GlobalLightGizmoSize;
        const float k_InnerRangeCapSize = 0.08f * k_GlobalLightGizmoSize;

        SerializedProperty m_LightType;
        SerializedProperty m_LightColor;
        SerializedProperty m_LightIntensity;
        SerializedProperty m_UseNormalMap;
        SerializedProperty m_ShadowIntensity;
        SerializedProperty m_ShadowIntensityEnabled;
        SerializedProperty m_ShadowVolumeIntensity;
        SerializedProperty m_ShadowVolumeIntensityEnabled;
        SerializedProperty m_ApplyToSortingLayers;
        SerializedProperty m_VolumetricIntensity;
        SerializedProperty m_VolumetricIntensityEnabled;
        SerializedProperty m_BlendStyleIndex;
        SerializedProperty m_FalloffIntensity;
        SerializedProperty m_NormalMapZDistance;
        SerializedProperty m_NormalMapQuality;
        SerializedProperty m_LightOrder;
        SerializedProperty m_OverlapOperation;

        // Point Light Properties
        SerializedProperty m_PointInnerAngle;
        SerializedProperty m_PointOuterAngle;
        SerializedProperty m_PointInnerRadius;
        SerializedProperty m_PointOuterRadius;
        SerializedProperty m_DeprecatedPointLightSprite;

        // Shape Light Properties
        SerializedProperty m_ShapeLightParametricRadius;
        SerializedProperty m_ShapeLightFalloffSize;
        SerializedProperty m_ShapeLightParametricSides;
        SerializedProperty m_ShapeLightSprite;

        SavedBool m_BlendingSettingsFoldout;
        SavedBool m_ShadowsSettingsFoldout;
        SavedBool m_VolumetricSettingsFoldout;
        SavedBool m_NormalMapsSettingsFoldout;


        int[] m_BlendStyleIndices;
        GUIContent[] m_BlendStyleNames;
        bool m_AnyBlendStyleEnabled = false;

        SortingLayerDropDown m_SortingLayerDropDown;

        Light2D lightObject => target as Light2D;

        Analytics.Renderer2DAnalytics m_Analytics;
        HashSet<Light2D> m_ModifiedLights;

        private void AnalyticsTrackChanges(SerializedObject serializedObject)
        {
            if (serializedObject.hasModifiedProperties)
            {
                foreach (Object targetObj in serializedObject.targetObjects)
                {
                    Light2D light2d = (Light2D)targetObj;
                    if (!m_ModifiedLights.Contains(light2d))
                        m_ModifiedLights.Add(light2d);
                }
            }
        }

        void OnEnable()
        {
            m_Analytics = Analytics.Renderer2DAnalytics.instance;
            m_ModifiedLights = new HashSet<Light2D>();
            m_SortingLayerDropDown = new SortingLayerDropDown();

            m_BlendingSettingsFoldout = new SavedBool($"{target.GetType()}.2DURPBlendingSettingsFoldout", false);
            m_ShadowsSettingsFoldout = new SavedBool($"{target.GetType()}.2DURPShadowsSettingsFoldout", false);
            m_VolumetricSettingsFoldout = new SavedBool($"{target.GetType()}.2DURPVolumetricSettingsFoldout", false);
            m_NormalMapsSettingsFoldout = new SavedBool($"{target.GetType()}.2DURPNormalMapsSettingsFoldout", false);

            m_LightType = serializedObject.FindProperty("m_LightType");
            m_LightColor = serializedObject.FindProperty("m_Color");
            m_LightIntensity = serializedObject.FindProperty("m_Intensity");
            m_UseNormalMap = serializedObject.FindProperty("m_UseNormalMap");
            m_ShadowIntensity = serializedObject.FindProperty("m_ShadowIntensity");
            m_ShadowIntensityEnabled = serializedObject.FindProperty("m_ShadowIntensityEnabled");
            m_ShadowVolumeIntensity = serializedObject.FindProperty("m_ShadowVolumeIntensity");
            m_ShadowVolumeIntensityEnabled = serializedObject.FindProperty("m_ShadowVolumeIntensityEnabled");
            m_ApplyToSortingLayers = serializedObject.FindProperty("m_ApplyToSortingLayers");
            m_VolumetricIntensity = serializedObject.FindProperty("m_LightVolumeIntensity");
            m_VolumetricIntensityEnabled = serializedObject.FindProperty("m_LightVolumeIntensityEnabled");
            m_BlendStyleIndex = serializedObject.FindProperty("m_BlendStyleIndex");
            m_FalloffIntensity = serializedObject.FindProperty("m_FalloffIntensity");
            m_NormalMapZDistance = serializedObject.FindProperty("m_NormalMapDistance");
            m_NormalMapQuality = serializedObject.FindProperty("m_NormalMapQuality");
            m_LightOrder = serializedObject.FindProperty("m_LightOrder");
            m_OverlapOperation = serializedObject.FindProperty("m_OverlapOperation");

            // Point Light
            m_PointInnerAngle = serializedObject.FindProperty("m_PointLightInnerAngle");
            m_PointOuterAngle = serializedObject.FindProperty("m_PointLightOuterAngle");
            m_PointInnerRadius = serializedObject.FindProperty("m_PointLightInnerRadius");
            m_PointOuterRadius = serializedObject.FindProperty("m_PointLightOuterRadius");
            m_DeprecatedPointLightSprite = serializedObject.FindProperty("m_DeprecatedPointLightCookieSprite");

            // Shape Light
            m_ShapeLightParametricRadius = serializedObject.FindProperty("m_ShapeLightParametricRadius");
            m_ShapeLightFalloffSize = serializedObject.FindProperty("m_ShapeLightFalloffSize");
            m_ShapeLightParametricSides = serializedObject.FindProperty("m_ShapeLightParametricSides");
            m_ShapeLightSprite = serializedObject.FindProperty("m_LightCookieSprite");

            m_AnyBlendStyleEnabled = false;
            var blendStyleIndices = new List<int>();
            var blendStyleNames = new List<string>();

            var rendererData = Light2DEditorUtility.GetRenderer2DData();
            if (rendererData != null)
            {
                for (int i = 0; i < rendererData.lightBlendStyles.Length; ++i)
                {
                    blendStyleIndices.Add(i);

                    ref var blendStyle = ref rendererData.lightBlendStyles[i];

                    if (blendStyle.maskTextureChannel == Light2DBlendStyle.TextureChannel.None)
                        blendStyleNames.Add(blendStyle.name);
                    else
                    {
                        var name = string.Format("{0} ({1})", blendStyle.name, blendStyle.maskTextureChannel);
                        blendStyleNames.Add(name);
                    }

                    m_AnyBlendStyleEnabled = true;
                }
            }
            else
            {
                for (int i = 0; i < 4; ++i)
                {
                    blendStyleIndices.Add(i);
                    blendStyleNames.Add("Operation" + i);
                }
            }

            m_BlendStyleIndices = blendStyleIndices.ToArray();
            m_BlendStyleNames = blendStyleNames.Select(x => new GUIContent(x)).ToArray();


            m_SortingLayerDropDown.OnEnable(serializedObject, "m_ApplyToSortingLayers");
        }

        internal void SendModifiedAnalytics(Analytics.Renderer2DAnalytics analytics, Light2D light)
        {
            Analytics.Light2DData lightData = new Analytics.Light2DData();
            lightData.was_create_event = false;
            lightData.instance_id = light.GetInstanceID();
            lightData.light_type = light.lightType;
            Analytics.Renderer2DAnalytics.instance.SendData(Analytics.AnalyticsDataTypes.k_LightDataString, lightData);
        }

        void OnDestroy()
        {
            if (m_ModifiedLights != null && m_ModifiedLights.Count > 0)
            {
                foreach (Light2D light in m_ModifiedLights)
                {
                    SendModifiedAnalytics(m_Analytics, light);
                }
            }
        }

        void DrawBlendingGroup()
        {
            CoreEditorUtils.DrawSplitter(false);
            m_BlendingSettingsFoldout.value = CoreEditorUtils.DrawHeaderFoldout(Styles.blendingSettingsFoldout, m_BlendingSettingsFoldout.value);
            if (m_BlendingSettingsFoldout.value)
            {
                if (!m_AnyBlendStyleEnabled)
                    EditorGUILayout.HelpBox(Styles.generalLightNoLightEnabled);
                else
                    EditorGUILayout.IntPopup(m_BlendStyleIndex, m_BlendStyleNames, m_BlendStyleIndices, Styles.generalBlendStyle);

                EditorGUILayout.PropertyField(m_LightOrder, Styles.generalLightOrder);
                EditorGUILayout.PropertyField(m_OverlapOperation, Styles.generalLightOverlapOperation);
            }
        }

        void DrawShadowsGroup()
        {
            CoreEditorUtils.DrawSplitter(false);
            m_ShadowsSettingsFoldout.value = CoreEditorUtils.DrawHeaderFoldout(Styles.shadowsSettingsFoldout, m_ShadowsSettingsFoldout.value);
            if (m_ShadowsSettingsFoldout.value)
            {
                DrawToggleProperty(Styles.generalShadowIntensity, m_ShadowIntensityEnabled, m_ShadowIntensity);
            }
        }

        void DrawVolumetricGroup()
        {
            CoreEditorUtils.DrawSplitter(false);
            m_VolumetricSettingsFoldout.value = CoreEditorUtils.DrawHeaderFoldout(Styles.volumetricSettingsFoldout, m_VolumetricSettingsFoldout.value);
            if (m_VolumetricSettingsFoldout.value)
            {
                DrawToggleProperty(Styles.generalVolumeIntensity, m_VolumetricIntensityEnabled, m_VolumetricIntensity);
                if (m_VolumetricIntensity.floatValue < 0)
                    m_VolumetricIntensity.floatValue = 0;

                EditorGUI.BeginDisabledGroup(!m_VolumetricIntensityEnabled.boolValue);
                DrawToggleProperty(Styles.generalShadowVolumeIntensity, m_ShadowVolumeIntensityEnabled, m_ShadowVolumeIntensity);
                EditorGUI.EndDisabledGroup();
            }
        }

        void DrawNormalMapGroup()
        {
            CoreEditorUtils.DrawSplitter(false);
            m_NormalMapsSettingsFoldout.value = CoreEditorUtils.DrawHeaderFoldout(Styles.normalMapsSettingsFoldout, m_NormalMapsSettingsFoldout.value);
            if (m_NormalMapsSettingsFoldout.value)
            {
                EditorGUILayout.PropertyField(m_NormalMapQuality, Styles.generalNormalMapLightQuality);

                EditorGUI.BeginDisabledGroup(m_NormalMapQuality.intValue == (int)Light2D.NormalMapQuality.Disabled);
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(m_NormalMapZDistance, Styles.generalNormalMapZDistance);
                if (EditorGUI.EndChangeCheck())
                    m_NormalMapZDistance.floatValue = Mathf.Max(0.0f, m_NormalMapZDistance.floatValue);

                EditorGUI.EndDisabledGroup();
            }
        }

        void DrawFoldouts()
        {
            DrawBlendingGroup();
            DrawShadowsGroup();
            DrawVolumetricGroup();
            DrawNormalMapGroup();
        }

        void DrawRadiusProperties(GUIContent label, SerializedProperty innerRadius, GUIContent content1, SerializedProperty outerRadius, GUIContent content2)
        {
            GUIStyle style = GUI.skin.box;

            float savedLabelWidth = EditorGUIUtility.labelWidth;
            int savedIndentLevel = EditorGUI.indentLevel;
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel(label);
            EditorGUILayout.BeginHorizontal();
            EditorGUI.indentLevel = 0;
            EditorGUIUtility.labelWidth = style.CalcSize(content1).x;

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(innerRadius, content1);
            if (EditorGUI.EndChangeCheck())
            {
                if (innerRadius.floatValue > outerRadius.floatValue)
                    innerRadius.floatValue = outerRadius.floatValue;
                else if (innerRadius.floatValue < 0)
                    innerRadius.floatValue = 0;
            }

            EditorGUIUtility.labelWidth = style.CalcSize(content2).x;
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(outerRadius, content2);
            if (EditorGUI.EndChangeCheck() && outerRadius.floatValue < innerRadius.floatValue)
                outerRadius.floatValue = innerRadius.floatValue;

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndHorizontal();
            EditorGUIUtility.labelWidth = savedLabelWidth;
            EditorGUI.indentLevel = savedIndentLevel;
        }

        void DrawToggleProperty(GUIContent label, SerializedProperty boolProperty, SerializedProperty property)
        {
            int savedIndentLevel = EditorGUI.indentLevel;
            float savedLabelWidth = EditorGUIUtility.labelWidth;
            const int kCheckboxWidth = 20;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(boolProperty, GUIContent.none, GUILayout.MaxWidth(kCheckboxWidth));

            EditorGUIUtility.labelWidth = EditorGUIUtility.labelWidth - kCheckboxWidth;
            EditorGUI.BeginDisabledGroup(!boolProperty.boolValue);
            EditorGUILayout.PropertyField(property, label);
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.EndHorizontal();

            EditorGUI.indentLevel = savedIndentLevel;

            EditorGUIUtility.labelWidth = savedLabelWidth;
        }

        public void DrawInnerAndOuterSpotAngle(SerializedProperty minProperty, SerializedProperty maxProperty, GUIContent label)
        {
            float textFieldWidth = 45f;

            float min = minProperty.floatValue;
            float max = maxProperty.floatValue;

            var rect = EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight);
            // This widget is a little bit of a special case.
            // The right hand side of the min max slider will control the reset of the max value
            // The left hand side of the min max slider will control the reset of the min value
            // The label itself will not have a right click and reset value.

            rect = EditorGUI.PrefixLabel(rect, label);
            EditorGUI.BeginProperty(new Rect(rect) { width = rect.width * 0.5f }, label, minProperty);
            EditorGUI.BeginProperty(new Rect(rect) { xMin = rect.x + rect.width * 0.5f }, GUIContent.none, maxProperty);

            var minRect = new Rect(rect) { width = textFieldWidth };
            var maxRect = new Rect(rect) { xMin = rect.xMax - textFieldWidth };
            var sliderRect = new Rect(rect) { xMin = minRect.xMax + 4, xMax = maxRect.xMin - 4 };

            EditorGUI.BeginChangeCheck();
            EditorGUI.DelayedFloatField(minRect, minProperty, GUIContent.none);
            if (EditorGUI.EndChangeCheck())
            {
                if (minProperty.floatValue > maxProperty.floatValue)
                    minProperty.floatValue = maxProperty.floatValue;
                else if (minProperty.floatValue < 0)
                    minProperty.floatValue = 0;
            }

            EditorGUI.BeginChangeCheck();
            EditorGUI.MinMaxSlider(sliderRect, ref min, ref max, 0f, 360f);

            if (EditorGUI.EndChangeCheck())
            {
                minProperty.floatValue = min;
                maxProperty.floatValue = max;
            }

            EditorGUI.BeginChangeCheck();
            EditorGUI.DelayedFloatField(maxRect, m_PointOuterAngle, GUIContent.none);
            if (EditorGUI.EndChangeCheck())
            {
                if (minProperty.floatValue > maxProperty.floatValue)
                    maxProperty.floatValue = minProperty.floatValue;
                else if (maxProperty.floatValue > 360)
                    maxProperty.floatValue = 360;
            }


            EditorGUI.EndProperty();
            EditorGUI.EndProperty();
        }

        void DrawGlobalLight(SerializedObject serializedObject)
        {
            m_SortingLayerDropDown.OnTargetSortingLayers(serializedObject, targets, Styles.generalSortingLayerPrefixLabel, AnalyticsTrackChanges);
            DrawBlendingGroup();
        }

        void DrawParametricDeprecated(SerializedObject serializedObject)
        {
            GUIContent buttonText = targets.Length > 1 ? Styles.deprecatedParametricLightButtonMulti : Styles.deprecatedParametricLightButtonSingle;
            GUIContent helpText = targets.Length > 1 ? Styles.deprecatedParametricLightWarningMulti : Styles.deprecatedParametricLightWarningSingle;
            string dialogText = targets.Length > 1 ? Styles.deprecatedParametricLightDialogTextMulti : Styles.deprecatedParametricLightDialogTextSingle;

            EditorGUILayout.HelpBox(helpText);
            EditorGUILayout.Space();


            if (GUILayout.Button(buttonText))
            {
                if (EditorUtility.DisplayDialog(Styles.deprecatedParametricLightDialogTitle, dialogText, Styles.deprecatedParametricLightDialogProceed, Styles.deprecatedParametricLightDialogCancel))
                {
                    for (int i = 0; i < targets.Length; i++)
                    {
                        Light2D light = (Light2D)targets[i];

                        if (light.lightType == (Light2D.LightType)Light2D.DeprecatedLightType.Parametric)
                            ParametricToFreeformLightUpgrader.UpgradeParametricLight(light);
                    }
                }
            }
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(Styles.deprecatedParametricLightInstructions);
        }

        bool DrawLightCommon()
        {
            var meshChanged = false;
            Rect lightTypeRect = EditorGUILayout.GetControlRect();
            EditorGUI.BeginProperty(lightTypeRect, GUIContent.none, m_LightType);
            EditorGUI.BeginChangeCheck();
            int newLightType = EditorGUI.Popup(lightTypeRect, Styles.generalLightType, m_LightType.intValue - 1, Styles.lightTypeOptions);  // -1 is a bit hacky its to support compatibiltiy. We need something better.
            if (EditorGUI.EndChangeCheck())
            {
                m_LightType.intValue = newLightType + 1; // -1 is a bit hacky its to support compatibiltiy. We need something better.
                meshChanged = true;
            }
            EditorGUI.EndProperty();

            // Color and intensity
            EditorGUILayout.PropertyField(m_LightColor, Styles.generalLightColor);
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(m_LightIntensity, Styles.generalLightIntensity);
            if (EditorGUI.EndChangeCheck())
                m_LightIntensity.floatValue = Mathf.Max(m_LightIntensity.floatValue, 0);

            return meshChanged;
        }

        void DrawSpotLight(SerializedObject serializedObject)
        {
            DrawRadiusProperties(Styles.pointLightRadius, m_PointInnerRadius, Styles.pointLightInner, m_PointOuterRadius, Styles.pointLightOuter);
            DrawInnerAndOuterSpotAngle(m_PointInnerAngle, m_PointOuterAngle, Styles.InnerOuterSpotAngle);
            EditorGUILayout.Slider(m_FalloffIntensity, 0, 1, Styles.generalFalloffIntensity);

            if (m_DeprecatedPointLightSprite.objectReferenceValue != null)
                EditorGUILayout.PropertyField(m_DeprecatedPointLightSprite, Styles.pointLightSprite);

            m_SortingLayerDropDown.OnTargetSortingLayers(serializedObject, targets, Styles.generalSortingLayerPrefixLabel, AnalyticsTrackChanges);

            DrawFoldouts();
        }

        void DrawSpriteLight(SerializedObject serializedObject)
        {
            EditorGUILayout.PropertyField(m_ShapeLightSprite, Styles.shapeLightSprite);

            m_SortingLayerDropDown.OnTargetSortingLayers(serializedObject, targets, Styles.generalSortingLayerPrefixLabel, AnalyticsTrackChanges);
            DrawFoldouts();
        }

        void DrawShapeLight(SerializedObject serializedObject)
        {
            EditorGUILayout.PropertyField(m_ShapeLightFalloffSize, Styles.generalFalloffSize);
            if (m_ShapeLightFalloffSize.floatValue < 0)
                m_ShapeLightFalloffSize.floatValue = 0;

            EditorGUILayout.Slider(m_FalloffIntensity, 0, 1, Styles.generalFalloffIntensity);

            m_SortingLayerDropDown.OnTargetSortingLayers(serializedObject, targets, Styles.generalSortingLayerPrefixLabel, AnalyticsTrackChanges);

            if (m_LightType.intValue == (int)Light2D.LightType.Freeform)
            {
                DoEditButton<FreeformShapeTool>(PathEditorToolContents.icon, "Edit Shape");
                DoPathInspector<FreeformShapeTool>();
                DoSnappingInspector<FreeformShapeTool>();
            }

            DrawFoldouts();
        }

        Vector3 DrawAngleSlider2D(Transform transform, Quaternion rotation, float radius, float offset, Handles.CapFunction capFunc, float capSize, bool leftAngle, bool drawLine, bool useCapOffset, ref float angle)
        {
            float oldAngle = angle;

            float angleBy2 = (angle / 2) * (leftAngle ? -1.0f : 1.0f);
            Vector3 trcwPos = Quaternion.AngleAxis(angleBy2, -transform.forward) * (transform.up);
            Vector3 cwPos = transform.position + trcwPos * (radius + offset);

            float direction = leftAngle ? 1 : -1;

            // Offset the handle
            float size = .25f * capSize;

            Vector3 handleOffset = useCapOffset ? rotation * new Vector3(direction * size, 0, 0) : Vector3.zero;

            EditorGUI.BeginChangeCheck();
            var id = GUIUtility.GetControlID("AngleSlider".GetHashCode(), FocusType.Passive);
            Vector3 cwHandle = Handles.Slider2D(id, cwPos, handleOffset, Vector3.forward, rotation * Vector3.up, rotation * Vector3.right, capSize, capFunc, Vector3.zero);
            if (EditorGUI.EndChangeCheck())
            {
                Vector3 toCwHandle = (transform.position - cwHandle).normalized;

                angle = 360 - 2 * Quaternion.Angle(Quaternion.FromToRotation(transform.up, toCwHandle), Quaternion.identity);
                angle = Mathf.Round(angle * 100) / 100f;

                float side = Vector3.Dot(direction * transform.right, toCwHandle);
                if (side < 0)
                {
                    if (oldAngle < 180)
                        angle = 0;
                    else
                        angle = 360;
                }
            }

            if (drawLine)
                Handles.DrawLine(transform.position, cwHandle);

            return cwHandle;
        }

        private float DrawAngleHandle(Transform transform, float radius, float offset, Handles.CapFunction capLeft, Handles.CapFunction capRight, ref float angle)
        {
            float old = angle;
            float handleOffset = HandleUtility.GetHandleSize(transform.position) * offset;
            float handleSize = HandleUtility.GetHandleSize(transform.position) * k_AngleCapSize;

            Quaternion rotLt = Quaternion.AngleAxis(-angle / 2, -transform.forward) * transform.rotation;
            DrawAngleSlider2D(transform, rotLt, radius, handleOffset, capLeft, handleSize, true, true, true, ref angle);

            Quaternion rotRt = Quaternion.AngleAxis(angle / 2, -transform.forward) * transform.rotation;
            DrawAngleSlider2D(transform, rotRt, radius, handleOffset, capRight, handleSize, false, true, true, ref angle);

            return angle - old;
        }

        private void DrawRadiusArc(Transform transform, float radius, float angle, int steps, Handles.CapFunction capFunc, float capSize, bool even)
        {
            Handles.DrawWireArc(transform.position, transform.forward, Quaternion.AngleAxis(180 - angle / 2, transform.forward) * -transform.up, angle, radius);
        }

        Handles.CapFunction GetCapFunc(Texture texture, bool isAngleHandle)
        {
            return (controlID, position, rotation, size, eventType) => Light2DEditorUtility.GUITextureCap(controlID, texture, position, rotation, size, eventType, isAngleHandle);
        }

        private void DrawAngleHandles(Light2D light)
        {
            var oldColor = Handles.color;
            Handles.color = Color.yellow;

            float outerAngle = light.pointLightOuterAngle;
            float diff = DrawAngleHandle(light.transform, light.pointLightOuterRadius, k_AngleCapOffset, GetCapFunc(Styles.lightCapTopRight, true), GetCapFunc(Styles.lightCapBottomRight, true), ref outerAngle);
            light.pointLightOuterAngle = outerAngle;

            if (diff != 0.0f)
                light.pointLightInnerAngle = Mathf.Max(0.0f, light.pointLightInnerAngle + diff);

            float innerAngle = light.pointLightInnerAngle;
            diff = DrawAngleHandle(light.transform, light.pointLightOuterRadius, -k_AngleCapOffset, GetCapFunc(Styles.lightCapTopLeft, true), GetCapFunc(Styles.lightCapBottomLeft, true), ref innerAngle);
            light.pointLightInnerAngle = innerAngle;

            if (diff != 0.0f)
                light.pointLightInnerAngle = light.pointLightInnerAngle < light.pointLightOuterAngle ? light.pointLightInnerAngle : light.pointLightOuterAngle;

            light.pointLightInnerAngle = Mathf.Min(light.pointLightInnerAngle, light.pointLightOuterAngle);

            Handles.color = oldColor;
        }

        private void DrawRangeHandles(Light2D light)
        {
            var dummy = 0.0f;
            bool radiusChanged = false;
            Vector3 handlePos = Vector3.zero;
            Quaternion rotLeft = Quaternion.AngleAxis(0, -light.transform.forward) * light.transform.rotation;
            float handleOffset = HandleUtility.GetHandleSize(light.transform.position) * k_AngleCapOffsetSecondary;
            float handleSize = HandleUtility.GetHandleSize(light.transform.position) * k_AngleCapSize;

            var oldColor = Handles.color;
            Handles.color = Color.yellow;

            float outerRadius = light.pointLightOuterRadius;
            EditorGUI.BeginChangeCheck();
            Vector3 returnPos = DrawAngleSlider2D(light.transform, rotLeft, outerRadius, -handleOffset, GetCapFunc(Styles.lightCapUp, false), handleSize, false, false, false, ref dummy);
            if (EditorGUI.EndChangeCheck())
            {
                var vec = (returnPos - light.transform.position).normalized;
                light.transform.up = new Vector3(vec.x, vec.y, 0);
                outerRadius = (returnPos - light.transform.position).magnitude;
                outerRadius = outerRadius + handleOffset;
                radiusChanged = true;
            }
            DrawRadiusArc(light.transform, light.pointLightOuterRadius, light.pointLightOuterAngle, 0, Handles.DotHandleCap, k_RangeCapSize, false);

            Handles.color = Color.gray;
            float innerRadius = light.pointLightInnerRadius;
            EditorGUI.BeginChangeCheck();
            returnPos = DrawAngleSlider2D(light.transform, rotLeft, innerRadius, handleOffset, GetCapFunc(Styles.lightCapDown, false), handleSize, true, false, false, ref dummy);
            if (EditorGUI.EndChangeCheck())
            {
                innerRadius = (returnPos - light.transform.position).magnitude;
                innerRadius = innerRadius - handleOffset;
                radiusChanged = true;
            }
            DrawRadiusArc(light.transform, light.pointLightInnerRadius, light.pointLightOuterAngle, 0, Handles.SphereHandleCap, k_InnerRangeCapSize, false);

            Handles.color = oldColor;

            if (radiusChanged)
            {
                light.pointLightInnerRadius = (outerRadius < innerRadius) ? outerRadius : innerRadius;
                light.pointLightOuterRadius = (innerRadius > outerRadius) ? innerRadius : outerRadius;
            }
        }

        void OnSceneGUI()
        {
            var light = target as Light2D;
            if (light == null)
                return;

            Transform t = light.transform;
            switch (light.lightType)
            {
                case Light2D.LightType.Point:
                {
                    Undo.RecordObject(light.transform, "Edit Point Light Transform");
                    Undo.RecordObject(light, "Edit Point Light");

                    DrawRangeHandles(light);
                    DrawAngleHandles(light);

                    if (GUI.changed)
                        EditorUtility.SetDirty(light);
                }
                break;
                case Light2D.LightType.Sprite:
                {
                    var cookieSprite = light.lightCookieSprite;
                    if (cookieSprite != null)
                    {
                        Vector3 min = cookieSprite.bounds.min;
                        Vector3 max = cookieSprite.bounds.max;

                        Vector3 v0 = t.TransformPoint(new Vector3(min.x, min.y));
                        Vector3 v1 = t.TransformPoint(new Vector3(max.x, min.y));
                        Vector3 v2 = t.TransformPoint(new Vector3(max.x, max.y));
                        Vector3 v3 = t.TransformPoint(new Vector3(min.x, max.y));

                        Handles.DrawLine(v0, v1);
                        Handles.DrawLine(v1, v2);
                        Handles.DrawLine(v2, v3);
                        Handles.DrawLine(v3, v0);
                    }
                }
                break;
                case Light2D.LightType.Freeform:
                {
                    // Draw the falloff shape's outline
                    List<Vector2> falloffShape = light.GetFalloffShape();
                    Handles.color = Color.white;

                    for (int i = 0; i < falloffShape.Count - 1; ++i)
                    {
                        Handles.DrawLine(t.TransformPoint(falloffShape[i]), t.TransformPoint(falloffShape[i + 1]));
                    }

                    if (falloffShape.Count > 0)
                        Handles.DrawLine(t.TransformPoint(falloffShape[falloffShape.Count - 1]), t.TransformPoint(falloffShape[0]));

                    for (int i = 0; i < light.shapePath.Length - 1; ++i)
                    {
                        Handles.DrawLine(t.TransformPoint(light.shapePath[i]),
                            t.TransformPoint(light.shapePath[i + 1]));
                    }

                    if (light.shapePath.Length > 0)
                        Handles.DrawLine(t.TransformPoint(light.shapePath[light.shapePath.Length - 1]), t.TransformPoint(light.shapePath[0]));
                }
                break;
            }
        }

        public override void OnInspectorGUI()
        {
            var meshChanged = false;

            serializedObject.Update();

            UniversalRenderPipelineAsset asset = UniversalRenderPipeline.asset;
            if (asset != null)
            {
                if (!Light2DEditorUtility.IsUsing2DRenderer())
                {
                    EditorGUILayout.HelpBox(Styles.asset2DUnassignedWarning);
                }
                else
                {
                    if (m_LightType.intValue != (int)Light2D.DeprecatedLightType.Parametric)
                        meshChanged = DrawLightCommon();

                    switch (m_LightType.intValue)
                    {
                        case (int)Light2D.LightType.Point:
                        {
                            DrawSpotLight(serializedObject);
                        }
                        break;
                        case (int)Light2D.LightType.Freeform:
                        {
                            DrawShapeLight(serializedObject);
                        }
                        break;
                        case (int)Light2D.LightType.Sprite:
                        {
                            DrawSpriteLight(serializedObject);
                        }
                        break;
                        case (int)Light2D.LightType.Global:
                        {
                            DrawGlobalLight(serializedObject);
                        }
                        break;
                        case (int)Light2D.DeprecatedLightType.Parametric:
                        {
                            DrawParametricDeprecated(serializedObject);
                        }
                        break;
                    }

                    AnalyticsTrackChanges(serializedObject);
                    if (serializedObject.ApplyModifiedProperties())
                    {
                        if (meshChanged)
                            lightObject.UpdateMesh();
                    }
                }
            }
            else
            {
                EditorGUILayout.HelpBox(Styles.renderPipelineUnassignedWarning);

                if (meshChanged)
                    lightObject.UpdateMesh();
            }
        }
    }

    internal class Light2DPostProcess : AssetPostprocessor
    {
        void OnPostprocessSprites(Texture2D texture, Sprite[] sprites)
        {
            var lights = Resources.FindObjectsOfTypeAll<Light2D>().Where(x => x.lightType == Light2D.LightType.Sprite && x.lightCookieSprite == null);

            foreach (var light in lights)
                light.MarkForUpdate();
        }
    }
}
