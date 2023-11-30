using System.Collections.Generic;
using System.Linq;
using UnityEngine.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering
{
    internal interface ILineRenderer
    {
        bool LineRendererIsValid();
        LineRendering.RendererData GetLineRendererData(RenderGraph renderGraph, Camera camera);
    }

    partial class LineRendering
    {
        private static HashSet<ILineRenderer> s_RendererInstances = new HashSet<ILineRenderer>();
        private static ILineRenderer[]        s_RendererInstancesAsArray = null;
        private static int                    s_RendererInstanceCount = 0;

        private static void UpdateInstanceArray()
        {
            s_RendererInstanceCount = s_RendererInstances.Count;

            if (s_RendererInstanceCount > 0)
            {
                s_RendererInstancesAsArray = new ILineRenderer[s_RendererInstanceCount];
                s_RendererInstances.CopyTo(s_RendererInstancesAsArray);
            }
            else
            {
                s_RendererInstancesAsArray = null;
            }
        }

        internal static void AddRenderer(ILineRenderer renderer)
        {
            s_RendererInstances.Add(renderer);
            UpdateInstanceArray();
        }

        internal static void RemoveRenderer(ILineRenderer renderer)
        {
            s_RendererInstances.Remove(renderer);
            UpdateInstanceArray();
        }

        private static bool HasRenderDatas() => s_RendererInstancesAsArray != null;
        private static RendererData[] GetValidRenderDatas(RenderGraph renderGraph, Camera camera) =>
            s_RendererInstancesAsArray.Where(o => o.LineRendererIsValid()).Select(o => o.GetLineRendererData(renderGraph, camera)).ToArray();
    }
}
