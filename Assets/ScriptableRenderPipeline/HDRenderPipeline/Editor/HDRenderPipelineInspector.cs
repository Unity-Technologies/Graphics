using System;
using System.Reflection;
using System.Linq.Expressions;
using UnityEditor;

//using EditorGUIUtility=UnityEditor.EditorGUIUtility;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [CustomEditor(typeof(HDRenderPipeline))]
    public class HDRenderPipelineInspector : Editor
    {
        private class Styles
        {
            public readonly GUIContent settingsLabel = new GUIContent("Settings");

            // Rendering Settings
            public readonly GUIContent renderingSettingsLabel = new GUIContent("Rendering Settings");
            public readonly GUIContent useForwardRenderingOnly = new GUIContent("Use Forward Rendering Only");
            public readonly GUIContent useDepthPrepass = new GUIContent("Use Depth Prepass");

            // Texture Settings
            public readonly GUIContent textureSettings = new GUIContent("Texture Settings");
            public readonly GUIContent spotCookieSize = new GUIContent("Spot cookie size");
            public readonly GUIContent pointCookieSize = new GUIContent("Point cookie size");
            public readonly GUIContent reflectionCubemapSize = new GUIContent("Reflection cubemap size");

            public readonly GUIContent sssSettings = new GUIContent("Subsurface Scattering Settings");

            // Shadow Settings
            public readonly GUIContent shadowSettings = new GUIContent("Shadow Settings");
            public readonly GUIContent shadowsAtlasWidth = new GUIContent("Atlas width");
            public readonly GUIContent shadowsAtlasHeight = new GUIContent("Atlas height");

            // Subsurface Scaterring Settings
            public readonly GUIContent[] sssProfiles = new GUIContent[SubsurfaceScatteringSettings.maxNumProfiles] { new GUIContent("Profile #0"), new GUIContent("Profile #1"), new GUIContent("Profile #2"), new GUIContent("Profile #3"), new GUIContent("Profile #4"), new GUIContent("Profile #5"), new GUIContent("Profile #6"), new GUIContent("Profile #7") };
            public readonly GUIContent sssProfilePreview0 = new GUIContent("Profile preview");
            public readonly GUIContent sssProfilePreview1 = new GUIContent("Shows the fraction of light scattered from the source as radius increases to 1.");
            public readonly GUIContent sssProfilePreview2 = new GUIContent("Note that the intensity of the region in the center may be clamped.");
            public readonly GUIContent sssTransmittancePreview0 = new GUIContent("Transmittance preview");
            public readonly GUIContent sssTransmittancePreview1 = new GUIContent("Shows the fraction of light passing through the object as thickness increases to 1.");
            public readonly GUIContent sssNumProfiles = new GUIContent("Number of profiles");
            public readonly GUIContent sssProfileStdDev1 = new GUIContent("Standard deviation #1", "Determines the shape of the 1st Gaussian filter. Increases the strength and the radius of the blur of the corresponding color channel.");
            public readonly GUIContent sssProfileStdDev2 = new GUIContent("Standard deviation #2", "Determines the shape of the 2nd Gaussian filter. Increases the strength and the radius of the blur of the corresponding color channel.");
            public readonly GUIContent sssProfileLerpWeight = new GUIContent("Filter interpolation", "Controls linear interpolation between the two Gaussian filters.");
            public readonly GUIContent sssProfileTransmission = new GUIContent("Enable transmission", "Toggles simulation of light passing through thin objects. Depends on the thickness of the material.");
            public readonly GUIContent sssProfileThicknessRemap = new GUIContent("Thickness remap", "Remaps the thickness parameter from [0, 1] to the desired range.");
            public readonly GUIContent sssTexturingMode = new GUIContent("Texturing mode", "Specifies when the diffuse texture should be applied.");
            public readonly GUIContent[] sssTexturingModeOptions = new GUIContent[3] { new GUIContent("Pre-scatter", "Before the blurring pass. Effectively results in the diffuse texture getting blurred together with the lighting."), new GUIContent("Post-scatter", "After the blurring pass. Effectively preserves the sharpness of the diffuse texture."), new GUIContent("Pre- and post-scatter", "Both before and after the blurring pass.") };

            public readonly GUIStyle centeredMiniBoldLabel = new GUIStyle(GUI.skin.label);

            // Tile pass Settings
            public readonly GUIContent tileLightLoopSettings = new GUIContent("Tile Light Loop Settings");
            public readonly string[] tileLightLoopDebugTileFlagStrings = new string[] { "Punctual Light", "Area Light", "Env Light"};
            public readonly GUIContent splitLightEvaluation = new GUIContent("Split light and reflection evaluation", "Toggle");
            public readonly GUIContent bigTilePrepass = new GUIContent("Enable big tile prepass", "Toggle");
            public readonly GUIContent clustered = new GUIContent("Enable clustered", "Toggle");
            public readonly GUIContent enableTileAndCluster = new GUIContent("Enable Tile/clustered", "Toggle");
            public readonly GUIContent enableComputeLightEvaluation = new GUIContent("Enable Compute Light Evaluation", "Toggle");

            // Sky Settings
            public readonly GUIContent skyParams = new GUIContent("Sky Settings");

            // Global debug Settings
            public readonly GUIContent debugging = new GUIContent("Debugging");
            public readonly GUIContent debugOverlayRatio = new GUIContent("Overlay Ratio");

            // Material debug
            public readonly GUIContent materialDebugLabel = new GUIContent("Material Debug");
            public readonly GUIContent debugViewMaterial = new GUIContent("DebugView Material", "Display various properties of Materials.");
            public bool isDebugViewMaterialInit = false;
            public GUIContent[] debugViewMaterialStrings = null;
            public int[] debugViewMaterialValues = null;

            // Rendering Debug
            public readonly GUIContent renderingDebugSettings = new GUIContent("Rendering Debug");
            public readonly GUIContent displayOpaqueObjects = new GUIContent("Display Opaque Objects", "Toggle opaque objects rendering on and off.");
            public readonly GUIContent displayTransparentObjects = new GUIContent("Display Transparent Objects", "Toggle transparent objects rendering on and off.");
            public readonly GUIContent enableDistortion = new GUIContent("Enable Distortion");
            public readonly GUIContent enableSSS = new GUIContent("Enable Subsurface Scattering");

            // Lighting Debug
            public readonly GUIContent lightingDebugSettings = new GUIContent("Lighting Debug");
            public readonly GUIContent shadowDebugEnable = new GUIContent("Enable Shadows");
            public readonly GUIContent shadowDebugVisualizationMode = new GUIContent("Shadow Maps Debug Mode");
            public readonly GUIContent shadowDebugVisualizeShadowIndex = new GUIContent("Visualize Shadow Index");
            public readonly GUIContent lightingDebugMode = new GUIContent("Lighting Debug Mode");
            public readonly GUIContent lightingDebugOverrideSmoothness = new GUIContent("Override Smoothness");
            public readonly GUIContent lightingDebugOverrideSmoothnessValue = new GUIContent("Smoothness Value");
            public readonly GUIContent lightingDebugAlbedo = new GUIContent("Lighting Debug Albedo");
            public readonly GUIContent lightingDisplaySkyReflection = new GUIContent("Display Sky Reflection");
            public readonly GUIContent lightingDisplaySkyReflectionMipmap = new GUIContent("Reflection Mipmap");

            public Styles()
            {
                centeredMiniBoldLabel.alignment = TextAnchor.MiddleCenter;
                centeredMiniBoldLabel.fontSize = 10;
                centeredMiniBoldLabel.fontStyle = FontStyle.Bold;
            }
        }

        private static Styles s_Styles = null;

        private static Styles styles
        {
            get
            {
                if (s_Styles == null)
                    s_Styles = new Styles();
                return s_Styles;
            }
        }

        // Global debug
        SerializedProperty m_ShowMaterialDebug = null;
        SerializedProperty m_ShowLightingDebug = null;
        SerializedProperty m_ShowRenderingDebug = null;
        SerializedProperty m_DebugOverlayRatio = null;

        // Material Debug
        SerializedProperty m_MaterialDebugMode = null;

        // Rendering Debug
        SerializedProperty m_DisplayOpaqueObjects = null;
        SerializedProperty m_DisplayTransparentObjects = null;
        SerializedProperty m_EnableDistortion = null;
        SerializedProperty m_EnableSSS = null;

        // Lighting debug
        SerializedProperty m_DebugShadowEnabled = null;
        SerializedProperty m_ShadowDebugMode = null;
        SerializedProperty m_ShadowDebugShadowMapIndex = null;
        SerializedProperty m_LightingDebugMode = null;
        SerializedProperty m_LightingDebugOverrideSmoothness = null;
        SerializedProperty m_LightingDebugOverrideSmoothnessValue = null;
        SerializedProperty m_LightingDebugAlbedo = null;
        SerializedProperty m_LightingDebugDisplaySkyReflection = null;
        SerializedProperty m_LightingDebugDisplaySkyReflectionMipmap = null;

        // Rendering Settings
        SerializedProperty m_RenderingUseForwardOnly = null;
        SerializedProperty m_RenderingUseDepthPrepass = null;

        // Subsurface Scattering Settings
        SerializedProperty m_TexturingMode = null;
        SerializedProperty m_Profiles = null;
        SerializedProperty m_NumProfiles = null;

        // Subsurface Scattering internal data
        private Material m_ProfileMaterial, m_TransmittanceMaterial;
        private RenderTexture[] m_ProfileImages, m_TransmittanceImages;

        private void InitializeProperties()
        {
            // Global debug
            m_DebugOverlayRatio = FindProperty(x => x.globalDebugSettings.debugOverlayRatio);
            m_ShowLightingDebug = FindProperty(x => x.globalDebugSettings.displayLightingDebug);
            m_ShowRenderingDebug = FindProperty(x => x.globalDebugSettings.displayRenderingDebug);
            m_ShowMaterialDebug = FindProperty(x => x.globalDebugSettings.displayMaterialDebug);

            // Material debug
            m_MaterialDebugMode = FindProperty(x => x.globalDebugSettings.materialDebugSettings.debugViewMaterial);

            // Rendering debug
            m_DisplayOpaqueObjects = FindProperty(x => x.globalDebugSettings.renderingDebugSettings.displayOpaqueObjects);
            m_DisplayTransparentObjects = FindProperty(x => x.globalDebugSettings.renderingDebugSettings.displayTransparentObjects);
            m_EnableDistortion = FindProperty(x => x.globalDebugSettings.renderingDebugSettings.enableDistortion);
            m_EnableSSS = FindProperty(x => x.globalDebugSettings.renderingDebugSettings.enableSSS);

            // Lighting debug
            m_DebugShadowEnabled = FindProperty(x => x.globalDebugSettings.lightingDebugSettings.enableShadows);
            m_ShadowDebugMode = FindProperty(x => x.globalDebugSettings.lightingDebugSettings.shadowDebugMode);
            m_ShadowDebugShadowMapIndex = FindProperty(x => x.globalDebugSettings.lightingDebugSettings.shadowMapIndex);
            m_LightingDebugMode = FindProperty(x => x.globalDebugSettings.lightingDebugSettings.lightingDebugMode);
            m_LightingDebugOverrideSmoothness = FindProperty(x => x.globalDebugSettings.lightingDebugSettings.overrideSmoothness);
            m_LightingDebugOverrideSmoothnessValue = FindProperty(x => x.globalDebugSettings.lightingDebugSettings.overrideSmoothnessValue);
            m_LightingDebugAlbedo = FindProperty(x => x.globalDebugSettings.lightingDebugSettings.debugLightingAlbedo);
            m_LightingDebugDisplaySkyReflection = FindProperty(x => x.globalDebugSettings.lightingDebugSettings.displaySkyReflection);
            m_LightingDebugDisplaySkyReflectionMipmap = FindProperty(x => x.globalDebugSettings.lightingDebugSettings.skyReflectionMipmap);

            // Rendering settings
            m_RenderingUseForwardOnly = FindProperty(x => x.renderingSettings.useForwardRenderingOnly);
            m_RenderingUseDepthPrepass = FindProperty(x => x.renderingSettings.useDepthPrepass);

            // Subsurface Scattering Settings
            m_TexturingMode = FindProperty(x => x.sssSettings.texturingMode);
            m_Profiles = FindProperty(x => x.sssSettings.profiles);
            m_NumProfiles = m_Profiles.FindPropertyRelative("Array.size");
        }

        void InitializeSSS()
        {
            m_ProfileMaterial = Utilities.CreateEngineMaterial("Hidden/HDRenderPipeline/DrawGaussianProfile");
            m_TransmittanceMaterial = Utilities.CreateEngineMaterial("Hidden/HDRenderPipeline/DrawTransmittanceGraph");
            m_ProfileImages = new RenderTexture[SubsurfaceScatteringSettings.maxNumProfiles];
            m_TransmittanceImages = new RenderTexture[SubsurfaceScatteringSettings.maxNumProfiles];

            for (int i = 0; i < SubsurfaceScatteringSettings.maxNumProfiles; i++)
            {
                m_ProfileImages[i] = new RenderTexture(256, 256, 0, RenderTextureFormat.DefaultHDR);
                m_TransmittanceImages[i] = new RenderTexture(16, 256, 0, RenderTextureFormat.DefaultHDR);
            }
        }

        SerializedProperty FindProperty<TValue>(Expression<Func<HDRenderPipeline, TValue>> expr)
        {
            var path = Utilities.GetFieldPath(expr);
            return serializedObject.FindProperty(path);
        }

        string GetSubNameSpaceName(Type type)
        {
            return type.Namespace.Substring(type.Namespace.LastIndexOf((".")) + 1) + "/";
        }

        void FillWithProperties(Type type, GUIContent[] debugViewMaterialStrings, int[] debugViewMaterialValues, bool isBSDFData, string strSubNameSpace, ref int index)
        {
            var attributes = type.GetCustomAttributes(true);
            // Get attribute to get the start number of the value for the enum
            var attr = attributes[0] as GenerateHLSL;

            if (!attr.needParamDefines)
            {
                return ;
            }

            var fields = type.GetFields();

            var localIndex = 0;
            foreach (var field in fields)
            {
                var fieldName = field.Name;

                // Check if the display name have been override by the users
                if (Attribute.IsDefined(field, typeof(SurfaceDataAttributes)))
                {
                    var propertyAttr = (SurfaceDataAttributes[])field.GetCustomAttributes(typeof(SurfaceDataAttributes), false);
                    if (propertyAttr[0].displayName != "")
                    {
                        fieldName = propertyAttr[0].displayName;
                    }
                }

                fieldName = (isBSDFData ? "Engine/" : "") + strSubNameSpace + fieldName;

                debugViewMaterialStrings[index] = new GUIContent(fieldName);
                debugViewMaterialValues[index] = attr.paramDefinesStart + (int)localIndex;
                index++;
                localIndex++;
            }
        }

        void FillWithPropertiesEnum(Type type, GUIContent[] debugViewMaterialStrings, int[] debugViewMaterialValues, string prefix, bool isBSDFData, ref int index)
        {
            var names = Enum.GetNames(type);

            var localIndex = 0;
            foreach (var value in Enum.GetValues(type))
            {
                var valueName = (isBSDFData ? "Engine/" : "" + prefix) + names[localIndex];

                debugViewMaterialStrings[index] = new GUIContent(valueName);
                debugViewMaterialValues[index] = (int)value;
                index++;
                localIndex++;
            }
        }

        static void HackSetDirty(RenderPipelineAsset asset)
        {
            EditorUtility.SetDirty(asset);
            var method = typeof(RenderPipelineAsset).GetMethod("OnValidate", BindingFlags.FlattenHierarchy | BindingFlags.NonPublic | BindingFlags.Instance);
            if (method != null)
                method.Invoke(asset, new object[0]);
        }

        private void DebuggingUI(HDRenderPipeline renderContext, HDRenderPipelineInstance renderpipelineInstance)
        {
            EditorGUILayout.LabelField(styles.debugging);

            // Global debug settings
            EditorGUI.indentLevel++;
            m_DebugOverlayRatio.floatValue = EditorGUILayout.Slider(styles.debugOverlayRatio, m_DebugOverlayRatio.floatValue, 0.1f, 1.0f);
            EditorGUILayout.Space();

            MaterialDebugSettingsUI(renderContext);
            RenderingDebugSettingsUI(renderContext);
            LightingDebugSettingsUI(renderContext, renderpipelineInstance);

            EditorGUILayout.Space();

            EditorGUI.indentLevel--;
        }


        private void MaterialDebugSettingsUI(HDRenderPipeline renderContext)
        {
            m_ShowMaterialDebug.boolValue = EditorGUILayout.Foldout(m_ShowMaterialDebug.boolValue, styles.materialDebugLabel);
            if (!m_ShowMaterialDebug.boolValue)
                return;

            EditorGUI.indentLevel++;
            EditorGUI.BeginChangeCheck();

            if (!styles.isDebugViewMaterialInit)
            {
                var varyingNames = Enum.GetNames(typeof(Attributes.DebugViewVarying));
                var gbufferNames = Enum.GetNames(typeof(Attributes.DebugViewGbuffer));

                // +1 for the zero case
                var num = 1 + varyingNames.Length
                          + gbufferNames.Length
                          + typeof(Builtin.BuiltinData).GetFields().Length * 2 // BuildtinData are duplicated for each material
                          + typeof(Lit.SurfaceData).GetFields().Length
                          + typeof(Lit.BSDFData).GetFields().Length
                          + typeof(Unlit.SurfaceData).GetFields().Length
                          + typeof(Unlit.BSDFData).GetFields().Length;

                styles.debugViewMaterialStrings = new GUIContent[num];
                styles.debugViewMaterialValues = new int[num];

                var index = 0;

                // 0 is a reserved number
                styles.debugViewMaterialStrings[0] = new GUIContent("None");
                styles.debugViewMaterialValues[0] = 0;
                index++;

                FillWithPropertiesEnum(typeof(Attributes.DebugViewVarying), styles.debugViewMaterialStrings, styles.debugViewMaterialValues, GetSubNameSpaceName(typeof(Attributes.DebugViewVarying)), false, ref index);
                FillWithProperties(typeof(Builtin.BuiltinData), styles.debugViewMaterialStrings, styles.debugViewMaterialValues, false, GetSubNameSpaceName(typeof(Lit.SurfaceData)), ref index);
                FillWithProperties(typeof(Lit.SurfaceData), styles.debugViewMaterialStrings, styles.debugViewMaterialValues, false, GetSubNameSpaceName(typeof(Lit.SurfaceData)), ref index);
                FillWithProperties(typeof(Builtin.BuiltinData), styles.debugViewMaterialStrings, styles.debugViewMaterialValues, false, GetSubNameSpaceName(typeof(Unlit.SurfaceData)), ref index);
                FillWithProperties(typeof(Unlit.SurfaceData), styles.debugViewMaterialStrings, styles.debugViewMaterialValues, false, GetSubNameSpaceName(typeof(Unlit.SurfaceData)), ref index);

                // Engine
                FillWithPropertiesEnum(typeof(Attributes.DebugViewGbuffer), styles.debugViewMaterialStrings, styles.debugViewMaterialValues, "", true, ref index);
                FillWithProperties(typeof(Lit.BSDFData), styles.debugViewMaterialStrings, styles.debugViewMaterialValues, true, "", ref index);
                FillWithProperties(typeof(Unlit.BSDFData), styles.debugViewMaterialStrings, styles.debugViewMaterialValues, true, "", ref index);

                styles.isDebugViewMaterialInit = true;
            }

            EditorGUI.showMixedValue = m_MaterialDebugMode.hasMultipleDifferentValues;
            m_MaterialDebugMode.intValue = EditorGUILayout.IntPopup(styles.debugViewMaterial, m_MaterialDebugMode.intValue, styles.debugViewMaterialStrings, styles.debugViewMaterialValues);
            EditorGUI.showMixedValue = false;

            if (EditorGUI.EndChangeCheck())
            {
                HackSetDirty(renderContext); // Repaint
            }
            EditorGUI.indentLevel--;
        }

        private void RenderingDebugSettingsUI(HDRenderPipeline renderContext)
        {
            m_ShowRenderingDebug.boolValue = EditorGUILayout.Foldout(m_ShowRenderingDebug.boolValue, styles.renderingDebugSettings);
            if (!m_ShowRenderingDebug.boolValue)
                return;

            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(m_DisplayOpaqueObjects, styles.displayOpaqueObjects);
            EditorGUILayout.PropertyField(m_DisplayTransparentObjects, styles.displayTransparentObjects);
            EditorGUILayout.PropertyField(m_EnableDistortion, styles.enableDistortion);
            EditorGUILayout.PropertyField(m_EnableSSS, styles.enableSSS);
            EditorGUI.indentLevel--;
        }

        private void SssSettingsUI(HDRenderPipeline pipe)
        {
            EditorGUILayout.Space();

            EditorGUILayout.LabelField(styles.sssSettings);
            EditorGUI.BeginChangeCheck();
            EditorGUI.indentLevel++;

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.PropertyField(m_NumProfiles, styles.sssNumProfiles);
            m_TexturingMode.intValue = EditorGUILayout.Popup(styles.sssTexturingMode, m_TexturingMode.intValue, styles.sssTexturingModeOptions, (GUILayoutOption[])null);

            for (int i = 0, n = Math.Min(m_Profiles.arraySize, SubsurfaceScatteringSettings.maxNumProfiles); i < n; i++)
            {
                SerializedProperty profile = m_Profiles.GetArrayElementAtIndex(i);
                EditorGUILayout.PropertyField(profile, styles.sssProfiles[i]);

                if (profile.isExpanded)
                {
                    EditorGUI.indentLevel++;

                    SerializedProperty profileStdDev1 = profile.FindPropertyRelative("stdDev1");
                    SerializedProperty profileStdDev2 = profile.FindPropertyRelative("stdDev2");
                    SerializedProperty profileLerpWeight = profile.FindPropertyRelative("lerpWeight");
                    SerializedProperty profileTransmission = profile.FindPropertyRelative("enableTransmission");
                    SerializedProperty profileThicknessRemap = profile.FindPropertyRelative("thicknessRemap");

                    EditorGUILayout.PropertyField(profileStdDev1, styles.sssProfileStdDev1);
                    EditorGUILayout.PropertyField(profileStdDev2, styles.sssProfileStdDev2);
                    EditorGUILayout.PropertyField(profileLerpWeight, styles.sssProfileLerpWeight);
                    EditorGUILayout.PropertyField(profileTransmission, styles.sssProfileTransmission);

                    Vector2 thicknessRemap = profileThicknessRemap.vector2Value;
                    EditorGUILayout.LabelField("Min thickness: ", thicknessRemap.x.ToString());
                    EditorGUILayout.LabelField("Max thickness: ", thicknessRemap.y.ToString());
                    EditorGUILayout.MinMaxSlider(styles.sssProfileThicknessRemap, ref thicknessRemap.x, ref thicknessRemap.y, 0, 10);
                    profileThicknessRemap.vector2Value = thicknessRemap;

                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField(styles.sssProfilePreview0, styles.centeredMiniBoldLabel);
                    EditorGUILayout.LabelField(styles.sssProfilePreview1, EditorStyles.centeredGreyMiniLabel);
                    EditorGUILayout.LabelField(styles.sssProfilePreview2, EditorStyles.centeredGreyMiniLabel);
                    EditorGUILayout.Space();

                    // Draw the profile.
                    m_ProfileMaterial.SetColor("_StdDev1", profileStdDev1.colorValue);
                    m_ProfileMaterial.SetColor("_StdDev2", profileStdDev2.colorValue);
                    m_ProfileMaterial.SetFloat("_LerpWeight", profileLerpWeight.floatValue);
                    EditorGUI.DrawPreviewTexture(GUILayoutUtility.GetRect(256, 256), m_ProfileImages[i], m_ProfileMaterial, ScaleMode.ScaleToFit, 1.0f);

                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField(styles.sssTransmittancePreview0, styles.centeredMiniBoldLabel);
                    EditorGUILayout.LabelField(styles.sssTransmittancePreview1, EditorStyles.centeredGreyMiniLabel);
                    EditorGUILayout.Space();

                    // Draw the transmittance graph.
                    m_TransmittanceMaterial.SetColor("_StdDev1", profileStdDev1.colorValue);
                    m_TransmittanceMaterial.SetColor("_StdDev2", profileStdDev2.colorValue);
                    m_TransmittanceMaterial.SetFloat("_LerpWeight", profileLerpWeight.floatValue);
                    m_TransmittanceMaterial.SetVector("_ThicknessRemap", profileThicknessRemap.vector2Value);
                    EditorGUI.DrawPreviewTexture(GUILayoutUtility.GetRect(16, 16), m_TransmittanceImages[i], m_TransmittanceMaterial, ScaleMode.ScaleToFit, 16.0f);

                    EditorGUILayout.Space();

                    EditorGUI.indentLevel--;
                }
            }

            EditorGUI.indentLevel--;
        }

        private void LightingDebugSettingsUI(HDRenderPipeline renderContext, HDRenderPipelineInstance renderpipelineInstance)
        {
            m_ShowLightingDebug.boolValue = EditorGUILayout.Foldout(m_ShowLightingDebug.boolValue, styles.lightingDebugSettings);
            if (!m_ShowLightingDebug.boolValue)
                return;

            EditorGUI.BeginChangeCheck();

            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(m_DebugShadowEnabled, styles.shadowDebugEnable);
            EditorGUILayout.PropertyField(m_ShadowDebugMode, styles.shadowDebugVisualizationMode);
            if (!m_ShadowDebugMode.hasMultipleDifferentValues)
            {
                if ((ShadowMapDebugMode)m_ShadowDebugMode.intValue == ShadowMapDebugMode.VisualizeShadowMap)
                {
                    EditorGUILayout.IntSlider(m_ShadowDebugShadowMapIndex, 0, renderpipelineInstance.GetCurrentShadowCount() - 1, styles.shadowDebugVisualizeShadowIndex);
                }
            }
            EditorGUILayout.PropertyField(m_LightingDebugMode, styles.lightingDebugMode);
            if (!m_LightingDebugMode.hasMultipleDifferentValues)
            {
                if ((LightingDebugMode)m_LightingDebugMode.intValue != LightingDebugMode.None)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(m_LightingDebugAlbedo, styles.lightingDebugAlbedo);
                    EditorGUILayout.PropertyField(m_LightingDebugOverrideSmoothness, styles.lightingDebugOverrideSmoothness);
                    if (!m_LightingDebugOverrideSmoothness.hasMultipleDifferentValues && m_LightingDebugOverrideSmoothness.boolValue == true)
                    {
                        EditorGUI.indentLevel++;
                        EditorGUILayout.PropertyField(m_LightingDebugOverrideSmoothnessValue, styles.lightingDebugOverrideSmoothnessValue);
                        EditorGUI.indentLevel--;
                    }
                    EditorGUI.indentLevel--;
                }
            }

            EditorGUILayout.PropertyField(m_LightingDebugDisplaySkyReflection, styles.lightingDisplaySkyReflection);
            if (!m_LightingDebugDisplaySkyReflection.hasMultipleDifferentValues && m_LightingDebugDisplaySkyReflection.boolValue == true)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(m_LightingDebugDisplaySkyReflectionMipmap, styles.lightingDisplaySkyReflectionMipmap);
                EditorGUI.indentLevel--;
            }

            EditorGUI.indentLevel--;

            if (EditorGUI.EndChangeCheck())
            {
                HackSetDirty(renderContext);
            }
        }

        private void SettingsUI(HDRenderPipeline renderContext)
        {
            EditorGUILayout.LabelField(styles.settingsLabel);
            EditorGUI.indentLevel++;

            EditorGUI.BeginChangeCheck();

            renderContext.lightLoopProducer = (LightLoopProducer)EditorGUILayout.ObjectField(new GUIContent("Light Loop"), renderContext.lightLoopProducer, typeof(LightLoopProducer), false);

            if (EditorGUI.EndChangeCheck())
            {
                HackSetDirty(renderContext); // Repaint
            }

            SssSettingsUI(renderContext);
            ShadowSettingsUI(renderContext);
            TextureSettingsUI(renderContext);
            RendereringSettingsUI(renderContext);
            //TilePassUI(renderContext);

            EditorGUI.indentLevel--;
        }

        private void ShadowSettingsUI(HDRenderPipeline renderContext)
        {
            EditorGUILayout.Space();
            var shadowSettings = renderContext.shadowSettings;

            EditorGUILayout.LabelField(styles.shadowSettings);
            EditorGUI.indentLevel++;
            EditorGUI.BeginChangeCheck();

            shadowSettings.shadowAtlasWidth = Mathf.Max(0, EditorGUILayout.IntField(styles.shadowsAtlasWidth, shadowSettings.shadowAtlasWidth));
            shadowSettings.shadowAtlasHeight = Mathf.Max(0, EditorGUILayout.IntField(styles.shadowsAtlasHeight, shadowSettings.shadowAtlasHeight));

            if (EditorGUI.EndChangeCheck())
            {
                HackSetDirty(renderContext); // Repaint
            }
            EditorGUI.indentLevel--;
        }

        private void RendereringSettingsUI(HDRenderPipeline renderContext)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(styles.renderingSettingsLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(m_RenderingUseDepthPrepass, styles.useDepthPrepass);
            EditorGUILayout.PropertyField(m_RenderingUseForwardOnly, styles.useForwardRenderingOnly);
            EditorGUI.indentLevel--;
        }

        private void TextureSettingsUI(HDRenderPipeline renderContext)
        {
            EditorGUILayout.Space();
            var textureSettings = renderContext.textureSettings;

            EditorGUILayout.LabelField(styles.textureSettings);
            EditorGUI.indentLevel++;
            EditorGUI.BeginChangeCheck();

            textureSettings.spotCookieSize = Mathf.NextPowerOfTwo(Mathf.Clamp(EditorGUILayout.IntField(styles.spotCookieSize, textureSettings.spotCookieSize), 16, 1024));
            textureSettings.pointCookieSize = Mathf.NextPowerOfTwo(Mathf.Clamp(EditorGUILayout.IntField(styles.pointCookieSize, textureSettings.pointCookieSize), 16, 1024));
            textureSettings.reflectionCubemapSize = Mathf.NextPowerOfTwo(Mathf.Clamp(EditorGUILayout.IntField(styles.reflectionCubemapSize, textureSettings.reflectionCubemapSize), 64, 1024));

            if (EditorGUI.EndChangeCheck())
            {
                renderContext.textureSettings = textureSettings;
                HackSetDirty(renderContext); // Repaint
            }
            EditorGUI.indentLevel--;
        }

        /*  private void TilePassUI(HDRenderPipeline renderContext)
        {
            EditorGUILayout.Space();

            // TODO: we should call a virtual method or something similar to setup the UI, inspector should not know about it
            var tilePass = renderContext.tileSettings;
            if (tilePass != null)
            {
                EditorGUILayout.LabelField(styles.tileLightLoopSettings);
                EditorGUI.indentLevel++;
                EditorGUI.BeginChangeCheck();

                tilePass.enableBigTilePrepass = EditorGUILayout.Toggle(styles.bigTilePrepass, tilePass.enableBigTilePrepass);
                tilePass.enableClustered = EditorGUILayout.Toggle(styles.clustered, tilePass.enableClustered);

                if (EditorGUI.EndChangeCheck())
                {
                   HackSetDirty(renderContext); // Repaint

                    // SetAssetDirty will tell renderloop to rebuild
                    renderContext.DestroyCreatedInstances();
                }

                EditorGUI.BeginChangeCheck();

                tilePass.debugViewTilesFlags = EditorGUILayout.MaskField("DebugView Tiles", tilePass.debugViewTilesFlags, styles.tileLightLoopDebugTileFlagStrings);
                tilePass.enableSplitLightEvaluation = EditorGUILayout.Toggle(styles.splitLightEvaluation, tilePass.enableSplitLightEvaluation);
                tilePass.enableTileAndCluster = EditorGUILayout.Toggle(styles.enableTileAndCluster, tilePass.enableTileAndCluster);
                tilePass.enableComputeLightEvaluation = EditorGUILayout.Toggle(styles.enableComputeLightEvaluation, tilePass.enableComputeLightEvaluation);

                if (EditorGUI.EndChangeCheck())
                {
                   HackSetDirty(renderContext); // Repaint
                    UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
                }
                EditorGUI.indentLevel--;
            }
        }*/

        public void OnEnable()
        {
            InitializeProperties();
            InitializeSSS();
        }

        public override void OnInspectorGUI()
        {
            var renderContext = target as HDRenderPipeline;
            HDRenderPipelineInstance renderpipelineInstance = UnityEngine.Experimental.Rendering.RenderPipelineManager.currentPipeline as HDRenderPipelineInstance;

            if (!renderContext || renderpipelineInstance == null)
                return;

            serializedObject.Update();

            DebuggingUI(renderContext, renderpipelineInstance);
            SettingsUI(renderContext);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
