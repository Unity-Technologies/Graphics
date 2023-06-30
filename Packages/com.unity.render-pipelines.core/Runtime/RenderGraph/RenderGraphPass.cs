using System;
using System.Diagnostics;
using System.Collections.Generic;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.RenderGraphModule
{
    [DebuggerDisplay("RenderPass: {name} (Index:{index} Async:{enableAsyncCompute})")]
    abstract class RenderGraphPass
    {
        public abstract void Execute(InternalRenderGraphContext renderGraphContext);
        public abstract void Release(RenderGraphObjectPool pool);
        public abstract bool HasRenderFunc();

        public string name { get; protected set; }
        public int index { get; protected set; }
        public RenderGraphPassType type { get; internal set; }
        public ProfilingSampler customSampler { get; protected set; }
        public bool enableAsyncCompute { get; protected set; }
        public bool allowPassCulling { get; protected set; }
        public bool allowGlobalState { get; protected set; }

        public TextureHandle depthBuffer { get; protected set; }
        public IBaseRenderGraphBuilder.AccessFlags depthBufferAccessFlags { get; protected set; }

        public TextureHandle[] colorBuffers { get; protected set; } = new TextureHandle[RenderGraph.kMaxMRTCount];
        public IBaseRenderGraphBuilder.AccessFlags[] colorBufferAccessFlags { get; protected set; } = new IBaseRenderGraphBuilder.AccessFlags[RenderGraph.kMaxMRTCount];
        public int colorBufferMaxIndex { get; protected set; } = -1;

        // Used by native pass compiler only
        public TextureHandle[] fragmentInputs { get; protected set; } = new TextureHandle[RenderGraph.kMaxMRTCount];
        public IBaseRenderGraphBuilder.AccessFlags[] fragmentInputAccessFlags { get; protected set; } = new IBaseRenderGraphBuilder.AccessFlags[RenderGraph.kMaxMRTCount];
        public int fragmentInputMaxIndex { get; protected set; } = -1;

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
            allowGlobalState = false;
            generateDebugData = true;
            refCount = 0;

            // Invalidate everything
            colorBufferMaxIndex = -1;
            depthBuffer = TextureHandle.nullHandle;
            for (int i = 0; i < RenderGraph.kMaxMRTCount; ++i)
            {
                colorBuffers[i] = TextureHandle.nullHandle;
                colorBufferAccessFlags[i] = IBaseRenderGraphBuilder.AccessFlags.None;
            }
            fragmentInputMaxIndex = -1;
            for (int i = 0; i < RenderGraph.kMaxMRTCount; ++i)
            {
                fragmentInputs[i] = TextureHandle.nullHandle;
                fragmentInputAccessFlags[i] = IBaseRenderGraphBuilder.AccessFlags.None;
            }
        }


        // Checks if the resource is involved in this pass
        public bool IsTransient(in ResourceHandle res)
        {
            return transientResourceList[res.iType].Contains(res);
        }

        public bool IsWritten(in ResourceHandle res)
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
            if (res.IsVersioned)
            {
                return resourceReadLists[res.iType].Contains(res);
            }
            else
            {
                // Just look if we are readying any version of this texture.
                // Note that in theory this pass could read from several versions of the same texture
                // e.g. ColorBuffer,v3 and ColorBuffer,v5 so this check is always conservative
                for (int i = 0; i < resourceReadLists[res.iType].Count; i++)
                {
                    if (resourceReadLists[res.iType][i].index == res.index)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        public bool IsAttachment(in TextureHandle res)
        {
            // We ignore the version when checking if any version is used it is considered a match

            if (depthBuffer.handle.index == res.handle.index) return true;
            for (int i = 0; i < colorBuffers.Length; i++)
            {
                if (colorBuffers[i].handle.index == res.handle.index) return true;
            }

            return false;
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

        public void AllowGlobalState(bool value)
        {
            allowGlobalState = value;
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

        // Sets up the color buffer for this pass but not any resource Read/Writes for it
        public void SetColorBufferRaw(TextureHandle resource, int index, IBaseRenderGraphBuilder.AccessFlags accessFlags)
        {
            Debug.Assert(index < RenderGraph.kMaxMRTCount && index >= 0);
            if (colorBuffers[index].handle.Equals(resource.handle) || colorBuffers[index].handle.IsNull())
            {
                colorBufferMaxIndex = Math.Max(colorBufferMaxIndex, index);
                colorBuffers[index] = resource;
                colorBufferAccessFlags[index] = accessFlags;
            }
            else
            {
                // You tried to do UseTextureFragment(tex1, 1, ..); UseTextureFragment(tex2, 1, ..); that is not valid for different textures on the same index
                throw new InvalidOperationException("You can only bind a single texture to an MRT index. Verify your indexes are correct.");
            }
        }

        // Sets up the color buffer for this pass but not any resource Read/Writes for it
        public void SetFragmentInputRaw(TextureHandle resource, int index, IBaseRenderGraphBuilder.AccessFlags accessFlags)
        {
            Debug.Assert(index < RenderGraph.kMaxMRTCount && index >= 0);
            if (fragmentInputs[index].handle.Equals(resource.handle) || fragmentInputs[index].handle.IsNull())
            {
                fragmentInputMaxIndex = Math.Max(fragmentInputMaxIndex, index);
                fragmentInputs[index] = resource;
                fragmentInputAccessFlags[index] = accessFlags;
            }
            else
            {
                // You tried to do UseTextureFragment(tex1, 1, ..); UseTextureFragment(tex2, 1, ..); that is not valid for different textures on the same index
                throw new InvalidOperationException("You can only bind a single texture to an fragment input index. Verify your indexes are correct.");
            }
        }

        public void SetDepthBuffer(TextureHandle resource, DepthAccess flags)
        {
            depthBuffer = resource;
            if ((flags & DepthAccess.Read) != 0)
                AddResourceRead(resource.handle);
            if ((flags & DepthAccess.Write) != 0)
                AddResourceWrite(resource.handle);
        }

        // Sets up the depth buffer for this pass but not any resource Read/Writes for it
        public void SetDepthBufferRaw(TextureHandle resource, IBaseRenderGraphBuilder.AccessFlags accessFlags)
        {
            // If no depth buffer yet or it's the same one as previous allow the call otherwise log an error.
            if (depthBuffer.handle.Equals(resource.handle) || depthBuffer.handle.IsNull())
            {
                depthBuffer = resource;
                depthBufferAccessFlags = accessFlags;
            }
            else
            {
                throw new InvalidOperationException("You can only set a single depth texture per pass.");
            }
        }
    }

    // This used to have an extra generic argument 'RenderGraphContext' abstracting the context and avoiding
    // the RenderGraphPass/ComputeRenderGraphPass/RasterRenderGraphPass/LowLevelRenderGraphPass classes below
    // but this confuses IL2CPP and causes garbage when boxing the context created (even though they are structs)
    [DebuggerDisplay("RenderPass: {name} (Index:{index} Async:{enableAsyncCompute})")]
    internal abstract class BaseRenderGraphPass<PassData> : RenderGraphPass
        where PassData : class, new()
    {
        internal PassData data;

        public void Initialize(int passIndex, PassData passData, string passName, RenderGraphPassType passType, ProfilingSampler sampler)
        {
            Clear();
            index = passIndex;
            data = passData;
            name = passName;
            type = passType;
            customSampler = sampler;
        }
    }

    [DebuggerDisplay("RenderPass: {name} (Index:{index} Async:{enableAsyncCompute})")]
    internal sealed class RenderGraphPass<PassData> : BaseRenderGraphPass<PassData>
        where PassData : class, new()
    {
        internal BaseRenderFunc<PassData, RenderGraphContext> renderFunc;
        internal static RenderGraphContext c = new RenderGraphContext();
        public override void Execute(InternalRenderGraphContext renderGraphContext)
        {
            c.FromInternalContext(renderGraphContext);
            renderFunc(data, c);
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

    [DebuggerDisplay("RenderPass: {name} (Index:{index} Async:{enableAsyncCompute})")]
    internal sealed class ComputeRenderGraphPass<PassData> : BaseRenderGraphPass<PassData>
    where PassData : class, new()
    {
        internal BaseRenderFunc<PassData, ComputeGraphContext> renderFunc;
        internal static ComputeGraphContext c = new ComputeGraphContext(); 
        public override void Execute(InternalRenderGraphContext renderGraphContext)
        {
            c.FromInternalContext(renderGraphContext);
            renderFunc(data, c);
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

    [DebuggerDisplay("RenderPass: {name} (Index:{index} Async:{enableAsyncCompute})")]
    internal sealed class RasterRenderGraphPass<PassData> : BaseRenderGraphPass<PassData>
    where PassData : class, new()
    {
        internal BaseRenderFunc<PassData, RasterGraphContext> renderFunc;
        internal static RasterGraphContext c = new RasterGraphContext();
        public override void Execute(InternalRenderGraphContext renderGraphContext)
        {
            c.FromInternalContext(renderGraphContext);
           renderFunc(data, c);
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

    [DebuggerDisplay("RenderPass: {name} (Index:{index} Async:{enableAsyncCompute})")]
    internal sealed class LowLevelRenderGraphPass<PassData> : BaseRenderGraphPass<PassData>
    where PassData : class, new()
    {
        internal BaseRenderFunc<PassData, LowLevelGraphContext> renderFunc;
        internal static LowLevelGraphContext c = new LowLevelGraphContext();

        public override void Execute(InternalRenderGraphContext renderGraphContext)
        {
            c.FromInternalContext(renderGraphContext);
            renderFunc(data, c);
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
