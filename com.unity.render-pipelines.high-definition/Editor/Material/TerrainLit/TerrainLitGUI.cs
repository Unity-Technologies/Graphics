using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.HighDefinition;

// Include material common properties names
using static UnityEngine.Rendering.HighDefinition.HDMaterialProperties;

namespace UnityEditor.Rendering.HighDefinition
{
    /// <summary>
    /// GUI for HDRP Terrain Lit materials (does not include ShaderGraphs)
    /// </summary>
    class TerrainLitGUI : HDShaderGUI, ITerrainLayerCustomUI
    {
        const SurfaceOptionUIBlock.Features surfaceOptionFeatures = SurfaceOptionUIBlock.Features.Unlit | SurfaceOptionUIBlock.Features.ReceiveDecal;
        const AdvancedOptionsUIBlock.Features advancedOptionsFeatures = AdvancedOptionsUIBlock.Features.Instancing | AdvancedOptionsUIBlock.Features.SpecularOcclusion;

        [Flags]
        enum Expandable
        {
            Terrain = 1 << 1,
        }

        MaterialUIBlockList uiBlocks = new MaterialUIBlockList
        {
            new SurfaceOptionUIBlock(MaterialUIBlock.ExpandableBit.Base, features: surfaceOptionFeatures),
            new AdvancedOptionsUIBlock(MaterialUIBlock.ExpandableBit.Advance, features: advancedOptionsFeatures),
        };

        protected override void OnMaterialGUI(MaterialEditor materialEditor, MaterialProperty[] props)
        {
            FindMaterialProperties(props);

            uiBlocks.Initialize(materialEditor, props);
            uiBlocks.FetchUIBlock<SurfaceOptionUIBlock>().UpdateMaterialProperties(props);
            uiBlocks.FetchUIBlock<SurfaceOptionUIBlock>().OnGUI();

            // TODO: move the terrain UI to a MaterialUIBlock to clarify the code
            DrawTerrainGUI(materialEditor);

            uiBlocks.FetchUIBlock<AdvancedOptionsUIBlock>().UpdateMaterialProperties(props);
            uiBlocks.FetchUIBlock<AdvancedOptionsUIBlock>().OnGUI();
        }

        private class StylesLayer
        {
            public GUIContent header { get; } = EditorGUIUtility.TrTextContent("Terrain");
            public readonly GUIContent enableHeightBlend = new GUIContent("Enable Height-based Blend", "Blend terrain layers based on height values.");
            public readonly GUIContent heightTransition = new GUIContent("Height Transition", "Size in world units of the smooth transition between layers.");
            public readonly GUIContent enableInstancedPerPixelNormal = new GUIContent("Enable Per-pixel Normal", "Enable per-pixel normal when the terrain uses instanced rendering.");

            public readonly GUIContent diffuseTexture = new GUIContent("Diffuse");
            public readonly GUIContent colorTint = new GUIContent("Color Tint");
            public readonly GUIContent opacityAsDensity = new GUIContent("Opacity as Density", "Enable Density Blend");
            public readonly GUIContent normalMapTexture = new GUIContent("Normal Map");
            public readonly GUIContent normalScale = new GUIContent("Normal Scale");
            public readonly GUIContent maskMapTexture = new GUIContent("Mask", "R: Metallic\nG: Ambient Occlusion\nB: Height\nA: Smoothness.");
            public readonly GUIContent maskMapTextureWithoutHeight = new GUIContent("Mask Map", "R: Metallic\nG: Ambient Occlusion\nA: Smoothness.");
            public readonly GUIContent channelRemapping = new GUIContent("Channel Remapping");
            public readonly GUIContent defaultValues = new GUIContent("Channel Default Values");
            public readonly GUIContent metallic = new GUIContent("R: Metallic");
            public readonly GUIContent ao = new GUIContent("G: AO");
            public readonly GUIContent height = new GUIContent("B: Height", "Specifies the Height Map for this Material.");
            public readonly GUIContent heightParametrization = new GUIContent("Parametrization", "Specifies the parametrization method for the Height Map.");
            public readonly GUIContent heightAmplitude = new GUIContent("Amplitude", "Sets the amplitude of the Height Map (in centimeters).");
            public readonly GUIContent heightBase = new GUIContent("Base", "Controls the base of the Height Map (between 0 and 1).");
            public readonly GUIContent heightMin = new GUIContent("Min", "Sets the minimum value in the Height Map (in centimeters).");
            public readonly GUIContent heightMax = new GUIContent("Max", "Sets the maximum value in the Height Map (in centimeters).");
            public readonly GUIContent heightCm = new GUIContent("B: Height (cm)");
            public readonly GUIContent smoothness = new GUIContent("A: Smoothness");
        }

