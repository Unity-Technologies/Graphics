using System.Diagnostics;
using Unity.Scripting.LifecycleManagement;
using CoreBridge = Unity.RenderPipelines.Core.Runtime.Shared.ConstantBufferBackedPropertiesBridge;

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// Warns in the Editor when a loose-uniform API writes a shader property that URP stores in a constant buffer it binds
    /// itself, or will bind itself in a future version. The engine skips a bound constant buffer when it gathers loose
    /// uniforms, so once URP binds the constant buffer, a Shader.SetGlobal*, CommandBuffer.SetGlobal*, Material.Set* or
    /// MaterialPropertyBlock.Set* write to one of these properties never reaches URP shaders.
    /// </summary>
    /// <remarks>
    /// The engine does the checks, see ConstantBufferBackedPropertyWarnings.h. URP marks the properties and enables the
    /// checks of the writes other than Material.Set* only while it renders: loose uniforms set before or after URP
    /// rendering can be meant for other shaders.
    /// </remarks>
    internal static class ConstantBufferPropertyWarnings
    {
        // URP binds these itself whenever RenderingUtils.usePersistentConstantBuffer is true.
        internal readonly struct LightingConstantBuffer
        {
            public readonly string name;
            public readonly string[] members;

            public LightingConstantBuffer(string name, params string[] members)
            {
                this.name = name;
                this.members = members;
            }
        }

        [NoAutoStaticsCleanup]
        internal static readonly LightingConstantBuffer[] k_LightingConstantBuffers =
        {
            new LightingConstantBuffer("AdditionalLights",
                "_AdditionalLightsPosition", "_AdditionalLightsColor", "_AdditionalLightsAttenuation",
                "_AdditionalLightsSpotDir", "_AdditionalLightsOcclusionProbes"),
            new LightingConstantBuffer("AdditionalLightShadows",
                "_AdditionalLightsWorldToShadow", "_AdditionalShadowParams", "_AdditionalShadowReceiverNormalBias"),
            new LightingConstantBuffer("AdditionalLightPCSS",
                "_AdditionalLightsPCSSParams", "_AdditionalLightsPCSSZParams", "_AdditionalLightsPCSSAtlas"),
            new LightingConstantBuffer("urp_ReflectionProbeBuffer",
                "urp_ReflProbes_BoxMax", "urp_ReflProbes_BoxMin", "urp_ReflProbes_ProbePosition",
                "urp_ReflProbes_MipScaleOffset", "urp_ReflProbes_Rotation", "urp_ReflProbes_ExposureMultipliers"),
            new LightingConstantBuffer("LightCookies",
                "_AdditionalLightsWorldToLights", "_AdditionalLightsCookieAtlasUVRects", "_AdditionalLightsLightTypes",
                "_AdditionalLightsCookieEnableBits"),
        };

        internal const string k_GlobalWarningBoundLater =
            "Shader property '{0}' is a variable of URP's global constant buffer (GlobalShaderVariables), which URP will bind " +
            "itself in a future version. From then on, setting it with Shader.SetGlobal*, CommandBuffer.SetGlobal*, " +
            "Material.Set* or MaterialPropertyBlock.Set* won't change the value that URP shaders read. Rename the property if " +
            "it belongs to your own shader. This warning is logged once per property.";

        internal const string k_GlobalWarningBound =
            "Shader property '{0}' is a variable of URP's global constant buffer (GlobalShaderVariables), which URP binds " +
            "itself while Use Global Constant Buffer is on (Project Settings > Graphics > URP > Rendering Capabilities). " +
            "Setting it with Shader.SetGlobal*, CommandBuffer.SetGlobal*, Material.Set* or MaterialPropertyBlock.Set* doesn't " +
            "change the value that URP shaders read. Rename the property if it belongs to your own shader. This warning is " +
            "logged once per property.";

        internal static string LightingConstantBufferWarning(string constantBuffer)
        {
            return "Shader property '{0}' is a variable of URP's " + constantBuffer + " constant buffer, which URP binds " +
                "itself. Setting it with Shader.SetGlobal*, CommandBuffer.SetGlobal*, Material.Set* or " +
                "MaterialPropertyBlock.Set* doesn't change the value that URP shaders read. Rename the property if it " +
                "belongs to your own shader. This warning is logged once per property.";
        }

        // The members of GlobalShaderVariables but the padding, which ConstantBufferPropertyWarningsTests checks against the HLSL.
        [NoAutoStaticsCleanup]
        internal static readonly string[] k_GlobalShaderVariables =
        {
            "_Time", "_SinTime", "_CosTime", "unity_DeltaTime", "_TimeParameters", "_LastTimeParameters", "_ScreenParams",
            "_ZBufferParams", "_RTHandleScale", "_ScreenSize", "_ScreenSizeOverride", "_ScreenCoordScaleBias",
            "unity_AmbientSky", "unity_AmbientEquator", "unity_AmbientGround", "_GlossyEnvironmentColor", "unity_OrthoParams",
            "_ScaledScreenParams", "_GlobalMipBias", "_DitheringTextureInvSize", "_SubtractiveShadowColor",
            "_GlossyEnvironmentCubeMap_HDR",

            "_MainLightPosition", "_MainLightColor", "_MainLightOcclusionProbes", "_MainLightLayerMask",
            "_RenderingLayerMaxInt", "_EnableProbeVolumes", "_MainLightCookieTextureFormat", "_FPParams0", "_FPParams1",
            "_FPParams2", "_AdditionalLightsCount", "_CascadeShadowSplitSpheres0", "_CascadeShadowSplitSpheres1",
            "_CascadeShadowSplitSpheres2", "_CascadeShadowSplitSpheres3", "_CascadeShadowSplitSphereRadii",
            "_MainLightShadowOffset0", "_MainLightShadowOffset1", "_MainLightShadowmapSize", "_MainLightPCSSParams0",
            "_MainLightPCSSParams1", "_MainLightPCSSAtlas", "_MainLightPCSSScalars", "_AdditionalShadowOffset0",
            "_AdditionalShadowOffset1", "_AdditionalShadowmapSize", "_MainLightWorldToShadow", "_MainLightWorldToLight",
            "_NonJitteredViewProjMatrix", "_PrevViewProjMatrix", "_AdditionalLightsCookieAtlasTextureFormat",
        };

        // The mode the properties are marked for: -1 when none is, else 1 when URP binds its global constant buffer, 0 otherwise.
        [NoAutoStaticsCleanup] static int s_RegisteredMode = -1;
        // Render() and render requests, which can happen while URP renders.
        [NoAutoStaticsCleanup] static int s_RenderDepth;
        [NoAutoStaticsCleanup] static int s_PipelineWriteDepth;

        /// <summary>
        /// Call when URP starts rendering, in UniversalRenderPipeline.Render() or for a render request, paired with
        /// <see cref="EndRender"/>.
        /// </summary>
        /// <remarks>
        /// Reads the Use Global Constant Buffer setting, like RenderCameras() does later in the frame.
        /// UniversalRenderPipeline.useGlobalConstantBuffer still holds the mode of the previous frame here, so on the frame
        /// that changes the mode, the warnings would describe the other one.
        /// </remarks>
        [Conditional("UNITY_EDITOR")]
        internal static void BeginRender()
        {
            var settings = GraphicsSettings.GetRenderPipelineSettings<RenderingCapabilitiesSettings>();
            BeginRender(settings != null && settings.useGlobalConstantBuffer);
        }

        /// <summary>
        /// <see cref="BeginRender()"/> for a given mode.
        /// </summary>
        /// <param name="useGlobalConstantBuffer">Whether URP binds its global constant buffer itself this frame.</param>
        [Conditional("UNITY_EDITOR")]
        internal static void BeginRender(bool useGlobalConstantBuffer)
        {
            if (!AreValidityChecksEnabled())
            {
                Release();
                return;
            }

            // A render request made while URP renders keeps the warnings of that rendering.
            if (s_RenderDepth > 0)
            {
                ++s_RenderDepth;
                return;
            }

            if (s_RegisteredMode != (useGlobalConstantBuffer ? 1 : 0))
                Register(useGlobalConstantBuffer);

            // A pipeline write section that an exception left open doesn't silence the next frames.
            s_PipelineWriteDepth = 0;
            CoreBridge.EnableWarnings(true);
            s_RenderDepth = 1;
        }

        /// <summary>
        /// Call when URP stops rendering, even when rendering threw.
        /// </summary>
        [Conditional("UNITY_EDITOR")]
        internal static void EndRender()
        {
            if (s_RenderDepth > 0 && --s_RenderDepth == 0)
                CoreBridge.EnableWarnings(false);
        }

        /// <summary>
        /// Call before URP sets the variables of its global constant buffer as loose uniforms, which the engine copies to
        /// that constant buffer while URP doesn't bind it, paired with <see cref="EndPipelineWrites"/>. These writes aren't
        /// the ones to warn about.
        /// </summary>
        [Conditional("UNITY_EDITOR")]
        internal static void BeginPipelineWrites()
        {
            if (s_PipelineWriteDepth++ == 0 && s_RenderDepth > 0)
                CoreBridge.EnableWarnings(false);
        }

        [Conditional("UNITY_EDITOR")]
        internal static void EndPipelineWrites()
        {
            if (s_PipelineWriteDepth > 0 && --s_PipelineWriteDepth == 0 && s_RenderDepth > 0)
                CoreBridge.EnableWarnings(true);
        }

        /// <summary>
        /// Unmarks every property, so none stays marked for another render pipeline. Call when the pipeline is disposed.
        /// </summary>
        [Conditional("UNITY_EDITOR")]
        internal static void Release()
        {
            CoreBridge.ClearWarnings();
            CoreBridge.EnableWarnings(false);
            s_RegisteredMode = -1;
            s_RenderDepth = 0;
            s_PipelineWriteDepth = 0;
        }

        static void Register(bool globalsBoundByPipeline)
        {
            // Clear first, so no mark of the previous mode stays.
            CoreBridge.ClearWarnings();

            string globalWarning = globalsBoundByPipeline ? k_GlobalWarningBound : k_GlobalWarningBoundLater;
            foreach (string name in k_GlobalShaderVariables)
                MarkProperty(name, globalWarning);

            if (RenderingUtils.usePersistentConstantBuffer)
            {
                foreach (LightingConstantBuffer constantBuffer in k_LightingConstantBuffers)
                {
                    string warning = LightingConstantBufferWarning(constantBuffer.name);
                    foreach (string member in constantBuffer.members)
                        MarkProperty(member, warning);
                }
            }

            s_RegisteredMode = globalsBoundByPipeline ? 1 : 0;
        }

        internal static void MarkProperty(string propertyName, string warning)
        {
            CoreBridge.SetBackedByConstantBuffer(Shader.PropertyToID(propertyName), warning.Replace("{0}", propertyName));
        }

        // Without Render Graph settings, as in some unit tests, the checks stay on like Render Graph's.
        static bool AreValidityChecksEnabled()
        {
            return !GraphicsSettings.TryGetRenderPipelineSettings<RenderGraphGlobalSettings>(out var renderGraphSettings)
                || renderGraphSettings.enableValidityChecks;
        }
    }
}
