using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;
using UnityEditor.ShaderGraph;
using System.Linq;

// Include material common properties names
using static UnityEngine.Rendering.HighDefinition.HDMaterialProperties;

namespace UnityEditor.Rendering.HighDefinition
{
    internal class SurfaceOptionUIBlock : MaterialUIBlock
    {
        [Flags]
        public enum Features
        {
            None                        = 0,
            Surface                     = 1 << 0,
            BlendMode                   = 1 << 1,
            DoubleSided                 = 1 << 2,
            AlphaCutoff                 = 1 << 3,
            AlphaCutoffThreshold        = 1 << 4,
            AlphaCutoffShadowThreshold  = 1 << 5,
            DoubleSidedNormalMode       = 1 << 6,
            BackThenFrontRendering      = 1 << 7,
            ReceiveSSR                  = 1 << 8,
            ReceiveDecal                = 1 << 9,
            ShowAfterPostProcessPass    = 1 << 10,
            AlphaToMask                 = 1 << 11,
            ShowPrePassAndPostPass      = 1 << 12,
            ShowDepthOffsetOnly         = 1 << 13,
            PreserveSpecularLighting    = 1 << 14,
            Unlit                       = Surface | BlendMode | DoubleSided | AlphaCutoff | AlphaCutoffThreshold | AlphaCutoffShadowThreshold| AlphaToMask | BackThenFrontRendering | ShowAfterPostProcessPass | ShowPrePassAndPostPass | ShowDepthOffsetOnly,
            Lit                         = All ^ SurfaceOptionUIBlock.Features.ShowAfterPostProcessPass ^ ShowDepthOffsetOnly, // Lit can't be display in after postprocess pass
            All                         = ~0,
        }

        internal static class Styles
        {
            public const string optionText = "Surface Options";
            public const string surfaceTypeText = "Surface Type";
            public const string renderingPassText = "Rendering Pass";
            public const string blendModeText = "Blending Mode";
            public const string notSupportedInMultiEdition = "Multiple Different Values";

            public static readonly string[] surfaceTypeNames = Enum.GetNames(typeof(SurfaceType));
            public static readonly string[] blendModeNames = Enum.GetNames(typeof(BlendMode));
            public static readonly int[] blendModeValues = Enum.GetValues(typeof(BlendMode)) as int[];

            public static GUIContent transparentPrepassText = new GUIContent("Appear in Refraction", "When enabled, HDRP handles objects with this Material before the refraction pass.");

            public static GUIContent doubleSidedEnableText = new GUIContent("Double-Sided", "When enabled, HDRP renders both faces of the polygons that make up meshes using this Material. Disables backface culling.");

            public static GUIContent useShadowThresholdText = new GUIContent("Use Shadow Threshold", "Enable separate threshold for shadow pass");
            public static GUIContent alphaCutoffEnableText = new GUIContent("Alpha Clipping", "When enabled, HDRP processes Alpha Clipping for this Material.");
            public static GUIContent alphaCutoffText = new GUIContent("Threshold", "Controls the threshold for the Alpha Clipping effect.");
            public static GUIContent alphaCutoffShadowText = new GUIContent("Shadow Threshold", "Controls the threshold for shadow pass alpha clipping.");
            public static GUIContent alphaCutoffPrepassText = new GUIContent("Prepass Threshold", "Controls the threshold for transparent depth prepass alpha clipping.");
            public static GUIContent alphaCutoffPostpassText = new GUIContent("Postpass Threshold", "Controls the threshold for transparent depth postpass alpha clipping.");
            public static GUIContent alphaToMaskText = new GUIContent("Alpha To Mask", "When enabled and using MSAA, HDRP enables alpha to coverage during the depth prepass.");
            public static GUIContent transparentDepthPostpassEnableText = new GUIContent("Transparent Depth Postpass", "When enabled, HDRP renders a depth postpass for transparent objects. This improves post-processing effects like depth of field.");
            public static GUIContent transparentDepthPrepassEnableText = new GUIContent("Transparent Depth Prepass", "When enabled, HDRP renders a depth prepass for transparent GameObjects. This improves sorting.");
            public static GUIContent transparentBackfaceEnableText = new GUIContent("Back Then Front Rendering", "When enabled, HDRP renders the back face and then the front face, in two separate draw calls, to better sort transparent meshes.");
            public static GUIContent transparentWritingMotionVecText = new GUIContent("Transparent Writes Motion Vectors", "When enabled, transparent objects write motion vectors, these replace what was previously rendered in the buffer.");

            public static GUIContent zWriteEnableText = new GUIContent("Depth Write", "When enabled, transparent objects write to the depth buffer.");
            public static GUIContent transparentZTestText = new GUIContent("Depth Test", "Set the comparison function to use during the Z Testing.");
            public static GUIContent rayTracingText = new GUIContent("Recursive Rendering (Preview)");
            public static GUIContent rayTracingTextInfo = new GUIContent("When enabled, if you enabled ray tracing in your project and a recursive rendering volume override is active, Unity uses recursive rendering to render the GameObject.");

            public static GUIContent transparentSortPriorityText = new GUIContent("Sorting Priority", "Sets the sort priority (from -100 to 100) of transparent meshes using this Material. HDRP uses this value to calculate the sorting order of all transparent meshes on screen.");
            public static GUIContent enableTransparentFogText = new GUIContent("Receive fog", "When enabled, this Material can receive fog.");
            public static GUIContent transparentCullModeText = new GUIContent("Cull Mode", "For transparent objects, change the cull mode of the object.");
            public static GUIContent enableBlendModePreserveSpecularLightingText = new GUIContent("Preserve specular lighting", "When enabled, blending only affects diffuse lighting, allowing for correct specular lighting on transparent meshes that use this Material. This parameter is only supported when the material's refraction model is set to None.");

