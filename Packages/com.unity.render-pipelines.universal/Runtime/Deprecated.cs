// This file should be used as a container for things on its
// way to being deprecated and removed in future releases
using System;
using System.ComponentModel;

namespace UnityEngine.Rendering.Universal
{
    public abstract partial class ScriptableRenderPass
    {
        // This callback method will be removed. Please use OnCameraCleanup() instead.
        /// <summary>
        /// Cleanup any allocated resources that were created during the execution of this render pass.
        /// </summary>
        /// <param name="cmd">Use this CommandBuffer to cleanup any generated data. </param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual void FrameCleanup(CommandBuffer cmd) => OnCameraCleanup(cmd);
    }

    namespace Internal
    {
        public partial class AdditionalLightsShadowCasterPass
        {
            /// <summary>
            /// The ID for the additional shadows buffer ID.
            /// This has been deprecated. Shadow slice matrix is now passed to the GPU using an entry in buffer m_AdditionalLightsWorldToShadow_SSBO.
            /// </summary>
            [Obsolete("AdditionalLightsShadowCasterPass.m_AdditionalShadowsBufferId was deprecated. Shadow slice matrix is now passed to the GPU using an entry in buffer m_AdditionalLightsWorldToShadow_SSBO #from(2021.1) #breakingFrom(2023.1)", true)]
            public static int m_AdditionalShadowsBufferId;

            /// <summary>
            /// The ID for the additional shadows buffer ID.
            /// This has been deprecated. hadow slice index is now passed to the GPU using last member of an entry in buffer m_AdditionalShadowParams_SSBO.
            /// </summary>
            [Obsolete("AdditionalLightsShadowCasterPass.m_AdditionalShadowsIndicesId was deprecated. Shadow slice index is now passed to the GPU using last member of an entry in buffer m_AdditionalShadowParams_SSBO #from(2021.1) #breakingFrom(2023.1)", true)]
            public static int m_AdditionalShadowsIndicesId;
        }
    }

    /// <summary>
    /// Previously contained the settings to control how many cascades to use. It is now deprecated.
    /// </summary>
    [Obsolete("This is obsolete, please use shadowCascadeCount instead. #from(2021.1) #breakingFrom(2023.1)", true)]
    public enum ShadowCascadesOption
    {
        /// <summary>
        /// No cascades used for the shadows
        /// </summary>
        NoCascades,
        /// <summary>
        /// Two cascades used for the shadows
        /// </summary>
        TwoCascades,
        /// <summary>
        /// Four cascades used for the shadows
        /// </summary>
        FourCascades,
    }

    /// <summary>
    /// Specifies the logging level for shader variants.
    /// This is obsolete, UnityEngine.Rendering.ShaderVariantLogLevel instead.
    /// </summary>
    [Obsolete("This is obsolete, UnityEngine.Rendering.ShaderVariantLogLevel instead. #from(2022.2) #breakingFrom(2023.1)", true)]
    public enum ShaderVariantLogLevel
    {
        /// <summary>Disable all log for shader variants.</summary>
        Disabled,

        /// <summary>Only logs SRP Shaders when logging shader variants.</summary>
        [InspectorName("Only URP Shaders")]
        OnlyUniversalRPShaders,

        /// <summary>Logs all shader variants.</summary>
        [InspectorName("All Shaders")]
        AllShaders
    }

    public partial class UniversalRenderPipelineAsset
    {
        #if UNITY_EDITOR
        [Obsolete("Editor resources are stored directly into GraphicsSettings. #from(2023.3)")]
        public static readonly string editorResourcesGUID = "a3d8d823eedde654bb4c11a1cfaf1abb";
        #endif

        [SerializeField] int m_ShaderVariantLogLevel;

#pragma warning disable 618 // Obsolete warning
        /// <summary>
        /// Previously returned the shader variant log level for this Render Pipeline Asset but is now deprecated.
        /// </summary>
        [Obsolete("Use GraphicsSettings.GetRenderPipelineSettings<ShaderStrippingSetting>().shaderVariantLogLevel instead. #from(2022.2)")]
        public ShaderVariantLogLevel shaderVariantLogLevel
        {
            get => (ShaderVariantLogLevel)GraphicsSettings.GetRenderPipelineSettings<ShaderStrippingSetting>().shaderVariantLogLevel;
            set => GraphicsSettings.GetRenderPipelineSettings<ShaderStrippingSetting>().shaderVariantLogLevel = (Rendering.ShaderVariantLogLevel)value;
        }
#pragma warning restore 618 // Obsolete warning

#pragma warning disable 618 // Obsolete warning
        [Obsolete("This is obsolete, please use shadowCascadeCount instead. #from(2021.1)")]
        [SerializeField] ShadowCascadesOption m_ShadowCascades = ShadowCascadesOption.NoCascades;

        /// <summary>
        /// Previously used insted of shadowCascadeCount. Please use that instead.
        /// </summary>
        [Obsolete("This is obsolete, please use shadowCascadeCount instead. #from(2021.1) #breakingFrom(2023.1)", true)]
        public ShadowCascadesOption shadowCascadeOption
        {
            get
            {
                switch (shadowCascadeCount)
                {
                    case 1: return ShadowCascadesOption.NoCascades;
                    case 2: return ShadowCascadesOption.TwoCascades;
                    case 4: return ShadowCascadesOption.FourCascades;
                    default: throw new InvalidOperationException("Cascade count is not compatible with obsolete API, please use shadowCascadeCount instead.");
                }
                ;
            }
            set
            {
                switch (value)
                {
                    case ShadowCascadesOption.NoCascades:
                        shadowCascadeCount = 1;
                        break;
                    case ShadowCascadesOption.TwoCascades:
                        shadowCascadeCount = 2;
                        break;
                    case ShadowCascadesOption.FourCascades:
                        shadowCascadeCount = 4;
                        break;
                    default:
                        throw new InvalidOperationException("Cascade count is not compatible with obsolete API, please use shadowCascadeCount instead.");
                }
            }
        }
#pragma warning restore 618 // Obsolete warning

        /// <summary>
        /// Class containing texture resources used in URP.
        /// </summary>
        [Serializable, ReloadGroup]
        [Obsolete("Moved to UniversalRenderPipelineRuntimeTextures on GraphicsSettings. #from(2023.3)")]
        public sealed class TextureResources
        {
            /// <summary>
            /// Pre-baked blue noise textures.
            /// </summary>
            [Reload("Textures/BlueNoise64/L/LDR_LLL1_0.png")]
            public Texture2D blueNoise64LTex;

            /// <summary>
            /// Bayer matrix texture.
            /// </summary>
            [Reload("Textures/BayerMatrix.png")]
            public Texture2D bayerMatrixTex;

            /// <summary>
            /// Check if the textures need reloading.
            /// </summary>
            /// <returns>True if any of the textures need reloading.</returns>
            public bool NeedsReload()
            {
                return blueNoise64LTex == null || bayerMatrixTex == null;
            }
        }

