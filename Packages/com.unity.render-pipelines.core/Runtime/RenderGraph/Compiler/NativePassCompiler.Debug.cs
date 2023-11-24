using System;
using System.Collections.Generic;

namespace UnityEngine.Rendering.RenderGraphModule.NativeRenderPassCompiler
{
    // External debuggers implement this interface to get notified every time a graph is compiled
    // It's up to the debuggers to filter this callback to executions they are interested in
    // E.g You could filter on the graphs name using graph.graph.debugName
    internal interface IRenderGraphDebugger
    {
        public void OutputGraph(NativePassCompiler graph);
    }

    internal partial class NativePassCompiler
    {
        static List<IRenderGraphDebugger> debuggerList = new List<IRenderGraphDebugger>();

        internal static void AddRenderGraphDebugger(IRenderGraphDebugger debugger)
        {
            debuggerList.Add(debugger);
        }

        internal void OutputDebugGraph()
        {
            foreach (IRenderGraphDebugger debugger in debuggerList)
            {
                debugger.OutputGraph(this);
            }
        }

        static RenderGraph.DebugData.PassData.NRPInfo.NativeRenderPassInfo.AttachmentInfo MakeAttachmentInfo(
            CompilerContextData ctx,
            in NativePassData nativePass,
            int attachmentIndex)
        {
            var attachment = nativePass.attachments[attachmentIndex];
            var pointTo = ctx.UnversionedResourceData(attachment.handle);

            LoadAudit loadAudit = nativePass.loadAudit[attachmentIndex];
            string loadReason = LoadAudit.LoadReasonMessages[(int) loadAudit.reason];
            if (loadAudit.passId >= 0)
                loadReason = loadReason.Replace("{pass}", $"<b>{ctx.passNames[loadAudit.passId].name}</b>");

            StoreAudit storeAudit = nativePass.storeAudit[attachmentIndex];
            string storeReason = StoreAudit.StoreReasonMessages[(int) storeAudit.reason];
            if (storeAudit.passId >= 0)
                storeReason = storeReason.Replace("{pass}", $"<b>{ctx.passNames[storeAudit.passId].name}</b>");

            return new RenderGraph.DebugData.PassData.NRPInfo.NativeRenderPassInfo.AttachmentInfo
            {
                resourceName = pointTo.GetName(ctx, attachment.handle),
                loadAction = attachment.loadAction.ToString(),
                loadReason = loadReason,
                storeAction = attachment.storeAction.ToString(),
                storeReason = storeReason,
            };
        }

        internal static string MakePassBreakInfoMessage(CompilerContextData ctx, in NativePassData nativePass)
        {
            string msg = "";
            if (nativePass.breakAudit.breakPass >= 0)
            {
                msg += $"Failed to merge {ctx.passNames[nativePass.breakAudit.breakPass].name} into this native pass.\n";
            }

            var reason = PassBreakAudit.BreakReasonMessages[(int) nativePass.breakAudit.reason];
            msg += reason + "\n";
            return msg;
        }

