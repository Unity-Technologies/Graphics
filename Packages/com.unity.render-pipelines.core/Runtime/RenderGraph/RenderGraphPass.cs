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

        public string name { get; protected set; }
        public int index { get; protected set; }
        public ProfilingSampler customSampler { get; protected set; }
        public bool enableAsyncCompute { get; protected set; }
        public bool allowPassCulling { get; protected set; }

        public TextureHandle depthBuffer { get; protected set; }
        public TextureHandle[] colorBuffers { get; protected set; } = new TextureHandle[RenderGraph.kMaxMRTCount];
        public int colorBufferMaxIndex { get; protected set; } = -1;
        public int refCount { get; protected set; }
        public bool generateDebugData { get; protected set; }

        public bool allowRendererListCulling { get; protected set; }

        public List<ResourceHandle>[] resourceReadLists = new List<ResourceHandle>[(int)RenderGraphResourceType.Count];
        public List<ResourceHandle>[] resourceWriteLists = new List<ResourceHandle>[(int)RenderGraphResourceType.Count];
        public List<ResourceHandle>[] transientResourceList = new List<ResourceHandle>[(int)RenderGraphResourceType.Count];

        public List<RendererListHandle> usedRendererListList = new List<RendererListHandle>();

        public RenderGraphPass()
        {
            for (int i = 0; i < (int)RenderGraphResourceType.Count; ++i)
            {
                resourceReadLists[i] = new List<ResourceHandle>();
                resourceWriteLists[i] = new List<ResourceHandle>();
                transientResourceList[i] = new List<ResourceHandle>();
            }
        }

        public void Clear()
        {
            name = "";
            index = -1;
            customSampler = null;
            for (int i = 0; i < (int)RenderGraphResourceType.Count; ++i)
            {
                resourceReadLists[i].Clear();
                resourceWriteLists[i].Clear();
                transientResourceList[i].Clear();
            }

            usedRendererListList.Clear();
            enableAsyncCompute = false;
            allowPassCulling = true;
            allowRendererListCulling = true;
            generateDebugData = true;
            refCount = 0;

            // Invalidate everything
            colorBufferMaxIndex = -1;
            depthBuffer = TextureHandle.nullHandle;
            for (int i = 0; i < RenderGraph.kMaxMRTCount; ++i)
            {
                colorBuffers[i] = TextureHandle.nullHandle;
            }
        }

        public void AddResourceWrite(in ResourceHandle res)
        {
            resourceWriteLists[res.iType].Add(res);
        }

        public void AddResourceRead(in ResourceHandle res)
        {
            resourceReadLists[res.iType].Add(res);
        }

        public void AddTransientResource(in ResourceHandle res)
        {
            transientResourceList[res.iType].Add(res);
        }

        public void UseRendererList(RendererListHandle rendererList)
        {
            usedRendererListList.Add(rendererList);
        }

        public void EnableAsyncCompute(bool value)
        {
            enableAsyncCompute = value;
        }

        public void AllowPassCulling(bool value)
        {
            allowPassCulling = value;
        }

        public void AllowRendererListCulling(bool value)
        {
            allowRendererListCulling = value;
        }

        public void GenerateDebugData(bool value)
        {
            generateDebugData = value;
        }

        public void SetColorBuffer(TextureHandle resource, int index)
        {
            Debug.Assert(index < RenderGraph.kMaxMRTCount && index >= 0);
            colorBufferMaxIndex = Math.Max(colorBufferMaxIndex, index);
            colorBuffers[index] = resource;
            AddResourceWrite(resource.handle);
        }

        public void SetDepthBuffer(TextureHandle resource, DepthAccess flags)
        {
            depthBuffer = resource;
            if ((flags & DepthAccess.Read) != 0)
                AddResourceRead(resource.handle);
            if ((flags & DepthAccess.Write) != 0)
                AddResourceWrite(resource.handle);
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
