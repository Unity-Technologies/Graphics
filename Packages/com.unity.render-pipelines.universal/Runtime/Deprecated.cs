// This file should be used as a container for things on its
// way to being deprecated and removed in future releases
using System;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

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
            [Obsolete("AdditionalLightsShadowCasterPass.m_AdditionalShadowsBufferId was deprecated. Shadow slice matrix is now passed to the GPU using an entry in buffer m_AdditionalLightsWorldToShadow_SSBO", true)]
            public static int m_AdditionalShadowsBufferId;

            /// <summary>
            /// The ID for the additional shadows buffer ID.
            /// This has been deprecated. hadow slice index is now passed to the GPU using last member of an entry in buffer m_AdditionalShadowParams_SSBO.
            /// </summary>
            [Obsolete("AdditionalLightsShadowCasterPass.m_AdditionalShadowsIndicesId was deprecated. Shadow slice index is now passed to the GPU using last member of an entry in buffer m_AdditionalShadowParams_SSBO", true)]
            public static int m_AdditionalShadowsIndicesId;
        }
    }

    /// <summary>
    /// Previously contained the settings to control how many cascades to use. It is now deprecated.
    /// </summary>
    [Obsolete("This is obsolete, please use shadowCascadeCount instead.", true)]
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
    [Obsolete("This is obsolete, UnityEngine.Rendering.ShaderVariantLogLevel instead.", true)]
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
        [Obsolete("Editor resources are stored directly into GraphicsSettings. #from(23.3)", false)]
        public static readonly string editorResourcesGUID = "a3d8d823eedde654bb4c11a1cfaf1abb";
        #endif

        [SerializeField] int m_ShaderVariantLogLevel;

#pragma warning disable 618 // Obsolete warning
        /// <summary>
        /// Previously returned the shader variant log level for this Render Pipeline Asset but is now deprecated.
        /// </summary>
        [Obsolete("Use GraphicsSettings.GetRenderPipelineSettings<ShaderStrippingSetting>().shaderVariantLogLevel instead.", true)]
        public ShaderVariantLogLevel shaderVariantLogLevel
        {
            get => (ShaderVariantLogLevel)GraphicsSettings.GetRenderPipelineSettings<ShaderStrippingSetting>().shaderVariantLogLevel;
            set => GraphicsSettings.GetRenderPipelineSettings<ShaderStrippingSetting>().shaderVariantLogLevel = (Rendering.ShaderVariantLogLevel)value;
        }
#pragma warning restore 618 // Obsolete warning

