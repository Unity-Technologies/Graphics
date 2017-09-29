using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;

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
            public static GUIContent doubleSidedNormalModeText = new GUIContent("Normal mode", "This will modify the normal base on the selected mode. None: untouch, Mirror: Mirror the normal with vertex normal plane, Flip: Flip the normal");
            public static GUIContent depthOffsetEnableText = new GUIContent("Enable Depth Offset", "EnableDepthOffset on this shader (Use with heightmap)");

            // Material ID
            public static GUIContent materialIDText = new GUIContent("Material type", "Subsurface Scattering: enable for translucent materials such as skin, vegetation, fruit, marble, wax and milk.");

            // Per pixel displacement
            public static GUIContent enablePerPixelDisplacementText = new GUIContent("Enable Per Pixel Displacement", "Per pixel displacement work best with flat surfaces. This is an expensive features and should be enable wisely. Typical use case is paved road.");
            public static GUIContent ppdMinSamplesText = new GUIContent("Minimum steps", "Minimum steps (texture sample) to use with per pixel displacement mapping");
            public static GUIContent ppdMaxSamplesText = new GUIContent("Maximum steps", "Maximum steps (texture sample) to use with per pixel displacement mapping");
            public static GUIContent ppdLodThresholdText = new GUIContent("Fading mip level start", "Starting heightmap mipmap lod number where the parallax occlusion mapping effect start to disappear");
            public static GUIContent perPixelDisplacementObjectScaleText = new GUIContent("Lock with object scale", "Per Pixel displacement will take into account the tiling scale - Only work with uniform positive scale");            

            // Vertex displacement
            public static string vertexDisplacementText = "Vertex displacement";

            public static GUIContent enableVertexDisplacementText = new GUIContent("Enable vertex displacement", "Use heightmap as a displacement map. Displacement map is use to move vertex position in local space");
            public static GUIContent vertexDisplacementObjectScaleText = new GUIContent("Lock with object scale", "Vertex displacement will take into account the object scale - Only work with uniform positive scale");
            public static GUIContent vertexDisplacementTilingScaleText = new GUIContent("Lock with heightmap tiling", "Vertex displacement will take into account the tiling scale - Only work with uniform positive scale");

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

            // Vertex animation
            public static string vertexAnimation = "Vertex animation";

            // Wind
            public static GUIContent windText = new GUIContent("Enable Wind");
            public static GUIContent windInitialBendText = new GUIContent("Initial Bend");
            public static GUIContent windStiffnessText = new GUIContent("Stiffness");
            public static GUIContent windDragText = new GUIContent("Drag");
            public static GUIContent windShiverDragText = new GUIContent("Shiver Drag");
            public static GUIContent windShiverDirectionalityText = new GUIContent("Shiver Directionality");
        }

        public enum DoubleSidedNormalMode
        {
           None,
           Mirror,
           Flip
        }

        public enum TessellationMode
        {
            None,
            Phong
        }

        protected MaterialProperty doubleSidedNormalMode = null;
        protected const string kDoubleSidedNormalMode = "_DoubleSidedNormalMode";
        protected MaterialProperty depthOffsetEnable = null;
        protected const string kDepthOffsetEnable = "_DepthOffsetEnable";

        // Properties
        // Material ID
        protected MaterialProperty materialID  = null;
        protected const string     kMaterialID = "_MaterialID";

        protected const string     kStencilRef = "_StencilRef";

        // Per pixel displacement params
        protected MaterialProperty enablePerPixelDisplacement = null;
        protected const string kEnablePerPixelDisplacement = "_EnablePerPixelDisplacement";
        protected MaterialProperty ppdMinSamples = null;
        protected const string kPpdMinSamples = "_PPDMinSamples";
        protected MaterialProperty ppdMaxSamples = null;
        protected const string kPpdMaxSamples = "_PPDMaxSamples";
        protected MaterialProperty ppdLodThreshold = null;
        protected const string kPpdLodThreshold = "_PPDLodThreshold";
        protected MaterialProperty perPixelDisplacementObjectScale = null;
        protected const string kPerPixelDisplacementObjectScale = "_PerPixelDisplacementObjectScale";

        // Vertex displacement
        protected MaterialProperty enableVertexDisplacement = null;
        protected const string kEnableVertexDisplacement = "_EnableVertexDisplacement";
        protected MaterialProperty vertexDisplacementObjectScale = null;
        protected const string kVertexDisplacementObjectScale = "_VertexDisplacementObjectScale";
        protected MaterialProperty vertexDisplacementTilingScale = null;
        protected const string kVertexDisplacementTilingScale = "_VertexDisplacementTilingScale";

        // Wind
        protected MaterialProperty windEnable = null;
        protected const string kWindEnabled = "_EnableWind";
        protected MaterialProperty windInitialBend = null;
        protected const string kWindInitialBend = "_InitialBend";
        protected MaterialProperty windStiffness = null;
        protected const string kWindStiffness = "_Stiffness";
        protected MaterialProperty windDrag = null;
        protected const string kWindDrag = "_Drag";
        protected MaterialProperty windShiverDrag = null;
        protected const string kWindShiverDrag = "_ShiverDrag";
        protected MaterialProperty windShiverDirectionality = null;
        protected const string kWindShiverDirectionality = "_ShiverDirectionality";

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

        protected override void FindBaseMaterialProperties(MaterialProperty[] props)
        {
            base.FindBaseMaterialProperties(props);

            doubleSidedNormalMode = FindProperty(kDoubleSidedNormalMode, props);
            depthOffsetEnable = FindProperty(kDepthOffsetEnable, props);

            // MaterialID
            materialID = FindProperty(kMaterialID, props, false); // LayeredLit is force to be standard for now, so materialID could not exist

            // Per pixel displacement
            enablePerPixelDisplacement = FindProperty(kEnablePerPixelDisplacement, props);
            ppdMinSamples = FindProperty(kPpdMinSamples, props);
            ppdMaxSamples = FindProperty(kPpdMaxSamples, props);
            ppdLodThreshold = FindProperty(kPpdLodThreshold, props);
            perPixelDisplacementObjectScale = FindProperty(kPerPixelDisplacementObjectScale, props);            

            // vertex displacement
            enableVertexDisplacement = FindProperty(kEnableVertexDisplacement, props);
            vertexDisplacementObjectScale = FindProperty(kVertexDisplacementObjectScale, props);
            vertexDisplacementTilingScale = FindProperty(kVertexDisplacementTilingScale, props);

            // tessellation specific, silent if not found
            tessellationMode = FindProperty(kTessellationMode, props, false);
            tessellationFactor = FindProperty(kTessellationFactor, props, false);
            tessellationFactorMinDistance = FindProperty(kTessellationFactorMinDistance, props, false);
            tessellationFactorMaxDistance = FindProperty(kTessellationFactorMaxDistance, props, false);
            tessellationFactorTriangleSize = FindProperty(kTessellationFactorTriangleSize, props, false);
            tessellationShapeFactor = FindProperty(kTessellationShapeFactor, props, false);
            tessellationBackFaceCullEpsilon = FindProperty(kTessellationBackFaceCullEpsilon, props, false);

            // Wind
            windEnable = FindProperty(kWindEnabled, props);
            windInitialBend = FindProperty(kWindInitialBend, props);
            windStiffness = FindProperty(kWindStiffness, props);
            windDrag = FindProperty(kWindDrag, props);
            windShiverDrag = FindProperty(kWindShiverDrag, props);
            windShiverDirectionality = FindProperty(kWindShiverDirectionality, props);
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

            EditorGUI.indentLevel++;

            // This follow double sided option
            if (doubleSidedEnable.floatValue > 0.0f)
            {
                EditorGUI.indentLevel++;
                m_MaterialEditor.ShaderProperty(doubleSidedNormalMode, StylesBaseLit.doubleSidedNormalModeText);
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
                //m_MaterialEditor.ShaderProperty(perPixelDisplacementObjectScale, StylesBaseLit.perPixelDisplacementObjectScaleText);
                m_MaterialEditor.ShaderProperty(depthOffsetEnable, StylesBaseLit.depthOffsetEnableText);                
                EditorGUI.indentLevel--;
            }

            EditorGUI.indentLevel--;

            // Vertex displacement options
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(StylesBaseLit.vertexDisplacementText, EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            m_MaterialEditor.ShaderProperty(enableVertexDisplacement, StylesBaseLit.enableVertexDisplacementText);
            if (enableVertexDisplacement.floatValue > 0.0f)
            {
                EditorGUI.indentLevel++;
                m_MaterialEditor.ShaderProperty(vertexDisplacementObjectScale, StylesBaseLit.vertexDisplacementObjectScaleText);
                m_MaterialEditor.ShaderProperty(vertexDisplacementTilingScale, StylesBaseLit.vertexDisplacementTilingScaleText);
                EditorGUI.indentLevel--;
            }

            EditorGUI.indentLevel--;

            // Display tessellation option if it exist
            if (tessellationMode != null)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField(StylesBaseLit.tessellationText, EditorStyles.boldLabel);
                EditorGUI.indentLevel++;
                TessellationModePopup();
                m_MaterialEditor.ShaderProperty(tessellationFactor, StylesBaseLit.tessellationFactorText);
                m_MaterialEditor.ShaderProperty(tessellationFactorMinDistance, StylesBaseLit.tessellationFactorMinDistanceText);
                m_MaterialEditor.ShaderProperty(tessellationFactorMaxDistance, StylesBaseLit.tessellationFactorMaxDistanceText);
                // clamp min distance to be below max distance
                tessellationFactorMinDistance.floatValue = Math.Min(tessellationFactorMaxDistance.floatValue, tessellationFactorMinDistance.floatValue);
                m_MaterialEditor.ShaderProperty(tessellationFactorTriangleSize, StylesBaseLit.tessellationFactorTriangleSizeText);
                if ((TessellationMode)tessellationMode.floatValue == TessellationMode.Phong)
                {
                    m_MaterialEditor.ShaderProperty(tessellationShapeFactor, StylesBaseLit.tessellationShapeFactorText);
                }
                if (doubleSidedEnable.floatValue == 0.0)
                {
                    m_MaterialEditor.ShaderProperty(tessellationBackFaceCullEpsilon, StylesBaseLit.tessellationBackFaceCullEpsilonText);
                }
                EditorGUI.indentLevel--;
            }
        }

        protected override void VertexAnimationPropertiesGUI()
        {
            EditorGUILayout.LabelField(StylesBaseLit.vertexAnimation, EditorStyles.boldLabel);

            EditorGUI.indentLevel++;

            m_MaterialEditor.ShaderProperty(windEnable, StylesBaseLit.windText);
            if (!windEnable.hasMixedValue && windEnable.floatValue > 0.0f)
            {
                EditorGUI.indentLevel++;
                m_MaterialEditor.ShaderProperty(windInitialBend, StylesBaseLit.windInitialBendText);
                m_MaterialEditor.ShaderProperty(windStiffness, StylesBaseLit.windStiffnessText);
                m_MaterialEditor.ShaderProperty(windDrag, StylesBaseLit.windDragText);
                m_MaterialEditor.ShaderProperty(windShiverDrag, StylesBaseLit.windShiverDragText);
                m_MaterialEditor.ShaderProperty(windShiverDirectionality, StylesBaseLit.windShiverDirectionalityText);
                EditorGUI.indentLevel--;
            }

            EditorGUI.indentLevel--;
        }

        // All Setup Keyword functions must be static. It allow to create script to automatically update the shaders with a script if code change
        static public void SetupBaseLitKeywords(Material material)
        {
            SetupBaseUnlitKeywords(material);

            bool doubleSidedEnable = material.GetFloat(kDoubleSidedEnable) > 0.0f;

            if (doubleSidedEnable)
            {
                DoubleSidedNormalMode doubleSidedNormalMode = (DoubleSidedNormalMode)material.GetFloat(kDoubleSidedNormalMode);
                switch (doubleSidedNormalMode)
                {
                    case DoubleSidedNormalMode.None:
                        material.SetVector("_DoubleSidedConstants", new Vector4(1.0f, 1.0f, 1.0f, 0.0f));
                        break;

                    case DoubleSidedNormalMode.Mirror: // Mirror mode (in tangent space)
                        material.SetVector("_DoubleSidedConstants", new Vector4(1.0f, 1.0f, -1.0f, 0.0f));
                        break;

                    case DoubleSidedNormalMode.Flip: // Flip mode (in tangent space)
                        material.SetVector("_DoubleSidedConstants", new Vector4(-1.0f, -1.0f, -1.0f, 0.0f));
                        break;
                }
            }

            // Depth offset is only enabled if per pixel displacement is
            bool depthOffsetEnable = (material.GetFloat(kDepthOffsetEnable) > 0.0f) && (material.GetFloat(kEnablePerPixelDisplacement) > 0.0f);
            SetKeyword(material, "_DEPTHOFFSET_ON", depthOffsetEnable);

            // Set the reference value for the stencil test.
            int stencilRef = (int)StencilLightingUsage.RegularLighting;
            if (material.HasProperty(kMaterialID))
            {
                if ((int)material.GetFloat(kMaterialID) == (int)UnityEngine.Experimental.Rendering.HDPipeline.Lit.MaterialId.LitSSS)
                {
                    stencilRef = (int)StencilLightingUsage.SplitLighting;
                }
            }
            material.SetInt(kStencilRef, stencilRef);

            bool enablePerPixelDisplacement = material.GetFloat(kEnablePerPixelDisplacement) > 0.0f;
            SetKeyword(material, "_PER_PIXEL_DISPLACEMENT", enablePerPixelDisplacement);

            bool perPixelDisplacementObjectScale = material.GetFloat(kPerPixelDisplacementObjectScale) > 0.0;
            SetKeyword(material, "_PER_PIXEL_DISPLACEMENT_OBJECT_SCALE", perPixelDisplacementObjectScale && enablePerPixelDisplacement);

            bool enableVertexDisplacement = material.GetFloat(kEnableVertexDisplacement) > 0.0f;
            SetKeyword(material, "_VERTEX_DISPLACEMENT", enableVertexDisplacement);

            bool vertexDisplacementObjectScaleEnable = material.GetFloat(kVertexDisplacementObjectScale) > 0.0;
            SetKeyword(material, "_VERTEX_DISPLACEMENT_OBJECT_SCALE", vertexDisplacementObjectScaleEnable && enableVertexDisplacement);

            bool vertexDisplacementTilingScaleEnable = material.GetFloat(kVertexDisplacementTilingScale) > 0.0;
            SetKeyword(material, "_VERTEX_DISPLACEMENT_TILING_SCALE", vertexDisplacementTilingScaleEnable && enableVertexDisplacement);

            bool windEnabled = material.GetFloat(kWindEnabled) > 0.0f;
            SetKeyword(material, "_VERTEX_WIND", windEnabled);

            if (material.HasProperty(kTessellationMode))
            {
                TessellationMode tessMode = (TessellationMode)material.GetFloat(kTessellationMode);
                SetKeyword(material, "_TESSELLATION_PHONG", tessMode == TessellationMode.Phong);
            }
        }

        static public void SetupBaseLitMaterialPass(Material material)
        {
            bool distortionEnable = material.GetFloat(kDistortionEnable) > 0.0f;
            bool distortionOnly = material.GetFloat(kDistortionOnly) > 0.0f;

            if (distortionEnable && distortionOnly)
            {
                // Disable all passes except distortion (setup in BaseUnlitUI.cs) and debug passes (to visualize distortion)
                material.SetShaderPassEnabled("GBuffer", false);
                material.SetShaderPassEnabled("GBufferDisplayDebug", true);
                material.SetShaderPassEnabled("Meta", false);
                material.SetShaderPassEnabled("ShadowCaster", false);
                material.SetShaderPassEnabled("DepthOnly", false);
                material.SetShaderPassEnabled("MotionVectors", false);
                material.SetShaderPassEnabled("Forward", false);
                material.SetShaderPassEnabled("ForwardDisplayDebug", true);
            }
            else
            {
                // Enable all passes except distortion (setup in BaseUnlitUI.cs)
                material.SetShaderPassEnabled("GBuffer", true);
                material.SetShaderPassEnabled("GBufferDisplayDebug", true);
                material.SetShaderPassEnabled("Meta", true);
                material.SetShaderPassEnabled("ShadowCaster", true);
                material.SetShaderPassEnabled("DepthOnly", true);
                material.SetShaderPassEnabled("MotionVectors", true);
                material.SetShaderPassEnabled("Forward", true);
                material.SetShaderPassEnabled("ForwardDisplayDebug", true);
            }
        }
    }
} // namespace UnityEditor
