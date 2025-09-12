using System;
using System.Diagnostics;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Rendering;

namespace UnityEngine.Rendering.RenderGraphModule.NativeRenderPassCompiler
{
    internal partial class NativePassCompiler : IDisposable
    {
        internal struct RenderGraphInputInfo
        {
            public RenderGraphResourceRegistry m_ResourcesForDebugOnly;
            public List<RenderGraphPass> m_RenderPasses;
            public string debugName;
            public bool disablePassCulling;
            public bool disablePassMerging;
            public RenderTextureUVOriginStrategy renderTextureUVOriginStrategy;
        }

        internal RenderGraphInputInfo graph;
        internal CompilerContextData contextData = null;
        internal CompilerContextData defaultContextData;
        internal CommandBuffer previousCommandBuffer;
        Stack<int> toVisitPassIds;

        RenderGraphCompilationCache m_CompilationCache;

        RenderTargetIdentifier[][] m_TempMRTArrays = null;

        internal const int k_EstimatedPassCount = 100;
        internal const int k_MaxSubpass = 8; // Needs to match with RenderPassSetup.h

        NativeList<AttachmentDescriptor> m_BeginRenderPassAttachments;

        internal static bool s_ForceGenerateAuditsForTests = false;

        public NativePassCompiler(RenderGraphCompilationCache cache)
        {
            m_CompilationCache = cache;
            defaultContextData = new CompilerContextData();
            toVisitPassIds = new Stack<int>(k_EstimatedPassCount);

            m_TempMRTArrays = new RenderTargetIdentifier[RenderGraph.kMaxMRTCount][];
            for (int i = 0; i < RenderGraph.kMaxMRTCount; ++i)
                m_TempMRTArrays[i] = new RenderTargetIdentifier[i + 1];
        }

        // IDisposable implementation

        ~NativePassCompiler() => Cleanup();

        public void Dispose()
        {
            Cleanup();
            GC.SuppressFinalize(this);
        }

        public void Cleanup()
        {
            // If caching enabled, the two can be different
            contextData?.Dispose();
            defaultContextData?.Dispose();

            if (m_BeginRenderPassAttachments.IsCreated)
            {
                m_BeginRenderPassAttachments.Dispose();
            }
        }

        public bool Initialize(RenderGraphResourceRegistry resources, List<RenderGraphPass> renderPasses, RenderGraphDebugParams debugParams, string debugName, bool useCompilationCaching,
            int graphHash, int frameIndex, RenderTextureUVOriginStrategy renderTextureUVOriginStrategy)
        {
            bool cached = false;
            if (!useCompilationCaching)
                contextData = defaultContextData;
            else
                cached = m_CompilationCache.GetCompilationCache(graphHash, frameIndex, out contextData);

            graph.m_ResourcesForDebugOnly = resources;
            graph.m_RenderPasses = renderPasses;
            graph.disablePassCulling = debugParams.disablePassCulling;
            graph.disablePassMerging = debugParams.disablePassMerging;
            graph.debugName = debugName;
            graph.renderTextureUVOriginStrategy = renderTextureUVOriginStrategy;

            Clear(clearContextData: !useCompilationCaching);

            return cached;
        }

        void HandleExtendedFeatureFlags()
        {
            for (int nativePassIndex = 0; nativePassIndex < contextData.nativePassData.Length; nativePassIndex++)
            {
                int firstNativeSubPass = contextData.nativePassData[nativePassIndex].firstNativeSubPass;
                // Does this native pass have any sub passes.
                if (firstNativeSubPass >= 0)
                {
                    int firstGraphPass = contextData.nativePassData[nativePassIndex].firstGraphPass;
                    int graphPassIndex = 0;
                    for (int nativeSubPassIndex = 0; nativeSubPassIndex < contextData.nativePassData[nativePassIndex].numNativeSubPasses; nativeSubPassIndex++)
                    {
                        // Start with the MVPVV compatible flag set so that it can be used for the & operation later
                        SubPassFlags extendedSubPassFlags = SubPassFlags.MultiviewRenderRegionsCompatible;
                        // Iterate over all graph passes that got merged into this sub pass
                        while ((graphPassIndex < contextData.nativePassData[nativePassIndex].numGraphPasses) && (contextData.passData[graphPassIndex + firstGraphPass].nativeSubPassIndex == nativeSubPassIndex))
                        {
                            if (contextData.passData[graphPassIndex + firstGraphPass].extendedFeatureFlags.HasFlag(ExtendedFeatureFlags.TileProperties))
                            {
                                extendedSubPassFlags |= SubPassFlags.TileProperties;
                            }
                            // A native sub pass is MultiviewRenderRegionsCompatible only if all of its graph passes are compatible
                            if (!contextData.passData[graphPassIndex + firstGraphPass].extendedFeatureFlags.HasFlag(ExtendedFeatureFlags.MultiviewRenderRegionsCompatible))
                            {
                                extendedSubPassFlags &= ~SubPassFlags.MultiviewRenderRegionsCompatible;
                            }
                            graphPassIndex++;
                        }
                        contextData.nativeSubPassData.ElementAt(firstNativeSubPass + nativeSubPassIndex).flags |= extendedSubPassFlags;
                    }
                }
            }
        }

        public void Compile(RenderGraphResourceRegistry resources)
        {
            ValidatePasses();

            SetupContextData(resources);

            BuildGraph();

            CullUnusedRenderPasses();

            TryMergeNativePasses();

            HandleExtendedFeatureFlags();

            FindResourceUsageRanges();

            DetectMemoryLessResources();

            PrepareNativeRenderPasses();

            if (graph.renderTextureUVOriginStrategy == RenderTextureUVOriginStrategy.PropagateAttachmentOrientation)
                PropagateTextureUVOrigin();
        }

        public void Clear(bool clearContextData)
        {
            if (clearContextData)
                contextData.Clear();
            toVisitPassIds.Clear();
        }

        void SetPassStatesForNativePass(int nativePassId)
        {
            NativePassData.SetPassStatesForNativePass(contextData, nativePassId);
        }

        internal enum NativeCompilerProfileId
        {
            NRPRGComp_PrepareNativePass,
            NRPRGComp_SetupContextData,
            NRPRGComp_BuildGraph,
            NRPRGComp_CullNodes,
            NRPRGComp_TryMergeNativePasses,
            NRPRGComp_FindResourceUsageRanges,
            NRPRGComp_DetectMemorylessResources,
            NRPRGComp_PropagateTextureUVOrigin,
            NRPRGComp_ExecuteInitializeResources,
            NRPRGComp_ExecuteBeginRenderpassCommand,
            NRPRGComp_ExecuteDestroyResources,
        }

        [Conditional("DEVELOPMENT_BUILD"), Conditional("UNITY_EDITOR")]
        void ValidatePasses()
        {
            if (RenderGraph.enableValidityChecks)
            {
                int tilePropertiesPassIndex = -1;
                for (int passId = 0; passId < graph.m_RenderPasses.Count; passId++)
                {
                    if (graph.m_RenderPasses[passId].extendedFeatureFlags.HasFlag(ExtendedFeatureFlags.TileProperties))
                    {
                        if (tilePropertiesPassIndex > -1)
                        {
                            throw new Exception($"ExtendedFeatureFlags.TileProperties can only be set once per render graph (render graph {graph.debugName}, pass {graph.m_RenderPasses[passId].name}), previously set at (pass {graph.m_RenderPasses[tilePropertiesPassIndex].name}).");
                        }
                        tilePropertiesPassIndex = passId;
                    }
                }
            }
        }

        void SetupContextData(RenderGraphResourceRegistry resources)
        {
            using (new ProfilingScope(ProfilingSampler.Get(NativeCompilerProfileId.NRPRGComp_SetupContextData)))
            {
                contextData.Initialize(resources, k_EstimatedPassCount);
            }
        }

