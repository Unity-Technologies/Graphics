using System;
using System.Diagnostics;
using System.Collections.Generic;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.RenderGraphModule
{
    [DebuggerDisplay("RenderPass: {name} (Index:{index} Async:{enableAsyncCompute})")]
    abstract class RenderGraphPass
    {
        public RenderFunc<PassData> GetExecuteDelegate<PassData>()
            where PassData : class, new() => ((RenderGraphPass<PassData>)this).renderFunc;

        public abstract void Execute(RenderGraphContext renderGraphContext);
        public abstract void Release(RenderGraphObjectPool pool);
        public abstract bool HasRenderFunc();

        public string           name { get; protected set; }
        public int              index { get; protected set; }
        public ProfilingSampler customSampler { get; protected set; }
        public bool             enableAsyncCompute { get; protected set; }
        public bool             allowPassPruning { get; protected set; }

        public TextureHandle    depthBuffer { get; protected set; }
        public TextureHandle[]  colorBuffers { get; protected set; } = new TextureHandle[RenderGraph.kMaxMRTCount];
        public int              colorBufferMaxIndex { get; protected set; } = -1;
        public int              refCount { get; protected set; }

        public List<TextureHandle>          textureReadList = new List<TextureHandle>();
        public List<TextureHandle>          textureWriteList = new List<TextureHandle>();
        public List<TextureHandle>          transientTextureList = new List<TextureHandle>();
        public List<ComputeBufferHandle>    bufferReadList = new List<ComputeBufferHandle>();
        public List<ComputeBufferHandle>    bufferWriteList = new List<ComputeBufferHandle>();
        public List<RendererListHandle>     usedRendererListList = new List<RendererListHandle>();

        public void Clear()
        {
            name = "";
            index = -1;
            customSampler = null;
            textureReadList.Clear();
            textureWriteList.Clear();
            bufferReadList.Clear();
            bufferWriteList.Clear();
            transientTextureList.Clear();
            usedRendererListList.Clear();
            enableAsyncCompute = false;
            allowPassPruning = true;
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
            textureWriteList.Add(texture);
            refCount++;
        }

        public void AddTextureRead(TextureHandle texture)
        {
            textureReadList.Add(texture);
        }

        public void AddBufferWrite(ComputeBufferHandle buffer)
        {
            bufferWriteList.Add(buffer);
            refCount++;
        }

        public void AddTransientTexture(TextureHandle texture)
        {
            transientTextureList.Add(texture);
        }

        public void AddBufferRead(ComputeBufferHandle buffer)
        {
            bufferReadList.Add(buffer);
        }

        public void UseRendererList(RendererListHandle rendererList)
        {
            usedRendererListList.Add(rendererList);
        }

        public void EnableAsyncCompute(bool value)
        {
            enableAsyncCompute = value;
        }

        public void AllowPassPruning(bool value)
        {
            allowPassPruning = value;
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
            if ((flags & DepthAccess.Read) != 0)
                AddTextureRead(resource);
            if ((flags & DepthAccess.Write) != 0)
                AddTextureWrite(resource);
        }
    }

    [DebuggerDisplay("RenderPass: {name} (Index:{index} Async:{enableAsyncCompute})")]
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

        public override void Release(RenderGraphObjectPool pool)
        {
            Clear();
            pool.Release(data);
            data = null;
            renderFunc = null;

            // We need to do the release from here because we need the final type.
            pool.Release(this);
        }

        public override bool HasRenderFunc()
        {
            return renderFunc != null;
        }
    }
}
