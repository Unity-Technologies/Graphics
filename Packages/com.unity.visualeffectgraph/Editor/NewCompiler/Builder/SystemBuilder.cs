using Unity.GraphCommon.LowLevel.Editor;
using UnityEngine.VFX;

namespace UnityEditor.VFX
{
    abstract class SystemBuilder
    {
        BuildContext m_Context;

        protected VFXGraph Graph => m_Context.Graph;
        protected VFXCompilationMode CompilationMode => m_Context.CompilationMode;
        protected TaskGraphBuilder TaskGraphBuilder => m_Context.TaskGraphBuilder;
        protected VFXSystemNames SystemNames => m_Context.SystemNames;
        protected VFXExpressionGraph ExpressionGraph => m_Context.ExpressionGraph;

        public virtual void BeginBuilding(BuildContext context)
        {
            m_Context = context;
        }

        public virtual bool ProcessContext(VFXContext context) => false;

        public abstract void Build();

        public virtual void EndBuilding()
        {
            m_Context = null;
        }

        protected void RegisterDataView(object key, DataViewId dataViewId) => m_Context.RegisterDataView(key, dataViewId);

        protected bool TryGetDataView(object key, out DataViewId dataViewId) => m_Context.TryGetDataView(key, out dataViewId);

        protected bool TryGetDataView(VFXExpression expression, out DataViewId dataViewId)
        {
            if (TryGetDataView((object)expression, out dataViewId))
                return true;

            var task = new LegacyExpressionTask(expression);
            var taskId = TaskGraphBuilder.AddTask(task);

            var parents = expression.parents;
            for (int i = 0; i < parents.Length; ++i)
            {
                TryGetDataView(parents[i], out var parentDataViewId);
                TaskGraphBuilder.BindData(taskId, new IndexDataKey(i), parentDataViewId, BindingUsage.Read);
            }

            var dataDescription = ValueData.Create(VFXExpression.TypeToType(expression.valueType));
            var expressionContainerName = $"{expression.GetType().Name}_{TaskGraphBuilder.Graph.DataContainers.Count}";
            dataViewId = TaskGraphBuilder.AddData(expressionContainerName, dataDescription);
            TaskGraphBuilder.BindData(taskId, LegacyExpressionTask.Value, dataViewId, BindingUsage.Write);

            RegisterDataView(expression, dataViewId);
            return true;
        }
    }
}
