using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
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
            public static GUIContent doubleSidedNormalModeText = new GUIContent("Normal mode", "This will modify the normal base on the selected mode. Mirror: Mirror the normal with vertex normal plane, Flip: Flip the normal");
            public static GUIContent depthOffsetEnableText = new GUIContent("Enable Depth Offset", "EnableDepthOffset on this shader (Use with heightmap)");

            // Displacement mapping (POM, tessellation, per vertex)
            //public static GUIContent enablePerPixelDisplacementText = new GUIContent("Enable Per Pixel Displacement", "");

            public static GUIContent displacementModeText = new GUIContent("Displacement mode", "Apply heightmap displacement to the selected element: Vertex, pixel or tessellated vertex. Pixel displacement must be use with flat surfaces, it is an expensive features and typical usage is paved road.");
            public static GUIContent lockWithObjectScaleText = new GUIContent("Lock with object scale", "Displacement mapping will take the absolute value of the scale of the object into account.");
            public static GUIContent lockWithTilingRateText = new GUIContent("Lock with height map tiling rate", "Displacement mapping will take the absolute value of the tiling rate of the height map into account.");

            public static GUIContent enableMotionVectorForVertexAnimationText = new GUIContent("Enable MotionVector For Vertex Animation", "This will enable an object motion vector pass for this material. Useful if wind animation is enabled or if displacement map is animated");

            // Material ID
            public static GUIContent materialIDText = new GUIContent("Material type", "Subsurface Scattering: enable for translucent materials such as skin, vegetation, fruit, marble, wax and milk.");

            // Per pixel displacement
            public static GUIContent ppdMinSamplesText = new GUIContent("Minimum steps", "Minimum steps (texture sample) to use with per pixel displacement mapping");
            public static GUIContent ppdMaxSamplesText = new GUIContent("Maximum steps", "Maximum steps (texture sample) to use with per pixel displacement mapping");
            public static GUIContent ppdLodThresholdText = new GUIContent("Fading mip level start", "Starting heightmap mipmap lod number where the parallax occlusion mapping effect start to disappear");
            public static GUIContent ppdPrimitiveLength = new GUIContent("Primitive length", "Dimensions of the primitive (with the scale of 1) to which the per-pixel displacement mapping is being applied. For example, the standard quad is 1 x 1 meter, while the standard plane is 10 x 10 meters.");
            public static GUIContent ppdPrimitiveWidth = new GUIContent("Primitive width", "Dimensions of the primitive (with the scale of 1) to which the per-pixel displacement mapping is being applied. For example, the standard quad is 1 x 1 meter, while the standard plane is 10 x 10 meters.");

            // Tessellation
            public static string tessellationModeStr = "Tessellation Mode";
            public static readonly string[] tessellationModeNames = Enum.GetNames(typeof(TessellationMode));

            public static GUIContent tessellationText = new GUIContent("Tessellation options", "Tessellation options");
            public static GUIContent tessellationFactorText = new GUIContent("Tessellation factor", "This value is the tessellation factor use for tessellation, higher mean more tessellated. Above 15 is costly. Maximum tessellation factor is 15 on XBone / PS4");
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

            public static GUIContent supportDBufferText = new GUIContent("Enable Decal", "Allow to specify if the material can receive decal or not");
        }

        public enum DoubleSidedNormalMode
        {
            Flip,
            Mirror
        }

        public enum TessellationMode
        {
            None,
            Phong
        }

        public enum DisplacementMode
        {
            None,
            Vertex,
            Pixel,
            Tessellation
        }

        public enum MaterialId
        {
            LitSSS = 0,
            LitStandard = 1,
            LitAniso = 2,
            LitClearCoat = 3,
            LitSpecular = 4,
            LitIridescence = 5,
        };

        public enum HeightmapParametrization
        {
            MinMax = 0,
            Amplitude = 1
        }

        protected MaterialProperty doubleSidedNormalMode = null;
        protected const string kDoubleSidedNormalMode = "_DoubleSidedNormalMode";
        protected MaterialProperty depthOffsetEnable = null;
        protected const string kDepthOffsetEnable = "_DepthOffsetEnable";

        // Properties
        // Material ID
        protected MaterialProperty materialID  = null;
        protected const string     kMaterialID = "_MaterialID";

        protected const string kStencilRef = "_StencilRef";
        protected const string kStencilWriteMask = "_StencilWriteMask";
        protected const string kStencilRefMV = "_StencilRefMV";
        protected const string kStencilWriteMaskMV = "_StencilWriteMaskMV";

        protected MaterialProperty displacementMode = null;
        protected const string kDisplacementMode = "_DisplacementMode";
        protected MaterialProperty displacementLockObjectScale = null;
        protected const string kDisplacementLockObjectScale = "_DisplacementLockObjectScale";
        protected MaterialProperty displacementLockTilingScale = null;
        protected const string kDisplacementLockTilingScale = "_DisplacementLockTilingScale";

        protected MaterialProperty enableMotionVectorForVertexAnimation = null;
        protected const string kEnableMotionVectorForVertexAnimation = "_EnableMotionVectorForVertexAnimation";

        // Per pixel displacement params
        protected MaterialProperty ppdMinSamples = null;
        protected const string kPpdMinSamples = "_PPDMinSamples";
        protected MaterialProperty ppdMaxSamples = null;
        protected const string kPpdMaxSamples = "_PPDMaxSamples";
        protected MaterialProperty ppdLodThreshold = null;
        protected const string kPpdLodThreshold = "_PPDLodThreshold";
        protected MaterialProperty ppdPrimitiveLength = null;
        protected const string kPpdPrimitiveLength = "_PPDPrimitiveLength";
        protected MaterialProperty ppdPrimitiveWidth = null;
        protected const string kPpdPrimitiveWidth = "_PPDPrimitiveWidth";
        protected MaterialProperty invPrimScale = null;
        protected const string kInvPrimScale = "_InvPrimScale";

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

        // Decal
        protected MaterialProperty supportDBuffer = null;
        protected const string kSupportDBuffer = "_SupportDBuffer";


        protected override void FindBaseMaterialProperties(MaterialProperty[] props)
        {
            base.FindBaseMaterialProperties(props);

            doubleSidedNormalMode = FindProperty(kDoubleSidedNormalMode, props);
            depthOffsetEnable = FindProperty(kDepthOffsetEnable, props);

            // MaterialID
            materialID = FindProperty(kMaterialID, props);

            displacementMode = FindProperty(kDisplacementMode, props);
            displacementLockObjectScale = FindProperty(kDisplacementLockObjectScale, props);
            displacementLockTilingScale = FindProperty(kDisplacementLockTilingScale, props);

            enableMotionVectorForVertexAnimation = FindProperty(kEnableMotionVectorForVertexAnimation, props);

            // Per pixel displacement
            ppdMinSamples = FindProperty(kPpdMinSamples, props);
            ppdMaxSamples = FindProperty(kPpdMaxSamples, props);
            ppdLodThreshold = FindProperty(kPpdLodThreshold, props);
            ppdPrimitiveLength = FindProperty(kPpdPrimitiveLength, props);
            ppdPrimitiveWidth  = FindProperty(kPpdPrimitiveWidth, props);
            invPrimScale = FindProperty(kInvPrimScale, props);

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

            // Decal
            supportDBuffer = FindProperty(kSupportDBuffer, props);
        }

        void TessellationModePopup()
        {
            EditorGUI.showMixedValue = tessellationMode.hasMixedValue;
            var mode = (TessellationMode)tessellationMode.floatValue;

            EditorGUI.BeginChangeCheck();
            mode = (TessellationMode)EditorGUILayout.Popup(StylesBaseLit.tessellationModeStr, (int)mode, StylesBaseLit.tessellationModeNames);
            if (EditorGUI.EndChangeCheck())
            {
                m_MaterialEditor.RegisterPropertyChangeUndo("Tessellation Mode");
                tessellationMode.floatValue = (float)mode;
            }

            EditorGUI.showMixedValue = false;
        }

        protected abstract void UpdateDisplacement();

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

            m_MaterialEditor.ShaderProperty(materialID, StylesBaseLit.materialIDText);

            m_MaterialEditor.ShaderProperty(supportDBuffer, StylesBaseLit.supportDBufferText);

            m_MaterialEditor.ShaderProperty(enableMotionVectorForVertexAnimation, StylesBaseLit.enableMotionVectorForVertexAnimationText);

            EditorGUI.BeginChangeCheck();
            m_MaterialEditor.ShaderProperty(displacementMode, StylesBaseLit.displacementModeText);
            if(EditorGUI.EndChangeCheck())
            {
                UpdateDisplacement();
            }

            if ((DisplacementMode)displacementMode.floatValue != DisplacementMode.None)
            {
                EditorGUI.indentLevel++;
                m_MaterialEditor.ShaderProperty(displacementLockObjectScale, StylesBaseLit.lockWithObjectScaleText);
                m_MaterialEditor.ShaderProperty(displacementLockTilingScale, StylesBaseLit.lockWithTilingRateText);
                EditorGUI.indentLevel--;
            }

            if ((DisplacementMode)displacementMode.floatValue == DisplacementMode.Pixel)
            {
                EditorGUILayout.Space();
                EditorGUI.indentLevel++;
                m_MaterialEditor.ShaderProperty(ppdMinSamples, StylesBaseLit.ppdMinSamplesText);
                m_MaterialEditor.ShaderProperty(ppdMaxSamples, StylesBaseLit.ppdMaxSamplesText);
                ppdMinSamples.floatValue = Mathf.Min(ppdMinSamples.floatValue, ppdMaxSamples.floatValue);
                m_MaterialEditor.ShaderProperty(ppdLodThreshold, StylesBaseLit.ppdLodThresholdText);
                m_MaterialEditor.ShaderProperty(ppdPrimitiveLength, StylesBaseLit.ppdPrimitiveLength);
                ppdPrimitiveLength.floatValue = Mathf.Max(0.01f, ppdPrimitiveLength.floatValue);
                m_MaterialEditor.ShaderProperty(ppdPrimitiveWidth, StylesBaseLit.ppdPrimitiveWidth);
                ppdPrimitiveWidth.floatValue = Mathf.Max(0.01f, ppdPrimitiveWidth.floatValue);
                invPrimScale.vectorValue = new Vector4(1.0f / ppdPrimitiveLength.floatValue, 1.0f / ppdPrimitiveWidth.floatValue); // Precompute
                m_MaterialEditor.ShaderProperty(depthOffsetEnable, StylesBaseLit.depthOffsetEnableText);
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
                    case DoubleSidedNormalMode.Mirror: // Mirror mode (in tangent space)
                        material.SetVector("_DoubleSidedConstants", new Vector4(1.0f, 1.0f, -1.0f, 0.0f));
                        break;

                    case DoubleSidedNormalMode.Flip: // Flip mode (in tangent space)
                        material.SetVector("_DoubleSidedConstants", new Vector4(-1.0f, -1.0f, -1.0f, 0.0f));
                        break;
                }
            }

            // Set the reference value for the stencil test.
            int stencilRef = (int)StencilLightingUsage.RegularLighting;
            if ((int)material.GetFloat(kMaterialID) == (int)BaseLitGUI.MaterialId.LitSSS)
            {
                stencilRef = (int)StencilLightingUsage.SplitLighting;
            }
            // As we tag both during velocity pass and Gbuffer pass we need a separate state and we need to use the write mask
            material.SetInt(kStencilRef, stencilRef);
            material.SetInt(kStencilWriteMask, (int)HDRenderPipeline.StencilBitMask.LightingMask);
            material.SetInt(kStencilRefMV, (int)HDRenderPipeline.StencilBitMask.ObjectVelocity);
            material.SetInt(kStencilWriteMaskMV, (int)HDRenderPipeline.StencilBitMask.ObjectVelocity);

            bool enableDisplacement = (DisplacementMode)material.GetFloat(kDisplacementMode) != DisplacementMode.None;
            bool enableVertexDisplacement = (DisplacementMode)material.GetFloat(kDisplacementMode) == DisplacementMode.Vertex;
            bool enablePixelDisplacement = (DisplacementMode)material.GetFloat(kDisplacementMode) == DisplacementMode.Pixel;
            bool enableTessellationDisplacement = ((DisplacementMode)material.GetFloat(kDisplacementMode) == DisplacementMode.Tessellation) && material.HasProperty(kTessellationMode);

            CoreUtils.SetKeyword(material, "_VERTEX_DISPLACEMENT", enableVertexDisplacement);
            CoreUtils.SetKeyword(material, "_PIXEL_DISPLACEMENT", enablePixelDisplacement);
            // Only set if tessellation exist
            CoreUtils.SetKeyword(material, "_TESSELLATION_DISPLACEMENT", enableTessellationDisplacement);

            bool displacementLockObjectScale = material.GetFloat(kDisplacementLockObjectScale) > 0.0;
            bool displacementLockTilingScale = material.GetFloat(kDisplacementLockTilingScale) > 0.0;
            // Tessellation reuse vertex flag.
            CoreUtils.SetKeyword(material, "_VERTEX_DISPLACEMENT_LOCK_OBJECT_SCALE", displacementLockObjectScale && (enableVertexDisplacement || enableTessellationDisplacement));
            CoreUtils.SetKeyword(material, "_PIXEL_DISPLACEMENT_LOCK_OBJECT_SCALE", displacementLockObjectScale && enablePixelDisplacement);
            CoreUtils.SetKeyword(material, "_DISPLACEMENT_LOCK_TILING_SCALE", displacementLockTilingScale && enableDisplacement);

            bool windEnabled = material.GetFloat(kWindEnabled) > 0.0f;
            CoreUtils.SetKeyword(material, "_VERTEX_WIND", windEnabled);

            // Depth offset is only enabled if per pixel displacement is
            bool depthOffsetEnable = (material.GetFloat(kDepthOffsetEnable) > 0.0f) && enablePixelDisplacement;
            CoreUtils.SetKeyword(material, "_DEPTHOFFSET_ON", depthOffsetEnable);

            if (material.HasProperty(kTessellationMode))
            {
                TessellationMode tessMode = (TessellationMode)material.GetFloat(kTessellationMode);
                CoreUtils.SetKeyword(material, "_TESSELLATION_PHONG", tessMode == TessellationMode.Phong);
            }

            SetupMainTexForAlphaTestGI("_BaseColorMap", "_BaseColor", material);

            // Use negation so we don't create keyword by default
            CoreUtils.SetKeyword(material, "_DISABLE_DBUFFER", material.GetFloat(kSupportDBuffer) == 0.0);
        }

        static public void SetupBaseLitMaterialPass(Material material)
        {
            SetupBaseUnlitMaterialPass(material);

            material.SetShaderPassEnabled(HDShaderPassNames.s_MotionVectorsStr, material.GetFloat(kEnableMotionVectorForVertexAnimation) > 0.0f);
        }
    }
} // namespace UnityEditor
