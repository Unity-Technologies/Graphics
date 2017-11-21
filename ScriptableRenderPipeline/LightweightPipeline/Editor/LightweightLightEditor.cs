using UnityEditor.AnimatedValues;
using UnityEngine;
using UnityEngine.Experimental.Rendering.LightweightPipeline;

// TODO: Once we can target 2018.1 we can remove many duplicated properties in this class
namespace UnityEditor.Experimental.Rendering.LightweightPipeline
{
    [CanEditMultipleObjects]
    [CustomEditorForRenderPipeline(typeof(Light), typeof(LightweightPipelineAsset))]
    class LightweightLightEditor : LightEditor
    {
        SerializedProperty typeProp;
        SerializedProperty rangeProp;
        SerializedProperty spotAngleProp;
        SerializedProperty cookieSizeProp;
        SerializedProperty colorProp;
        SerializedProperty intensityProp;
        SerializedProperty bounceIntensityProp;
        SerializedProperty cookieProp;
        SerializedProperty shadowsTypeProp;
        SerializedProperty shadowsStrengthProp;
        SerializedProperty shadowsResolutionProp;
        SerializedProperty shadowsBiasProp;
        SerializedProperty shadowsNormalBiasProp;
        SerializedProperty shadowsNearPlaneProp;
        SerializedProperty renderModeProp;
        SerializedProperty cullingMaskProp;
        SerializedProperty lightmappingProp;
        SerializedProperty areaSizeXProp;
        SerializedProperty areaSizeYProp;
        SerializedProperty bakedShadowRadiusProp;
        SerializedProperty bakedShadowAngleProp;

        AnimBool animShowSpotOptions = new AnimBool();
        AnimBool animShowPointOptions = new AnimBool();
        AnimBool animShowDirOptions = new AnimBool();
        AnimBool animShowAreaOptions = new AnimBool();
        AnimBool animShowRuntimeOptions = new AnimBool();
        AnimBool animShowShadowOptions = new AnimBool();
        AnimBool animBakedShadowAngleOptions = new AnimBool();
        AnimBool animBakedShadowRadiusOptions = new AnimBool();
        AnimBool animShowLightBounceIntensity = new AnimBool();

        class Styles
        {
            public readonly GUIContent Type = new GUIContent("Type", "Specifies the current type of light. Possible types are Directional, Spot, Point, and Area lights.");
            public readonly GUIContent Range = new GUIContent("Range", "Controls how far the light is emitted from the center of the object.");
            public readonly GUIContent SpotAngle = new GUIContent("Spot Angle", "Controls the angle in degrees at the base of a Spot light's cone.");
            public readonly GUIContent Color = new GUIContent("Color", "Controls the color being emitted by the light.");
            public readonly GUIContent Intensity = new GUIContent("Intensity", "Controls the brightness of the light. Light color is multiplied by this value.");
            public readonly GUIContent LightmappingMode = new GUIContent("Mode", "Specifies the light mode used to determine if and how a light will be baked. Possible modes are Baked, Mixed, and Realtime.");
            public readonly GUIContent LightBounceIntensity = new GUIContent("Indirect Multiplier", "Controls the intensity of indirect light being contributed to the scene. A value of 0 will cause Realtime lights to be removed from realtime global illumination and Baked and Mixed lights to no longer emit indirect lighting. Has no effect when both Realtime and Baked Global Illumination are disabled.");
            public readonly GUIContent ShadowType = new GUIContent("Shadow Type", "Specifies whether Hard Shadows, Soft Shadows, or No Shadows will be cast by the light.");
            //realtime
            public readonly GUIContent ShadowRealtimeSettings = new GUIContent("Realtime Shadows", "Settings for realtime direct shadows.");
            public readonly GUIContent ShadowStrength = new GUIContent("Strength", "Controls how dark the shadows cast by the light will be.");
            public readonly GUIContent ShadowResolution = new GUIContent("Resolution", "Controls the rendered resolution of the shadow maps. A higher resolution will increase the fidelity of shadows at the cost of GPU performance and memory usage.");
            public readonly GUIContent ShadowBias = new GUIContent("Bias", "Controls the distance at which the shadows will be pushed away from the light. Useful for avoiding false self-shadowing artifacts.");
            public readonly GUIContent ShadowNormalBias = new GUIContent("Normal Bias", "Controls distance at which the shadow casting surfaces will be shrunk along the surface normal. Useful for avoiding false self-shadowing artifacts.");
            public readonly GUIContent ShadowNearPlane = new GUIContent("Near Plane", "Controls the value for the near clip plane when rendering shadows. Currently clamped to 0.1 units or 1% of the lights range property, whichever is lower.");
            //baked
            public readonly GUIContent BakedShadowRadius = new GUIContent("Baked Shadow Radius", "Controls the amount of artificial softening applied to the edges of shadows cast by the Point or Spot light.");
            public readonly GUIContent BakedShadowAngle = new GUIContent("Baked Shadow Angle", "Controls the amount of artificial softening applied to the edges of shadows cast by directional lights.");