        [Obsolete("Moved to UniversalRenderPipelineRuntimeTextures on GraphicsSettings. #from(2023.3)")]
        [SerializeField]
        TextureResources m_Textures;

        /// <summary>
        /// Returns asset texture resources
        /// </summary>
        [Obsolete("Moved to UniversalRenderPipelineRuntimeTextures on GraphicsSettings. #from(2023.3)")]
        public TextureResources textures
        {
            get
            {
                if (m_Textures == null)
                    m_Textures = new TextureResources();

#if UNITY_EDITOR
                if (m_Textures.NeedsReload())
                    ResourceReloader.ReloadAllNullIn(this, packagePath);
#endif

                return m_Textures;
            }
        }
        
        /// <summary>
        /// Controls when URP renders via an intermediate texture.
        /// </summary>
        [Obsolete("This property is not used. #from(6000.3)", false)]
        public IntermediateTextureMode intermediateTextureMode
        {
            get => default;
            set {}
        }
    }

    public abstract partial class ScriptableRenderer
    {
        // Deprecated in 10.x
        /// <summary>
        /// The render target identifier for camera depth.
        /// This is obsolete, cameraDepth has been renamed to cameraDepthTarget.
        /// </summary>
        [Obsolete("cameraDepth has been renamed to cameraDepthTarget. #from(2021.1) #breakingFrom(2023.1) (UnityUpgradable) -> cameraDepthTarget", true)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public RenderTargetIdentifier cameraDepth
        {
            get => m_CameraDepthTarget.nameID;
        }
    }

    public abstract partial class ScriptableRendererData
    {
        /// <summary>
        /// Class contains references to shader resources used by Rendering Debugger.
        /// </summary>
        [Obsolete("Moved to UniversalRenderPipelineDebugShaders on GraphicsSettings. #from(2023.3)")]
        [Serializable, ReloadGroup]
        public sealed class DebugShaderResources
        {
            /// <summary>
            /// Debug shader used to output interpolated vertex attributes.
            /// </summary>
            [Obsolete("Moved to UniversalRenderPipelineDebugShaders on GraphicsSettings. #from(2023.3)")]
            [Reload("Shaders/Debug/DebugReplacement.shader")]
            public Shader debugReplacementPS;

            /// <summary>
            /// Debug shader used to output HDR Chromacity mapping.
            /// </summary>
            [Obsolete("Moved to UniversalRenderPipelineDebugShaders on GraphicsSettings. #from(2023.3)")]
            [Reload("Shaders/Debug/HDRDebugView.shader")]
            public Shader hdrDebugViewPS;

#if UNITY_EDITOR
            /// <summary>
            /// Debug shader used to output world position and world normal for the pixel under the cursor.
            /// </summary>
            [Obsolete("Moved to UniversalRenderPipelineDebugShaders on GraphicsSettings. #from(2023.3)")]
            [Reload("Shaders/Debug/ProbeVolumeSamplingDebugPositionNormal.compute")]
            public ComputeShader probeVolumeSamplingDebugComputeShader;
#endif
        }

        /// <summary>
        /// Container for shader resources used by Rendering Debugger.
        /// </summary>
        [Obsolete("Moved to UniversalRenderPipelineDebugShaders on GraphicsSettings. #from(2023.3)")]
        public DebugShaderResources debugShaders;

        /// <summary>
        /// Class contains references to shader resources used by APV.
        /// </summary>
        [Serializable, ReloadGroup]
        [Obsolete("Probe volume debug resource are now in the ProbeVolumeDebugResources class. #from(2023.3)")]
        public sealed class ProbeVolumeResources
        {
            /// <summary>
            /// Debug shader used to render probes in the volume.
            /// </summary>
            [Obsolete("This shader is now in the ProbeVolumeDebugResources class. #from(2023.3)")]
            public Shader probeVolumeDebugShader;

            /// <summary>
            /// Debug shader used to display fragmentation of the GPU memory.
            /// </summary>
            [Obsolete("This shader is now in the ProbeVolumeDebugResources class. #from(2023.3)")]
            public Shader probeVolumeFragmentationDebugShader;

            /// <summary>
            /// Debug shader used to draw the offset direction used for a probe.
            /// </summary>
            [Obsolete("This shader is now in the ProbeVolumeDebugResources class. #from(2023.3)")]
            public Shader probeVolumeOffsetDebugShader;

            /// <summary>
            /// Debug shader used to draw the sampling weights of the probe volume.
            /// </summary>
            [Obsolete("This shader is now in the ProbeVolumeDebugResources class. #from(2023.3)")]
            public Shader probeVolumeSamplingDebugShader;

            /// <summary>
            /// Debug mesh used to draw the sampling weights of the probe volume.
            /// </summary>
            [Obsolete("This shader is now in the ProbeVolumeDebugResources class. #from(2023.3)")]
            public Mesh probeSamplingDebugMesh;

            /// <summary>
            /// Texture with the numbers dor sampling weights.
            /// </summary>
            [Obsolete("This shader is now in the ProbeVolumeDebugResources class. #from(2023.3)")]
            public Texture2D probeSamplingDebugTexture;

            /// <summary>
            /// Compute Shader used for Blending.
            /// </summary>
            [Obsolete("This shader is now in the ProbeVolumeRuntimeResources class. #from(2023.3)")]
            public ComputeShader probeVolumeBlendStatesCS;
        }

        /// <summary>
        /// Probe volume resources used by URP
        /// </summary>
        [Obsolete("Probe volume debug resource are now in the ProbeVolumeDebugResources class. #from(2023.3)")]
        public ProbeVolumeResources probeVolumeResources;
    }

    public sealed partial class Bloom : VolumeComponent, IPostProcessComponent
    {
        // Deprecated in 13.x.x
        /// <summary>
        /// The number of final iterations to skip in the effect processing sequence.
        /// This is obsolete, please use maxIterations instead.
        /// </summary>
        [Obsolete("This is obsolete, please use maxIterations instead. #from(2022.2) #breakingFrom(2023.1)", true)]
        [Tooltip("The number of final iterations to skip in the effect processing sequence.")]
        public ClampedIntParameter skipIterations = new ClampedIntParameter(1, 0, 16);
    }

    /// <summary>
    /// Class containing shader resources needed in URP for XR.
    /// </summary>
    /// <seealso cref="Shader"/>
    [Serializable]
    [Obsolete("Moved to UniversalRenderPipelineRuntimeXRResources on GraphicsSettings. #from(2023.3)")]
    public class XRSystemData : ScriptableObject
    {
        /// <summary>
        /// Class containing shader resources used in URP for XR.
        /// </summary>
        [Serializable, ReloadGroup]
        [Obsolete("Moved to UniversalRenderPipelineRuntimeXRResources on GraphicsSettings. #from(2023.3)")]
        public sealed class ShaderResources
        {
            /// <summary>
            /// XR Occlusion mesh shader.
            /// </summary>
            [Reload("Shaders/XR/XROcclusionMesh.shader")]
            public Shader xrOcclusionMeshPS;