            // Lit properties
            public static GUIContent doubleSidedNormalModeText = new GUIContent("Normal Mode", "Specifies the method HDRP uses to modify the normal base.\nMirror: Mirrors the normals with the vertex normal plane.\nFlip: Flips the normal.");
            public static GUIContent depthOffsetEnableText = new GUIContent("Depth Offset", "When enabled, HDRP uses the Height Map to calculate the depth offset for this Material.");

            // Displacement mapping (POM, tessellation, per vertex)
            //public static GUIContent enablePerPixelDisplacementText = new GUIContent("Per Pixel Displacement", "");

            public static GUIContent displacementModeText = new GUIContent("Displacement Mode", "Specifies the method HDRP uses to apply height map displacement to the selected element: Vertex, pixel, or tessellated vertex.\n You must use flat surfaces for Pixel displacement.");
            public static GUIContent lockWithObjectScaleText = new GUIContent("Lock With Object Scale", "When enabled, displacement mapping takes the absolute value of the scale of the object into account.");
            public static GUIContent lockWithTilingRateText = new GUIContent("Lock With Height Map Tiling Rate", "When enabled, displacement mapping takes the absolute value of the tiling rate of the height map into account.");

            // Material ID
            public static GUIContent materialIDText = new GUIContent("Material Type", "Specifies additional feature for this Material. Customize you Material with different settings depending on which Material Type you select.");
            public static GUIContent transmissionEnableText = new GUIContent("Transmission", "When enabled HDRP processes the transmission effect for subsurface scattering. Simulates the translucency of the object.");
            public static string transparentSSSErrorMessage = "Transparent Materials With SubSurface Scattering is not supported.";

            // Per pixel displacement
            public static GUIContent ppdMinSamplesText = new GUIContent("Minimum Steps", "Controls the minimum number of steps HDRP uses for per pixel displacement mapping.");
            public static GUIContent ppdMaxSamplesText = new GUIContent("Maximum Steps", "Controls the maximum number of steps HDRP uses for per pixel displacement mapping.");
            public static GUIContent ppdLodThresholdText = new GUIContent("Fading Mip Level Start", "Controls the Height Map mip level where the parallax occlusion mapping effect begins to disappear.");
            public static GUIContent ppdPrimitiveLength = new GUIContent("Primitive Length", "Sets the length of the primitive (with the scale of 1) to which HDRP applies per-pixel displacement mapping. For example, the standard quad is 1 x 1 meter, while the standard plane is 10 x 10 meters.");
            public static GUIContent ppdPrimitiveWidth = new GUIContent("Primitive Width", "Sets the width of the primitive (with the scale of 1) to which HDRP applies per-pixel displacement mapping. For example, the standard quad is 1 x 1 meter, while the standard plane is 10 x 10 meters.");

            public static GUIContent supportDecalsText = new GUIContent("Receive Decals", "Enable to allow Materials to receive decals.");

            public static GUIContent enableGeometricSpecularAAText = new GUIContent("Geometric Specular AA", "When enabled, HDRP reduces specular aliasing on high density meshes (particularly useful when the not using a normal map).");
            public static GUIContent specularAAScreenSpaceVarianceText = new GUIContent("Screen space variance", "Controls the strength of the Specular AA reduction. Higher values give a more blurry result and less aliasing.");
            public static GUIContent specularAAThresholdText = new GUIContent("Threshold", "Controls the effect of Specular AA reduction. A values of 0 does not apply reduction, higher values allow higher reduction.");

            // SSR
            public static GUIContent receivesSSRText = new GUIContent("Receive SSR/SSGI", "When enabled, this Material can receive screen space reflections and screen space global illumination.");
            public static GUIContent receivesSSRTransparentText = new GUIContent("Receive SSR Transparent", "When enabled, this Material can receive screen space reflections.");

            public static GUIContent opaqueCullModeText = new GUIContent("Cull Mode", "For opaque objects, change the cull mode of the object.");
            
            public static string afterPostProcessInfoBox = "If After post-process objects don't render, make sure to enable \"After Post-process\" in the frame settings.\nAfter post-process material wont be ZTested. Enable the \"ZTest For After PostProcess\" checkbox in the Frame Settings to force the depth-test if the TAA is disabled.";
        }

        // Properties common to Unlit and Lit
        MaterialProperty surfaceType = null;

        MaterialProperty alphaCutoffEnable = null;
        const string kAlphaCutoffEnabled = "_AlphaCutoffEnable";
        MaterialProperty useShadowThreshold = null;
        const string kUseShadowThreshold = "_UseShadowThreshold";
        MaterialProperty alphaCutoff = null;
        const string kAlphaCutoff = "_AlphaCutoff";
        MaterialProperty alphaCutoffShadow = null;
        const string kAlphaCutoffShadow = "_AlphaCutoffShadow";
        MaterialProperty alphaCutoffPrepass = null;
        const string kAlphaCutoffPrepass = "_AlphaCutoffPrepass";
        MaterialProperty alphaCutoffPostpass = null;
        const string kAlphaCutoffPostpass = "_AlphaCutoffPostpass";
        MaterialProperty alphaToMask = null;
        const string kAlphaToMask = kAlphaToMaskInspector;
        MaterialProperty transparentDepthPrepassEnable = null;
        const string kTransparentDepthPrepassEnable = "_TransparentDepthPrepassEnable";
        MaterialProperty transparentDepthPostpassEnable = null;
        const string kTransparentDepthPostpassEnable = "_TransparentDepthPostpassEnable";
        MaterialProperty transparentBackfaceEnable = null;
        const string kTransparentBackfaceEnable = "_TransparentBackfaceEnable";
        MaterialProperty transparentSortPriority = null;
        const string kTransparentSortPriority = HDMaterialProperties.kTransparentSortPriority;
        MaterialProperty transparentWritingMotionVec = null;
        MaterialProperty doubleSidedEnable = null;
        const string kDoubleSidedEnable = "_DoubleSidedEnable";
        MaterialProperty blendMode = null;
        const string kBlendMode = "_BlendMode";
        MaterialProperty enableBlendModePreserveSpecularLighting = null;
        MaterialProperty enableFogOnTransparent = null;
        const string kEnableFogOnTransparent = "_EnableFogOnTransparent";

