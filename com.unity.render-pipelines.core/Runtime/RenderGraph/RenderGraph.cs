using System;
using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine.Profiling;

namespace UnityEngine.Experimental.Rendering.RenderGraphModule
{
    /// <summary>
    /// Sets the read and write access for the depth buffer.
    /// </summary>
    [Flags]
    public enum DepthAccess
    {
        ///<summary>Read Access.</summary>
        Read = 1 << 0,
        ///<summary>Write Access.</summary>
        Write = 1 << 1,
        ///<summary>Read and Write Access.</summary>
        ReadWrite = Read | Write,
    }

    /// <summary>
    /// This struct specifies the context given to every render pass.
    /// </summary>
    public ref struct RenderGraphContext
    {
        ///<summary>Scriptable Render Context used for rendering.</summary>
        public ScriptableRenderContext      renderContext;
        ///<summary>Command Buffer used for rendering.</summary>
        public CommandBuffer                cmd;
        ///<summary>Render Graph pooll used for temporary data.</summary>
        public RenderGraphObjectPool        renderGraphPool;
        ///<summary>Render Graph Resource Registry used for accessing resources.</summary>
        public RenderGraphResourceRegistry  resources;
    }

    /// <summary>
    /// This struct contains properties which control the execution of the Render Graph.
    /// </summary>
    public struct RenderGraphExecuteParams
    {
        ///<summary>Rendering width.</summary>
        public int         renderingWidth;
        ///<summary>Rendering height.</summary>
        public int         renderingHeight;
        ///<summary>Number of MSAA samples.</summary>
        public MSAASamples msaaSamples;
    }

    class RenderGraphDebugParams
    {
        public bool enableRenderGraph = false; // TODO: TEMP TO REMOVE
        public bool tagResourceNamesWithRG;
        public bool clearRenderTargetsAtCreation;
        public bool clearRenderTargetsAtRelease;
        public bool unbindGlobalTextures;
        public bool logFrameInformation;
        public bool logResources;

        public void RegisterDebug()
        {
            var list = new List<DebugUI.Widget>();
            list.Add(new DebugUI.BoolField { displayName = "Enable Render Graph", getter = () => enableRenderGraph, setter = value => enableRenderGraph = value });
            list.Add(new DebugUI.BoolField { displayName = "Tag Resources with RG", getter = () => tagResourceNamesWithRG, setter = value => tagResourceNamesWithRG = value });
            list.Add(new DebugUI.BoolField { displayName = "Clear Render Targets at creation", getter = () => clearRenderTargetsAtCreation, setter = value => clearRenderTargetsAtCreation = value });
            list.Add(new DebugUI.BoolField { displayName = "Clear Render Targets at release", getter = () => clearRenderTargetsAtRelease, setter = value => clearRenderTargetsAtRelease = value });
            list.Add(new DebugUI.BoolField { displayName = "Unbind Global Textures", getter = () => unbindGlobalTextures, setter = value => unbindGlobalTextures = value });
            list.Add(new DebugUI.Button { displayName = "Log Frame Information", action = () => logFrameInformation = true });
            list.Add(new DebugUI.Button { displayName = "Log Resources", action = () => logResources = true });

            var testPanel = DebugManager.instance.GetPanel("Render Graph", true);
            testPanel.children.Add(list.ToArray());
        }

        public void UnRegisterDebug()
        {
            DebugManager.instance.RemovePanel("Render Graph");
        }
    }

    /// <summary>
    /// The Render Pass rendering delegate.
    /// </summary>
    /// <typeparam name="PassData">The type of the class used to provide data to the Render Pass.</typeparam>
    /// <param name="data">Render Pass specific data.</param>
    /// <param name="renderGraphContext">Global Render Graph context.</param>
    public delegate void RenderFunc<PassData>(PassData data, RenderGraphContext renderGraphContext) where PassData : class, new();

    /// <summary>
    /// This class is the main entry point of the Render Graph system.
    /// </summary>
    public class RenderGraph
    {
        ///<summary>Maximum number of MRTs supported by Render Graph.</summary>
        public static readonly int kMaxMRTCount = 8;

