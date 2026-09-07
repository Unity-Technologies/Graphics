using System.Collections.Generic;
using Unity.GraphCommon.LowLevel.Editor;
using Unity.Profiling;
using UnityEngine.VFX;
using UnityEngine;

namespace UnityEditor.VFX
{
    class IntermediateGraphBuilder
    {
        static readonly ProfilerMarker k_BuildGraphMarker = new("IntermediateGraphBuilder.BuildGraph");

        public static IDataKey DefaultAttributeKey { get; } = new NameDataKey("Attributes");
        public static IDataKey SourceAttributeKey { get; } = new NameDataKey("SourceAttributes");

        SystemBuilder m_GraphBuilder;
        BuildContext m_BuildContext = new();
        HashSet<ScriptableObject> m_Models = new();

        public IntermediateGraphBuilder()
        {
            // Order is relevant, it is the order in which they are built
            m_GraphBuilder = new CompositeSystemBuilder(
                new ExpressionBuilder(),
                new SpawnerSystemBuilder(),
                new GpuEventBuilder(),
                new ParticleSystemBuilder()
            );
        }

        public IReadOnlyGraph BuildGraph(VFXGraph graph, VFXCompilationMode compilationMode)
        {
            using var _ = k_BuildGraphMarker.Auto();

            var taskGraphBuilder = new TaskGraphBuilder();

            m_BuildContext.Begin(graph, compilationMode, taskGraphBuilder);

            m_GraphBuilder.BeginBuilding(m_BuildContext);

            graph.CollectDependencies(m_Models, false);

            // This is required for implicit blocks (like Age, Reap, EulerIntegration) to work correctly. Will be removed in the future
            PrepareImplicitBlocks();

            foreach (var model in m_Models)
            {
                if (model is VFXContext context && context.CanBeCompiled())
                {
                    m_GraphBuilder.ProcessContext(context);
                }
            }

            m_GraphBuilder.Build();

            m_GraphBuilder.EndBuilding();

            Clear();

            return taskGraphBuilder.EndBuilding();
        }

        void PrepareImplicitBlocks()
        {
            var compilableData = new List<VFXData>();

            foreach (var model in m_Models)
            {
                if (model is VFXData data && data.CanBeCompiled())
                    compilableData.Add(data);
            }

            foreach (var data in compilableData)
                data.InitImplicitContexts();

            foreach (var data in compilableData)
                data.CollectAttributes();

            foreach (var data in compilableData)
                data.ProcessDependencies();
        }

        void Clear()
        {
            m_BuildContext.Clear();
            m_Models.Clear();
        }
    }
}
