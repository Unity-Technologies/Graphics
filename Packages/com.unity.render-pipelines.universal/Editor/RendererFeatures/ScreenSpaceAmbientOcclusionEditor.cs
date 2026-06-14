using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace UnityEditor.Rendering.Universal
{
    [CustomEditor(typeof(ScreenSpaceAmbientOcclusion))]
    internal class ScreenSpaceAmbientOcclusionEditor : Editor, IOwningRendererDataConsumer
    {
        #region Serialized Properties
        private SerializedProperty m_AOMethod;
        private SerializedProperty m_Downsample;
        private SerializedProperty m_AfterOpaque;
        private SerializedProperty m_Source;
        private SerializedProperty m_NormalQuality;
        private SerializedProperty m_Intensity;
        private SerializedProperty m_DirectLightingStrength;
        private SerializedProperty m_Radius;
        private SerializedProperty m_Falloff;
        private SerializedProperty m_Samples;
        private SerializedProperty m_BlurQuality;
        #endregion

        private bool m_IsInitialized = false;
        private HeaderBool m_ShowQualitySettings;
        private HeaderBool m_ShowDeprecatedSettings;
        private bool m_ShowAfterOpaqueTileOnlyError;

        private static readonly string k_AfterOpaqueIncompatibleWithTileOnlyMode = L10n.Tr("'After Opaque' is incompatible with the enabled 'Tile-Only Mode'. Disable After Opaque.");

        /// <summary>
        /// The renderer data that owns the feature when the inspector is drawn.
        /// </summary>
        public ScriptableRendererData owningRendererData { get; set; }

        class HeaderBool
        {
            private string key;
            public bool value;

            internal HeaderBool(string _key, bool _default = false)
            {
                key = _key;
                if (EditorPrefs.HasKey(key))
                    value = EditorPrefs.GetBool(key);
                else
                    value = _default;
                EditorPrefs.SetBool(key, value);
            }

            internal void SetValue(bool newValue)
            {
                value = newValue;
                EditorPrefs.SetBool(key, value);
            }
        }


        // Structs
        private struct Styles
        {
            public static GUIContent AOMethod = EditorGUIUtility.TrTextContent("Method", "The noise method to use when calculating the Ambient Occlusion value.");
            public static GUIContent Intensity = EditorGUIUtility.TrTextContent("Intensity", "The degree of darkness that Ambient Occlusion adds.");
            public static GUIContent Radius = EditorGUIUtility.TrTextContent("Radius", "The radius around a given point, where Unity calculates and applies the effect.");
            public static GUIContent Falloff = EditorGUIUtility.TrTextContent("Falloff Distance", "The distance from the camera where Ambient Occlusion should be visible.");
            public static GUIContent DirectLightingStrength = EditorGUIUtility.TrTextContent("Direct Lighting Strength", "Controls how much the ambient occlusion affects direct lighting.");

            public static GUIContent Quality = EditorGUIUtility.TrTextContent("Quality", "");
            public static GUIContent Source = EditorGUIUtility.TrTextContent("Source", "The source of the normal vector values.\nDepth Normals: the feature uses the values generated in the Depth Normal prepass.\nDepth: the feature reconstructs the normal values using the depth buffer.\nIn the Deferred rendering path, the feature uses the G-buffer normals texture.");
            public static GUIContent NormalQuality = new GUIContent("Normal Quality", "The number of depth texture samples that Unity takes when computing the normals. Low:1 sample, Medium: 5 samples, High: 9 samples.");
            public static GUIContent Downsample = EditorGUIUtility.TrTextContent("Downsample", "With this option enabled, Unity downsamples the SSAO effect texture to improve performance. Each dimension of the texture is reduced by a factor of 2.");
            public static GUIContent AfterOpaque = EditorGUIUtility.TrTextContent("After Opaque", "With this option enabled, Unity calculates and apply SSAO after the opaque pass to improve performance on mobile platforms with tiled-based GPU architectures. This is not physically correct.");
            public static GUIContent BlurQuality = EditorGUIUtility.TrTextContent("Blur Quality", "High: Bilateral, Medium: Gaussian. Low: Kawase (Single Pass).");
            public static GUIContent Samples = EditorGUIUtility.TrTextContent("Samples", "The number of samples that Unity takes when calculating the obscurance value. Low:4 samples, Medium: 8 samples, High: 12 samples.");
        }

        private void Init()
        {
            m_ShowQualitySettings = new HeaderBool($"SSAO.QualityFoldout", false);
            m_ShowDeprecatedSettings = new HeaderBool("SSAO.DeprecatedFoldout", false);

            SerializedProperty settings = serializedObject.FindProperty("m_Settings");

            m_AOMethod = settings.FindPropertyRelative("AOMethod");
            m_Intensity = settings.FindPropertyRelative("Intensity");
            m_Radius = settings.FindPropertyRelative("Radius");
            m_Falloff = settings.FindPropertyRelative("Falloff");
            m_DirectLightingStrength = settings.FindPropertyRelative("DirectLightingStrength");

            m_Source = settings.FindPropertyRelative("Source");
            m_NormalQuality = settings.FindPropertyRelative("NormalSamples");
            m_Downsample = settings.FindPropertyRelative("Downsample");
            m_AfterOpaque = settings.FindPropertyRelative("AfterOpaque");
            m_BlurQuality = settings.FindPropertyRelative("BlurQuality");
            m_Samples = settings.FindPropertyRelative("Samples");

            m_IsInitialized = true;
        }

        public override void OnInspectorGUI()
        {
            if (!m_IsInitialized)
                Init();

#if MODERN_SSAO
            bool volumeActive = SceneHasActiveSSAOVolumeOverride();
            if (volumeActive)
            {
                EditorGUILayout.HelpBox(
                    "A Screen Space Ambient Occlusion Volume Override is active in the scene and controls all SSAO settings. " +
                    "The settings below are kept as fallback only and will not take effect while the Volume Override is present.",
                    MessageType.Info);
            }
            else
            {
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    EditorGUILayout.LabelField(
                        new GUIContent(
                            "Configuring SSAO via the SSAO Renderer Feature is deprecated. Use the new Screen Space Ambient Occlusion Volume Override instead.",
                            EditorGUIUtility.GetHelpIcon(MessageType.Info)),
                        EditorStyles.wordWrappedLabel);

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button("Add Volume Override"))
                            MigrateSettingsToVolume();
                    }
                }
            }

            EditorGUILayout.Space(5);
            m_ShowDeprecatedSettings.SetValue(EditorGUILayout.Foldout(m_ShowDeprecatedSettings.value, "Settings (Deprecated)"));
            EditorGUI.BeginDisabledGroup(volumeActive);
            if (m_ShowDeprecatedSettings.value)
            {
                EditorGUI.indentLevel++;
                DrawSsaoSettingsGUI();
                EditorGUI.indentLevel--;
            }
            EditorGUI.EndDisabledGroup();