            public readonly GUIContent Cookie = new GUIContent("Cookie", "Specifies the Texture mask to cast shadows, create silhouettes, or patterned illumination for the light.");
            public readonly GUIContent CookieSize = new GUIContent("Cookie Size", "Controls the size of the cookie mask currently assigned to the light.");
            public readonly GUIContent RenderMode = new GUIContent("Render Mode", "Specifies the importance of the light which impacts lighting fidelity and performance. Options are Auto, Important, and Not Important. This only affects Forward Rendering");
            public readonly GUIContent CullingMask = new GUIContent("Culling Mask", "Specifies which layers will be affected or excluded from the light's effect on objects in the scene.");

            public readonly GUIStyle invisibleButton = "InvisibleButton";

            public readonly GUIContent AreaWidth = new GUIContent("Width", "Controls the width in units of the area light.");
            public readonly GUIContent AreaHeight = new GUIContent("Height", "Controls the height in units of the area light.");

            public readonly GUIContent BakingWarning = new GUIContent("Light mode is currently overridden to Realtime mode. Enable Baked Global Illumination to use Mixed or Baked light modes.");
            public readonly GUIContent IndirectBounceShadowWarning = new GUIContent("Realtime indirect bounce shadowing is not supported for Spot and Point lights.");
            public readonly GUIContent CookieWarning = new GUIContent("Cookie textures for spot lights should be set to clamp, not repeat, to avoid artifacts.");
            public readonly GUIContent DisabledLightWarning = new GUIContent("Lighting has been disabled in at least one Scene view. Any changes applied to lights in the Scene will not be updated in these views until Lighting has been enabled again.");

            public readonly GUIContent[] LightmapBakeTypeTitles = { new GUIContent("Realtime"), new GUIContent("Mixed"), new GUIContent("Baked") };
            public readonly int[] LightmapBakeTypeValues = { (int)LightmapBakeType.Realtime, (int)LightmapBakeType.Mixed, (int)LightmapBakeType.Baked };

            public readonly GUIContent ShadowsNotSupportedWarning = new GUIContent("Realtime shadows for point lights are not supported. Either disable shadows or set the light mode to Baked.");
        }

        static Styles s_Styles;

        private bool TypeIsSame { get { return !typeProp.hasMultipleDifferentValues; } }
        private bool ShadowTypeIsSame { get { return !shadowsTypeProp.hasMultipleDifferentValues; } }
        private bool LightmappingTypeIsSame { get { return !lightmappingProp.hasMultipleDifferentValues; } }
        private Light LightProperty { get { return target as Light; } }
        private bool IsRealtime { get { return lightmappingProp.intValue == 4; } }

        private bool IsCompletelyBaked { get { return lightmappingProp.intValue == 2; } }
        private bool IsBakedOrMixed { get { return !IsRealtime; } }
        private Texture Cookie { get { return cookieProp.objectReferenceValue as Texture; } }

        private bool SpotOptionsValue { get { return TypeIsSame && LightProperty.type == LightType.Spot; } }
        private bool PointOptionsValue { get { return TypeIsSame && LightProperty.type == LightType.Point; } }
        private bool DirOptionsValue { get { return TypeIsSame && LightProperty.type == LightType.Directional; } }
        private bool AreaOptionsValue { get { return TypeIsSame && LightProperty.type == LightType.Area; } }

