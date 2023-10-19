using System;
using System.Collections.Generic;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Experimental.Rendering.RenderGraphModule.NativeRenderPassCompiler;
using UnityEngine.Rendering;

internal class RenderGraphDebugger : IRenderGraphDebugger
{
    public bool captureNextGraph;

    internal static RenderGraphDebugger instance;
    static RenderGraphDebugger()
    {
        instance = new RenderGraphDebugger();
        NativePassCompiler.AddRenderGraphDebugger(instance);
    }

    public void OutputGraph(NativePassCompiler graph)
    {
        if (captureNextGraph)
        {
            var view = RenderGraphWindow.OpenGraphVisualizer();
            CollectGraphDebugData(graph.contextData, graph.graph, view);
            view.UpdateNodeVisualisation();
            captureNextGraph = false;
        }
    }

    public enum InputUsageType
    {
        Texture, //Use with texture ops
        Raster, //Used with raster ops
        Fetch //Used with fetch ops
    }

    // Actual values of global variables when a certain pass executes
    class PassGlobalState
    {
        public PassGlobalState() { }
        public PassGlobalState(PassGlobalState copyFrom)
        {
            values = new DynamicArray<ValueTuple<int, ResourceHandle>>(copyFrom.values);
        }

        public void SetGlobalValue(int shaderPropertyId, ResourceHandle value)
        {
            if (TryFind(shaderPropertyId, out var index))
            {
                values[index].Item2 = value;
            }
            else
            {
                values.Add(ValueTuple.Create(shaderPropertyId, value));
            }
        }

        public ResourceHandle GetGlobalValue(int shaderPropertyId)
        {
            if (TryFind(shaderPropertyId, out var index))
            {
                return values[index].Item2;
            }
            return new ResourceHandle();
        }

        public bool IsUsedAsGlobal(ResourceHandle h)
        {
            for (int i = 0; i < values.size; i++)
            {
                if (h.index == values[i].Item2.index)
                {
                    return true;
                }
            }
            return false;
        }

        private bool TryFind(int shaderPropertyId, out int idx)
        {
            for (int i = 0; i < values.size; i++)
            {
                if (shaderPropertyId == values[i].Item1)
                {
                    idx = i;
                    return true;
                }
            }
            idx = -1;
            return false;
        }

        DynamicArray<ValueTuple<int, ResourceHandle>> values = new DynamicArray<ValueTuple<int, ResourceHandle>>();
    }


