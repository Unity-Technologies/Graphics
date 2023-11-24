using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Rendering;

namespace UnityEngine.Rendering.RenderGraphModule.NativeRenderPassCompiler
{
    internal partial class NativePassCompiler
    {
        internal struct RenderGraphInputInfo
        {
            public RenderGraphResourceRegistry m_ResourcesForDebugOnly;
            public List<RenderGraphPass> m_RenderPasses;
            public string debugName;
            public bool disableCulling;
        }

        internal RenderGraphInputInfo graph;
        internal CompilerContextData contextData;
        internal CommandBuffer previousCommandBuffer;
        Stack<int> toVisitPassIds;

        public NativePassCompiler()
        {
            int estNumPasses = 100;
            contextData = new CompilerContextData(estNumPasses, 50);
            toVisitPassIds = new Stack<int>(estNumPasses);
        }

        public void Initialize(RenderGraphResourceRegistry resources, List<RenderGraphPass> renderPasses, bool disableCulling, string debugName)
        {
            graph.m_ResourcesForDebugOnly = resources;
            graph.m_RenderPasses = renderPasses;
            graph.disableCulling = disableCulling;
            graph.debugName = debugName;

            SetupContextData(resources);

            BuildGraph();

            CullUnusedRenderPasses();

            TryMergeNativePasses();

            FindResourceUsageRanges();

            DetectMemoryLessResources();

            PrepareNativeRenderPasses();
        }

        public void Clear()
        {
            contextData.Clear();
            toVisitPassIds.Clear();
        }

        void SetPassStatesForNativePass(int nativePassId)
        {
            NativePassData.SetPassStatesForNativePass(contextData, nativePassId);
        }

        internal enum NativeCompilerProfileId
        {
            NRPRGComp_Compile,
            NRPRGComp_Execute,
            NRPRGComp_SetupContextData,
            NRPRGComp_BuildGraph,
            NRPRGComp_CullNodes,
            NRPRGComp_TryMergeNativePasses,
            NRPRGComp_FindResourceUsageRanges,
            NRPRGComp_DetectMemorylessResources,
            NRPRGComp_ExecuteGraph,
            NRPRGComp_ExecuteCreateResources,
            NRPRGComp_ExecuteBeginRenderpassCommand,
            NRPRGComp_ExecuteDestroyCommands,
            NRPRGComp_ExecutePassCommandBuffer
        }

        void SetupContextData(RenderGraphResourceRegistry resources)
        {
            using (new ProfilingScope(ProfilingSampler.Get(NativeCompilerProfileId.NRPRGComp_SetupContextData)))
            {
                contextData.Initialize(resources);
            }
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
                        throw new Exception("Pass '" + inputPass.name + "' is using the legacy rendergraph API." +
                            " You cannot use legacy passes with the native render pass compiler." +
                            " Please do not use AddPass on the rendergrpah but use one of the more specific pass types such as AddRasterRenderPass");
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
                        // Grab offset in context fragment list to begin building the fragment list
                        ctxPass.firstFragment = ctx.fragmentData.Length;

                        // Depth attachment is always at index 0
                        if (inputPass.depthBuffer.handle.IsValid())
                        {
                            ctxPass.fragmentInfoHasDepth = true;

                            var resource = inputPass.depthBuffer.handle;
                            if (ctx.AddToFragmentList(resource, inputPass.depthBufferAccessFlags, ctxPass.firstFragment, ctxPass.numFragments))
                            {
                                ctxPass.AddFragment(resource, ctx);
                            }
                        }

                        for (var ci = 0; ci < inputPass.colorBufferMaxIndex + 1; ++ci)
                        {
                            // Skip unused color slots
                            if (!inputPass.colorBuffers[ci].handle.IsValid()) continue;

                            var resource = inputPass.colorBuffers[ci].handle;
                            if (ctx.AddToFragmentList(resource, inputPass.colorBufferAccessFlags[ci], ctxPass.firstFragment, ctxPass.numFragments))
                            {
                                ctxPass.AddFragment(resource, ctx);
                            }
                        }

                        // Grab offset in context fragment list to begin building the fragment input list
                        ctxPass.firstFragmentInput = ctx.fragmentData.Length;

                        for (var ci = 0; ci < inputPass.fragmentInputMaxIndex + 1; ++ci)
                        {
                            ref var currInpFragment = ref inputPass.fragmentInputs[ci].handle;
                            // Skip unused fragment input slots
                            if (!currInpFragment.IsValid()) continue;

                            var resource = inputPass.fragmentInputs[ci].handle;
                            if (ctx.AddToFragmentList(resource, inputPass.fragmentInputAccessFlags[ci], ctxPass.firstFragmentInput, ctxPass.numFragmentInputs))
                            {
                                ctxPass.AddFragmentInput(resource, ctx);
                            }
                        }

                        // Grab offset in context random write list to begin building the per pass random write lists
                        ctxPass.firstRandomAccessResource = ctx.randomAccessResourceData.Length;

                        for (var ci = 0; ci < passes[passId].randomAccessResourceMaxIndex + 1; ++ci)
                        {
                            ref var uav = ref passes[passId].randomAccessResource[ci];

                            // Skip unused random write slots
                            if (!uav.h.IsValid()) continue;

                            if (ctx.AddToRandomAccessResourceList(uav.h, ci, uav.preserveCounterValue, ctxPass.firstRandomAccessResource, ctxPass.numRandomAccessResources))
                            {
                                ctxPass.AddRandomAccessResource();
                            }
                        }

                        // This is suspicious, there are frame buffer fetch inputs but nothing is output. We don't allow this for now.
                        // In theory you could fb-fetch inputs and write something to a uav and output nothing? This needs to be investigated
                        // so don't allow it for now.
                        if (ctxPass.numFragments == 0)
                        {
                            Debug.Assert(ctxPass.numFragmentInputs == 0);
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
                                if (ctxPass.hasSideEffects == false)
                                {
                                    ctxPass.hasSideEffects = true;
                                    toVisitPassIds.Push(passId);
                                }
                            }

                            // Mark this pass as writing to this version of the resource
                            ctx.resources[resource].SetWritingPass(ctx, resource, passId);

                            ctx.outputData.Add(new PassOutputData
                            {
                                resource = resource,
                            });

                            ctxPass.numOutputs++;
                        }