        // Lit properties
        MaterialProperty doubleSidedNormalMode = null;
        const string kDoubleSidedNormalMode = "_DoubleSidedNormalMode";
        MaterialProperty materialID  = null;
        MaterialProperty supportDecals = null;
        const string kSupportDecals = "_SupportDecals";
        MaterialProperty enableGeometricSpecularAA = null;
        const string kEnableGeometricSpecularAA = "_EnableGeometricSpecularAA";
        MaterialProperty specularAAScreenSpaceVariance = null;
        const string kSpecularAAScreenSpaceVariance = "_SpecularAAScreenSpaceVariance";
        MaterialProperty specularAAThreshold = null;
        const string kSpecularAAThreshold = "_SpecularAAThreshold";
        MaterialProperty transmissionEnable = null;
        const string kTransmissionEnable = "_TransmissionEnable";

        // Per pixel displacement params
        MaterialProperty ppdMinSamples = null;
        const string kPpdMinSamples = "_PPDMinSamples";
        MaterialProperty ppdMaxSamples = null;
        const string kPpdMaxSamples = "_PPDMaxSamples";
        MaterialProperty ppdLodThreshold = null;
        const string kPpdLodThreshold = "_PPDLodThreshold";
        MaterialProperty ppdPrimitiveLength = null;
        const string kPpdPrimitiveLength = "_PPDPrimitiveLength";
        MaterialProperty ppdPrimitiveWidth = null;
        const string kPpdPrimitiveWidth = "_PPDPrimitiveWidth";
        MaterialProperty invPrimScale = null;
        const string kInvPrimScale = "_InvPrimScale";

        // SSR
        MaterialProperty receivesSSR = null;
        const string kReceivesSSR = "_ReceivesSSR";
        MaterialProperty receivesSSRTransparent = null;
        const string kReceivesSSRTransparent = "_ReceivesSSRTransparent";

        MaterialProperty displacementMode = null;
        const string kDisplacementMode = "_DisplacementMode";
        MaterialProperty displacementLockObjectScale = null;
        const string kDisplacementLockObjectScale = "_DisplacementLockObjectScale";
        MaterialProperty displacementLockTilingScale = null;
        const string kDisplacementLockTilingScale = "_DisplacementLockTilingScale";

        MaterialProperty depthOffsetEnable = null;

        MaterialProperty tessellationMode = null;
        const string kTessellationMode = "_TessellationMode";

        MaterialProperty[] heightMap = new MaterialProperty[kMaxLayerCount];
        const string kHeightMap = "_HeightMap";
        MaterialProperty[] heightAmplitude = new MaterialProperty[kMaxLayerCount];
        const string kHeightAmplitude = "_HeightAmplitude";
        MaterialProperty[] heightCenter = new MaterialProperty[kMaxLayerCount];
        const string kHeightCenter = "_HeightCenter";
        MaterialProperty[] heightPoMAmplitude = new MaterialProperty[kMaxLayerCount];
        const string kHeightPoMAmplitude = "_HeightPoMAmplitude";
        MaterialProperty[] heightTessCenter = new MaterialProperty[kMaxLayerCount];
        const string kHeightTessCenter = "_HeightTessCenter";
        MaterialProperty[] heightTessAmplitude = new MaterialProperty[kMaxLayerCount];
        const string kHeightTessAmplitude = "_HeightTessAmplitude";
        MaterialProperty[] heightMin = new MaterialProperty[kMaxLayerCount];
        const string kHeightMin = "_HeightMin";
        MaterialProperty[] heightMax = new MaterialProperty[kMaxLayerCount];
        const string kHeightMax = "_HeightMax";
        MaterialProperty[] heightOffset = new MaterialProperty[kMaxLayerCount];
        const string kHeightOffset = "_HeightOffset";
        MaterialProperty[] heightParametrization = new MaterialProperty[kMaxLayerCount];
        const string kHeightParametrization = "_HeightMapParametrization";

        // Refraction (for show pre-refraction pass enum)
        protected MaterialProperty refractionModel = null;

        MaterialProperty transparentZWrite = null;
        MaterialProperty stencilRef = null;
        MaterialProperty zTest = null;
        MaterialProperty transparentCullMode = null;
        MaterialProperty opaqueCullMode = null;
        MaterialProperty rayTracing = null;

        SurfaceType defaultSurfaceType { get { return SurfaceType.Opaque; } }

        // start faking MaterialProperty for renderQueue
        bool renderQueueHasMultipleDifferentValue
        {
            get
            {
                if (materialEditor.targets.Length < 2)
                    return false;

                int firstRenderQueue = renderQueue;
                for (int index = 1; index < materialEditor.targets.Length; ++index)
                {
                    if ((materialEditor.targets[index] as Material).renderQueue != firstRenderQueue)
                        return true;
                }
                return false;
            }
        }

        // TODO: does not support material multi-editing
        int renderQueue
        {
            get => (materialEditor.targets[0] as Material).renderQueue;
            set
            {
                foreach (var target in materialEditor.targets)
                {
                    (target as Material).renderQueue = value;
                }
            }
        }

        SurfaceType surfaceTypeValue
        {
            get { return surfaceType != null ? (SurfaceType)surfaceType.floatValue : defaultSurfaceType; }
        }

        List<string> m_RenderingPassNames = new List<string>();
        List<int> m_RenderingPassValues = new List<int>();

        Expandable  m_ExpandableBit;
        Features    m_Features;
        int         m_LayerCount;