#pragma warning disable 618 // Obsolete warning
        [Obsolete("This is obsolete, please use shadowCascadeCount instead.", false)]
        [SerializeField] ShadowCascadesOption m_ShadowCascades = ShadowCascadesOption.NoCascades;

        /// <summary>
        /// Previously used insted of shadowCascadeCount. Please use that instead.
        /// </summary>
        [Obsolete("This is obsolete, please use shadowCascadeCount instead.", true)]
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
        [Obsolete("Moved to UniversalRenderPipelineRuntimeTextures on GraphicsSettings. #from(2023.3)", false)]
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

        [Obsolete("Moved to UniversalRenderPipelineRuntimeTextures on GraphicsSettings. #from(2023.3)", false)]
        [SerializeField]
        TextureResources m_Textures;

        /// <summary>
        /// Returns asset texture resources
        /// </summary>
        [Obsolete("Moved to UniversalRenderPipelineRuntimeTextures on GraphicsSettings. #from(2023.3)", false)]
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
    }

    public abstract partial class ScriptableRenderer
    {
        // Deprecated in 10.x
        /// <summary>
        /// The render target identifier for camera depth.
        /// This is obsolete, cameraDepth has been renamed to cameraDepthTarget.
        /// </summary>
        [Obsolete("cameraDepth has been renamed to cameraDepthTarget. (UnityUpgradable) -> cameraDepthTarget", true)]
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
        [Obsolete("Moved to UniversalRenderPipelineDebugShaders on GraphicsSettings. #from(2023.3)", false)]
        [Serializable, ReloadGroup]
        public sealed class DebugShaderResources
        {
            /// <summary>
            /// Debug shader used to output interpolated vertex attributes.
            /// </summary>
            [Obsolete("Moved to UniversalRenderPipelineDebugShaders on GraphicsSettings. #from(2023.3)", false)]
            [Reload("Shaders/Debug/DebugReplacement.shader")]
            public Shader debugReplacementPS;

            /// <summary>
            /// Debug shader used to output HDR Chromacity mapping.
            /// </summary>
            [Obsolete("Moved to UniversalRenderPipelineDebugShaders on GraphicsSettings. #from(2023.3)", false)]
            [Reload("Shaders/Debug/HDRDebugView.shader")]
            public Shader hdrDebugViewPS;

#if UNITY_EDITOR
            /// <summary>
            /// Debug shader used to output world position and world normal for the pixel under the cursor.
            /// </summary>
            [Obsolete("Moved to UniversalRenderPipelineDebugShaders on GraphicsSettings. #from(2023.3)", false)]
            [Reload("Shaders/Debug/ProbeVolumeSamplingDebugPositionNormal.compute")]
            public ComputeShader probeVolumeSamplingDebugComputeShader;
#endif
        }

        /// <summary>
        /// Container for shader resources used by Rendering Debugger.
        /// </summary>
        [Obsolete("Moved to UniversalRenderPipelineDebugShaders on GraphicsSettings. #from(2023.3)", false)]
        public DebugShaderResources debugShaders;

        /// <summary>
        /// Class contains references to shader resources used by APV.
        /// </summary>
        [Serializable, ReloadGroup]
        [Obsolete("Probe volume debug resource are now in the ProbeVolumeDebugResources class.")]
        public sealed class ProbeVolumeResources
        {
            /// <summary>
            /// Debug shader used to render probes in the volume.
            /// </summary>
            [Obsolete("This shader is now in the ProbeVolumeDebugResources class.")]
            public Shader probeVolumeDebugShader;

            /// <summary>
            /// Debug shader used to display fragmentation of the GPU memory.
            /// </summary>
            [Obsolete("This shader is now in the ProbeVolumeDebugResources class.")]
            public Shader probeVolumeFragmentationDebugShader;

            /// <summary>
            /// Debug shader used to draw the offset direction used for a probe.
            /// </summary>
            [Obsolete("This shader is now in the ProbeVolumeDebugResources class.")]
            public Shader probeVolumeOffsetDebugShader;

            /// <summary>
            /// Debug shader used to draw the sampling weights of the probe volume.
            /// </summary>
            [Obsolete("This shader is now in the ProbeVolumeDebugResources class.")]
            public Shader probeVolumeSamplingDebugShader;

            /// <summary>
            /// Debug mesh used to draw the sampling weights of the probe volume.
            /// </summary>
            [Obsolete("This shader is now in the ProbeVolumeDebugResources class.")]
            public Mesh probeSamplingDebugMesh;

            /// <summary>
            /// Texture with the numbers dor sampling weights.
            /// </summary>
            [Obsolete("This shader is now in the ProbeVolumeDebugResources class.")]
            public Texture2D probeSamplingDebugTexture;

            /// <summary>
            /// Compute Shader used for Blending.
            /// </summary>
            [Obsolete("This shader is now in the ProbeVolumeRuntimeResources class.")]
            public ComputeShader probeVolumeBlendStatesCS;
        }

        /// <summary>
        /// Probe volume resources used by URP
        /// </summary>
        [Obsolete("Probe volume debug resource are now in the ProbeVolumeDebugResources class.")]
        public ProbeVolumeResources probeVolumeResources;
    }

    public sealed partial class Bloom : VolumeComponent, IPostProcessComponent
    {
        // Deprecated in 13.x.x
        /// <summary>
        /// The number of final iterations to skip in the effect processing sequence.
        /// This is obsolete, please use maxIterations instead.
        /// </summary>
        [Obsolete("This is obsolete, please use maxIterations instead.", true)]
        [Tooltip("The number of final iterations to skip in the effect processing sequence.")]
        public ClampedIntParameter skipIterations = new ClampedIntParameter(1, 0, 16);
    }

    /// <summary>
    /// Class containing shader resources needed in URP for XR.
    /// </summary>
    /// <seealso cref="Shader"/>
    [Serializable]
    [Obsolete("Moved to UniversalRenderPipelineRuntimeXRResources on GraphicsSettings. #from(2023.3)", false)]
    public class XRSystemData : ScriptableObject
    {
        /// <summary>
        /// Class containing shader resources used in URP for XR.
        /// </summary>
        [Serializable, ReloadGroup]
        [Obsolete("Moved to UniversalRenderPipelineRuntimeXRResources on GraphicsSettings. #from(2023.3)", false)]
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
        [Obsolete("Moved to UniversalRenderPipelineRuntimeXRResources on GraphicsSettings. #from(2023.3)", false)]
        public ShaderResources shaders;
    }

    public partial class UniversalRendererData
    {
#if ENABLE_VR && ENABLE_XR_MODULE
        /// <summary>
        /// Shader resources needed in URP for XR.
        /// </summary>
        [Obsolete("Moved to UniversalRenderPipelineRuntimeXRResources on GraphicsSettings. #from(2023.3)", false)]
        //[Reload("Runtime/Data/XRSystemData.asset")]
        public XRSystemData xrSystemData;
#endif
    }

    /// Class containing shader and texture resources needed in URP.
    /// </summary>
    /// <seealso cref="Shader"/>
    /// <seealso cref="Material"/>
    [Obsolete("Moved to GraphicsSettings. #from(23.3)", false)]
    public class UniversalRenderPipelineEditorResources : ScriptableObject
    {
        /// <summary>
        /// Class containing shader resources used in URP.
        /// </summary>
        [Serializable, ReloadGroup]
        [Obsolete("UniversalRenderPipelineEditorResources.ShaderResources is obsolete GraphicsSettings.TryGetRenderPipelineSettings<UniversalRenderPipelineEditorShaders>(). #from(23.3)", false)]
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
        [Obsolete("UniversalRenderPipelineEditorResources.MaterialResources is obsolete GraphicsSettings.TryGetRenderPipelineSettings<UniversalRenderPipelineEditorMaterials>(). #from(23.3)", false)]
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
        [Obsolete("UniversalRenderPipelineEditorResources.ShaderResources is obsolete GraphicsSettings.TryGetRenderPipelineSettings<UniversalRenderPipelineEditorShaders>(). #from(23.3)", false)]
        public ShaderResources shaders;

        /// <summary>
        /// Material resources used in URP.
        /// </summary>
        [Obsolete("UniversalRenderPipelineEditorResources.MaterialResources is obsolete GraphicsSettings.TryGetRenderPipelineSettings<UniversalRenderPipelineEditorMaterials>(). #from(23.3)", false)]
        public MaterialResources materials;
    }