        internal abstract class RenderPass
        {
            internal RenderFunc<PassData> GetExecuteDelegate<PassData>()
                where PassData : class, new() => ((RenderPass<PassData>)this).renderFunc;

            internal abstract void Execute(RenderGraphContext renderGraphContext);
            internal abstract void Release(RenderGraphContext renderGraphContext);
            internal abstract bool HasRenderFunc();

            internal string                           name;
            internal int                              index;
            internal ProfilingSampler                 customSampler;
            internal List<RenderGraphResource>        resourceReadList = new List<RenderGraphResource>();
            internal List<RenderGraphMutableResource> resourceWriteList = new List<RenderGraphMutableResource>();
            internal List<RenderGraphResource>        usedRendererListList = new List<RenderGraphResource>();
            internal bool                             enableAsyncCompute;
            internal RenderGraphMutableResource       depthBuffer { get { return m_DepthBuffer; } }
            internal RenderGraphMutableResource[]     colorBuffers { get { return m_ColorBuffers; } }
            internal int                              colorBufferMaxIndex { get { return m_MaxColorBufferIndex; } }

            protected RenderGraphMutableResource[]    m_ColorBuffers = new RenderGraphMutableResource[RenderGraph.kMaxMRTCount];
            protected RenderGraphMutableResource      m_DepthBuffer;
            protected int                             m_MaxColorBufferIndex = -1;

            internal void Clear()
            {
                name = "";
                index = -1;
                customSampler = null;
                resourceReadList.Clear();
                resourceWriteList.Clear();
                usedRendererListList.Clear();
                enableAsyncCompute = false;

                // Invalidate everything
                m_MaxColorBufferIndex = -1;
                m_DepthBuffer = new RenderGraphMutableResource();
                for (int i = 0; i < RenderGraph.kMaxMRTCount; ++i)
                {
                    m_ColorBuffers[i] = new RenderGraphMutableResource();
                }
            }

            internal void SetColorBuffer(in RenderGraphMutableResource resource, int index)
            {
                Debug.Assert(index < RenderGraph.kMaxMRTCount && index >= 0);
                m_MaxColorBufferIndex = Math.Max(m_MaxColorBufferIndex, index);
                m_ColorBuffers[index] = resource;
                resourceWriteList.Add(resource);
            }

            internal void SetDepthBuffer(in RenderGraphMutableResource resource, DepthAccess flags)
            {
                m_DepthBuffer = resource;
                if ((flags | DepthAccess.Read) != 0)
                    resourceReadList.Add(resource);
                if ((flags | DepthAccess.Write) != 0)
                    resourceWriteList.Add(resource);

            }
        }

        internal sealed class RenderPass<PassData> : RenderPass
            where PassData : class, new()
        {
            internal PassData data;
            internal RenderFunc<PassData> renderFunc;

            internal override void Execute(RenderGraphContext renderGraphContext)
            {
                GetExecuteDelegate<PassData>()(data, renderGraphContext);
            }

            internal override void Release(RenderGraphContext renderGraphContext)
            {
                Clear();
                renderGraphContext.renderGraphPool.Release(data);
                data = null;
                renderFunc = null;
                renderGraphContext.renderGraphPool.Release(this);
            }

            internal override bool HasRenderFunc()
            {
                return renderFunc != null;
            }
        }

        RenderGraphResourceRegistry m_Resources;
        RenderGraphObjectPool       m_RenderGraphPool = new RenderGraphObjectPool();
        List<RenderPass>            m_RenderPasses = new List<RenderPass>();
        List<RenderGraphResource>   m_RendererLists = new List<RenderGraphResource>();
        RenderGraphDebugParams      m_DebugParameters = new RenderGraphDebugParams();
        RenderGraphLogger           m_Logger = new RenderGraphLogger();

        #region Public Interface

        /// <summary>
        /// Returns true if rendering with Render Graph is enabled.
        /// </summary>
        public bool enabled { get { return m_DebugParameters.enableRenderGraph; } }