        // Returns true if the RasterFragmentList is successfully set up
        bool TrySetupRasterFragmentList(ref PassData ctxPass, ref RenderGraphPass inputPass, out string errorMessage)
        {
            errorMessage = null;
            var ctx = contextData;
            // Grab offset in context fragment list to begin building the fragment list
            ctxPass.firstFragment = ctx.fragmentData.Length;

            // Depth attachment is always at index 0
            if (inputPass.depthAccess.textureHandle.handle.IsValid())
            {
                ctxPass.fragmentInfoHasDepth = true;

                if (ctx.TryAddToFragmentList(inputPass.depthAccess, ctxPass.firstFragment, ctxPass.numFragments, out errorMessage))
                {
                    ctxPass.TryAddFragment(inputPass.depthAccess.textureHandle.handle, ctx, out errorMessage);
                }

                if (errorMessage != null)
                {
                    errorMessage =
                        $"when trying to add depth attachment of type {inputPass.depthAccess.textureHandle.handle.type} at index {inputPass.depthAccess.textureHandle.handle.index} - {errorMessage}";
                    return false;
                }
            }

            for (var ci = 0; ci < inputPass.colorBufferMaxIndex + 1; ++ci)
            {
                // Skip unused color slots
                if (!inputPass.colorBufferAccess[ci].textureHandle.handle.IsValid()) continue;

                if (ctx.TryAddToFragmentList(inputPass.colorBufferAccess[ci], ctxPass.firstFragment, ctxPass.numFragments, out errorMessage))
                {
                    ctxPass.TryAddFragment(inputPass.colorBufferAccess[ci].textureHandle.handle, ctx, out errorMessage);
                }

                if (errorMessage != null)
                {
                    errorMessage =
                        $"when trying to add render attachment of type {inputPass.colorBufferAccess[ci].textureHandle.handle.type} at index {inputPass.colorBufferAccess[ci].textureHandle.handle.index} - {errorMessage}";
                    return false;
                }
            }

            // shading rate
            if (inputPass.hasShadingRateImage &&
                inputPass.shadingRateAccess.textureHandle.handle.IsValid())
            {
                ctxPass.shadingRateImageIndex = ctx.fragmentData.Length;
                ctx.TryAddToFragmentList(inputPass.shadingRateAccess, ctxPass.shadingRateImageIndex, 0, out errorMessage);

                if (errorMessage != null)
                {
                    errorMessage = $"when trying to add VRS attachment of type {inputPass.shadingRateAccess.textureHandle.handle.type} at index {inputPass.shadingRateAccess.textureHandle.handle.index} - {errorMessage}";
                    return false;
                }
            }

            // Grab offset in context fragment list to begin building the fragment input list
            ctxPass.firstFragmentInput = ctx.fragmentData.Length;

            for (var ci = 0; ci < inputPass.fragmentInputMaxIndex + 1; ++ci)
            {
                // Skip unused fragment input slots
                if (!inputPass.fragmentInputAccess[ci].textureHandle.IsValid()) continue;

                var resource = inputPass.fragmentInputAccess[ci].textureHandle;
                if (ctx.TryAddToFragmentList(inputPass.fragmentInputAccess[ci], ctxPass.firstFragmentInput,
                        ctxPass.numFragmentInputs, out errorMessage))
                {
                    ctxPass.TryAddFragmentInput(inputPass.fragmentInputAccess[ci].textureHandle.handle, ctx, out errorMessage);
                }

                if (errorMessage != null)
                {
                    errorMessage =
                        $"when trying to add input attachment of type {inputPass.fragmentInputAccess[ci].textureHandle.handle.type} at index {inputPass.fragmentInputAccess[ci].textureHandle.handle.index} - {errorMessage}";
                    return false;
                }
            }

            // Grab offset in context random write list to begin building the per pass random write lists
            ctxPass.firstRandomAccessResource = ctx.randomAccessResourceData.Length;

            for (var ci = 0; ci < inputPass.randomAccessResourceMaxIndex + 1; ++ci)
            {
                ref var uav = ref inputPass.randomAccessResource[ci];

                // Skip unused random write slots
                if (!uav.h.IsValid()) continue;

                if (ctx.TryAddToRandomAccessResourceList(uav.h, ci, uav.preserveCounterValue,
                        ctxPass.firstRandomAccessResource, ctxPass.numRandomAccessResources, out errorMessage))
                {
                    ctxPass.AddRandomAccessResource();
                }

                if (errorMessage != null)
                {
                    errorMessage = $"when trying to add random access attachment of type {uav.h.type} at index {uav.h.index} - {errorMessage}";
                    return false;
                }
            }

            // This is suspicious, there are frame buffer fetch inputs but nothing is output. We don't allow this for now.
            // In theory you could fb-fetch inputs and write something to a uav and output nothing? This needs to be investigated
            // so don't allow it for now.
            if (ctxPass.numFragments == 0)
            {
                Debug.Assert(ctxPass.numFragmentInputs == 0);
            }

            return true;
        }

        void BuildGraph()
        {
            var ctx = contextData;
            List<RenderGraphPass> passes = graph.m_RenderPasses;

            // Not clearing data, we will do it right after in the for loop
            // This is to prevent unnecessary costly copies of pass struct (128bytes)
            ctx.passData.ResizeUninitialized(passes.Count);

            // Build up the context graph and keep track of nodes we encounter that can't be culled
            using (new ProfilingScope(ProfilingSampler.Get(NativeCompilerProfileId.NRPRGComp_BuildGraph)))
            {
                for (int passId = 0; passId < passes.Count; passId++)
                {
                    var inputPass = passes[passId];

#if DEVELOPMENT_BUILD || UNITY_EDITOR
                    if (inputPass.type == RenderGraphPassType.Legacy)
                    {
                        throw new Exception(RenderGraph.RenderGraphExceptionMessages.UsingLegacyRenderGraph(inputPass.name));
                    }
#endif

                    // Accessing already existing passData in place in the container through reference to avoid deep copy
                    // Make sure everything is reset and initialized or we will use obsolete data from previous frame
                    ref var ctxPass = ref ctx.passData.ElementAt(passId);
                    ctxPass.ResetAndInitialize(inputPass, passId);

                    ctx.passNames.Add(new Name(inputPass.name, true));

                    if (ctxPass.hasSideEffects)
                    {
                        toVisitPassIds.Push(passId);
                    }

                    // Set up the list of fragment attachments for this pass
                    // Note: This doesn't set up the resource reader/writer list as the fragment attachments
                    // will also be in the pass read/write lists accordingly
                    if (ctxPass.type == RenderGraphPassType.Raster)
                    {
                        if (!TrySetupRasterFragmentList(ref ctxPass, ref inputPass, out var errorMessage))
                        {
                            throw new Exception($"In pass '{inputPass.name}', {errorMessage}");
                        }
                    }

                    // Set up per resource type read/write lists for this pass
                    ctxPass.firstInput = ctx.inputData.Length; // Grab offset in context input list
                    ctxPass.firstOutput = ctx.outputData.Length; // Grab offset in context output list
                    for (int type = 0; type < (int)RenderGraphResourceType.Count; ++type)
                    {
                        var resourceWrite = inputPass.resourceWriteLists[type];
                        var resourceWriteCount = resourceWrite.Count;
                        for (var i = 0; i < resourceWriteCount; ++i)
                        {
                            var resource = resourceWrite[i];

                            // Writing to an imported resource is a side effect so mark the pass if needed
                            ref var resData = ref ctx.UnversionedResourceData(resource);
                            if (resData.isImported)
                            {
                                if (!ctxPass.hasSideEffects)
                                {
                                    ctxPass.hasSideEffects = true;
                                    toVisitPassIds.Push(passId);
                                }
                            }

                            // Mark this pass as writing to this version of the resource
                            ctx.resources[resource].SetWritingPass(ctx, resource, passId);

                            ctx.outputData.Add(new PassOutputData(resource));

                            ctxPass.numOutputs++;
                        }

                        var resourceRead = inputPass.resourceReadLists[type];
                        var resourceReadCount = resourceRead.Count;
                        for (var i = 0; i < resourceReadCount; ++i)
                        {
                            var resource = resourceRead[i];

                            // Mark this pass as reading from this version of the resource
                            ctx.resources[resource].RegisterReadingPass(ctx, resource, passId, ctxPass.numInputs);

                            ctx.inputData.Add(new PassInputData(resource));

                            ctxPass.numInputs++;
                        }

                        var resourceTrans = inputPass.transientResourceList[type];
                        var resourceTransCount = resourceTrans.Count;

                        for (var i = 0; i < resourceTransCount; ++i)
                        {
                            var resource = resourceTrans[i];

                            // Mark this pass as reading from this version of the resource
                            ctx.resources[resource].RegisterReadingPass(ctx, resource, passId, ctxPass.numInputs);

                            ctx.inputData.Add(new PassInputData(resource));

                            ctxPass.numInputs++;

                            // Mark this pass as writing to this version of the resource
                            ctx.resources[resource].SetWritingPass(ctx, resource, passId);

                            ctx.outputData.Add(new PassOutputData(resource));

                            ctxPass.numOutputs++;
                        }
                    }
                }
            }
        }

        void CullUnusedRenderPasses()
        {
            var ctx = contextData;

            // Source = input of the graph and starting point that takes no inputs itself : e.g. z-prepass
            // Sink = output of the graph and end point e.g. rendered frame
            // Usually sinks will have to be pinned or write to an external resource or the whole graph would get culled :-)

            using (new ProfilingScope(ProfilingSampler.Get(NativeCompilerProfileId.NRPRGComp_CullNodes)))
            {
                // No need to go further if we don't enable culling
                if (graph.disablePassCulling)
                    return;

                // Cull all passes first
                ctx.CullAllPasses(true);

                // Flood fill downstream algorithm using BFS,
                // starting from the pinned nodes to all their dependencies
                while (toVisitPassIds.Count != 0)
                {
                    int passId = toVisitPassIds.Pop();

                    ref var passData = ref ctx.passData.ElementAt(passId);

                    // We already found this node through another dependency chain
                    if (!passData.culled) continue;

                    // Flow upstream from this node
                    foreach (ref readonly var input in passData.Inputs(ctx))
                    {
                        int inputPassIndex = ctx.resources[input.resource].writePassId;
                        toVisitPassIds.Push(inputPassIndex);
                    }

                    // We need this node, don't cull it
                    passData.culled = false;
                }

                // Update graph based on freshly culled nodes, remove any connection to them
                // We start from the latest passes to the first ones as we might need to decrement the version number of unwritten resources
                var numPasses = ctx.passData.Length;
                for (int passIndex = numPasses - 1; passIndex >= 0; passIndex--)
                {
                    ref readonly var pass = ref ctx.passData.ElementAt(passIndex);

                    // Remove the connections from the list so they won't be visited again
                    if (pass.culled)
                    {
                        // If the culled pass was supposed to generate the latest version of a given resource,
                        // we need to decrement the latestVersionNumber of this resource
                        // because its last version will never be created due to its producer being culled
                        foreach (ref readonly var output in pass.Outputs(ctx))
                        {
                            var outputResource = output.resource;
                            bool isOutputLastVersion = (outputResource.version == ctx.UnversionedResourceData(outputResource).latestVersionNumber);

                            if (isOutputLastVersion)
                                ctx.UnversionedResourceData(outputResource).latestVersionNumber--;
                        }

                        // Notifying the versioned resources that this pass is no longer reading them
                        foreach (ref readonly var input in pass.Inputs(ctx))
                        {
                            var inputResource = input.resource;
                            ctx.resources[inputResource].RemoveReadingPass(ctx, inputResource, pass.passId);
                        }
                    }
                }
            }
        }

