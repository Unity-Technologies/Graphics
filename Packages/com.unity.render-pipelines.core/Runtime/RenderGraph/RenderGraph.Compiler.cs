using System;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.RenderGraphModule
{
    public partial class RenderGraph
    {
        //TODO(ddebaets) move old compile func/members over

        NativeRenderPassCompiler.PassCommandBuffer<NativeRenderPassCompiler.DefaultExecution> nativeRenderPasses = new NativeRenderPassCompiler.PassCommandBuffer<NativeRenderPassCompiler.DefaultExecution>();
        NativeRenderPassCompiler.NativePassCompiler nativeCompiler = null;

        internal NativeRenderPassCompiler.NativePassCompiler CompileNativeRenderGraph()
        {
            using (new ProfilingScope(m_RenderGraphContext.cmd, ProfilingSampler.Get(NativeRenderPassCompiler.NativePassCompiler.NativeCompilerProfileId.NRPRGComp_Compile)))
            {
                if (nativeCompiler == null)
                {
                    nativeCompiler = new NativeRenderPassCompiler.NativePassCompiler();
                }
                nativeCompiler.Clear();
                nativeCompiler.Initialize(this.m_Resources, this.m_RenderPasses, this.m_DebugParameters.disablePassCulling, this.name);

                nativeCompiler.GeneratePassCommandBuffer(ref nativeRenderPasses);

                nativeCompiler.OutputDebugGraph();

                foreach (var pass in nativeCompiler.ActivePasses)
                {
                    var rp = m_RenderPasses[pass];
                    m_RendererLists.AddRange(rp.usedRendererListList);
                }

                m_Resources.CreateRendererLists(m_RendererLists, m_RenderGraphContext.renderContext, m_RendererListCulling);

                return nativeCompiler;
            }
        }

        void ExecuteNativeRenderGraph()
        {
            using (new ProfilingScope(m_RenderGraphContext.cmd, ProfilingSampler.Get(NativeRenderPassCompiler.NativePassCompiler.NativeCompilerProfileId.NRPRGComp_Execute)))
            {
                nativeRenderPasses.Execute(m_RenderGraphContext, m_Resources, m_RenderPasses);
            }
            m_RenderGraphContext.renderContext.ExecuteCommandBuffer(m_RenderGraphContext.cmd);
            m_RenderGraphContext.cmd.Clear();
        }
    }
}
