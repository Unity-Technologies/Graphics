using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace UnityEngine.Rendering.RenderGraphModule
{
    [DebuggerDisplay("RenderPass: {name} (Index:{index} Async:{enableAsyncCompute})")]
    abstract class RenderGraphPass
    {
        public abstract void Execute(InternalRenderGraphContext renderGraphContext);
        public abstract void Release(RenderGraphObjectPool pool);
        public abstract bool HasRenderFunc();
        public abstract int GetRenderFuncHash();

        public string name { get; protected set; }
        public int index { get; protected set; }
        public RenderGraphPassType type { get; internal set; }
        public ProfilingSampler customSampler { get; protected set; }
        public bool enableAsyncCompute { get; protected set; }
        public bool allowPassCulling { get; protected set; }
        public bool allowGlobalState { get; protected set; }
        public bool enableFoveatedRasterization { get; protected set; }

        // Before using the AccessFlags use resourceHandle.isValid()
        // to make sure that the data in the colorBuffer/fragmentInput/randomAccessResource buffers are up to date
        public TextureAccess depthAccess { get; protected set; }

        public TextureAccess[] colorBufferAccess { get; protected set; } = new TextureAccess[RenderGraph.kMaxMRTCount];
        public int colorBufferMaxIndex { get; protected set; } = -1;

        // Used by native pass compiler only
        public TextureAccess[] fragmentInputAccess { get; protected set; } = new TextureAccess[RenderGraph.kMaxMRTCount];
        public int fragmentInputMaxIndex { get; protected set; } = -1;

        public struct RandomWriteResourceInfo
        {
            public ResourceHandle h;
            public bool preserveCounterValue;
        }

        // This list can contain both texture and buffer resources based on their binding index.
        public RandomWriteResourceInfo[] randomAccessResource { get; protected set; } = new RandomWriteResourceInfo[RenderGraph.kMaxMRTCount];
        public int randomAccessResourceMaxIndex { get; protected set; } = -1;

        public bool generateDebugData { get; protected set; }

        public bool allowRendererListCulling { get; protected set; }

        public List<ResourceHandle>[] resourceReadLists = new List<ResourceHandle>[(int)RenderGraphResourceType.Count];
        public List<ResourceHandle>[] resourceWriteLists = new List<ResourceHandle>[(int)RenderGraphResourceType.Count];
        public List<ResourceHandle>[] transientResourceList = new List<ResourceHandle>[(int)RenderGraphResourceType.Count];

        public List<RendererListHandle> usedRendererListList = new List<RendererListHandle>();

        public List<ValueTuple<TextureHandle, int>> setGlobalsList = new List<ValueTuple<TextureHandle, int>>();
        public bool useAllGlobalTextures;

        public List<ResourceHandle> implicitReadsList = new List<ResourceHandle>();

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
            setGlobalsList.Clear();
            useAllGlobalTextures = false;
            implicitReadsList.Clear();
            enableAsyncCompute = false;
            allowPassCulling = true;
            allowRendererListCulling = true;
            allowGlobalState = false;
            enableFoveatedRasterization = false;
            generateDebugData = true;

            // Invalidate the buffers without clearing them, as it is too costly
            // Use IsValid() to make sure that the data in the colorBuffer/fragmentInput/randomAccessResource buffers are up to date
            colorBufferMaxIndex = -1;
            fragmentInputMaxIndex = -1;
            randomAccessResourceMaxIndex = -1;
        }

        // Check if the pass has any render targets set-up
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasRenderAttachments()
        {
            return depthAccess.textureHandle.IsValid() || colorBufferAccess[0].textureHandle.IsValid() || colorBufferMaxIndex > 0;
        }

        // Checks if the resource is involved in this pass
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsTransient(in ResourceHandle res)
        {
            return transientResourceList[res.iType].Contains(res);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsRead(in ResourceHandle res)
        {
            if (res.IsVersioned)
            {
                return resourceReadLists[res.iType].Contains(res);
            }
            else
            {
                // Just look if we are reading any version of this texture.
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsAttachment(in TextureHandle res)
        {
            // We ignore the version when checking, if any version is used it is considered a match

            if (depthAccess.textureHandle.IsValid() && depthAccess.textureHandle.handle.index == res.handle.index) return true;
            for (int i = 0; i < colorBufferAccess.Length; i++)
            {
                if (colorBufferAccess[i].textureHandle.IsValid() && colorBufferAccess[i].textureHandle.handle.index == res.handle.index) return true;
            }

            return false;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddResourceWrite(in ResourceHandle res)
        {
            resourceWriteLists[res.iType].Add(res);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddResourceRead(in ResourceHandle res)
        {
            resourceReadLists[res.iType].Add(res);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddTransientResource(in ResourceHandle res)
        {
            transientResourceList[res.iType].Add(res);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UseRendererList(in RendererListHandle rendererList)
        {
            usedRendererListList.Add(rendererList);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void EnableAsyncCompute(bool value)
        {
            enableAsyncCompute = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AllowPassCulling(bool value)
        {
            allowPassCulling = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void EnableFoveatedRasterization(bool value)
        {
            enableFoveatedRasterization = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AllowRendererListCulling(bool value)
        {
            allowRendererListCulling = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AllowGlobalState(bool value)
        {
            allowGlobalState = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void GenerateDebugData(bool value)
        {
            generateDebugData = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetColorBuffer(in TextureHandle resource, int index)
        {
            Debug.Assert(index < RenderGraph.kMaxMRTCount && index >= 0);
            colorBufferMaxIndex = Math.Max(colorBufferMaxIndex, index);
            colorBufferAccess[index].textureHandle = resource;
            AddResourceWrite(resource.handle);
        }

        // Sets up the color buffer for this pass but not any resource Read/Writes for it
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetColorBufferRaw(in TextureHandle resource, int index, AccessFlags accessFlags, int mipLevel, int depthSlice)
        {
            Debug.Assert(index < RenderGraph.kMaxMRTCount && index >= 0);
            if (colorBufferAccess[index].textureHandle.handle.Equals(resource.handle) || !colorBufferAccess[index].textureHandle.IsValid())
            {
                colorBufferMaxIndex = Math.Max(colorBufferMaxIndex, index);
                colorBufferAccess[index].textureHandle = resource;
                colorBufferAccess[index].flags = accessFlags;
                colorBufferAccess[index].mipLevel = mipLevel;
                colorBufferAccess[index].depthSlice = depthSlice;
            }
            else
            {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                // You tried to do SetRenderAttachment(tex1, 1, ..); SetRenderAttachment(tex2, 1, ..); that is not valid for different textures on the same index
                throw new InvalidOperationException("You can only bind a single texture to an MRT index. Verify your indexes are correct.");
#endif
            }
        }

        // Sets up the color buffer for this pass but not any resource Read/Writes for it
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetFragmentInputRaw(in TextureHandle resource, int index, AccessFlags accessFlags, int mipLevel, int depthSlice)
        {
            Debug.Assert(index < RenderGraph.kMaxMRTCount && index >= 0);
            if (fragmentInputAccess[index].textureHandle.handle.Equals(resource.handle) || !fragmentInputAccess[index].textureHandle.IsValid())
            {
                fragmentInputMaxIndex = Math.Max(fragmentInputMaxIndex, index);
                fragmentInputAccess[index].textureHandle = resource;
                fragmentInputAccess[index].flags = accessFlags;
                fragmentInputAccess[index].mipLevel = mipLevel;
                fragmentInputAccess[index].depthSlice = depthSlice;
            }
            else
            {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                // You tried to do SetRenderAttachment(tex1, 1, ..); SetRenderAttachment(tex2, 1, ..); that is not valid for different textures on the same index
                throw new InvalidOperationException("You can only bind a single texture to an fragment input index. Verify your indexes are correct.");
#endif
            }
        }

        // Sets up the color buffer for this pass but not any resource Read/Writes for it
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetRandomWriteResourceRaw(in ResourceHandle resource, int index, bool preserveCounterValue, AccessFlags accessFlags)
        {
            Debug.Assert(index < RenderGraph.kMaxMRTCount && index >= 0);
            if (randomAccessResource[index].h.Equals(resource) || !randomAccessResource[index].h.IsValid())
            {
                randomAccessResourceMaxIndex = Math.Max(randomAccessResourceMaxIndex, index);
                ref var info = ref randomAccessResource[index];
                info.h = resource;
                info.preserveCounterValue = preserveCounterValue;
            }
            else
            {
                // You tried to do SetRenderAttachment(tex1, 1, ..); SetRenderAttachment(tex2, 1, ..); that is not valid for different textures on the same index
                throw new InvalidOperationException("You can only bind a single texture to an random write input index. Verify your indexes are correct.");
            }
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetDepthBuffer(in TextureHandle resource, DepthAccess flags)
        {
            depthAccess = new TextureAccess(resource, (AccessFlags)flags, 0, 0);
            if ((flags & DepthAccess.Read) != 0)
                AddResourceRead(resource.handle);
            if ((flags & DepthAccess.Write) != 0)
                AddResourceWrite(resource.handle);
        }

        // Sets up the depth buffer for this pass but not any resource Read/Writes for it
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetDepthBufferRaw(in TextureHandle resource, AccessFlags accessFlags, int mipLevel, int depthSlice)
        {
            // If no depth buffer yet or if it is the same one as the previous one, allow the call otherwise log an error.
            if (depthAccess.textureHandle.handle.Equals(resource.handle) || !depthAccess.textureHandle.IsValid())
            {
                depthAccess = new TextureAccess(resource, accessFlags, mipLevel, depthSlice);
            }
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            else
            {
                throw new InvalidOperationException("You can only set a single depth texture per pass.");
            }
#endif
        }


        // Here we want to keep computation to a minimum and only hash what will influence NRP compiler: Pass merging, load/store actions etc.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void ComputeTextureHash(ref int hash, in ResourceHandle handle, RenderGraphResourceRegistry resources)
        {
            if (handle.index != 0)
            {
                if (resources.IsRenderGraphResourceImported(handle))
                {
                    var res = resources.GetTextureResource(handle);
                    if (res.graphicsResource.externalTexture != null) // External texture
                    {
                        var externalTexture = res.graphicsResource.externalTexture;
                        hash = hash * 23 + (int)externalTexture.graphicsFormat;
                        hash = hash * 23 + (int)externalTexture.dimension;
                        hash = hash * 23 + externalTexture.width;
                        hash = hash * 23 + externalTexture.height;
                        if (externalTexture is RenderTexture externalRT)
                            hash = hash * 23 + externalRT.antiAliasing;
                    }
                    else if (res.graphicsResource.rt != null) // Regular RTHandle
                    {
                        var rt = res.graphicsResource.rt;
                        hash = hash * 23 + (int)rt.graphicsFormat;
                        hash = hash * 23 + (int)rt.dimension;
                        hash = hash * 23 + rt.antiAliasing;
                        if (res.graphicsResource.useScaling)
                        {
                            if (res.graphicsResource.scaleFunc != null)
                                hash = hash * 23 + res.graphicsResource.scaleFunc.GetHashCode();
                            else
                                hash = hash * 23 + res.graphicsResource.scaleFactor.GetHashCode();
                        }
                        else
                        {
                            hash = hash * 23 + rt.width;
                            hash = hash * 23 + rt.height;
                        }
                    }
                    else if (res.graphicsResource.nameID != default(RenderTargetIdentifier)) // External RTI
                    {
                        // The only info we have is from the provided desc upon importing.
                        ref var desc = ref res.desc;
                        hash = hash * 23 + (int)desc.colorFormat;
                        hash = hash * 23 + (int)desc.dimension;
                        hash = hash * 23 + (int)desc.msaaSamples;
                        hash = hash * 23 + desc.width;
                        hash = hash * 23 + desc.height;
                    }

                    // Add the clear/discard buffer flags to the hash (used in all the cases above)
                    hash = hash * 23 + res.desc.clearBuffer.GetHashCode();
                    hash = hash * 23 + res.desc.discardBuffer.GetHashCode();
                }
                else
                {
                    var desc = resources.GetTextureResourceDesc(handle);
                    hash = hash * 23 + (int)desc.colorFormat;
                    hash = hash * 23 + (int)desc.dimension;
                    hash = hash * 23 + (int)desc.msaaSamples;
                    hash = hash * 23 + desc.clearBuffer.GetHashCode();
                    hash = hash * 23 + desc.discardBuffer.GetHashCode();
                    switch (desc.sizeMode)
                    {
                        case TextureSizeMode.Explicit:
                            hash = hash * 23 + desc.width;
                            hash = hash * 23 + desc.height;
                            break;
                        case TextureSizeMode.Scale:
                            hash = hash * 23 + desc.scale.GetHashCode();
                            break;
                        case TextureSizeMode.Functor:
                            hash = hash * 23 + desc.func.GetHashCode();
                            break;
                    }
                }
            }
        }

        // This function is performance sensitive.
        // Avoid mass function calls to get the hashCode and compute locally instead.
        public int ComputeHash(RenderGraphResourceRegistry resources)
        {
            int hash = index;
            hash = hash * 23 + (int)type;
            hash = hash * 23 + (enableAsyncCompute ? 1 : 0);
            hash = hash * 23 + (allowPassCulling ? 1 : 0);
            hash = hash * 23 + (allowGlobalState ? 1 : 0);
            hash = hash * 23 + (enableFoveatedRasterization ? 1 : 0);

            var depthHandle = depthAccess.textureHandle.handle;
            if (depthHandle.IsValid())
            {
                hash = hash * 23 + depthHandle.index;
                hash = hash * 23 + (int)depthAccess.flags;
                hash = hash * 23 + depthAccess.mipLevel;
                hash = hash * 23 + depthAccess.depthSlice;
                ComputeTextureHash(ref hash, depthHandle, resources);
            }

            for (int i = 0; i < colorBufferMaxIndex + 1; ++i)
            {
                var handle = colorBufferAccess[i].textureHandle.handle;
                if (handle.IsValid())
                {
                    ComputeTextureHash(ref hash, handle, resources);
                    hash = hash * 23 + handle.index;
                    hash = hash * 23 + (int)colorBufferAccess[i].flags;
                    hash = hash * 23 + colorBufferAccess[i].mipLevel;
                    hash = hash * 23 + colorBufferAccess[i].depthSlice;
                }
            }

            hash = hash * 23 + colorBufferMaxIndex;

            for (int i = 0; i < fragmentInputMaxIndex + 1; ++i)
            {
                var handle = fragmentInputAccess[i].textureHandle.handle;
                if (handle.IsValid())
                {
                    ComputeTextureHash(ref hash, handle, resources);
                    hash = hash * 23 + handle.index;
                    hash = hash * 23 + (int)fragmentInputAccess[i].flags;
                    hash = hash * 23 + fragmentInputAccess[i].mipLevel;
                    hash = hash * 23 + fragmentInputAccess[i].depthSlice;
                }
            }

            for (int i = 0; i < randomAccessResourceMaxIndex + 1; ++i)
            {
                var rar = randomAccessResource[i];
                if (rar.h.IsValid())
                {
                    hash = hash * 23 + rar.h.index;
                    hash = hash * 23 + (rar.preserveCounterValue ? 1 : 0);
                }
            }
            hash = hash * 23 + randomAccessResourceMaxIndex;

            hash = hash * 23 + fragmentInputMaxIndex;
            hash = hash * 23 + (generateDebugData ? 1 : 0);
            hash = hash * 23 + (allowRendererListCulling ? 1 : 0);

            for (int resType = 0; resType < (int)RenderGraphResourceType.Count; resType++)
            {
                var resourceReads = resourceReadLists[resType];
                for (int i = 0; i < resourceReads.Count; ++i)
                    hash = hash * 23 + resourceReads[i].index;

                var resourceWrites = resourceWriteLists[resType];
                for (int i = 0; i < resourceWrites.Count; ++i)
                    hash = hash * 23 + resourceWrites[i].index;

                var resourceTransient = transientResourceList[resType];
                for (int i = 0; i < resourceTransient.Count; ++i)
                    hash = hash * 23 + resourceTransient[i].index;
            }

            for (int i = 0; i < usedRendererListList.Count; ++i)
                hash = hash * 23 + usedRendererListList[i].handle;

            for (int i = 0; i < setGlobalsList.Count; ++i)
            {
                var global = setGlobalsList[i];
                hash = hash * 23 + global.Item1.handle.index;
                hash = hash * 23 + global.Item2;
            }
            hash = hash * 23 + (useAllGlobalTextures ? 1 : 0);

            for (int i = 0; i < implicitReadsList.Count; ++i)
                hash = hash * 23 + implicitReadsList[i].index;

            hash = hash * 23 + GetRenderFuncHash();

            return hash;
        }
    }

    // This used to have an extra generic argument 'RenderGraphContext' abstracting the context and avoiding
    // the RenderGraphPass/ComputeRenderGraphPass/RasterRenderGraphPass/UnsafeRenderGraphPass classes below
    // but this confuses IL2CPP and causes garbage when boxing the context created (even though they are structs)
    [DebuggerDisplay("RenderPass: {name} (Index:{index} Async:{enableAsyncCompute})")]
    internal abstract class BaseRenderGraphPass<PassData> : RenderGraphPass
        where PassData : class, new()
    {
        internal PassData data;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void Execute(InternalRenderGraphContext renderGraphContext)
        {
            c.FromInternalContext(renderGraphContext);
            renderFunc(data, c);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void Release(RenderGraphObjectPool pool)
        {
            pool.Release(data);
            data = null;
            renderFunc = null;

            // We need to do the release from here because we need the final type.
            pool.Release(this);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool HasRenderFunc()
        {
            return renderFunc != null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetRenderFuncHash()
        {
            return renderFunc.GetHashCode();
        }
    }

    [DebuggerDisplay("RenderPass: {name} (Index:{index} Async:{enableAsyncCompute})")]
    internal sealed class ComputeRenderGraphPass<PassData> : BaseRenderGraphPass<PassData>
    where PassData : class, new()
    {
        internal BaseRenderFunc<PassData, ComputeGraphContext> renderFunc;
        internal static ComputeGraphContext c = new ComputeGraphContext();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void Execute(InternalRenderGraphContext renderGraphContext)
        {
            c.FromInternalContext(renderGraphContext);
            renderFunc(data, c);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void Release(RenderGraphObjectPool pool)
        {
            pool.Release(data);
            data = null;
            renderFunc = null;

            // We need to do the release from here because we need the final type.
            pool.Release(this);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool HasRenderFunc()
        {
            return renderFunc != null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetRenderFuncHash()
        {
            return renderFunc.GetHashCode();
        }
    }

    [DebuggerDisplay("RenderPass: {name} (Index:{index} Async:{enableAsyncCompute})")]
    internal sealed class RasterRenderGraphPass<PassData> : BaseRenderGraphPass<PassData>
    where PassData : class, new()
    {
        internal BaseRenderFunc<PassData, RasterGraphContext> renderFunc;
        internal static RasterGraphContext c = new RasterGraphContext();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void Execute(InternalRenderGraphContext renderGraphContext)
        {
            c.FromInternalContext(renderGraphContext);
            renderFunc(data, c);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void Release(RenderGraphObjectPool pool)
        {
            pool.Release(data);
            data = null;
            renderFunc = null;

            // We need to do the release from here because we need the final type.
            pool.Release(this);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool HasRenderFunc()
        {
            return renderFunc != null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetRenderFuncHash()
        {
            return renderFunc.GetHashCode();
        }
    }

    [DebuggerDisplay("RenderPass: {name} (Index:{index} Async:{enableAsyncCompute})")]
    internal sealed class UnsafeRenderGraphPass<PassData> : BaseRenderGraphPass<PassData>
        where PassData : class, new()
    {
        internal BaseRenderFunc<PassData, UnsafeGraphContext> renderFunc;
        internal static UnsafeGraphContext c = new UnsafeGraphContext();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void Execute(InternalRenderGraphContext renderGraphContext)
        {
            c.FromInternalContext(renderGraphContext);
            renderFunc(data, c);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void Release(RenderGraphObjectPool pool)
        {
            pool.Release(data);
            data = null;
            renderFunc = null;

            // We need to do the release from here because we need the final type.
            pool.Release(this);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool HasRenderFunc()
        {
            return renderFunc != null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetRenderFuncHash()
        {
            return renderFunc.GetHashCode();
        }
    }
}