        // Point light realtime shadows not supported
        private bool RuntimeOptionsValue { get { return TypeIsSame && (LightProperty.type != LightType.Area && LightProperty.type != LightType.Point && !IsCompletelyBaked); } }
        private bool BakedShadowRadius { get { return TypeIsSame && (LightProperty.type == LightType.Point || LightProperty.type == LightType.Spot) && IsBakedOrMixed; } }
        private bool BakedShadowAngle { get { return TypeIsSame && LightProperty.type == LightType.Directional && IsBakedOrMixed; } }
        private bool ShadowOptionsValue { get { return ShadowTypeIsSame && LightProperty.shadows != LightShadows.None; } }

        private bool BounceWarningValue
        {
            get
            {
                return TypeIsSame && (LightProperty.type == LightType.Point || LightProperty.type == LightType.Spot) &&
                    LightmappingTypeIsSame && IsRealtime && !bounceIntensityProp.hasMultipleDifferentValues && bounceIntensityProp.floatValue > 0.0F;
            }
        }
        private bool BakingWarningValue { get { return !UnityEditor.Lightmapping.bakedGI && LightmappingTypeIsSame && IsBakedOrMixed; } }
        private bool ShowLightBounceIntensity { get { return true; } }
        private bool CookieWarningValue
        {
            get
            {
                return TypeIsSame && LightProperty.type == LightType.Spot &&
                    !cookieProp.hasMultipleDifferentValues && Cookie && Cookie.wrapMode != TextureWrapMode.Clamp;
            }
        }

        private bool IsShadowEnabled { get { return shadowsTypeProp.intValue != 0; } }

        private bool RealtimeShadowsWarningValue
        {
            get
            {
                return TypeIsSame && LightProperty.type == LightType.Point &&
                        ShadowTypeIsSame && IsShadowEnabled &&
                       LightmappingTypeIsSame && !IsCompletelyBaked;
            }
        }


        private void SetOptions(AnimBool animBool, bool initialize, bool targetValue)
        {
            if (initialize)
            {
                animBool.value = targetValue;
                animBool.valueChanged.AddListener(Repaint);
            }
            else
            {
                animBool.target = targetValue;
            }
        }

        private void UpdateShowOptions(bool initialize)
        {
            SetOptions(animShowSpotOptions, initialize, SpotOptionsValue);
            SetOptions(animShowPointOptions, initialize, PointOptionsValue);
            SetOptions(animShowDirOptions, initialize, DirOptionsValue);
            SetOptions(animShowAreaOptions, initialize, AreaOptionsValue);
            SetOptions(animShowShadowOptions, initialize, ShadowOptionsValue);
            SetOptions(animShowRuntimeOptions, initialize, RuntimeOptionsValue);
            SetOptions(animBakedShadowAngleOptions, initialize, BakedShadowAngle);
            SetOptions(animBakedShadowRadiusOptions, initialize, BakedShadowRadius);
            SetOptions(animShowLightBounceIntensity, initialize, ShowLightBounceIntensity);
        }

        void OnEnable()
        {
            typeProp = serializedObject.FindProperty("m_Type");
            rangeProp = serializedObject.FindProperty("m_Range");
            spotAngleProp = serializedObject.FindProperty("m_SpotAngle");
            cookieSizeProp = serializedObject.FindProperty("m_CookieSize");
            colorProp = serializedObject.FindProperty("m_Color");
            intensityProp = serializedObject.FindProperty("m_Intensity");
            bounceIntensityProp = serializedObject.FindProperty("m_BounceIntensity");
            cookieProp = serializedObject.FindProperty("m_Cookie");
            shadowsTypeProp = serializedObject.FindProperty("m_Shadows.m_Type");
            shadowsStrengthProp = serializedObject.FindProperty("m_Shadows.m_Strength");
            shadowsResolutionProp = serializedObject.FindProperty("m_Shadows.m_Resolution");
            shadowsBiasProp = serializedObject.FindProperty("m_Shadows.m_Bias");
            shadowsNormalBiasProp = serializedObject.FindProperty("m_Shadows.m_NormalBias");
            shadowsNearPlaneProp = serializedObject.FindProperty("m_Shadows.m_NearPlane");
            renderModeProp = serializedObject.FindProperty("m_RenderMode");
            cullingMaskProp = serializedObject.FindProperty("m_CullingMask");
            lightmappingProp = serializedObject.FindProperty("m_Lightmapping");
            areaSizeXProp = serializedObject.FindProperty("m_AreaSize.x");
            areaSizeYProp = serializedObject.FindProperty("m_AreaSize.y");
            bakedShadowRadiusProp = serializedObject.FindProperty("m_ShadowRadius");
            bakedShadowAngleProp = serializedObject.FindProperty("m_ShadowAngle");

            UpdateShowOptions(true);
        }