#else
            DrawSsaoSettingsGUI();
#endif
        }

        void DrawSsaoSettingsGUI()
        {
            EditorGUILayout.PropertyField(m_AOMethod, Styles.AOMethod);
            EditorGUILayout.PropertyField(m_Intensity, Styles.Intensity);
            EditorGUILayout.PropertyField(m_Radius, Styles.Radius);
            EditorGUILayout.PropertyField(m_Falloff, Styles.Falloff);
            m_DirectLightingStrength.floatValue = EditorGUILayout.Slider(Styles.DirectLightingStrength, m_DirectLightingStrength.floatValue, 0f, 1f);

            // Make sure these fields are never below 0.0...
            m_Intensity.floatValue = Mathf.Max(m_Intensity.floatValue, 0f);
            m_Radius.floatValue = Mathf.Max(m_Radius.floatValue, 0f);
            m_Falloff.floatValue = Mathf.Max(m_Falloff.floatValue, 0f);

            m_ShowQualitySettings.SetValue(EditorGUILayout.Foldout(m_ShowQualitySettings.value, Styles.Quality));
            if (m_ShowQualitySettings.value)
            {
                bool isDeferredRenderingMode = RendererIsDeferred();

                EditorGUI.indentLevel++;

                // Selecting source is not available for Deferred Rendering...
                using (new EditorGUI.DisabledScope(isDeferredRenderingMode))
                    EditorGUILayout.PropertyField(m_Source, Styles.Source);

                // We only enable this field when depth source is selected...
                using (new EditorGUI.DisabledScope(isDeferredRenderingMode || m_Source.enumValueIndex != (int)ScreenSpaceAmbientOcclusionSettings.DepthSource.Depth))
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(m_NormalQuality, Styles.NormalQuality);
                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.PropertyField(m_Downsample, Styles.Downsample);
                EditorGUILayout.PropertyField(m_AfterOpaque, Styles.AfterOpaque);

                if (Event.current.type == EventType.Layout)
                {
                    var rendererData = (this as IOwningRendererDataConsumer).owningRendererData as UniversalRendererData;
                    bool tileOnlyMode = rendererData != null && rendererData.tileOnlyMode;
                    bool afterOpaque = m_AfterOpaque.boolValue;
                    m_ShowAfterOpaqueTileOnlyError = tileOnlyMode && afterOpaque;
                }
                if (m_ShowAfterOpaqueTileOnlyError)
                    EditorGUILayout.HelpBox(k_AfterOpaqueIncompatibleWithTileOnlyMode, MessageType.Error, true);

                EditorGUILayout.PropertyField(m_BlurQuality, Styles.BlurQuality);
                EditorGUILayout.PropertyField(m_Samples, Styles.Samples);

                EditorGUI.indentLevel--;
            }
        }

