using UnityEditor.AnimatedValues;
using UnityEngine;
using UnityEngine.Experimental.Rendering.LightweightPipeline;

namespace UnityEditor.Experimental.Rendering.LightweightPipeline
{
    [CanEditMultipleObjects]
    [CustomEditorForRenderPipeline(typeof(Light), typeof(LightweightPipelineAsset))]
    class LightweightLightEditor : LightEditor
    {
        AnimBool m_AnimSpotOptions = new AnimBool();
        AnimBool m_AnimPointOptions = new AnimBool();
        AnimBool m_AnimDirOptions = new AnimBool();
        AnimBool m_AnimAreaOptions = new AnimBool();
        AnimBool m_AnimRuntimeOptions = new AnimBool();
        AnimBool m_AnimShadowOptions = new AnimBool();
        AnimBool m_AnimShadowAngleOptions = new AnimBool();
        AnimBool m_AnimShadowRadiusOptions = new AnimBool();
        AnimBool m_AnimLightBounceIntensity = new AnimBool();

        class Styles
        {
            public readonly GUIContent SpotAngle = new GUIContent("Spot Angle", "Controls the angle in degrees at the base of a Spot light's cone.");

            public readonly GUIContent Cookie = new GUIContent("Cookie", "Specifies the Texture mask to cast shadows, create silhouettes, or patterned illumination for the light.");
            public readonly GUIContent CookieSize = new GUIContent("Cookie Size", "Controls the size of the cookie mask currently assigned to the light.");

            public readonly GUIStyle invisibleButton = "InvisibleButton";

            public readonly GUIContent BakingWarning = new GUIContent("Light mode is currently overridden to Realtime mode. Enable Baked Global Illumination to use Mixed or Baked light modes.");
            public readonly GUIContent CookieWarning = new GUIContent("Cookie textures for spot lights should be set to clamp, not repeat, to avoid artifacts.");
            public readonly GUIContent DisabledLightWarning = new GUIContent("Lighting has been disabled in at least one Scene view. Any changes applied to lights in the Scene will not be updated in these views until Lighting has been enabled again.");

            public readonly GUIContent ShadowsNotSupportedWarning = new GUIContent("Realtime shadows for point lights are not supported. Either disable shadows or set the light mode to Baked.");
        }

        static Styles s_Styles;

        public bool typeIsSame { get { return !settings.lightType.hasMultipleDifferentValues; } }
        public bool shadowTypeIsSame { get { return !settings.shadowsType.hasMultipleDifferentValues; } }
        public bool lightmappingTypeIsSame { get { return !settings.lightmapping.hasMultipleDifferentValues; } }
        public Light lightProperty { get { return target as Light; } }

        public bool spotOptionsValue { get { return typeIsSame && lightProperty.type == LightType.Spot; } }
        public bool pointOptionsValue { get { return typeIsSame && lightProperty.type == LightType.Point; } }
        public bool dirOptionsValue { get { return typeIsSame && lightProperty.type == LightType.Directional; } }
        public bool areaOptionsValue { get { return typeIsSame && lightProperty.type == LightType.Rectangle; } }

        // Point light realtime shadows not supported
        public bool runtimeOptionsValue { get { return typeIsSame && (lightProperty.type != LightType.Rectangle && lightProperty.type != LightType.Point && !settings.isCompletelyBaked); } }
        public bool bakedShadowRadius { get { return typeIsSame && (lightProperty.type == LightType.Point || lightProperty.type == LightType.Spot) && settings.isBakedOrMixed; } }
        public bool bakedShadowAngle { get { return typeIsSame && lightProperty.type == LightType.Directional && settings.isBakedOrMixed; } }
        public bool shadowOptionsValue { get { return shadowTypeIsSame && lightProperty.shadows != LightShadows.None; } }

        public bool bakingWarningValue { get { return !UnityEditor.Lightmapping.bakedGI && lightmappingTypeIsSame && settings.isBakedOrMixed; } }
        public bool showLightBounceIntensity { get { return true; } }
        public bool cookieWarningValue
        {
            get
            {
                return typeIsSame && lightProperty.type == LightType.Spot &&
                    !settings.cookieProp.hasMultipleDifferentValues && settings.cookie && settings.cookie.wrapMode != TextureWrapMode.Clamp;
            }
        }

        public bool isShadowEnabled { get { return settings.shadowsType.intValue != 0; } }

        public bool realtimeShadowsWarningValue
        {
            get
            {
                return typeIsSame && lightProperty.type == LightType.Point &&
                    shadowTypeIsSame && isShadowEnabled &&
                    lightmappingTypeIsSame && !settings.isCompletelyBaked;
            }
        }

        protected override void OnEnable()
        {
            settings.OnEnable();
            UpdateShowOptions(true);
        }

