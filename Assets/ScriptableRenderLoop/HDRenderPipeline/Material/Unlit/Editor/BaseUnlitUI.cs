using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    public abstract class BaseUnlitGUI : ShaderGUI
    {
        protected static class Styles
        {
            public static string optionText = "Options";
            public static string surfaceTypeText = "Surface Type";
            public static string blendModeText = "Blend Mode";

            public static GUIContent alphaCutoffEnableText = new GUIContent("Alpha Cutoff Enable", "Threshold for alpha cutoff");
            public static GUIContent alphaCutoffText = new GUIContent("Alpha Cutoff", "Threshold for alpha cutoff");
            public static GUIContent doubleSidedModeText = new GUIContent("Double Sided", "This will render the two face of the objects (disable backface culling)");
            public static GUIContent distortionEnableText = new GUIContent("Distortion", "Enable distortion on this shader");
            public static GUIContent distortionOnlyText = new GUIContent("Distortion Only", "This shader will only be use to render distortion");
            public static GUIContent distortionDepthTestText = new GUIContent("Distortion Depth Test", "Enable the depth test for distortion");

            public static readonly string[] surfaceTypeNames = Enum.GetNames(typeof(SurfaceType));
            public static readonly string[] blendModeNames = Enum.GetNames(typeof(BlendMode));

            public static string InputsOptionsText = "Inputs options";

            public static string InputsText = "Inputs";

            public static string InputsMapText = "";

            public static GUIContent colorText = new GUIContent("Color + Opacity", "Albedo (RGB) and Opacity (A)");

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
            DoubleSided
        }

       void SurfaceTypePopup()
        {
            EditorGUI.showMixedValue = surfaceType.hasMixedValue;
            var mode = (SurfaceType)surfaceType.floatValue;

            EditorGUI.BeginChangeCheck();
            mode = (SurfaceType)EditorGUILayout.Popup(Styles.surfaceTypeText, (int)mode, Styles.surfaceTypeNames);
            if (EditorGUI.EndChangeCheck())
            {
                m_MaterialEditor.RegisterPropertyChangeUndo("Surface Type");
                surfaceType.floatValue = (float)mode;
            }

            EditorGUI.showMixedValue = false;
        }

        void BlendModePopup()
        {
            EditorGUI.showMixedValue = blendMode.hasMixedValue;
            var mode = (BlendMode)blendMode.floatValue;

            EditorGUI.BeginChangeCheck();
            mode = (BlendMode)EditorGUILayout.Popup(Styles.blendModeText, (int)mode, Styles.blendModeNames);
            if (EditorGUI.EndChangeCheck())
            {
                m_MaterialEditor.RegisterPropertyChangeUndo("Blend Mode");
                blendMode.floatValue = (float)mode;
            }

            EditorGUI.showMixedValue = false;
        }

        protected void ShaderOptionsGUI()
        {
            EditorGUI.indentLevel++;
            GUILayout.Label(Styles.optionText, EditorStyles.boldLabel);
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

            EditorGUI.indentLevel--;
        }

        public void FindCommonOptionProperties(MaterialProperty[] props)
        {
            surfaceType = FindProperty(kSurfaceType, props);
            blendMode = FindProperty(kBlendMode, props);
            alphaCutoff = FindProperty(kAlphaCutoff, props);
            alphaCutoffEnable = FindProperty(kAlphaCutoffEnabled, props);
            doubleSidedMode = FindProperty(kDoubleSidedMode, props);
            distortionEnable = FindProperty(kDistortionEnable, props);
            distortionOnly = FindProperty(kDistortionOnly, props);
            distortionDepthTest = FindProperty(kDistortionDepthTest, props);
        }

		protected void SetupCommonOptionsKeywords(Material material)
		{
			// Note: keywords must be based on Material value not on MaterialProperty due to multi-edit & material animation
            // (MaterialProperty value might come from renderer material property block)

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

            SetKeyword(material, "_ALPHATEST_ON", alphaTestEnable);

            bool distortionEnable = material.GetFloat(kDistortionEnable) == 1.0;
            bool distortionOnly = material.GetFloat(kDistortionOnly) == 1.0;
            bool distortionDepthTest = material.GetFloat(kDistortionDepthTest) == 1.0;

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
                material.SetShaderPassEnabled("DebugViewMaterial", true);
                material.SetShaderPassEnabled("Meta", false);
                material.SetShaderPassEnabled("Forward", false);
                material.SetShaderPassEnabled("ForwardOnlyOpaque", false);
            }
            else
            {
                material.SetShaderPassEnabled("DebugViewMaterial", true);
                material.SetShaderPassEnabled("Meta", true);
                material.SetShaderPassEnabled("Forward", true);
                material.SetShaderPassEnabled("ForwardOnlyOpaque", true);
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
        protected const string kSurfaceType = "_SurfaceType";
        MaterialProperty blendMode = null;
        protected const string kBlendMode = "_BlendMode";
        MaterialProperty alphaCutoff = null;
        protected const string kAlphaCutoff = "_AlphaCutoff";
        MaterialProperty alphaCutoffEnable = null;
        protected const string kAlphaCutoffEnabled = "_AlphaCutoffEnable";
        MaterialProperty doubleSidedMode = null;
        protected const string kDoubleSidedMode = "_DoubleSidedMode";
        MaterialProperty distortionEnable = null;
        const string kDistortionEnable = "_DistortionEnable";
        MaterialProperty distortionOnly = null;
        const string kDistortionOnly = "_DistortionOnly";
        MaterialProperty distortionDepthTest = null;
        const string kDistortionDepthTest = "_DistortionDepthTest";

        protected static string[] reservedProperties = new string[] { kSurfaceType, kBlendMode, kAlphaCutoff, kAlphaCutoffEnabled, kDoubleSidedMode };

        protected abstract void FindMaterialProperties(MaterialProperty[] props);
        protected abstract void ShaderInputGUI();
        protected abstract void ShaderInputOptionsGUI();
		protected abstract void SetupMaterialKeywords(Material material);
        protected abstract bool ShouldEmissionBeEnabled(Material material);
    }

}
