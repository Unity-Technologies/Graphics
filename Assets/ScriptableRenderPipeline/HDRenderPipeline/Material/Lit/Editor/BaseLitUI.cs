using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    // A Material can be authored from the shader graph or by hand. When written by hand we need to provide an inspector.
    // Such a Material will share some properties between it various variant (shader graph variant or hand authored variant).
    // This is the purpose of BaseLitGUI. It contain all properties that are common to all Material based on Lit template.
    // For the default hand written Lit material see LitUI.cs that contain specific properties for our default implementation.
    public abstract class BaseLitGUI : ShaderGUI
    {
        protected static class StylesBase
        {
            public static string optionText = "Surface options";
            public static string surfaceTypeText = "Surface Type";
            public static string blendModeText = "Blend Mode";            

            public static readonly string[] surfaceTypeNames = Enum.GetNames(typeof(SurfaceType));
            public static readonly string[] blendModeNames = Enum.GetNames(typeof(BlendMode));

            public static GUIContent alphaCutoffEnableText = new GUIContent("Alpha Cutoff Enable", "Threshold for alpha cutoff");
            public static GUIContent alphaCutoffText = new GUIContent("Alpha Cutoff", "Threshold for alpha cutoff");
            public static GUIContent doubleSidedEnableText = new GUIContent("Double Sided", "This will render the two face of the objects (disable backface culling) and flip/mirror normal");
            public static GUIContent doubleSidedMirrorEnableText = new GUIContent("Mirror normal", "This will mirror the normal with vertex normal plane if enabled, else flip the normal");
            public static GUIContent distortionEnableText = new GUIContent("Distortion", "Enable distortion on this shader");
            public static GUIContent distortionOnlyText = new GUIContent("Distortion Only", "This shader will only be use to render distortion");
            public static GUIContent distortionDepthTestText = new GUIContent("Distortion Depth Test", "Enable the depth test for distortion");
            public static GUIContent depthOffsetEnableText = new GUIContent("Enable Depth Offset", "EnableDepthOffset on this shader (Use with heightmap)");

            public static GUIContent emissiveWarning = new GUIContent("Emissive value is animated but the material has not been configured to support emissive. Please make sure the material itself has some amount of emissive.");
            public static GUIContent emissiveColorWarning = new GUIContent("Ensure emissive color is non-black for emission to have effect.");

            // Material ID
            public static GUIContent materialIDText = new GUIContent("Material type", "Subsurface Scattering: enable for translucent materials such as skin, vegetation, fruit, marble, wax and milk.");

            // Per pixel displacement
            public static GUIContent enablePerPixelDisplacementText = new GUIContent("Enable Per Pixel Displacement", "");
            public static GUIContent ppdMinSamplesText = new GUIContent("Minimum samples", "Minimun samples to use with per pixel displacement mapping");
            public static GUIContent ppdMaxSamplesText = new GUIContent("Maximum samples", "Maximum samples to use with per pxiel displacement mapping");
            public static GUIContent ppdLodThresholdText = new GUIContent("Fading LOD start", "Starting Lod where the parallax occlusion mapping effect start to disappear");

            // Tessellation
            public static string tessellationModeText = "Tessellation Mode";
            public static readonly string[] tessellationModeNames = Enum.GetNames(typeof(TessellationMode));

            public static GUIContent tessellationText = new GUIContent("Tessellation options", "Tessellation options");
            public static GUIContent tessellationFactorText = new GUIContent("Tessellation factor", "This value is the tessellation factor use for tessellation, higher mean more tessellated");
            public static GUIContent tessellationFactorMinDistanceText = new GUIContent("Start fade distance", "Distance (in unity unit) at which the tessellation start to fade out. Must be inferior at Max distance");
            public static GUIContent tessellationFactorMaxDistanceText = new GUIContent("End fade distance", "Maximum distance (in unity unit) to the camera where triangle are tessellated");
            public static GUIContent tessellationFactorTriangleSizeText = new GUIContent("Triangle size", "Desired screen space sized of triangle (in pixel). Smaller value mean smaller triangle.");
            public static GUIContent tessellationShapeFactorText = new GUIContent("Shape factor", "Strength of Phong tessellation shape (lerp factor)");
            public static GUIContent tessellationBackFaceCullEpsilonText = new GUIContent("Triangle culling Epsilon", "If -1.0 back face culling is enabled for tessellation, higher number mean more aggressive culling and better performance");
            public static GUIContent tessellationObjectScaleText = new GUIContent("Enable object scale", "Tesselation displacement will take into account the object scale - Only work with uniform positive scale");
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

        public enum TessellationMode
        {
            Phong,
            Displacement,
            DisplacementPhong,
        }

        protected MaterialEditor m_MaterialEditor;

        // Properties
        protected MaterialProperty surfaceType = null;
        protected const string kSurfaceType = "_SurfaceType";
        protected MaterialProperty alphaCutoffEnable = null;
        protected const string kAlphaCutoffEnabled = "_AlphaCutoffEnable";
        protected MaterialProperty alphaCutoff = null;
        protected const string kAlphaCutoff = "_AlphaCutoff";
        protected MaterialProperty doubleSidedEnable = null;
        protected const string kDoubleSidedEnable = "_DoubleSidedEnable";
        protected MaterialProperty doubleSidedMirrorEnable = null;
        protected const string kDoubleSidedMirrorEnable = "_DoubleSidedMirrorEnable";
        protected MaterialProperty blendMode = null;
        protected const string kBlendMode = "_BlendMode";
        protected MaterialProperty distortionEnable = null;
        protected const string kDistortionEnable = "_DistortionEnable";
        protected MaterialProperty distortionOnly = null;
        protected const string kDistortionOnly = "_DistortionOnly";
        protected MaterialProperty distortionDepthTest = null;
        protected const string kDistortionDepthTest = "_DistortionDepthTest";
        protected MaterialProperty depthOffsetEnable = null;
        protected const string kDepthOffsetEnable = "_DepthOffsetEnable";

        // Material ID
        protected MaterialProperty materialID = null;
        protected const string kMaterialID = "_MaterialID";

        // Per pixel displacement params
        protected MaterialProperty enablePerPixelDisplacement = null;
        protected const string kEnablePerPixelDisplacement = "_EnablePerPixelDisplacement";
        protected MaterialProperty ppdMinSamples = null;
        protected const string kPpdMinSamples = "_PPDMinSamples";
        protected MaterialProperty ppdMaxSamples = null;
        protected const string kPpdMaxSamples = "_PPDMaxSamples";
        protected MaterialProperty ppdLodThreshold = null;
        protected const string kPpdLodThreshold = "_PPDLodThreshold";

        // tessellation params
        protected MaterialProperty tessellationMode = null;
        protected const string kTessellationMode = "_TessellationMode";
        protected MaterialProperty tessellationFactor = null;
        protected const string kTessellationFactor = "_TessellationFactor";
        protected MaterialProperty tessellationFactorMinDistance = null;
        protected const string kTessellationFactorMinDistance = "_TessellationFactorMinDistance";
        protected MaterialProperty tessellationFactorMaxDistance = null;
        protected const string kTessellationFactorMaxDistance = "_TessellationFactorMaxDistance";
        protected MaterialProperty tessellationFactorTriangleSize = null;
        protected const string kTessellationFactorTriangleSize = "_TessellationFactorTriangleSize";
        protected MaterialProperty tessellationShapeFactor = null;
        protected const string kTessellationShapeFactor = "_TessellationShapeFactor";
        protected MaterialProperty tessellationBackFaceCullEpsilon = null;
        protected const string kTessellationBackFaceCullEpsilon = "_TessellationBackFaceCullEpsilon";
        protected MaterialProperty tessellationObjectScale = null;
        protected const string kTessellationObjectScale = "_TessellationObjectScale";

        // See comment in LitProperties.hlsl
        const string kEmissionColor = "_EmissionColor";

        // The following set of functions are call by the ShaderGraph
        // It will allow to display our common parameters + setup keyword correctly for them
        protected abstract void FindMaterialProperties(MaterialProperty[] props);
        protected abstract void SetupMaterialKeywordsInternal(Material material);
        protected abstract void MaterialPropertiesGUI();
        // This function will said if emissive is use or not dor enlighten/PVR consideration
        protected abstract bool ShouldEmissionBeEnabled(Material material);

        protected void FindBaseMaterialProperties(MaterialProperty[] props)
        {
            surfaceType = FindProperty(kSurfaceType, props);
            alphaCutoffEnable = FindProperty(kAlphaCutoffEnabled, props);
            alphaCutoff = FindProperty(kAlphaCutoff, props);
            doubleSidedEnable = FindProperty(kDoubleSidedEnable, props);
            doubleSidedMirrorEnable = FindProperty(kDoubleSidedMirrorEnable, props);
            blendMode = FindProperty(kBlendMode, props);
            distortionEnable = FindProperty(kDistortionEnable, props);
            distortionOnly = FindProperty(kDistortionOnly, props);
            distortionDepthTest = FindProperty(kDistortionDepthTest, props);
            depthOffsetEnable = FindProperty(kDepthOffsetEnable, props);

            // MaterialID
            materialID = FindProperty(kMaterialID, props, false);

            // Per pixel displacement
            enablePerPixelDisplacement = FindProperty(kEnablePerPixelDisplacement, props);
            ppdMinSamples = FindProperty(kPpdMinSamples, props);
            ppdMaxSamples = FindProperty(kPpdMaxSamples, props);
            ppdLodThreshold = FindProperty(kPpdLodThreshold, props);

            // tessellation specific, silent if not found
            tessellationMode = FindProperty(kTessellationMode, props, false);
            tessellationFactor = FindProperty(kTessellationFactor, props, false);
            tessellationFactorMinDistance = FindProperty(kTessellationFactorMinDistance, props, false);
            tessellationFactorMaxDistance = FindProperty(kTessellationFactorMaxDistance, props, false);
            tessellationFactorTriangleSize = FindProperty(kTessellationFactorTriangleSize, props, false);
            tessellationShapeFactor = FindProperty(kTessellationShapeFactor, props, false);
            tessellationBackFaceCullEpsilon = FindProperty(kTessellationBackFaceCullEpsilon, props, false);
            tessellationObjectScale = FindProperty(kTessellationObjectScale, props, false);
        }

        void SurfaceTypePopup()
        {
            EditorGUI.showMixedValue = surfaceType.hasMixedValue;
            var mode = (SurfaceType)surfaceType.floatValue;

            EditorGUI.BeginChangeCheck();
            mode = (SurfaceType)EditorGUILayout.Popup(StylesBase.surfaceTypeText, (int)mode, StylesBase.surfaceTypeNames);
            if (EditorGUI.EndChangeCheck())
            {
                m_MaterialEditor.RegisterPropertyChangeUndo("Surface Type");
                surfaceType.floatValue = (float)mode;
            }

            EditorGUI.showMixedValue = false;
        }

        private void BlendModePopup()
        {
            EditorGUI.showMixedValue = blendMode.hasMixedValue;
            var mode = (BlendMode)blendMode.floatValue;

            EditorGUI.BeginChangeCheck();
            mode = (BlendMode)EditorGUILayout.Popup(StylesBase.blendModeText, (int)mode, StylesBase.blendModeNames);
            if (EditorGUI.EndChangeCheck())
            {
                m_MaterialEditor.RegisterPropertyChangeUndo("Blend Mode");
                blendMode.floatValue = (float)mode;
            }

            EditorGUI.showMixedValue = false;
        }

        void TessellationModePopup()
        {
            EditorGUI.showMixedValue = tessellationMode.hasMixedValue;
            var mode = (TessellationMode)tessellationMode.floatValue;

            EditorGUI.BeginChangeCheck();
            mode = (TessellationMode)EditorGUILayout.Popup(StylesBase.tessellationModeText, (int)mode, StylesBase.tessellationModeNames);
            if (EditorGUI.EndChangeCheck())
            {
                m_MaterialEditor.RegisterPropertyChangeUndo("Tessellation Mode");
                tessellationMode.floatValue = (float)mode;
            }

            EditorGUI.showMixedValue = false;
        }

        protected void BaseMaterialPropertiesGUI()
        {
            EditorGUI.indentLevel++;
            GUILayout.Label(StylesBase.optionText, EditorStyles.boldLabel);
            SurfaceTypePopup();
            if ((SurfaceType)surfaceType.floatValue == SurfaceType.Transparent)
            {
                BlendModePopup();
                m_MaterialEditor.ShaderProperty(distortionEnable, StylesBase.distortionEnableText);

                if (distortionEnable.floatValue == 1.0f)
                {
                    m_MaterialEditor.ShaderProperty(distortionOnly, StylesBase.distortionOnlyText);
                    m_MaterialEditor.ShaderProperty(distortionDepthTest, StylesBase.distortionDepthTestText);
                }
            }
            m_MaterialEditor.ShaderProperty(alphaCutoffEnable, StylesBase.alphaCutoffEnableText);
            if (alphaCutoffEnable.floatValue == 1.0f)
            {
                m_MaterialEditor.ShaderProperty(alphaCutoff, StylesBase.alphaCutoffText);
            }
            m_MaterialEditor.ShaderProperty(doubleSidedEnable, StylesBase.doubleSidedEnableText);
            if (doubleSidedEnable.floatValue > 0.0f)
            {
                EditorGUI.indentLevel++;
                m_MaterialEditor.ShaderProperty(doubleSidedMirrorEnable, StylesBase.doubleSidedMirrorEnableText);
                EditorGUI.indentLevel--;
            }

            if (materialID != null)
                m_MaterialEditor.ShaderProperty(materialID, StylesBase.materialIDText);

            m_MaterialEditor.ShaderProperty(enablePerPixelDisplacement, StylesBase.enablePerPixelDisplacementText);
            if (enablePerPixelDisplacement.floatValue > 0.0f)
            {
                EditorGUI.indentLevel++;
                m_MaterialEditor.ShaderProperty(ppdMinSamples, StylesBase.ppdMinSamplesText);
                m_MaterialEditor.ShaderProperty(ppdMaxSamples, StylesBase.ppdMaxSamplesText);
                ppdMinSamples.floatValue = Mathf.Min(ppdMinSamples.floatValue, ppdMaxSamples.floatValue);
                m_MaterialEditor.ShaderProperty(ppdLodThreshold, StylesBase.ppdLodThresholdText);
                m_MaterialEditor.ShaderProperty(depthOffsetEnable, StylesBase.depthOffsetEnableText);
                EditorGUI.indentLevel--;
            }

            EditorGUI.indentLevel--;

            // Display tessellation option if it exist
            if (tessellationMode != null)
            {
                GUILayout.Label(StylesBase.tessellationText, EditorStyles.boldLabel);
                EditorGUI.indentLevel++;
                TessellationModePopup();
                m_MaterialEditor.ShaderProperty(tessellationFactor, StylesBase.tessellationFactorText);
                m_MaterialEditor.ShaderProperty(tessellationFactorMinDistance, StylesBase.tessellationFactorMinDistanceText);
                m_MaterialEditor.ShaderProperty(tessellationFactorMaxDistance, StylesBase.tessellationFactorMaxDistanceText);
                // clamp min distance to be below max distance
                tessellationFactorMinDistance.floatValue = Math.Min(tessellationFactorMaxDistance.floatValue, tessellationFactorMinDistance.floatValue);
                m_MaterialEditor.ShaderProperty(tessellationFactorTriangleSize, StylesBase.tessellationFactorTriangleSizeText);
                if ((TessellationMode)tessellationMode.floatValue == TessellationMode.Phong ||
                    (TessellationMode)tessellationMode.floatValue == TessellationMode.DisplacementPhong)
                {
                    m_MaterialEditor.ShaderProperty(tessellationShapeFactor, StylesBase.tessellationShapeFactorText);
                }
                if (doubleSidedEnable.floatValue == 0.0)
                {
                    m_MaterialEditor.ShaderProperty(tessellationBackFaceCullEpsilon, StylesBase.tessellationBackFaceCullEpsilonText);
                }
                m_MaterialEditor.ShaderProperty(tessellationObjectScale, StylesBase.tessellationObjectScaleText);
                EditorGUI.indentLevel--;
            }
        }
        static public void SetKeyword(Material m, string keyword, bool state)
        {
            if (state)
                m.EnableKeyword(keyword);
            else
                m.DisableKeyword(keyword);
        }

        // All Setup Keyword functions must be static. It allow to create script to automatically update the shaders with a script if ocde change
        static public void SetupBaseKeywords(Material material)
        {
            bool alphaTestEnable = material.GetFloat(kAlphaCutoffEnabled) > 0.0f;
            SurfaceType surfaceType = (SurfaceType)material.GetFloat(kSurfaceType);
            BlendMode blendMode = (BlendMode)material.GetFloat(kBlendMode);
            bool doubleSidedEnable = material.GetFloat(kDoubleSidedEnable) > 0.0f;
            bool doubleSidedMirrorEnable = material.GetFloat(kDoubleSidedMirrorEnable) > 0.0f;
            
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

            if (doubleSidedEnable)
            {
                material.SetInt("_CullMode", (int)UnityEngine.Rendering.CullMode.Off);
                if (doubleSidedMirrorEnable)
                {
                    // Flip mode (in tangent space)
                    material.SetVector("_DoubleSidedConstants", new Vector3(-1.0f, -1.0f, -1.0f));
                }
                else
                {
                    // Mirror mode (in tangent space)
                    material.SetVector("_DoubleSidedConstants", new Vector3(1.0f, 1.0f, -1.0f));
                }
            }
            else
            {
                material.SetInt("_CullMode", (int)UnityEngine.Rendering.CullMode.Back);
            }

            SetKeyword(material, "_DOUBLESIDED_ON", doubleSidedEnable);
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
                // Disable all passes except debug material
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

            material.SetInt("_StencilRef", (int)material.GetFloat(kMaterialID)); // See 'StencilBits'.

            bool enablePerPixelDisplacement = material.GetFloat(kEnablePerPixelDisplacement) > 0.0f;
            SetKeyword(material, "_PER_PIXEL_DISPLACEMENT", enablePerPixelDisplacement);

            if (material.HasProperty(kTessellationMode))
            {
                TessellationMode tessMode = (TessellationMode)material.GetFloat(kTessellationMode);

                if (tessMode == TessellationMode.Phong)
                {
                    material.DisableKeyword("_TESSELLATION_DISPLACEMENT");
                    material.DisableKeyword("_TESSELLATION_DISPLACEMENT_PHONG");
                }
                else if (tessMode == TessellationMode.Displacement)
                {
                    material.EnableKeyword("_TESSELLATION_DISPLACEMENT");
                    material.DisableKeyword("_TESSELLATION_DISPLACEMENT_PHONG");
                }
                else
                {
                    material.DisableKeyword("_TESSELLATION_DISPLACEMENT");
                    material.EnableKeyword("_TESSELLATION_DISPLACEMENT_PHONG");
                }

                bool tessellationObjectScaleEnable = material.GetFloat(kTessellationObjectScale) == 1.0;
                SetKeyword(material, "_TESSELLATION_OBJECT_SCALE", tessellationObjectScaleEnable);
            }
        }

        // Dedicated to emissive - for emissive Enlighten/PVR
        protected void DoEmissionArea(Material material)
        {
            // Emission for GI?
            if (ShouldEmissionBeEnabled(material))
            {
                if (m_MaterialEditor.EmissionEnabledProperty())
                {
                    // change the GI flag and fix it up with emissive as black if necessary
                    m_MaterialEditor.LightmapEmissionFlagsProperty(MaterialEditor.kMiniTextureFieldLabelIndentLevel, true);
                }
            }
        }

        public void ShaderPropertiesGUI(Material material)
        {
            // Use default labelWidth
            EditorGUIUtility.labelWidth = 0f;

            // Detect any changes to the material
            EditorGUI.BeginChangeCheck();
            {
                BaseMaterialPropertiesGUI();
                EditorGUILayout.Space();

                EditorGUILayout.Space();
                MaterialPropertiesGUI();

                DoEmissionArea(material);
                m_MaterialEditor.EnableInstancingField();
            }

            if (EditorGUI.EndChangeCheck())
            {
                foreach (var obj in m_MaterialEditor.targets)
                    SetupMaterialKeywordsInternal((Material)obj);
            }
        }

        // This is call by the inspector
        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] props)
        {
            m_MaterialEditor = materialEditor;
            // We should always do this call at the beginning
            m_MaterialEditor.serializedObject.Update();

            // MaterialProperties can be animated so we do not cache them but fetch them every event to ensure animated values are updated correctly
            FindBaseMaterialProperties(props);
			FindMaterialProperties(props);

            Material material = materialEditor.target as Material;
            ShaderPropertiesGUI(material);

            // We should always do this call at the end
            m_MaterialEditor.serializedObject.ApplyModifiedProperties();
        }
    }
} // namespace UnityEditor