                        var resourceRead = inputPass.resourceReadLists[type];
                        var resourceReadCount = resourceRead.Count;
                        for (var i = 0; i < resourceReadCount; ++i)
                        {
                            var resource = resourceRead[i];

                            // Mark this pass as reading from this version of the resource
                            ctx.resources[resource].RegisterReadingPass(ctx, resource, passId, ctxPass.numInputs);

                            ctx.inputData.Add(new PassInputData
                            {
                                resource = resource,
                            });

                            ctxPass.numInputs++;
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
                if (graph.disableCulling)
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
                var numPasses = ctx.passData.Length;
                for (int passIndex = 0; passIndex < numPasses; passIndex++)
                {
                    ref readonly var pass = ref ctx.passData.ElementAt(passIndex);

                    // Remove the connections from the list so they won't be visited again
                    if (pass.culled)
                    {
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

                int activeNativePassId = -1;

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
                        var mergeTestResult = NativePassData.TryMerge(contextData, activeNativePassId, passIdx);

                        // Merge failed, close current native render pass and create a new one
                        if (mergeTestResult.reason != PassBreakReason.Merged)
                        {
                            SetPassStatesForNativePass(activeNativePassId);
#if UNITY_EDITOR
                            ref var nativePassData = ref contextData.nativePassData.ElementAt(activeNativePassId);
                            nativePassData.breakAudit = mergeTestResult;
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
#if UNITY_EDITOR
                    ref var nativePassData = ref contextData.nativePassData.ElementAt(activeNativePassId);
                    nativePassData.breakAudit = new PassBreakAudit(PassBreakReason.EndOfGraph, -1);
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
                            var refC = pointTo.tag - 1;//Decrease refcount this pass is done using it
                            if (refC == 0)// We're the last pass done using it, this pass should destroy it.
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
                            ref var wPass = ref ctx.passData.ElementAt(pointToVer.writePassId);
                            if (wPass.asyncCompute != pass.asyncCompute)
                            {
                                pass.waitOnGraphicsFencePassId = wPass.passId;
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
                            var depIdx = ResourcesData.IndexReader(outputResource, i);
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
                PrepareNativeRenderPass(ref nativePassData);
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

        private bool IsSameNativeSubPass(ref SubPassDescriptor a, ref SubPassDescriptor b)
        {
            if (a.flags != b.flags
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


        private void ExecuteCreateRessource(InternalRenderGraphContext rgContext, RenderGraphResourceRegistry resources, in PassData pass)
        {
            using (new ProfilingScope(ProfilingSampler.Get(NativeCompilerProfileId.NRPRGComp_ExecuteCreateResources)))
            {
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
                                if (resInfo.isImported == false && resInfo.memoryLess == false)
                                {
                                    bool usedAsFragmentThisPass = subPass.IsUsedAsFragment(res, contextData);

                                    // If usedAsFragmentThisPass=false this is a resource read for the first time but as a regular texture not as a framebuffer attachment
                                    // so we need to explicitly clear it as loadaction.clear only works on fb attachments of course not texturereads
                                    // TODO: Should this be a performance warning?? Maybe rare enough in practice?
                                    resources.forceManualClearOfResourceDisabled = usedAsFragmentThisPass;
                                    resources.CreatePooledResource(rgContext, res);
                                    resources.forceManualClearOfResourceDisabled = false;
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
                        if (pointTo.isImported == false)
                        {
                            resources.forceManualClearOfResourceDisabled = false;
                            resources.CreatePooledResource(rgContext, create);
                            resources.forceManualClearOfResourceDisabled = false;
                        }
                    }
                }
            }
        }


        void PrepareNativeRenderPass(ref NativePassData nativePass)
        {
            using (new ProfilingScope(ProfilingSampler.Get(NativeCompilerProfileId.NRPRGComp_ExecuteBeginRenderpassCommand)))
            {
                ref readonly var firstGraphPass = ref contextData.passData.ElementAt(nativePass.firstGraphPass);
                ref readonly var lastGraphPass = ref contextData.passData.ElementAt(nativePass.firstGraphPass + nativePass.numGraphPasses - 1);

                // Some passes don't do any rendering only state changes so just skip them
                // If these passes trigger any drawing the raster command buffer will warn users no render targets are set-up for their rendering
                if (nativePass.fragments.size <= 0)
                {
                    return;
                }

                // Some sanity checks, these should not happen
                Debug.Assert(firstGraphPass.mergeState == PassMergeState.Begin || firstGraphPass.mergeState == PassMergeState.None);
                Debug.Assert(lastGraphPass.mergeState == PassMergeState.End || lastGraphPass.mergeState == PassMergeState.None);

                ref readonly var fragmentList = ref nativePass.fragments;

                // Fill out the subpass descriptors for the native renderpasses
                // NOTE: Not all graph subpasses get an actual native pass:
                // - There could be passes that do only non-raster ops (like setglobal) and have no attachments. They don't get a native pass
                // - Renderpasses that use exactly the same rendertargets at the previous pass use the same native pass. This is because
                //   nextSubpass is expensive on some platforms (even if its' essentially a no-op as it's using the same attachments).
                nativePass.firstNativeSubPass = contextData.nativeSubPassData.Length;
                for (var graphPassIndex = 0; graphPassIndex < nativePass.numGraphPasses; ++graphPassIndex)
                {
                    SubPassDescriptor desc = new SubPassDescriptor();
                    ref var graphPass = ref contextData.passData.ElementAt(nativePass.firstGraphPass + graphPassIndex);

                    // We have no output attachments, this is an "empty" raster pass doing only non-rendering command so skip it.
                    if (graphPass.numFragments == 0)
                    {
                        // We always merge it into the currently active
                        graphPass.nativeSubPassIndex = nativePass.numNativeSubPasses - 1;
                        graphPass.beginNativeSubpass = false;
                        continue;
                    }

                    // If depth ends up being bound only because of merging we explicitly say that we will not write to it
                    // which could have been implied by leaving the flag to None
                    if (!graphPass.fragmentInfoHasDepth && nativePass.hasDepth)
                    {
                        desc.flags = SubPassFlags.ReadOnlyDepth;
                    }

                    // MRT attachments
                    {
                        int fragmentIdx = 0;
                        int colorOffset = (graphPass.fragmentInfoHasDepth) ? -1 : 0;

                        desc.colorOutputs = new AttachmentIndexArray(graphPass.numFragments + colorOffset);

                        foreach (ref readonly var fragment in graphPass.Fragments(contextData))
                        {
                            // Check if we're handling the depth attachment
                            if (graphPass.fragmentInfoHasDepth && fragmentIdx == 0)
                            {
                                desc.flags = (fragment.accessFlags.HasFlag(AccessFlags.Write)) ? SubPassFlags.None : SubPassFlags.ReadOnlyDepth;
                            }
                            // It's a color attachment
                            else
                            {
                                // Find the index of this subpass's attachment in the native renderpass attachment list
                                int colorAttachmentIdx = -1;
                                for (int fragmentId = 0; fragmentId < fragmentList.size; ++fragmentId)
                                {
                                    if (fragmentList[fragmentId].resource.index == fragment.resource.index)
                                    {
                                        colorAttachmentIdx = fragmentId;
                                        break;
                                    }
                                }
                                Debug.Assert(colorAttachmentIdx >= 0); // If this is not the case it means we are using an attachment in a sub pass that is not part of the native pass !?!? clear bug
                                                                       // Set up the color indexes
                                desc.colorOutputs[fragmentIdx + colorOffset] = colorAttachmentIdx;
                            }
                            fragmentIdx++;
                        }
                    }

                    // FB-fetch attachments
                    {
                        int inputIndex = 0;

                        desc.inputs = new AttachmentIndexArray(graphPass.numFragmentInputs);

                        foreach (ref readonly var fragmentInput in graphPass.FragmentInputs(contextData))
                        {
                            // Find the index of this subpass's attachment in the native renderpass attachment list
                            int inputAttachmentIdx = -1;
                            for (int fragmentId = 0; fragmentId < fragmentList.size; ++fragmentId)
                            {
                                if (fragmentList[fragmentId].resource.index == fragmentInput.resource.index)
                                {
                                    inputAttachmentIdx = fragmentId;
                                    break;
                                }
                            }
                            Debug.Assert(inputAttachmentIdx >= 0); // If this is not the case it means we are using an attachment in a sub pass that is not part of the native pass !?!? clear bug
                                                                   // Set up the color indexes
                            desc.inputs[inputIndex] = inputAttachmentIdx;
                            inputIndex++;
                        }
                    }

                    // Check if we can merge the native sub pass with the previous one
                    if (nativePass.numNativeSubPasses == 0 || !IsSameNativeSubPass(ref desc, ref contextData.nativeSubPassData.ElementAt(nativePass.firstNativeSubPass + nativePass.numNativeSubPasses - 1)))
                    {
                        contextData.nativeSubPassData.Add(desc);
                        int idx = contextData.nativeSubPassData.LastIndex();
                        Debug.Assert(idx == nativePass.firstNativeSubPass + nativePass.numNativeSubPasses);
                        nativePass.numNativeSubPasses++;

                        graphPass.beginNativeSubpass = true;
                    }
                    else
                    {
                        graphPass.beginNativeSubpass = false;
                    }

                    graphPass.nativeSubPassIndex = nativePass.numNativeSubPasses - 1;
                }

                // determine load store actions
                // This pass also contains the latest versions used within this pass
                // As we have no pass reordering for now the merged passes are always a consecutive list and we can simply do a range
                // check on the create/destroy passid to see if it's allocated/freed in this native renderpass
                for (int fragmentId = 0; fragmentId < fragmentList.size; ++fragmentId)
                {
                    ref readonly var fragment = ref fragmentList[fragmentId];

                    int idx = nativePass.attachments.Add(new NativePassAttachment());
                    ref var currAttachment = ref nativePass.attachments[idx];

#if UNITY_EDITOR
                    nativePass.loadAudit.Add(new LoadAudit(LoadReason.FullyRewritten));
                    ref var currLoadAudit = ref nativePass.loadAudit[idx];

                    nativePass.storeAudit.Add(new StoreAudit(StoreReason.DiscardUnused));
                    ref var currStoreAudit = ref nativePass.storeAudit[idx];
#endif
                    currAttachment.handle = fragment.resource;

                    // Don't care by default
                    currAttachment.loadAction = UnityEngine.Rendering.RenderBufferLoadAction.DontCare;
                    currAttachment.storeAction = UnityEngine.Rendering.RenderBufferStoreAction.DontCare;

                    // Writing by-default has to preserve the contents, think rendering only a few small triangles on top of a big framebuffer
                    // So it means we need to load/clear contents potentially.
                    // If a user pass knows it will write all pixels in a buffer (like a blit) it can use the WriteAll/Discard usage to indicate this to the graph
                    bool partialWrite = fragment.accessFlags.HasFlag(AccessFlags.Write) && !fragment.accessFlags.HasFlag(AccessFlags.Discard);

                    ref readonly var resourceData = ref contextData.UnversionedResourceData(fragment.resource);
                    bool isImported = resourceData.isImported;

                    if (fragment.accessFlags.HasFlag(AccessFlags.Read) || partialWrite)
                    {
                        // The resource is already allocated before this pass so we need to load it
                        if (resourceData.firstUsePassID < nativePass.firstGraphPass)
                        {
                            currAttachment.loadAction = UnityEngine.Rendering.RenderBufferLoadAction.Load;
#if UNITY_EDITOR
                            currLoadAudit = new LoadAudit(LoadReason.LoadPreviouslyWritten, resourceData.firstUsePassID);
#endif
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
                                    currAttachment.loadAction = UnityEngine.Rendering.RenderBufferLoadAction.Clear;
#if UNITY_EDITOR
                                    currLoadAudit = new LoadAudit(LoadReason.ClearImported);
#endif
                                }
                                else
                                {
                                    currAttachment.loadAction = UnityEngine.Rendering.RenderBufferLoadAction.Load;
#if UNITY_EDITOR
                                    currLoadAudit = new LoadAudit(LoadReason.LoadImported);
#endif
                                }
                            }
                            else
                            {
                                // Created by the graph internally clear on first read
                                currAttachment.loadAction = UnityEngine.Rendering.RenderBufferLoadAction.Clear;
#if UNITY_EDITOR
                                currLoadAudit = new LoadAudit(LoadReason.ClearCreated);
#endif
                            }
                        }
                    }

                    if (fragment.accessFlags.HasFlag(AccessFlags.Write))
                    {
                        // Simple non-msaa case
                        if (nativePass.samples <= 1)
                        {
                            // The resource is still used after this renderpass so we need to store it imported resources always need to be sored
                            // as we don't know what happens with them and assume the contents are somewhow used outside the graph
                            int destroyPassID = resourceData.lastUsePassID;
                            if (destroyPassID >= nativePass.firstGraphPass + nativePass.numGraphPasses)
                            {
                                currAttachment.storeAction = RenderBufferStoreAction.Store;
#if UNITY_EDITOR
                                currStoreAudit = new StoreAudit(StoreReason.StoreUsedByLaterPass, destroyPassID);
#endif
                            }
                            // It's last used during this native pass just discard it unless it's imported in which case we need to store
                            else
                            {
                                if (isImported)
                                {
                                    if (resourceData.discard)
                                    {
                                        currAttachment.storeAction = RenderBufferStoreAction.DontCare;
#if UNITY_EDITOR
                                        currStoreAudit = new StoreAudit(StoreReason.DiscardImported);
#endif
                                    }
                                    else
                                    {
                                        currAttachment.storeAction = RenderBufferStoreAction.Store;
#if UNITY_EDITOR
                                        currStoreAudit = new StoreAudit(StoreReason.StoreImported);
#endif
                                    }
                                }
                                else
                                {
                                    currAttachment.storeAction = RenderBufferStoreAction.DontCare;
#if UNITY_EDITOR
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

                            int destroyPassID = resourceData.lastUsePassID;
                            currAttachment.storeAction = RenderBufferStoreAction.DontCare;

                            //Check if we're the last pass writing it by checking the output version of the current pass is the higherst version the resource will reach
                            bool lastWriter = (resourceData.latestVersionNumber == fragment.resource.version); // Cheaper but same? = resourceData.lastWritePassID >= pass.firstGraphPass && resourceData.lastWritePassID < pass.firstGraphPass + pass.numSubPasses;
                            bool isImportedLastWriter = isImported && lastWriter;

                            // Used outside this native render pass, we need to store something
                            if (destroyPassID >= nativePass.firstGraphPass + nativePass.numGraphPasses)
                            {
                                // Assume nothing is needed unless we are an imported texture (which doesn't require discarding) and we're the last ones writing it
                                bool needsMSAASamples = isImportedLastWriter && !resourceData.discard;
                                bool needsResolvedData = isImportedLastWriter && (resourceData.bindMS == false);//bindMS never resolves
                                int userPassID = 0;
                                int msaaUserPassID = 0;

                                // Check if we need msaa/resolved data by checking all the passes using this buffer
                                // Partial writes will register themselves as readers so this should be adequate
                                foreach (ref readonly var reader in contextData.Readers(fragment.resource))
                                {
                                    bool isFragment = contextData.passData.ElementAt(reader.passId).IsUsedAsFragment(fragment.resource, contextData);
                                    // A fragment attachment use we need the msaa samples
                                    if (isFragment)
                                    {
                                        needsMSAASamples = true;
                                        msaaUserPassID = reader.passId;
                                    }
                                    else
                                    {
                                        // Used as a mutlisample-texture we need the msaa samples
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
                                    currAttachment.storeAction = RenderBufferStoreAction.StoreAndResolve;
#if UNITY_EDITOR
                                    currStoreAudit = new StoreAudit(
                                        (isImportedLastWriter) ? StoreReason.StoreImported : StoreReason.StoreUsedByLaterPass, userPassID,
                                        (isImportedLastWriter) ? StoreReason.StoreImported : StoreReason.StoreUsedByLaterPass, msaaUserPassID);
#endif
                                }
                                else if (needsResolvedData)
                                {
                                    currAttachment.storeAction = RenderBufferStoreAction.Resolve;
#if UNITY_EDITOR
                                    currStoreAudit = new StoreAudit((isImportedLastWriter) ? StoreReason.StoreImported : StoreReason.StoreUsedByLaterPass, userPassID, StoreReason.DiscardUnused);
#endif
                                }
                                else if (needsMSAASamples)
                                {
                                    currAttachment.storeAction = RenderBufferStoreAction.Store;
#if UNITY_EDITOR
                                    currStoreAudit = new StoreAudit(
                                        (resourceData.bindMS) ? StoreReason.DiscardBindMs : StoreReason.DiscardUnused, -1,
                                        (isImportedLastWriter) ? StoreReason.StoreImported : StoreReason.StoreUsedByLaterPass, msaaUserPassID);
#endif
                                }
                                else
                                {
                                    Debug.Assert(false, "Resource was not destroyed but nobody seems to be using it!??");
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
                                        currAttachment.storeAction = RenderBufferStoreAction.DontCare;
#if UNITY_EDITOR
                                        currStoreAudit = new StoreAudit(StoreReason.DiscardImported);
#endif
                                    }
                                    else
                                    {
                                        currAttachment.storeAction = RenderBufferStoreAction.Store;
#if UNITY_EDITOR
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
                                        currAttachment.storeAction = RenderBufferStoreAction.DontCare;
#if UNITY_EDITOR
                                        currStoreAudit = new StoreAudit(
                                            StoreReason.DiscardImported, -1, StoreReason.DiscardImported);
#endif
                                    }
                                    else
                                    {
                                        currAttachment.storeAction = RenderBufferStoreAction.StoreAndResolve;
#if UNITY_EDITOR
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
                        currAttachment.memoryless = true;
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                        // Ensure load/store actions are actually valid for memory less
                        if (currAttachment.loadAction == RenderBufferLoadAction.Load) throw new Exception("Resource was marked as memoryless but is trying to load.");
                        if (currAttachment.storeAction != RenderBufferStoreAction.DontCare) throw new Exception("Resource was marked as memoryless but is trying to store or resolve.");
#endif
                    }
                }
            }
        }

        internal unsafe void ExecuteBeginRenderPass(
            InternalRenderGraphContext rgContext,
            RenderGraphResourceRegistry resources,
            ref NativePassData nativePass)
        {
            ref var attachments = ref nativePass.attachments;
            var attachmentCount = attachments.size;
            ref readonly var firstGraphPass = ref contextData.passData.ElementAt(nativePass.firstGraphPass);
            var w = firstGraphPass.fragmentInfoWidth;
            var h = firstGraphPass.fragmentInfoHeight;
            var d = firstGraphPass.fragmentInfoVolumeDepth;
            var s = firstGraphPass.fragmentInfoSamples;
            ref var passes = ref contextData.nativeSubPassData;

            passNamesForDebug.Clear();

#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (nativePass.attachments.size == 0 || nativePass.numNativeSubPasses == 0)
                throw new Exception("Empty render pass");

            nativePass.GetPassNames(contextData, passNamesForDebug);

            if (w == 0 || h == 0 || d == 0 || s == 0 || nativePass.numNativeSubPasses == 0 || attachmentCount == 0)
            {
                throw new Exception("Invalid render pass properties. One or more properties are zero.");
            }
#endif

            NativeArray<SubPassDescriptor> subpass = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<SubPassDescriptor>(passes.GetUnsafeReadOnlyPtr<SubPassDescriptor>() + nativePass.firstNativeSubPass, nativePass.numNativeSubPasses, Allocator.None);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            var safetyHandle = AtomicSafetyHandle.Create();
            AtomicSafetyHandle.SetAllowReadOrWriteAccess(safetyHandle, true);
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref subpass, safetyHandle);
#endif

            if (nativePass.hasFoveatedRasterization)
            {
                rgContext.cmd.SetFoveatedRenderingMode(FoveatedRenderingMode.Enabled);
            }

            var depthIDx = nativePass.hasDepth ? 0 : -1;
            var att = new NativeArray<AttachmentDescriptor>(attachmentCount, Allocator.Temp);
            for (var i = 0; i < attachmentCount; ++i)
            {
                var idx = attachments[i].handle.index;
                resources.GetRenderTargetInfo(attachments[i].handle, out var renderTargetInfo);
                var ad = new AttachmentDescriptor(renderTargetInfo.format);

                // Set up the RT pointers
                if (attachments[i].memoryless == false)
                {
                    var rthandle = resources.GetTexture(new TextureHandle(attachments[i].handle));

                    //HACK: Always set the loadstore target even if StoreAction == DontCare or Resolve
                    //and LoadAction == Clear or DontCare
                    //in these cases you could argue setting the loadStoreTarget to NULL and only set the resolveTarget
                    //but this confuses the backend (on vulkan) and in general is not how the lower level APIs tend to work.
                    //because of the RenderTexture duality where we always bundle store+resolve targets as one RTex
                    //it does become impossible to have a memoryless loadStore texture with a memoryfull resolve
                    //but that is why we mark this as a hack and future work to fix.
                    //The proper (and planned) solution would be to move away from the render texture duality.
                    RenderTargetIdentifier rtidAllSlices = rthandle;
                    rtidAllSlices = new RenderTargetIdentifier(rtidAllSlices, 0, CubemapFace.Unknown, -1);
                    ad.loadStoreTarget = rtidAllSlices;

                    if (attachments[i].storeAction == RenderBufferStoreAction.Resolve ||
                        attachments[i].storeAction == RenderBufferStoreAction.StoreAndResolve)
                    {
                        ad.resolveTarget = rthandle;
                    }
                }
                // In the memoryless case it's valid to not set both loadStoreTarget/and resolveTarget as the backend will allocate a transient one

                ad.loadAction = attachments[i].loadAction;
                ad.storeAction = attachments[i].storeAction;

                // Set up clear colors if we have a clear load action
                if (attachments[i].loadAction == RenderBufferLoadAction.Clear)
                {
                    ad.clearColor = Color.red;
                    ad.clearDepth = 1.0f;
                    ad.clearStencil = 0;
                    var desc = resources.GetTextureResourceDesc(attachments[i].handle);
                    if (i == 0 && nativePass.hasDepth)
                    {
                        // TODO: There seems to be no clear depth specified ?!?!
                        ad.clearDepth = 1.0f;// desc.clearDepth;
                    }
                    else
                    {
                        ad.clearColor = desc.clearColor;
                    }
                }
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                if (renderTargetInfo.width != w || renderTargetInfo.height != h || renderTargetInfo.msaaSamples != s)
                {
                    throw new Exception("Low level rendergraph error: Attachments in renderpass do not match!");
                }
#endif
                att[i] = ad;
            }

            int utf8CStrDebugNameLength = 0;
            foreach (ref readonly Name passName in passNamesForDebug)
            {
                utf8CStrDebugNameLength += passName.utf8ByteCount + 1; // +1 to add '/' between passes or the null terminator at the end
            }

            var nameBytes = stackalloc byte[utf8CStrDebugNameLength];
            if (utf8CStrDebugNameLength > 0)
            {
                int startStr = 0;
                foreach (ref readonly var passName in passNamesForDebug)
                {
                    int strByteCount = passName.utf8ByteCount;
                    System.Text.Encoding.UTF8.GetBytes(passName.name.AsSpan(), new Span<byte>(nameBytes + startStr, strByteCount));
                    startStr += strByteCount;
                    // Adding '/' in UTF8
                    nameBytes[startStr++] = (byte)(0x2F);
                }
                // Rewriting last '/' to be the null terminator
                nameBytes[utf8CStrDebugNameLength - 1] = (byte)0;
            }

            var debugNameValue = new ReadOnlySpan<byte>(nameBytes, utf8CStrDebugNameLength);

            rgContext.cmd.BeginRenderPass(w, h, d, s, att, depthIDx, subpass, debugNameValue);

            att.Dispose();
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.Release(safetyHandle);
#endif
            CommandBuffer.ThrowOnSetRenderTarget = true;
        }

        const int ArbitraryMaxNbMergedPasses = 16;
        DynamicArray<Name> passNamesForDebug = new DynamicArray<Name>(ArbitraryMaxNbMergedPasses);

        private void ExecuteDestroyResource(InternalRenderGraphContext rgContext, RenderGraphResourceRegistry resources, ref PassData pass)
        {
            using (new ProfilingScope(ProfilingSampler.Get(NativeCompilerProfileId.NRPRGComp_ExecuteDestroyCommands)))
            {
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
                                if (resInfo.isImported == false && resInfo.memoryLess == false)
                                {
                                    resources.ReleasePooledResource(rgContext, res);
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
                            resources.ReleasePooledResource(rgContext, destroy);
                        }
                    }
                }
            }
        }

        internal unsafe void SetRandomWriteTarget(in CommandBuffer cmd, RenderGraphResourceRegistry resources, int index, ResourceHandle resource, bool preserveCounterValue = true)
        {
            if (resource.type == RenderGraphResourceType.Texture)
            {
                var tex = resources.GetTexture(new TextureHandle(resource));
                cmd.SetRandomWriteTarget(index, tex);
            }
            else if (resource.type == RenderGraphResourceType.Buffer)
            {
                var buff = resources.GetBuffer(new BufferHandle(resource));
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
                throw new Exception($"Invalid resource type {resource.type}, expected texture or buffer");
            }
        }

        internal void ExecuteGraphNode(ref InternalRenderGraphContext rgContext, RenderGraphResourceRegistry resources, RenderGraphPass pass)
        {
#if THROW_ON_SETRENDERTARGET_DEBUG
            if (pass.type == RenderGraphPassType.Raster)
            {
                CommandBuffer.ThrowOnSetRenderTarget = true;
            }
#endif
            try
            {
                rgContext.executingPass = pass;

                if (!pass.HasRenderFunc())
                {
                    throw new InvalidOperationException(string.Format("RenderPass {0} was not provided with an execute function.", pass.name));
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
            finally
            {
#if THROW_ON_SETRENDERTARGET_DEBUG
                if (pass.type == RenderGraphPassType.Raster)
                {
                    CommandBuffer.ThrowOnSetRenderTarget = false;
                }
#endif
            }
        }

        public void ExecuteGraph(InternalRenderGraphContext rgContext, RenderGraphResourceRegistry resources, in List<RenderGraphPass> passes)
        {
            using (new ProfilingScope(ProfilingSampler.Get(NativeCompilerProfileId.NRPRGComp_ExecutePassCommandBuffer)))
            {
                bool inRenderPass = false;
                previousCommandBuffer = rgContext.cmd;

                for (int passIndex = 0; passIndex < contextData.passData.Length; passIndex++)
                {
                    ref var pass = ref contextData.passData.ElementAt(passIndex);

                    //Fix low level passes being merged into nrp giving errors
                    //because of the "isRaster" check below

                    if (pass.culled)
                        continue;

                    var isRaster = pass.type == RenderGraphPassType.Raster;

                    ExecuteCreateRessource(rgContext, resources, pass);

                    var isAsyncCompute = pass.type == RenderGraphPassType.Compute && pass.asyncCompute == true;
                    if (isAsyncCompute)
                    {
                        if (rgContext.contextlessTesting == false)
                            rgContext.renderContext.ExecuteCommandBuffer(rgContext.cmd);
                        rgContext.cmd.Clear();

                        var asyncCmd = CommandBufferPool.Get("async cmd");
                        asyncCmd.SetExecutionFlags(CommandBufferExecutionFlags.AsyncCompute);
                        rgContext.cmd = asyncCmd;
                    }

                    // also make sure to insert fence=waits for multiple queue syncs
                    if (pass.waitOnGraphicsFencePassId != -1)
                    {
                        var fence = contextData.fences[pass.waitOnGraphicsFencePassId];
                        rgContext.cmd.WaitOnAsyncGraphicsFence(fence);
                    }

                    var nrpBegan = false;
                    if (isRaster == true && pass.mergeState <= PassMergeState.Begin)
                    {
                        if (pass.nativePassIndex >= 0)
                        {
                            ref var nativePass = ref contextData.nativePassData.ElementAt(pass.nativePassIndex);
                            if (nativePass.fragments.size > 0)
                            {
                                ExecuteBeginRenderPass(rgContext, resources, ref nativePass);
                                nrpBegan = true;
                                inRenderPass = true;
                            }
                        }
                    }

                    if (pass.mergeState >= PassMergeState.SubPass)
                    {
                        if (pass.beginNativeSubpass)
                        {
                            if (!inRenderPass)
                            {
                                throw new Exception("Compiler error: Pass is marked as beginning a native sub pass but no pass is currently active.");
                            }
                            rgContext.cmd.NextSubPass();
                        }
                    }

                    if (pass.numRandomAccessResources > 0)
					{
                        //TODO: are we that paranoid? maybe clean it once at the beginning of the graph?
                        rgContext.cmd.ClearRandomWriteTargets();
                        foreach (var randomWriteAttachment in pass.RandomWriteTextures(contextData))
                        if (pass.numRandomAccessResources > 0)
                    	{
                            SetRandomWriteTarget(rgContext.cmd, resources, randomWriteAttachment.index, randomWriteAttachment.resource);
                    	}
					}

#if THROW_ON_SETRENDERTARGET_DEBUG
                    if (passes[pass.passId].type == RenderGraphPassType.Raster)
                    {
                        CommandBuffer.ThrowOnSetRenderTarget = true;
                    }
#endif

                    ExecuteGraphNode(ref rgContext, resources, passes[pass.passId]);

                    // should we insert a fence to sync between difference queues?
                    if (pass.insertGraphicsFence)
                    {
                        var fence = rgContext.cmd.CreateAsyncGraphicsFence();
                        contextData.fences[pass.passId] = fence;
                    }

                    if (isRaster)
                    {
                        if ((pass.mergeState == PassMergeState.None && nrpBegan)
                            || pass.mergeState == PassMergeState.End)
                        {
                            if (pass.nativePassIndex >= 0)
                            {
                                ref var nativePass = ref contextData.nativePassData.ElementAt(pass.nativePassIndex);
                                if (nativePass.fragments.size > 0)
                                {
                                    if (!inRenderPass)
                                    {
                                        throw new Exception("Compiler error: Generated a subpass pass but no pass is currently active.");
                                    }

                                    if (nativePass.hasFoveatedRasterization)
                                    {
                                        rgContext.cmd.SetFoveatedRenderingMode(FoveatedRenderingMode.Disabled);
                                    }

                                    rgContext.cmd.EndRenderPass();
                                    CommandBuffer.ThrowOnSetRenderTarget = false;
                                    inRenderPass = false;
                                }
                            }
                        }
                    }
                    else if (isAsyncCompute)
                    {
                        rgContext.renderContext.ExecuteCommandBufferAsync(rgContext.cmd, ComputeQueueType.Background);
                        CommandBufferPool.Release(rgContext.cmd);
                        rgContext.cmd = previousCommandBuffer;
                    }

                    ExecuteDestroyResource(rgContext, resources, ref pass);
                }
            }
        }
    }
}
