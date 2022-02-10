using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    public class TerrainSurfaceOptionsUIBlock : MaterialUIBlock
    {
        [Flags]
        internal enum Expandable
        {
            Terrain = 1 << 1,
        }

        internal enum HeightParametrization
        {
            Amplitude,
            MinMax
        };

        internal class Styles
        {
            public static GUIContent optionText { get; } = EditorGUIUtility.TrTextContent("Terrain Options");
            public static readonly GUIContent enableHeightBlend = new GUIContent("Enable Height-based Blend", "Blend terrain layers based on height values.");
            public static readonly GUIContent heightTransition = new GUIContent("Height Transition", "Size in world units of the smooth transition between layers.");
            public static readonly GUIContent enableInstancedPerPixelNormal = new GUIContent("Enable Per-pixel Normal", "Enable per-pixel normal when the terrain uses instanced rendering.");

            public static readonly GUIContent diffuseTexture = new GUIContent("Diffuse");
            public static readonly GUIContent colorTint = new GUIContent("Color Tint");
            public static readonly GUIContent opacityAsDensity = new GUIContent("Opacity as Density", "Enable Density Blend");
            public static readonly GUIContent normalMapTexture = new GUIContent("Normal Map");
            public static readonly GUIContent normalScale = new GUIContent("Normal Scale");
            public static readonly GUIContent maskMapTexture = new GUIContent("Mask", "R: Metallic\nG: Ambient Occlusion\nB: Height\nA: Smoothness.");
            public static readonly GUIContent maskMapTextureWithoutHeight = new GUIContent("Mask Map", "R: Metallic\nG: Ambient Occlusion\nA: Smoothness.");
            public static readonly GUIContent channelRemapping = new GUIContent("Channel Remapping");
            public static readonly GUIContent defaultValues = new GUIContent("Channel Default Values");
            public static readonly GUIContent metallic = new GUIContent("R: Metallic");
            public static readonly GUIContent ao = new GUIContent("G: AO");
            public static readonly GUIContent height = new GUIContent("B: Height", "Specifies the Height Map for this Material.");
            public static readonly GUIContent heightParametrization = new GUIContent("Parametrization", "Specifies the parametrization method for the Height Map.");
            public static readonly GUIContent heightAmplitude = new GUIContent("Amplitude", "Sets the amplitude of the Height Map (in centimeters).");
            public static readonly GUIContent heightBase = new GUIContent("Base", "Controls the base of the Height Map (between 0 and 1).");
            public static readonly GUIContent heightMin = new GUIContent("Min", "Sets the minimum value in the Height Map (in centimeters).");
            public static readonly GUIContent heightMax = new GUIContent("Max", "Sets the maximum value in the Height Map (in centimeters).");
            public static readonly GUIContent heightCm = new GUIContent("B: Height (cm)");
            public static readonly GUIContent smoothness = new GUIContent("A: Smoothness");
        }

        private MaterialProperty enableHeightBlend = null;
        private MaterialProperty heightTransition = null;
        private MaterialProperty enableInstancedPerPixelNormal = null;

        // Custom fields
        private List<MaterialProperty> customProperties = new List<MaterialProperty>();

        private bool m_ShowChannelRemapping = false;
        private HeightParametrization m_HeightParametrization = HeightParametrization.Amplitude;

        public TerrainSurfaceOptionsUIBlock(ExpandableBit expandableBit) : base(expandableBit, Styles.optionText)
        {

        }

        public override void LoadMaterialProperties()
        {
            FindTerrainLitProperties(properties);
        }

        protected override void OnGUIOpen()
        {
            if (enableHeightBlend != null)
            {
                materialEditor.ShaderProperty(enableHeightBlend, Styles.enableHeightBlend);
                if (enableHeightBlend.floatValue > 0)
                {
                    EditorGUI.indentLevel++;
                    materialEditor.ShaderProperty(heightTransition, Styles.heightTransition);
                    EditorGUI.indentLevel--;
                }
            }

            if (enableInstancedPerPixelNormal != null)
            {
                EditorGUI.BeginDisabledGroup(!materialEditor.IsInstancingEnabled());
                materialEditor.ShaderProperty(enableInstancedPerPixelNormal, Styles.enableInstancedPerPixelNormal);
                EditorGUI.EndDisabledGroup();
            }

            foreach (var prop in customProperties)
                materialEditor.ShaderProperty(prop, prop.displayName);
        }

        public bool OnTerrainLayerGUI(TerrainLayer terrainLayer, Terrain terrain)
        {
            var terrainLayers = terrain.terrainData.terrainLayers;
            if (!DoesTerrainUseMaskMaps(terrainLayers))
                return false;

            // Don't use the member field enableHeightBlend as ShaderGUI.OnGUI might not be called if the material UI is folded.
            bool heightBlend =
                terrain.materialTemplate.HasProperty(HDMaterialProperties.kEnableHeightBlend) &&
                terrain.materialTemplate.GetFloat(HDMaterialProperties.kEnableHeightBlend) > 0;

            terrainLayer.diffuseTexture = EditorGUILayout.ObjectField(Styles.diffuseTexture, terrainLayer.diffuseTexture, typeof(Texture2D), false) as Texture2D;
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
                diffuseTint = EditorGUI.ColorField(rect, Styles.colorTint, diffuseTint, true, false, false);
                diffuseRemapMax.x = diffuseTint.r;
                diffuseRemapMax.y = diffuseTint.g;
                diffuseRemapMax.z = diffuseTint.b;
                diffuseRemapMin.x = diffuseRemapMin.y = diffuseRemapMin.z = 0;

                if (!heightBlend)
                {
                    rect.y = rect.yMax + 2;
                    enableDensity = EditorGUI.Toggle(rect, Styles.opacityAsDensity, diffuseRemapMin.w > 0);
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

            terrainLayer.normalMapTexture = EditorGUILayout.ObjectField(Styles.normalMapTexture, terrainLayer.normalMapTexture, typeof(Texture2D), false) as Texture2D;
            TerrainLayerUtility.ValidateNormalMapTextureUI(terrainLayer.normalMapTexture, TerrainLayerUtility.CheckNormalMapTextureType(terrainLayer.normalMapTexture));

            if (terrainLayer.normalMapTexture != null)
            {
                var rect = GUILayoutUtility.GetLastRect();
                rect.y += 16 + 4;
                rect.width = EditorGUIUtility.labelWidth + 64;
                rect.height = 16;

                ++EditorGUI.indentLevel;
                terrainLayer.normalScale = EditorGUI.FloatField(rect, Styles.normalScale, terrainLayer.normalScale);
                --EditorGUI.indentLevel;
            }

            terrainLayer.maskMapTexture = EditorGUILayout.ObjectField(heightBlend ? Styles.maskMapTexture : Styles.maskMapTextureWithoutHeight, terrainLayer.maskMapTexture, typeof(Texture2D), false) as Texture2D;
            TerrainLayerUtility.ValidateMaskMapTextureUI(terrainLayer.maskMapTexture);

            var maskMapRemapMin = terrainLayer.maskMapRemapMin;
            var maskMapRemapMax = terrainLayer.maskMapRemapMax;
            var smoothness = terrainLayer.smoothness;
            var metallic = terrainLayer.metallic;

            ++EditorGUI.indentLevel;
            EditorGUI.BeginChangeCheck();

            m_ShowChannelRemapping = EditorGUILayout.Foldout(m_ShowChannelRemapping, terrainLayer.maskMapTexture != null ? Styles.channelRemapping : Styles.defaultValues);
            if (m_ShowChannelRemapping)
            {
                if (terrainLayer.maskMapTexture != null)
                {
                    float min, max;
                    min = maskMapRemapMin.x; max = maskMapRemapMax.x;
                    EditorGUILayout.MinMaxSlider(Styles.metallic, ref min, ref max, 0, 1);
                    maskMapRemapMin.x = min; maskMapRemapMax.x = max;

                    min = maskMapRemapMin.y; max = maskMapRemapMax.y;
                    EditorGUILayout.MinMaxSlider(Styles.ao, ref min, ref max, 0, 1);
                    maskMapRemapMin.y = min; maskMapRemapMax.y = max;

                    if (heightBlend)
                    {
                        EditorGUILayout.LabelField(Styles.height);
                        ++EditorGUI.indentLevel;
                        m_HeightParametrization = (HeightParametrization)EditorGUILayout.EnumPopup(Styles.heightParametrization, m_HeightParametrization);
                        if (m_HeightParametrization == HeightParametrization.Amplitude)
                        {
                            // (height - heightBase) * amplitude
                            float amplitude = Mathf.Max(maskMapRemapMax.z - maskMapRemapMin.z, Mathf.Epsilon); // to avoid divide by zero
                            float heightBase = -maskMapRemapMin.z / amplitude;
                            amplitude = EditorGUILayout.FloatField(Styles.heightAmplitude, amplitude * 100) / 100;
                            heightBase = EditorGUILayout.Slider(Styles.heightBase, heightBase, 0.0f, 1.0f);
                            maskMapRemapMin.z = -heightBase * amplitude;
                            maskMapRemapMax.z = (1 - heightBase) * amplitude;
                        }
                        else
                        {
                            maskMapRemapMin.z = EditorGUILayout.FloatField(Styles.heightMin, maskMapRemapMin.z * 100) / 100;
                            maskMapRemapMax.z = EditorGUILayout.FloatField(Styles.heightMax, maskMapRemapMax.z * 100) / 100;
                        }
                        --EditorGUI.indentLevel;
                    }

                    min = maskMapRemapMin.w; max = maskMapRemapMax.w;
                    EditorGUILayout.MinMaxSlider(Styles.smoothness, ref min, ref max, 0, 1);
                    maskMapRemapMin.w = min; maskMapRemapMax.w = max;
                }
                else
                {
                    metallic = EditorGUILayout.Slider(Styles.metallic, metallic, 0, 1);
                    // AO and Height are still exclusively controlled via the maskRemap controls
                    // metallic and smoothness have their own values as fields within the LayerData.
                    maskMapRemapMax.y = EditorGUILayout.Slider(Styles.ao, maskMapRemapMax.y, 0, 1);

                    if (heightBlend)
                    {
                        maskMapRemapMax.z = EditorGUILayout.FloatField(Styles.heightCm, maskMapRemapMax.z * 100) / 100;
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
                        GUIStyle warnStyle = new GUIStyle(GUI.skin.label);
                        warnStyle.wordWrap = true;
                        GUILayout.Label("Smoothness is controlled by diffuse alpha channel", warnStyle);
                    }
                    else
                        smoothness = EditorGUILayout.Slider(Styles.smoothness, smoothness, 0, 1);
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

        private static bool DoesTerrainUseMaskMaps(TerrainLayer[] terrainLayers)
        {
            for (int i = 0; i < terrainLayers.Length; ++i)
            {
                if (terrainLayers[i].maskMapTexture != null)
                    return true;
            }

            return false;
        }

        private static bool TextureHasAlpha(Texture2D inTex)
        {
            if (inTex == null)
                return false;

            return GraphicsFormatUtility.HasAlphaChannel(GraphicsFormatUtility.GetGraphicsFormat(inTex.format, true));
        }

        protected void FindTerrainLitProperties(MaterialProperty[] props)
        {
            customProperties.Clear();
            foreach (var prop in props)
            {
                if (prop.name == HDMaterialProperties.kEnableHeightBlend)
                    enableHeightBlend = prop;
                else if (prop.name == HDMaterialProperties.kHeightTransition)
                    heightTransition = prop;
                else if (prop.name == HDMaterialProperties.kEnableInstancedPerPixelNormal)
                    enableInstancedPerPixelNormal = prop;
                else if ((prop.flags & (MaterialProperty.PropFlags.HideInInspector | MaterialProperty.PropFlags.PerRendererData)) == 0)
                    customProperties.Add(prop);
            }
        }
    }
}
