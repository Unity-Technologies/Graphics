using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.RenderGraphModule.NativeRenderPassCompiler
{
    internal static class RenderGraphUtil
    {

        internal static class Synchronization
        {
            internal static bool NeedsGraphicsFence(PassData pass, CompilerContextData ctx)
            {
                foreach (ref var res in pass.Outputs(ctx))
                {
                    var pointTo = ctx.resources[res.resource];
                    var numReaders = pointTo.numReaders;
                    for (var i = 0; i < numReaders; ++i)
                    {
                        var depIdx = ResourcesData.IndexReader(res.resource, i);
                        var dep = ctx.resources.readerData[res.resource.iType][depIdx];
                        var depPass = ctx.passData[dep.passId];
                        if (pass.asyncCompute != depPass.asyncCompute)
                        {
                            return true;
                        }
                    }
                }

                return false;
            }

            internal static bool NeedsToWaitOfGraphicsFence(PassData pass, CompilerContextData ctx, out int passIDToWaiton)
            {
                foreach (var inp in pass.Inputs(ctx))
                {
                    var handle = inp.resource;
                    var resource = ctx.resources[handle];
                    var wPass = ctx.passData[resource.writePass];
                    if (wPass.asyncCompute != pass.asyncCompute)
                    {
                        passIDToWaiton = wPass.passId;
                        return true;
                    }
                }

                passIDToWaiton = -1;
                return false;
            }
        }
    }
}