        // TODO: Currently only needed by SSAO to sample correctly depth texture mips. Need to figure out a way to hide this behind a proper formalization.
        /// <summary>
        /// Gets the RTHandleProperties structure associated with the Render Graph's RTHandle System.
        /// </summary>
        public RTHandleProperties rtHandleProperties { get { return m_Resources.GetRTHandleProperties(); } }

        /// <summary>
        /// Render Graph constructor.
        /// </summary>
        /// <param name="supportMSAA">Specify if this Render Graph should support MSAA.</param>
        /// <param name="initialSampleCount">Specify the initial sample count of MSAA render textures.</param>
        public RenderGraph(bool supportMSAA, MSAASamples initialSampleCount)
        {
            m_Resources = new RenderGraphResourceRegistry(supportMSAA, initialSampleCount, m_DebugParameters, m_Logger);
        }

        /// <summary>
        /// Cleanup the Render Graph.
        /// </summary>
        public void Cleanup()
        {
            m_Resources.Cleanup();
        }

        /// <summary>
        /// Register this Render Graph to the debug window.
        /// </summary>
        public void RegisterDebug()
        {
            //m_DebugParameters.RegisterDebug();
        }

        /// <summary>
        /// Unregister this Render Graph from the debug window.
        /// </summary>
        public void UnRegisterDebug()
        {
            //m_DebugParameters.UnRegisterDebug();
        }

        /// <summary>
        /// Resets the reference size of the internal RTHandle System.
        /// This allows users to reduce the memory footprint of render textures after doing a super sampled rendering pass for example.
        /// </summary>
        /// <param name="width">New width of the internal RTHandle System.</param>
        /// <param name="height">New height of the internal RTHandle System.</param>
        public void ResetRTHandleReferenceSize(int width, int height)
        {
            m_Resources.ResetRTHandleReferenceSize(width, height);
        }

        /// <summary>
        /// Import an external texture to the Render Graph.
        /// </summary>
        /// <param name="rt">External RTHandle that needs to be imported.</param>
        /// <param name="shaderProperty">Optional property that allows you to specify a Shader property name to use for automatic resource binding.</param>
        /// <returns>A new RenderGraphMutableResource.</returns>
        public RenderGraphMutableResource ImportTexture(RTHandle rt, int shaderProperty = 0)
        {
            return m_Resources.ImportTexture(rt, shaderProperty);
        }

        /// <summary>
        /// Create a new Render Graph Texture resource.
        /// </summary>
        /// <param name="desc">Texture descriptor.</param>
        /// <param name="shaderProperty">Optional property that allows you to specify a Shader property name to use for automatic resource binding.</param>
        /// <returns>A new RenderGraphMutableResource.</returns>
        public RenderGraphMutableResource CreateTexture(TextureDesc desc, int shaderProperty = 0)
        {
            if (m_DebugParameters.tagResourceNamesWithRG)
                desc.name = string.Format("{0}_RenderGraph", desc.name);
            return m_Resources.CreateTexture(desc, shaderProperty);
        }

        /// <summary>
        /// Create a new Render Graph Texture resource using the descriptor from another texture.
        /// </summary>
        /// <param name="texture">Texture from which the descriptor should be used.</param>
        /// <param name="shaderProperty">Optional property that allows you to specify a Shader property name to use for automatic resource binding.</param>
        /// <returns>A new RenderGraphMutableResource.</returns>
        public RenderGraphMutableResource CreateTexture(in RenderGraphResource texture, int shaderProperty = 0)
        {
            var desc = m_Resources.GetTextureResourceDesc(texture);
            if (m_DebugParameters.tagResourceNamesWithRG)
                desc.name = string.Format("{0}_RenderGraph", desc.name);
            return m_Resources.CreateTexture(desc, shaderProperty);
        }

        /// <summary>
        /// Gets the descriptor of the specified Texture resource.
        /// </summary>
        /// <param name="texture"></param>
        /// <returns>The input texture descriptor.</returns>
        public TextureDesc GetTextureDesc(in RenderGraphResource texture)
        {
            if (texture.type != RenderGraphResourceType.Texture)
            {
                throw new ArgumentException("Trying to retrieve a TextureDesc from a resource that is not a texture.");
            }

            return m_Resources.GetTextureResourceDesc(texture);
        }

