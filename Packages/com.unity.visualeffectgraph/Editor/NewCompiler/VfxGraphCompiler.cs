using Unity.GraphCommon.LowLevel.Editor;
using Unity.Profiling;
using UnityEngine.VFX;

namespace UnityEditor.VFX
{
    class VfxGraphCompiler : IVFXCompiler
    {
        private Compiler<VfxGraphLegacyCompilationOutput> m_GraphCompiler;
        private IntermediateGraphBuilder m_GraphBuilder = new();

        private DataDescriptionWriterRegistry m_DataWriter;

        static readonly ProfilerMarker k_CompileMarker = new("VfxGraphCompiler.Compile");

        public VfxGraphCompiler()
        {
            var attributeDataWriter = new AttributeDataDescriptionWriter();

            m_DataWriter = new();
            m_DataWriter.Register(attributeDataWriter);
            m_DataWriter.Register(new ParticleSystemDataDescriptionWriter(attributeDataWriter));
            m_DataWriter.Register(new StructuredDataDescriptionWriter());
            m_DataWriter.Register(new EventListDataDescriptionWriter(attributeDataWriter));

            m_GraphCompiler = new(new VfxGraphLegacyOutputPass(),
                new AttributeLayoutPass(),
                new VfxGraphLegacyParticleSystemPass(),
                new DataLayoutPass(),
                new VfxTemplateCodeGenerationPass(m_DataWriter));
        }

        // For now the new compiler performs a full recompilation
        public VFXExpressionCompiledData CompileExpressionsOnly(VFXGraph graph, VFXCompilationMode compilationMode)
        {
            return Compile(graph, compilationMode, false).compiledData;
        }

        public VFXCompileOutput Compile(VFXGraph graph, VFXCompilationMode compilationMode, bool generateShadersDebugSymbols)
        {
            // One of supported SRPs is not current SRP
            if (VFXLibrary.currentSRPBinder == null)
            {
                return new() { success = false };
            }

            using var _ = k_CompileMarker.Auto();

            var intermediateGraph = m_GraphBuilder.BuildGraph(graph, compilationMode);

            // TODO: setup compilation mode and shader debug symbols
            var compilationResult = m_GraphCompiler.Compile(intermediateGraph);

            VFXCompileOutput output = new()
            {
                success = true, // TODO
                sourceDependencies = new(), // TODO
                assetDesc = compilationResult.result.GenerateAssetDesc(),
                compiledData = compilationResult.result.CreateCompiledData(),
            };

            return output;
        }
    }
}