    private static RenderGraphWindow debugWindow;
    static void CollectGraphDebugData(CompilerContextData ctx,
        NativePassCompiler.RenderGraphInputInfo inputInfo, RenderGraphView view)
    {
        var resources = inputInfo.m_ResourcesForDebugOnly;

        // Derive the state of the global variables at each pass
        List<PassGlobalState> passGlobals = new List<PassGlobalState>(ctx.passData.Length);
        for (var i = 0; i < ctx.passData.Length; i++)
            passGlobals.Add(null);
        passGlobals[0] = new PassGlobalState();// first pass has empty globals set

        for (var i = 0; i < ctx.passData.Length; i++)
        {
            ref readonly var pass = ref ctx.passData.ElementAt(i);
            if (pass.passId == ctx.passData.Length-1) continue; // last pass doesn't matter
            var nextGlobalState = new PassGlobalState(passGlobals[pass.passId]);
            var globals = inputInfo.m_RenderPasses[pass.passId].setGlobalsList;
            foreach (var g in globals)
            {
                nextGlobalState.SetGlobalValue(g.Item2, g.Item1.handle);
            }

            passGlobals[pass.passId + 1] = nextGlobalState;
        }

        //loop over all passes to add them and their resources to the graph
        for (int passIndex = 0; passIndex < ctx.passData.Length; passIndex++)
        {
            ref var pass = ref ctx.passData.ElementAt(passIndex);
            RenderGraphWindow.PassDebugData passDebug = new RenderGraphWindow.PassDebugData
            {
                allocations = new List<string>(),
                releases = new List<string>(),
                lastWrites = new List<string>(),
                tag = pass.tag,
                width = pass.fragmentInfoWidth,
                height = pass.fragmentInfoHeight,
                samples = pass.fragmentInfoSamples,
                hasDepth = pass.fragmentInfoHasDepth,
                asyncCompute = pass.asyncCompute,
                syncList = "",
                isCulled = pass.culled
            };

            //loop inputs. last/first to have an input are the passes that allocate/release
            foreach (ref readonly var input in pass.Inputs(ctx))
            {
                var inputResource = input.resource;
                ref var pointTo = ref ctx.UnversionedResourceData(inputResource);
                ref var pointToVer = ref ctx.VersionedResourceData(inputResource);

                var unversionedName = pointTo.GetName(ctx, inputResource);
                var versionedName = unversionedName + " V" + inputResource.version;

                if (pointTo.lastWritePassID == pass.passId)
                {
                    passDebug.lastWrites.Add(unversionedName);
                }

                var wPass = ctx.passData[pointToVer.writePassId];
                if (wPass.asyncCompute != pass.asyncCompute)
                {
                    passDebug.syncList += versionedName + "\\l";
                }

                if (!pointToVer.written)
                {
                    var resourceDesc = resources.GetTextureResourceDesc(inputResource);

                    ref readonly var resourceData = ref ctx.UnversionedResourceData(inputResource);

                    var info = new RenderTargetInfo();
                    try
                    {
                        resources.GetRenderTargetInfo(inputResource, out info);
                    }
                    catch (Exception) { }

                    RenderGraphWindow.ResourceDebugData data = new RenderGraphWindow.ResourceDebugData
                    {
                        height = pass.fragmentInfoHeight,
                        width = pass.fragmentInfoWidth,
                        samples = pass.fragmentInfoSamples,
                        clearBuffer = resourceDesc.clearBuffer,
                        isImported = pointTo.isImported,
                        format = info.format,
                        bindMS = resourceDesc.bindTextureMS,
                        isMemoryless = resourceData.memoryLess,
                    };
                    view.AddResource(versionedName, unversionedName, data);
                }
            }

            foreach (ref readonly var released in pass.LastUsedResources(ctx))
            {
                ref var pointTo = ref ctx.UnversionedResourceData(released);
                string name = pointTo.GetName(ctx, released);
                passDebug.releases.Add(name);
            }

            foreach (ref readonly var allocated in pass.FirstUsedResources(ctx))
            {
                ref var pointTo = ref ctx.UnversionedResourceData(allocated);
                string name = pointTo.GetName(ctx, allocated);
                passDebug.releases.Add(name);
            }

            if (pass.numFragments > 0 && pass.nativePassIndex >= 0)
            {
                ref var nativePass = ref ctx.nativePassData.ElementAt(pass.nativePassIndex);

                passDebug.nativeRPInfo = $"Attachment Dimensions: {nativePass.width}x{nativePass.height}x{nativePass.samples}\n";

                passDebug.nativeRPInfo += "\nAttachments:\n";
                for (int i = 0; i < nativePass.attachments.size; i++)
                {
                    var name = ctx.GetResourceVersionedName(nativePass.attachments[i].handle);
                    passDebug.nativeRPInfo +=
                        $"Attachment {name} Load:{nativePass.attachments[i].loadAction} Store: {nativePass.attachments[i].storeAction} \n";
                }

                {
                    passDebug.nativeRPInfo += $"\nPass Break Reasoning:\n";
                    if (nativePass.breakAudit.breakPass >= 0)
                    {
                        passDebug.nativeRPInfo += $"Failed to merge {ctx.GetPassName(nativePass.breakAudit.breakPass)} into this native pass.\n";
                    }
                    var reason = PassBreakAudit.BreakReasonMessages[(int)nativePass.breakAudit.reason];
                    passDebug.nativeRPInfo += reason + "\n";
                }

                passDebug.nativeRPInfo += "\nLoad Reasoning:\n";
                for (int i = 0; i < nativePass.attachments.size; i++)
                {
                    var name = ctx.GetResourceVersionedName(nativePass.attachments[i].handle);
                    var loadReason = LoadAudit.LoadReasonMessages[(int)nativePass.loadAudit[i].reason];
                    var passName = "";
                    if (nativePass.loadAudit[i].passId >= 0)
                    {
                        passName = ctx.GetPassName(nativePass.loadAudit[i].passId);
                    }
                    loadReason = loadReason.Replace("{pass}", passName);

                    passDebug.nativeRPInfo += $"Load {name}:{loadReason}\n";
                }

                passDebug.nativeRPInfo += "\nStore Reasoning:\n";
                for (int i = 0; i < nativePass.attachments.size; i++)
                {
                    var name = ctx.GetResourceVersionedName(nativePass.attachments[i].handle);
                    var storeReason = StoreAudit.StoreReasonMessages[(int)nativePass.storeAudit[i].reason];
                    var passName = "";
                    if (nativePass.storeAudit[i].passId >= 0)
                    {
                        passName = ctx.GetPassName(nativePass.storeAudit[i].passId);
                    }
                    storeReason = storeReason.Replace("{pass}", passName);

                    var msaaStoreReason = "";
                    if (nativePass.samples > 1)
                    {
                        msaaStoreReason = StoreAudit.StoreReasonMessages[(int)nativePass.storeAudit[i].msaaReason];
                        var msaaPassName = "";
                        if (nativePass.storeAudit[i].msaaPassId >= 0)
                        {
                            msaaPassName = ctx.GetPassName(nativePass.storeAudit[i].msaaPassId);
                        }
                        msaaStoreReason = storeReason.Replace("{pass}", msaaPassName);
                    }

                    if (nativePass.samples > 1)
                    {
                        passDebug.nativeRPInfo += $"Store Samples {name}:{storeReason}\n";
                        passDebug.nativeRPInfo += $"Store Resolved {name}:{msaaStoreReason}\n";
                    }
                    else
                    {
                        passDebug.nativeRPInfo += $"Store {name}:{storeReason}\n";
                    }
                }
            }

            view.AddPass(pass.identifier, ctx.GetPassName(pass.passId), passDebug);
        }

        //After all passes and resources have been added to the graph -> register connections.
        for (int passIndex = 0; passIndex < ctx.passData.Length; passIndex++)
        {
            ref var pass = ref ctx.passData.ElementAt(passIndex);
            foreach (ref readonly var input in pass.Inputs(ctx))
            {
                var inputResource = input.resource;
                var resourceName = ctx.GetResourceName(inputResource);
                var resourceVersionedName = ctx.GetResourceVersionedName(inputResource);
                ref var pointToVer = ref ctx.VersionedResourceData(inputResource);

                var use = InputUsageType.Texture;
                if (pass.type == RenderGraphPassType.Raster)
                {
                    foreach (ref readonly var fragment in pass.Fragments(ctx))
                    {
                        var fragmentResource = fragment.resource;
                        if (fragmentResource.index == inputResource.index)
                        {
                            use = InputUsageType.Raster;
                            break;
                        }
                    }

                    foreach (ref readonly var fragmentInput in pass.FragmentInputs(ctx))
                    {
                        var fragmentInputResource = fragmentInput.resource;
                        if (fragmentInputResource.index == inputResource.index)
                        {
                            use = InputUsageType.Fetch;
                            break;
                        }
                    }
                }

                ref var prevPass = ref ctx.passData.ElementAt(pointToVer.writePassId);

                string passName = ctx.GetPassName(pass.passId);
                string prevPassName = ctx.GetPassName(pointToVer.writePassId);

                PassBreakAudit mergeResult;
                if (prevPass.nativePassIndex >= 0)
                {
                    mergeResult = NativePassData.CanMerge(ctx, prevPass.nativePassIndex, pass.passId);
                }
                else
                {
                    mergeResult = new PassBreakAudit(PassBreakReason.NonRasterPass, pass.passId);
                }

                string mergeMessage = "";

                switch (mergeResult.reason)
                {
                    case (PassBreakReason.Merged):
                        if (pass.nativePassIndex == prevPass.nativePassIndex &&
                            pass.mergeState != PassMergeState.None)
                        {
                            mergeMessage = "Passes are merged";
                        }
                        else
                        {
                            mergeMessage = "Passes can be merged but are not recorded consecutively.";
                        }
                        break;

                    case PassBreakReason.TargetSizeMismatch:
                        mergeMessage = "Passes have different sizes/samples.\n"
                                       + prevPassName + ": " +
                                       prevPass.fragmentInfoWidth + "x" + prevPass.fragmentInfoHeight + "x" + prevPass.fragmentInfoSamples + ". \n"
                                       + passName + ": " +
                                       pass.fragmentInfoWidth + "x" + pass.fragmentInfoHeight + "x" + pass.fragmentInfoSamples + ".";
                        break;

                    case PassBreakReason.DepthBufferUseMismatch:
                        mergeMessage = "Some passes use a depth buffer and some not. " + prevPassName + ": " +
                                       prevPass.fragmentInfoHasDepth + ". \n" + passName + ": " + pass.fragmentInfoHasDepth + '.';
                        break;

                    case PassBreakReason.NextPassReadsTexture:
                        mergeMessage = "The next pass reads one of the outputs as a regular texture, the pass needs to break.";
                        break;

                    case PassBreakReason.NonRasterPass:
                        mergeMessage = passName + " is not a raster pass but " + pass.type;
                        break;

                    case PassBreakReason.DifferentDepthTextures:
                        mergeMessage = prevPassName + " uses a different depth buffer than " + passName;
                        break;

                    case PassBreakReason.AttachmentLimitReached:
                        mergeMessage = "Merging the passes would use more than " + 8 + " attachments";
                        break;

                    case PassBreakReason.EndOfGraph:
                        mergeMessage = "The pass is the last pass in the graph";
                        break;

                    default:
                        throw new ArgumentOutOfRangeException();
                }

                if (pointToVer.written)
                {
                    bool isGlobal = passGlobals[pass.passId].IsUsedAsGlobal(inputResource);
                    view.AddConnection(prevPass.identifier, pass.identifier, resourceVersionedName, resourceName, mergeMessage, use, isGlobal);
                }
            }
        }

        //Register merged passes
        for (int i = 0; i < ctx.nativePassData.Length; i++)
        {
            ref var nrp = ref ctx.nativePassData.ElementAt(i);
            string passes = "";

            foreach (ref readonly var pass in nrp.GraphPasses(ctx))
                passes += pass.identifier + '|';

            if (passes.Length > 1)
                view.AddNRP(passes.Substring(0, passes.Length - 1));
        }
    }

    static internal void ShowRenderGraphDebugger()
    {
        //instance.captureNextGraph = true;
        RenderGraphWindow.OpenGraphVisualizer();
    }
}