        static StylesLayer s_Styles = null;
        private static StylesLayer styles { get { if (s_Styles == null) s_Styles = new StylesLayer(); return s_Styles; } }

        public TerrainLitGUI()
        {
        }

        MaterialProperty enableHeightBlend;
        MaterialProperty heightTransition = null;
        MaterialProperty enableInstancedPerPixelNormal = null;

        // Custom fields
        List<MaterialProperty> customProperties = new List<MaterialProperty>();

        protected void FindMaterialProperties(MaterialProperty[] props)
        {
            customProperties.Clear();
            foreach (var prop in props)
            {
                if (prop.name == kEnableHeightBlend)
                    enableHeightBlend = prop;
                else if (prop.name == kHeightTransition)
                    heightTransition = prop;
                else if (prop.name == kEnableInstancedPerPixelNormal)
                    enableInstancedPerPixelNormal = prop;
                else if ((prop.flags & (MaterialProperty.PropFlags.HideInInspector | MaterialProperty.PropFlags.PerRendererData)) == 0)
                    customProperties.Add(prop);
            }
        }

        static public void SetupLayersMappingKeywords(Material material)
        {
            const string kLayerMappingPlanar = "_LAYER_MAPPING_PLANAR";
            const string kLayerMappingTriplanar = "_LAYER_MAPPING_TRIPLANAR";

            for (int i = 0; i < kMaxLayerCount; ++i)
            {
                string layerUVBaseParam = string.Format("{0}{1}", kUVBase, i);
                UVBaseMapping layerUVBaseMapping = (UVBaseMapping)material.GetFloat(layerUVBaseParam);
                string currentLayerMappingPlanar = string.Format("{0}{1}", kLayerMappingPlanar, i);
                CoreUtils.SetKeyword(material, currentLayerMappingPlanar, layerUVBaseMapping == UVBaseMapping.Planar);
                string currentLayerMappingTriplanar = string.Format("{0}{1}", kLayerMappingTriplanar, i);
                CoreUtils.SetKeyword(material, currentLayerMappingTriplanar, layerUVBaseMapping == UVBaseMapping.Triplanar);
            }
        }

        // All Setup Keyword functions must be static. It allow to create script to automatically update the shaders with a script if code change

        static public bool TextureHasAlpha(Texture2D inTex)
        {
            if (inTex != null)
            {
                return GraphicsFormatUtility.HasAlphaChannel(GraphicsFormatUtility.GetGraphicsFormat(inTex.format, true));
            }
            return false;
        }

        protected void DrawTerrainGUI(MaterialEditor materialEditor)
        {
            // Don't draw the header if we have empty content
            if (enableHeightBlend == null && enableInstancedPerPixelNormal == null && customProperties.Count == 0)
                return;

            using (var header = new MaterialHeaderScope(styles.header, (uint)Expandable.Terrain, materialEditor))
            {
                if (header.expanded)
                {
                    if (enableHeightBlend != null)
                    {
                        materialEditor.ShaderProperty(enableHeightBlend, styles.enableHeightBlend);
                        if (enableHeightBlend.floatValue > 0)
                        {
                            EditorGUI.indentLevel++;
                            materialEditor.ShaderProperty(heightTransition, styles.heightTransition);
                            EditorGUI.indentLevel--;
                        }
                    }
                    if (enableInstancedPerPixelNormal != null)
                    {
                        EditorGUI.BeginDisabledGroup(!materialEditor.IsInstancingEnabled());
                        materialEditor.ShaderProperty(enableInstancedPerPixelNormal, styles.enableInstancedPerPixelNormal);
                        EditorGUI.EndDisabledGroup();
                    }
                    foreach (var prop in customProperties)
                        materialEditor.ShaderProperty(prop, prop.displayName);
                }
            }
        }

        private bool m_ShowChannelRemapping = false;

        enum HeightParametrization
        {
            Amplitude,
            MinMax
        };
        private HeightParametrization m_HeightParametrization = HeightParametrization.Amplitude;

        private static bool DoesTerrainUseMaskMaps(TerrainLayer[] terrainLayers)
        {
            for (int i = 0; i < terrainLayers.Length; ++i)
            {
                if (terrainLayers[i].maskMapTexture != null)
                    return true;
            }
            return false;
        }