        public SurfaceOptionUIBlock(Expandable expandableBit, int layerCount = 1, Features features = Features.All)
        {
            m_ExpandableBit = expandableBit;
            m_Features = features;
            m_LayerCount = layerCount;
        }

        public override void LoadMaterialProperties()
        {
            surfaceType = FindProperty(kSurfaceType);
            useShadowThreshold = FindProperty(kUseShadowThreshold);
            alphaCutoffEnable = FindProperty(kAlphaCutoffEnabled);
            alphaCutoff = FindProperty(kAlphaCutoff);

            alphaCutoffShadow = FindProperty(kAlphaCutoffShadow);
            alphaCutoffPrepass = FindProperty(kAlphaCutoffPrepass);
            alphaCutoffPostpass = FindProperty(kAlphaCutoffPostpass);
            alphaToMask = FindProperty(kAlphaToMask);
            transparentDepthPrepassEnable = FindProperty(kTransparentDepthPrepassEnable);
            transparentDepthPostpassEnable = FindProperty(kTransparentDepthPostpassEnable);

            if ((m_Features & Features.BackThenFrontRendering) != 0)
                transparentBackfaceEnable = FindProperty(kTransparentBackfaceEnable);

            transparentSortPriority = FindProperty(kTransparentSortPriority);

            transparentWritingMotionVec = FindProperty(kTransparentWritingMotionVec);

            if ((m_Features & Features.PreserveSpecularLighting) != 0)
                enableBlendModePreserveSpecularLighting = FindProperty(kEnableBlendModePreserveSpecularLighting);

            enableFogOnTransparent = FindProperty(kEnableFogOnTransparent);

            if ((m_Features & Features.DoubleSided) != 0)
                doubleSidedEnable = FindProperty(kDoubleSidedEnable);

            // Height
            heightMap = FindPropertyLayered(kHeightMap, m_LayerCount);
            heightAmplitude = FindPropertyLayered(kHeightAmplitude, m_LayerCount);
            heightCenter = FindPropertyLayered(kHeightCenter, m_LayerCount);
            heightPoMAmplitude = FindPropertyLayered(kHeightPoMAmplitude, m_LayerCount);
            heightMin = FindPropertyLayered(kHeightMin, m_LayerCount);
            heightMax = FindPropertyLayered(kHeightMax, m_LayerCount);
            heightTessCenter = FindPropertyLayered(kHeightTessCenter, m_LayerCount);
            heightTessAmplitude = FindPropertyLayered(kHeightTessAmplitude, m_LayerCount);
            heightOffset = FindPropertyLayered(kHeightOffset, m_LayerCount);
            heightParametrization = FindPropertyLayered(kHeightParametrization, m_LayerCount);

            blendMode = FindProperty(kBlendMode);

            transmissionEnable = FindProperty(kTransmissionEnable);

            if ((m_Features & Features.DoubleSidedNormalMode) != 0)
                doubleSidedNormalMode = FindProperty(kDoubleSidedNormalMode);
            depthOffsetEnable = FindProperty(kDepthOffsetEnable);

            // MaterialID
            materialID = FindProperty(kMaterialID);
            transmissionEnable = FindProperty(kTransmissionEnable);

            displacementMode = FindProperty(kDisplacementMode);
            displacementLockObjectScale = FindProperty(kDisplacementLockObjectScale);
            displacementLockTilingScale = FindProperty(kDisplacementLockTilingScale);

            // Per pixel displacement
            ppdMinSamples = FindProperty(kPpdMinSamples);
            ppdMaxSamples = FindProperty(kPpdMaxSamples);
            ppdLodThreshold = FindProperty(kPpdLodThreshold);
            ppdPrimitiveLength = FindProperty(kPpdPrimitiveLength);
            ppdPrimitiveWidth  = FindProperty(kPpdPrimitiveWidth);
            invPrimScale = FindProperty(kInvPrimScale);

            // tessellation specific, silent if not found
            tessellationMode = FindProperty(kTessellationMode);

            // Decal
            if ((m_Features & Features.ReceiveDecal) != 0)
            {
                supportDecals = FindProperty(kSupportDecals);
            }

            // specular AA
            enableGeometricSpecularAA = FindProperty(kEnableGeometricSpecularAA);
            specularAAScreenSpaceVariance = FindProperty(kSpecularAAScreenSpaceVariance);
            specularAAThreshold = FindProperty(kSpecularAAThreshold);

            // SSR
            if ((m_Features & Features.ReceiveSSR) != 0)
            {
                receivesSSR = FindProperty(kReceivesSSR);
                receivesSSRTransparent = FindProperty(kReceivesSSRTransparent);
            }

            transparentZWrite = FindProperty(kTransparentZWrite);
            stencilRef = FindProperty(kStencilRef);
            zTest = FindProperty(kZTestTransparent);
            transparentCullMode = FindProperty(kTransparentCullMode);
            opaqueCullMode = FindProperty(kOpaqueCullMode);
            rayTracing = FindProperty(kRayTracing);

            refractionModel = FindProperty(kRefractionModel);
        }

        public override void OnGUI()
        {
            using (var header = new MaterialHeaderScope(Styles.optionText, (uint)m_ExpandableBit, materialEditor))
            {
                if (header.expanded)
                    DrawSurfaceOptionGUI();
            }
        }

        void DrawSurfaceOptionGUI()
        {
            if ((m_Features & Features.Surface) != 0)
                DrawSurfaceGUI();

            if ((m_Features & Features.AlphaCutoff) != 0)
                DrawAlphaCutoffGUI();

            if ((m_Features & Features.DoubleSided) != 0)
                DrawDoubleSidedGUI();

            DrawLitSurfaceOptions();
        }

        bool AreMaterialsShaderGraphs() => materials.All(m => m.shader.IsShaderGraph());