            /// <summary>
            /// XR Mirror View shader.
            /// </summary>
            [Reload("Shaders/XR/XRMirrorView.shader")]
            public Shader xrMirrorViewPS;
        }

        /// <summary>
        /// Shader resources used in URP for XR.
        /// </summary>
        [Obsolete("Moved to UniversalRenderPipelineRuntimeXRResources on GraphicsSettings. #from(2023.3)")]
        public ShaderResources shaders;
    }

    public partial class UniversalRendererData
    {
#if ENABLE_VR && ENABLE_XR_MODULE
        /// <summary>
        /// Shader resources needed in URP for XR.
        /// </summary>
        [Obsolete("Moved to UniversalRenderPipelineRuntimeXRResources on GraphicsSettings. #from(2023.3)")]
        //[Reload("Runtime/Data/XRSystemData.asset")]
        public XRSystemData xrSystemData;
#endif
    }

    /// Class containing shader and texture resources needed in URP.
    /// </summary>
    /// <seealso cref="Shader"/>
    /// <seealso cref="Material"/>
    [Obsolete("Moved to GraphicsSettings. #from(2023.3)")]
    public class UniversalRenderPipelineEditorResources : ScriptableObject
    {
        /// <summary>
        /// Class containing shader resources used in URP.
        /// </summary>
        [Serializable, ReloadGroup]
        [Obsolete("UniversalRenderPipelineEditorResources.ShaderResources is obsolete GraphicsSettings.TryGetRenderPipelineSettings<UniversalRenderPipelineEditorShaders>(). #from(2023.3)")]
        public sealed class ShaderResources
        {
            /// <summary>
            /// Autodesk Interactive ShaderGraph shader.
            /// </summary>
            [Reload("Shaders/AutodeskInteractive/AutodeskInteractive.shadergraph")]
            public Shader autodeskInteractivePS;

            /// <summary>
            /// Autodesk Interactive Transparent ShaderGraph shader.
            /// </summary>
            [Reload("Shaders/AutodeskInteractive/AutodeskInteractiveTransparent.shadergraph")]
            public Shader autodeskInteractiveTransparentPS;

            /// <summary>
            /// Autodesk Interactive Masked ShaderGraph shader.
            /// </summary>
            [Reload("Shaders/AutodeskInteractive/AutodeskInteractiveMasked.shadergraph")]
            public Shader autodeskInteractiveMaskedPS;

            /// <summary>
            /// Terrain Detail Lit shader.
            /// </summary>
            [Reload("Shaders/Terrain/TerrainDetailLit.shader")]
            public Shader terrainDetailLitPS;

            /// <summary>
            /// Terrain Detail Grass shader.
            /// </summary>
            [Reload("Shaders/Terrain/WavingGrass.shader")]
            public Shader terrainDetailGrassPS;

            /// <summary>
            /// Waving Grass Billboard shader.
            /// </summary>
            [Reload("Shaders/Terrain/WavingGrassBillboard.shader")]
            public Shader terrainDetailGrassBillboardPS;

            /// <summary>
            /// SpeedTree7 shader.
            /// </summary>
            [Reload("Shaders/Nature/SpeedTree7.shader")]
            public Shader defaultSpeedTree7PS;

            /// <summary>
            /// SpeedTree8 ShaderGraph shader.
            /// </summary>
            [Reload("Shaders/Nature/SpeedTree8_PBRLit.shadergraph")]
            public Shader defaultSpeedTree8PS;
        }

        /// <summary>
        /// Class containing material resources used in URP.
        /// </summary>
        [Serializable, ReloadGroup]
        [Obsolete("UniversalRenderPipelineEditorResources.MaterialResources is obsolete GraphicsSettings.TryGetRenderPipelineSettings<UniversalRenderPipelineEditorMaterials>(). #from(2023.3)")]
        public sealed class MaterialResources
        {
            /// <summary>
            /// Lit material.
            /// </summary>
            [Reload("Runtime/Materials/Lit.mat")]
            public Material lit;

            // particleLit is the URP default material for new particle systems.
            // ParticlesUnlit.mat is closest match to the built-in shader.
            // This is correct (current 22.2) despite the Lit/Unlit naming conflict.
            /// <summary>
            /// Particle Lit material.
            /// </summary>
            [Reload("Runtime/Materials/ParticlesUnlit.mat")]
            public Material particleLit;

            /// <summary>
            /// Terrain Lit material.
            /// </summary>
            [Reload("Runtime/Materials/TerrainLit.mat")]
            public Material terrainLit;

            /// <summary>
            /// Decal material.
            /// </summary>
            [Reload("Runtime/Materials/Decal.mat")]
            public Material decal;
        }

        /// <summary>
        /// Shader resources used in URP.
        /// </summary>
        [Obsolete("UniversalRenderPipelineEditorResources.ShaderResources is obsolete GraphicsSettings.TryGetRenderPipelineSettings<UniversalRenderPipelineEditorShaders>(). #from(2023.3)")]
        public ShaderResources shaders;

        /// <summary>
        /// Material resources used in URP.
        /// </summary>
        [Obsolete("UniversalRenderPipelineEditorResources.MaterialResources is obsolete GraphicsSettings.TryGetRenderPipelineSettings<UniversalRenderPipelineEditorMaterials>(). #from(2023.3)")]
        public MaterialResources materials;
    }

#if UNITY_EDITOR
    [UnityEditor.CustomEditor(typeof(UniversalRenderPipelineEditorResources), true)]
    [Obsolete("Deprectated alongside with UniversalRenderPipelineEditorResources. #from(2023.3)")]
    class UniversalRenderPipelineEditorResourcesEditor : UnityEditor.Editor
    {
        /// <inheritdoc/>
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            // Add a "Reload All" button in inspector when we are in developer's mode
            if (UnityEditor.EditorPrefs.GetBool("DeveloperMode") && GUILayout.Button("Reload All"))
            {
                var resources = target as UniversalRenderPipelineEditorResources;
                resources.materials = null;
                resources.shaders = null;
                ResourceReloader.ReloadAllNullIn(target, UniversalRenderPipelineAsset.packagePath);
            }
        }
    }
#endif

    /// <summary>
    /// Class containing shader resources used in URP.
    /// </summary>
    [Serializable, ReloadGroup]
    [Obsolete("Moved to UniversalRenderPipelineRuntimeShaders on GraphicsSettings. #from(2023.3)")]
    public sealed class ShaderResources
    {
        /// <summary>
        /// Blit shader.
        /// </summary>
        [Obsolete("Moved to UniversalRenderPipelineRuntimeShaders on GraphicsSettings. #from(2023.3)")]
        [Reload("Shaders/Utils/Blit.shader")]
        public Shader blitPS;