        bool ITerrainLayerCustomUI.OnTerrainLayerGUI(TerrainLayer terrainLayer, Terrain terrain)
        {
            var terrainLayers = terrain.terrainData.terrainLayers;
            if (!DoesTerrainUseMaskMaps(terrainLayers))
                return false;

            // Don't use the member field enableHeightBlend as ShaderGUI.OnGUI might not be called if the material UI is folded.
            bool heightBlend = terrain.materialTemplate.HasProperty(kEnableHeightBlend) && terrain.materialTemplate.GetFloat(kEnableHeightBlend) > 0;

            terrainLayer.diffuseTexture = EditorGUILayout.ObjectField(styles.diffuseTexture, terrainLayer.diffuseTexture, typeof(Texture2D), false) as Texture2D;
            TerrainLayerUtility.ValidateDiffuseTextureUI(terrainLayer.diffuseTexture);

            var diffuseRemapMin = terrainLayer.diffuseRemapMin;
            var diffuseRemapMax = terrainLayer.diffuseRemapMax;
            EditorGUI.BeginChangeCheck();

            bool enableDensity = false;
            if (terrainLayer.diffuseTexture != null)
            {
                var rect = GUILayoutUtility.GetLastRect();
                rect.y += 16 + 4;
                rect.width = EditorGUIUtility.labelWidth + 64;
                rect.height = 16;

                ++EditorGUI.indentLevel;

                var diffuseTint = new Color(diffuseRemapMax.x, diffuseRemapMax.y, diffuseRemapMax.z);
                diffuseTint = EditorGUI.ColorField(rect, styles.colorTint, diffuseTint, true, false, false);
                diffuseRemapMax.x = diffuseTint.r;
                diffuseRemapMax.y = diffuseTint.g;
                diffuseRemapMax.z = diffuseTint.b;
                diffuseRemapMin.x = diffuseRemapMin.y = diffuseRemapMin.z = 0;

                if (!heightBlend)
                {
                    rect.y = rect.yMax + 2;
                    enableDensity = EditorGUI.Toggle(rect, styles.opacityAsDensity, diffuseRemapMin.w > 0);
                }

                --EditorGUI.indentLevel;
            }
            diffuseRemapMax.w = 1;
            diffuseRemapMin.w = enableDensity ? 1 : 0;

            if (EditorGUI.EndChangeCheck())
            {
                terrainLayer.diffuseRemapMin = diffuseRemapMin;
                terrainLayer.diffuseRemapMax = diffuseRemapMax;
            }

            terrainLayer.normalMapTexture = EditorGUILayout.ObjectField(styles.normalMapTexture, terrainLayer.normalMapTexture, typeof(Texture2D), false) as Texture2D;
            TerrainLayerUtility.ValidateNormalMapTextureUI(terrainLayer.normalMapTexture, TerrainLayerUtility.CheckNormalMapTextureType(terrainLayer.normalMapTexture));

            if (terrainLayer.normalMapTexture != null)
            {
                var rect = GUILayoutUtility.GetLastRect();
                rect.y += 16 + 4;
                rect.width = EditorGUIUtility.labelWidth + 64;
                rect.height = 16;

                ++EditorGUI.indentLevel;
                terrainLayer.normalScale = EditorGUI.FloatField(rect, styles.normalScale, terrainLayer.normalScale);
                --EditorGUI.indentLevel;
            }

            terrainLayer.maskMapTexture = EditorGUILayout.ObjectField(heightBlend ? styles.maskMapTexture : styles.maskMapTextureWithoutHeight, terrainLayer.maskMapTexture, typeof(Texture2D), false) as Texture2D;
            TerrainLayerUtility.ValidateMaskMapTextureUI(terrainLayer.maskMapTexture);

            var maskMapRemapMin = terrainLayer.maskMapRemapMin;
            var maskMapRemapMax = terrainLayer.maskMapRemapMax;
            var smoothness = terrainLayer.smoothness;
            var metallic = terrainLayer.metallic;

            ++EditorGUI.indentLevel;
            EditorGUI.BeginChangeCheck();

            m_ShowChannelRemapping = EditorGUILayout.Foldout(m_ShowChannelRemapping, terrainLayer.maskMapTexture != null ? s_Styles.channelRemapping : s_Styles.defaultValues);
            if (m_ShowChannelRemapping)
            {
                if (terrainLayer.maskMapTexture != null)
                {
                    float min, max;
                    min = maskMapRemapMin.x; max = maskMapRemapMax.x;
                    EditorGUILayout.MinMaxSlider(s_Styles.metallic, ref min, ref max, 0, 1);
                    maskMapRemapMin.x = min; maskMapRemapMax.x = max;

                    min = maskMapRemapMin.y; max = maskMapRemapMax.y;
                    EditorGUILayout.MinMaxSlider(s_Styles.ao, ref min, ref max, 0, 1);
                    maskMapRemapMin.y = min; maskMapRemapMax.y = max;

                    if (heightBlend)
                    {
                        EditorGUILayout.LabelField(styles.height);
                        ++EditorGUI.indentLevel;
                        m_HeightParametrization = (HeightParametrization)EditorGUILayout.EnumPopup(styles.heightParametrization, m_HeightParametrization);
                        if (m_HeightParametrization == HeightParametrization.Amplitude)
                        {
                            // (height - heightBase) * amplitude
                            float amplitude = Mathf.Max(maskMapRemapMax.z - maskMapRemapMin.z, Mathf.Epsilon); // to avoid divide by zero
                            float heightBase = -maskMapRemapMin.z / amplitude;
                            amplitude = EditorGUILayout.FloatField(styles.heightAmplitude, amplitude * 100) / 100;
                            heightBase = EditorGUILayout.Slider(styles.heightBase, heightBase, 0.0f, 1.0f);
                            maskMapRemapMin.z = -heightBase * amplitude;
                            maskMapRemapMax.z = (1 - heightBase) * amplitude;
                        }
                        else
                        {
                            maskMapRemapMin.z = EditorGUILayout.FloatField(styles.heightMin, maskMapRemapMin.z * 100) / 100;
                            maskMapRemapMax.z = EditorGUILayout.FloatField(styles.heightMax, maskMapRemapMax.z * 100) / 100;
                        }
                        --EditorGUI.indentLevel;
                    }

                    min = maskMapRemapMin.w; max = maskMapRemapMax.w;
                    EditorGUILayout.MinMaxSlider(s_Styles.smoothness, ref min, ref max, 0, 1);
                    maskMapRemapMin.w = min; maskMapRemapMax.w = max;
                }
                else
                {
                    metallic = EditorGUILayout.Slider(s_Styles.metallic, metallic, 0, 1);
                    // AO and Height are still exclusively controlled via the maskRemap controls
                    // metallic and smoothness have their own values as fields within the LayerData.
                    maskMapRemapMax.y = EditorGUILayout.Slider(s_Styles.ao, maskMapRemapMax.y, 0, 1);

                    if (heightBlend)
                    {
                        maskMapRemapMax.z = EditorGUILayout.FloatField(s_Styles.heightCm, maskMapRemapMax.z * 100) / 100;
                    }
                    // There's a possibility that someone could slide max below the existing min value
                    // so we'll just protect against that by locking the min value down a little bit.
                    // In the case of height (Z), we are trying to set min to no lower than zero value unless
                    // max goes negative.  Zero is a good sensible value for the minimum.  For AO (Y), we
                    // don't need this extra protection step because the UI blocks us from going negative
                    // anyway.  In both cases, pushing the slider below the min value will lock them together,
                    // but min will be "left behind" if you go back up.
                    maskMapRemapMin.y = Mathf.Min(maskMapRemapMin.y, maskMapRemapMax.y);
                    maskMapRemapMin.z = Mathf.Min(Mathf.Max(0, maskMapRemapMin.z), maskMapRemapMax.z);

                    if (TextureHasAlpha(terrainLayer.diffuseTexture))
                    {
                        terrainLayer.smoothnessSource = (UnityEngine.TerrainLayerSmoothnessSource)EditorGUILayout.EnumPopup(EditorGUIUtility.TrTextContent("Smoothness Source"), terrainLayer.smoothnessSource);
                        if (terrainLayer.smoothnessSource == TerrainLayerSmoothnessSource.DiffuseAlphaChannel)
                        {
                            // See also: TerrainLitShaderGUI, TerrainLayerInspector
                            GUIStyle warnStyle = new GUIStyle(GUI.skin.label);
                            warnStyle.wordWrap = true;
                            GUILayout.Label("Smoothness is controlled by diffuse alpha channel", warnStyle);
                        }
                        else
                        {
                            smoothness = EditorGUILayout.Slider(s_Styles.smoothness, smoothness, 0, 1);
                        }
                    }
                    else
                    {
                        smoothness = EditorGUILayout.Slider(s_Styles.smoothness, smoothness, 0, 1);
                    }
                }
            }

            if (EditorGUI.EndChangeCheck())
            {
                terrainLayer.maskMapRemapMin = maskMapRemapMin;
                terrainLayer.maskMapRemapMax = maskMapRemapMax;
                terrainLayer.smoothness = smoothness;
                terrainLayer.metallic = metallic;
            }
            --EditorGUI.indentLevel;

            EditorGUILayout.Space();
            TerrainLayerUtility.TilingSettingsUI(terrainLayer);

            return true;
        }

        public override void ValidateMaterial(Material material) => TerrainLitAPI.ValidateMaterial(material);
    }
} // namespace UnityEditor
