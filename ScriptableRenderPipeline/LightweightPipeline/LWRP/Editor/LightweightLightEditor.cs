using UnityEditor.AnimatedValues;
using UnityEngine;
using UnityEngine.Experimental.Rendering.LightweightPipeline;

namespace UnityEditor.Experimental.Rendering.LightweightPipeline
{
    [CanEditMultipleObjects]
    [CustomEditorForRenderPipeline(typeof(Light), typeof(LightweightPipelineAsset))]
    class LightweightLightEditor : LightEditor
    {
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

        private bool TypeIsSame { get { return !settings.lightType.hasMultipleDifferentValues; } }
        private bool ShadowTypeIsSame { get { return !settings.shadowsType.hasMultipleDifferentValues; } }
        private bool LightmappingTypeIsSame { get { return !settings.lightmapping.hasMultipleDifferentValues; } }
        private Light LightProperty { get { return target as Light; } }

        private bool SpotOptionsValue { get { return TypeIsSame && LightProperty.type == LightType.Spot; } }
        private bool PointOptionsValue { get { return TypeIsSame && LightProperty.type == LightType.Point; } }
        private bool DirOptionsValue { get { return TypeIsSame && LightProperty.type == LightType.Directional; } }
        private bool AreaOptionsValue { get { return TypeIsSame && LightProperty.type == LightType.Area; } }

        // Point light realtime shadows not supported
        private bool RuntimeOptionsValue { get { return TypeIsSame && (LightProperty.type != LightType.Area && LightProperty.type != LightType.Point && !settings.isCompletelyBaked); } }
        private bool BakedShadowRadius { get { return TypeIsSame && (LightProperty.type == LightType.Point || LightProperty.type == LightType.Spot) && settings.isBakedOrMixed; } }
        private bool BakedShadowAngle { get { return TypeIsSame && LightProperty.type == LightType.Directional && settings.isBakedOrMixed; } }
        private bool ShadowOptionsValue { get { return ShadowTypeIsSame && LightProperty.shadows != LightShadows.None; } }

        private bool BakingWarningValue { get { return !UnityEditor.Lightmapping.bakedGI && LightmappingTypeIsSame && settings.isBakedOrMixed; } }
        private bool ShowLightBounceIntensity { get { return true; } }
        private bool CookieWarningValue
        {
            get
            {
                return TypeIsSame && LightProperty.type == LightType.Spot &&
                    !settings.cookieProp.hasMultipleDifferentValues && settings.cookie && settings.cookie.wrapMode != TextureWrapMode.Clamp;
            }
        }

        private bool IsShadowEnabled { get { return settings.shadowsType.intValue != 0; } }

        private bool RealtimeShadowsWarningValue
        {
            get
            {
                return TypeIsSame && LightProperty.type == LightType.Point &&
                    ShadowTypeIsSame && IsShadowEnabled &&
                    LightmappingTypeIsSame && !settings.isCompletelyBaked;
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

        protected override void OnEnable()
        {
            settings.OnEnable();
            UpdateShowOptions(true);
        }

        public void DrawSpotAngle()
        {
            EditorGUILayout.Slider(settings.spotAngle, 1f, 179f, s_Styles.SpotAngle);
        }

        public void DrawCookie()
        {
            EditorGUILayout.PropertyField(settings.cookieProp, s_Styles.Cookie);

            if (CookieWarningValue)
            {
                // warn on spotlights if the cookie is set to repeat
                EditorGUILayout.HelpBox(s_Styles.CookieWarning.text, MessageType.Warning);
            }
        }

        public void DrawCookieSize()
        {
            EditorGUILayout.PropertyField(settings.cookieSize, s_Styles.CookieSize);
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
            using (var group = new EditorGUILayout.FadeGroupScope(1.0f - animShowDirOptions.faded))
                if (group.visible)
                    settings.DrawRange(animShowAreaOptions.target);

            // Spot angle
            using (var group = new EditorGUILayout.FadeGroupScope(animShowSpotOptions.faded))
                if (group.visible)
                    DrawSpotAngle();

            // Area width & height
            using (var group = new EditorGUILayout.FadeGroupScope(animShowAreaOptions.faded))
                if (group.visible)
                    settings.DrawArea();

            settings.DrawColor();

            EditorGUILayout.Space();

            using (var group = new EditorGUILayout.FadeGroupScope(1.0f - animShowAreaOptions.faded))
                if (group.visible)
                    settings.DrawLightmapping();

            settings.DrawIntensity();

            using (var group = new EditorGUILayout.FadeGroupScope(animShowLightBounceIntensity.faded))
                if (group.visible)
                    settings.DrawBounceIntensity();

            ShadowsGUI();

            using (var group = new EditorGUILayout.FadeGroupScope(animShowRuntimeOptions.faded))
                if (group.visible)
                    DrawCookie();

            // Cookie size also requires directional light
            using (var group = new EditorGUILayout.FadeGroupScope(animShowRuntimeOptions.faded * animShowDirOptions.faded))
                if (group.visible)
                    DrawCookieSize();

            settings.DrawRenderMode();
            settings.DrawCullingMask();

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
                settings.DrawShadowsType();

            EditorGUI.indentLevel += 1;
            show *= animShowShadowOptions.faded;
            // Baked Shadow radius
            using (var group = new EditorGUILayout.FadeGroupScope(show * animBakedShadowRadiusOptions.faded))
                if (group.visible)
                    settings.DrawBakedShadowRadius();

            // Baked Shadow angle
            using (var group = new EditorGUILayout.FadeGroupScope(show * animBakedShadowAngleOptions.faded))
                if (group.visible)
                    settings.DrawBakedShadowAngle();

            // Runtime shadows - shadow strength, resolution, bias
            using (var group = new EditorGUILayout.FadeGroupScope(show * animShowRuntimeOptions.faded))
                if (group.visible)
                    settings.DrawRuntimeShadow();
            EditorGUI.indentLevel -= 1;

            if (BakingWarningValue)
                EditorGUILayout.HelpBox(s_Styles.BakingWarning.text, MessageType.Warning);

            if (RealtimeShadowsWarningValue)
                EditorGUILayout.HelpBox(s_Styles.ShadowsNotSupportedWarning.text, MessageType.Warning);

            EditorGUILayout.Space();
        }
    }
}
