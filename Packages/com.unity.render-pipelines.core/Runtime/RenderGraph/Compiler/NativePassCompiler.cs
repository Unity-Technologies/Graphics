using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine.Rendering;

// Typedef for the in-engine RendererList API (to avoid conflicts with the experimental version)
using CoreRendererListDesc = UnityEngine.Rendering.RendererUtils.RendererListDesc;

namespace UnityEngine.Experimental.Rendering.RenderGraphModule.NativeRenderPassCompiler
{
    internal partial class NativePassCompiler
    {
        internal struct RenderGraphInputInfo
        {
            public RenderGraphResourceRegistry m_ResourcesForDebugOnly;
            public List<RenderGraphPass> m_RenderPasses;
            public bool disableCulling;
            public string debugName;
        }

        internal RenderGraphInputInfo graph;
        internal CompilerContextData contextData;
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
            NRPRGComp_GenBeginRenderpassCommand,
            NRPRGComp_GenPassCommandBuffer,
            NRPRGComp_GenCreateCommands,
            NRPRGComp_GenDestroyCommands
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

            // Build up the context graph and keep track of nodes we encounter that can't be culled
            using (new ProfilingScope(ProfilingSampler.Get(NativeCompilerProfileId.NRPRGComp_BuildGraph)))
            {
                for (int passId = 0; passId < passes.Count; passId++)
                {
                    if (passes[passId].type == RenderGraphPassType.Legacy)
                    {
                        throw new Exception("Pass '" + passes[passId].name + "' is using the legacy rendergraph API. You cannot use legacy passes with the native render pass compiler. Please do not use AddPass on the rendergrpah but use one of the more specific pas types such as AddRasterPass");
                    }

                    ctx.passData.Add(new PassData(ref passes, passId));
                    int addIdx = ctx.passData.LastIndex();
                    Debug.Assert(addIdx == passId);
                    ctx.passNames.Add(new Name(passes[passId].name));

                    ref var pass = ref ctx.passData.ElementAt(passId);

                    if (pass.hasSideEffects)
                    {
                        toVisitPassIds.Push(passId);
                    }

                    // Set up the list of fragment attachments for this pass
                    // Note: This doesn't set up the resource reader/writer list as the fragment attachments
                    // will also be in the pass read/write lists accordingly
                    if (pass.type == RenderGraphPassType.Raster)
                    {
                        // Grab offset in context fragment list to begin building the fragment list
                        pass.firstFragment = ctx.fragmentData.Length;

                        // Depth attachment is always at index 0
                        if (passes[passId].depthBuffer.handle.IsValid())
                        {
                            pass.fragmentInfoHasDepth = true;

                            var resource = passes[passId].depthBuffer.handle;
                            if (ctx.AddToFragmentList(resource, passes[passId].depthBufferAccessFlags, pass.firstFragment, pass.numFragments))
                            {
                                pass.AddFragment(resource, ctx);
                            }
                        }

                        for (var ci = 0; ci < passes[passId].colorBufferMaxIndex + 1; ++ci)
                        {
                            // Skip unused color slots
                            if (!passes[passId].colorBuffers[ci].handle.IsValid()) continue;

                            var resource = passes[passId].colorBuffers[ci].handle;
                            if (ctx.AddToFragmentList(resource, passes[passId].colorBufferAccessFlags[ci], pass.firstFragment, pass.numFragments))
                            {
                                pass.AddFragment(resource, ctx);
                            }
                        }

                        // Grab offset in context fragment list to begin building the fragment input list
                        pass.firstFragmentInput = ctx.fragmentData.Length;

                        for (var ci = 0; ci < passes[passId].fragmentInputMaxIndex + 1; ++ci)
                        {
                            // Skip unused fragment input slots
                            if (!passes[passId].fragmentInputs[ci].handle.IsValid()) continue;

                            var resource = passes[passId].fragmentInputs[ci].handle;
                            if (ctx.AddToFragmentList(resource, passes[passId].fragmentInputAccessFlags[ci], pass.firstFragmentInput, pass.numFragmentInputs))
                            {
                                pass.AddFragmentInput(resource, ctx);
                            }
                        }

                        // Allocate a stand-alone native renderpass for each rasterpass that actually renders something for now
                        if (pass.numFragments > 0)
                        {
                            ctx.nativePassData.Add(new NativePassData(ref pass, ctx));
                            pass.nativePassIndex = ctx.nativePassData.LastIndex();
                        }
                        else
                        {
                            // Not sure what this would mean, you're reading fragment inputs but not outputting anything???
                            // could happen in if you're trying to write to a raw buffer instead of pixel output?!? anyhow
                            // this sounds dodgy so assert for now until we clear this one out
                            Debug.Assert(pass.numFragmentInputs == 0);
                        }

                    }

                    // Set up per resource type read/write lists for this pass
                    pass.firstInput = ctx.inputData.Length; // Grab offset in context input list
                    pass.firstOutput = ctx.outputData.Length; // Grab offset in context output list
                    for (int type = 0; type < (int)RenderGraphResourceType.Count; ++type)
                    {
                        var resourceWrite = passes[passId].resourceWriteLists[type];
                        var resourceWriteCount = resourceWrite.Count;
                        for (var i = 0; i < resourceWriteCount; ++i)
                        {
                            var resource = resourceWrite[i];

                            // Writing to an imported resource is a side effect so mark the pass if needed
                            ref var resData = ref ctx.UnversionedResourceData(resource);
                            if (resData.isImported)
                            {
                                if (pass.hasSideEffects == false)
                                {
                                    pass.hasSideEffects = true;
                                    toVisitPassIds.Push(passId);
                                }
                            }

                            // Mark this pass as writing to this version of the resource
                            ctx.resources[resource].SetWritingPass(ctx, resource, passId, pass.numOutputs);

                            ctx.outputData.Add(new PassOutputData
                            {
                                resource = resource,
                            });

                            pass.numOutputs++;
                        }

                        var resourceRead = passes[passId].resourceReadLists[type];
                        var resourceReadCount = resourceRead.Count;
                        for (var i = 0; i < resourceReadCount; ++i)
                        {
                            var resource = resourceRead[i];

                            // Mark this pass as reading from this version of the resource
                            ctx.resources[resource].RegisterReadingPass(ctx, resource, passId, pass.numInputs);

                            ctx.inputData.Add(new PassInputData
                            {
                                resource = resource,
                            });

                            pass.numInputs++;
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
                // Flood fill downstream to sources to all nodes starting from the pinned nodes
                // this will mark nodes that have their output not used as culled (but not the dependencies yet)
                ctx.TagAll(0);
                while (toVisitPassIds.Count != 0)
                {
                    int passId = toVisitPassIds.Pop();

                    ref var passData = ref ctx.passData.ElementAt(passId);

                    // We already got to this node through another dependency chain
                    if (passData.tag != 0) continue;

                    // Flow upstream from this node
                    foreach (ref readonly var input in passData.Inputs(ctx))
                    {
                        int inputPassIndex = ctx.resources[input.resource].writePass;
                        toVisitPassIds.Push(inputPassIndex);
                    }

                    // Mark this node as visited
                    passData.tag = 1;
                }

                // Update graph based on freshly culled nodes, remove any connections
                var numPasses = ctx.passData.Length;
                for (int passIndex = 0; passIndex < numPasses; passIndex++)
                {
                    ref var pass = ref ctx.passData.ElementAt(passIndex);
                    // mark any unvisited passes as culled
                    pass.culled = !graph.disableCulling && (pass.tag == 0);

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

                // loop over the non-culled nodes and find those that have outputs that are never used (because the above loop removed them)
                // cull those nodes (and restart the process since culling a node might trigger a different node to get culled)
                var atLeastOne = true;
                while (atLeastOne)
                {
                    atLeastOne = false;
                    for (int passIndex = 0; passIndex < numPasses; passIndex++)
                    {
                        ref var pass = ref ctx.passData.ElementAt(passIndex);
                        if (pass.culled || pass.hasSideEffects)
                            continue;

                        var noReaders = true;
                        foreach (ref readonly var output in pass.Outputs(ctx))
                        {
                            var outputResource = output.resource;
                            if (ctx.resources[outputResource].numReaders != 0)
                            {
                                noReaders = false;
                                break;
                            }
                        }

                        if (noReaders == true)
                        {
                            atLeastOne = true;
                            pass.culled = true;
                            foreach (ref readonly var input in pass.Inputs(ctx))
                            {
                                var inputResource = input.resource;
                                ctx.resources[inputResource].RemoveReadingPass(ctx, inputResource, pass.passId);
                            }
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

                    // If we're culled just ignore it
                    if (passToAdd.culled)
                    {
                        continue;
                    }

                    // Check if no pass is active currently, in that case here is nothing to merge really...
                    if (activeNativePassId == -1)
                    {
                        //If raster, start a new native pass with the current pass
                        //if non-raster just continue without an active pass.
                        if (passToAdd.type == RenderGraphPassType.Raster)
                        {

                            activeNativePassId = passToAdd.nativePassIndex;
                        }
                        continue;
                    }

                    // There is an native pass currently open try to add ths graph pass to it
                    Debug.Assert(activeNativePassId >= 0);
                    var testResult = NativePassData.TryMerge(contextData, activeNativePassId, passIdx, false);

                    // Merging failed close and create a new renderpass
                    if (testResult.reason != PassBreakReason.Merged)
                    {
                        SetPassStatesForNativePass(activeNativePassId);
                        ref var nativePassData = ref contextData.nativePassData.ElementAt(activeNativePassId);
                        nativePassData.breakAudit = testResult;

                        if (testResult.reason == PassBreakReason.NonRasterPass)
                        {
                            // non-raster pass no native pass it active at all
                            activeNativePassId = -1;
                        }
                        else
                        {
                            // Raster but cannot be merged, start a new native pass with the current pass
                            activeNativePassId = passToAdd.nativePassIndex;
                        }
                        continue;
                    }
                }

                if (activeNativePassId >= 0)
                {
                    // "Close" the last native pass by marking the last graph pass as end
                    SetPassStatesForNativePass(activeNativePassId);
                    ref var nativePassData = ref contextData.nativePassData.ElementAt(activeNativePassId);
                    nativePassData.breakAudit = new PassBreakAudit(PassBreakReason.EndOfGraph, -1);
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

                // TODO: I have a feeling this can be done more optimally by walking the graph instead of loping over all the pases twice
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

                        // If we use version 0 and nobody else is using it yet mark this pass
                        // as the first using the resource. It can happen that two passes use v0
                        // E.g.
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
                    //Because it isn't read it won't be in the inputs array with V0

                    foreach (ref readonly var output in pass.Outputs(ctx))
                    {
                        var outputResource = output.resource;
                        ref var pointTo = ref ctx.UnversionedResourceData(outputResource);
                        if (outputResource.version == 1)
                        {
                            // If we use version 0 and nobody else is using it yet mark this pass
                            // as the first using the resource. It can happen that two passes use v0
                            // E.g.
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
                            ref var wPass = ref ctx.passData.ElementAt(pointToVer.writePass);
                            if (wPass.asyncCompute != pass.asyncCompute)
                            {
                                pass.waitOnGraphicsFencePassId = wPass.passId;
                            }
                        }
                    }

                    // We're outputting a resource that is never used.
                    // This can happen if this pass has multiple outputs and some of them are used and some not
                    // because some are used the whole pass it not culled but the unused output still should be freed
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
                                // Note: You could think but what if the texture is used as a regular non-frambuffer attachment texture
                                // surely it can't be memoryless then?
                                // That is true, but it can never happen as the fact these passes got merged means the textures cannot be used
                                // as regular textures halfway through a pass. If that were the case they would never have been merged in the first place.

                                // Check if it is in the destroy list of any of the subpasses > if yes > memoryless
                                foreach (ref readonly var subPass2 in graphPasses)
                                {
                                    foreach (ref readonly var destroyedRes in subPass2.LastUsedResources(contextData))
                                    {
                                        ref var destInfo = ref contextData.UnversionedResourceData(destroyedRes);
                                        if (destroyedRes.type == RenderGraphResourceType.Texture && destInfo.isImported == false)
                                        {
                                            if (createdRes.index == destroyedRes.index)
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

        DynamicString debugName = new DynamicString(1024);

        private bool IsSameNativeSubPass(ref SubPassDescriptor a, ref SubPassDescriptor b)
        {
            if (a.flags != b.flags)
            {
                return false;
            }

            if (a.colorOutputs.Length != b.colorOutputs.Length)
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

            if (a.inputs.Length != b.inputs.Length)
            {
                return false;
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

        private void GenerateBeginRenderPassCommand<T>(ref NativePassData nativePass, ref PassCommandBuffer<T> passCmdBuffer) where T : IExecutionPolicy
        {
            using (new ProfilingScope(ProfilingSampler.Get(NativeCompilerProfileId.NRPRGComp_GenBeginRenderpassCommand)))
            {
                // Some passes don't do any rendering so just skip them
                // E.g. the "SetShadowGlobals" pass
                ref readonly var firstGraphPass = ref contextData.passData.ElementAt(nativePass.firstGraphPass);
                ref readonly var lastGraphPass = ref contextData.passData.ElementAt(nativePass.firstGraphPass + nativePass.numGraphPasses - 1);

                // This can happen when we merge a number of raster passes that all have no rendertargets
                // e.g. a number of "set global" passes
                if (nativePass.fragments.size <= 0)
                {
                    return;
                }

                // Some sanity checks, these should not happen
                Debug.Assert(firstGraphPass.mergeState == PassMergeState.Begin || firstGraphPass.mergeState == PassMergeState.None);
                Debug.Assert(lastGraphPass.mergeState == PassMergeState.End || lastGraphPass.mergeState == PassMergeState.None);

                ref readonly var fragmentList = ref nativePass.fragments;
                var numFragments = nativePass.fragments.size;
                var numMSAASamples = nativePass.samples;
                var hasDepth = nativePass.hasDepth;

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

                    // We have no output attachments this is an "empty" raster pass doing only non-rendering command so skip it.
                    if (graphPass.numFragments == 0)
                    {
                        // We always merge it into the currently active
                        graphPass.nativeSubPassIndex = nativePass.numNativeSubPasses - 1;
                        graphPass.beginNativeSubpass = false;
                        continue;
                    }

                    // MRT attachments
                    {
                        int fragmentIdx = 0;
                        int colorOffset = (graphPass.fragmentInfoHasDepth) ? -1 : 0;

                        desc.colorOutputs = new AttachmentIndexArray(graphPass.numFragments + colorOffset);

                        foreach (ref readonly var fragment in graphPass.Fragments(contextData))
                        {
                            // Check if we're handling the depth atachment
                            if (graphPass.fragmentInfoHasDepth && fragmentIdx == 0)
                            {
                                desc.flags = (fragment.accessFlags.HasFlag(IBaseRenderGraphBuilder.AccessFlags.Write)) ? SubPassFlags.None : SubPassFlags.ReadOnlyDepth;
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

                        graphPass.nativeSubPassIndex = nativePass.numNativeSubPasses - 1;
                        graphPass.beginNativeSubpass = true;
                    }
                    else
                    {
                        graphPass.nativeSubPassIndex = nativePass.numNativeSubPasses - 1;
                        graphPass.beginNativeSubpass = false;
                    }
                }

                // determine load store actions
                // This pass also contains the latest versions used within this pass
                // As we have no pass reordering for now the merged passes are always a consecutive list and we can simply do a range
                // check on the create/destroy passid to see if it's allocated/freed in this native renderpass
                for (int fragmentId = 0; fragmentId < fragmentList.size; ++fragmentId)
                {
                    ref readonly var fragment = ref fragmentList[fragmentId];
                    int idx = nativePass.attachments.Add(new NativePassAttachment());
                    nativePass.loadAudit.Add(new LoadAudit(LoadReason.FullyRewritten));
                    nativePass.storeAudit.Add(new StoreAudit(StoreReason.DiscardUnused));

                    nativePass.attachments[idx].handle = fragment.resource;

                    // Don't care by default
                    nativePass.attachments[idx].loadAction = UnityEngine.Rendering.RenderBufferLoadAction.DontCare;
                    nativePass.attachments[idx].storeAction = UnityEngine.Rendering.RenderBufferStoreAction.DontCare;

                    // Writing by-default has to preserve the contents, think rendering only a few small triangles on top of a big framebuffer
                    // So it means we need to load/clear contents potentially.
                    // If a user pass knows it will write all pixels in a buffer (like a blit) it can use the WriteAll/Discard usage to indicate this to the graph
                    bool partialWrite = fragment.accessFlags.HasFlag(IBaseRenderGraphBuilder.AccessFlags.Write) && !fragment.accessFlags.HasFlag(IBaseRenderGraphBuilder.AccessFlags.Discard);

                    var resourceData = contextData.UnversionedResourceData(fragment.resource);
                    bool isImported = resourceData.isImported;

                    if (fragment.accessFlags.HasFlag(IBaseRenderGraphBuilder.AccessFlags.Read) || partialWrite)
                    {
                        // The resource is already allocated before this pass so we need to load it
                        if (resourceData.firstUsePassID < nativePass.firstGraphPass)
                        {
                            nativePass.attachments[idx].loadAction = UnityEngine.Rendering.RenderBufferLoadAction.Load;
                            nativePass.loadAudit[idx] = new LoadAudit(LoadReason.LoadPreviouslyWritten, resourceData.firstUsePassID);
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
                                    nativePass.attachments[idx].loadAction = UnityEngine.Rendering.RenderBufferLoadAction.Clear;
                                    nativePass.loadAudit[idx] = new LoadAudit(LoadReason.ClearImported);
                                }
                                else
                                {
                                    nativePass.attachments[idx].loadAction = UnityEngine.Rendering.RenderBufferLoadAction.Load;
                                    nativePass.loadAudit[idx] = new LoadAudit(LoadReason.LoadImported);
                                }
                            }
                            else
                            {
                                // Created by the graph internally clear on first read
                                nativePass.attachments[idx].loadAction = UnityEngine.Rendering.RenderBufferLoadAction.Clear;
                                nativePass.loadAudit[idx] = new LoadAudit(LoadReason.ClearCreated);
                            }
                        }
                    }

                    if (fragment.accessFlags.HasFlag(IBaseRenderGraphBuilder.AccessFlags.Write))
                    {
                        // Simple non-msaa case
                        if (numMSAASamples <= 1)
                        {
                            // The resource is still used after this renderpass so we need to store it imported resources always need to be sored
                            // as we don't know what happens with them and assume the contents are somewhow used outside the graph
                            int destroyPassID = resourceData.lastUsePassID;
                            if (destroyPassID >= nativePass.firstGraphPass + nativePass.numGraphPasses)
                            {
                                nativePass.attachments[idx].storeAction = RenderBufferStoreAction.Store;
                                nativePass.storeAudit[idx] = new StoreAudit(StoreReason.StoreUsedByLaterPass, destroyPassID);
                            }
                            // It's last used during this native pass just discard it unless it's imported in which case we need to store
                            else
                            {
                                if (isImported)
                                {
                                    if (resourceData.discard)
                                    {
                                        nativePass.attachments[idx].storeAction = RenderBufferStoreAction.DontCare;
                                        nativePass.storeAudit[idx] = new StoreAudit(StoreReason.DiscardImported);
                                    }
                                    else
                                    {
                                        nativePass.attachments[idx].storeAction = RenderBufferStoreAction.Store;
                                        nativePass.storeAudit[idx] = new StoreAudit(StoreReason.StoreImported);
                                    }
                                }
                                else
                                {
                                    nativePass.attachments[idx].storeAction = RenderBufferStoreAction.DontCare;
                                    nativePass.storeAudit[idx] = new StoreAudit(StoreReason.DiscardUnused);
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
                            //graph.m_Resources.GetRenderTargetInfo(fragment.resource, out var info);
                            nativePass.attachments[idx].storeAction = RenderBufferStoreAction.DontCare;


                            //Check if we're the last pass writing it by checking the output version of the current pass is the higherst version the resource will reach
                            bool lastWriter = (resourceData.latestVersionNumber == fragment.resource.version); // Cheaper but same? = resourceData.lastWritePassID >= pass.firstGraphPass && resourceData.lastWritePassID < pass.firstGraphPass + pass.numSubPasses;
                            bool isImportedLastWriter = isImported && lastWriter;

                            // Used outside this native render pass we need to store something
                            if (destroyPassID >= nativePass.firstGraphPass + nativePass.numGraphPasses)
                            {
                                //var desc = graph.m_Resources.GetTextureResourceDesc(fragment.resource);

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
                                    nativePass.attachments[idx].storeAction = RenderBufferStoreAction.StoreAndResolve;
                                    nativePass.storeAudit[idx] = new StoreAudit(
                                        (isImportedLastWriter) ? StoreReason.StoreImported : StoreReason.StoreUsedByLaterPass, userPassID,
                                        (isImportedLastWriter) ? StoreReason.StoreImported : StoreReason.StoreUsedByLaterPass, msaaUserPassID);
                                }
                                else if (needsResolvedData)
                                {
                                    nativePass.attachments[idx].storeAction = RenderBufferStoreAction.Resolve;
                                    nativePass.storeAudit[idx] = new StoreAudit((isImportedLastWriter) ? StoreReason.StoreImported : StoreReason.StoreUsedByLaterPass, userPassID, StoreReason.DiscardUnused);
                                }
                                else if (needsMSAASamples)
                                {
                                    nativePass.attachments[idx].storeAction = RenderBufferStoreAction.Store;
                                    nativePass.storeAudit[idx] = new StoreAudit(
                                        (resourceData.bindMS) ? StoreReason.DiscardBindMs : StoreReason.DiscardUnused, -1,
                                        (isImportedLastWriter) ? StoreReason.StoreImported : StoreReason.StoreUsedByLaterPass, msaaUserPassID);
                                }
                                else
                                {
                                    Debug.Assert(false, "Resource was not destroyed but nobody seems to be using it!??");
                                }
                            }
                            else if (isImportedLastWriter)
                            {
                                //It's an imported texture and we're the last ones writing it make sure to store the results

                                //var desc = graph.m_Resources.GetTextureResourceDesc(fragment.resource);

                                // Used as a mutlisample-texture we need the msaa samples only
                                if (resourceData.bindMS)
                                {
                                    if (resourceData.discard)
                                    {
                                        nativePass.attachments[idx].storeAction = RenderBufferStoreAction.DontCare;
                                        nativePass.storeAudit[idx] = new StoreAudit(StoreReason.DiscardImported);
                                    }
                                    else
                                    {
                                        nativePass.attachments[idx].storeAction = RenderBufferStoreAction.Store;
                                        nativePass.storeAudit[idx] = new StoreAudit(
                                            StoreReason.DiscardBindMs, -1, StoreReason.StoreImported);
                                    }
                                }
                                // Used as a regular non-multisample texture we need samples as resolved data
                                // we have no idea which one of them will be needed by the external users
                                else
                                {
                                    if (resourceData.discard)
                                    {
                                        nativePass.attachments[idx].storeAction = RenderBufferStoreAction.DontCare;
                                        nativePass.storeAudit[idx] = new StoreAudit(
                                            StoreReason.DiscardImported, -1, StoreReason.DiscardImported);
                                    }
                                    else
                                    {
                                        nativePass.attachments[idx].storeAction = RenderBufferStoreAction.StoreAndResolve;
                                        nativePass.storeAudit[idx] = new StoreAudit(
                                            StoreReason.StoreImported, -1, StoreReason.StoreImported);
                                    }
                                }
                            }
                        }
                    }

                    if (resourceData.memoryLess)
                    {
                        nativePass.attachments[idx].memoryless = true;
                        // Ensure load/store actions are actually valid for memory less
                        if (nativePass.attachments[idx].loadAction == RenderBufferLoadAction.Load) throw new Exception("Resource was marked as memoryless but is trying to load.");
                        if (nativePass.attachments[idx].storeAction != RenderBufferStoreAction.DontCare) throw new Exception("Resource was marked as memoryless but is trying to store or resolve.");
                    }
                }

                if (nativePass.attachments.size == 0 || nativePass.numNativeSubPasses == 0) throw new Exception("Empty render pass");

                debugName.Clear();
                nativePass.GetDebugName(contextData, debugName);

                passCmdBuffer.BeginRenderPass(firstGraphPass.fragmentInfoWidth, firstGraphPass.fragmentInfoHeight, firstGraphPass.fragmentInfoVolumeDepth, firstGraphPass.fragmentInfoSamples,
                ref nativePass.attachments, nativePass.attachments.size, hasDepth, ref contextData.nativeSubPassData, nativePass.firstNativeSubPass, nativePass.numNativeSubPasses, debugName);
            }
        }

        public void GeneratePassCommandBuffer<T>(ref PassCommandBuffer<T> passCmdBuffer) where T : IExecutionPolicy
        {
            using (new ProfilingScope(ProfilingSampler.Get(NativeCompilerProfileId.NRPRGComp_GenPassCommandBuffer)))
            {
                passCmdBuffer.Reset();

                bool inRenderPass = false;

                for (int passIndex = 0; passIndex < contextData.passData.Length; passIndex++)
                {
                    ref var pass = ref contextData.passData.ElementAt(passIndex);

                    //Fix low leve passes being merged into nrp giving errors
                    //because of the "isRaster" check below

                    if (pass.culled)
                        continue;

                    var isRaster = pass.type == RenderGraphPassType.Raster;

                    using (new ProfilingScope(ProfilingSampler.Get(NativeCompilerProfileId.NRPRGComp_GenCreateCommands)))
                    {
                        // For raster passes we need to create resources for all the subpasses at the beginning of the native renderpass
                        if (isRaster && pass.nativePassIndex >= 0)
                        {
                            if (pass.mergeState == PassMergeState.Begin || pass.mergeState == PassMergeState.None)
                            {
                                ref var nativePass = ref contextData.nativePassData.ElementAt(pass.nativePassIndex);
                                foreach (ref readonly var subPass in nativePass.GraphPasses(contextData))
                                {
                                    foreach (ref readonly var res in subPass.FirstUsedResources(contextData))
                                    {
                                        ref var resInfo = ref contextData.UnversionedResourceData(res);
                                        if (resInfo.isImported == false && resInfo.memoryLess == false)
                                        {
                                            bool usedAsFragmentThisPass = subPass.IsUsedAsFragment(res, contextData);

                                            // If usedAsFragmentThisPass=false this is a resource read for the first time but as a regular texture not as a framebuffer attachment
                                            // so we need to explicitly clear it as loadaction.clear only works on fb attachments of course not texturereads
                                            // TODO: Should this be a performance warning?? Maybe rare enough in practice?
                                            passCmdBuffer.CreateResource(res, usedAsFragmentThisPass = false);
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
                                ref var pointTo = ref contextData.UnversionedResourceData(create);
                                if (pointTo.isImported == false)
                                {
                                    passCmdBuffer.CreateResource(create, true);
                                }
                            }
                        }
                    }

                    var isAsyncCompute = pass.type == RenderGraphPassType.Compute && pass.asyncCompute == true;
                    if (isAsyncCompute)
                    {
                        passCmdBuffer.BeginAsyncCompute();
                    }

                    // also make sure to insert fence=waits for multiple queue syncs
                    if (pass.waitOnGraphicsFencePassId != -1)
                    {
                        passCmdBuffer.WaitOnGraphicsFence(pass.waitOnGraphicsFencePassId);
                    }

                    var nrpBegan = false;
                    if (isRaster == true && pass.mergeState <= PassMergeState.Begin)
                    {
                        if (pass.nativePassIndex >= 0)
                        {
                            GenerateBeginRenderPassCommand(ref contextData.nativePassData.ElementAt(pass.nativePassIndex), ref passCmdBuffer);
                            nrpBegan = true;
                            inRenderPass = true;
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
                            passCmdBuffer.NextSubPass();
                        }
                    }

                    passCmdBuffer.ExecuteGraphNode(pass.passId);

                    // should we insert a fence to sync between difference queues?
                    if (pass.insertGraphicsFence)
                    {
                        passCmdBuffer.InsertGraphicsFence(pass.passId);
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
                                    passCmdBuffer.EndRenderPass();
                                    inRenderPass = false;
                                }
                            }
                        }
                    }
                    else if (isAsyncCompute)
                    {
                        passCmdBuffer.EndAsyncCompute();
                    }

                    using (new ProfilingScope(ProfilingSampler.Get(NativeCompilerProfileId.NRPRGComp_GenDestroyCommands)))
                    {
                        if (isRaster && pass.nativePassIndex >= 0)
                        {
                            // For raster passes we need to destroy resources after all the subpasses at the end of the native renderpass
                            if (pass.mergeState == PassMergeState.End || pass.mergeState == PassMergeState.None)
                            {
                                ref var nativePass = ref contextData.nativePassData.ElementAt(pass.nativePassIndex);
                                foreach (ref readonly var subPass in nativePass.GraphPasses(contextData))
                                {
                                    foreach (ref readonly var res in subPass.LastUsedResources(contextData))
                                    {
                                        ref var resInfo = ref contextData.UnversionedResourceData(res);
                                        if (resInfo.isImported == false && resInfo.memoryLess == false)
                                        {
                                            passCmdBuffer.ReleaseResource(res);
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            foreach (ref readonly var destroy in pass.LastUsedResources(contextData))
                            {
                                ref var pointTo = ref contextData.UnversionedResourceData(destroy);
                                if (pointTo.isImported == false)
                                {
                                    passCmdBuffer.ReleaseResource(destroy);
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