        void TryMergeNativePasses()
        {
            var ctx = contextData;

            using (new ProfilingScope(ProfilingSampler.Get(NativeCompilerProfileId.NRPRGComp_TryMergeNativePasses)))
            {
                // Try to merge raster passes into the currently active native pass. This will not do pass reordering yet
                // so it is simply greedy trying to add the next pass to a currently active one.
                // In the future we want to try adding any available (i.e. that has no data dependencies on any future results) future pass
                // and allow merging that into the pass thus greedily reordering passes. But reordering requires a lot of API validation
                // that ensures rendering behaves accordingly with reordered passes so we don't allow that for now.

                // !!! Compilation caching warning !!!
                // Merging of passes is highly dependent on render texture properties.
                // When caching the render graph compilation, we hash a subset of those render texture properties to make sure we recompile the graph if needed.
                // We only hash a subset for performance reason so if you add logic here that will change the behavior of pass merging,
                // make sure that the relevant properties are hashed properly. See RenderGraphPass.ComputeHash()

                int activeNativePassId = -1;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                bool generatePassBreakAudits = RenderGraphDebugSession.hasActiveDebugSession || s_ForceGenerateAuditsForTests;
#endif

                for (var passIdx = 0; passIdx < ctx.passData.Length; ++passIdx)
                {
                    ref var passToAdd = ref ctx.passData.ElementAt(passIdx);

                    // If the pass has been culled, just ignore it
                    if (passToAdd.culled)
                    {
                        continue;
                    }

                    // If no active pass, there is nothing to merge...
                    if (activeNativePassId == -1)
                    {
                        //If raster, start a new native pass with the current pass
                        if (passToAdd.type == RenderGraphPassType.Raster)
                        {
                            // Allocate a stand-alone native renderpass based on the current pass
                            ctx.nativePassData.Add(new NativePassData(ref passToAdd, ctx));
                            passToAdd.nativePassIndex = ctx.nativePassData.LastIndex();
                            activeNativePassId = passToAdd.nativePassIndex;
                        }
                    }
                    // There is an native pass currently open, try to add the current graph pass to it
                    else
                    {
                        var mergeTestResult = graph.disablePassMerging ? new PassBreakAudit(PassBreakReason.PassMergingDisabled, passIdx)
                                                                       : NativePassData.TryMerge(contextData, activeNativePassId, passIdx);

                        // Merge failed, close current native render pass and create a new one
                        if (mergeTestResult.reason != PassBreakReason.Merged)
                        {
                            SetPassStatesForNativePass(activeNativePassId);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                            if (generatePassBreakAudits)
                            {
                                ref var nativePassData = ref contextData.nativePassData.ElementAt(activeNativePassId);
                                nativePassData.breakAudit = mergeTestResult;
                            }
#endif
                            if (mergeTestResult.reason == PassBreakReason.NonRasterPass)
                            {
                                // Non-raster pass, no active native pass at all
                                activeNativePassId = -1;
                            }
                            else
                            {
                                // Raster but cannot be merged, allocate a new stand-alone native renderpass based on the current pass
                                ctx.nativePassData.Add(new NativePassData(ref passToAdd, ctx));
                                passToAdd.nativePassIndex = ctx.nativePassData.LastIndex();
                                activeNativePassId = passToAdd.nativePassIndex;
                            }
                        }
                    }
                }

                if (activeNativePassId >= 0)
                {
                    // "Close" the last native pass by marking the last graph pass as end
                    SetPassStatesForNativePass(activeNativePassId);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    if (generatePassBreakAudits)
                    {
                        ref var nativePassData = ref contextData.nativePassData.ElementAt(activeNativePassId);
                        nativePassData.breakAudit = new PassBreakAudit(PassBreakReason.EndOfGraph, -1);
                    }
#endif
                }
            }
        }

        void FindResourceUsageRanges()
        {
            var ctx = contextData;

            using (new ProfilingScope(ProfilingSampler.Get(NativeCompilerProfileId.NRPRGComp_FindResourceUsageRanges)))
            {
                // Find the passes that last use a resource
                // First do a forward-pass increasing the refcount
                // followed by another forward-pass decreasing it again (-> if we get 0, we must be the last ones using it)

                // TODO: I have a feeling this can be done more optimally by walking the graph instead of looping over all the passes twice
                // to be investigated
                // It also won't work if we start reordering passes.
                for (int passIndex = 0; passIndex < ctx.passData.Length; passIndex++)
                {
                    ref var pass = ref ctx.passData.ElementAt(passIndex);
                    if (pass.culled)
                        continue;

                    // Loop over all the resources this pass needs (=inputs)
                    foreach (ref readonly var input in pass.Inputs(ctx))
                    {
                        var inputResource = input.resource;
                        ref var pointTo = ref ctx.UnversionedResourceData(inputResource);
                        pointTo.lastUsePassID = -1;

                        // If we use version 0 and nobody else is using it yet,
                        // mark this pass as the first using the resource.
                        // It can happen that two passes use v0, e.g.:
                        // pass1.UseTex(v0,Read) -> this will clear the pass but keep it at v0
                        // pass2.UseTex(v0,Read) -> "reads" v0
                        if (inputResource.version == 0)
                        {
                            if (pointTo.firstUsePassID < 0)
                            {
                                pointTo.firstUsePassID = pass.passId;
                                pass.AddFirstUse(inputResource, ctx);
                            }
                        }

                        // This pass uses the last version of a resource increase the ref count of this resource
                        var last = pointTo.latestVersionNumber;
                        if (last == inputResource.version)
                        {
                            pointTo.tag++; //Refcount of how many passes are using the last version of a resource
                        }
                    }

                    //Also look at outputs (but with version 1) for edge case were we do a Write (but no read) to a texture and the pass is manually excluded from culling
                    //As it isn't read it won't be in the inputs array with V0

                    foreach (ref readonly var output in pass.Outputs(ctx))
                    {
                        var outputResource = output.resource;
                        ref var pointTo = ref ctx.UnversionedResourceData(outputResource);
                        if (outputResource.version == 1)
                        {
                            // If we use version 0 and nobody else is using it yet,
                            // Mark this pass as the first using the resource.
                            // It can happen that two passes use v0, e.g.:
                            // pass1.UseTex(v0,Read) -> this will clear the pass but keep it at v0
                            // pass3.UseTex(v0,Read/Write) -> wites v0, brings it to v1 from here on
                            if (pointTo.firstUsePassID < 0)
                            {
                                pointTo.firstUsePassID = pass.passId;
                                pass.AddFirstUse(outputResource, ctx);
                            }
                        }

                        // This pass outputs the last version of a resource track that
                        var last = pointTo.latestVersionNumber;
                        if (last == outputResource.version)
                        {
                            Debug.Assert(pointTo.lastWritePassID == -1); // Only one can be the last writer
                            pointTo.lastWritePassID = pass.passId;
                        }
                    }
                }

                for (int passIndex = 0; passIndex < ctx.passData.Length; passIndex++)
                {
                    ref var pass = ref ctx.passData.ElementAt(passIndex);
                    if (pass.culled)
                        continue;

                    pass.waitOnGraphicsFencePassId = -1;
                    pass.insertGraphicsFence = false;

                    foreach (ref readonly var input in pass.Inputs(ctx))
                    {
                        var inputResource = input.resource;
                        ref var pointTo = ref ctx.UnversionedResourceData(inputResource);
                        var last = pointTo.latestVersionNumber;
                        if (last == inputResource.version)
                        {
                            var refC = pointTo.tag - 1; //Decrease refcount this pass is done using it
                            if (refC == 0) // We're the last pass done using it, this pass should destroy it.
                            {
                                pointTo.lastUsePassID = pass.passId;
                                pass.AddLastUse(inputResource, ctx);
                            }

                            pointTo.tag = refC;
                        }

                        // Resolve if this pass needs to wait on a fence due to its inputs
                        if (pass.waitOnGraphicsFencePassId == -1)
                        {
                            ref var pointToVer = ref ctx.VersionedResourceData(inputResource);
                            // If no RG pass writes to the resource, no need to wait for anyone
                            if (pointToVer.written)
                            {
                                ref var wPass = ref ctx.passData.ElementAt(pointToVer.writePassId);
                                if (wPass.asyncCompute != pass.asyncCompute)
                                {
                                    pass.waitOnGraphicsFencePassId = wPass.passId;
                                }
                            }
                        }
                    }

                    // We're outputting a resource that is never used.
                    // This can happen if this pass has multiple outputs and only a portion of them are used
                    // as some are used, the whole pass is not culled but the unused output still should be freed
                    foreach (ref readonly var output in pass.Outputs(ctx))
                    {
                        var outputResource = output.resource;
                        ref var pointTo = ref ctx.UnversionedResourceData(outputResource);
                        ref var pointToVer = ref ctx.VersionedResourceData(outputResource);
                        var last = pointTo.latestVersionNumber;
                        if (last == outputResource.version && pointToVer.numReaders == 0)
                        {
                            pointTo.lastUsePassID = pass.passId;
                            pass.AddLastUse(outputResource, ctx);
                        }

                        // Resolve if this pass should insert a fence for its outputs
                        var numReaders = pointToVer.numReaders;
                        for (var i = 0; i < numReaders; ++i)
                        {
                            var depIdx = ctx.resources.IndexReader(outputResource, i);
                            ref var dep = ref ctx.resources.readerData[outputResource.iType].ElementAt(depIdx);
                            ref var depPass = ref ctx.passData.ElementAt(dep.passId);
                            if (pass.asyncCompute != depPass.asyncCompute)
                            {
                                pass.insertGraphicsFence = true;
                                break;
                            }
                        }
                    }
                }
            }
        }

