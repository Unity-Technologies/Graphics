using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.Experimental.ScriptableRenderLoop
{
    public abstract class BaseLitGUI : ShaderGUI
    {
        protected static class Styles
        {
            public static string OptionText = "Options";
            public static string SurfaceTypeText = "Surface Type";
            public static string BlendModeText = "Blend Mode";
            public static string detailText = "Inputs Detail";
            public static string lightingText = "Inputs Lighting";

            public static GUIContent alphaCutoffEnableText = new GUIContent("Alpha Cutoff Enable", "Threshold for alpha cutoff");
            public static GUIContent alphaCutoffText = new GUIContent("Alpha Cutoff", "Threshold for alpha cutoff");
            public static GUIContent doubleSidedModeText = new GUIContent("Double Sided", "This will render the two face of the objects (disable backface culling)");
            public static GUIContent distortionEnableText = new GUIContent("Distortion", "Enable distortion on this shader");
            public static GUIContent distortionOnlyText = new GUIContent("Distortion Only", "This shader will only be use to render distortion");
            public static GUIContent distortionDepthTestText = new GUIContent("Distortion Depth Test", "Enable the depth test for distortion");
            public static GUIContent depthOffsetEnableText = new GUIContent("DepthOffset", "EnableDepthOffset on this shader (Use with heightmap)");

            public static readonly string[] surfaceTypeNames = Enum.GetNames(typeof(SurfaceType));
            public static readonly string[] blendModeNames = Enum.GetNames(typeof(BlendMode));

            public static string InputsOptionsText = "Inputs options";

            public static GUIContent smoothnessMapChannelText = new GUIContent("Smoothness Source", "Smoothness texture and channel");
            public static GUIContent UVBaseMappingText = new GUIContent("UV set for Base", "");
            public static GUIContent texWorldScaleText = new GUIContent("Tiling", "Tiling factor applied to Planar/Trilinear mapping");
            public static GUIContent UVBaseDetailMappingText = new GUIContent("UV set for Base and Detail", "");
            public static GUIContent normalMapSpaceText = new GUIContent("Normal/Tangent Map space", "");
            public static GUIContent heightMapModeText = new GUIContent("Height Map Mode", "");
            public static GUIContent detailMapModeText = new GUIContent("Detail Map with Normal", "Detail Map with AO / Height");
            public static GUIContent UVDetailMappingText = new GUIContent("UV set for Detail", "");
            public static GUIContent emissiveColorModeText = new GUIContent("Emissive Color Usage", "Use emissive color or emissive mask");

            public static string InputsText = "Inputs";

            public static string InputsMapText = "";

            public static GUIContent baseColorText = new GUIContent("Base Color + Opacity", "Albedo (RGB) and Opacity (A)");
            public static GUIContent baseColorSmoothnessText = new GUIContent("Base Color + Smoothness", "Albedo (RGB) and Smoothness (A)");

            public static GUIContent metallicText = new GUIContent("Metallic", "Metallic scale factor");
            public static GUIContent smoothnessText = new GUIContent("Smoothness", "Smoothness scale factor");
            public static GUIContent maskMapESText = new GUIContent("Mask Map - M(R), AO(G), E(B), S(A)", "Mask map");
            public static GUIContent maskMapEText = new GUIContent("Mask Map - M(R), AO(G), E(B)", "Mask map");
            public static GUIContent maskMapText = new GUIContent("Mask Map - M(R), AO(G)", "Mask map");
            public static GUIContent maskMapSText = new GUIContent("Mask Map - M(R), AO(G), S(A)", "Mask map");

            public static GUIContent specularOcclusionMapText = new GUIContent("Specular Occlusion Map (RGBA)", "Specular Occlusion Map");

            public static GUIContent normalMapText = new GUIContent("Normal Map", "Normal Map (DXT5) - Need to implement BC5");
 
            public static GUIContent heightMapText = new GUIContent("Height Map (R)", "Height Map");

            public static GUIContent tangentMapText = new GUIContent("Tangent Map", "Tangent Map (BC5) - DXT5 for test");
            public static GUIContent anisotropyText = new GUIContent("Anisotropy", "Anisotropy scale factor");
            public static GUIContent anisotropyMapText = new GUIContent("Anisotropy Map (G)", "Anisotropy");

            public static GUIContent detailMapNormalText = new GUIContent("Detail Map A(R) Ny(G) S(B) Nx(A)", "Detail Map");
            public static GUIContent detailMapAOHeightText = new GUIContent("Detail Map  A(R) AO(G) S(B) H(A)", "Detail Map");
            public static GUIContent detailMaskText = new GUIContent("Detail Mask (B)", "Mask for detailMap");
            public static GUIContent detailAlbedoScaleText = new GUIContent("Detail AlbedoScale", "Detail Albedo Scale factor");
            public static GUIContent detailNormalScaleText = new GUIContent("Detail NormalScale", "Normal Scale factor");
            public static GUIContent detailSmoothnessScaleText = new GUIContent("Detail SmoothnessScale", "Smoothness Scale factor");
            public static GUIContent detailHeightScaleText = new GUIContent("Detail HeightScale", "Height Scale factor");
            public static GUIContent detailAOScaleText = new GUIContent("Detail AOScale", "AO Scale factor");

            public static GUIContent emissiveText = new GUIContent("Emissive Color", "Emissive");
            public static GUIContent emissiveIntensityText = new GUIContent("Emissive Intensity", "Emissive");

            public static GUIContent emissiveWarning = new GUIContent("Emissive value is animated but the material has not been configured to support emissive. Please make sure the material itself has some amount of emissive.");
            public static GUIContent emissiveColorWarning = new GUIContent("Ensure emissive color is non-black for emission to have effect.");
        }

        public enum SurfaceType
        {
            Opaque,
            Transparent
        }
        public enum BlendMode
        {
            Lerp,
            Add,
            SoftAdd,
            Multiply,
            Premultiply
        }
        public enum DoubleSidedMode
        {
            None,
            DoubleSided,
            DoubleSidedLightingFlip,
            DoubleSidedLightingMirror,
        }

        void SurfaceTypePopup()
        {
            EditorGUI.showMixedValue = surfaceType.hasMixedValue;
            var mode = (SurfaceType)surfaceType.floatValue;

            EditorGUI.BeginChangeCheck();
            mode = (SurfaceType)EditorGUILayout.Popup(Styles.SurfaceTypeText, (int)mode, Styles.surfaceTypeNames);
            if (EditorGUI.EndChangeCheck())
            {
                m_MaterialEditor.RegisterPropertyChangeUndo("Surface Type");
                surfaceType.floatValue = (float)mode;
            }

            EditorGUI.showMixedValue = false;
        }

        protected void ShaderOptionsGUI()
        {
            EditorGUI.indentLevel++;
            GUILayout.Label(Styles.OptionText, EditorStyles.boldLabel);
            SurfaceTypePopup();
            if ((SurfaceType)surfaceType.floatValue == SurfaceType.Transparent)
            {
                BlendModePopup();
                m_MaterialEditor.ShaderProperty(distortionEnable, Styles.distortionEnableText.text);

                if (distortionEnable.floatValue == 1.0)
                {
                    m_MaterialEditor.ShaderProperty(distortionOnly, Styles.distortionOnlyText.text);
                    m_MaterialEditor.ShaderProperty(distortionDepthTest, Styles.distortionDepthTestText.text);
                }
            }
            m_MaterialEditor.ShaderProperty(alphaCutoffEnable, Styles.alphaCutoffEnableText.text);
            if (alphaCutoffEnable.floatValue == 1.0)
            {
                m_MaterialEditor.ShaderProperty(alphaCutoff, Styles.alphaCutoffText.text);
            }
            m_MaterialEditor.ShaderProperty(doubleSidedMode, Styles.doubleSidedModeText.text);
            m_MaterialEditor.ShaderProperty(depthOffsetEnable, Styles.depthOffsetEnableText.text);

            EditorGUI.indentLevel--;
        }

        private void BlendModePopup()
        {
            EditorGUI.showMixedValue = blendMode.hasMixedValue;
            var mode = (BlendMode)blendMode.floatValue;

            EditorGUI.BeginChangeCheck();
            mode = (BlendMode)EditorGUILayout.Popup(Styles.BlendModeText, (int)mode, Styles.blendModeNames);
            if (EditorGUI.EndChangeCheck())
            {
                m_MaterialEditor.RegisterPropertyChangeUndo("Blend Mode");
                blendMode.floatValue = (float)mode;
            }

            EditorGUI.showMixedValue = false;
        }

        protected void FindCommonOptionProperties(MaterialProperty[] props)
        {
            surfaceType = FindProperty(kSurfaceType, props);
            blendMode = FindProperty(kBlendMode, props);
            alphaCutoff = FindProperty(kAlphaCutoff, props);
            alphaCutoffEnable = FindProperty(kAlphaCutoffEnabled, props);
            doubleSidedMode = FindProperty(kDoubleSidedMode, props);
            distortionEnable = FindProperty(kDistortionEnable, props);
            distortionOnly = FindProperty(kDistortionOnly, props);
            distortionDepthTest = FindProperty(kDistortionDepthTest, props);
            depthOffsetEnable = FindProperty(kDepthOffsetEnable, props);
        }

        protected void SetupCommonOptionsKeywords(Material material)
        {
            bool alphaTestEnable = material.GetFloat(kAlphaCutoffEnabled) == 1.0;
            SurfaceType surfaceType = (SurfaceType)material.GetFloat(kSurfaceType);
            BlendMode blendMode = (BlendMode)material.GetFloat(kBlendMode);
            DoubleSidedMode doubleSidedMode = (DoubleSidedMode)material.GetFloat(kDoubleSidedMode);
 
            if (surfaceType == SurfaceType.Opaque)
            {
                material.SetOverrideTag("RenderType", alphaTestEnable ? "TransparentCutout" : "");
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                material.SetInt("_ZWrite", 1);
                material.renderQueue = alphaTestEnable ? (int)UnityEngine.Rendering.RenderQueue.AlphaTest : -1;
            }
            else
            {
                material.SetOverrideTag("RenderType", "Transparent");
                material.SetInt("_ZWrite", 0);
                material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

                switch (blendMode)
                {
                    case BlendMode.Lerp:
                        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                        break;

                    case BlendMode.Add:
                        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
                        break;

                    case BlendMode.SoftAdd:
                        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusDstColor);
                        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
                        break;

                    case BlendMode.Multiply:
                        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.DstColor);
                        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                        break;

                    case BlendMode.Premultiply:
                        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                        break;
                }
            }

            if (doubleSidedMode == DoubleSidedMode.None)
            {
                material.SetInt("_CullMode", (int)UnityEngine.Rendering.CullMode.Back);
            }
            else
            {
                material.SetInt("_CullMode", (int)UnityEngine.Rendering.CullMode.Off);
            }

            if (doubleSidedMode == DoubleSidedMode.DoubleSidedLightingFlip)
            {
                material.EnableKeyword("_DOUBLESIDED_LIGHTING_FLIP");
                material.DisableKeyword("_DOUBLESIDED_LIGHTING_MIRROR");
            }
            else if (doubleSidedMode == DoubleSidedMode.DoubleSidedLightingMirror)
            {
                material.DisableKeyword("_DOUBLESIDED_LIGHTING_FLIP");
                material.EnableKeyword("_DOUBLESIDED_LIGHTING_MIRROR");
            }
            else
            {
                material.DisableKeyword("_DOUBLESIDED_LIGHTING_FLIP");
                material.DisableKeyword("_DOUBLESIDED_LIGHTING_MIRROR");
            }

            SetKeyword(material, "_ALPHATEST_ON", alphaTestEnable);

            bool distortionEnable = material.GetFloat(kDistortionEnable) == 1.0;
            bool distortionOnly = material.GetFloat(kDistortionOnly) == 1.0;
            bool distortionDepthTest = material.GetFloat(kDistortionDepthTest) == 1.0;
            bool depthOffsetEnable = material.GetFloat(kDepthOffsetEnable) == 1.0;

            if (distortionEnable)
            {
                material.SetShaderPassEnabled("DistortionVectors", true);
            }
            else
            {
                material.SetShaderPassEnabled("DistortionVectors", false);
            }

            if (distortionEnable && distortionOnly)
            {
                // Disable all passes except dbug material
                material.SetShaderPassEnabled("GBuffer", false);
                material.SetShaderPassEnabled("DebugViewMaterial", true);
                material.SetShaderPassEnabled("Meta", false);
                material.SetShaderPassEnabled("ShadowCaster", false);
                material.SetShaderPassEnabled("DepthOnly", false);
                material.SetShaderPassEnabled("MotionVectors", false);
                material.SetShaderPassEnabled("Forward", false); 
            }
            else
            {
                material.SetShaderPassEnabled("GBuffer", true);
                material.SetShaderPassEnabled("DebugViewMaterial", true);
                material.SetShaderPassEnabled("Meta", true);
                material.SetShaderPassEnabled("ShadowCaster", true);
                material.SetShaderPassEnabled("DepthOnly", true);
                material.SetShaderPassEnabled("MotionVectors", true);
                material.SetShaderPassEnabled("Forward", true);
            }

            if (distortionDepthTest)
            {
                material.SetInt("_ZTestMode", (int)UnityEngine.Rendering.CompareFunction.LessEqual);
            }
            else
            {
                material.SetInt("_ZTestMode", (int)UnityEngine.Rendering.CompareFunction.Always);
            }         

            SetKeyword(material, "_DISTORTION_ON", distortionEnable);
            SetKeyword(material, "_DEPTHOFFSET_ON", depthOffsetEnable);

            SetupEmissionGIFlags(material);
        }

        protected void SetKeyword(Material m, string keyword, bool state)
        {
            if (state)
                m.EnableKeyword(keyword);
            else
                m.DisableKeyword(keyword);
        }

        public void ShaderPropertiesGUI(Material material)
        {
            // Use default labelWidth
            EditorGUIUtility.labelWidth = 0f;

            // Detect any changes to the material
            EditorGUI.BeginChangeCheck();
            {
                ShaderOptionsGUI();
                EditorGUILayout.Space();

                ShaderInputOptionsGUI();

                EditorGUILayout.Space();
                ShaderInputGUI();
            }

            if (EditorGUI.EndChangeCheck())
            {
                foreach (var obj in m_MaterialEditor.targets)
                    SetupMaterialKeywords((Material)obj);
            }
        }

        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] props)
        {
            FindCommonOptionProperties(props); // MaterialProperties can be animated so we do not cache them but fetch them every event to ensure animated values are updated correctly
			FindMaterialProperties(props);

            m_MaterialEditor = materialEditor;
            Material material = materialEditor.target as Material;
            ShaderPropertiesGUI(material);
        }

        // TODO: ? or remove
        bool HasValidEmissiveKeyword(Material material)
        {
            /*
            // Material animation might be out of sync with the material keyword.
            // So if the emission support is disabled on the material, but the property blocks have a value that requires it, then we need to show a warning.
            // (note: (Renderer MaterialPropertyBlock applies its values to emissionColorForRendering))
            bool hasEmissionKeyword = material.IsKeywordEnabled ("_EMISSION");
            if (!hasEmissionKeyword && ShouldEmissionBeEnabled (material, emissionColorForRendering.colorValue))
                return false;
            else
                return true;
            */

            return true;
        }

        protected virtual void SetupEmissionGIFlags(Material material)
        {
            // Setup lightmap emissive flags
            MaterialGlobalIlluminationFlags flags = material.globalIlluminationFlags;
            if ((flags & (MaterialGlobalIlluminationFlags.BakedEmissive | MaterialGlobalIlluminationFlags.RealtimeEmissive)) != 0)
            {
                if (ShouldEmissionBeEnabled(material))
                    flags &= ~MaterialGlobalIlluminationFlags.EmissiveIsBlack;
                else
                    flags |= MaterialGlobalIlluminationFlags.EmissiveIsBlack;

                material.globalIlluminationFlags = flags;
            }
        }

        protected MaterialEditor m_MaterialEditor;

        MaterialProperty surfaceType = null;
        MaterialProperty alphaCutoffEnable = null;
        MaterialProperty blendMode = null;
        MaterialProperty alphaCutoff = null;
        MaterialProperty doubleSidedMode = null;
        MaterialProperty distortionEnable = null;
        MaterialProperty distortionOnly = null;
        MaterialProperty distortionDepthTest = null;
        MaterialProperty depthOffsetEnable = null;

        const string kSurfaceType = "_SurfaceType";
        const string kBlendMode = "_BlendMode";
        const string kAlphaCutoff = "_AlphaCutoff";
        const string kAlphaCutoffEnabled = "_AlphaCutoffEnable";
        const string kDoubleSidedMode = "_DoubleSidedMode";
        const string kDistortionEnable = "_DistortionEnable";
        const string kDistortionOnly = "_DistortionOnly";
        const string kDistortionDepthTest = "_DistortionDepthTest";
        const string kDepthOffsetEnable = "_DepthOffsetEnable";
        protected static string[] reservedProperties = new string[] { kSurfaceType, kBlendMode, kAlphaCutoff, kAlphaCutoffEnabled, kDoubleSidedMode };

        protected abstract void FindMaterialProperties(MaterialProperty[] props);
		protected abstract void ShaderInputGUI();
        protected abstract void ShaderInputOptionsGUI();
        protected abstract void SetupMaterialKeywords(Material material);
        protected abstract bool ShouldEmissionBeEnabled(Material material);
    }
} // namespace UnityEditor
