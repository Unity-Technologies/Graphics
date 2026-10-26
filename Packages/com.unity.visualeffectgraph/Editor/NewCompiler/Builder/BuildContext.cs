using System.Collections.Generic;
using Unity.GraphCommon.LowLevel.Editor;
using UnityEngine.VFX;
using UnityEngine;

namespace UnityEditor.VFX
{
    class BuildContext
    {
        public VFXGraph Graph { get; private set; }
        public VFXCompilationMode CompilationMode { get; private set; }
        public TaskGraphBuilder TaskGraphBuilder { get; private set; }
        public VFXSystemNames SystemNames { get; } = new();
        public VFXExpressionGraph ExpressionGraph { get; } = new();

        readonly Dictionary<object, DataViewId> m_DataViews = new();

        public void Begin(VFXGraph graph, VFXCompilationMode compilationMode, TaskGraphBuilder taskGraphBuilder)
        {
            Graph = graph;
            CompilationMode = compilationMode;
            TaskGraphBuilder = taskGraphBuilder;
        }

        public void RegisterDataView(object key, DataViewId dataViewId)
        {
            Debug.Assert(key != null);
            m_DataViews[key] = dataViewId;
        }

        public bool TryGetDataView(object key, out DataViewId dataViewId)
        {
            Debug.Assert(key != null);
            return m_DataViews.TryGetValue(key, out dataViewId);
        }

        public void Clear()
        {
            Graph = null;
            CompilationMode = default;
            TaskGraphBuilder = null;
            m_DataViews.Clear();
            ExpressionGraph.Clear();
            SystemNames.Clear();
        }
    }
}