        public void DrawLightType()
        {
            EditorGUILayout.PropertyField(typeProp, s_Styles.Type);
        }

        public void DrawRange(bool showAreaOptions)
        {
            // If the light is an area light, the range is determined by other parameters.
            // Therefore, disable area light's range for editing, but just update the editor field.
            if (showAreaOptions)
            {
                GUI.enabled = false;
                string areaLightToolTip = "For area lights " + rangeProp.displayName + " is computed from Width, Height and Intensity";
                GUIContent areaRangeWithToolTip = new GUIContent(rangeProp.displayName, areaLightToolTip);
                EditorGUILayout.FloatField(areaRangeWithToolTip, LightProperty.range);
                GUI.enabled = true;
            }
            else
                EditorGUILayout.PropertyField(rangeProp, s_Styles.Range);
        }

        public void DrawSpotAngle()
        {
            EditorGUILayout.Slider(spotAngleProp, 1f, 179f, s_Styles.SpotAngle);
        }

        public void DrawArea()
        {
            EditorGUILayout.PropertyField(areaSizeXProp, s_Styles.AreaWidth);
            EditorGUILayout.PropertyField(areaSizeYProp, s_Styles.AreaHeight);
        }

        public void DrawColor()
        {
            EditorGUILayout.PropertyField(colorProp, s_Styles.Color);
        }

        public void DrawLightmapping()
        {
            EditorGUILayout.IntPopup(lightmappingProp, s_Styles.LightmapBakeTypeTitles, s_Styles.LightmapBakeTypeValues, s_Styles.LightmappingMode);

            // Warning if GI Baking disabled and m_Lightmapping isn't realtime
            if (BakingWarningValue)
            {
                EditorGUILayout.HelpBox(s_Styles.BakingWarning.text, MessageType.Info);
            }
        }

        public void DrawIntensity()
        {
            EditorGUILayout.PropertyField(intensityProp, s_Styles.Intensity);
        }

        public void DrawBounceIntensity()
        {
            EditorGUILayout.PropertyField(bounceIntensityProp, s_Styles.LightBounceIntensity);
            // Indirect shadows warning (Should be removed when we support realtime indirect shadows)
            if (BounceWarningValue)
            {
                EditorGUILayout.HelpBox(s_Styles.IndirectBounceShadowWarning.text, MessageType.Info);
            }
        }

        public void DrawCookie()
        {
            EditorGUILayout.PropertyField(cookieProp, s_Styles.Cookie);

            if (CookieWarningValue)
            {
                // warn on spotlights if the cookie is set to repeat
                EditorGUILayout.HelpBox(s_Styles.CookieWarning.text, MessageType.Warning);
            }
        }

        public void DrawCookieSize()
        {
            EditorGUILayout.PropertyField(cookieSizeProp, s_Styles.CookieSize);
        }

        public void DrawRenderMode()
        {
            EditorGUILayout.PropertyField(renderModeProp, s_Styles.RenderMode);
        }

        public void DrawCullingMask()
        {
            EditorGUILayout.PropertyField(cullingMaskProp, s_Styles.CullingMask);
        }

        public void DrawShadowsType()
        {
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(shadowsTypeProp, s_Styles.ShadowType);
        }

        public void DrawBakedShadowRadius()
        {
            using (new EditorGUI.DisabledScope(shadowsTypeProp.intValue != (int)LightShadows.Soft))
            {
                EditorGUILayout.PropertyField(bakedShadowRadiusProp, s_Styles.BakedShadowRadius);
            }
        }

        public void DrawBakedShadowAngle()
        {
            using (new EditorGUI.DisabledScope(shadowsTypeProp.intValue != (int)LightShadows.Soft))
            {
                EditorGUILayout.Slider(bakedShadowAngleProp, 0.0F, 90.0F, s_Styles.BakedShadowAngle);
            }
        }