        /// <summary>
        /// Copy Depth shader.
        /// </summary>
        [Reload("Shaders/Utils/CopyDepth.shader")]
        [Obsolete("Moved to UniversalRenderPipelineRuntimeShaders on GraphicsSettings. #from(2023.3)")]
        public Shader copyDepthPS;

        /// <summary>
        /// Screen Space Shadows shader.
        /// </summary>
        [Obsolete("Obsolete, this feature will be supported by new 'ScreenSpaceShadows' renderer feature. #from(2023.3) #breakingFrom(2023.3)", true)]
        public Shader screenSpaceShadowPS = null;

        /// <summary>
        /// Sampling shader.
        /// </summary>
        [Obsolete("Moved to UniversalRenderPipelineRuntimeShaders on GraphicsSettings. #from(2023.3)")]
        [Reload("Shaders/Utils/Sampling.shader")]
        public Shader samplingPS;

        /// <summary>
        /// Stencil Deferred shader.
        /// </summary>
        [Reload("Shaders/Utils/StencilDeferred.shader")]
        [Obsolete("Moved to UniversalRenderPipelineRuntimeShaders on GraphicsSettings. #from(2023.3)")]
        public Shader stencilDeferredPS;

        /// <summary>
        /// Fallback error shader.
        /// </summary>
        [Reload("Shaders/Utils/FallbackError.shader")]
        [Obsolete("Moved to UniversalRenderPipelineRuntimeShaders on GraphicsSettings. #from(2023.3)")]
        public Shader fallbackErrorPS;

        /// <summary>
        /// Fallback loading shader.
        /// </summary>
        [Reload("Shaders/Utils/FallbackLoading.shader")]
        [Obsolete("Moved to UniversalRenderPipelineRuntimeShaders on GraphicsSettings. #from(2023.3)")]
        public Shader fallbackLoadingPS;

        /// <summary>
        /// Material Error shader.
        /// </summary>
        [Obsolete("Use fallbackErrorPS instead. #from(2023.3) #breakingFrom(2023.3)", true)]
        public Shader materialErrorPS = null;

        // Core blitter shaders, adapted from HDRP
        // TODO: move to core and share with HDRP
        [Reload("Shaders/Utils/CoreBlit.shader"), SerializeField]
        [Obsolete("Moved to UniversalRenderPipelineRuntimeShaders on GraphicsSettings. #from(2023.3)")]
        internal Shader coreBlitPS;

        [Reload("Shaders/Utils/CoreBlitColorAndDepth.shader"), SerializeField]
        [Obsolete("Moved to UniversalRenderPipelineRuntimeShaders on GraphicsSettings. #from(2023.3)")]
        internal Shader coreBlitColorAndDepthPS;

        /// <summary>
        /// Blit shader that blits UI Overlay and performs HDR encoding.
        /// </summary>
        [Reload("Shaders/Utils/BlitHDROverlay.shader"), SerializeField]
        [Obsolete("Moved to UniversalRenderPipelineRuntimeShaders on GraphicsSettings. #from(2023.3)")]
        internal Shader blitHDROverlay;

        /// <summary>
        /// Camera Motion Vectors shader.
        /// </summary>
        [Reload("Shaders/CameraMotionVectors.shader")]
        [Obsolete("Moved to UniversalRenderPipelineRuntimeShaders on GraphicsSettings. #from(2023.3)")]
        public Shader cameraMotionVector;

        /// <summary>
        /// Screen Space Lens Flare shader.
        /// </summary>
        [Reload("Shaders/PostProcessing/LensFlareScreenSpace.shader")]
        [Obsolete("Moved to UniversalRenderPipelineRuntimeShaders on GraphicsSettings. #from(2023.3)")]
        public Shader screenSpaceLensFlare;

        /// <summary>
        /// Data Driven Lens Flare shader.
        /// </summary>
        [Reload("Shaders/PostProcessing/LensFlareDataDriven.shader")]
        [Obsolete("Moved to UniversalRenderPipelineRuntimeShaders on GraphicsSettings. #from(2023.3)")]
        public Shader dataDrivenLensFlare;
    }
  
    partial class UniversalRenderPipelineGlobalSettings
    {
#pragma warning disable 0414
        [SerializeField, Obsolete("Keep for migration. #from(2023.2)")] internal ShaderStrippingSetting m_ShaderStrippingSetting = new();
        [SerializeField, Obsolete("Keep for migration. #from(2023.2)")] internal URPShaderStrippingSetting m_URPShaderStrippingSetting = new();
        [SerializeField, Obsolete("Keep for migration. #from(2023.2)")] internal Rendering.ShaderVariantLogLevel m_ShaderVariantLogLevel = Rendering.ShaderVariantLogLevel.Disabled;
        [SerializeField, Obsolete("Keep for migration. #from(2023.2)")] internal bool m_ExportShaderVariants = true;
        [SerializeField, Obsolete("Keep for migration. #from(2023.2)")] internal bool m_StripDebugVariants = true;
        [SerializeField, Obsolete("Keep for migration. #from(2023.2)")] internal bool m_StripUnusedPostProcessingVariants = false;
        [SerializeField, Obsolete("Keep for migration. #from(2023.2)")] internal bool m_StripUnusedVariants = true;
        [SerializeField, Obsolete("Keep for migration. #from(2023.2)")] internal bool m_StripScreenCoordOverrideVariants = true;
#pragma warning restore 0414

        /// <summary>
        /// If this property is true, Unity strips the LOD variants if the LOD cross-fade feature (UniversalRenderingPipelineAsset.enableLODCrossFade) is disabled.
        /// </summary>
        [Obsolete("No longer used as Shader Prefiltering automatically strips out unused LOD Crossfade variants. Please use the LOD Crossfade setting in the URP Asset to disable the feature if not used. #from(2023.1)")]
        public bool stripUnusedLODCrossFadeVariants { get => false; set { } }

        /// <summary>
        /// Controls whether debug display shaders for Rendering Debugger are available in Player builds.
        /// </summary>
        [Obsolete("Please use stripRuntimeDebugShaders instead. #from(2023.1)")]
        public bool supportRuntimeDebugDisplay = false;

        [SerializeField, Obsolete("Keep for migration. #from(2023.2)")] internal bool m_EnableRenderGraph;
    }

#if !URP_COMPATIBILITY_MODE
    internal struct DeprecationMessage
    {
        internal const string CompatibilityScriptingAPIHidden = "This rendering path is for Compatibility Mode only which has been deprecated and hidden behind URP_COMPATIBILITY_MODE define. This will do nothing.";
    }