#if UNITY_EDITOR
    [UnityEditor.CustomEditor(typeof(UniversalRenderPipelineEditorResources), true)]
    [Obsolete("Deprectated alongside with UniversalRenderPipelineEditorResources. #from(23.3)", false)]
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
    [Obsolete("Moved to UniversalRenderPipelineRuntimeShaders on GraphicsSettings. #from(2023.3)", false)]
    public sealed class ShaderResources
    {
        /// <summary>
        /// Blit shader.
        /// </summary>
        [Obsolete("Moved to UniversalRenderPipelineRuntimeShaders on GraphicsSettings. #from(2023.3)", false)]
        [Reload("Shaders/Utils/Blit.shader")]
        public Shader blitPS;

        /// <summary>
        /// Copy Depth shader.
        /// </summary>
        [Reload("Shaders/Utils/CopyDepth.shader")]
        [Obsolete("Moved to UniversalRenderPipelineRuntimeShaders on GraphicsSettings. #from(2023.3)", false)]
        public Shader copyDepthPS;

        /// <summary>
        /// Screen Space Shadows shader.
        /// </summary>
        [Obsolete("Obsolete, this feature will be supported by new 'ScreenSpaceShadows' renderer feature", true)]
        public Shader screenSpaceShadowPS = null;

        /// <summary>
        /// Sampling shader.
        /// </summary>
        [Obsolete("Moved to UniversalRenderPipelineRuntimeShaders on GraphicsSettings. #from(2023.3)", false)]
        [Reload("Shaders/Utils/Sampling.shader")]
        public Shader samplingPS;

        /// <summary>
        /// Stencil Deferred shader.
        /// </summary>
        [Reload("Shaders/Utils/StencilDeferred.shader")]
        [Obsolete("Moved to UniversalRenderPipelineRuntimeShaders on GraphicsSettings. #from(2023.3)", false)]
        public Shader stencilDeferredPS;

        /// <summary>
        /// Fallback error shader.
        /// </summary>
        [Reload("Shaders/Utils/FallbackError.shader")]
        [Obsolete("Moved to UniversalRenderPipelineRuntimeShaders on GraphicsSettings. #from(2023.3)", false)]
        public Shader fallbackErrorPS;

        /// <summary>
        /// Fallback loading shader.
        /// </summary>
        [Reload("Shaders/Utils/FallbackLoading.shader")]
        [Obsolete("Moved to UniversalRenderPipelineRuntimeShaders on GraphicsSettings. #from(2023.3)", false)]
        public Shader fallbackLoadingPS;

        /// <summary>
        /// Material Error shader.
        /// </summary>
        [Obsolete("Use fallbackErrorPS instead", true)]
        public Shader materialErrorPS = null;

        // Core blitter shaders, adapted from HDRP
        // TODO: move to core and share with HDRP
        [Reload("Shaders/Utils/CoreBlit.shader"), SerializeField]
        [Obsolete("Moved to UniversalRenderPipelineRuntimeShaders on GraphicsSettings. #from(2023.3)", false)]
        internal Shader coreBlitPS;

        [Reload("Shaders/Utils/CoreBlitColorAndDepth.shader"), SerializeField]
        [Obsolete("Moved to UniversalRenderPipelineRuntimeShaders on GraphicsSettings. #from(2023.3)", false)]
        internal Shader coreBlitColorAndDepthPS;

        /// <summary>
        /// Blit shader that blits UI Overlay and performs HDR encoding.
        /// </summary>
        [Reload("Shaders/Utils/BlitHDROverlay.shader"), SerializeField]
        [Obsolete("Moved to UniversalRenderPipelineRuntimeShaders on GraphicsSettings. #from(2023.3)", false)]
        internal Shader blitHDROverlay;

        /// <summary>
        /// Camera Motion Vectors shader.
        /// </summary>
        [Reload("Shaders/CameraMotionVectors.shader")]
        [Obsolete("Moved to UniversalRenderPipelineRuntimeShaders on GraphicsSettings. #from(2023.3)", false)]
        public Shader cameraMotionVector;

        /// <summary>
        /// Screen Space Lens Flare shader.
        /// </summary>
        [Reload("Shaders/PostProcessing/LensFlareScreenSpace.shader")]
        [Obsolete("Moved to UniversalRenderPipelineRuntimeShaders on GraphicsSettings. #from(2023.3)", false)]
        public Shader screenSpaceLensFlare;

        /// <summary>
        /// Data Driven Lens Flare shader.
        /// </summary>
        [Reload("Shaders/PostProcessing/LensFlareDataDriven.shader")]
        [Obsolete("Moved to UniversalRenderPipelineRuntimeShaders on GraphicsSettings. #from(2023.3)", false)]
        public Shader dataDrivenLensFlare;
    }
  
    partial class UniversalRenderPipelineGlobalSettings
    {
#pragma warning disable 0414
        [SerializeField, Obsolete("Keep for migration. #from(23.2)")] internal ShaderStrippingSetting m_ShaderStrippingSetting = new();
        [SerializeField, Obsolete("Keep for migration. #from(23.2)")] internal URPShaderStrippingSetting m_URPShaderStrippingSetting = new();
        [SerializeField, Obsolete("Keep for migration. #from(23.2)")] internal Rendering.ShaderVariantLogLevel m_ShaderVariantLogLevel = Rendering.ShaderVariantLogLevel.Disabled;
        [SerializeField, Obsolete("Keep for migration. #from(23.2)")] internal bool m_ExportShaderVariants = true;
        [SerializeField, Obsolete("Keep for migration. #from(23.2)")] internal bool m_StripDebugVariants = true;
        [SerializeField, Obsolete("Keep for migration. #from(23.2)")] internal bool m_StripUnusedPostProcessingVariants = false;
        [SerializeField, Obsolete("Keep for migration. #from(23.2)")] internal bool m_StripUnusedVariants = true;
        [SerializeField, Obsolete("Keep for migration. #from(23.2)")] internal bool m_StripScreenCoordOverrideVariants = true;
#pragma warning restore 0414

        /// <summary>
        /// If this property is true, Unity strips the LOD variants if the LOD cross-fade feature (UniversalRenderingPipelineAsset.enableLODCrossFade) is disabled.
        /// </summary>
        [Obsolete("No longer used as Shader Prefiltering automatically strips out unused LOD Crossfade variants. Please use the LOD Crossfade setting in the URP Asset to disable the feature if not used. #from(2023.1)", false)]
        public bool stripUnusedLODCrossFadeVariants { get => false; set { } }

        /// <summary>
        /// Controls whether debug display shaders for Rendering Debugger are available in Player builds.
        /// </summary>
        [Obsolete("Please use stripRuntimeDebugShaders instead. #from(23.1)", false)]
        public bool supportRuntimeDebugDisplay = false;

        [SerializeField, Obsolete("Keep for migration. #from(23.2)")] internal bool m_EnableRenderGraph;
    }
}
