using System.Collections.Generic;
using System.Linq;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering
{
    internal interface ILineRenderer
    {
        bool LineRendererIsValid();
        LineRendering.RendererData GetLineRendererData(RenderGraph renderGraph, Camera camera);
    }

    partial class LineRendering
    {

        struct LineRendererEntry
        {
            public ILineRenderer renderer;
            public PerRendererPersistentData perRendererResources;
        }
        private static Dictionary<ILineRenderer, PerRendererPersistentData> s_RendererInstances = new Dictionary<ILineRenderer, PerRendererPersistentData>();
        private static LineRendererEntry[]    s_RendererInstancesAsArray = null;
        private static int                    s_RendererInstanceCount = 0;

        private static void UpdateInstanceArray()
        {
            s_RendererInstanceCount = s_RendererInstances.Count;

            if (s_RendererInstanceCount > 0)
            {
                s_RendererInstancesAsArray = new LineRendererEntry[s_RendererInstanceCount];
                int i = 0;
                foreach (var entry in s_RendererInstances)
                {
                    s_RendererInstancesAsArray[i++] = new LineRendererEntry() {renderer = entry.Key, perRendererResources = entry.Value};
                }
            }
            else
            {
                s_RendererInstancesAsArray = null;
            }
        }

        internal static void AddRenderer(ILineRenderer renderer)
        {
            if (s_RendererInstances.ContainsKey(renderer))
                return;

            s_RendererInstances.TryAdd(renderer, new PerRendererPersistentData());
            UpdateInstanceArray();
        }

        internal static void RemoveRenderer(ILineRenderer renderer)
        {
            s_RendererInstances.Remove(renderer);
            UpdateInstanceArray();
        }

        private static bool HasRenderDatas() => s_RendererInstancesAsArray != null;
        private static RenderData[] GetValidRenderDatas(RenderGraph renderGraph, Camera camera) => s_RendererInstancesAsArray.Where(o => o.renderer.LineRendererIsValid()).Select(o => new RenderData {rendererData = o.renderer.GetLineRendererData(renderGraph, camera), persistentData = o.perRendererResources}).ToArray();
        private static RenderData[] GetRenderDatasInGroup(RenderData[] data, RendererGroup group) => data.Where(o => o.rendererData.group == group).ToArray();
        private static RenderData[] GetRenderDatasNoGroup(RenderData[] data) => data.Where(o => o.rendererData.group == RendererGroup.None).ToArray();
    }
}