        /// <summary>Returns false if there are multiple materials selected and they have different default values for propName</summary>
        float GetShaderDefaultFloatValue(string propName)
        {
            // It's okay to ignore all other materials here because if the material editor is displayed, the shader is the same for all materials
            var shader = materials[0].shader;
            return shader.GetPropertyDefaultFloatValue(shader.FindPropertyIndex(propName));
        }

        void DrawAlphaCutoffGUI()
        {
            // For shadergraphs we show this slider only if the feature is enabled in the shader settings.
            bool showAlphaClipThreshold = true;
            
            bool isShaderGraph = AreMaterialsShaderGraphs();
            if (isShaderGraph)
                showAlphaClipThreshold = GetShaderDefaultFloatValue(kAlphaCutoffEnabled) > 0.0f;

            if (showAlphaClipThreshold && alphaCutoffEnable != null)
                materialEditor.ShaderProperty(alphaCutoffEnable, Styles.alphaCutoffEnableText);

            if (showAlphaClipThreshold && alphaCutoffEnable != null && alphaCutoffEnable.floatValue == 1.0f)
            {
                EditorGUI.indentLevel++;

                if (showAlphaClipThreshold && alphaCutoff != null)
                    materialEditor.ShaderProperty(alphaCutoff, Styles.alphaCutoffText);

                if (showAlphaClipThreshold && (m_Features & Features.AlphaCutoffShadowThreshold) != 0)
                {
                    // For shadergraphs we show this slider only if the feature is enabled in the shader settings.
                    bool showUseShadowThreshold = useShadowThreshold != null;
                    if (isShaderGraph)
                        showUseShadowThreshold = GetShaderDefaultFloatValue(kUseShadowThreshold) > 0.0f;

                    if (showUseShadowThreshold)
                        materialEditor.ShaderProperty(useShadowThreshold, Styles.useShadowThresholdText);

                    if (alphaCutoffShadow != null && useShadowThreshold != null && useShadowThreshold.floatValue == 1.0f && (m_Features & Features.AlphaCutoffShadowThreshold) != 0)
                    {
                        EditorGUI.indentLevel++;
                        materialEditor.ShaderProperty(alphaCutoffShadow, Styles.alphaCutoffShadowText);
                        EditorGUI.indentLevel--;
                    }
                }

                if (showAlphaClipThreshold && (m_Features & Features.AlphaToMask) != 0)
                {
                    if (alphaToMask != null)
                        materialEditor.ShaderProperty(alphaToMask, Styles.alphaToMaskText);
                }

                // With transparent object and few specific materials like Hair, we need more control on the cutoff to apply
                // This allow to get a better sorting (with prepass), better shadow (better silhouettes fidelity) etc...
                if (showAlphaClipThreshold && surfaceTypeValue == SurfaceType.Transparent)
                {
                    // TODO: check if passes exists
                    if (transparentDepthPrepassEnable != null && transparentDepthPrepassEnable.floatValue == 1.0f)
                    {
                        if (alphaCutoffPrepass != null)
                            materialEditor.ShaderProperty(alphaCutoffPrepass, Styles.alphaCutoffPrepassText);
                    }

                    if (transparentDepthPostpassEnable != null && transparentDepthPostpassEnable.floatValue == 1.0f)
                    {
                        if (alphaCutoffPostpass != null)
                            materialEditor.ShaderProperty(alphaCutoffPostpass, Styles.alphaCutoffPostpassText);
                    }
                }
                EditorGUI.indentLevel--;
            }

            EditorGUI.showMixedValue = false;
        }

        void DrawDoubleSidedGUI()
        {
            // This function must finish with double sided option (see LitUI.cs)
            if (doubleSidedEnable != null)
                materialEditor.ShaderProperty(doubleSidedEnable, Styles.doubleSidedEnableText);
        }

