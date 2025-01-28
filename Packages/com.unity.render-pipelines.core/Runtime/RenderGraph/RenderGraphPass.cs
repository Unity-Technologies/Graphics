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

        public bool hasShadingRateImage { get; protected set; }
        public TextureAccess shadingRateAccess { get; protected set; }

        public bool hasShadingRateStates { get; protected set; }
        public ShadingRateFragmentSize shadingRateFragmentSize { get; protected set; }
        public ShadingRateCombiner primitiveShadingRateCombiner { get; protected set; }
        public ShadingRateCombiner fragmentShadingRateCombiner { get; protected set; }

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

            // We do not need to clear colorBufferAccess and fragmentInputAccess as we have the colorBufferMaxIndex and fragmentInputMaxIndex
            // which are reset above so we only clear depthAccess here.
            depthAccess = default(TextureAccess);

            hasShadingRateImage = false;
            hasShadingRateStates = false;
            shadingRateFragmentSize = ShadingRateFragmentSize.FragmentSize1x1;
            primitiveShadingRateCombiner = ShadingRateCombiner.Keep;
            fragmentShadingRateCombiner = ShadingRateCombiner.Keep;
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
            // Versioning doesn't matter much for transient resources as they are only used within a single pass
            for (int i = 0; i < transientResourceList[res.iType].Count; i++)
            {
                if (transientResourceList[res.iType][i].index == res.index)
                {
                    return true;
                }
            }
            return false;
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
        void ComputeTextureHash(ref HashFNV1A32 generator, in ResourceHandle handle, RenderGraphResourceRegistry resources)
        {
            if (handle.index == 0)
                return;

            if (resources.IsRenderGraphResourceImported(handle))
            {
                var res = resources.GetTextureResource(handle);
                var graphicsResource = res.graphicsResource;
                ref var desc = ref res.desc;
                
                var externalTexture = graphicsResource.externalTexture;
                if (externalTexture != null) // External texture
                {
                    generator.Append((int) externalTexture.graphicsFormat);
                    generator.Append((int) externalTexture.dimension);
                    generator.Append(externalTexture.width);
                    generator.Append(externalTexture.height);
                    if (externalTexture is RenderTexture externalRT)
                        generator.Append(externalRT.antiAliasing);
                }
                else if (graphicsResource.rt != null) // Regular RTHandle
                {
                    var rt = graphicsResource.rt;
                    generator.Append((int) rt.graphicsFormat);
                    generator.Append((int) rt.dimension);
                    generator.Append(rt.antiAliasing);
                    if (graphicsResource.useScaling)
                        if (graphicsResource.scaleFunc != null)
                            generator.Append(DelegateHashCodeUtils.GetFuncHashCode(graphicsResource.scaleFunc));
                        else
                            generator.Append(graphicsResource.scaleFactor);
                    else
                    {
                        generator.Append(rt.width);
                        generator.Append(rt.height);
                    }
                }
                else if (graphicsResource.nameID != default) // External RTI
                {
                    // The only info we have is from the provided desc upon importing.
                    generator.Append((int) desc.format);
                    generator.Append((int) desc.dimension);
                    generator.Append((int) desc.msaaSamples);
                    generator.Append(desc.width);
                    generator.Append(desc.height);
                }

                // Add the clear/discard buffer flags to the hash (used in all the cases above)
                generator.Append(desc.clearBuffer);
                generator.Append(desc.discardBuffer);
            }
            else
            {
                var desc = resources.GetTextureResourceDesc(handle);
                generator.Append((int) desc.format);
                generator.Append((int) desc.dimension);
                generator.Append((int) desc.msaaSamples);
                generator.Append(desc.clearBuffer);
                generator.Append(desc.discardBuffer);
                switch (desc.sizeMode)
                {
                    case TextureSizeMode.Explicit:
                        generator.Append(desc.width);
                        generator.Append(desc.height);
                        break;
                    case TextureSizeMode.Scale:
                        generator.Append(desc.scale);
                        break;
                    case TextureSizeMode.Functor:
                        generator.Append(DelegateHashCodeUtils.GetFuncHashCode(desc.func));
                        break;
                }
            }
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void ComputeHashForTextureAccess(ref HashFNV1A32 generator, in ResourceHandle handle, in TextureAccess textureAccess)
        {
            generator.Append(handle.index);
            generator.Append((int) textureAccess.flags);
            generator.Append(textureAccess.mipLevel);
            generator.Append(textureAccess.depthSlice);
        }

        // This function is performance sensitive.
        // Avoid mass function calls to get the hashCode and compute locally instead.
        public void ComputeHash(ref HashFNV1A32 generator, RenderGraphResourceRegistry resources)
        {
            generator.Append((int) type);
            generator.Append(enableAsyncCompute);
            generator.Append(allowPassCulling);
            generator.Append(allowGlobalState);
            generator.Append(enableFoveatedRasterization);

            var depthHandle = depthAccess.textureHandle.handle;
            if (depthHandle.IsValid())
            {
                ComputeTextureHash(ref generator, depthHandle, resources);
                ComputeHashForTextureAccess(ref generator, depthHandle, depthAccess);
            }

            for (int i = 0; i < colorBufferMaxIndex + 1; ++i)
            {
                var colorBufferAccessElement = colorBufferAccess[i];
                var handle = colorBufferAccessElement.textureHandle.handle;
                if (!handle.IsValid())
                    continue;

                ComputeTextureHash(ref generator, handle, resources);
                ComputeHashForTextureAccess(ref generator, handle, colorBufferAccessElement);
            }

            generator.Append(colorBufferMaxIndex);

            generator.Append(hasShadingRateImage);
            if (hasShadingRateImage)
            {
                var handle = shadingRateAccess.textureHandle.handle;
                if (handle.IsValid())
                {
                    ComputeTextureHash(ref generator, handle, resources);
                    ComputeHashForTextureAccess(ref generator, handle, shadingRateAccess);
                }
            }

            generator.Append(hasShadingRateStates);
            generator.Append((int)shadingRateFragmentSize);
            generator.Append((int)primitiveShadingRateCombiner);
            generator.Append((int)fragmentShadingRateCombiner);

            for (int i = 0; i < fragmentInputMaxIndex + 1; ++i)
            {
                var fragmentInputAccessElement = fragmentInputAccess[i];
                var handle = fragmentInputAccessElement.textureHandle.handle;
                if (!handle.IsValid())
                    continue;

                ComputeTextureHash(ref generator, handle, resources);
                ComputeHashForTextureAccess(ref generator, handle, fragmentInputAccessElement);
            }

            for (int i = 0; i < randomAccessResourceMaxIndex + 1; ++i)
            {
                var rar = randomAccessResource[i];
                if (!rar.h.IsValid())
                    continue;

                generator.Append(rar.h.index);
                generator.Append(rar.preserveCounterValue);
            }
            generator.Append(randomAccessResourceMaxIndex);
            generator.Append(fragmentInputMaxIndex);
            generator.Append(generateDebugData);
            generator.Append(allowRendererListCulling);

            for (int resType = 0; resType < (int)RenderGraphResourceType.Count; resType++)
            {
                var resourceReads = resourceReadLists[resType];
                var resourceReadsCount = resourceReads.Count;
                for (int i = 0; i < resourceReadsCount; ++i)
                    generator.Append(resourceReads[i].index);

                var resourceWrites = resourceWriteLists[resType];
                var resourceWritesCount = resourceWrites.Count;
                for (int i = 0; i < resourceWritesCount; ++i)
                    generator.Append(resourceWrites[i].index);

                var resourceTransient = transientResourceList[resType];
                var resourceTransientCount = resourceTransient.Count;
                for (int i = 0; i < resourceTransientCount; ++i)
                    generator.Append(resourceTransient[i].index);
            }

            var usedRendererListListCount = usedRendererListList.Count;
            for (int i = 0; i < usedRendererListListCount; ++i)
                generator.Append(usedRendererListList[i].handle);

            var setGlobalsListCount = setGlobalsList.Count;
            for (int i = 0; i < setGlobalsListCount; ++i)
            {
                var global = setGlobalsList[i];
                generator.Append(global.Item1.handle.index);
                generator.Append(global.Item2);
            }
            generator.Append(useAllGlobalTextures);

            var implicitReadsListCount = implicitReadsList.Count;
            for (int i = 0; i < implicitReadsListCount; ++i)
                generator.Append(implicitReadsList[i].index);

            generator.Append(GetRenderFuncHash());
        }

        public void SetShadingRateImage(in TextureHandle shadingRateImage, AccessFlags accessFlags, int mipLevel, int depthSlice)
        {
            if (ShadingRateInfo.supportsPerImageTile)
            {
                hasShadingRateImage = true;
                shadingRateAccess = new TextureAccess(shadingRateImage, accessFlags, mipLevel, depthSlice);
                AddResourceRead(shadingRateAccess.textureHandle.handle);
            }
        }

        public void SetShadingRateFragmentSize(ShadingRateFragmentSize shadingRateFragmentSize)
        {
            if (ShadingRateInfo.supportsPerDrawCall)
            {
                hasShadingRateStates = true;
                this.shadingRateFragmentSize = shadingRateFragmentSize;
            }
        }

        public void SetShadingRateCombiner(ShadingRateCombinerStage stage, ShadingRateCombiner combiner)
        {
            if (ShadingRateInfo.supportsPerImageTile)
            {
                switch (stage)
                {
                    case ShadingRateCombinerStage.Primitive:
                        hasShadingRateStates = true;
                        primitiveShadingRateCombiner = combiner;
                        break;

                    case ShadingRateCombinerStage.Fragment:
                        hasShadingRateStates = true;
                        fragmentShadingRateCombiner = combiner;
                        break;
                }
            }
        }
    }

    // This used to have an extra generic argument 'RenderGraphContext' abstracting the context and avoiding
    // the RenderGraphPass/ComputeRenderGraphPass/RasterRenderGraphPass/UnsafeRenderGraphPass classes below
    // but this confuses IL2CPP and causes garbage when boxing the context created (even though they are structs)
    [DebuggerDisplay("RenderPass: {name} (Index:{index} Async:{enableAsyncCompute})")]
    internal abstract class BaseRenderGraphPass<PassData, TRenderGraphContext> : RenderGraphPass
        where PassData : class, new()
    {
        internal PassData data;
        internal BaseRenderFunc<PassData, TRenderGraphContext> renderFunc;

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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void Release(RenderGraphObjectPool pool)
        {
            pool.Release(data);
            data = null;
            renderFunc = null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool HasRenderFunc()
        {
            return renderFunc != null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetRenderFuncHash()
        {
            return renderFunc != null ? DelegateHashCodeUtils.GetFuncHashCode(renderFunc) : 0;
        }
    }

    [DebuggerDisplay("RenderPass: {name} (Index:{index} Async:{enableAsyncCompute})")]
    internal sealed class RenderGraphPass<PassData> : BaseRenderGraphPass<PassData, RenderGraphContext>
        where PassData : class, new()
    {
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
            base.Release(pool);

            // We need to do the release from here because we need the final type.
            pool.Release(this);
        }
    }

    [DebuggerDisplay("RenderPass: {name} (Index:{index} Async:{enableAsyncCompute})")]
    internal sealed class ComputeRenderGraphPass<PassData> : BaseRenderGraphPass<PassData, ComputeGraphContext>
    where PassData : class, new()
    {
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
            base.Release(pool);

            // We need to do the release from here because we need the final type.
            pool.Release(this);
        }
    }

    [DebuggerDisplay("RenderPass: {name} (Index:{index} Async:{enableAsyncCompute})")]
    internal sealed class RasterRenderGraphPass<PassData> : BaseRenderGraphPass<PassData, RasterGraphContext>
    where PassData : class, new()
    {
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
            base.Release(pool);

            // We need to do the release from here because we need the final type.
            pool.Release(this);
        }
    }

    [DebuggerDisplay("RenderPass: {name} (Index:{index} Async:{enableAsyncCompute})")]
    internal sealed class UnsafeRenderGraphPass<PassData> : BaseRenderGraphPass<PassData, UnsafeGraphContext>
        where PassData : class, new()
    {
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
            base.Release(pool);

            // We need to do the release from here because we need the final type.
            pool.Release(this);
        }
    }
}