        public void DrawRuntimeShadow()
        {
            EditorGUILayout.LabelField(s_Styles.ShadowRealtimeSettings);
            EditorGUI.indentLevel += 1;
            EditorGUILayout.Slider(shadowsStrengthProp, 0f, 1f, s_Styles.ShadowStrength);
            EditorGUILayout.PropertyField(shadowsResolutionProp, s_Styles.ShadowResolution);
            EditorGUILayout.Slider(shadowsBiasProp, 0.0f, 2.0f, s_Styles.ShadowBias);
            EditorGUILayout.Slider(shadowsNormalBiasProp, 0.0f, 3.0f, s_Styles.ShadowNormalBias);

            // this min bound should match the calculation in SharedLightData::GetNearPlaneMinBound()
            float nearPlaneMinBound = Mathf.Min(0.01f*rangeProp.floatValue, 0.1f);
            EditorGUILayout.Slider(shadowsNearPlaneProp, nearPlaneMinBound, 10.0f, s_Styles.ShadowNearPlane);
            EditorGUI.indentLevel -= 1;
        }

        public override void OnInspectorGUI()
        {
            if (s_Styles == null)
                s_Styles = new Styles();

            serializedObject.Update();

            // Update AnimBool options. For properties changed they will be smoothly interpolated.
            UpdateShowOptions(false);

            DrawLightType();

            EditorGUILayout.Space();

            // When we are switching between two light types that don't show the range (directional and area lights)
            // we want the fade group to stay hidden.
            using (var group = new EditorGUILayout.FadeGroupScope(1.0f - animShowDirOptions.faded))
                if (group.visible) DrawRange(animShowAreaOptions.target);

            // Spot angle
            using (var group = new EditorGUILayout.FadeGroupScope(animShowSpotOptions.faded))
                if (group.visible) DrawSpotAngle();

            // Area width & height
            using (var group = new EditorGUILayout.FadeGroupScope(animShowAreaOptions.faded))
                if (group.visible) DrawArea();

            DrawColor();

            EditorGUILayout.Space();

            using (var group = new EditorGUILayout.FadeGroupScope(1.0f - animShowAreaOptions.faded))
                if (group.visible) DrawLightmapping();

            DrawIntensity();

            using (var group = new EditorGUILayout.FadeGroupScope(animShowLightBounceIntensity.faded))
                if (group.visible) DrawBounceIntensity();

            ShadowsGUI();

            using (var group = new EditorGUILayout.FadeGroupScope(animShowRuntimeOptions.faded))
                if (group.visible) DrawCookie();

            // Cookie size also requires directional light
            using (var group = new EditorGUILayout.FadeGroupScope(animShowRuntimeOptions.faded * animShowDirOptions.faded))
                if (group.visible) DrawCookieSize();

            DrawRenderMode();
            DrawCullingMask();

            EditorGUILayout.Space();

            if (SceneView.lastActiveSceneView != null && SceneView.lastActiveSceneView.m_SceneLighting == false)
                EditorGUILayout.HelpBox(s_Styles.DisabledLightWarning.text, MessageType.Warning);

            serializedObject.ApplyModifiedProperties();
        }

        void ShadowsGUI()
        {
            // Shadows drop-down. Area lights can only be baked and always have shadows.
            float show = 1.0f - animShowAreaOptions.faded;
            using (new EditorGUILayout.FadeGroupScope(show))
                DrawShadowsType();

            EditorGUI.indentLevel += 1;
            show *= animShowShadowOptions.faded;
            // Baked Shadow radius
            using (var group = new EditorGUILayout.FadeGroupScope(show * animBakedShadowRadiusOptions.faded))
                if (group.visible) DrawBakedShadowRadius();

            // Baked Shadow angle
            using (var group = new EditorGUILayout.FadeGroupScope(show * animBakedShadowAngleOptions.faded))
                if (group.visible) DrawBakedShadowAngle();

            // Runtime shadows - shadow strength, resolution, bias
            using (var group = new EditorGUILayout.FadeGroupScope(show * animShowRuntimeOptions.faded))
                if (group.visible) DrawRuntimeShadow();
            EditorGUI.indentLevel -= 1;

            if (BakingWarningValue)
                EditorGUILayout.HelpBox(s_Styles.BakingWarning.text, MessageType.Warning);

            if (RealtimeShadowsWarningValue)
                EditorGUILayout.HelpBox(s_Styles.ShadowsNotSupportedWarning.text, MessageType.Warning);

            EditorGUILayout.Space();
        }
    }
}