        void DrawSurfaceGUI()
        {
            SurfaceTypePopup();

            if (surfaceTypeValue == SurfaceType.Transparent)
            {
                if (HDRenderQueue.k_RenderQueue_AfterPostProcessTransparent.Contains(renderQueue))
                {
                    if (!zTest.hasMixedValue && materials.All(m => m.GetTransparentZTest() != CompareFunction.Disabled))
                    {
                        ShowAfterPostProcessZTestInfoBox();
                    }
                }

                EditorGUI.indentLevel++;

                if (renderQueueHasMultipleDifferentValue)
                {
                    using (new EditorGUI.DisabledScope(true))
                        EditorGUILayout.LabelField(Styles.blendModeText, Styles.notSupportedInMultiEdition);
                }
                else if (blendMode != null)
                    BlendModePopup();

                if ((m_Features & Features.PreserveSpecularLighting) != 0)
                {
                    EditorGUI.indentLevel++; if (renderQueueHasMultipleDifferentValue)
                    {
                        using (new EditorGUI.DisabledScope(true))
                            EditorGUILayout.LabelField(Styles.enableBlendModePreserveSpecularLightingText, Styles.notSupportedInMultiEdition);
                    }
                    else if (enableBlendModePreserveSpecularLighting != null && blendMode != null)
                        materialEditor.ShaderProperty(enableBlendModePreserveSpecularLighting, Styles.enableBlendModePreserveSpecularLightingText);
                    EditorGUI.indentLevel--;
                }

                if (transparentSortPriority != null)
                {
                    EditorGUI.BeginChangeCheck();
                    materialEditor.ShaderProperty(transparentSortPriority, Styles.transparentSortPriorityText);
                    if (EditorGUI.EndChangeCheck())
                    {
                        transparentSortPriority.floatValue = HDRenderQueue.ClampsTransparentRangePriority((int)transparentSortPriority.floatValue);
                    }
                }

                if (enableFogOnTransparent != null)
                    materialEditor.ShaderProperty(enableFogOnTransparent, Styles.enableTransparentFogText);

                bool shaderHasBackThenFrontPass = materials.All(m => m.FindPass(HDShaderPassNames.s_TransparentBackfaceStr) != -1);
                if (shaderHasBackThenFrontPass && transparentBackfaceEnable != null)
                    materialEditor.ShaderProperty(transparentBackfaceEnable, Styles.transparentBackfaceEnableText);

                if ((m_Features & Features.ShowPrePassAndPostPass) != 0)
                {
                    bool shaderHasDepthPrePass = materials.All(m => m.FindPass(HDShaderPassNames.s_TransparentDepthPrepassStr) != -1);
                    if (shaderHasDepthPrePass && transparentDepthPrepassEnable != null)
                        materialEditor.ShaderProperty(transparentDepthPrepassEnable, Styles.transparentDepthPrepassEnableText);

                    bool shaderHasDepthPostPass = materials.All(m => m.FindPass(HDShaderPassNames.s_TransparentDepthPostpassStr) != -1);
                    if (shaderHasDepthPostPass && transparentDepthPostpassEnable != null)
                        materialEditor.ShaderProperty(transparentDepthPostpassEnable, Styles.transparentDepthPostpassEnableText);
                }

                if (transparentWritingMotionVec != null)
                    materialEditor.ShaderProperty(transparentWritingMotionVec, Styles.transparentWritingMotionVecText);

                if (transparentZWrite != null)
                    materialEditor.ShaderProperty(transparentZWrite, Styles.zWriteEnableText);

                if (zTest != null)
                    materialEditor.ShaderProperty(zTest, Styles.transparentZTestText);

                bool showTransparentCullMode = transparentCullMode != null && doubleSidedEnable.floatValue == 0;
                if (transparentBackfaceEnable != null)
                    showTransparentCullMode &= transparentBackfaceEnable.floatValue == 0;
                if (showTransparentCullMode)
                    materialEditor.ShaderProperty(transparentCullMode, Styles.transparentCullModeText);

                EditorGUI.indentLevel--;
            }
            else // SurfaceType.Opaque
            {
                EditorGUI.indentLevel++;
                if (doubleSidedEnable != null && doubleSidedEnable.floatValue == 0 && opaqueCullMode != null)
                    materialEditor.ShaderProperty(opaqueCullMode, Styles.opaqueCullModeText);
                else
                {
                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUILayout.Popup(Styles.opaqueCullModeText, 0, new string[] { "Off" });
                    EditorGUI.EndDisabledGroup();
                }
                EditorGUI.indentLevel--;
                if (HDRenderQueue.k_RenderQueue_AfterPostProcessOpaque.Contains(renderQueue))
                {
                    ShowAfterPostProcessZTestInfoBox();
                }
            }
        }

        void ShowAfterPostProcessZTestInfoBox()
        {
            EditorGUILayout.HelpBox(Styles.afterPostProcessInfoBox, MessageType.Info);
        }

        void SurfaceTypePopup()
        {
            if (surfaceType == null)
                return;

            // TODO: does not work with multi-selection
            Material material = materialEditor.target as Material;

            var mode = (SurfaceType)surfaceType.floatValue;
            var renderQueueType = HDRenderQueue.GetTypeByRenderQueueValue(material.renderQueue);
            bool alphaTest = material.HasProperty(kAlphaCutoffEnabled) && material.GetFloat(kAlphaCutoffEnabled) > 0.0f;
            bool receiveDecal = material.HasProperty(kSupportDecals) && material.GetFloat(kSupportDecals) > 0.0f;

            // Shader graph only property, used to transfer the render queue from the shader graph to the material,
            // because we can't use the renderqueue from the shader as we have to keep the renderqueue on the material side.
            if (material.HasProperty("_RenderQueueType"))
            {
                renderQueueType = (HDRenderQueue.RenderQueueType)material.GetFloat("_RenderQueueType");
            }
            // To know if we need to update the renderqueue, mainly happens if a material is created from a shader graph shader
            // with default render-states.
            bool renderQueueTypeMismatchRenderQueue = HDRenderQueue.GetTypeByRenderQueueValue(material.renderQueue) != renderQueueType;

            EditorGUI.showMixedValue = surfaceType.hasMixedValue;
            var newMode = (SurfaceType)EditorGUILayout.Popup(Styles.surfaceTypeText, (int)mode, Styles.surfaceTypeNames);
            if (newMode != mode) //EditorGUI.EndChangeCheck is called even if value remain the same after the popup. Prefer not to use it here
            {
                materialEditor.RegisterPropertyChangeUndo(Styles.surfaceTypeText);
                surfaceType.floatValue = (float)newMode;
            }
            EditorGUI.showMixedValue = false;

            bool isMixedRenderQueue = surfaceType.hasMixedValue || renderQueueHasMultipleDifferentValue;
            bool showAfterPostProcessPass = (m_Features & Features.ShowAfterPostProcessPass) != 0;
            bool showPreRefractionPass = refractionModel == null || refractionModel.floatValue == 0;
            bool showLowResolutionPass = true;

            EditorGUI.showMixedValue = isMixedRenderQueue;
            ++EditorGUI.indentLevel;

            if (newMode == SurfaceType.Transparent)
            {
                if (stencilRef != null && ((int)stencilRef.floatValue & (int)StencilUsage.SubsurfaceScattering) != 0)
                    EditorGUILayout.HelpBox(Styles.transparentSSSErrorMessage, MessageType.Error);
            }

            switch (mode)
            {
                case SurfaceType.Opaque:
                    //GetOpaqueEquivalent: prevent issue when switching surface type
                    HDRenderQueue.OpaqueRenderQueue renderQueueOpaqueType = HDRenderQueue.ConvertToOpaqueRenderQueue(HDRenderQueue.GetOpaqueEquivalent(renderQueueType));
                    var newRenderQueueOpaqueType = (HDRenderQueue.OpaqueRenderQueue)DoOpaqueRenderingPassPopup(Styles.renderingPassText, (int)renderQueueOpaqueType, showAfterPostProcessPass);
                    if (newRenderQueueOpaqueType != renderQueueOpaqueType || renderQueueTypeMismatchRenderQueue) //EditorGUI.EndChangeCheck is called even if value remain the same after the popup. Prefer not to use it here
                    {
                        materialEditor.RegisterPropertyChangeUndo("Rendering Pass");
                        renderQueueType = HDRenderQueue.ConvertFromOpaqueRenderQueue(newRenderQueueOpaqueType);
                        renderQueue = HDRenderQueue.ChangeType(renderQueueType, alphaTest: alphaTest, receiveDecal: receiveDecal);
                    }
                    break;
                case SurfaceType.Transparent:
                    //GetTransparentEquivalent: prevent issue when switching surface type
                    HDRenderQueue.TransparentRenderQueue renderQueueTransparentType = HDRenderQueue.ConvertToTransparentRenderQueue(HDRenderQueue.GetTransparentEquivalent(renderQueueType));
                    var newRenderQueueTransparentType = (HDRenderQueue.TransparentRenderQueue)DoTransparentRenderingPassPopup(Styles.renderingPassText, (int)renderQueueTransparentType, true, showLowResolutionPass, showAfterPostProcessPass);
                    if (newRenderQueueTransparentType != renderQueueTransparentType || renderQueueTypeMismatchRenderQueue) //EditorGUI.EndChangeCheck is called even if value remain the same after the popup. Prefer not to use it here
                    {
                        materialEditor.RegisterPropertyChangeUndo("Rendering Pass");
                        renderQueueType = HDRenderQueue.ConvertFromTransparentRenderQueue(newRenderQueueTransparentType);
                        renderQueue = HDRenderQueue.ChangeType(renderQueueType, offset: (int)transparentSortPriority.floatValue);
                    }
                    break;
                default:
                    throw new ArgumentException("Unknown SurfaceType");
            }
            --EditorGUI.indentLevel;
            EditorGUI.showMixedValue = false;

            if (material.HasProperty("_RenderQueueType"))
                material.SetFloat("_RenderQueueType", (float)renderQueueType);
        }