        internal static string MakePassMergeMessage(CompilerContextData ctx, in PassData pass, in PassData prevPass, PassBreakAudit mergeResult)
        {
            string message = mergeResult.reason == PassBreakReason.Merged ?
                "The passes are <b>compatible</b> to be merged.\n\n" :
                "The passes are <b>incompatible</b> to be merged.\n\n";
            string passName = pass.GetName(ctx).name;
            string prevPassName = prevPass.GetName(ctx).name;
            switch (mergeResult.reason)
            {
                case (PassBreakReason.Merged):
                    if (pass.nativePassIndex == prevPass.nativePassIndex && pass.mergeState != PassMergeState.None)
                        message += "Passes are merged.";
                    else
                        message += "Passes can be merged but are not recorded consecutively.";
                    break;
                case PassBreakReason.TargetSizeMismatch:
                    message += "The fragment attachments of the passes have different sizes or sample counts.\n" +
                               $"- {prevPassName}: {prevPass.fragmentInfoWidth}x{prevPass.fragmentInfoHeight}, {prevPass.fragmentInfoSamples} sample(s).\n" +
                               $"- {passName}: {pass.fragmentInfoWidth}x{pass.fragmentInfoHeight}, {pass.fragmentInfoSamples} sample(s).";
                    break;
                case PassBreakReason.NextPassReadsTexture:
                    message += "The next pass reads one of the outputs as a regular texture, the pass needs to break.";
                    break;
                case PassBreakReason.NonRasterPass:
                    message += $"{prevPassName} is type {pass.type}. Only Raster passes can be merged.";
                    break;
                case PassBreakReason.DifferentDepthTextures:
                    message += $"{prevPassName} uses a different depth buffer than {passName}.";
                    break;
                case PassBreakReason.AttachmentLimitReached:
                    message += $"Merging the passes would use more than {FixedAttachmentArray<PassFragmentData>.MaxAttachments} attachments.";
                    break;
                case PassBreakReason.EndOfGraph:
                    message += "The pass is the last pass in the graph.";
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return message;
        }

        internal void GenerateNativeCompilerDebugData(ref RenderGraph.DebugData debugData)
        {
            ref var ctx = ref contextData;

            debugData.isNRPCompiler = true;

            // Resolve read/write lists per resource
            Dictionary<(RenderGraphResourceType, int), List<int>> resourceReadLists = new Dictionary<(RenderGraphResourceType, int), List<int>>();
            Dictionary<(RenderGraphResourceType, int), List<int>> resourceWriteLists = new Dictionary<(RenderGraphResourceType, int), List<int>>();

            foreach (var renderGraphPass in graph.m_RenderPasses)
            {
                for (int type = 0; type < (int)RenderGraphResourceType.Count; ++type)
                {
                    int numResources = ctx.resources.unversionedData[type].Length;

                    for (int resIndex = 0; resIndex < numResources; ++resIndex)
                    {
                        foreach (var read in renderGraphPass.resourceReadLists[type])
                        {
                            if (renderGraphPass.implicitReadsList.Contains(read))
                                continue; // Implicit read - do not display them in RG Viewer

                            if (read.type == (RenderGraphResourceType) type && read.index == resIndex)
                            {
                                var pair = ((RenderGraphResourceType) type, resIndex);
                                if (!resourceReadLists.ContainsKey(pair))
                                    resourceReadLists[pair] = new List<int>();
                                resourceReadLists[pair].Add(renderGraphPass.index);
                            }
                        }
                        foreach (var read in renderGraphPass.resourceWriteLists[type])
                        {
                            if (read.type == (RenderGraphResourceType) type && read.index == resIndex)
                            {
                                var pair = ((RenderGraphResourceType) type, resIndex);
                                if (!resourceWriteLists.ContainsKey(pair))
                                    resourceWriteLists[pair] = new List<int>();
                                resourceWriteLists[pair].Add(renderGraphPass.index);
                            }
                        }
                    }
                }
            }

            // Create resource debug data
            for (int t = 0; t < (int)RenderGraphResourceType.Count; ++t)
            {
                var numResources = ctx.resources.unversionedData[t].Length;
                for (int i = 0; i < numResources; ++i)
                {
                    ref var resourceUnversioned = ref ctx.resources.unversionedData[t].ElementAt(i);
                    RenderGraph.DebugData.ResourceData debugResource = new RenderGraph.DebugData.ResourceData();
                    RenderGraphResourceType type = (RenderGraphResourceType) t;
                    bool isNullResource = i == 0;

                    if (!isNullResource)
                    {
                        string resourceName = ctx.resources.resourceNames[t][i].name;
                        debugResource.name = !string.IsNullOrEmpty(resourceName) ? resourceName : "(unnamed)";
                        debugResource.imported = resourceUnversioned.isImported;
                    }
                    else
                    {
                        // The above functions will throw exceptions when used with the null argument so just use a dummy instead
                        debugResource.name = "<null>";
                        debugResource.imported = true;
                    }

                    var info = new RenderTargetInfo();
                    if (type == RenderGraphResourceType.Texture && !isNullResource)
                    {
                        var handle = new ResourceHandle(i, type, false);
                        try
                        {
                            graph.m_ResourcesForDebugOnly.GetRenderTargetInfo(handle, out info);
                        }
                        catch (Exception) { }
                    }

                    debugResource.creationPassIndex = resourceUnversioned.firstUsePassID;
                    debugResource.releasePassIndex = resourceUnversioned.lastUsePassID;

                    debugResource.width = resourceUnversioned.width;
                    debugResource.height = resourceUnversioned.height;
                    debugResource.depth = resourceUnversioned.volumeDepth;
                    debugResource.samples = resourceUnversioned.msaaSamples;
                    debugResource.format = info.format;
                    debugResource.memoryless = resourceUnversioned.memoryLess;

                    debugResource.consumerList = new List<int>();
                    debugResource.producerList = new List<int>();

                    if (resourceReadLists.ContainsKey(((RenderGraphResourceType) t, i)))
                        debugResource.consumerList = resourceReadLists[((RenderGraphResourceType) t, i)];

                    if (resourceWriteLists.ContainsKey(((RenderGraphResourceType) t, i)))
                        debugResource.producerList = resourceWriteLists[((RenderGraphResourceType) t, i)];

                    debugData.resourceLists[t].Add(debugResource);
                }
            }

            // Create pass debug data
            for (int passId = 0; passId < ctx.passData.Length; passId++)
            {
                var graphPass = graph.m_RenderPasses[passId];
                ref var passData = ref ctx.passData.ElementAt(passId);
                RenderGraph.DebugData.PassData debugPass = new RenderGraph.DebugData.PassData();
                debugPass.name = passData.GetName(ctx).name;
                debugPass.type = passData.type;
                debugPass.culled = passData.culled;
                debugPass.async = passData.asyncCompute;
                debugPass.generateDebugData = graphPass.generateDebugData;
                debugPass.resourceReadLists = new List<int>[(int)RenderGraphResourceType.Count];
                debugPass.resourceWriteLists = new List<int>[(int)RenderGraphResourceType.Count];

                debugPass.syncFromPassIndex = -1; // TODO async compute support
                debugPass.syncToPassIndex = -1; // TODO async compute support

                debugPass.nrpInfo = new RenderGraph.DebugData.PassData.NRPInfo();

                debugPass.nrpInfo.width = passData.fragmentInfoWidth;
                debugPass.nrpInfo.height = passData.fragmentInfoHeight;
                debugPass.nrpInfo.volumeDepth = passData.fragmentInfoVolumeDepth;
                debugPass.nrpInfo.samples = passData.fragmentInfoSamples;
                debugPass.nrpInfo.hasDepth = passData.fragmentInfoHasDepth;

                foreach (var setGlobal in graphPass.setGlobalsList)
                    debugPass.nrpInfo.setGlobals.Add(setGlobal.Item1.handle.index);

                for (int type = 0; type < (int)RenderGraphResourceType.Count; ++type)
                {
                    debugPass.resourceReadLists[type] = new List<int>();
                    debugPass.resourceWriteLists[type] = new List<int>();

                    foreach (var resRead in graphPass.resourceReadLists[type])
                    {
                        if (graphPass.implicitReadsList.Contains(resRead))
                            continue; // Implicit read - do not display them in RG Viewer
                        debugPass.resourceReadLists[type].Add(resRead.index);
                    }

                    foreach (var resWrite in graphPass.resourceWriteLists[type])
                        debugPass.resourceWriteLists[type].Add(resWrite.index);
                }

                foreach (var fragmentInput in passData.FragmentInputs(ctx))
                {
                    Debug.Assert(fragmentInput.resource.type == RenderGraphResourceType.Texture);
                    debugPass.nrpInfo.textureFBFetchList.Add(fragmentInput.resource.index);
                }

                debugData.passList.Add(debugPass);
            }

            // Native pass info
            foreach (ref readonly var nativePassData in ctx.NativePasses)
            {
                List<int> mergedPassIds = new List<int>();
                for (int passOffset = 0; passOffset < nativePassData.numGraphPasses; ++passOffset)
                    mergedPassIds.Add(nativePassData.firstGraphPass + passOffset);

                if (nativePassData.numGraphPasses > 0)
                {
                    var nativePassInfo = new RenderGraph.DebugData.PassData.NRPInfo.NativeRenderPassInfo();
                    nativePassInfo.passBreakReasoning = MakePassBreakInfoMessage(ctx, in nativePassData);
                    nativePassInfo.attachmentInfos = new ();
                    for (int a = 0; a < nativePassData.attachments.size; a++)
                        nativePassInfo.attachmentInfos.Add(MakeAttachmentInfo(ctx, in nativePassData, a));
                    nativePassInfo.passCompatibility = new Dictionary<int, RenderGraph.DebugData.PassData.NRPInfo.NativeRenderPassInfo.PassCompatibilityInfo>();
                    nativePassInfo.mergedPassIds = mergedPassIds;

                    for (int i = 0; i < mergedPassIds.Count; ++i)
                    {
                        var mergedPassId = mergedPassIds[i];
                        var debugPass = debugData.passList[mergedPassId];
                        debugPass.nrpInfo.nativePassInfo = nativePassInfo;
                        debugData.passList[mergedPassId] = debugPass;
                    }
                }
            }

            // Pass compatibility info
            for (int passIndex = 0; passIndex < ctx.passData.Length; passIndex++)
            {
                ref var pass = ref ctx.passData.ElementAt(passIndex);
                var nativePassInfo = debugData.passList[pass.passId].nrpInfo.nativePassInfo;
                if (nativePassInfo == null)
                    continue;

                // Input dependencies
                foreach (ref readonly var input in pass.Inputs(ctx))
                {
                    ref var inputDataVersioned = ref ctx.VersionedResourceData(input.resource);
                    if (inputDataVersioned.written)
                    {
                        var inputDependencyPass = ctx.passData[inputDataVersioned.writePassId];
                        PassBreakAudit mergeResult = inputDependencyPass.nativePassIndex >= 0
                            ? NativePassData.CanMerge(ctx, inputDependencyPass.nativePassIndex, pass.passId)
                            : new PassBreakAudit(PassBreakReason.NonRasterPass, pass.passId);

                        string mergeMessage = "This pass writes to a resource that is read by the currently selected pass.\n\n"
                                              + MakePassMergeMessage(ctx, pass, inputDependencyPass, mergeResult);
                        nativePassInfo.passCompatibility.TryAdd(inputDependencyPass.passId,
                            new RenderGraph.DebugData.PassData.NRPInfo.NativeRenderPassInfo.PassCompatibilityInfo
                            {
                                message = mergeMessage,
                                isCompatible = mergeResult.reason == PassBreakReason.Merged
                            });
                    }
                }

                // Output dependencies (only relevant if current pass is part of a native pass, and therefore candidate for merging)
                if (pass.nativePassIndex >= 0)
                {
                    foreach (ref readonly var output in pass.Outputs(ctx))
                    {
                        ref var outputDataUnversioned = ref ctx.UnversionedResourceData(output.resource);
                        if (outputDataUnversioned.lastUsePassID != pass.passId) // Someone else is using this resource
                        {
                            ref var outputDataVersioned = ref ctx.VersionedResourceData(output.resource);
                            var numReaders = outputDataVersioned.numReaders;
                            for (var i = 0; i < numReaders; ++i)
                            {
                                var depIdx = ResourcesData.IndexReader(output.resource, i);
                                ref var dep = ref ctx.resources.readerData[output.resource.iType].ElementAt(depIdx);

                                var outputDependencyPass = ctx.passData[dep.passId];
                                PassBreakAudit mergeResult = NativePassData.CanMerge(ctx, pass.nativePassIndex,
                                    outputDependencyPass.passId);

                                string mergeMessage = "This pass reads a resource that is written to by the currently selected pass.\n\n"
                                                      + MakePassMergeMessage(ctx, outputDependencyPass, pass, mergeResult);
                                nativePassInfo.passCompatibility.TryAdd(outputDependencyPass.passId,
                                    new RenderGraph.DebugData.PassData.NRPInfo.NativeRenderPassInfo.PassCompatibilityInfo
                                    {
                                        message = mergeMessage,
                                        isCompatible = mergeResult.reason == PassBreakReason.Merged
                                    });
                            }
                        }
                    }
                }
            }
        }
    }
}
