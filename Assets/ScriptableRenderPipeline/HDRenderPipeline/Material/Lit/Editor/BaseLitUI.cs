using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    // A Material can be authored from the shader graph or by hand. When written by hand we need to provide an inspector.
    // Such a Material will share some properties between it various variant (shader graph variant or hand authored variant).
    // This is the purpose of BaseLitGUI. It contain all properties that are common to all Material based on Lit template.
    // For the default hand written Lit material see LitUI.cs that contain specific properties for our default implementation.
    public abstract class BaseLitGUI : BaseUnlitGUI
    {
        protected static class StylesBaseLit
        {
            public static GUIContent doubleSidedMirrorEnableText = new GUIContent("Mirror normal", "This will mirror the normal with vertex normal plane if enabled, else flip the normal");
            public static GUIContent depthOffsetEnableText = new GUIContent("Enable Depth Offset", "EnableDepthOffset on this shader (Use with heightmap)");

            // Material ID
            public static GUIContent materialIDText = new GUIContent("Material type", "Subsurface Scattering: enable for translucent materials such as skin, vegetation, fruit, marble, wax and milk.");

            // Per pixel displacement
            public static GUIContent enablePerPixelDisplacementText = new GUIContent("Enable Per Pixel Displacement", "");
            public static GUIContent ppdMinSamplesText = new GUIContent("Minimum samples", "Minimum samples to use with per pixel displacement mapping");
            public static GUIContent ppdMaxSamplesText = new GUIContent("Maximum samples", "Maximum samples to use with per pixel displacement mapping");
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
            public static GUIContent tessellationObjectScaleText = new GUIContent("Enable object scale", "Tessellation displacement will take into account the object scale - Only work with uniform positive scale");
            public static GUIContent tessellationTilingScaleText = new GUIContent("Enable tiling scale", "Tessellation displacement will take into account the tiling scale - Only work with uniform positive scale");
        }

        public enum TessellationMode
        {
            Phong,
            Displacement,
            DisplacementPhong,
        }

        protected MaterialProperty doubleSidedMirrorEnable = null;
        protected const string kDoubleSidedMirrorEnable = "_DoubleSidedMirrorEnable";
        protected MaterialProperty depthOffsetEnable = null;
        protected const string kDepthOffsetEnable = "_DepthOffsetEnable";

        // Properties
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
        protected MaterialProperty tessellationTilingScale = null;
        protected const string kTessellationTilingScale = "_TessellationTilingScale";

        protected override void FindBaseMaterialProperties(MaterialProperty[] props)
        {
            base.FindBaseMaterialProperties(props);

            doubleSidedMirrorEnable = FindProperty(kDoubleSidedMirrorEnable, props);
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
            tessellationTilingScale = FindProperty(kTessellationTilingScale, props, false);
        }

        void TessellationModePopup()
        {
            EditorGUI.showMixedValue = tessellationMode.hasMixedValue;
            var mode = (TessellationMode)tessellationMode.floatValue;

            EditorGUI.BeginChangeCheck();
            mode = (TessellationMode)EditorGUILayout.Popup(StylesBaseLit.tessellationModeText, (int)mode, StylesBaseLit.tessellationModeNames);
            if (EditorGUI.EndChangeCheck())
            {
                m_MaterialEditor.RegisterPropertyChangeUndo("Tessellation Mode");
                tessellationMode.floatValue = (float)mode;
            }

            EditorGUI.showMixedValue = false;
        }

        protected override void BaseMaterialPropertiesGUI()
        {
            base.BaseMaterialPropertiesGUI();

            // This follow double sided option
            if (doubleSidedEnable.floatValue > 0.0f)
            {
                EditorGUI.indentLevel++;
                m_MaterialEditor.ShaderProperty(doubleSidedMirrorEnable, StylesBaseLit.doubleSidedMirrorEnableText);
                EditorGUI.indentLevel--;
            }

            if (materialID != null)
                m_MaterialEditor.ShaderProperty(materialID, StylesBaseLit.materialIDText);

            m_MaterialEditor.ShaderProperty(enablePerPixelDisplacement, StylesBaseLit.enablePerPixelDisplacementText);
            if (enablePerPixelDisplacement.floatValue > 0.0f)
            {
                EditorGUI.indentLevel++;
                m_MaterialEditor.ShaderProperty(ppdMinSamples, StylesBaseLit.ppdMinSamplesText);
                m_MaterialEditor.ShaderProperty(ppdMaxSamples, StylesBaseLit.ppdMaxSamplesText);
                ppdMinSamples.floatValue = Mathf.Min(ppdMinSamples.floatValue, ppdMaxSamples.floatValue);
                m_MaterialEditor.ShaderProperty(ppdLodThreshold, StylesBaseLit.ppdLodThresholdText);
                m_MaterialEditor.ShaderProperty(depthOffsetEnable, StylesBaseLit.depthOffsetEnableText);
                EditorGUI.indentLevel--;
            }

            EditorGUI.indentLevel--;

            // Display tessellation option if it exist
            if (tessellationMode != null)
            {
                GUILayout.Label(StylesBaseLit.tessellationText, EditorStyles.boldLabel);
                EditorGUI.indentLevel++;
                TessellationModePopup();
                m_MaterialEditor.ShaderProperty(tessellationFactor, StylesBaseLit.tessellationFactorText);
                m_MaterialEditor.ShaderProperty(tessellationFactorMinDistance, StylesBaseLit.tessellationFactorMinDistanceText);
                m_MaterialEditor.ShaderProperty(tessellationFactorMaxDistance, StylesBaseLit.tessellationFactorMaxDistanceText);
                // clamp min distance to be below max distance
                tessellationFactorMinDistance.floatValue = Math.Min(tessellationFactorMaxDistance.floatValue, tessellationFactorMinDistance.floatValue);
                m_MaterialEditor.ShaderProperty(tessellationFactorTriangleSize, StylesBaseLit.tessellationFactorTriangleSizeText);
                if ((TessellationMode)tessellationMode.floatValue == TessellationMode.Phong ||
                    (TessellationMode)tessellationMode.floatValue == TessellationMode.DisplacementPhong)
                {
                    m_MaterialEditor.ShaderProperty(tessellationShapeFactor, StylesBaseLit.tessellationShapeFactorText);
                }
                if (doubleSidedEnable.floatValue == 0.0)
                {
                    m_MaterialEditor.ShaderProperty(tessellationBackFaceCullEpsilon, StylesBaseLit.tessellationBackFaceCullEpsilonText);
                }
                m_MaterialEditor.ShaderProperty(tessellationObjectScale, StylesBaseLit.tessellationObjectScaleText);
                m_MaterialEditor.ShaderProperty(tessellationTilingScale, StylesBaseLit.tessellationTilingScaleText);
                EditorGUI.indentLevel--;
            }
        }

        // All Setup Keyword functions must be static. It allow to create script to automatically update the shaders with a script if ocde change
        static public void SetupBaseLitKeywords(Material material)
        {
            SetupBaseUnlitKeywords(material);

            bool doubleSidedEnable = material.GetFloat(kDoubleSidedEnable) > 0.0f;
            bool doubleSidedMirrorEnable = material.GetFloat(kDoubleSidedMirrorEnable) > 0.0f;

            if (doubleSidedEnable)
            {
                if (doubleSidedMirrorEnable)
                {
                    // Mirror mode (in tangent space)
                    material.SetVector("_DoubleSidedConstants", new Vector4(1.0f, 1.0f, -1.0f, 0.0f));
                }
                else
                {
                    // Flip mode (in tangent space)
                    material.SetVector("_DoubleSidedConstants", new Vector4(-1.0f, -1.0f, -1.0f, 0.0f));
                }
            }

            bool depthOffsetEnable = material.GetFloat(kDepthOffsetEnable) > 0.0f;
            SetKeyword(material, "_DEPTHOFFSET_ON", depthOffsetEnable);

            int stencilRef = (int)UnityEngine.Experimental.Rendering.HDPipeline.StencilBits.Standard; // See 'StencilBits'.
            if (material.HasProperty(kMaterialID))
            {
                int materialID = (int)material.GetFloat(kMaterialID);

                switch (materialID)
                {
                   case (int)UnityEngine.Experimental.Rendering.HDPipeline.Lit.MaterialId.LitSSS:
                        stencilRef = (int)UnityEngine.Experimental.Rendering.HDPipeline.StencilBits.SSS;
                        break;
                    case (int)UnityEngine.Experimental.Rendering.HDPipeline.Lit.MaterialId.LitStandard:
                        stencilRef = (int)UnityEngine.Experimental.Rendering.HDPipeline.StencilBits.Standard;
                        break;
                    default:
                        stencilRef = 1 + materialID;
                        break;
                }
            }
            material.SetInt("_StencilRef", stencilRef);

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

                bool tessellationObjectScaleEnable = material.GetFloat(kTessellationObjectScale) > 0.0;
                SetKeyword(material, "_TESSELLATION_OBJECT_SCALE", tessellationObjectScaleEnable);

                bool tessellationTilingScaleEnable = material.GetFloat(kTessellationTilingScale) > 0.0;
                SetKeyword(material, "_TESSELLATION_TILING_SCALE", tessellationTilingScaleEnable);
            }
        }

        static public void SetupBaseLitMaterialPass(Material material)
        {
            bool distortionEnable = material.GetFloat(kDistortionEnable) > 0.0f;
            bool distortionOnly = material.GetFloat(kDistortionOnly) > 0.0f;

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
        }
    }
} // namespace UnityEditor