        void BlendModePopup()
        {
            EditorGUI.showMixedValue = blendMode.hasMixedValue;
            var mode = (BlendMode)blendMode.floatValue;

            EditorGUI.BeginChangeCheck();
            mode = (BlendMode)EditorGUILayout.IntPopup(Styles.blendModeText, (int)mode, Styles.blendModeNames, Styles.blendModeValues);
            if (EditorGUI.EndChangeCheck())
            {
                materialEditor.RegisterPropertyChangeUndo("Blend Mode");
                blendMode.floatValue = (float)mode;
            }

            EditorGUI.showMixedValue = false;
        }

        int DoOpaqueRenderingPassPopup(string text, int inputValue, bool afterPost)
        {
            // Build UI enums
            m_RenderingPassNames.Clear();
            m_RenderingPassValues.Clear();

            m_RenderingPassNames.Add("Default");
            m_RenderingPassValues.Add((int)HDRenderQueue.OpaqueRenderQueue.Default);

            if (afterPost)
            {
                m_RenderingPassNames.Add("After post-process");
                m_RenderingPassValues.Add((int)HDRenderQueue.OpaqueRenderQueue.AfterPostProcessing);
            }

            return EditorGUILayout.IntPopup(text, inputValue, m_RenderingPassNames.ToArray(), m_RenderingPassValues.ToArray());
        }

        int DoTransparentRenderingPassPopup(string text, int inputValue, bool refraction, bool lowRes, bool afterPost)
        {
            // Build UI enums
            m_RenderingPassNames.Clear();
            m_RenderingPassValues.Clear();

            if (refraction)
            {
                m_RenderingPassNames.Add("Before refraction");
                m_RenderingPassValues.Add((int)HDRenderQueue.TransparentRenderQueue.BeforeRefraction);
            }

            m_RenderingPassNames.Add("Default");
            m_RenderingPassValues.Add((int)HDRenderQueue.TransparentRenderQueue.Default);

            if (lowRes)
            {
                m_RenderingPassNames.Add("Low resolution");
                m_RenderingPassValues.Add((int)HDRenderQueue.TransparentRenderQueue.LowResolution);
            }

            if (afterPost)
            {
                m_RenderingPassNames.Add("After post-process");
                m_RenderingPassValues.Add((int)HDRenderQueue.TransparentRenderQueue.AfterPostProcessing);
            }

            return EditorGUILayout.IntPopup(text, inputValue, m_RenderingPassNames.ToArray(), m_RenderingPassValues.ToArray());
        }

