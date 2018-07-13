using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;

using System.Linq;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    public class TerrainLitGUI : LitGUI, ITerrainLayerCustomUI
    {
        private class StylesLayer
        {
            public readonly GUIContent layersText = new GUIContent("Inputs");
            public readonly GUIContent layerMapMaskText = new GUIContent("Layer Mask", "Layer mask");
            public readonly GUIContent layerBlendMode = new GUIContent("Layer Blend Mode", "Controls how terrain layers are blended.");
            public readonly GUIContent heightTransition = new GUIContent("Height Transition", "Size in world units of the smooth transition between layers.");
            public readonly GUIContent enableInstancedPerPixelNormal = new GUIContent("Enable Per-pixel Normal", "Enable per-pixel normal when the terrain uses instanced rendering.");

            public readonly GUIContent diffuseTexture = new GUIContent("Diffuse", "Alpha: Density");
            public readonly GUIContent tint = new GUIContent("Tint");
            public readonly GUIContent enableDensity = new GUIContent("Enable Density");
            public readonly GUIContent diffuseTextureWithoutDensity = new GUIContent("Diffuse");
            public readonly GUIContent normalMapTexture = new GUIContent("Normal Map");
            public readonly GUIContent normalScale = new GUIContent("Normal Scale");
            public readonly GUIContent maskMapTexture = new GUIContent("Mask", "R: Metallic\nG: AO\nB: Height\nA: Smoothness");
            public readonly GUIContent maskMapTextureWithoutHeight = new GUIContent("Mask Map", "R: Metallic\nG: AO\nA: Smoothness");
            public readonly GUIContent channelRemapping = new GUIContent("Channel Remapping");
            public readonly GUIContent defaultValues = new GUIContent("Channel Default Values");
            public readonly GUIContent metallic = new GUIContent("R: Metallic");
            public readonly GUIContent ao = new GUIContent("G: AO");
            public readonly GUIContent height = new GUIContent("B: Height");
            public readonly GUIContent heightParametrization = new GUIContent("Parametrization");
            public readonly GUIContent heightAmplitude = new GUIContent("Amplitude (cm)");
            public readonly GUIContent heightBase = new GUIContent("Base");
            public readonly GUIContent heightMin = new GUIContent("Min (cm)");
            public readonly GUIContent heightMax = new GUIContent("Max (cm)");
            public readonly GUIContent heightCm = new GUIContent("B: Height (cm)");
            public readonly GUIContent smoothness = new GUIContent("A: Smoothness");
        }

        static StylesLayer s_Styles = null;
        private static StylesLayer styles { get { if (s_Styles == null) s_Styles = new StylesLayer(); return s_Styles; } }

        public TerrainLitGUI()
        {
        }

        MaterialProperty layerBlendMode;
        const string kLayerBlendMode = "_LayerBlendMode";

        // Height blend
        MaterialProperty heightTransition = null;
        const string kHeightTransition = "_HeightTransition";

        MaterialProperty enableInstancedPerPixelNormal = null;
        const string kEnableInstancedPerPixelNormal = "_EnableInstancedPerPixelNormal";

        protected override void FindMaterialProperties(MaterialProperty[] props)
        {
            layerBlendMode = FindProperty(kLayerBlendMode, props, false);
            heightTransition = FindProperty(kHeightTransition, props, false);
            enableInstancedPerPixelNormal = FindProperty(kEnableInstancedPerPixelNormal, props, false);
        }

        // We use the user data to save a string that represent the referenced lit material
        // so we can keep reference during serialization
        void DoLayeringInputGUI()
        {
            EditorGUI.indentLevel++;
            GUILayout.Label(styles.layersText, EditorStyles.boldLabel);

            if (layerBlendMode != null)
            {
                m_MaterialEditor.ShaderProperty(layerBlendMode, styles.layerBlendMode);
                if (layerBlendMode.floatValue == 2)
                {
                    EditorGUI.indentLevel++;
                    m_MaterialEditor.ShaderProperty(heightTransition, styles.heightTransition);
                    EditorGUI.indentLevel--;
                }
            }

            EditorGUI.indentLevel--;
        }

        bool DoLayersGUI(AssetImporter materialImporter)
        {
            bool layerChanged = false;

            GUI.changed = false;

            DoLayeringInputGUI();

            EditorGUILayout.Space();

            layerChanged |= GUI.changed;
            GUI.changed = false;

            return layerChanged;
        }

        protected override bool ShouldEmissionBeEnabled(Material mat)
        {
            return false;
        }

        protected override void SetupMaterialKeywordsAndPassInternal(Material material)
        {
            SetupMaterialKeywordsAndPass(material);
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
        static new public void SetupMaterialKeywordsAndPass(Material material)
        {
            SetupBaseLitKeywords(material);
            SetupBaseLitMaterialPass(material);

            // TODO: planar/triplannar supprt
            //SetupLayersMappingKeywords(material);

            int layerBlendMode = (int)material.GetFloat(kLayerBlendMode);
            CoreUtils.SetKeyword(material, "_TERRAIN_BLEND_DENSITY", layerBlendMode == 1);
            CoreUtils.SetKeyword(material, "_TERRAIN_BLEND_HEIGHT", layerBlendMode == 2);

            bool enableInstancedPerPixelNormal = material.GetFloat(kEnableInstancedPerPixelNormal) > 0.0f;
            CoreUtils.SetKeyword(material, "_TERRAIN_INSTANCED_PERPIXEL_NORMAL", enableInstancedPerPixelNormal);
        }

        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] props)
        {
            FindBaseMaterialProperties(props);
            FindMaterialProperties(props);

            m_MaterialEditor = materialEditor;
            // We should always do this call at the beginning
            m_MaterialEditor.serializedObject.Update();

            Material material = m_MaterialEditor.target as Material;
            AssetImporter materialImporter = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(material.GetInstanceID()));

            bool optionsChanged = false;
            EditorGUI.BeginChangeCheck();
            {
                BaseMaterialPropertiesGUI();
                EditorGUILayout.Space();
            }
            if (EditorGUI.EndChangeCheck())
            {
                optionsChanged = true;
            }

            bool layerChanged = DoLayersGUI(materialImporter);
            bool enablePerPixelNormalChanged = false;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField(StylesBaseUnlit.advancedText, EditorStyles.boldLabel);
            // NB RenderQueue editor is not shown on purpose: we want to override it based on blend mode
            EditorGUI.indentLevel++;
            m_MaterialEditor.EnableInstancingField();
            if (m_MaterialEditor.IsInstancingEnabled())
            {
                EditorGUI.indentLevel++;
                EditorGUI.BeginChangeCheck();
                m_MaterialEditor.ShaderProperty(enableInstancedPerPixelNormal, styles.enableInstancedPerPixelNormal);
                enablePerPixelNormalChanged = EditorGUI.EndChangeCheck();
                EditorGUI.indentLevel--;
            }
            EditorGUI.indentLevel--;

            if (layerChanged || optionsChanged || enablePerPixelNormalChanged)
            {
                foreach (var obj in m_MaterialEditor.targets)
                {
                    SetupMaterialKeywordsAndPassInternal((Material)obj);
                }
            }

            // We should always do this call at the end
            m_MaterialEditor.serializedObject.ApplyModifiedProperties();
        }

        private bool m_ShowChannelRemapping = false;

        enum HeightParametrization
        {
            Amplitude,
            MinMax
        };
        private HeightParametrization m_HeightParametrization = HeightParametrization.Amplitude;

        private static bool DoesTerrainUseMaskMaps(Terrain terrain)
        {
            var terrainLayers = terrain.terrainData.terrainLayers;
            for (int i = 0; i < terrainLayers.Length; ++i)
            {
                if (terrainLayers[i].maskMapTexture != null)
                    return true;
            }
            return false;
        }

        bool ITerrainLayerCustomUI.OnTerrainLayerGUI(TerrainLayer terrainLayer, Terrain terrain)
        {
            if (!DoesTerrainUseMaskMaps(terrain))
                return false;

            var layerBlendMode = terrain.materialTemplate.GetFloat("_LayerBlendMode"); // Don't use the member field layerBlendMode as ShaderGUI.OnGUI might not be called if the material is folded.
            bool densityUsed = layerBlendMode == 1;
            bool heightUsed = layerBlendMode == 2;
            
            terrainLayer.diffuseTexture = EditorGUILayout.ObjectField(densityUsed ? styles.diffuseTexture : styles.diffuseTextureWithoutDensity, terrainLayer.diffuseTexture, typeof(Texture2D), false) as Texture2D;
            TerrainLayerUtility.ValidateDiffuseTextureUI(terrainLayer.diffuseTexture);

            var diffuseRemapMin = terrainLayer.diffuseRemapMin;
            var diffuseRemapMax = terrainLayer.diffuseRemapMax;
            EditorGUI.BeginChangeCheck();

            if (terrainLayer.diffuseTexture != null)
            {
                var rect = GUILayoutUtility.GetLastRect();
                rect.y += 16;
                ++EditorGUI.indentLevel;
                rect = EditorGUI.PrefixLabel(rect, styles.tint);
                rect.width = 64;
                rect.height = 16;
                --EditorGUI.indentLevel;
                var diffuseTint = new Color(diffuseRemapMax.x, diffuseRemapMax.y, diffuseRemapMax.z);
                diffuseTint = EditorGUI.ColorField(rect, GUIContent.none, diffuseTint, true, false, false);
                diffuseRemapMax.x = diffuseTint.r;
                diffuseRemapMax.y = diffuseTint.g;
                diffuseRemapMax.z = diffuseTint.b;
                diffuseRemapMin.x = diffuseRemapMin.y = diffuseRemapMin.z = 0;
            }

            if (densityUsed)
            {
                bool enableDensity = EditorGUILayout.Toggle(styles.enableDensity, diffuseRemapMax.w == 1);
                diffuseRemapMax.w = enableDensity ? 1 : 0;
                diffuseRemapMin.w = terrainLayer.diffuseTexture == null && enableDensity ? 1 : 0;
            }

            if (EditorGUI.EndChangeCheck())
            {
                terrainLayer.diffuseRemapMin = diffuseRemapMin;
                terrainLayer.diffuseRemapMax = diffuseRemapMax;
            }

            terrainLayer.normalMapTexture = EditorGUILayout.ObjectField(styles.normalMapTexture, terrainLayer.normalMapTexture, typeof(Texture2D), false) as Texture2D;
            TerrainLayerUtility.ValidateNormalMapTextureUI(terrainLayer.normalMapTexture, TerrainLayerUtility.CheckNormalMapTextureType(terrainLayer.normalMapTexture));

            if (terrainLayer.normalMapTexture != null)
            {
                ++EditorGUI.indentLevel;
                terrainLayer.normalScale = EditorGUILayout.FloatField(styles.normalScale, terrainLayer.normalScale);
                --EditorGUI.indentLevel;
                EditorGUILayout.Space();
            }

            terrainLayer.maskMapTexture = EditorGUILayout.ObjectField(heightUsed ? styles.maskMapTexture : styles.maskMapTextureWithoutHeight, terrainLayer.maskMapTexture, typeof(Texture2D), false) as Texture2D;
            TerrainLayerUtility.ValidateMaskMapTextureUI(terrainLayer.maskMapTexture);

            var maskMapRemapMin = terrainLayer.maskMapRemapMin;
            var maskMapRemapMax = terrainLayer.maskMapRemapMax;

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

                    if (heightUsed)
                    {
                        EditorGUILayout.LabelField(styles.height);
                        ++EditorGUI.indentLevel;
                        m_HeightParametrization = (HeightParametrization)EditorGUILayout.EnumPopup(styles.heightParametrization, m_HeightParametrization);
                        if (m_HeightParametrization == HeightParametrization.Amplitude)
                        {
                            // (height - heightBase) * amplitude
                            float amplitude = maskMapRemapMax.z - maskMapRemapMin.z;
                            float heightBase = -maskMapRemapMin.z / amplitude;
                            amplitude = EditorGUILayout.FloatField(styles.heightAmplitude, amplitude * 100) / 100;
                            heightBase = EditorGUILayout.FloatField(styles.heightBase, heightBase);
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
                    maskMapRemapMin.x = maskMapRemapMax.x = EditorGUILayout.Slider(s_Styles.metallic, maskMapRemapMin.x, 0, 1);
                    maskMapRemapMin.y = maskMapRemapMax.y = EditorGUILayout.Slider(s_Styles.ao, maskMapRemapMin.y, 0, 1);
                    if (heightUsed)
                        maskMapRemapMin.z = maskMapRemapMax.z = EditorGUILayout.FloatField(s_Styles.heightCm, maskMapRemapMin.z * 100) / 100;
                    maskMapRemapMin.w = maskMapRemapMax.w = EditorGUILayout.Slider(s_Styles.smoothness, maskMapRemapMin.w, 0, 1);
                }
            }

            if (EditorGUI.EndChangeCheck())
            {
                terrainLayer.maskMapRemapMin = maskMapRemapMin;
                terrainLayer.maskMapRemapMax = maskMapRemapMax;
            }
            --EditorGUI.indentLevel;

            EditorGUILayout.Space();
            TerrainLayerUtility.TilingSettingsUI(terrainLayer);

            return true;
        }
    }
} // namespace UnityEditor
