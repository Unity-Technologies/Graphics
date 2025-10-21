using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using Unity.Collections;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// Input requirements for <c>ScriptableRenderPass</c>.
    /// 
    /// URP adds render passes to generate the inputs, or reuses inputs that are already available from earlier in the frame.
    /// 
    /// URP binds the inputs as global shader texture properties.
    /// </summary>
    /// <seealso cref="ConfigureInput"/>
    [Flags]
    public enum ScriptableRenderPassInput
    {
        /// <summary>
        /// Used when a <c>ScriptableRenderPass</c> does not require any texture.
        /// </summary>
        None = 0,

        /// <summary>
        /// Used when a <c>ScriptableRenderPass</c> requires a depth texture.
        /// 
        /// To sample the depth texture in a shader, include `Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl`, then use the `SampleSceneDepth` method.
        /// </summary>
        Depth = 1 << 0,

        /// <summary>
        /// Used when a <c>ScriptableRenderPass</c> requires a normal texture.
        /// 
        /// To sample the normals texture in a shader, include `Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl`, then use the `SampleSceneNormals` method.
        /// </summary>
        Normal = 1 << 1,

        /// <summary>
        /// Used when a <c>ScriptableRenderPass</c> requires a color texture.
        /// 
        /// To sample the color texture in a shader, include `Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl`, then use the `SampleSceneColor` method. 
        /// 
        /// **Note:** The opaque texture might be a downscaled copy of the framebuffer from before rendering transparent objects.
        /// </summary>
        Color = 1 << 2,

        /// <summary>
        /// Used when a <c>ScriptableRenderPass</c> requires a motion vectors texture.
        /// 
        /// To sample the motion vectors texture in a shader, use `TEXTURE2D_X(_MotionVectorTexture)`, then `LOAD_TEXTURE2D_X_LOD(_MotionVectorTexture, pixelCoords, 0).xy`.
        /// </summary>
        Motion = 1 << 3,
    }

    // Note: Spaced built-in events so we can add events in between them
    // We need to leave room as we sort render passes based on event.
    // Users can also inject render pass events in a specific point by doing RenderPassEvent + offset

    /// <summary>
    /// Controls when the render pass executes.
    /// </summary>
    public enum RenderPassEvent
    {
        /// <summary>
        /// Executes a <c>ScriptableRenderPass</c> before rendering any other passes in the pipeline.
        /// Camera matrices and stereo rendering are not setup this point.
        /// You can use this to draw to custom input textures used later in the pipeline, f.ex LUT textures.
        /// </summary>
        BeforeRendering = 0,

        /// <summary>
        /// Executes a <c>ScriptableRenderPass</c> before rendering shadowmaps.
        /// Camera matrices and stereo rendering are not setup this point.
        /// </summary>
        BeforeRenderingShadows = 50,

        /// <summary>
        /// Executes a <c>ScriptableRenderPass</c> after rendering shadowmaps.
        /// Camera matrices and stereo rendering are not setup this point.
        /// </summary>
        AfterRenderingShadows = 100,

        /// <summary>
        /// Executes a <c>ScriptableRenderPass</c> before rendering prepasses, f.ex, depth prepass.
        /// Camera matrices and stereo rendering are already setup at this point.
        /// </summary>
        BeforeRenderingPrePasses = 150,

        /// <summary>
        /// Executes a <c>ScriptableRenderPass</c> after rendering prepasses, f.ex, depth prepass.
        /// Camera matrices and stereo rendering are already setup at this point.
        /// </summary>
        AfterRenderingPrePasses = 200,

        /// <summary>
        /// Executes a <c>ScriptableRenderPass</c> before rendering gbuffer pass.
        /// </summary>
        BeforeRenderingGbuffer = 210,

        /// <summary>
        /// Executes a <c>ScriptableRenderPass</c> after rendering gbuffer pass.
        /// </summary>
        AfterRenderingGbuffer = 220,

        /// <summary>
        /// Executes a <c>ScriptableRenderPass</c> before rendering deferred shading pass.
        /// </summary>
        BeforeRenderingDeferredLights = 230,

        /// <summary>
        /// Executes a <c>ScriptableRenderPass</c> after rendering deferred shading pass.
        /// </summary>
        AfterRenderingDeferredLights = 240,

        /// <summary>
        /// Executes a <c>ScriptableRenderPass</c> before rendering opaque objects.
        /// </summary>
        BeforeRenderingOpaques = 250,

        /// <summary>
        /// Executes a <c>ScriptableRenderPass</c> after rendering opaque objects.
        /// </summary>
        AfterRenderingOpaques = 300,

        /// <summary>
        /// Executes a <c>ScriptableRenderPass</c> before rendering the sky.
        /// </summary>
        BeforeRenderingSkybox = 350,

        /// <summary>
        /// Executes a <c>ScriptableRenderPass</c> after rendering the sky.
        /// </summary>
        AfterRenderingSkybox = 400,

        /// <summary>
        /// Executes a <c>ScriptableRenderPass</c> before rendering transparent objects.
        /// </summary>
        BeforeRenderingTransparents = 450,

        /// <summary>
        /// Executes a <c>ScriptableRenderPass</c> after rendering transparent objects.
        /// </summary>
        AfterRenderingTransparents = 500,

        /// <summary>
        /// Executes a <c>ScriptableRenderPass</c> before rendering post-processing effects.
        /// </summary>
        BeforeRenderingPostProcessing = 550,

        /// <summary>
        /// Executes a <c>ScriptableRenderPass</c> after rendering post-processing effects but before final blit, post-processing AA effects and color grading.
        /// </summary>
        AfterRenderingPostProcessing = 600,

        /// <summary>
        /// Executes a <c>ScriptableRenderPass</c> after rendering all effects.
        /// </summary>
        AfterRendering = 1000,
    }

    /// <summary>
    /// Framebuffer fetch events in Universal RP
    /// </summary>
    internal enum FramebufferFetchEvent
    {
        None = 0,
        FetchGbufferInDeferred = 1
    }

    internal static class RenderPassEventsEnumValues
    {
        // we cache the values in this array at construction time to avoid runtime allocations, which we would cause if we accessed valuesInternal directly
        public static int[] values;

        static RenderPassEventsEnumValues()
        {
            System.Array valuesInternal = Enum.GetValues(typeof(RenderPassEvent));

            values = new int[valuesInternal.Length];

            int index = 0;
            foreach (int value in valuesInternal)
            {
                values[index] = value;
                index++;
            }
        }
    }

    /// <summary>
    /// <c>ScriptableRenderPass</c> implements a logical rendering pass that can be used to extend Universal RP renderer.
    /// </summary>
    /// <remarks>
    /// To implement your own rendering pass you need to take the following steps:
    /// 1. Create a new Subclass from ScriptableRenderPass that implements the rendering logic.
    /// 2. Create an instance of your subclass and set up the relevant parameters such as <c>ScriptableRenderPass.renderPassEvent</c> in the constructor or initialization code.
    /// 3. Ensure your pass instance gets picked up by URP, this can be done through a <c>ScriptableRendererFeature</c> or by calling <c>ScriptableRenderer.EnqueuePass</c> from an event callback like <c>RenderPipelineManager.beginCameraRendering</c>
    ///
    /// See [link] for more info on working with a <c>ScriptableRendererFeature</c> or [link] for more info on working with <c>ScriptableRenderer.EnqueuePass</c>.
    /// </remarks>
    public abstract partial class ScriptableRenderPass : IRenderGraphRecorder
    {
        /// <summary>
        /// The event when the render pass executes.
        /// </summary>
        public RenderPassEvent renderPassEvent { get; set; }

        /// <summary>
        /// The input requirements for the <c>ScriptableRenderPass</c>, which has been set using <c>ConfigureInput</c>
        /// </summary>
        /// <seealso cref="ConfigureInput"/>
        public ScriptableRenderPassInput input => m_Input;

        /// <summary>
        /// Setting this property to true forces rendering of all passes in the URP frame via an intermediate texture. Use this option for passes that do not support rendering directly to the backbuffer or that require sampling the active color target. Using this option might have a significant performance impact on untethered VR platforms.
        /// </summary>
        public bool requiresIntermediateTexture { get; set; }

        private ProfilingSampler m_ProfingSampler;
        private string m_PassName;
        
        /// <summary>
        /// A ProfilingSampler for the entire render pass. Used as a profiling name by <c>ScriptableRenderer</c> when executing the pass.
        /// The default is named as the class type of the sub-class.
        /// Set <c>base.profilingSampler</c> from the sub-class constructor to set a different profiling name for a custom <c>ScriptableRenderPass
        /// This returns null in release build (non-development).</c>.
        /// </summary>
        protected internal ProfilingSampler profilingSampler
        {
            get
            {

#if (DEVELOPMENT_BUILD || UNITY_EDITOR)
                return m_ProfingSampler;
#else
                return null;
#endif
            }
            set
            {
                m_ProfingSampler = value;
                m_PassName = (value != null) ? value.name : this.GetType().Name;                
            }
        }

        /// <summary>
        /// The name of the pass that will show up in profiler and other tools. This will be indentical to the 
        /// name of <c>profilingSampler</c>. <c>profilingSampler</c> is set to null in the release build (non-development)
        /// so this <c>passName</c> property is the safe way to access the name and use it consistently. This will always return a valid string.
        /// </summary>
        protected internal string passName{ get { return m_PassName; } }

        internal bool isBlitRenderPass { get; set; }
        
        // index to track the position in the current frame
        internal int renderPassQueueIndex { get; set; }

        internal NativeArray<int> m_ColorAttachmentIndices;
        internal NativeArray<int> m_InputAttachmentIndices;

        internal GraphicsFormat[] renderTargetFormat { get; set; }

        ScriptableRenderPassInput m_Input = ScriptableRenderPassInput.None;

        static internal DebugHandler GetActiveDebugHandler(UniversalCameraData cameraData)
        {
            var debugHandler = cameraData.renderer.DebugHandler;
            if ((debugHandler != null) && debugHandler.IsActiveForCamera(cameraData.isPreviewCamera))
                return debugHandler;
            return null;
        }

        /// <summary>
        /// Creates a new <c>ScriptableRenderPass"</c> instance.
        /// </summary>
        public ScriptableRenderPass()            
        {
            renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
            profilingSampler = new ProfilingSampler(this.GetType().Name);
        }

        /// <summary>
        /// Configures Input Requirements for this render pass.
        /// This method should be called inside <c>ScriptableRendererFeature.AddRenderPasses</c>.
        /// </summary>
        /// <param name="passInput">ScriptableRenderPassInput containing information about what requirements the pass needs.</param>
        /// <seealso cref="ScriptableRendererFeature.AddRenderPasses"/>
        public void ConfigureInput(ScriptableRenderPassInput passInput)
        {
            m_Input = passInput;
        }

        /// <summary>
        /// Called upon finish rendering a camera. You can use this callback to release any resources created
        /// by this render
        /// pass that need to be cleanup once camera has finished rendering.
        /// This method should be called for all cameras in a camera stack.
        /// </summary>
        /// <param name="cmd">Use this CommandBuffer to cleanup any generated data</param>
        public virtual void OnCameraCleanup(CommandBuffer cmd)
        {
        }

        /// <inheritdoc cref="IRenderGraphRecorder.RecordRenderGraph"/>
        public virtual void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            Debug.LogWarning("The render pass " + this.ToString() + " does not have an implementation of the RecordRenderGraph method. Please implement this method, or consider turning on Compatibility Mode (RenderGraph disabled) in the menu Edit > Project Settings > Graphics > URP. Otherwise the render pass will have no effect. For more information, refer to https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@latest/index.html?subfolder=/manual/customizing-urp.html.");
        }
        
        /// <summary>
        /// Creates <c>DrawingSettings</c> based on current the rendering state.
        /// </summary>
        /// <param name="shaderTagId">Shader pass tag to render.</param>
        /// <param name="renderingData">Current rendering state.</param>
        /// <param name="sortingCriteria">Criteria to sort objects being rendered.</param>
        /// <returns>Returns the draw settings created.</returns>
        /// <seealso cref="DrawingSettings"/>
        public DrawingSettings CreateDrawingSettings(ShaderTagId shaderTagId, ref RenderingData renderingData, SortingCriteria sortingCriteria)
        {
            ContextContainer frameData = renderingData.frameData;
            UniversalRenderingData universalRenderingData = frameData.Get<UniversalRenderingData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            UniversalLightData lightData = frameData.Get<UniversalLightData>();

            return RenderingUtils.CreateDrawingSettings(shaderTagId, universalRenderingData, cameraData, lightData, sortingCriteria);
        }

        /// <summary>
        /// Creates <c>DrawingSettings</c> based on current the rendering state.
        /// </summary>
        /// <param name="shaderTagId">Shader pass tag to render.</param>
        /// <param name="renderingData">Current rendering state.</param>
        /// <param name="cameraData">Current camera state.</param>
        /// <param name="lightData">Current light state.</param>
        /// <param name="sortingCriteria">Criteria to sort objects being rendered.</param>
        /// <returns>Returns the draw settings created.</returns>
        /// <seealso cref="DrawingSettings"/>
        public DrawingSettings CreateDrawingSettings(ShaderTagId shaderTagId, UniversalRenderingData renderingData,
            UniversalCameraData cameraData, UniversalLightData lightData, SortingCriteria sortingCriteria)
        {
            return RenderingUtils.CreateDrawingSettings(shaderTagId, renderingData, cameraData, lightData, sortingCriteria);
        }

        /// <summary>
        /// Creates <c>DrawingSettings</c> based on current rendering state.
        /// </summary>
        /// <param name="shaderTagIdList">List of shader pass tag to render.</param>
        /// <param name="renderingData">Current rendering state.</param>
        /// <param name="sortingCriteria">Criteria to sort objects being rendered.</param>
        /// <returns>Returns the draw settings created.</returns>
        /// <seealso cref="DrawingSettings"/>
        public DrawingSettings CreateDrawingSettings(List<ShaderTagId> shaderTagIdList,
            ref RenderingData renderingData, SortingCriteria sortingCriteria)
        {
            ContextContainer frameData = renderingData.frameData;
            UniversalRenderingData universalRenderingData = frameData.Get<UniversalRenderingData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            UniversalLightData lightData = frameData.Get<UniversalLightData>();

            return RenderingUtils.CreateDrawingSettings(shaderTagIdList, universalRenderingData, cameraData, lightData, sortingCriteria);
        }

        /// <summary>
        /// Creates <c>DrawingSettings</c> based on current rendering state.
        /// </summary>
        /// <param name="shaderTagIdList">List of shader pass tag to render.</param>
        /// <param name="renderingData">Current rendering state.</param>
        /// <param name="cameraData">Current camera state.</param>
        /// <param name="lightData">Current light state.</param>
        /// <param name="sortingCriteria">Criteria to sort objects being rendered.</param>
        /// <returns>Returns the draw settings created.</returns>
        /// <seealso cref="DrawingSettings"/>
        public DrawingSettings CreateDrawingSettings(List<ShaderTagId> shaderTagIdList,
            UniversalRenderingData renderingData, UniversalCameraData cameraData,
            UniversalLightData lightData, SortingCriteria sortingCriteria)
        {
            return RenderingUtils.CreateDrawingSettings(shaderTagIdList, renderingData, cameraData, lightData, sortingCriteria);
        }

        /// <summary>
        /// Compares two instances of <c>ScriptableRenderPass</c> by their <c>RenderPassEvent</c> and returns if <paramref name="lhs"/> is executed before <paramref name="rhs"/>.
        /// </summary>
        /// <param name="lhs"></param>
        /// <param name="rhs"></param>
        /// <returns></returns>
        public static bool operator <(ScriptableRenderPass lhs, ScriptableRenderPass rhs)
        {
            return lhs.renderPassEvent < rhs.renderPassEvent;
        }

        /// <summary>
        /// Compares two instances of <c>ScriptableRenderPass</c> by their <c>RenderPassEvent</c> and returns if <paramref name="lhs"/> is executed after <paramref name="rhs"/>.
        /// </summary>
        /// <param name="lhs"></param>
        /// <param name="rhs"></param>
        /// <returns></returns>
        public static bool operator >(ScriptableRenderPass lhs, ScriptableRenderPass rhs)
        {
            return lhs.renderPassEvent > rhs.renderPassEvent;
        }

        internal static int GetRenderPassEventRange(RenderPassEvent renderPassEvent)
        {
            int numEvents = RenderPassEventsEnumValues.values.Length;
            int currentIndex = 0;

            // find the index of the renderPassEvent in the values array
            for(int i = 0; i < numEvents; ++i)
            {
                if (RenderPassEventsEnumValues.values[currentIndex] == (int)renderPassEvent)
                    break;

                currentIndex++;
            }

            if (currentIndex >= numEvents)
            {
                Debug.LogError("GetRenderPassEventRange: invalid renderPassEvent value cannot be found in the RenderPassEvent enumeration");
                return 0;
            }

            if (currentIndex + 1 >= numEvents)
                return 50; // if this was the last event in the enum, then add 50 as the range

            int nextValue = RenderPassEventsEnumValues.values[currentIndex + 1];

            return nextValue - (int) renderPassEvent;
        }
    }
}