        void DrawLitSurfaceOptions()
        {
            // This follow double sided option
            if (doubleSidedNormalMode != null && doubleSidedEnable != null && doubleSidedEnable.floatValue > 0.0f)
            {
                EditorGUI.indentLevel++;
                materialEditor.ShaderProperty(doubleSidedNormalMode, Styles.doubleSidedNormalModeText);
                EditorGUI.indentLevel--;
            }

            if (materialID != null)
            {
                materialEditor.ShaderProperty(materialID, Styles.materialIDText);

                if ((int)materialID.floatValue == (int)MaterialId.LitSSS)
                {
                    EditorGUI.indentLevel++;
                    materialEditor.ShaderProperty(transmissionEnable, Styles.transmissionEnableText);
                    EditorGUI.indentLevel--;
                }
            }

            // We only display the ray tracing option if the asset supports it (and the attributes exists in this shader)
            if ((RenderPipelineManager.currentPipeline as HDRenderPipeline).rayTracingSupported && rayTracing != null)
            {
                materialEditor.ShaderProperty(rayTracing, Styles.rayTracingText);
                if (rayTracing.floatValue == 1.0f)
                {
                    EditorGUILayout.HelpBox(Styles.rayTracingTextInfo.text, MessageType.Info, true);
                }
            }

            if (supportDecals != null)
            {
                materialEditor.ShaderProperty(supportDecals, Styles.supportDecalsText);
            }

            if (receivesSSR != null && receivesSSRTransparent != null)
            {
                // Based on the surface type, display the right recieveSSR option
                if (surfaceTypeValue == SurfaceType.Transparent)
                    materialEditor.ShaderProperty(receivesSSRTransparent, Styles.receivesSSRTransparentText);
                else
                    materialEditor.ShaderProperty(receivesSSR, Styles.receivesSSRText);
            }

            if (enableGeometricSpecularAA != null)
            {
                materialEditor.ShaderProperty(enableGeometricSpecularAA, Styles.enableGeometricSpecularAAText);

                if (enableGeometricSpecularAA.floatValue > 0.0)
                {
                    EditorGUI.indentLevel++;
                    materialEditor.ShaderProperty(specularAAScreenSpaceVariance, Styles.specularAAScreenSpaceVarianceText);
                    materialEditor.ShaderProperty(specularAAThreshold, Styles.specularAAThresholdText);
                    EditorGUI.indentLevel--;
                }
            }

            if ((m_Features & Features.ShowDepthOffsetOnly) != 0 && depthOffsetEnable != null)
            {
                // We only display Depth offset option when it's enabled in the ShaderGraph, otherwise the default value for depth offset is 0 does not make sense.
                if (!AreMaterialsShaderGraphs() || (AreMaterialsShaderGraphs() && GetShaderDefaultFloatValue(kDepthOffsetEnable) > 0.0f == true))
                    materialEditor.ShaderProperty(depthOffsetEnable, Styles.depthOffsetEnableText);
            }
            else if (displacementMode != null)
            {
                EditorGUI.BeginChangeCheck();
                FilterDisplacementMode();
                materialEditor.ShaderProperty(displacementMode, Styles.displacementModeText);
                if (EditorGUI.EndChangeCheck())
                {
                    for (int i = 0; i < m_LayerCount; i++)
                        UpdateDisplacement(i);
                }

                if ((DisplacementMode)displacementMode.floatValue != DisplacementMode.None)
                {
                    EditorGUI.indentLevel++;
                    materialEditor.ShaderProperty(displacementLockObjectScale, Styles.lockWithObjectScaleText);
                    materialEditor.ShaderProperty(displacementLockTilingScale, Styles.lockWithTilingRateText);
                    EditorGUI.indentLevel--;
                }

                if ((DisplacementMode)displacementMode.floatValue == DisplacementMode.Pixel)
                {
                    EditorGUILayout.Space();
                    EditorGUI.indentLevel++;
                    materialEditor.ShaderProperty(ppdMinSamples, Styles.ppdMinSamplesText);
                    materialEditor.ShaderProperty(ppdMaxSamples, Styles.ppdMaxSamplesText);
                    ppdMinSamples.floatValue = Mathf.Min(ppdMinSamples.floatValue, ppdMaxSamples.floatValue);
                    materialEditor.ShaderProperty(ppdLodThreshold, Styles.ppdLodThresholdText);
                    materialEditor.ShaderProperty(ppdPrimitiveLength, Styles.ppdPrimitiveLength);
                    ppdPrimitiveLength.floatValue = Mathf.Max(0.01f, ppdPrimitiveLength.floatValue);
                    materialEditor.ShaderProperty(ppdPrimitiveWidth, Styles.ppdPrimitiveWidth);
                    ppdPrimitiveWidth.floatValue = Mathf.Max(0.01f, ppdPrimitiveWidth.floatValue);
                    invPrimScale.vectorValue = new Vector4(1.0f / ppdPrimitiveLength.floatValue, 1.0f / ppdPrimitiveWidth.floatValue); // Precompute
                    materialEditor.ShaderProperty(depthOffsetEnable, Styles.depthOffsetEnableText);
                    EditorGUI.indentLevel--;
                }
            }
        }

        public void UpdateDisplacement(int layerIndex)
        {
            DisplacementMode displaceMode = (DisplacementMode)displacementMode.floatValue;
            if (displaceMode == DisplacementMode.Pixel)
            {
                heightAmplitude[layerIndex].floatValue = heightPoMAmplitude[layerIndex].floatValue * 0.01f; // Conversion centimeters to meters.
                heightCenter[layerIndex].floatValue = 1.0f; // PoM is always inward so base (0 height) is mapped to 1 in the texture
            }
            else
            {
                HeightmapParametrization parametrization = (HeightmapParametrization)heightParametrization[layerIndex].floatValue;
                if (parametrization == HeightmapParametrization.MinMax)
                {
                    float offset = heightOffset[layerIndex].floatValue;
                    float amplitude = (heightMax[layerIndex].floatValue - heightMin[layerIndex].floatValue);

                    heightAmplitude[layerIndex].floatValue = amplitude * 0.01f; // Conversion centimeters to meters.
                    heightCenter[layerIndex].floatValue = -(heightMin[layerIndex].floatValue + offset) / Mathf.Max(1e-6f, amplitude);
                }
                else
                {
                    float amplitude = heightTessAmplitude[layerIndex].floatValue;
                    heightAmplitude[layerIndex].floatValue = amplitude * 0.01f;
                    heightCenter[layerIndex].floatValue = -heightOffset[layerIndex].floatValue / Mathf.Max(1e-6f, amplitude) + heightTessCenter[layerIndex].floatValue;
                }
            }
        }

        void FilterDisplacementMode()
        {
            if (tessellationMode == null)
            {
                if ((DisplacementMode)displacementMode.floatValue == DisplacementMode.Tessellation)
                    displacementMode.floatValue = (float)DisplacementMode.None;
            }
            else
            {
                if ((DisplacementMode)displacementMode.floatValue == DisplacementMode.Pixel || (DisplacementMode)displacementMode.floatValue == DisplacementMode.Vertex)
                    displacementMode.floatValue = (float)DisplacementMode.None;
            }
        }
    }
}