        /// <summary>
        /// Creates a new Renderer List Render Graph resource.
        /// </summary>
        /// <param name="desc">Renderer List descriptor.</param>
        /// <returns>A new RenderGraphResource.</returns>
        public RenderGraphResource CreateRendererList(in RendererListDesc desc)
        {
            return m_Resources.CreateRendererList(desc);
        }

        /// <summary>
        /// Add a new Render Pass to the current Render Graph.
        /// </summary>
        /// <typeparam name="PassData">Type of the class to use to provide data to the Render Pass.</typeparam>
        /// <param name="passName">Name of the new Render Pass (this is also be used to generate a GPU profiling marker).</param>
        /// <param name="passData">Instance of PassData that is passed to the render function and you must fill.</param>
        /// <param name="sampler">Optional profiling sampler.</param>
        /// <returns>A new instance of a RenderGraphBuilder used to setup the new Render Pass.</returns>
        public RenderGraphBuilder AddRenderPass<PassData>(string passName, out PassData passData, ProfilingSampler sampler = null) where PassData : class, new()
        {
            var renderPass = m_RenderGraphPool.Get<RenderPass<PassData>>();
            renderPass.Clear();
            renderPass.index = m_RenderPasses.Count;
            renderPass.data = m_RenderGraphPool.Get<PassData>();
            renderPass.name = passName;
            renderPass.customSampler = sampler;

            passData = renderPass.data;

            m_RenderPasses.Add(renderPass);

            return new RenderGraphBuilder(renderPass, m_Resources);
        }

        /// <summary>
        /// Execute the Render Graph in its current state.
        /// </summary>
        /// <param name="renderContext">ScriptableRenderContext used to execute Scriptable Render Pipeline.</param>
        /// <param name="cmd">Command Buffer used for Render Passes rendering.</param>
        /// <param name="parameters">Render Graph execution parameters.</param>
        public void Execute(ScriptableRenderContext renderContext, CommandBuffer cmd, in RenderGraphExecuteParams parameters)
        {
            m_Logger.Initialize();

            // Update RTHandleSystem with size for this rendering pass.
            m_Resources.SetRTHandleReferenceSize(parameters.renderingWidth, parameters.renderingHeight, parameters.msaaSamples);

            LogFrameInformation(parameters.renderingWidth, parameters.renderingHeight);

            // First pass, traversal and pruning
            for (int passIndex = 0; passIndex < m_RenderPasses.Count; ++passIndex)
            {
                var pass = m_RenderPasses[passIndex];

                // TODO: Pruning

                // Gather all renderer lists
                m_RendererLists.AddRange(pass.usedRendererListList);
            }

            // Creates all renderer lists
            m_Resources.CreateRendererLists(m_RendererLists);
            LogRendererListsCreation();

            // Second pass, execution
            RenderGraphContext rgContext = new RenderGraphContext();
            rgContext.cmd = cmd;
            rgContext.renderContext = renderContext;
            rgContext.renderGraphPool = m_RenderGraphPool;
            rgContext.resources = m_Resources;

            try
            {
                for (int passIndex = 0; passIndex < m_RenderPasses.Count; ++passIndex)
                {
                    var pass = m_RenderPasses[passIndex];

                    if (!pass.HasRenderFunc())
                    {
                        throw new InvalidOperationException(string.Format("RenderPass {0} was not provided with an execute function.", pass.name));
                    }

                    using (new ProfilingScope(cmd, pass.customSampler))
                    {
                        LogRenderPassBegin(pass);
                        using (new RenderGraphLogIndent(m_Logger))
                        {
                            PreRenderPassExecute(passIndex, pass, rgContext);
                            pass.Execute(rgContext);
                            PostRenderPassExecute(passIndex, pass, rgContext);
                        }
                    }
                }
            }
            catch(Exception e)
            {
                Debug.LogError("Render Graph Execution error");
                Debug.LogException(e);
            }
            finally
            {
                ClearRenderPasses();
                m_Resources.Clear();
                m_RendererLists.Clear();

                if (m_DebugParameters.logFrameInformation || m_DebugParameters.logResources)
                    Debug.Log(m_Logger.GetLog());

                m_DebugParameters.logFrameInformation = false;
                m_DebugParameters.logResources = false;
            }
        }
        #endregion

