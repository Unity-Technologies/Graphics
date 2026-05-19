using Unity.GraphCommon.LowLevel.Editor;
using UnityEngine.VFX;

namespace UnityEditor.VFX
{
    class VfxGraphCompiler
    {
        private Compiler<VfxGraphLegacyCompilationOutput> m_GraphCompiler;
        private VfxIntermediateGraphBuilder m_GraphBuilder = new();

        private DataDescriptionWriterRegistry m_DataWriter;

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
                new TemplateCodeGenerationPass(m_DataWriter));
        }

        public VFXGraphCompiledData.VFXCompileOutput Compile(VFXGraph graph, VFXCompilationMode compilationMode, bool generateShadersDebugSymbols)
        {
            // One of supported SRPs is not current SRP
            if (VFXLibrary.currentSRPBinder == null)
            {
                return new() { success = false };
            }

            var intermediateGraph = m_GraphBuilder.BuildGraph(graph, compilationMode);

            // TODO: setup compilation mode and shader debug symbols
            var compilationResult = m_GraphCompiler.Compile(intermediateGraph);

            VFXGraphCompiledData.VFXCompileOutput output = new()
            {
                success = true, // TODO
                sourceDependencies = new(), // TODO
                assetDesc = compilationResult.result.GenerateAssetDesc()
            };

            return output;
        }
    }
}