    partial class UniversalCameraData
    {
        /// <summary>
        /// Returns the camera GPU projection matrix. This contains platform specific changes to handle y-flip and reverse z. Includes camera jitter if required by active features.
        /// Similar to <c>GL.GetGPUProjectionMatrix</c> but queries URP internal state to know if the pipeline is rendering to render texture.
        /// For more info on platform differences regarding camera projection check: https://docs.unity3d.com/Manual/SL-PlatformDifferences.html
        /// </summary>
        /// <param name="viewIndex"> View index in case of stereo rendering. By default <c>viewIndex</c> is set to 0. </param>
        /// <seealso cref="GL.GetGPUProjectionMatrix(Matrix4x4, bool)"/>
        /// <returns></returns>
        [Obsolete(DeprecationMessage.CompatibilityScriptingAPIHidden)]
        public Matrix4x4 GetGPUProjectionMatrix(int viewIndex = 0) => default;

        /// <summary>
        /// Returns the camera GPU projection matrix. This contains platform specific changes to handle y-flip and reverse z. Does not include any camera jitter.
        /// Similar to <c>GL.GetGPUProjectionMatrix</c> but queries URP internal state to know if the pipeline is rendering to render texture.
        /// For more info on platform differences regarding camera projection check: https://docs.unity3d.com/Manual/SL-PlatformDifferences.html
        /// </summary>
        /// <param name="viewIndex"> View index in case of stereo rendering. By default <c>viewIndex</c> is set to 0. </param>
        /// <seealso cref="GL.GetGPUProjectionMatrix(Matrix4x4, bool)"/>
        /// <returns></returns>
        [Obsolete(DeprecationMessage.CompatibilityScriptingAPIHidden)]
        public Matrix4x4 GetGPUProjectionMatrixNoJitter(int viewIndex = 0) => default;

        /// <summary>
        /// True if the camera device projection matrix is flipped. This happens when the pipeline is rendering
        /// to a render texture in non OpenGL platforms. If you are doing a custom Blit pass to copy camera textures
        /// (_CameraColorTexture, _CameraDepthAttachment) you need to check this flag to know if you should flip the
        /// matrix when rendering with for cmd.Draw* and reading from camera textures.
        /// </summary>
        /// <returns> True if the camera device projection matrix is flipped. </returns>
        [Obsolete(DeprecationMessage.CompatibilityScriptingAPIHidden)]
        public bool IsCameraProjectionMatrixFlipped() => default;
    }

    public abstract partial class ScriptableRenderPass
    {
        /// <summary>
        /// RTHandle alias for BuiltinRenderTextureType.CameraTarget which is the backbuffer.
        /// </summary>
        [Obsolete(DeprecationMessage.CompatibilityScriptingAPIHidden)]
        public static RTHandle k_CameraTarget = null;

        /// <summary>
        /// List for the g-buffer attachment handles.
        /// </summary>
        [Obsolete(DeprecationMessage.CompatibilityScriptingAPIHidden)]
        public RTHandle[] colorAttachmentHandles => null;

        /// <summary>
        /// The main color attachment handle.
        /// </summary>
        [Obsolete(DeprecationMessage.CompatibilityScriptingAPIHidden)]
        public RTHandle colorAttachmentHandle => null;

        /// <summary>
        /// The depth attachment handle.
        /// </summary>
        [Obsolete(DeprecationMessage.CompatibilityScriptingAPIHidden)]
        public RTHandle depthAttachmentHandle => null;

        /// <summary>
        /// The store actions for Color.
        /// </summary>
        [Obsolete(DeprecationMessage.CompatibilityScriptingAPIHidden)]
        public RenderBufferStoreAction[] colorStoreActions => null;

        /// <summary>
        /// The store actions for Depth.
        /// </summary>
        [Obsolete(DeprecationMessage.CompatibilityScriptingAPIHidden)]
        public RenderBufferStoreAction depthStoreAction => default;
        
        /// <summary>
        /// The flag to use when clearing.
        /// </summary>
        /// <seealso cref="ClearFlag"/>
        [Obsolete(DeprecationMessage.CompatibilityScriptingAPIHidden)]
        public ClearFlag clearFlag => default;

        /// <summary>
        /// The color value to use when clearing.
        /// </summary>
        [Obsolete(DeprecationMessage.CompatibilityScriptingAPIHidden)]
        public Color clearColor => default;

        /// <summary>
        /// Configures the Store Action for a color attachment of this render pass.
        /// </summary>
        /// <param name="storeAction">RenderBufferStoreAction to use</param>
        /// <param name="attachmentIndex">Index of the color attachment</param>
        [Obsolete(DeprecationMessage.CompatibilityScriptingAPIHidden)]
        public void ConfigureColorStoreAction(RenderBufferStoreAction storeAction, uint attachmentIndex = 0) { }

        /// <summary>
        /// Configures the Store Actions for all the color attachments of this render pass.
        /// </summary>
        /// <param name="storeActions">Array of RenderBufferStoreActions to use</param>
        [Obsolete(DeprecationMessage.CompatibilityScriptingAPIHidden)]
        public void ConfigureColorStoreActions(RenderBufferStoreAction[] storeActions) { }

        /// <summary>
        /// Configures the Store Action for the depth attachment of this render pass.
        /// </summary>
        /// <param name="storeAction">RenderBufferStoreAction to use</param>
        [Obsolete(DeprecationMessage.CompatibilityScriptingAPIHidden)]
        public void ConfigureDepthStoreAction(RenderBufferStoreAction storeAction) { }

        /// <summary>
        /// Resets render targets to default.
        /// This method effectively reset changes done by ConfigureTarget.
        /// </summary>
        /// <seealso cref="ConfigureTarget"/>
        [Obsolete(DeprecationMessage.CompatibilityScriptingAPIHidden)]
        public void ResetTarget() { }

        /// <summary>
        /// Configures render targets for this render pass. Call this instead of CommandBuffer.SetRenderTarget.
        /// This method should be called inside Configure.
        /// </summary>
        /// <param name="colorAttachment">Color attachment handle.</param>
        /// <param name="depthAttachment">Depth attachment handle.</param>
        /// <seealso cref="Configure"/>
        [Obsolete(DeprecationMessage.CompatibilityScriptingAPIHidden)]
        public void ConfigureTarget(RTHandle colorAttachment, RTHandle depthAttachment) { }

        /// <summary>
        /// Configures render targets for this render pass. Call this instead of CommandBuffer.SetRenderTarget.
        /// This method should be called inside Configure.
        /// </summary>
        /// <param name="colorAttachments">Color attachment handle.</param>
        /// <param name="depthAttachment">Depth attachment handle.</param>
        /// <seealso cref="Configure"/>
        [Obsolete(DeprecationMessage.CompatibilityScriptingAPIHidden)]
        public void ConfigureTarget(RTHandle[] colorAttachments, RTHandle depthAttachment) { }