        #region Internal Interface
        private RenderGraph()
        {

        }

        void PreRenderPassSetRenderTargets(in RenderPass pass, RenderGraphContext rgContext)
        {
            if (pass.depthBuffer.IsValid() || pass.colorBufferMaxIndex != -1)
            {
                var mrtArray = rgContext.renderGraphPool.GetTempArray<RenderTargetIdentifier>(pass.colorBufferMaxIndex + 1);
                var colorBuffers = pass.colorBuffers;

                if (pass.colorBufferMaxIndex > 0)
                {
                    for (int i = 0; i <= pass.colorBufferMaxIndex; ++i)
                    {
                        if (!colorBuffers[i].IsValid())
                            throw new InvalidOperationException("MRT setup is invalid. Some indices are not used.");
                        mrtArray[i] = m_Resources.GetTexture(colorBuffers[i]);
                    }

                    if (pass.depthBuffer.IsValid())
                    {
                        CoreUtils.SetRenderTarget(rgContext.cmd, mrtArray, m_Resources.GetTexture(pass.depthBuffer));
                    }
                    else
                    {
                        throw new InvalidOperationException("Setting MRTs without a depth buffer is not supported.");
                    }
                }
                else
                {
                    if (pass.depthBuffer.IsValid())
                    {
                        if (pass.colorBufferMaxIndex > -1)
                            CoreUtils.SetRenderTarget(rgContext.cmd, m_Resources.GetTexture(pass.colorBuffers[0]), m_Resources.GetTexture(pass.depthBuffer));
                        else
                            CoreUtils.SetRenderTarget(rgContext.cmd, m_Resources.GetTexture(pass.depthBuffer));
                    }
                    else
                    {
                        CoreUtils.SetRenderTarget(rgContext.cmd, m_Resources.GetTexture(pass.colorBuffers[0]));
                    }

                }
            }
        }

        void PreRenderPassExecute(int passIndex, in RenderPass pass, RenderGraphContext rgContext)
        {
            // TODO merge clear and setup here if possible
            m_Resources.CreateAndClearTexturesForPass(rgContext, pass.index, pass.resourceWriteList);
            PreRenderPassSetRenderTargets(pass, rgContext);
            m_Resources.PreRenderPassSetGlobalTextures(rgContext, pass.resourceReadList);
        }

        void PostRenderPassExecute(int passIndex, in RenderPass pass, RenderGraphContext rgContext)
        {
            if (m_DebugParameters.unbindGlobalTextures)
                m_Resources.PostRenderPassUnbindGlobalTextures(rgContext, pass.resourceReadList);

            m_RenderGraphPool.ReleaseAllTempAlloc();
            m_Resources.ReleaseTexturesForPass(rgContext, pass.index, pass.resourceReadList, pass.resourceWriteList);
            pass.Release(rgContext);
        }

        void ClearRenderPasses()
        {
            m_RenderPasses.Clear();
        }

        void LogFrameInformation(int renderingWidth, int renderingHeight)
        {
            if (m_DebugParameters.logFrameInformation)
            {
                m_Logger.LogLine("==== Staring frame at resolution ({0}x{1}) ====", renderingWidth, renderingHeight);
                m_Logger.LogLine("Number of passes declared: {0}", m_RenderPasses.Count);
            }
        }

        void LogRendererListsCreation()
        {
            if (m_DebugParameters.logFrameInformation)
            {
                m_Logger.LogLine("Number of renderer lists created: {0}", m_RendererLists.Count);
            }
        }

        void LogRenderPassBegin(in RenderPass pass)
        {
            if (m_DebugParameters.logFrameInformation)
            {
                m_Logger.LogLine("Executing pass \"{0}\" (index: {1})", pass.name, pass.index);
            }
        }

        #endregion
    }
}