        public override void OnInspectorGUI()
        {
            if (s_Styles == null)
                s_Styles = new Styles();

            settings.Update();

            // Update AnimBool options. For properties changed they will be smoothly interpolated.
            UpdateShowOptions(false);

            settings.DrawLightType();

            EditorGUILayout.Space();

            // When we are switching between two light types that don't show the range (directional and area lights)
            // we want the fade group to stay hidden.
            using (var group = new EditorGUILayout.FadeGroupScope(1.0f - m_AnimDirOptions.faded))
                if (group.visible)
                    settings.DrawRange(m_AnimAreaOptions.target);

            // Spot angle
            using (var group = new EditorGUILayout.FadeGroupScope(m_AnimSpotOptions.faded))
                if (group.visible)
                    DrawSpotAngle();

            // Area width & height
            using (var group = new EditorGUILayout.FadeGroupScope(m_AnimAreaOptions.faded))
                if (group.visible)
                    settings.DrawArea();

            settings.DrawColor();

            EditorGUILayout.Space();

            using (var group = new EditorGUILayout.FadeGroupScope(1.0f - m_AnimAreaOptions.faded))
                if (group.visible)
                    settings.DrawLightmapping();

            settings.DrawIntensity();

            using (var group = new EditorGUILayout.FadeGroupScope(m_AnimLightBounceIntensity.faded))
                if (group.visible)
                    settings.DrawBounceIntensity();

            ShadowsGUI();

            /* Tim: Disable cookie for v1 to save on shader combinations
            using (var group = new EditorGUILayout.FadeGroupScope(animShowRuntimeOptions.faded))
                if (group.visible)
                    DrawCookie();

            // Cookie size also requires directional light
            using (var group = new EditorGUILayout.FadeGroupScope(animShowRuntimeOptions.faded * animShowDirOptions.faded))
                if (group.visible)
                    DrawCookieSize();
           */

            settings.DrawRenderMode();
            settings.DrawCullingMask();

            EditorGUILayout.Space();

            if (SceneView.lastActiveSceneView != null && SceneView.lastActiveSceneView.m_SceneLighting == false)
                EditorGUILayout.HelpBox(s_Styles.DisabledLightWarning.text, MessageType.Warning);

            serializedObject.ApplyModifiedProperties();
        }

        void SetOptions(AnimBool animBool, bool initialize, bool targetValue)
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

        void UpdateShowOptions(bool initialize)
        {
            SetOptions(m_AnimSpotOptions, initialize, spotOptionsValue);
            SetOptions(m_AnimPointOptions, initialize, pointOptionsValue);
            SetOptions(m_AnimDirOptions, initialize, dirOptionsValue);
            SetOptions(m_AnimAreaOptions, initialize, areaOptionsValue);
            SetOptions(m_AnimShadowOptions, initialize, shadowOptionsValue);
            SetOptions(m_AnimRuntimeOptions, initialize, runtimeOptionsValue);
            SetOptions(m_AnimShadowAngleOptions, initialize, bakedShadowAngle);
            SetOptions(m_AnimShadowRadiusOptions, initialize, bakedShadowRadius);
            SetOptions(m_AnimLightBounceIntensity, initialize, showLightBounceIntensity);
        }

        void DrawSpotAngle()
        {
            EditorGUILayout.Slider(settings.spotAngle, 1f, 179f, s_Styles.SpotAngle);
        }

        void DrawCookie()
        {
            EditorGUILayout.PropertyField(settings.cookieProp, s_Styles.Cookie);

            if (cookieWarningValue)
            {
                // warn on spotlights if the cookie is set to repeat
                EditorGUILayout.HelpBox(s_Styles.CookieWarning.text, MessageType.Warning);
            }
        }

        void DrawCookieSize()
        {
            EditorGUILayout.PropertyField(settings.cookieSize, s_Styles.CookieSize);
        }

        void ShadowsGUI()
        {
            // Shadows drop-down. Area lights can only be baked and always have shadows.
            float show = 1.0f - m_AnimAreaOptions.faded;
            using (new EditorGUILayout.FadeGroupScope(show))
                settings.DrawShadowsType();

            EditorGUI.indentLevel += 1;
            show *= m_AnimShadowOptions.faded;
            // Baked Shadow radius
            using (var group = new EditorGUILayout.FadeGroupScope(show * m_AnimShadowRadiusOptions.faded))
                if (group.visible)
                    settings.DrawBakedShadowRadius();

            // Baked Shadow angle
            using (var group = new EditorGUILayout.FadeGroupScope(show * m_AnimShadowAngleOptions.faded))
                if (group.visible)
                    settings.DrawBakedShadowAngle();

            // Runtime shadows - shadow strength, resolution, bias
            using (var group = new EditorGUILayout.FadeGroupScope(show * m_AnimRuntimeOptions.faded))
                if (group.visible)
                    settings.DrawRuntimeShadow();
            EditorGUI.indentLevel -= 1;

            if (bakingWarningValue)
                EditorGUILayout.HelpBox(s_Styles.BakingWarning.text, MessageType.Warning);

            if (realtimeShadowsWarningValue)
                EditorGUILayout.HelpBox(s_Styles.ShadowsNotSupportedWarning.text, MessageType.Warning);

            EditorGUILayout.Space();
        }
    }
}
