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

        public abstract void Execute(InternalRenderGraphContext renderGraphContext);
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

        public string file { get; set; }
        public int line { get; set; }

        public List<ResourceHandle>[] resourceReadLists = new List<ResourceHandle>[(int)RenderGraphResourceType.Count];
        public List<ResourceHandle>[] resourceWriteLists = new List<ResourceHandle>[(int)RenderGraphResourceType.Count];
        public List<ResourceHandle>[] transientResourceList = new List<ResourceHandle>[(int)RenderGraphResourceType.Count];

        public List<RendererListHandle> usedRendererListList = new List<RendererListHandle>();

        public List<RendererListHandle> dependsOnRendererListList = new List<RendererListHandle>();

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
            dependsOnRendererListList.Clear();
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

        // Checks if the resource is involved in this pass
        public bool IsTransient(in ResourceHandle res)
        {
            return transientResourceList[res.iType].Contains(res);
        }

        public bool IsWriten(in ResourceHandle res)
        {
            // You can only ever write to the latest version so we ignore it when looking in the list
            for (int i = 0; i < resourceWriteLists[res.iType].Count; i++)
            {
                if (resourceWriteLists[res.iType][i].index == res.index)
                {
                    return true;
                }
            }
            return false;
        }

        public bool IsRead(in ResourceHandle res)
        {
            return resourceReadLists[res.iType].Contains(res);
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

        public void DependsOnRendererList(RendererListHandle rendererList)
        {
            dependsOnRendererListList.Add(rendererList);
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

        public void SetColorBuffer(RenderGraphResourceRegistry reg, TextureHandle resource, int index, ColorAccess flags = ColorAccess.ReadWrite)
        {
            Debug.Assert(index < RenderGraph.kMaxMRTCount && index >= 0);
            if (colorBuffers[index].handle == resource.handle || colorBuffers[index].handle == TextureHandle.nullHandle.handle)
            {
                colorBufferMaxIndex = Math.Max(colorBufferMaxIndex, index);
                colorBuffers[index] = resource;
                /*if ((flags & ColorAccess.Read) != 0)
                    AddResourceRead(resource.handle);
                if ((flags & ColorAccess.PartialWrite) != 0)
                    AddResourceWrite(resource.handle);*/
                if ((flags & ColorAccess.DiscardContents) == 0)
                {
                    AddResourceRead(reg.GetLatestVersionHandle(resource.handle));
                }
                if ((flags & ColorAccess.PartialWrite) != 0)
                {
                    reg.NewVersion(resource.handle);
                    AddResourceWrite(reg.GetLatestVersionHandle(resource.handle));
                }
            }
            else
            {
                throw new Exception("You can only bind a single texture to an MRT index. Verify your indexes are correct.");
            }
        }

        // Sets up the color buffer for this pass but not any resource Read/Writes for it
        public void SetColorBufferRaw(TextureHandle resource, int index)
        {
            Debug.Assert(index < RenderGraph.kMaxMRTCount && index >= 0);
            if (colorBuffers[index].handle == resource.handle || colorBuffers[index].handle == TextureHandle.nullHandle.handle)
            {
                colorBufferMaxIndex = Math.Max(colorBufferMaxIndex, index);
                colorBuffers[index] = resource;
            }
            else
            {
                throw new Exception("You can only bind a single texture to an MRT index. Verify your indexes are correct.");
            }
        }

        public void SetDepthBuffer(RenderGraphResourceRegistry reg, TextureHandle resource, DepthAccess flags)
        {
            // If no depth buffer yet or it's the same one as previous allow the call otherwise log an error.
            if (depthBuffer.handle == resource.handle || depthBuffer.handle == TextureHandle.nullHandle.handle)
            {
                depthBuffer = resource;
                if ((flags & DepthAccess.Read) != 0)
                    AddResourceRead(reg.GetLatestVersionHandle(resource.handle));
                if ((flags & DepthAccess.Write) != 0)
                {
                    reg.NewVersion(resource.handle);
                    AddResourceWrite(reg.GetLatestVersionHandle(resource.handle));
                }
            }
            else
            {
                throw new Exception("You can only set a single depth texture per pass.");
            }
        }

        // Sets up the depth buffer for this pass but not any resource Read/Writes for it
        public void SetDepthBufferRaw(TextureHandle resource)
        {
            // If no depth buffer yet or it's the same one as previous allow the call otherwise log an error.
            if (depthBuffer.handle == resource.handle || depthBuffer.handle == TextureHandle.nullHandle.handle)
            {
                depthBuffer = resource;
            }
            else
            {
                throw new Exception("You can only set a single depth texture per pass.");
            }
        }
    }

    [DebuggerDisplay("RenderPass: {name} (Index:{index} Async:{enableAsyncCompute})")]
    internal sealed class RenderGraphPass<PassData> : RenderGraphPass
        where PassData : class, new()
    {
        internal PassData data;
        internal RenderFunc<PassData> renderFunc;

        public override void Execute(InternalRenderGraphContext renderGraphContext)
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