        void PrepareNativeRenderPasses()
        {
            // Prepare all native render pass execution info:
            for (var passIdx = 0; passIdx < contextData.nativePassData.Length; ++passIdx)
            {
                ref var nativePassData = ref contextData.nativePassData.ElementAt(passIdx);
                DetermineLoadStoreActions(ref nativePassData);
            }
        }

        void PropagateTextureUVOrigin()
        {
            using (new ProfilingScope(ProfilingSampler.Get(NativeCompilerProfileId.NRPRGComp_PropagateTextureUVOrigin)))
            {
                // Work backwards through the native pass list and propagate the texture uv origin we store with to
                // any texture attachments that are not explicitly known (usually intermediate memoryless attachments).
                for (int passIdx = contextData.nativePassData.Length - 1; passIdx >= 0; --passIdx)
                {
                    ref NativePassData nativePassData = ref contextData.nativePassData.ElementAt(passIdx);

                    // Find a texture attachment that is storing to find out the orientation for this pass.
                    int attachmentsCount = nativePassData.attachments.size;
                    int firstStoreAttachmentIndex = 0;
                    TextureUVOriginSelection storeUVOrigin = TextureUVOriginSelection.Unknown;
                    for (int attIdx = 0; attIdx < attachmentsCount; ++attIdx)
                    {
                        ref NativePassAttachment nativePassAttachment = ref nativePassData.attachments[attIdx];
                        if (nativePassAttachment.storeAction != RenderBufferStoreAction.DontCare)
                        {
                            if (nativePassAttachment.handle.type == RenderGraphResourceType.Texture) // Only textures have orientation
                            {
                                ref ResourceUnversionedData resData = ref contextData.UnversionedResourceData(nativePassAttachment.handle);
                                storeUVOrigin = resData.textureUVOrigin; // Inherit the orientation of the store if we are currently storing to an unknown orientation.
                                firstStoreAttachmentIndex = attIdx;
                                break;
                            }
                        }
                    }

                    // Update any texture attachments with an unknown uv origin to the one we are going to use for storing and validate
                    // we don't have a mixture of uv origins on the texture attachment list as this would mean something is going to be
                    // read/written upside down.
                    for (int attIdx = 0; attIdx < attachmentsCount; ++attIdx)
                    {
                        ref NativePassAttachment nativePassAttachment = ref nativePassData.attachments[attIdx];

                        if (nativePassAttachment.handle.type == RenderGraphResourceType.Texture)
                        {
                            ref ResourceUnversionedData resData = ref contextData.UnversionedResourceData(nativePassAttachment.handle);
                            if (storeUVOrigin != TextureUVOriginSelection.Unknown && resData.textureUVOrigin != TextureUVOriginSelection.Unknown && resData.textureUVOrigin != storeUVOrigin)
                            {
                                ref NativePassAttachment firstStoreNativePassAttachment = ref nativePassData.attachments[firstStoreAttachmentIndex];
                                var firstStoreAttachmentName = graph.m_ResourcesForDebugOnly.GetRenderGraphResourceName(firstStoreNativePassAttachment.handle);
                                var name = graph.m_ResourcesForDebugOnly.GetRenderGraphResourceName(nativePassAttachment.handle);

                                throw new InvalidOperationException($"From pass '{contextData.passNames[nativePassData.firstGraphPass]}' to pass '{contextData.passNames[nativePassData.lastGraphPass]}' when trying to store resource '{name}' of type {nativePassAttachment.handle.type} at index {nativePassAttachment.handle.index} - "
                                                                    + RenderGraph.RenderGraphExceptionMessages.IncompatibleTextureUVOriginStore(firstStoreAttachmentName, storeUVOrigin, name, resData.textureUVOrigin));
                            }

                            resData.textureUVOrigin = storeUVOrigin;
                        }
                    }
                }
            }
        }

        static bool IsGlobalTextureInPass(RenderGraphPass pass, ResourceHandle handle)
        {
            foreach (var g in pass.setGlobalsList)
            {
                if (g.Item1.handle.index == handle.index)
                {
                    return true;
                }
            }

            return false;
        }

