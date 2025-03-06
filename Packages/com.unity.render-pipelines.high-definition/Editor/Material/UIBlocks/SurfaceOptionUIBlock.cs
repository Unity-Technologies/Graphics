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
    /// <summary>
    /// The UI block that represents surface option properties for materials.
    /// </summary>
    public class SurfaceOptionUIBlock : MaterialUIBlock
    {
        /// <summary>
        /// Options for surface option features. This allows you to display or hide certain parts of the UI.
        /// </summary>
        [Flags]
        public enum Features
        {
            /// <summary>Displays the minimum surface option fields.</summary>
            None = 0,
            /// <summary>Displays the surface field.</summary>
            Surface = 1 << 0,
            /// <summary>Displays the blend mode field.</summary>
            BlendMode = 1 << 1,
            /// <summary>Displays the double sided field.</summary>
            DoubleSided = 1 << 2,
            /// <summary>Displays the alpha cutoff field.</summary>
            AlphaCutoff = 1 << 3,
            /// <summary>Displays the alpha cutoff threshold field.</summary>
            AlphaCutoffThreshold = 1 << 4,
            /// <summary>Displays the alpha cutoff shadow treshold field.</summary>
            AlphaCutoffShadowThreshold = 1 << 5,
            /// <summary>Displays the double sided normal mode field.</summary>
            DoubleSidedNormalMode = 1 << 6,
            /// <summary>Displays the back then front rendering field.</summary>
            BackThenFrontRendering = 1 << 7,
            /// <summary>Displays the receive ssr field.</summary>
            ReceiveSSR = 1 << 8,
            /// <summary>Displays the receive decal field.</summary>
            ReceiveDecal = 1 << 9,
            /// <summary>Displays the show after post process field.</summary>
            ShowAfterPostProcessPass = 1 << 10,
            /// <summary>Obsolete - This field has no effect - Do not used.</summary>
            AlphaToMask = 1 << 11,
            /// <summary>Displays the show pre pass and post pass fields.</summary>
            ShowPrePassAndPostPass = 1 << 12,
            /// <summary>Displays the depth offset field.</summary>
            ShowDepthOffsetOnly = 1 << 13,
            /// <summary>Displays the preserve specular lighting field.</summary>
            PreserveSpecularLighting = 1 << 14,
            /// <summary>Displays all the Unlit Surface Option fields.</summary>
            Unlit = Surface | BlendMode | DoubleSided | AlphaCutoff | AlphaCutoffThreshold | AlphaCutoffShadowThreshold | AlphaToMask | BackThenFrontRendering | ShowAfterPostProcessPass | ShowPrePassAndPostPass | ShowDepthOffsetOnly,
            /// <summary>Displays all the Lit Surface Option fields field.</summary>
            Lit = All ^ SurfaceOptionUIBlock.Features.ShowAfterPostProcessPass ^ ShowDepthOffsetOnly, // Lit can't be display in after postprocess pass
            /// <summary>Displays all the fields.</summary>
            All = ~0,
        }

        internal static class Styles
        {
            public static GUIContent optionText { get; } = EditorGUIUtility.TrTextContent("Surface Options");
            public const string renderingPassText = "Rendering Pass";
            public const string blendModeText = "Blending Mode";
            public const string notSupportedInMultiEdition = "Multiple Different Values";
            public const string lowResTransparencyNotSupportedText = "Low resolution transparency is not enabled in the current HDRP Asset. The selected Pass will be Default.";

            public static readonly string[] surfaceTypeNames = Enum.GetNames(typeof(SurfaceType));
            public static readonly string[] blendModeNames = Enum.GetNames(typeof(BlendingMode));
            public static readonly int[] blendModeValues = Enum.GetValues(typeof(BlendingMode)) as int[];

            public static GUIContent surfaceTypeText = new GUIContent("Surface Type", "Controls whether the Material supports transparency or not");
            public static GUIContent transparentPrepassText = new GUIContent("Appear in Refraction", "When enabled, HDRP handles objects with this Material before the refraction pass.");

            public static GUIContent doubleSidedEnableText = new GUIContent("Double-Sided", "When enabled, HDRP renders both faces of the polygons that make up meshes using this Material. Disables backface culling.");

            public static GUIContent customVelocityText = new GUIContent("Add Custom Velocity", "When enabled, the provided custom object space velocity is added to the motion vector calculation.");

            public static GUIContent useShadowThresholdText = new GUIContent("Use Shadow Threshold", "Enable separate threshold for shadow pass");
            public static GUIContent alphaCutoffEnableText = new GUIContent("Alpha Clipping", "When enabled, HDRP processes Alpha Clipping for this Material.");
            public static GUIContent alphaCutoffText = new GUIContent("Threshold", "Controls the threshold for the Alpha Clipping effect.");
            public static GUIContent alphaCutoffShadowText = new GUIContent("Shadow Threshold", "Controls the threshold for shadow pass alpha clipping.");
            public static GUIContent alphaCutoffPrepassText = new GUIContent("Prepass Threshold", "Controls the threshold for transparent depth prepass alpha clipping.");
            public static GUIContent alphaCutoffPostpassText = new GUIContent("Postpass Threshold", "Controls the threshold for transparent depth postpass alpha clipping.");
            public static GUIContent transparentDepthPostpassEnableText = new GUIContent("Transparent Depth Postpass", "When enabled, HDRP renders a depth postpass for transparent objects. This improves post-processing effects like depth of field.");
            public static GUIContent transparentDepthPrepassEnableText = new GUIContent("Transparent Depth Prepass", "When enabled, HDRP renders a depth prepass for transparent GameObjects. This improves sorting.");
            public static GUIContent transparentBackfaceEnableText = new GUIContent("Back Then Front Rendering", "When enabled, HDRP renders the back face and then the front face, in two separate draw calls, to better sort transparent meshes.");
            public static GUIContent transparentWritingMotionVecText = new GUIContent("Transparent Writes Motion Vectors", "When enabled, transparent objects write motion vectors, these replace what was previously rendered in the buffer.");
            public static GUIContent perPixelSortingText = new GUIContent("Sort with Refractive", "When enabled, transparent objects in Rendering Pass Before Refraction will be depth sorted per pixel with Refractive objects.\nThis is useful when a Before Refraction transparent object is both behind and in front of a refractive object, for example when crossing a water surface.");

            public static GUIContent zWriteEnableText = new GUIContent("Depth Write", "When enabled, transparent objects write to the depth buffer.");
            public static GUIContent transparentZTestText = new GUIContent("Depth Test", "Set the comparison function to use during the Z Testing.");
            public static GUIContent rayTracingText = new GUIContent("Recursive Rendering");
            public static GUIContent rayTracingTextInfo = new GUIContent("When enabled, if you enabled ray tracing in your project and a recursive rendering volume override is active, Unity uses recursive rendering to render the GameObject.");

            public static GUIContent transparentSortPriorityText = new GUIContent("Sorting Priority", "Sets the sort priority (from -100 to 100) of transparent meshes using this Material. HDRP uses this value to calculate the sorting order of all transparent meshes on screen.");
            public static GUIContent enableTransparentFogText = new GUIContent("Receive fog", "When enabled, this Material can receive fog and absorption from underwater.");
            public static GUIContent transparentCullModeText = new GUIContent("Cull Mode", "For transparent objects, change the cull mode of the object.");
            public static GUIContent enableBlendModePreserveSpecularLightingText = new GUIContent("Preserve specular lighting", "When enabled, blending only affects diffuse lighting, allowing for correct specular lighting on transparent meshes that use this Material. This parameter is only supported when the material's refraction model is set to None.");

            // Lit properties
            public static GUIContent doubleSidedNormalModeText = new GUIContent("Normal Mode", "Specifies the method HDRP uses to modify the normal base.\nMirror: Mirrors the normals with the vertex normal plane.\nFlip: Flips the normal.");
            public static GUIContent depthOffsetEnableText = new GUIContent("Depth Offset", "When enabled, HDRP uses the Height Map to calculate the depth offset for this Material.");

            // SG property
            public static GUIContent fragmentNormalSpace = new GUIContent("Fragment Normal Space", "Select the space use for normal map in Fragment shader in this shader graph.");

            public static GUIContent doubleSidedGIText = new GUIContent("Double-Sided GI", "When selecting Auto, Double-Sided GI is enabled if the material is Double-Sided, otherwise On enables it and Off disables it.\n" +
                "When enabled, the lightmapper accounts for both sides of the geometry when calculating Global Illumination. Backfaces are not rendered or added to lightmaps, but get treated as valid when seen from other objects. When using the Progressive Lightmapper backfaces bounce light using the same emission and albedo as frontfaces. (Currently this setting is only available when baking with the Progressive Lightmapper backend.).");
            public static GUIContent conservativeDepthOffsetEnableText = new GUIContent("Conservative", "When enabled, only positive depth offsets will be applied in order to take advantage of the early depth test mechanic.");

            // Displacement mapping (POM, tessellation, per vertex)
            //public static GUIContent enablePerPixelDisplacementText = new GUIContent("Per Pixel Displacement", "");

            public static GUIContent displacementModeText = new GUIContent("Displacement Mode", "Specifies the method HDRP uses to apply height map displacement to the selected element: Vertex, pixel, or tessellated vertex.\nYou must use flat surfaces for Pixel displacement.");
            public static GUIContent lockWithObjectScaleText = new GUIContent("Lock With Object Scale", "When enabled, displacement mapping takes the absolute value of the scale of the object into account.");
            public static GUIContent lockWithTilingRateText = new GUIContent("Lock With Height Map Tiling Rate", "When enabled, displacement mapping takes the absolute value of the tiling rate of the height map into account.");

            // Material ID
            public static GUIContent materialIDText = new GUIContent("Material Type", "Specifies additional feature for this Material. Customize you Material with different settings depending on which Material Type you select.");
            public static GUIContent transmissionEnableText = new GUIContent("Transmission", "When enabled HDRP processes the transmission effect for subsurface scattering. Simulates the translucency of the object.");
            public static string transparentSSSErrorMessage = "Transparent Materials With SubSurface Scattering is not supported.";
            public static GUIContent clearCoatEnabledText = new GUIContent("Clear Coat", "Controls whether the clear coat effect is enabled or not.");

            // Per pixel displacement
            public static GUIContent ppdMinSamplesText = new GUIContent("Minimum Steps", "Controls the minimum number of steps HDRP uses for per pixel displacement mapping.");
            public static GUIContent ppdMaxSamplesText = new GUIContent("Maximum Steps", "Controls the maximum number of steps HDRP uses for per pixel displacement mapping.");
            public static GUIContent ppdLodThresholdText = new GUIContent("Fading Mip Level Start", "Controls the Height Map mip level where the parallax occlusion mapping effect begins to disappear.");
            public static GUIContent ppdPrimitiveLength = new GUIContent("Primitive Length", "Sets the length of the primitive (with the scale of 1) to which HDRP applies per-pixel displacement mapping. For example, the standard quad is 1 x 1 meter, while the standard plane is 10 x 10 meters.");
            public static GUIContent ppdPrimitiveWidth = new GUIContent("Primitive Width", "Sets the width of the primitive (with the scale of 1) to which HDRP applies per-pixel displacement mapping. For example, the standard quad is 1 x 1 meter, while the standard plane is 10 x 10 meters.");

            public static GUIContent supportDecalsText = new GUIContent("Receive Decals", "Enable to allow Materials to receive decals.");

            public static GUIContent enableGeometricSpecularAAText = new GUIContent("Geometric Specular AA", "When enabled, HDRP reduces specular aliasing on high density meshes (particularly useful when a normal map is not used).");
            public static GUIContent specularAAScreenSpaceVarianceText = new GUIContent("Screen space variance", "Controls the strength of the Specular AA reduction. Higher values give a more blurry result and less aliasing.");
            public static GUIContent specularAAThresholdText = new GUIContent("Threshold", "Controls the effect of Specular AA reduction. A values of 0 does not apply reduction, higher values allow higher reduction.");

            public static GUIContent excludeFromTUAndAAText = new GUIContent("Exclude From Temporal Upscalers and Anti Aliasing", "When enabled, the current material wont be temporaly sampled during TAA and will have reduced ghosting on upscalers.");

            // SSR
            public static GUIContent receivesSSRText = new GUIContent("Receive SSR", "When enabled, this Material can receive screen space reflections.");
            public static GUIContent receivesSSRTransparentText = new GUIContent("Receive SSR Transparent", "When enabled, this Material can receive screen space reflections. This will force a transparent depth prepass on the object if HDRP supports it.");

            public static GUIContent opaqueCullModeText = new GUIContent("Cull Mode", "For opaque objects, change the cull mode of the object.");

            public static string afterPostProcessInfoBox = "If After post-process objects don't render, make sure to enable \"After Post-process\" in the frame settings.\nAfter post-process material wont be ZTested. Enable the \"ZTest For After PostProcess\" checkbox in the Frame Settings to force the depth-test if the TAA is disabled.";

            public static readonly GUIContent[] displacementModeLitNames = new GUIContent[] { new GUIContent("None"), new GUIContent("Vertex displacement"), new GUIContent("Pixel displacement") };
            public static readonly int[] displacementModeLitValues = new int[] { (int)DisplacementMode.None, (int)DisplacementMode.Vertex, (int)DisplacementMode.Pixel };

            enum DisplacementModeLitTessellation { None = DisplacementMode.None, Tessellation = DisplacementMode.Tessellation };
            public static readonly GUIContent[] displacementModeLitTessellationNames = new GUIContent[] { new GUIContent("None"), new GUIContent("Tessellation displacement") };
            public static readonly int[] displacementModeLitTessellationValues = new int[] { (int)DisplacementMode.None, (int)DisplacementMode.Tessellation };
        }

        // Properties common to Unlit and Lit
        MaterialProperty surfaceType = null;

        MaterialProperty alphaCutoffEnable = null;
        MaterialProperty useShadowThreshold = null;
        MaterialProperty alphaCutoff = null;
        MaterialProperty alphaCutoffShadow = null;
        MaterialProperty alphaCutoffPrepass = null;
        MaterialProperty alphaCutoffPostpass = null;
        MaterialProperty transparentDepthPrepassEnable = null;
        MaterialProperty transparentDepthPostpassEnable = null;
        MaterialProperty transparentBackfaceEnable = null;
        MaterialProperty transparentSortPriority = null;
        const string kTransparentSortPriority = HDMaterialProperties.kTransparentSortPriority;
        MaterialProperty perPixelSorting = null;
        MaterialProperty transparentWritingMotionVec = null;
        MaterialProperty doubleSidedEnable = null;
        MaterialProperty blendMode = null;
        MaterialProperty enableBlendModePreserveSpecularLighting = null;
        MaterialProperty enableFogOnTransparent = null;
        MaterialProperty refractionModel = null;

        // Lit properties
        MaterialProperty doubleSidedNormalMode = null;
        MaterialProperty doubleSidedGIMode = null;
        MaterialProperty materialID = null;
        MaterialProperty supportDecals = null;
        MaterialProperty enableGeometricSpecularAA = null;
        MaterialProperty specularAAScreenSpaceVariance = null;
        const string kSpecularAAScreenSpaceVariance = "_SpecularAAScreenSpaceVariance";
        MaterialProperty specularAAThreshold = null;
        const string kSpecularAAThreshold = "_SpecularAAThreshold";
        MaterialProperty transmissionEnable = null;
        MaterialProperty clearCoatEnabled = null;
        MaterialProperty excludeFromTUAndAA = null;

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
        MaterialProperty receivesSSRTransparent = null;

        MaterialProperty displacementMode = null;
        MaterialProperty displacementLockObjectScale = null;
        MaterialProperty displacementLockTilingScale = null;

        MaterialProperty depthOffsetEnable = null;
        MaterialProperty conservativeDepthOffsetEnable = null;

        MaterialProperty transparentZWrite = null;
        MaterialProperty stencilRef = null;
        MaterialProperty zTest = null;
        MaterialProperty transparentCullMode = null;
        MaterialProperty opaqueCullMode = null;
        MaterialProperty rayTracing = null;

        MaterialProperty renderQueueTypeSG = null;
        SerializedProperty renderQueueProperty = null;

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
                foreach (Material target in materialEditor.targets)
                {
                    if (renderQueueTypeSG != null)
                        renderQueueTypeSG.floatValue = (int)HDRenderQueue.GetTypeByRenderQueueValue(value);
                    target.renderQueue = value;
                }
            }
        }

        SurfaceType surfaceTypeValue
        {
            get { return surfaceType != null ? (SurfaceType)surfaceType.floatValue : defaultSurfaceType; }
        }

        List<string> m_RenderingPassNames = new List<string>();
        List<int> m_RenderingPassValues = new List<int>();

        Features m_Features;
        int m_LayerCount;

        /// <summary>
        /// Constructs a SurfaceOptionUIBlock based on the parameters.
        /// </summary>
        /// <param name="expandableBit">Bit used for the foldout state.</param>
        /// <param name="layerCount">Number of layers available in the shader.</param>
        /// <param name="features">Features of the block.</param>
        public SurfaceOptionUIBlock(ExpandableBit expandableBit, int layerCount = 1, Features features = Features.All)
            : base(expandableBit, Styles.optionText)
        {
            m_Features = features;
            m_LayerCount = layerCount;
        }

        /// <summary>
        /// Loads the material properties for the block.
        /// </summary>
        public override void LoadMaterialProperties()
        {
            surfaceType = FindProperty(kSurfaceType);
            useShadowThreshold = FindProperty(kUseShadowThreshold);
            alphaCutoffEnable = FindProperty(kAlphaCutoffEnabled);
            alphaCutoff = FindProperty(kAlphaCutoff);

            alphaCutoffShadow = FindProperty(kAlphaCutoffShadow);
            alphaCutoffPrepass = FindProperty(kAlphaCutoffPrepass);
            alphaCutoffPostpass = FindProperty(kAlphaCutoffPostpass);
            transparentDepthPrepassEnable = FindProperty(kTransparentDepthPrepassEnable);
            transparentDepthPostpassEnable = FindProperty(kTransparentDepthPostpassEnable);

            if ((m_Features & Features.BackThenFrontRendering) != 0)
                transparentBackfaceEnable = FindProperty(kTransparentBackfaceEnable);

            transparentSortPriority = FindProperty(kTransparentSortPriority);

            refractionModel = FindProperty(kRefractionModel);
            perPixelSorting = FindProperty(kPerPixelSorting);
            transparentWritingMotionVec = FindProperty(kTransparentWritingMotionVec);

            if ((m_Features & Features.PreserveSpecularLighting) != 0)
                enableBlendModePreserveSpecularLighting = FindProperty(kEnableBlendModePreserveSpecularLighting);

            enableFogOnTransparent = FindProperty(kEnableFogOnTransparent);

            if ((m_Features & Features.DoubleSided) != 0)
                doubleSidedEnable = FindProperty(kDoubleSidedEnable);

            blendMode = FindProperty(kBlendMode);

            transmissionEnable = FindProperty(kTransmissionEnable);
            clearCoatEnabled = FindProperty(kClearCoatEnabled);

            excludeFromTUAndAA = FindProperty(kExcludeFromTUAndAA);

            if ((m_Features & Features.DoubleSidedNormalMode) != 0)
            {
                doubleSidedNormalMode = FindProperty(kDoubleSidedNormalMode);
            }
            doubleSidedGIMode = FindProperty(kDoubleSidedGIMode);
            depthOffsetEnable = FindProperty(kDepthOffsetEnable);
            conservativeDepthOffsetEnable = FindProperty(kConservativeDepthOffsetEnable);

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
            ppdPrimitiveWidth = FindProperty(kPpdPrimitiveWidth);
            invPrimScale = FindProperty(kInvPrimScale);

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

            renderQueueProperty = materialEditor.serializedObject.FindProperty("m_CustomRenderQueue");
            if (!(materialEditor.target as Material).isVariant)
                renderQueueTypeSG = FindProperty(kRenderQueueTypeShaderGraph);
        }

        /// <summary>
        /// Renders the properties in the block.
        /// </summary>
        protected override void OnGUIOpen()
        {
            if ((m_Features & Features.Surface) != 0)
                DrawSurfaceGUI();

            if ((m_Features & Features.AlphaCutoff) != 0)
                DrawAlphaCutoffGUI();

            if ((m_Features & Features.DoubleSided) != 0)
                DrawDoubleSidedGUI();

            DrawLitSurfaceOptions();
        }

        bool AreMaterialsShaderGraphs() => materials.All(m => m.IsShaderGraph());

        /// <summary>Returns false if there are multiple materials selected and they have different default values for propName</summary>
        float GetShaderDefaultFloatValue(string propName)
        {
            if (!materials[0].HasProperty(propName))
                return 0;

            // It's okay to ignore all other materials here because if the material editor is displayed, the shader is the same for all materials
            var shader = materials[0].shader;
            return shader.GetPropertyDefaultFloatValue(shader.FindPropertyIndex(propName));
        }

        /// <summary>
        /// Draws the Alpha Cutoff GUI.
        /// </summary>
        protected void DrawAlphaCutoffGUI()
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

                if (alphaCutoff != null)
                    materialEditor.ShaderProperty(alphaCutoff, Styles.alphaCutoffText);

                if ((m_Features & Features.AlphaCutoffShadowThreshold) != 0)
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

                // With transparent object and few specific materials like Hair, we need more control on the cutoff to apply
                // This allow to get a better sorting (with prepass), better shadow (better silhouettes fidelity) etc...
                if (surfaceTypeValue == SurfaceType.Transparent)
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

        /// <summary>
        /// Draws the Double Sided GUI.
        /// </summary>
        protected void DrawDoubleSidedGUI()
        {
            // This function must finish with double sided option (see LitUI.cs)
            if (doubleSidedEnable != null)
                materialEditor.ShaderProperty(doubleSidedEnable, Styles.doubleSidedEnableText);

            // This follow double sided option
            if (doubleSidedEnable != null && doubleSidedEnable.floatValue > 0.0f)
            {
                if (doubleSidedNormalMode != null)
                {
                    EditorGUI.indentLevel++;
                    materialEditor.ShaderProperty(doubleSidedNormalMode, Styles.doubleSidedNormalModeText);
                    EditorGUI.indentLevel--;
                }
            }

            if (doubleSidedGIMode != null)
            {
                materialEditor.ShaderProperty(doubleSidedGIMode, Styles.doubleSidedGIText);
            }
        }

        void TogglePropertyOrDisable(bool disabled, MaterialProperty property, GUIContent style, bool forceValue)
        {
            using (new EditorGUI.DisabledScope(disabled))
            {
                if (!disabled)
                    materialEditor.ShaderProperty(property, style);
                else
                    EditorGUILayout.Toggle(style, forceValue);
            }
        }

        /// <summary>
        /// Draws the Surface GUI.
        /// </summary>
        protected void DrawSurfaceGUI()
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
                    materialEditor.IntPopupShaderProperty(blendMode, Styles.blendModeText, Styles.blendModeNames, Styles.blendModeValues);

                if ((m_Features & Features.PreserveSpecularLighting) != 0)
                {
                    EditorGUI.indentLevel++;
                    if (renderQueueHasMultipleDifferentValue)
                    {
                        using (new EditorGUI.DisabledScope(true))
                            EditorGUILayout.LabelField(Styles.enableBlendModePreserveSpecularLightingText.text, Styles.notSupportedInMultiEdition);
                    }
                    else if (enableBlendModePreserveSpecularLighting != null && blendMode != null)
                        materialEditor.ShaderProperty(enableBlendModePreserveSpecularLighting, Styles.enableBlendModePreserveSpecularLightingText);
                    EditorGUI.indentLevel--;
                }

                if (transparentSortPriority != null)
                    materialEditor.IntSliderShaderProperty(transparentSortPriority, -HDRenderQueue.sortingPriorityRange, HDRenderQueue.sortingPriorityRange, Styles.transparentSortPriorityText);

                if (enableFogOnTransparent != null)
                    materialEditor.ShaderProperty(enableFogOnTransparent, Styles.enableTransparentFogText);

                bool forceMotionVec = false;
                bool preRefraction = HDRenderQueue.k_RenderQueue_PreRefraction.Contains(renderQueue);
                if (perPixelSorting != null)
                {
                    bool recursiveRendering = (RenderPipelineManager.currentPipeline as HDRenderPipeline).rayTracingSupported && rayTracing != null && rayTracing.floatValue == 1.0f;
                    TogglePropertyOrDisable(!preRefraction || recursiveRendering, perPixelSorting, Styles.perPixelSortingText, false);
                    forceMotionVec = preRefraction && !recursiveRendering && perPixelSorting.floatValue > 0.0f;
                }

                bool shaderHasBackThenFrontPass = materials.All(m => m.FindPass(HDShaderPassNames.s_TransparentBackfaceStr) != -1);
                if (shaderHasBackThenFrontPass && transparentBackfaceEnable != null)
                    materialEditor.ShaderProperty(transparentBackfaceEnable, Styles.transparentBackfaceEnableText);

                if ((m_Features & Features.ShowPrePassAndPostPass) != 0 && !HDRenderQueue.k_RenderQueue_LowTransparent.Contains(renderQueue))
                {
                    bool shaderHasDepthPrePass = materials.All(m => m.FindPass(HDShaderPassNames.s_TransparentDepthPrepassStr) != -1);
                    if (shaderHasDepthPrePass && transparentDepthPrepassEnable != null)
                    {
                        bool ssrTransparent = receivesSSRTransparent != null && receivesSSRTransparent.floatValue > 0.0f;
                        bool isRefractive = !preRefraction && refractionModel != null && refractionModel.floatValue != (float)(int)ScreenSpaceRefraction.RefractionModel.None;
                        TogglePropertyOrDisable(ssrTransparent || isRefractive, transparentDepthPrepassEnable, Styles.transparentDepthPrepassEnableText, true);
                    }

                    bool shaderHasDepthPostPass = materials.All(m => m.FindPass(HDShaderPassNames.s_TransparentDepthPostpassStr) != -1);
                    if (shaderHasDepthPostPass && transparentDepthPostpassEnable != null)
                        materialEditor.ShaderProperty(transparentDepthPostpassEnable, Styles.transparentDepthPostpassEnableText);
                }

                if (transparentWritingMotionVec != null && !HDRenderQueue.k_RenderQueue_LowTransparent.Contains(renderQueue))
                    TogglePropertyOrDisable(forceMotionVec, transparentWritingMotionVec, Styles.transparentWritingMotionVecText, true);

                if (transparentZWrite != null && !HDRenderQueue.k_RenderQueue_LowTransparent.Contains(renderQueue))
                    materialEditor.ShaderProperty(transparentZWrite, Styles.zWriteEnableText);

                if (zTest != null)
                    materialEditor.ShaderProperty(zTest, Styles.transparentZTestText);

                bool showTransparentCullMode = transparentCullMode != null;
                if (transparentBackfaceEnable != null)
                    showTransparentCullMode &= transparentBackfaceEnable.floatValue == 0;

                if (showTransparentCullMode)
                {
                    if (doubleSidedEnable != null && doubleSidedEnable.floatValue == 0 && transparentCullMode != null)
                        materialEditor.ShaderProperty(transparentCullMode, Styles.transparentCullModeText);
                    else
                    {
                        EditorGUI.BeginDisabledGroup(true);
                        EditorGUILayout.Popup(Styles.transparentCullModeText, 0, new string[] { "Off" });
                        EditorGUI.EndDisabledGroup();
                    }
                }

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
            if (renderQueueTypeSG != null)
                renderQueueType = (HDRenderQueue.RenderQueueType)renderQueueTypeSG.floatValue;

            // To know if we need to update the renderqueue, mainly happens if a material is created from a shader graph shader
            // with default render-states.
            bool renderQueueTypeMismatchRenderQueue = HDRenderQueue.GetTypeByRenderQueueValue(material.renderQueue) != renderQueueType;

            var newMode = (SurfaceType)materialEditor.PopupShaderProperty(surfaceType, Styles.surfaceTypeText, Styles.surfaceTypeNames);

            bool isMixedRenderQueue = surfaceType.hasMixedValue || renderQueueHasMultipleDifferentValue;
            bool showAfterPostProcessPass = (m_Features & Features.ShowAfterPostProcessPass) != 0;
            bool showLowResolutionPass = true;

            EditorGUI.showMixedValue = isMixedRenderQueue;
            ++EditorGUI.indentLevel;

            if (newMode == SurfaceType.Transparent)
            {
                if (stencilRef != null && ((int)stencilRef.floatValue & (int)StencilUsage.SubsurfaceScattering) != 0)
                    EditorGUILayout.HelpBox(Styles.transparentSSSErrorMessage, MessageType.Error);
            }

            MaterialEditor.BeginProperty(renderQueueProperty);
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
                    if (renderQueueTransparentType == HDRenderQueue.TransparentRenderQueue.LowResolution)
                    {
                        if (HDRenderPipeline.currentPipeline != null
                            && !HDRenderPipeline.currentPipeline.currentPlatformRenderPipelineSettings.lowresTransparentSettings.enabled)
                        {
                            EditorGUILayout.HelpBox(Styles.lowResTransparencyNotSupportedText, MessageType.Info);
                        }
                    }
                    break;
                default:
                    throw new ArgumentException("Unknown SurfaceType");
            }
            MaterialEditor.EndProperty();

            --EditorGUI.indentLevel;
            EditorGUI.showMixedValue = false;

            if (renderQueueTypeSG != null)
                renderQueueTypeSG.floatValue = (float)renderQueueType;
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

        /// <summary>
        /// Draws the Lit Surface Options GUI.
        /// </summary>
        protected void DrawLitSurfaceOptions()
        {
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

            if (clearCoatEnabled != null)
            {
                materialEditor.ShaderProperty(clearCoatEnabled, Styles.clearCoatEnabledText);
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

            if (receivesSSR != null && receivesSSRTransparent != null && !HDRenderQueue.k_RenderQueue_LowTransparent.Contains(renderQueue))
            {
                // Based on the surface type, display the right recieveSSR option
                if (surfaceTypeValue == SurfaceType.Transparent)
                    materialEditor.ShaderProperty(receivesSSRTransparent, Styles.receivesSSRTransparentText);
                else
                    materialEditor.ShaderProperty(receivesSSR, Styles.receivesSSRText);
            }

            if (excludeFromTUAndAA != null && BaseLitAPI.CompatibleWithExcludeFromTUAndAA(surfaceTypeValue, renderQueue))
                materialEditor.ShaderProperty(excludeFromTUAndAA, Styles.excludeFromTUAndAAText);

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
                {
                    materialEditor.ShaderProperty(depthOffsetEnable, Styles.depthOffsetEnableText);
                    if (conservativeDepthOffsetEnable != null)
                    {
                        EditorGUI.indentLevel++;
                        materialEditor.ShaderProperty(conservativeDepthOffsetEnable, Styles.conservativeDepthOffsetEnableText);
                        EditorGUI.indentLevel--;
                    }
                }
            }
            else if (displacementMode != null)
            {
                var displaceMode = DisplacementModePopup(Styles.displacementModeText, displacementMode);

                if (displaceMode != DisplacementMode.None)
                {
                    EditorGUI.indentLevel++;
                    materialEditor.ShaderProperty(displacementLockObjectScale, Styles.lockWithObjectScaleText);
                    materialEditor.ShaderProperty(displacementLockTilingScale, Styles.lockWithTilingRateText);
                    EditorGUI.indentLevel--;
                }

                if (displaceMode == DisplacementMode.Pixel)
                {
                    EditorGUILayout.Space();
                    EditorGUI.indentLevel++;

                    materialEditor.IntSliderShaderProperty(ppdMinSamples, Styles.ppdMinSamplesText);
                    ppdMaxSamples.floatValue = Mathf.Max(ppdMinSamples.floatValue, ppdMaxSamples.floatValue);
                    materialEditor.IntSliderShaderProperty(ppdMaxSamples, Styles.ppdMaxSamplesText);
                    ppdMinSamples.floatValue = Mathf.Min(ppdMinSamples.floatValue, ppdMaxSamples.floatValue);

                    materialEditor.ShaderProperty(ppdLodThreshold, Styles.ppdLodThresholdText);

                    materialEditor.MinFloatShaderProperty(ppdPrimitiveLength, Styles.ppdPrimitiveLength, 0.01f);
                    materialEditor.MinFloatShaderProperty(ppdPrimitiveWidth, Styles.ppdPrimitiveWidth, 0.01f);
                    invPrimScale.vectorValue = new Vector4(1.0f / ppdPrimitiveLength.floatValue, 1.0f / ppdPrimitiveWidth.floatValue); // Precompute

                    materialEditor.ShaderProperty(depthOffsetEnable, Styles.depthOffsetEnableText);
                    EditorGUI.indentLevel--;
                }

                if (displaceMode != DisplacementMode.None && materials[0].GetTexture(kHeightMap) == null)
                {
                    EditorGUILayout.Space();
                    EditorGUI.indentLevel++;

                    EditorGUILayout.HelpBox("Please set a valid HeightMap (in the 'Surface Inputs' category) to apply any displacement.", MessageType.Warning);
                    EditorGUI.indentLevel--;
                }
            }
        }

        DisplacementMode DisplacementModePopup(GUIContent label, MaterialProperty prop)
        {
            var displayedOptions = Styles.displacementModeLitNames;
            var optionValues = Styles.displacementModeLitValues;
            if (materials[0].HasProperty(kTessellationMode))
            {
                displayedOptions = Styles.displacementModeLitTessellationNames;
                optionValues = Styles.displacementModeLitTessellationValues;
            }

            int mode = (int)GetFilteredDisplacementMode(prop);
            bool mixed = HasMixedDisplacementMode(prop);

            MaterialEditor.BeginProperty(prop);
            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = mixed;
            int newMode = EditorGUILayout.IntPopup(label, mode, displayedOptions, optionValues);
            EditorGUI.showMixedValue = false;
            if (EditorGUI.EndChangeCheck() && (newMode != mode || mixed))
            {
                materialEditor.RegisterPropertyChangeUndo(label.text);
                prop.floatValue = newMode;
            }
            MaterialEditor.EndProperty();

            return (DisplacementMode)newMode;
        }

        static internal DisplacementMode GetFilteredDisplacementMode(MaterialProperty displacementMode)
        {
            var material = displacementMode.targets[0] as Material;
            return material.GetFilteredDisplacementMode((DisplacementMode)displacementMode.floatValue);
        }

        static internal bool HasMixedDisplacementMode(MaterialProperty displacementMode)
        {
            Material mat0 = displacementMode.targets[0] as Material;
            var mode = mat0.GetFilteredDisplacementMode((DisplacementMode)displacementMode.floatValue);
            for (int i = 1; i < displacementMode.targets.Length; i++)
            {
                Material mat = displacementMode.targets[i] as Material;
                var currentMode = (DisplacementMode)mat.GetFloat(displacementMode.name);
                if (mat.GetFilteredDisplacementMode(currentMode) != mode)
                    return true;
            }
            return false;
        }
    }
}
