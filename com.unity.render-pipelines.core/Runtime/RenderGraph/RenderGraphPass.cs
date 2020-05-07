using System;
using System.Diagnostics;
using System.Collections.Generic;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.RenderGraphModule
{
    [DebuggerDisplay("RenderPass: {name} ({index})")]
    abstract class RenderGraphPass
    {
        public RenderFunc<PassData> GetExecuteDelegate<PassData>()
            where PassData : class, new() => ((RenderGraphPass<PassData>)this).renderFunc;

        public abstract void Execute(RenderGraphContext renderGraphContext);
        public abstract void Release(RenderGraphContext renderGraphContext);
        public abstract bool HasRenderFunc();

        public string           name { get; protected set; }
        public int              index { get; protected set; }
        public ProfilingSampler customSampler { get; protected set; }
        public bool             enableAsyncCompute { get; protected set; }

        public TextureHandle    depthBuffer { get; protected set; }
        public TextureHandle[]  colorBuffers { get; protected set; } = new TextureHandle[RenderGraph.kMaxMRTCount];
        public int              colorBufferMaxIndex { get; protected set; } = -1;
        public int              refCount { get; protected set; }

        List<TextureHandle>         m_TextureReadList = new List<TextureHandle>();
        List<TextureHandle>         m_TextureWriteList = new List<TextureHandle>();
        List<TextureHandle>         m_TransientTextureList = new List<TextureHandle>();
        List<ComputeBufferHandle>   m_BufferReadList = new List<ComputeBufferHandle>();
        List<ComputeBufferHandle>   m_BufferWriteList = new List<ComputeBufferHandle>();
        List<RendererListHandle>    m_UsedRendererListList = new List<RendererListHandle>();

        public IReadOnlyCollection<TextureHandle>       textureReadList { get { return m_TextureReadList; } }
        public IReadOnlyCollection<TextureHandle>       textureWriteList { get { return m_TextureWriteList; } }
        public IReadOnlyCollection<TextureHandle>       transientTextureList { get { return m_TransientTextureList; } }
        public IReadOnlyCollection<ComputeBufferHandle> bufferReadList { get { return m_BufferReadList; } }
        public IReadOnlyCollection<ComputeBufferHandle> bufferWriteList { get { return m_BufferWriteList; } }
        public IReadOnlyCollection<RendererListHandle> usedRendererListList { get { return m_UsedRendererListList; } }

        public void Clear()
        {
            name = "";
            index = -1;
            customSampler = null;
            m_TextureReadList.Clear();
            m_TextureWriteList.Clear();
            m_BufferReadList.Clear();
            m_BufferWriteList.Clear();
            m_TransientTextureList.Clear();
            m_UsedRendererListList.Clear();
            enableAsyncCompute = false;
            refCount = 0;

            // Invalidate everything
            colorBufferMaxIndex = -1;
            depthBuffer = new TextureHandle();
            for (int i = 0; i < RenderGraph.kMaxMRTCount; ++i)
            {
                colorBuffers[i] = new TextureHandle();
            }
        }

        public void AddTextureWrite(TextureHandle texture)
        {
            m_TextureWriteList.Add(texture);
            refCount++;
        }

        public void AddTextureRead(TextureHandle texture)
        {
            m_TextureReadList.Add(texture);
        }

        public void AddBufferWrite(ComputeBufferHandle buffer)
        {
            m_BufferWriteList.Add(buffer);
            refCount++;
        }

        public void AddTransientTexture(TextureHandle texture)
        {
            m_TransientTextureList.Add(texture);
        }

        public void AddBufferRead(ComputeBufferHandle buffer)
        {
            m_BufferReadList.Add(buffer);
        }

        public void UseRendererList(RendererListHandle rendererList)
        {
            m_UsedRendererListList.Add(rendererList);
        }

        public void EnableAsyncCompute(bool value)
        {
            enableAsyncCompute = value;
        }

        public void SetColorBuffer(TextureHandle resource, int index)
        {
            Debug.Assert(index < RenderGraph.kMaxMRTCount && index >= 0);
            colorBufferMaxIndex = Math.Max(colorBufferMaxIndex, index);
            colorBuffers[index] = resource;
            AddTextureWrite(resource);
        }

        public void SetDepthBuffer(TextureHandle resource, DepthAccess flags)
        {
            depthBuffer = resource;
            if ((flags | DepthAccess.Read) != 0)
                AddTextureRead(resource);
            if ((flags | DepthAccess.Write) != 0)
                AddTextureWrite(resource);
        }
    }

    [DebuggerDisplay("RenderPass: {name} ({index})")]
    internal sealed class RenderGraphPass<PassData> : RenderGraphPass
        where PassData : class, new()
    {
        internal PassData data;
        internal RenderFunc<PassData> renderFunc;

        public override void Execute(RenderGraphContext renderGraphContext)
        {
            GetExecuteDelegate<PassData>()(data, renderGraphContext);
        }

        public void Initialize(int passIndex, PassData passData, string passName, ProfilingSampler sampler)
        {
            Clear();
            index = passIndex;
            data = passData;
            name = passName;
            customSampler = sampler;
        }

        public override void Release(RenderGraphContext renderGraphContext)
        {
            Clear();
            renderGraphContext.renderGraphPool.Release(data);
            data = null;
            renderFunc = null;
            renderGraphContext.renderGraphPool.Release(this);
        }

        public override bool HasRenderFunc()
        {
            return renderFunc != null;
        }
    }
}