        void DetectMemoryLessResources()
        {
            using (new ProfilingScope(ProfilingSampler.Get(NativeCompilerProfileId.NRPRGComp_DetectMemorylessResources)))
            {
                // No need to go further if we don't support memoryless textures
                if (!SystemInfo.supportsMemorylessTextures)
                    return;

                // Native renderpasses and create/destroy lists have now been set-up. Detect memoryless resources, i.e resources that are created/destroyed
                // within the scope of an nrp
                foreach (ref readonly var nativePass in contextData.NativePasses)
                {
                    // Loop over all created resources by this nrp
                    var graphPasses = nativePass.GraphPasses(contextData);
                    foreach (ref readonly var subPass in graphPasses)
                    {
                        foreach (ref readonly var createdRes in subPass.FirstUsedResources(contextData))
                        {
                            ref var createInfo = ref contextData.UnversionedResourceData(createdRes);
                            if (createdRes.type == RenderGraphResourceType.Texture && createInfo.isImported == false)
                            {
                                bool isGlobal = IsGlobalTextureInPass(graph.m_RenderPasses[subPass.passId], createdRes);

                                // Note: You could think but what if the texture is used as a regular non-frambuffer attachment texture
                                // surely it can't be memoryless then?
                                // That is true, but it can never happen as the fact these passes got merged means the textures cannot be used
                                // as regular textures halfway through a pass. If that were the case they would never have been merged in the first place.
                                // Except! If the pass consists of a single pass, in that case a texture could be allocated and freed within the single pass
                                // This is a somewhat degenerate case (e.g. a pass with culling forced off doing a uav write that is never used anywhere)
                                // But to avoid execution errors we still need to create the resource in this case.

                                // Check if it is in the destroy list of any of the subpasses > if yes > memoryless
                                foreach (ref readonly var subPass2 in graphPasses)
                                {
                                    foreach (ref readonly var destroyedRes in subPass2.LastUsedResources(contextData))
                                    {
                                        ref var destInfo = ref contextData.UnversionedResourceData(destroyedRes);
                                        if (destroyedRes.type == RenderGraphResourceType.Texture && destInfo.isImported == false)
                                        {
                                            if (createdRes.index == destroyedRes.index && !isGlobal)
                                            {
                                                // If a single pass in the native pass we need to check fragment attachment otherwise we're good
                                                // we could always check this in theory but it's an optimization not to check it.
                                                if (nativePass.numNativeSubPasses > 1 || subPass2.IsUsedAsFragment(createdRes, contextData))
                                                {
                                                    createInfo.memoryLess = true;
                                                    destInfo.memoryLess = true;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        internal static bool IsSameNativeSubPass(ref SubPassDescriptor a, ref SubPassDescriptor b)
        {
            const SubPassFlags k_SubPassMergeIgnoreMask = ~(SubPassFlags.TileProperties | SubPassFlags.MultiviewRenderRegionsCompatible);
            // Mask out the flags we can ignore.
            SubPassFlags aflags = a.flags & k_SubPassMergeIgnoreMask;
            SubPassFlags bflags = b.flags & k_SubPassMergeIgnoreMask;
            if (aflags != bflags
                || a.colorOutputs.Length != b.colorOutputs.Length
                || a.inputs.Length != b.inputs.Length)
            {
                return false;
            }

            for (int i = 0; i < a.colorOutputs.Length; i++)
            {
                if (a.colorOutputs[i] != b.colorOutputs[i])
                {
                    return false;
                }
            }

            for (int i = 0; i < a.inputs.Length; i++)
            {
                if (a.inputs[i] != b.inputs[i])
                {
                    return false;
                }
            }

            return true;
        }

        private void ExecuteInitializeResource(InternalRenderGraphContext rgContext, RenderGraphResourceRegistry resources, in PassData pass)
        {
            using (new ProfilingScope(ProfilingSampler.Get(NativeCompilerProfileId.NRPRGComp_ExecuteInitializeResources)))
            {
                resources.forceManualClearOfResource = true;

                // For raster passes we need to create resources for all the subpasses at the beginning of the native renderpass
                if (pass.type == RenderGraphPassType.Raster && pass.nativePassIndex >= 0)
                {
                    if (pass.mergeState == PassMergeState.Begin || pass.mergeState == PassMergeState.None)
                    {
                        ref var nativePass = ref contextData.nativePassData.ElementAt(pass.nativePassIndex);
                        foreach (ref readonly var subPass in nativePass.GraphPasses(contextData))
                        {
                            foreach (ref readonly var res in subPass.FirstUsedResources(contextData))
                            {
                                ref readonly var resInfo = ref contextData.UnversionedResourceData(res);

                                bool usedAsFragmentThisPass = subPass.IsUsedAsFragment(res, contextData);

                                // This resource is read for the first time as a regular texture and not as a framebuffer attachment
                                // so if requested we need to explicitly clear it, as loadAction.clear only works on framebuffer attachments
                                resources.forceManualClearOfResource = !usedAsFragmentThisPass;

                                if (!resInfo.isImported)
                                {
                                    // If the compiler has detected that this resource can be memoryless,
                                    // we need to update the texture descriptor that will be used to create the memoryless RTHandle.
                                    // Memoryless resources are created to allow implicit conversion from TextureHandle to RTHandle.
                                    // Such conversions can happen on users side when manipulating texture handles.
                                    if (resInfo.memoryLess)
                                    {
                                        resources.SetTextureAsMemoryLess(res);
                                    }

                                    // We create the resources from a pool
                                    // memoryless resources are also created but will not allocate in system memory
                                    resources.CreatePooledResource(rgContext, res.iType, res.index);
                                }
                                else // Imported resource
                                {
                                    if (resInfo.clear && !resInfo.memoryLess && resources.forceManualClearOfResource)
                                        resources.ClearResource(rgContext, res.iType, res.index);
                                }
                            }
                        }
                    }
                }
                // Other passes just create them at the beginning of the individual pass
                else
                {
                    foreach (ref readonly var create in pass.FirstUsedResources(contextData))
                    {
                        ref readonly var pointTo = ref contextData.UnversionedResourceData(create);
                        if (!pointTo.isImported)
                        {
                            resources.CreatePooledResource(rgContext, create.iType, create.index);
                        }
                        else // Imported resource
                        {
                            if (pointTo.clear)
                                resources.ClearResource(rgContext, create.iType, create.index);
                        }
                    }
                }

                resources.forceManualClearOfResource = true;
            }
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        static LoadAudit s_EmptyLoadAudit = new LoadAudit(LoadReason.InvalidReason);
        static StoreAudit s_EmptyStoreAudit = new StoreAudit(StoreReason.InvalidReason);
#endif

        void DetermineLoadStoreActions(ref NativePassData nativePass)
        {
            using (new ProfilingScope(ProfilingSampler.Get(NativeCompilerProfileId.NRPRGComp_PrepareNativePass)))
            {
                ref readonly var firstGraphPass = ref contextData.passData.ElementAt(nativePass.firstGraphPass);
                ref readonly var lastGraphPass = ref contextData.passData.ElementAt(nativePass.lastGraphPass);

                // Some passes don't do any rendering only state changes so just skip them
                // If these passes trigger any drawing the raster command buffer will warn users no render targets are set-up for their rendering
                if (nativePass.fragments.size <= 0)
                    return;

                // Some sanity checks, these should not happen
                Debug.Assert(firstGraphPass.mergeState is PassMergeState.Begin or PassMergeState.None);
                Debug.Assert(lastGraphPass.mergeState is PassMergeState.End or PassMergeState.None);

                ref readonly var fragmentList = ref nativePass.fragments;

                // determine load store actions
                // This pass also contains the latest versions used within this pass
                // As we have no pass reordering for now the merged passes are always a consecutive list and we can simply do a range
                // check on the create/destroy passid to see if it's allocated/freed in this native renderpass
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                bool generateAudits = RenderGraphDebugSession.hasActiveDebugSession || s_ForceGenerateAuditsForTests;
                ref var currLoadAudit = ref s_EmptyLoadAudit;
                ref var currStoreAudit = ref s_EmptyStoreAudit;
#endif

                for (int fragmentId = 0; fragmentId < fragmentList.size; ++fragmentId)
                {
                    ref readonly var fragment = ref fragmentList[fragmentId];

                    // Default values
                    ResourceHandle handle = fragment.resource;
                    bool memoryless = false;
                    int mipLevel = fragment.mipLevel;
                    int depthSlice = fragment.depthSlice;
                    // Don't care by default
                    RenderBufferLoadAction loadAction = RenderBufferLoadAction.DontCare;
                    RenderBufferStoreAction storeAction = RenderBufferStoreAction.DontCare;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    if (generateAudits)
                    {
                        nativePass.loadAudit.Add(new LoadAudit(LoadReason.FullyRewritten));
                        currLoadAudit = ref nativePass.loadAudit[nativePass.loadAudit.size - 1]; // Get the last added element

                        nativePass.storeAudit.Add(new StoreAudit(StoreReason.DiscardUnused));
                        currStoreAudit = ref nativePass.storeAudit[nativePass.storeAudit.size - 1]; // Similarly for storeAudit
                    }
#endif

                    // Writing by-default has to preserve the contents, think rendering only a few small triangles on top of a big framebuffer
                    // So it means we need to load/clear contents potentially.
                    // If a user pass knows it will write all pixels in a buffer (like a blit) it can use the WriteAll/Discard usage to indicate this to the graph
                    bool partialWrite = fragment.accessFlags.HasFlag(AccessFlags.Write)
                                        && !fragment.accessFlags.HasFlag(AccessFlags.Discard);

                    ref readonly var resourceData = ref contextData.UnversionedResourceData(fragment.resource);
                    bool isImported = resourceData.isImported;

                    int destroyPassID = resourceData.lastUsePassID;
                    bool usedAfterThisNativePass = (destroyPassID >= (nativePass.lastGraphPass + 1));

                    // Read or partial-write logic
                    if (fragment.accessFlags.HasFlag(AccessFlags.Read) || partialWrite)
                    {
                        // The resource is already allocated before this pass so we need to load it
                        if (resourceData.firstUsePassID < nativePass.firstGraphPass)
                        {
                            loadAction = RenderBufferLoadAction.Load;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                            if (generateAudits)
                                currLoadAudit = new LoadAudit(LoadReason.LoadPreviouslyWritten, resourceData.firstUsePassID);
#endif
                            // Once we decide to load a resource, we must default to the Store action if the resource is used after the current native pass.
                            // If we were to use the DontCare action in this case, the driver would be effectively be allowed to discard the
                            // contents of the resource. This is true even when we're only performing reads on it.
                            if (usedAfterThisNativePass)
                            {
                                storeAction = RenderBufferStoreAction.Store;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                                if (generateAudits)
                                    currStoreAudit = new StoreAudit(StoreReason.StoreUsedByLaterPass, destroyPassID);
#endif
                            }
                        }
                        // It's first used this native pass so we need to clear it so reads/partial writes return the correct clear value
                        // the clear colors are part of the resource description and set-up when executing the graph we don't need to care about that here.
                        else
                        {
                            if (isImported)
                            {
                                // Check if the user indicated he wanted clearing of his imported resource on it's first use by the graph
                                if (resourceData.clear)
                                {
                                    loadAction = RenderBufferLoadAction.Clear;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                                    if (generateAudits)
                                        currLoadAudit = new LoadAudit(LoadReason.ClearImported);
#endif
                                }
                                else
                                {
                                    loadAction = RenderBufferLoadAction.Load;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                                    if (generateAudits)
                                        currLoadAudit = new LoadAudit(LoadReason.LoadImported);
#endif
                                }
                            }
                            else
                            {
                                // Created by the graph internally clear on first read
                                loadAction = RenderBufferLoadAction.Clear;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                                if (generateAudits)
                                    currLoadAudit = new LoadAudit(LoadReason.ClearCreated);
#endif
                            }
                        }
                    }

                    // Write logic
                    if (fragment.accessFlags.HasFlag(AccessFlags.Write))
                    {
                        // Simple non-msaa case
                        if (nativePass.samples <= 1)
                        {
                            if (usedAfterThisNativePass)
                            {
                                // The resource is still used after this native pass so we need to store it.
                                storeAction = RenderBufferStoreAction.Store;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                                if (generateAudits)
                                    currStoreAudit = new StoreAudit(StoreReason.StoreUsedByLaterPass, destroyPassID);
#endif
                            }
                            else
                            {
                                // This is the last native pass that uses the resource.
                                // If it's imported, we store it because its contents may be used outside the graph.
                                // Otherwise, we can safely discard its contents.
                                //
                                // The one exception to this, is the user declared discard flag which allows us to assume an imported
                                // resource is not used outside the graph.
                                if (isImported)
                                {
                                    if (resourceData.discard)
                                    {
                                        storeAction = RenderBufferStoreAction.DontCare;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                                        if (generateAudits)
                                            currStoreAudit = new StoreAudit(StoreReason.DiscardImported);
#endif
                                    }
                                    else
                                    {
                                        storeAction = RenderBufferStoreAction.Store;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                                        if (generateAudits)
                                            currStoreAudit = new StoreAudit(StoreReason.StoreImported);
#endif
                                    }
                                }
                                else
                                {
                                    storeAction = RenderBufferStoreAction.DontCare;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                                    if (generateAudits)
                                        currStoreAudit = new StoreAudit(StoreReason.DiscardUnused);
#endif
                                }
                            }
                        }
                        // Complex msaa case
                        else
                        {
                            // The resource is still used after this native pass so we need to store it.
                            // as we don't know what happens with them and assume the contents are somewhow used outside the graph
                            // With MSAA we may access the resolved data for longer than the MSAA data so we track the destroyPass and lastPassThatNeedsUnresolved separately
                            // In theory the opposite could also be true (use MSAA after resolve data is no longer needed) but we consider it sufficiently strange to not
                            // consider it here.
                            storeAction = RenderBufferStoreAction.DontCare;
                            //Check if we're the last pass writing it by checking the output version of the current pass is the higherst version the resource will reach
                            bool lastWriter = (resourceData.latestVersionNumber == fragment.resource.version);
                            // Cheaper but same? = resourceData.lastWritePassID >= pass.firstGraphPass && resourceData.lastWritePassID < pass.firstGraphPass + pass.numSubPasses;
                            bool isImportedLastWriter = isImported && lastWriter;

                            // Used outside this native render pass, we need to store something
                            if (destroyPassID >= nativePass.firstGraphPass + nativePass.numGraphPasses)
                            {
                                // Assume nothing is needed unless we are an imported texture (which doesn't require discarding) and we're the last ones writing it
                                bool needsMSAASamples = isImportedLastWriter && !resourceData.discard;
                                bool needsResolvedData = isImportedLastWriter && (resourceData.bindMS == false);
                                int userPassID = 0;
                                int msaaUserPassID = 0;

                                // Check if we need msaa/resolved data by checking all the passes using this buffer
                                // Partial writes will register themselves as readers so this should be adequate
                                foreach (ref readonly var reader in contextData.Readers(fragment.resource))
                                {
                                    ref var readerPass = ref contextData.passData.ElementAt(reader.passId);
                                    bool isFragmentUsed = readerPass.IsUsedAsFragment(fragment.resource, contextData);

                                    // Unsafe pass - we cannot know how it is used, so we need to both store and resolve
                                    if (readerPass.type == RenderGraphPassType.Unsafe)
                                    {
                                        needsMSAASamples = true;
                                        needsResolvedData = !resourceData.bindMS;
                                        msaaUserPassID = reader.passId;
                                        userPassID = reader.passId;
                                        break;
                                    }
                                    // A fragment attachment use we need the msaa samples
                                    if (isFragmentUsed)
                                    {
                                        needsMSAASamples = true;
                                        msaaUserPassID = reader.passId;
                                    }
                                    else
                                    {
                                        // Used as a multisample-texture we need the msaa samples
                                        if (resourceData.bindMS)
                                        {
                                            needsMSAASamples = true;
                                            msaaUserPassID = reader.passId;
                                        }
                                        // Used as a regular non-multisample texture we need resolved data
                                        else
                                        {
                                            needsResolvedData = true;
                                            userPassID = reader.passId;
                                        }
                                    }
                                }

                                if (needsMSAASamples && needsResolvedData)
                                {
                                    storeAction = RenderBufferStoreAction.StoreAndResolve;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                                    if (generateAudits)
                                        currStoreAudit = new StoreAudit(
                                            (isImportedLastWriter ? StoreReason.StoreImported : StoreReason.StoreUsedByLaterPass),
                                            userPassID,
                                            (isImportedLastWriter ? StoreReason.StoreImported : StoreReason.StoreUsedByLaterPass),
                                            msaaUserPassID);
#endif
                                }
                                else if (needsResolvedData)
                                {
                                    storeAction = RenderBufferStoreAction.Resolve;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                                    if (generateAudits)
                                        currStoreAudit = new StoreAudit(
                                            (isImportedLastWriter ? StoreReason.StoreImported : StoreReason.StoreUsedByLaterPass),
                                            userPassID,
                                            StoreReason.DiscardUnused);
#endif
                                }
                                else if (needsMSAASamples)
                                {
                                    storeAction = RenderBufferStoreAction.Store;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                                    if (generateAudits)
                                        currStoreAudit = new StoreAudit(
                                            (resourceData.bindMS ? StoreReason.DiscardBindMs : StoreReason.DiscardUnused),
                                            -1,
                                            (isImportedLastWriter ? StoreReason.StoreImported : StoreReason.StoreUsedByLaterPass),
                                            msaaUserPassID);
#endif
                                }
                                else
                                {
                                    Debug.Assert(false, "Resource was not destroyed but nobody seems to be using it?!");
                                }
                            }
                            else if (isImportedLastWriter)
                            {
                                //It's an imported texture and we're the last ones writing it make sure to store the results

                                // Used as a multisample-texture, we need the msaa samples only
                                if (resourceData.bindMS)
                                {
                                    if (resourceData.discard)
                                    {
                                        storeAction = RenderBufferStoreAction.DontCare;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                                        if (generateAudits)
                                            currStoreAudit = new StoreAudit(StoreReason.DiscardImported);
#endif
                                    }
                                    else
                                    {
                                        storeAction = RenderBufferStoreAction.Store;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                                        if (generateAudits)
                                            currStoreAudit = new StoreAudit(
                                                StoreReason.DiscardBindMs, -1, StoreReason.StoreImported);
#endif
                                    }
                                }
                                // Used as a regular non-multisample texture, we need samples as resolved data
                                // we have no idea which one of them will be needed by the external users
                                else
                                {
                                    if (resourceData.discard)
                                    {
                                        // Depth attachment always comes first if existing
                                        bool isDepthAttachment = (nativePass.hasDepth && nativePass.attachments.size == 0);

                                        // For color attachment, we only discard the MSAA buffers and keep the resolve texture
                                        // This is a design decision due to the restrictive ImportResourceParams API, it could be revised later
                                        storeAction = isDepthAttachment
                                            ? RenderBufferStoreAction.DontCare
                                            : RenderBufferStoreAction.Resolve;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                                        if (generateAudits)
                                            currStoreAudit = new StoreAudit(
                                                StoreReason.DiscardImported, -1, StoreReason.DiscardImported);
#endif
                                    }
                                    else
                                    {
                                        storeAction = RenderBufferStoreAction.StoreAndResolve;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                                        if (generateAudits)
                                            currStoreAudit = new StoreAudit(
                                                StoreReason.StoreImported, -1, StoreReason.StoreImported);
#endif
                                    }
                                }
                            }
                        }
                    }

                    if (resourceData.memoryLess)
                    {
                        memoryless = true;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
                        // Ensure load/store actions are actually valid for memory less
                        if (loadAction == RenderBufferLoadAction.Load)
                            throw new Exception(RenderGraph.RenderGraphExceptionMessages.k_LoadingMemorylessResource);
                        if (storeAction != RenderBufferStoreAction.DontCare)
                            throw new Exception(RenderGraph.RenderGraphExceptionMessages.k_ResolvignMemorylessResource);
#endif
                    }

                    var newAttachment = new NativePassAttachment(
                        handle,
                        loadAction,
                        storeAction,
                        memoryless,
                        mipLevel,
                        depthSlice
                    );

                    nativePass.attachments.Add(newAttachment);
                }
            }
        }

        [Conditional("DEVELOPMENT_BUILD"), Conditional("UNITY_EDITOR")]
        private void ValidateNativePass(in NativePassData nativePass, int width, int height, int depth, int samples, int attachmentCount)
        {
            if (RenderGraph.enableValidityChecks)
            {
                if (nativePass.attachments.size == 0 || nativePass.numNativeSubPasses == 0)
                    throw new Exception(RenderGraph.RenderGraphExceptionMessages.k_RenderPassIsEmpty);

                if (width == 0 || height == 0 || depth == 0 || samples == 0 || nativePass.numNativeSubPasses == 0 || attachmentCount == 0)
                    throw new Exception(RenderGraph.RenderGraphExceptionMessages.k_RenderPassHasInvalidProperties);
            }
        }

        [Conditional("DEVELOPMENT_BUILD"), Conditional("UNITY_EDITOR")]
        private void ValidateAttachment(in RenderTargetInfo attRenderTargetInfo, RenderGraphResourceRegistry resources, int nativePassWidth, int nativePassHeight, int nativePassMSAASamples,
            bool isVrs, bool isShaderResolve)
        {
            if (RenderGraph.enableValidityChecks)
            {
                if (isVrs)
                {
                    var tileSize = ShadingRateImage.GetAllocTileSize(nativePassWidth, nativePassHeight);

                    if (attRenderTargetInfo.width != tileSize.x || attRenderTargetInfo.height != tileSize.y || attRenderTargetInfo.msaaSamples != 1)
                    {
                        throw new Exception(RenderGraph.RenderGraphExceptionMessages.k_ShadingRateImageAttachmentDoesNotMatch);
                    }
                }
                else
                {
                    if (attRenderTargetInfo.width != nativePassWidth || attRenderTargetInfo.height != nativePassHeight || (attRenderTargetInfo.msaaSamples != nativePassMSAASamples && !isShaderResolve))
                        throw new Exception(RenderGraph.RenderGraphExceptionMessages.k_AttachmentsDoNotMatch);
                }
            }
        }

        internal unsafe void ExecuteBeginRenderPass(InternalRenderGraphContext rgContext, RenderGraphResourceRegistry resources, ref NativePassData nativePass)
        {
            using (new ProfilingScope(ProfilingSampler.Get(NativeCompilerProfileId.NRPRGComp_ExecuteBeginRenderpassCommand)))
            {
                ref var attachments = ref nativePass.attachments;
                var attachmentCount = attachments.size;

                var width = nativePass.width;
                var height = nativePass.height;
                var volumeDepth = nativePass.volumeDepth;
                var samples = nativePass.samples;
                var isShaderResolve = nativePass.extendedFeatureFlags.HasFlag(ExtendedFeatureFlags.MultisampledShaderResolve);
                ValidateNativePass(nativePass, width, height, volumeDepth, samples, attachmentCount);

                ref var nativeSubPasses = ref contextData.nativeSubPassData;
                NativeArray<SubPassDescriptor> nativeSubPassArray = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<SubPassDescriptor>(nativeSubPasses.GetUnsafeReadOnlyPtr<SubPassDescriptor>() + nativePass.firstNativeSubPass, nativePass.numNativeSubPasses, Allocator.None);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
                var safetyHandle = AtomicSafetyHandle.Create();
                AtomicSafetyHandle.SetAllowReadOrWriteAccess(safetyHandle, true);
                NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref nativeSubPassArray, safetyHandle);
#endif

                if (nativePass.hasFoveatedRasterization)
                {
                    rgContext.cmd.SetFoveatedRenderingMode(FoveatedRenderingMode.Enabled);
                }

                if (nativePass.hasShadingRateStates)
                {
                    rgContext.cmd.SetShadingRateFragmentSize(nativePass.shadingRateFragmentSize);
                    rgContext.cmd.SetShadingRateCombiner(ShadingRateCombinerStage.Primitive, nativePass.primitiveShadingRateCombiner);
                    rgContext.cmd.SetShadingRateCombiner(ShadingRateCombinerStage.Fragment, nativePass.fragmentShadingRateCombiner);
                }

                // Filling the attachments array to be sent to the rendering command buffer
                if(!m_BeginRenderPassAttachments.IsCreated)
                    m_BeginRenderPassAttachments = new NativeList<AttachmentDescriptor>(FixedAttachmentArray<AttachmentDescriptor>.MaxAttachments, Allocator.Persistent);

                m_BeginRenderPassAttachments.Resize(attachmentCount, NativeArrayOptions.UninitializedMemory);
                for (var i = 0; i < attachmentCount; ++i)
                {
                    ref readonly var currAttachmentHandle = ref attachments[i].handle;

                    resources.GetRenderTargetInfo(currAttachmentHandle, out var renderTargetInfo);

                    bool isVrs = (i == nativePass.shadingRateImageIndex);
                    ValidateAttachment(renderTargetInfo, resources, width, height, samples, isVrs, isShaderResolve);

                    ref var currBeginAttachment = ref m_BeginRenderPassAttachments.ElementAt(i);
                    currBeginAttachment = new AttachmentDescriptor(renderTargetInfo.format);

                    // Set up the RT pointers
                    if (attachments[i].memoryless == false)
                    {
                        var rtHandle = resources.GetTexture(currAttachmentHandle.index);

                        //HACK: Always set the loadstore target even if StoreAction == DontCare or Resolve
                        //and LoadAction == Clear or DontCare
                        //in these cases you could argue setting the loadStoreTarget to NULL and only set the resolveTarget
                        //but this confuses the backend (on vulkan) and in general is not how the lower level APIs tend to work.
                        //because of the RenderTexture duality where we always bundle store+resolve targets as one RTex
                        //it does become impossible to have a memoryless loadStore texture with a memoryfull resolve
                        //but that is why we mark this as a hack and future work to fix.
                        //The proper (and planned) solution would be to move away from the render texture duality.
                        RenderTargetIdentifier rtidAllSlices = rtHandle;
                        currBeginAttachment.loadStoreTarget = new RenderTargetIdentifier(rtidAllSlices, attachments[i].mipLevel, CubemapFace.Unknown, attachments[i].depthSlice);

                        if (attachments[i].storeAction == RenderBufferStoreAction.Resolve ||
                            attachments[i].storeAction == RenderBufferStoreAction.StoreAndResolve)
                        {
                            currBeginAttachment.resolveTarget = rtHandle;
                        }
                    }
                    // In the memoryless case it's valid to not set both loadStoreTarget/and resolveTarget as the backend will allocate a transient one

                    currBeginAttachment.loadAction = attachments[i].loadAction;
                    currBeginAttachment.storeAction = attachments[i].storeAction;

                    // Set up clear colors if we have a clear load action
                    if (attachments[i].loadAction == RenderBufferLoadAction.Clear)
                    {
                        currBeginAttachment.clearColor = Color.red;
                        currBeginAttachment.clearDepth = 1.0f;
                        currBeginAttachment.clearStencil = 0;
                        var desc = resources.GetTextureResourceDesc(currAttachmentHandle, true);
                        if (i == 0 && nativePass.hasDepth)
                        {
                            // TODO: There seems to be no clear depth specified ?!?!
                            currBeginAttachment.clearDepth = 1.0f; // desc.clearDepth;
                        }
                        else
                        {
                            currBeginAttachment.clearColor = desc.clearColor;
                        }
                    }
                }

                if (nativePass.extendedFeatureFlags.HasFlag(ExtendedFeatureFlags.MultisampledShaderResolve))
                {
                    var lastSubpass = nativeSubPassArray[^1];

                    // All input attachments must be memoryless for the shader resolve enabled subpass.
                    for (int i = 0; i < lastSubpass.inputs.Length; i++)
                    {
                        int inputIndex = lastSubpass.inputs[i];
                        ref var inputAttachment = ref m_BeginRenderPassAttachments.ElementAt(inputIndex);
                        if (inputAttachment.storeAction != RenderBufferStoreAction.DontCare)
                        {
                            throw new Exception(RenderGraph.RenderGraphExceptionMessages.k_MultisampledShaderResolveInputAttachmentNotMemoryless);
                        }
                    }

                    // The last subpass in a native pass with shader resolve is required to be the subpass that handles the resolve, and this subpass can only have 1 color attachment.
                    if (lastSubpass.colorOutputs.Length != 1)
                        throw new Exception(RenderGraph.RenderGraphExceptionMessages.k_MultisampledShaderResolveInvalidAttachmentSetup);

                    if (SystemInfo.supportsMultisampledShaderResolve)
                    {
                        int attachmentIndex = lastSubpass.colorOutputs[0];

                        ref var currBeginAttachment = ref m_BeginRenderPassAttachments.ElementAt(attachmentIndex);
                        currBeginAttachment.resolveTarget = currBeginAttachment.loadStoreTarget;
                        currBeginAttachment.loadStoreTarget = new RenderTargetIdentifier(BuiltinRenderTextureType.None);
                        currBeginAttachment.storeAction = RenderBufferStoreAction.Store;
                    }
                }

                NativeArray<AttachmentDescriptor> attachmentDescArray = m_BeginRenderPassAttachments.AsArray();

                var depthAttachmentIndex = nativePass.hasDepth ? 0 : -1;

                var graphPassNamesForDebugSpan = ReadOnlySpan<byte>.Empty;
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                if (RenderGraph.enableValidityChecks)
                {
                    graphPassNamesForDebug.Clear();

                    nativePass.GetGraphPassNames(contextData, graphPassNamesForDebug);

                    int utf8CStrDebugNameLength = 0;
                    foreach (ref readonly Name graphPassName in graphPassNamesForDebug)
                    {
                        utf8CStrDebugNameLength += graphPassName.utf8ByteCount + 1; // +1 to add '/' between passes or the null terminator at the end
                    }

                    var nameBytes = stackalloc byte[utf8CStrDebugNameLength];
                    if (utf8CStrDebugNameLength > 0)
                    {
                        int startStr = 0;
                        foreach (ref readonly var graphPassName in graphPassNamesForDebug)
                        {
                            int strByteCount = graphPassName.utf8ByteCount;
                            System.Text.Encoding.UTF8.GetBytes(graphPassName.name.AsSpan(), new Span<byte>(nameBytes + startStr, strByteCount));
                            startStr += strByteCount;
                            // Adding '/' in UTF8
                            nameBytes[startStr++] = (byte)(0x2F);
                        }

                        // Rewriting last '/' to be the null terminator
                        nameBytes[utf8CStrDebugNameLength - 1] = (byte)0;
                    }

                    graphPassNamesForDebugSpan = new ReadOnlySpan<byte>(nameBytes, utf8CStrDebugNameLength);
                }
#endif

                rgContext.cmd.BeginRenderPass(width, height, volumeDepth, samples, attachmentDescArray, depthAttachmentIndex, nativePass.shadingRateImageIndex, nativeSubPassArray, graphPassNamesForDebugSpan);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.Release(safetyHandle);
#endif
                CommandBuffer.ThrowOnSetRenderTarget = true;
            }
        }

        const int ArbitraryMaxNbMergedPasses = 16;
        DynamicArray<Name> graphPassNamesForDebug = new DynamicArray<Name>(ArbitraryMaxNbMergedPasses);

        private void ExecuteDestroyResource(InternalRenderGraphContext rgContext, RenderGraphResourceRegistry resources, ref PassData pass)
        {
            using (new ProfilingScope(ProfilingSampler.Get(NativeCompilerProfileId.NRPRGComp_ExecuteDestroyResources)))
            {
                // Unsafe pass might soon use temporary render targets,
                // users can also use temporary data in their render graph execute nodes using public RenderGraphObjectPool API
                // In both cases, we need to release these resources after the node execution
                rgContext.renderGraphPool.ReleaseAllTempAlloc();

                if (pass.type == RenderGraphPassType.Raster && pass.nativePassIndex >= 0)
                {
                    // For raster passes we need to destroy resources after all the subpasses at the end of the native renderpass
                    if (pass.mergeState == PassMergeState.End || pass.mergeState == PassMergeState.None)
                    {
                        ref var nativePass = ref contextData.nativePassData.ElementAt(pass.nativePassIndex);
                        foreach (ref readonly var subPass in nativePass.GraphPasses(contextData))
                        {
                            foreach (ref readonly var res in subPass.LastUsedResources(contextData))
                            {
                                ref readonly var resInfo = ref contextData.UnversionedResourceData(res);
                                if (resInfo.isImported == false)
                                {
                                    resources.ReleasePooledResource(rgContext, res.iType, res.index);
                                }
                            }
                        }
                    }
                }
                else
                {
                    foreach (ref readonly var destroy in pass.LastUsedResources(contextData))
                    {
                        ref readonly var pointTo = ref contextData.UnversionedResourceData(destroy);
                        if (pointTo.isImported == false)
                        {
                            resources.ReleasePooledResource(rgContext, destroy.iType, destroy.index);
                        }
                    }
                }
            }
        }

        private void ExecuteSetRenderTargets(RenderGraphPass pass, InternalRenderGraphContext rgContext)
        {
            var depthBufferIsValid = pass.depthAccess.textureHandle.IsValid();
            if (depthBufferIsValid || pass.colorBufferMaxIndex != -1)
            {
                var resources = graph.m_ResourcesForDebugOnly;
                var colorBufferAccess = pass.colorBufferAccess;
                if (pass.colorBufferMaxIndex > 0)
                {
                    var mrtArray = m_TempMRTArrays[pass.colorBufferMaxIndex];

                    for (int i = 0; i <= pass.colorBufferMaxIndex; ++i)
                    {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                        if (!colorBufferAccess[i].textureHandle.IsValid())
                            throw new InvalidOperationException($"In pass {pass.name}, when trying to use {colorBufferAccess[i].textureHandle.handle.type} attachment at index {colorBufferAccess[i].textureHandle.handle.index} - " + RenderGraph.RenderGraphExceptionMessages.k_InvalidMRTSetup);
#endif
                        mrtArray[i] = resources.GetTexture(colorBufferAccess[i].textureHandle);
                    }

                    if (depthBufferIsValid)
                    {
                        CoreUtils.SetRenderTarget(rgContext.cmd, mrtArray, resources.GetTexture(pass.depthAccess.textureHandle));
                    }
                    else
                    {
                        throw new InvalidOperationException($"In pass {pass.name} - " + RenderGraph.RenderGraphExceptionMessages.k_NoDepthBufferMRT);
                    }
                }
                else
                {
                    if (depthBufferIsValid)
                    {
                        if (pass.colorBufferMaxIndex > -1)
                        {
                            CoreUtils.SetRenderTarget(rgContext.cmd, resources.GetTexture(pass.colorBufferAccess[0].textureHandle),
                                resources.GetTexture(pass.depthAccess.textureHandle));
                        }
                        else
                        {
                            CoreUtils.SetRenderTarget(rgContext.cmd, resources.GetTexture(pass.depthAccess.textureHandle));
                        }
                    }
                    else
                    {
                        if (pass.colorBufferAccess[0].textureHandle.IsValid())
                        {
                            CoreUtils.SetRenderTarget(rgContext.cmd, resources.GetTexture(pass.colorBufferAccess[0].textureHandle));
                        }
                        else
                            throw new InvalidOperationException($"In pass {pass.name} - " + RenderGraph.RenderGraphExceptionMessages.k_InvalidDepthAndColorTargets);
                    }
                }
            }
        }

        internal unsafe void ExecuteSetRandomWriteTarget(in CommandBuffer cmd, RenderGraphResourceRegistry resources, int index, ResourceHandle resource, bool preserveCounterValue = true)
        {
            if (resource.type == RenderGraphResourceType.Texture)
            {
                var tex = resources.GetTexture(resource.index);
                cmd.SetRandomWriteTarget(index, tex);
            }
            else if (resource.type == RenderGraphResourceType.Buffer)
            {
                var buff = resources.GetBuffer(resource.index);
                // Default is to preserve the value
                if (preserveCounterValue)
                {
                    cmd.SetRandomWriteTarget(index, buff);
                }
                else
                {
                    cmd.SetRandomWriteTarget(index, buff, false);
                }
            }
            else
            {
                var name = resources.GetRenderGraphResourceName(resource);
                throw new Exception($"When trying to use resource '{name}' of type {resource.type} - " + RenderGraph.RenderGraphExceptionMessages.k_InvalidResourceType);
            }
        }

        internal void ExecuteRenderGraphPass(ref InternalRenderGraphContext rgContext, RenderGraphResourceRegistry resources, RenderGraphPass pass)
        {

            rgContext.executingPass = pass;

            if (!pass.HasRenderFunc())
            {
                throw new InvalidOperationException($"In pass {pass.name} - " +
                                                    RenderGraph.RenderGraphExceptionMessages.k_NoRenderFunction);
            }

            using (new ProfilingScope(rgContext.cmd, pass.customSampler))
            {
                pass.Execute(rgContext);

                foreach (var tex in pass.setGlobalsList)
                {
                    rgContext.cmd.SetGlobalTexture(tex.Item2, tex.Item1);
                }
            }
        }

        public void ExecuteGraph(InternalRenderGraphContext rgContext, RenderGraphResourceRegistry resources, in List<RenderGraphPass> passes)
        {
            bool inRenderPass = false;
            previousCommandBuffer = rgContext.cmd;

            // Having random access targets bound leads to all sorts of weird behavior so we clear them before executing the graph.
            rgContext.cmd.ClearRandomWriteTargets();

            for (int passIndex = 0; passIndex < contextData.passData.Length; passIndex++)
            {
                ref var passData = ref contextData.passData.ElementAt(passIndex);
                if (passData.culled)
                    continue;

                bool nrpBegan = false;
                ExecuteInitializeResource(rgContext, resources, passData);

                if (passData.type == RenderGraphPassType.Compute && passData.asyncCompute)
                {
                    if (!rgContext.contextlessTesting)
                        rgContext.renderContext.ExecuteCommandBuffer(rgContext.cmd);
                    rgContext.cmd.Clear();

                    var asyncCmd = CommandBufferPool.Get("async cmd");
                    asyncCmd.SetExecutionFlags(CommandBufferExecutionFlags.AsyncCompute);
                    rgContext.cmd = asyncCmd;
                }

                // also make sure to insert fence=waits for multiple queue syncs
                if (passData.waitOnGraphicsFencePassId != -1)
                {
                    var fence = contextData.fences[passData.waitOnGraphicsFencePassId];
                    rgContext.cmd.WaitOnAsyncGraphicsFence(fence, SynchronisationStageFlags.PixelProcessing);
                }

                if (passData.type == RenderGraphPassType.Raster && passData.mergeState <= PassMergeState.Begin)
                {
                    if (passData.nativePassIndex >= 0)
                    {
                        ref var nativePass = ref contextData.nativePassData.ElementAt(passData.nativePassIndex);
                        if (nativePass.fragments.size > 0)
                        {
                            ExecuteBeginRenderPass(rgContext, resources, ref nativePass);
                            nrpBegan = true;
                            inRenderPass = true;
                        }
                    }
                }

                else if (passData.type == RenderGraphPassType.Unsafe)
                {
                    ExecuteSetRenderTargets(passes[passIndex], rgContext);
                }

                if (passData.mergeState >= PassMergeState.SubPass)
                {
                    if (passData.beginNativeSubpass)
                    {
                        if (!inRenderPass)
                        {
                            throw new Exception(RenderGraph.RenderGraphExceptionMessages.k_BeginNoActivePass);
                        }

                        rgContext.cmd.NextSubPass();
                    }
                }

                if (passData.numRandomAccessResources > 0)
                {
                    foreach (var randomWriteAttachment in passData.RandomWriteTextures(contextData))
                    {
                        ExecuteSetRandomWriteTarget(rgContext.cmd, resources, randomWriteAttachment.index,
                            randomWriteAttachment.resource);
                    }
                }

                ExecuteRenderGraphPass(ref rgContext, resources, passes[passData.passId]);
                EndRenderGraphPass(ref rgContext, ref passData, ref inRenderPass, resources, nrpBegan);

            }
        }

        void EndRenderGraphPass(ref InternalRenderGraphContext rgContext, ref PassData passData,
            ref bool inRenderPass, RenderGraphResourceRegistry resources, bool nrpBegan)
        {
            // If we set any uavs clear them again so they are local to the pass
            if (passData.numRandomAccessResources > 0)
            {
                rgContext.cmd.ClearRandomWriteTargets();
            }

            // should we insert a fence to sync between difference queues?
            if (passData.insertGraphicsFence)
            {
                var fence = rgContext.cmd.CreateAsyncGraphicsFence();
                contextData.fences[passData.passId] = fence;
            }

            if (passData.type == RenderGraphPassType.Raster)
            {
                var hasRenderPassEnded = (passData.mergeState == PassMergeState.None && nrpBegan)
                                 || passData.mergeState == PassMergeState.End;

                if (hasRenderPassEnded)
                {
                    if (passData.nativePassIndex >= 0)
                    {
                        ref var nativePass = ref contextData.nativePassData.ElementAt(passData.nativePassIndex);
                        if (nativePass.fragments.size > 0)
                        {
                            if (!inRenderPass)
                            {
                                throw new Exception(RenderGraph.RenderGraphExceptionMessages.k_NoActivePassForSubpass);
                            }

                            if (nativePass.hasFoveatedRasterization)
                            {
                                rgContext.cmd.SetFoveatedRenderingMode(FoveatedRenderingMode.Disabled);
                            }

                            rgContext.cmd.EndRenderPass();
                            CommandBuffer.ThrowOnSetRenderTarget = false;
                            inRenderPass = false;

                            // VRS ShadingRate(Image) cannot be set inside a render pass (cmdBuf).
                            // ShadingRate is set before BeginRenderPass and here we ResetShadingRate after EndRenderPass.
                            if (nativePass.hasShadingRateStates || nativePass.hasShadingRateImage)
                            {
                                rgContext.cmd.ResetShadingRate();
                            }
                        }
                    }
                }
            }
            else if (passData.type == RenderGraphPassType.Compute && passData.asyncCompute)
            {
                rgContext.renderContext.ExecuteCommandBufferAsync(rgContext.cmd, ComputeQueueType.Background);
                CommandBufferPool.Release(rgContext.cmd);
                rgContext.cmd = previousCommandBuffer;
            }

            ExecuteDestroyResource(rgContext, resources, ref passData);
        }
    }
}