#if MODERN_SSAO
        static bool SceneHasActiveSSAOVolumeOverride()
        {
            var stack = VolumeManager.instance.stack;
            if (stack == null)
                return false;

            var ssaoVolume = stack.GetComponent<ScreenSpaceAmbientOcclusionVolumeOverride>();
            return ssaoVolume != null && ssaoVolume.AnyPropertiesIsOverridden();
        }

        static void CreateSSAOGlobalVolume(out Volume volume, out ScreenSpaceAmbientOcclusionVolumeOverride ssaoOverride)
        {
            var go = new GameObject("SSAO Global Volume");
            Undo.RegisterCreatedObjectUndo(go, "Create SSAO Global Volume");
            volume = go.AddComponent<Volume>();
            volume.isGlobal = true;

            Scene scene = go.scene;
            var profile = VolumeProfileFactory.CreateVolumeProfile(scene, "SSAO Volume Profile");

            volume.sharedProfile = profile;
            // AnyPropertiesIsOverridden() must return true for the runtime volume takeover path to activate.
            ssaoOverride = profile.Add<ScreenSpaceAmbientOcclusionVolumeOverride>(overrides: true);
            AssetDatabase.AddObjectToAsset(ssaoOverride, profile);
        }

        void MigrateSettingsToVolume()
        {
            if (SceneHasActiveSSAOVolumeOverride())
            {
                bool proceed = EditorUtility.DisplayDialog(
                    "SSAO Volume Already Exists",
                    "An SSAO Volume Override already exists in the scene. " +
                    "A new global volume will be created which may conflict with the existing one. Continue?",
                    "Create Anyway", "Cancel");
                if (!proceed)
                    return;
            }

            CreateSSAOGlobalVolume(out var volume, out var ssaoOverride);

            Undo.RecordObject(ssaoOverride, "Migrate SSAO Settings to Volume");

            var feature = (ScreenSpaceAmbientOcclusion)target;
#pragma warning disable CS0618
            ref var settings = ref feature.settings;
#pragma warning restore CS0618

            ssaoOverride.mode = ScreenSpaceAmbientOcclusionMode.Standard;
            ssaoOverride.quality = ScreenSpaceAmbientOcclusionQuality.Custom;
            ssaoOverride.intensity = settings.Intensity;
            ssaoOverride.radius = settings.Radius;
            ssaoOverride.falloffDistance = settings.Falloff;
            ssaoOverride.directLightingStrength = settings.DirectLightingStrength;
            ssaoOverride.downsample = settings.Downsample;
            ssaoOverride.afterOpaque = settings.AfterOpaque;

            ssaoOverride.method = settings.AOMethod == ScreenSpaceAmbientOcclusionSettings.AOMethodOptions.BlueNoise
                ? ScreenSpaceAmbientOcclusionNoiseMethod.BlueNoise
                : ScreenSpaceAmbientOcclusionNoiseMethod.InterleavedGradient;

            ssaoOverride.depthSource = (ScreenSpaceAmbientOcclusionDepthSource)(int)settings.Source;
            ssaoOverride.normalQuality = (ScreenSpaceAmbientOcclusionNormalQuality)(int)settings.NormalSamples;
            ssaoOverride.blurQuality = settings.BlurQuality switch
            {
                ScreenSpaceAmbientOcclusionSettings.BlurQualityOptions.High => ScreenSpaceAmbientOcclusionBlurQuality.High,
                ScreenSpaceAmbientOcclusionSettings.BlurQualityOptions.Medium => ScreenSpaceAmbientOcclusionBlurQuality.Medium,
                _ => ScreenSpaceAmbientOcclusionBlurQuality.Low
            };

            ssaoOverride.sampleCount = settings.Samples switch
            {
                ScreenSpaceAmbientOcclusionSettings.AOSampleOption.High => ScreenSpaceAmbientOcclusionSampleCount.High,
                ScreenSpaceAmbientOcclusionSettings.AOSampleOption.Medium => ScreenSpaceAmbientOcclusionSampleCount.Medium,
                _ => ScreenSpaceAmbientOcclusionSampleCount.Low
            };

            EditorUtility.SetDirty(ssaoOverride);
            EditorUtility.SetDirty(volume.sharedProfile);
            AssetDatabase.SaveAssets();

            string profilePath = AssetDatabase.GetAssetPath(volume.sharedProfile);

            var volumeGo = volume.gameObject;
            EditorApplication.delayCall += () =>
            {
                if (volumeGo == null)
                    return;
                Selection.activeGameObject = volumeGo;
                EditorGUIUtility.PingObject(volumeGo);
            };
        }
#endif

        private bool RendererIsDeferred()
        {
            ScreenSpaceAmbientOcclusion ssaoFeature = (ScreenSpaceAmbientOcclusion) target;
            UniversalRenderPipelineAsset pipelineAsset = (UniversalRenderPipelineAsset) GraphicsSettings.currentRenderPipeline;

            if (ssaoFeature == null || pipelineAsset == null)
                return false;

            // We have to find the renderer related to the SSAO feature, then test if it is in deferred mode.
            var rendererDataList = pipelineAsset.m_RendererDataList;
            for (int rendererIndex = 0; rendererIndex < rendererDataList.Length; ++rendererIndex)
            {
                var rendererData = rendererDataList[rendererIndex] as UniversalRendererData;
                if (rendererData == null)
                    continue;

                if (!rendererData.usesDeferredLighting)
                    continue;

                var rendererFeatures = rendererData.rendererFeatures;
                foreach (var feature in rendererFeatures)
                    if (feature is ScreenSpaceAmbientOcclusion occlusion && occlusion == ssaoFeature)
                        return true;
            }

            return false;
        }
    }
}