        /// <summary>
        /// Configures render targets for this render pass. Call this instead of CommandBuffer.SetRenderTarget.
        /// This method should be called inside Configure.
        /// </summary>
        /// <param name="colorAttachment">Color attachment handle.</param>
        /// <seealso cref="Configure"/>
        [Obsolete(DeprecationMessage.CompatibilityScriptingAPIHidden)]
        public void ConfigureTarget(RTHandle colorAttachment) { }

        /// <summary>
        /// Configures render targets for this render pass. Call this instead of CommandBuffer.SetRenderTarget.
        /// This method should be called inside Configure.
        /// </summary>
        /// <param name="colorAttachments">Color attachment handle.</param>
        /// <seealso cref="Configure"/>
        [Obsolete(DeprecationMessage.CompatibilityScriptingAPIHidden)]
        public void ConfigureTarget(RTHandle[] colorAttachments) { }

        /// <summary>
        /// Configures clearing for the render targets for this render pass. Call this inside Configure.
        /// </summary>
        /// <param name="clearFlag">ClearFlag containing information about what targets to clear.</param>
        /// <param name="clearColor">Clear color.</param>
        /// <seealso cref="Configure"/>
        [Obsolete(DeprecationMessage.CompatibilityScriptingAPIHidden)]
        public void ConfigureClear(ClearFlag clearFlag, Color clearColor) { }

        /// <summary>
        /// This method is called by the renderer before rendering a camera
        /// Override this method if you need to to configure render targets and their clear state, and to create temporary render target textures.
        /// If a render pass doesn't override this method, this render pass renders to the active Camera's render target.
        /// You should never call CommandBuffer.SetRenderTarget. Instead call <c>ConfigureTarget</c> and <c>ConfigureClear</c>.
        /// </summary>
        /// <param name="cmd">CommandBuffer to enqueue rendering commands. This will be executed by the pipeline.</param>
        /// <param name="renderingData">Current rendering state information</param>
        /// <seealso cref="ConfigureTarget"/>
        /// <seealso cref="ConfigureClear"/>
        [Obsolete(DeprecationMessage.CompatibilityScriptingAPIHidden)]
        public virtual void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData) { }

        /// <summary>
        /// This method is called by the renderer before executing the render pass.
        /// Override this method if you need to to configure render targets and their clear state, and to create temporary render target textures.
        /// If a render pass doesn't override this method, this render pass renders to the active Camera's render target.
        /// You should never call CommandBuffer.SetRenderTarget. Instead call <c>ConfigureTarget</c> and <c>ConfigureClear</c>.
        /// </summary>
        /// <param name="cmd">CommandBuffer to enqueue rendering commands. This will be executed by the pipeline.</param>
        /// <param name="cameraTextureDescriptor">Render texture descriptor of the camera render target.</param>
        /// <seealso cref="ConfigureTarget"/>
        /// <seealso cref="ConfigureClear"/>
        [Obsolete(DeprecationMessage.CompatibilityScriptingAPIHidden)]
        public virtual void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor) { }

        /// <summary>
        /// Called upon finish rendering a camera stack. You can use this callback to release any resources created
        /// by this render pass that need to be cleanup once all cameras in the stack have finished rendering.
        /// This method will be called once after rendering the last camera in the camera stack.
        /// Cameras that don't have an explicit camera stack are also considered stacked rendering.
        /// In that case the Base camera is the first and last camera in the stack.
        /// </summary>
        /// <param name="cmd">Use this CommandBuffer to cleanup any generated data</param>
        [Obsolete(DeprecationMessage.CompatibilityScriptingAPIHidden)]
        public virtual void OnFinishCameraStackRendering(CommandBuffer cmd) { }

        /// <summary>
        /// Execute the pass. This is where custom rendering occurs. Specific details are left to the implementation
        /// </summary>
        /// <param name="context">Use this render context to issue any draw commands during execution</param>
        /// <param name="renderingData">Current rendering state information</param>
        [Obsolete(DeprecationMessage.CompatibilityScriptingAPIHidden)]
        public virtual void Execute(ScriptableRenderContext context, ref RenderingData renderingData) { }

        /// <summary>
        /// Add a blit command to the context for execution. This changes the active render target in the ScriptableRenderer to
        /// destination.
        /// </summary>
        /// <param name="cmd">Command buffer to record command for execution.</param>
        /// <param name="source">Source texture or target handle to blit from.</param>
        /// <param name="destination">Destination texture or target handle to blit into. This becomes the renderer active render target.</param>
        /// <param name="material">Material to use.</param>
        /// <param name="passIndex">Shader pass to use. Default is 0.</param>
        /// <seealso cref="ScriptableRenderer"/>
        [Obsolete(DeprecationMessage.CompatibilityScriptingAPIHidden)]
        public void Blit(CommandBuffer cmd, RTHandle source, RTHandle destination, Material material = null, int passIndex = 0) { }

        /// <summary>
        /// Add a blit command to the context for execution. This applies the material to the color target.
        /// </summary>
        /// <param name="cmd">Command buffer to record command for execution.</param>
        /// <param name="data">RenderingData to access the active renderer.</param>
        /// <param name="material">Material to use.</param>
        /// <param name="passIndex">Shader pass to use. Default is 0.</param>
        [Obsolete(DeprecationMessage.CompatibilityScriptingAPIHidden)]
        public void Blit(CommandBuffer cmd, ref RenderingData data, Material material, int passIndex = 0) { }

        /// <summary>
        /// Add a blit command to the context for execution. This applies the material to the color target.
        /// </summary>
        /// <param name="cmd">Command buffer to record command for execution.</param>
        /// <param name="data">RenderingData to access the active renderer.</param>
        /// <param name="source">Source texture or target identifier to blit from.</param>
        /// <param name="material">Material to use.</param>
        /// <param name="passIndex">Shader pass to use. Default is 0.</param>
        [Obsolete(DeprecationMessage.CompatibilityScriptingAPIHidden)]
        public void Blit(CommandBuffer cmd, ref RenderingData data, RTHandle source, Material material, int passIndex = 0) { }
    }

#if ENABLE_VR && ENABLE_XR_MODULE
    partial class XROcclusionMeshPass
    {
        /// <summary>
        /// Used to indicate if the active target of the pass is the back buffer
        /// </summary>
        [Obsolete(DeprecationMessage.CompatibilityScriptingAPIHidden)]
        public bool m_IsActiveTargetBackBuffer; 
        
        /// <inheritdoc/>
        [Obsolete(DeprecationMessage.CompatibilityScriptingAPIHidden)]
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData) { }
    }
