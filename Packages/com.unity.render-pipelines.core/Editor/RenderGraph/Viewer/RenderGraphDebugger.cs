using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Experimental.Rendering.RenderGraphModule.NativeRenderPassCompiler;

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
            CollectGraphDebugData(graph.contextData, graph.graph.m_ResourcesForDebugOnly, view);
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

    private static RenderGraphWindow debugWindow;
    static void CollectGraphDebugData(CompilerContextData ctx,
        RenderGraphResourceRegistry resources, RenderGraphView view)
    {
        //loop over all passes to add them and their resources to the graph
        foreach (ref var pass in ctx.passData)
        {
            RenderGraphWindow.PassDebugData passDebug = new RenderGraphWindow.PassDebugData
            {
                allocations = new List<string>(),
                releases = new List<string>(),
                lastWrites = new List<string>(),
                tag = pass.tag,
                width = pass.FragmentInfoWidth,
                height = pass.FragmentInfoHeight,
                samples = pass.FragmentInfoSamples,
                hasDepth = pass.FragmentInfoHasDepth,
                asyncCompute = pass.asyncCompute,
                syncList = "",
                isCulled = pass.culled
            };

            //loop inputs. last/first to have an input are the passes that allocate/release
            foreach (ref var res in pass.Inputs(ctx))
            {
                var pointTo = ctx.UnversionedResourceData(res.resource);
                var pointToVer = ctx.VersionedResourceData(res.resource);

                var name = pointTo.name + " V" + res.resource.version;

                if (pointTo.firstUsePassID == pass.passId)
                {
                    passDebug.allocations.Add(pointTo.name);
                }

                if (pointTo.lastUsePassID == pass.passId)
                {
                    passDebug.releases.Add(pointTo.name);
                }

                if (pointTo.lastWritePassID == pass.passId)
                {
                    passDebug.lastWrites.Add(pointTo.name);
                }

                var wPass = ctx.passData[pointToVer.writePass];
                if (wPass.asyncCompute != pass.asyncCompute)
                {
                    passDebug.syncList += name + "\\l";
                }

                if (!pointToVer.written)
                {
                    var resourceName = pointTo.name;
                    var resourceVersionName = resourceName + " v" + res.resource.version;
                    var resourceDesc = resources.GetTextureResourceDesc(res.resource);

                    ref readonly var resourceData = ref ctx.UnversionedResourceData(res.resource);

                    var info = new RenderTargetInfo();
                    try
                    {
                        resources.GetRenderTargetInfo(res.resource, out info);
                    }
                    catch (Exception) { }

                    RenderGraphWindow.ResourceDebugData data = new RenderGraphWindow.ResourceDebugData
                    {
                        height = pass.FragmentInfoHeight,
                        width = pass.FragmentInfoWidth,
                        samples = pass.FragmentInfoSamples,
                        clearBuffer = resourceDesc.clearBuffer,
                        isImported = pointTo.isImported,
                        format = info.format,
                        bindMS = resourceDesc.bindTextureMS,
                        isMemoryless = resourceData.memoryLess,
                    };
                    view.AddResource(name, resourceName, data);
                }
            }

            if (pass.numFragments > 0 && pass.NativePassIndex >= 0)
            {
                ref var nativePass = ref ctx.nativePassData[pass.NativePassIndex];

                passDebug.nativeRPInfo = $"Attachment Dimensions: {nativePass.width}x{nativePass.height}x{nativePass.samples}\n";

                passDebug.nativeRPInfo += "\nAttachments:\n";
                for (int i = 0; i < nativePass.attachments.size; i++)
                {
                    var pointTo = ctx.UnversionedResourceData(nativePass.attachments[i].handle);
                    var name = pointTo.name + " V" + nativePass.attachments[i].handle.version;

                    passDebug.nativeRPInfo +=
                        $"Attachment {name} Load:{nativePass.attachments[i].loadAction} Store: {nativePass.attachments[i].storeAction} \n";
                }

                {
                    passDebug.nativeRPInfo += $"\nPass Break Reasoning:\n";
                    if (nativePass.breakAudit.breakPass >= 0)
                    {
                        passDebug.nativeRPInfo += $"Failed to merge {ctx.passData[nativePass.breakAudit.breakPass].name} into this native pass.\n";
                    }
                    var reason = PassBreakAudit.BreakReasonMessages[(int)nativePass.breakAudit.reason];
                    passDebug.nativeRPInfo += reason + "\n";
                }

                passDebug.nativeRPInfo += "\nLoad Reasoning:\n";
                for (int i = 0; i < nativePass.attachments.size; i++)
                {
                    var pointTo = ctx.UnversionedResourceData(nativePass.attachments[i].handle);
                    var name = pointTo.name + " V" + nativePass.attachments[i].handle.version;

                    var loadReason = LoadAudit.LoadReasonMessages[(int)nativePass.loadAudit[i].reason];
                    var passName = "";
                    if (nativePass.loadAudit[i].passId >= 0)
                    {
                        passName = ctx.passData[nativePass.loadAudit[i].passId].name;
                    }
                    loadReason = loadReason.Replace("{pass}", passName);

                    passDebug.nativeRPInfo += $"Load {name}:{loadReason}\n";
                }

                passDebug.nativeRPInfo += "\nStore Reasoning:\n";
                for (int i = 0; i < nativePass.attachments.size; i++)
                {
                    var pointTo = ctx.UnversionedResourceData(nativePass.attachments[i].handle);
                    var name = pointTo.name + " V" + nativePass.attachments[i].handle.version;

                    var storeReason = StoreAudit.StoreReasonMessages[(int)nativePass.storeAudit[i].reason];
                    var passName = "";
                    if (nativePass.storeAudit[i].passId >= 0)
                    {
                        passName = ctx.passData[nativePass.storeAudit[i].passId].name;
                    }
                    storeReason = storeReason.Replace("{pass}", passName);

                    var msaaStoreReason = "";
                    if (nativePass.samples > 1)
                    {
                        msaaStoreReason = StoreAudit.StoreReasonMessages[(int)nativePass.storeAudit[i].msaaReason];
                        var msaaPassName = "";
                        if (nativePass.storeAudit[i].msaaPassId >= 0)
                        {
                            msaaPassName = ctx.passData[nativePass.storeAudit[i].msaaPassId].name;
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

            view.AddPass(pass.identifier, pass.name, passDebug);
        }

        //After all passes and resources have been added to the graph -> register connections.
        foreach (ref var pass in ctx.passData)
        {
            foreach (ref var res in pass.Inputs(ctx))
            {
                var pointTo = ctx.UnversionedResourceData(res.resource);
                var name = pointTo.name + " V" + res.resource.version;
                ref var pointToVer = ref ctx.VersionedResourceData(res.resource);

                var use = InputUsageType.Texture;
                if (pass.type == RenderGraphPassType.Raster)
                {
                    foreach (var f in pass.Fragments(ctx))
                    {
                        if (f.resource.index == res.resource.index)
                        {
                            use = InputUsageType.Raster;
                            break;
                        }
                    }

                    foreach (var f in pass.FragmentInputs(ctx))
                    {
                        if (f.resource.index == res.resource.index)
                        {
                            use = InputUsageType.Fetch;
                            break;
                        }
                    }
                }

                var prevPass = ctx.passData[pointToVer.writePass];
                PassBreakAudit mergeResult;
                if (prevPass.NativePassIndex >= 0)
                {
                    mergeResult = NativePassData.TryMerge(ctx, prevPass.NativePassIndex, pass.passId, true);
                }
                else
                {
                    mergeResult = new PassBreakAudit(PassBreakReason.NonRasterPass, pass.passId);
                }

                string mergeMessage = "";

                switch (mergeResult.reason)
                {
                    case (PassBreakReason.Merged):
                        if (pass.NativePassIndex == prevPass.NativePassIndex &&
                            pass.MergeState != PassMergeState.None)
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
                                       + prevPass.name + ": " +
                                       prevPass.FragmentInfoWidth + "x" + prevPass.FragmentInfoHeight + "x" + prevPass.FragmentInfoSamples + ". \n"
                                       + pass.name + ": " +
                                       pass.FragmentInfoWidth + "x" + pass.FragmentInfoHeight + "x" + pass.FragmentInfoSamples + ".";
                        break;

                    case PassBreakReason.DepthBufferUseMismatch:
                        mergeMessage = "Some passes use a depth buffer and some not. " + prevPass.name + ": " +
                                       prevPass.FragmentInfoHasDepth + ". \n" + pass.name + ": " + pass.FragmentInfoHasDepth + '.';
                        break;

                    case PassBreakReason.NextPassReadsTexture:
                        mergeMessage = "The next pass reads one of the outputs as a regular texture, the pass needs to break.";
                        break;

                    case PassBreakReason.NonRasterPass:
                        mergeMessage = pass.name + " is not a raster pass but " + pass.type;
                        break;

                    case PassBreakReason.DifferentDepthTextures:
                        mergeMessage = prevPass.name + " uses a different depth buffer than " + pass.name;
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
                    view.AddConnection(prevPass.identifier, pass.identifier, name, pointTo.name, mergeMessage, use);
                }
            }
        }

        //Register merged passes
        foreach (ref var nrp in ctx.nativePassData)
        {
            string passes = "";
            foreach (var pass in nrp.GraphPasses(ctx))
            {
                passes += pass.identifier + '|';
            }

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
