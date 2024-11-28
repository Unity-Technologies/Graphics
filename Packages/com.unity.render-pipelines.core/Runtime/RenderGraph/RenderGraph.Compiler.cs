using UnityEngine.Rendering.RenderGraphModule.NativeRenderPassCompiler;

namespace UnityEngine.Rendering.RenderGraphModule
{
    public partial class RenderGraph
    {
        //TODO(ddebaets) move old compile func/members over

        NativePassCompiler nativeCompiler = null;

        internal NativePassCompiler CompileNativeRenderGraph(int graphHash)
        {
            using (new ProfilingScope(m_RenderGraphContext.cmd, ProfilingSampler.Get(RenderGraphProfileId.CompileRenderGraph)))
            {
                if (nativeCompiler == null)
                    nativeCompiler = new NativePassCompiler(m_CompilationCache);

                bool compilationIsCached = nativeCompiler.Initialize(m_Resources, m_RenderPasses, m_DebugParameters, name, m_EnableCompilationCaching, graphHash, m_ExecutionCount);
                if (!compilationIsCached)
                    nativeCompiler.Compile(m_Resources);

                var passData = nativeCompiler.contextData.passData;
                int numPasses = passData.Length;
                for (int i = 0; i < numPasses; ++i)
                {
                    if (passData.ElementAt(i).culled)
                        continue;
                    var rp = m_RenderPasses[i];
                    m_RendererLists.AddRange(rp.usedRendererListList);
                }

                m_Resources.CreateRendererLists(m_RendererLists, m_RenderGraphContext.renderContext, m_RendererListCulling);

                return nativeCompiler;
            }
        }

        void ExecuteNativeRenderGraph()
        {
            using (new ProfilingScope(m_RenderGraphContext.cmd, ProfilingSampler.Get(RenderGraphProfileId.ExecuteRenderGraph)))
            {
                nativeCompiler.ExecuteGraph(m_RenderGraphContext, m_Resources, m_RenderPasses);

                if (m_RenderGraphContext.contextlessTesting == false)
                    m_RenderGraphContext.renderContext.ExecuteCommandBuffer(m_RenderGraphContext.cmd);

                m_RenderGraphContext.cmd.Clear();
            }
        }
    }
}