#endif

    partial class DecalRendererFeature
    {
        /// <inheritdoc />
        [Obsolete(DeprecationMessage.CompatibilityScriptingAPIHidden)]
        public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData) { }
    }

    partial class ScriptableRenderer
    {
        /// <summary>
        /// Override to provide a custom profiling name
        /// </summary>
        [Obsolete(DeprecationMessage.CompatibilityScriptingAPIHidden)]
        protected ProfilingSampler profilingExecute { get; set; }
        
        /// <summary>
        /// Set camera matrices. This method will set <c>UNITY_MATRIX_V</c>, <c>UNITY_MATRIX_P</c>, <c>UNITY_MATRIX_VP</c> to the camera matrices.
        /// Additionally this will also set <c>unity_CameraProjection</c> and <c>unity_CameraProjection</c>.
        /// If <c>setInverseMatrices</c> is set to true this function will also set <c>UNITY_MATRIX_I_V</c> and <c>UNITY_MATRIX_I_VP</c>.
        /// This function has no effect when rendering in stereo. When in stereo rendering you cannot override camera matrices.
        /// If you need to set general purpose view and projection matrices call <see cref="SetViewAndProjectionMatrices(CommandBuffer, Matrix4x4, Matrix4x4, bool)"/> instead.
        /// </summary>
        /// <param name="cmd">CommandBuffer to submit data to GPU.</param>
        /// <param name="cameraData">CameraData containing camera matrices information.</param>
        /// <param name="setInverseMatrices">Set this to true if you also need to set inverse camera matrices.</param>
        [Obsolete(DeprecationMessage.CompatibilityScriptingAPIHidden)]
        public static void SetCameraMatrices(CommandBuffer cmd, ref CameraData cameraData, bool setInverseMatrices) { }

        /// <summary>
        /// Set camera matrices. This method will set <c>UNITY_MATRIX_V</c>, <c>UNITY_MATRIX_P</c>, <c>UNITY_MATRIX_VP</c> to camera matrices.
        /// Additionally this will also set <c>unity_CameraProjection</c> and <c>unity_CameraProjection</c>.
        /// If <c>setInverseMatrices</c> is set to true this function will also set <c>UNITY_MATRIX_I_V</c> and <c>UNITY_MATRIX_I_VP</c>.
        /// This function has no effect when rendering in stereo. When in stereo rendering you cannot override camera matrices.
        /// If you need to set general purpose view and projection matrices call <see cref="SetViewAndProjectionMatrices(CommandBuffer, Matrix4x4, Matrix4x4, bool)"/> instead.
        /// </summary>
        /// <param name="cmd">CommandBuffer to submit data to GPU.</param>
        /// <param name="cameraData">CameraData containing camera matrices information.</param>
        /// <param name="setInverseMatrices">Set this to true if you also need to set inverse camera matrices.</param>
        [Obsolete(DeprecationMessage.CompatibilityScriptingAPIHidden)]
        public static void SetCameraMatrices(CommandBuffer cmd, UniversalCameraData cameraData, bool setInverseMatrices) { }
        
        /// <summary>
        /// Returns the camera color target for this renderer.
        /// It's only valid to call cameraColorTargetHandle in the scope of <c>ScriptableRenderPass</c>.
        /// <seealso cref="ScriptableRenderPass"/>.
        /// </summary>
        [Obsolete(DeprecationMessage.CompatibilityScriptingAPIHidden)]
        public RTHandle cameraColorTargetHandle { get => null; set { } }

        /// <summary>
        /// Returns the camera depth target for this renderer.
        /// It's only valid to call cameraDepthTargetHandle in the scope of <c>ScriptableRenderPass</c>.
        /// <seealso cref="ScriptableRenderPass"/>.
        /// </summary>
        [Obsolete(DeprecationMessage.CompatibilityScriptingAPIHidden)]
        public RTHandle cameraDepthTargetHandle { get => null; set { } }
        
        /// <summary>
        /// Configures the camera target.
        /// </summary>
        /// <param name="colorTarget">Camera color target. Pass k_CameraTarget if rendering to backbuffer.</param>
        /// <param name="depthTarget">Camera depth target. Pass k_CameraTarget if color has depth or rendering to backbuffer.</param>
        [Obsolete(DeprecationMessage.CompatibilityScriptingAPIHidden)]
        public void ConfigureCameraTarget(RTHandle colorTarget, RTHandle depthTarget) { }

        /// <summary>
        /// Configures the render passes that will execute for this renderer.
        /// This method is called per-camera every frame.
        /// </summary>
        /// <param name="context">Use this render context to issue any draw commands during execution.</param>
        /// <param name="renderingData">Current render state information.</param>
        /// <seealso cref="ScriptableRenderPass"/>
        /// <seealso cref="ScriptableRendererFeature"/>
        [Obsolete(DeprecationMessage.CompatibilityScriptingAPIHidden)]
        public virtual void Setup(ScriptableRenderContext context, ref RenderingData renderingData) { }

        /// <summary>
        /// Override this method to implement the lighting setup for the renderer. You can use this to
        /// compute and upload light CBUFFER for example.
        /// </summary>
        /// <param name="context">Use this render context to issue any draw commands during execution.</param>
        /// <param name="renderingData">Current render state information.</param>
        [Obsolete(DeprecationMessage.CompatibilityScriptingAPIHidden)]
        public virtual void SetupLights(ScriptableRenderContext context, ref RenderingData renderingData) { }

        /// <summary>
        /// Execute the enqueued render passes. This automatically handles editor and stereo rendering.
        /// </summary>
        /// <param name="context">Use this render context to issue any draw commands during execution.</param>
        /// <param name="renderingData">Current render state information.</param>
        [Obsolete(DeprecationMessage.CompatibilityScriptingAPIHidden)]
        public void Execute(ScriptableRenderContext context, ref RenderingData renderingData) { }

        /// <summary>
        /// Calls <c>Setup</c> for each feature added to this renderer.
        /// <seealso cref="ScriptableRendererFeature.SetupRenderPasses(ScriptableRenderer, in RenderingData)"/>
        /// </summary>
        /// <param name="renderingData"></param>
        [Obsolete(DeprecationMessage.CompatibilityScriptingAPIHidden)]
        protected void SetupRenderPasses(in RenderingData renderingData) { }
    }

    partial class ScriptableRendererFeature
    {
        /// <summary>
        /// Callback after render targets are initialized. This allows for accessing targets from renderer after they are created and ready.
        /// </summary>
        /// <param name="renderer">Renderer used for adding render passes.</param>
        /// <param name="renderingData">Rendering state. Use this to setup render passes.</param>
        [Obsolete(DeprecationMessage.CompatibilityScriptingAPIHidden)]
        public virtual void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData) { }
    }

    partial struct CameraData
    {
        /// <summary>
        /// Returns the camera GPU projection matrix. This contains platform specific changes to handle y-flip and reverse z. Includes camera jitter if required by active features.
        /// Similar to <c>GL.GetGPUProjectionMatrix</c> but queries URP internal state to know if the pipeline is rendering to render texture.
        /// For more info on platform differences regarding camera projection check: https://docs.unity3d.com/Manual/SL-PlatformDifferences.html
        /// </summary>
        /// <param name="viewIndex"> View index in case of stereo rendering. By default <c>viewIndex</c> is set to 0. </param>
        /// <seealso cref="GL.GetGPUProjectionMatrix(Matrix4x4, bool)"/>
        /// <returns></returns>
        [Obsolete(DeprecationMessage.CompatibilityScriptingAPIHidden)]
        public Matrix4x4 GetGPUProjectionMatrix(int viewIndex = 0) => default;

        /// <summary>
        /// Returns the camera GPU projection matrix. This contains platform specific changes to handle y-flip and reverse z. Does not include any camera jitter.
        /// Similar to <c>GL.GetGPUProjectionMatrix</c> but queries URP internal state to know if the pipeline is rendering to render texture.
        /// For more info on platform differences regarding camera projection check: https://docs.unity3d.com/Manual/SL-PlatformDifferences.html
        /// </summary>
        /// <param name="viewIndex"> View index in case of stereo rendering. By default <c>viewIndex</c> is set to 0. </param>
        /// <seealso cref="GL.GetGPUProjectionMatrix(Matrix4x4, bool)"/>
        /// <returns></returns>
        [Obsolete(DeprecationMessage.CompatibilityScriptingAPIHidden)]
        public Matrix4x4 GetGPUProjectionMatrixNoJitter(int viewIndex = 0) => default;

        /// <summary>
        /// True if the camera device projection matrix is flipped. This happens when the pipeline is rendering
        /// to a render texture in non OpenGL platforms. If you are doing a custom Blit pass to copy camera textures
        /// (_CameraColorTexture, _CameraDepthAttachment) you need to check this flag to know if you should flip the
        /// matrix when rendering with for cmd.Draw* and reading from camera textures.
        /// </summary>
        /// <returns> True if the camera device projection matrix is flipped. </returns>
        [Obsolete(DeprecationMessage.CompatibilityScriptingAPIHidden)]
        public bool IsCameraProjectionMatrixFlipped() => default;
    }

    namespace Internal
    {
        partial class AdditionalLightsShadowCasterPass
        {
            /// <inheritdoc/>
            [Obsolete(DeprecationMessage.CompatibilityScriptingAPIHidden)]
            public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor) { }

            /// <inheritdoc/>
            [Obsolete(DeprecationMessage.CompatibilityScriptingAPIHidden)]
            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData) { }
        }

        partial class ColorGradingLutPass
        {
            /// <inheritdoc/>
            [Obsolete(DeprecationMessage.CompatibilityScriptingAPIHidden)]
            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData) { }
        }

        partial class CopyColorPass
        {
            /// <inheritdoc />
            [Obsolete(DeprecationMessage.CompatibilityScriptingAPIHidden)]
            public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData) { }

            /// <inheritdoc/>
            [Obsolete(DeprecationMessage.CompatibilityScriptingAPIHidden)]
            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData) { }
        }

        partial class CopyDepthPass
        {
            /// <inheritdoc />
            [Obsolete(DeprecationMessage.CompatibilityScriptingAPIHidden)]
            public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData) { }
        
            /// <inheritdoc/>
            [Obsolete(DeprecationMessage.CompatibilityScriptingAPIHidden)]
            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData) { }
        }

        partial class DepthNormalOnlyPass
        {
            /// <inheritdoc/>
            [Obsolete(DeprecationMessage.CompatibilityScriptingAPIHidden)]
            public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData) { }
        
            /// <inheritdoc/>
            [Obsolete(DeprecationMessage.CompatibilityScriptingAPIHidden)]
            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData) { }
        }

        partial class DepthOnlyPass
        {
            /// <inheritdoc />
            [Obsolete(DeprecationMessage.CompatibilityScriptingAPIHidden)]
            public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData) { }
        
            /// <inheritdoc/>
            [Obsolete(DeprecationMessage.CompatibilityScriptingAPIHidden)]
            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData) { }
        }
        
        partial class DrawObjectsPass
        {
            /// <summary>
            /// Used to indicate if the active target of the pass is the back buffer
            /// </summary>
            [Obsolete(DeprecationMessage.CompatibilityScriptingAPIHidden)]
            public bool m_IsActiveTargetBackBuffer; // TODO: Remove this when we remove non-RG path
            
            /// <summary>
            /// Sets up the pass.
            /// </summary>
            /// <param name="colorAttachment">Color attachment handle.</param>
            /// <param name="renderingLayersTexture">Texture used with rendering layers.</param>
            /// <param name="depthAttachment">Depth attachment handle.</param>
            /// <exception cref="ArgumentException"></exception>
            [Obsolete(DeprecationMessage.CompatibilityScriptingAPIHidden)]
            public void Setup(RTHandle colorAttachment, RTHandle renderingLayersTexture, RTHandle depthAttachment) { }

            /// <inheritdoc/>
            [Obsolete(DeprecationMessage.CompatibilityScriptingAPIHidden)]
            public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor) { }

            /// <inheritdoc/>
            [Obsolete(DeprecationMessage.CompatibilityScriptingAPIHidden)]
            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData) { }
        }
        
        partial class ForwardLights
        {
            /// <summary>
            /// Sets up the keywords and data for forward lighting.
            /// </summary>
            /// <param name="context"></param>
            /// <param name="renderingData"></param>
            [Obsolete(DeprecationMessage.CompatibilityScriptingAPIHidden)]
            public void Setup(ScriptableRenderContext context, ref RenderingData renderingData) { }
        }
        
        partial class FinalBlitPass
        {
            /// <inheritdoc/>
            [Obsolete(DeprecationMessage.CompatibilityScriptingAPIHidden)]
            public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData) { }

            /// <inheritdoc/>
            [Obsolete(DeprecationMessage.CompatibilityScriptingAPIHidden)]
            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData) { }
        }

        partial class MainLightShadowCasterPass
        {
            /// <inheritdoc />
            [Obsolete(DeprecationMessage.CompatibilityScriptingAPIHidden)]
            public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor) { }

            /// <inheritdoc/>
            [Obsolete(DeprecationMessage.CompatibilityScriptingAPIHidden)]
            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData) { }
        }
    }

    partial class DrawSkyboxPass
    {
        /// <inheritdoc/>
        [Obsolete(DeprecationMessage.CompatibilityScriptingAPIHidden)]
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData) { }
    }

    partial class RenderObjectsPass
    {
        /// <inheritdoc/>
        [Obsolete(DeprecationMessage.CompatibilityScriptingAPIHidden)]
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData) { }
    }

    partial class UniversalRenderer
    {
        /// <inheritdoc />
        [Obsolete(DeprecationMessage.CompatibilityScriptingAPIHidden)]
        public override void Setup(ScriptableRenderContext context, ref RenderingData renderingData) { }

        /// <inheritdoc />
        [Obsolete(DeprecationMessage.CompatibilityScriptingAPIHidden)]
        public override void SetupLights(ScriptableRenderContext context, ref RenderingData renderingData) { }
    }
#endif //!URP_COMPATIBILITY_MODE
}
