using System;
using UnityEngine.Rendering;

namespace UnityEngine.Rendering.RenderGraphModule
{
    public partial class RenderGraph
    {
        //TODO(ddebaets) move old compile func/members over

        NativeRenderPassCompiler.NativePassCompiler nativeCompiler = null;

        internal NativeRenderPassCompiler.NativePassCompiler CompileNativeRenderGraph(int graphHash)
        {
            using (new ProfilingScope(m_RenderGraphContext.cmd, ProfilingSampler.Get(RenderGraphProfileId.CompileRenderGraph)))
            {
                if (nativeCompiler == null)
                    nativeCompiler = new NativeRenderPassCompiler.NativePassCompiler(m_CompilationCache);

                bool compilationIsCached = nativeCompiler.Initialize(m_Resources, m_RenderPasses, m_DebugParameters.disablePassCulling, name, m_EnableCompilationCaching, graphHash, m_ExecutionCount);
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
