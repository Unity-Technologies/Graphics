using System.Collections.Generic;
using Unity.GraphCommon.LowLevel.Editor;
using UnityEngine.Pool;

namespace UnityEditor.VFX
{
    /// <summary>
    /// Replaces a kind of logical task with a <see cref="VfxTask"/> bound to the concrete buffers the data
    /// was lowered to. Tasks are replaced parents-first so that, when a replacement resolves the node writing
    /// a buffer it reads, the ancestors it depends on have already been rebound and reported themselves to
    /// <see cref="DataConcretizationResult"/>.
    /// </summary>
    abstract class TaskReplacementPass : CompilationPass
    {
        readonly Dictionary<TaskNodeId, TaskNodeId> m_TaskReplacements = new();
        protected DataConcretizationResult m_DataConcretizationResult;

        public bool Execute(ref CompilationContext context)
        {
            m_TaskReplacements.Clear();
            m_DataConcretizationResult = context.data.Get<DataConcretizationResult>();

            var traverser = context.graph.CreateTraverser();
            using (ListPool<TaskNodeId>.Get(out var taskNodeIds))
            {
                foreach (var taskNode in traverser.TraverseTasksParentsFirst())
                {
                    if (IsTargetTask(taskNode.Task))
                        taskNodeIds.Add(taskNode.Id);
                }

                foreach (var taskNodeId in taskNodeIds)
                {
                    var newTaskNodeId = AddVfxTaskNode(context.graph.TaskNodes[taskNodeId], context.graph);
                    OnTaskNodeReplaced(taskNodeId, newTaskNodeId, ref context);
                }
            }

            PatchSystemContainer(m_TaskReplacements, ref context);

            foreach (var oldTaskNodeId in m_TaskReplacements.Keys)
                context.graph.RemoveTask(oldTaskNodeId);

            return true;
        }

        protected abstract bool IsTargetTask(ITask task);

        /// <summary>
        /// The key the concrete buffer behind <paramref name="dataView"/> should be bound under, or null to
        /// leave that view unbound on the replacement task.
        /// </summary>
        protected abstract IDataKey GetConcreteBindingKey(DataView dataView);

        protected abstract void PatchSystemContainer(
            Dictionary<TaskNodeId, TaskNodeId> replacements,
            ref CompilationContext context);

        protected virtual void OnTaskNodeReplaced(TaskNodeId oldId, TaskNodeId newId, ref CompilationContext context) { }

        static bool ShouldPassThroughNonConcreteView(DataView dataView)
            => dataView.DataDescription is ValueData && !dataView.Parent.HasValue;

        TaskNodeId AddVfxTaskNode(TaskNode oldTaskNode, IGraph graph)
        {
            var newTask = new VfxTask();
            var oldTaskId = oldTaskNode.Id;

            using var bindingViews = ListPool<(DataViewId view, IDataKey key, DataNodeId originalDataNode, DataViewId originalDataView)>
                .Get(out var concreteBindings);
            using var seenRental = HashSetPool<DataViewId>.Get(out var boundViews);

            foreach (var binding in oldTaskNode.DataBindings)
            {
                var dataNode = binding.DataNode;
                foreach (var dataView in dataNode.UsedDataViews)
                {
                    var dataViewId = dataView.Id;

                    if (!m_DataConcretizationResult.TryGetConcreteDataView(dataViewId, out var concreteViewId))
                    {
                        if (ShouldPassThroughNonConcreteView(dataView))
                        {
                            concreteBindings.Add((dataViewId, binding.BindingDataKey, dataNode.Id, dataViewId));
                            RegisterUsage(newTask, binding.BindingDataKey, dataNode.IsRead(dataViewId), dataNode.IsWritten(dataViewId));
                        }
                        continue;
                    }

                    var bindingKey = GetConcreteBindingKey(dataView);
                    if (bindingKey == null)
                        continue;

                    if (boundViews.Add(concreteViewId))
                        concreteBindings.Add((concreteViewId, bindingKey, dataNode.Id, dataViewId));
                    RegisterUsage(newTask, bindingKey, dataNode.IsRead(dataViewId), dataNode.IsWritten(dataViewId));
                }
            }

            var newTaskNodeId = graph.AddTask(newTask, oldTaskNode.Name);
            m_TaskReplacements.Add(oldTaskId, newTaskNodeId);

            using (ListPool<DataNodeId>.Get(out var parentNodes))
            {
                foreach (var (view, key, logicalDataNode, logicalView) in concreteBindings)
                {
                    m_DataConcretizationResult.CollectConcreteParents(
                        graph.DataNodes[logicalDataNode], logicalView, parentNodes);

                    var newDataNodeId = graph.BindData(newTaskNodeId, key, view, parentNodes, BindingUsage.Unknown);
                    m_DataConcretizationResult.AddDataNodeMapping(logicalDataNode, logicalView, newDataNodeId);
                }
            }

            return newTaskNodeId;
        }

        static void RegisterUsage(VfxTask newTask, IDataKey bindingKey, bool read, bool write)
        {
            if (read)
                newTask.AddReadBinding(bindingKey);
            if (write)
                newTask.AddWriteBinding(bindingKey);
        }
    }
}
