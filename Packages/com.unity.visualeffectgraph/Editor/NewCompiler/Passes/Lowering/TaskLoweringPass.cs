using System.Collections.Generic;
using Unity.GraphCommon.LowLevel.Editor;
using UnityEngine.Pool;

namespace UnityEditor.VFX
{
    /// <summary>
    /// Replaces every logical task with a <see cref="VfxTask"/> bound to the concrete buffers the data
    /// was lowered to, one <see cref="ITaskLowering"/> per kind of task. All kinds are lowered in a
    /// single parents-first walk, so that when a task is rebound the ancestors it depends on have
    /// already been rebound and reported themselves to <see cref="DataConcretizationResult"/> - which
    /// holds however the kinds are interleaved, a sort sitting between two templated tasks included.
    /// </summary>
    class TaskLoweringPass : CompilationPass
    {
        /// <summary>
        /// How one kind of logical task is lowered: what its bindings are called once they name concrete
        /// buffers, and what the lowered task has to carry to be emitted.
        /// </summary>
        public interface ITaskLowering
        {
            /// <summary>Whether this is the lowering for that task.</summary>
            bool CanLower(ITask task);

            /// <summary>
            /// The key the concrete buffer behind <paramref name="dataView"/> should be bound under, or
            /// null to leave that view unbound on the lowered task.
            /// </summary>
            IDataKey GetConcreteBindingKey(ITask task, IDataKey logicalBindingKey, DataView dataView);

            /// <summary>
            /// Fills in what the lowered task carries beyond its bindings - the kind of task the runtime
            /// is to run, the model it came from, whether it runs generated code at all.
            /// </summary>
            void ConfigureTask(ITask oldTask, VfxTask newTask) { }

            /// <summary>
            /// Called once per binding and per view of it that resolved to a buffer, so the same pair of
            /// keys can come by more than once - a task binding several values of one buffer reaches
            /// that buffer through each of them.
            /// </summary>
            void OnBindingLowered(ITask oldTask, VfxTask newTask, IDataKey logicalBindingKey, IDataKey concreteBindingKey) { }

            /// <summary>Called once the lowered task is in the graph, with its bindings in place.</summary>
            void OnTaskLowered(TaskNodeId oldId, TaskNodeId newId, ref CompilationContext context) { }

            /// <summary>
            /// Called once every task of every kind has been lowered, for whatever has to be settled
            /// against the whole concrete graph rather than one task at a time.
            /// </summary>
            void OnLoweringComplete(ref CompilationContext context) { }
        }

        /// <summary>
        /// An <see cref="ITaskLowering"/> for one kind of task, which is what the kind test and the
        /// casts amount to for every lowering there is.
        /// </summary>
        public abstract class TaskLowering<T> : ITaskLowering where T : class, ITask
        {
            public bool CanLower(ITask task) => task is T;

            public abstract IDataKey GetConcreteBindingKey(T task, IDataKey logicalBindingKey, DataView dataView);

            public virtual void ConfigureTask(T oldTask, VfxTask newTask) { }

            public virtual void OnBindingLowered(T oldTask, VfxTask newTask, IDataKey logicalBindingKey,
                IDataKey concreteBindingKey) { }

            public virtual void OnTaskLowered(TaskNodeId oldId, TaskNodeId newId, ref CompilationContext context) { }

            public virtual void OnLoweringComplete(ref CompilationContext context) { }

            IDataKey ITaskLowering.GetConcreteBindingKey(ITask task, IDataKey logicalBindingKey, DataView dataView)
                => GetConcreteBindingKey((T)task, logicalBindingKey, dataView);

            void ITaskLowering.ConfigureTask(ITask oldTask, VfxTask newTask)
                => ConfigureTask((T)oldTask, newTask);

            void ITaskLowering.OnBindingLowered(ITask oldTask, VfxTask newTask, IDataKey logicalBindingKey,
                IDataKey concreteBindingKey)
                => OnBindingLowered((T)oldTask, newTask, logicalBindingKey, concreteBindingKey);
        }

        readonly ITaskLowering[] m_Lowerings;
        readonly Dictionary<TaskNodeId, TaskNodeId> m_TaskReplacements = new();

        /// <summary>
        /// The tasks lowered so far, which is what tells a parent that keeps its logical node from one
        /// that has been rebound - see <see cref="DataConcretizationResult.CollectConcreteParents"/>.
        /// </summary>
        readonly HashSet<TaskNodeId> m_LoweredTasks = new();

        DataConcretizationResult m_DataConcretizationResult;

        public TaskLoweringPass(params ITaskLowering[] lowerings)
        {
            m_Lowerings = lowerings;
        }

        public bool Execute(ref CompilationContext context)
        {
            m_TaskReplacements.Clear();
            m_LoweredTasks.Clear();
            m_DataConcretizationResult = context.data.Get<DataConcretizationResult>();
            var particleSystemContainer = context.data.Get<ParticleSystemContainer>();

            var traverser = context.graph.CreateTraverser();
            using (ListPool<(TaskNodeId taskNodeId, ITaskLowering lowering)>.Get(out var targets))
            {
                // Collected first: lowering adds tasks to the very collection the traversal walks.
                foreach (var taskNode in traverser.TraverseTasksParentsFirst())
                {
                    var lowering = FindLowering(taskNode.Task);
                    if (lowering != null)
                        targets.Add((taskNode.Id, lowering));
                }

                foreach (var (taskNodeId, lowering) in targets)
                {
                    var newTaskNodeId = LowerTask(context.graph.TaskNodes[taskNodeId], context.graph, lowering);
                    var newTaskNode = context.graph.TaskNodes[newTaskNodeId];
                    var newTask = (VfxTask)newTaskNode.Task;
                    particleSystemContainer.ReplaceTask(taskNodeId,
                        new ParticleSystemContainer.Task(newTaskNode.Name, newTaskNodeId));
                    lowering.OnTaskLowered(taskNodeId, newTaskNodeId, ref context);
                }
            }

            foreach (var lowering in m_Lowerings)
                lowering.OnLoweringComplete(ref context);

            foreach (var oldTaskNodeId in m_TaskReplacements.Keys)
                context.graph.RemoveTask(oldTaskNodeId);

            return true;
        }

        ITaskLowering FindLowering(ITask task)
        {
            foreach (var lowering in m_Lowerings)
            {
                if (lowering.CanLower(task))
                    return lowering;
            }
            return null;
        }

        static bool ShouldPassThroughNonConcreteView(DataView dataView)
            => dataView.DataDescription is ValueData && !dataView.Parent.HasValue;

        TaskNodeId LowerTask(TaskNode oldTaskNode, IGraph graph, ITaskLowering lowering)
        {
            var oldTask = oldTaskNode.Task;
            var oldTaskId = oldTaskNode.Id;
            var newTask = new VfxTask();
            lowering.ConfigureTask(oldTask, newTask);

            using var _ = ListPool<(DataViewId view, IDataKey key, DataNodeId originalDataNode, DataViewId originalDataView)>
                .Get(out var concreteBindings);
            using var __ = HashSetPool<(DataViewId, IDataKey)>.Get(out var boundViews);

            // Per binding rather than per data node: a task binding the same container twice - a sort,
            // reading the keys it is to sort and writing the indirection it sorts them into - has one
            // data node for both, and only the binding knows which views are its own.
            foreach (var binding in oldTaskNode.DataBindings)
            {
                var logicalBindingKey = binding.BindingDataKey;
                var dataNodeId = binding.DataNode.Id;

                foreach (var dataView in binding.UsedDataViews)
                {
                    var dataViewId = dataView.Id;

                    if (!m_DataConcretizationResult.TryGetConcreteDataView(dataViewId, out var concreteViewId))
                    {
                        if (ShouldPassThroughNonConcreteView(dataView))
                        {
                            concreteBindings.Add((dataViewId, logicalBindingKey, dataNodeId, dataViewId));
                            RegisterUsage(newTask, logicalBindingKey, binding.Reads(dataViewId), binding.Writes(dataViewId));
                            lowering.OnBindingLowered(oldTask, newTask, logicalBindingKey, logicalBindingKey);
                        }
                        continue;
                    }

                    var bindingKey = lowering.GetConcreteBindingKey(oldTask, logicalBindingKey, dataView);
                    if (bindingKey == null)
                        continue;

                    if (boundViews.Add((concreteViewId, bindingKey)))
                        concreteBindings.Add((concreteViewId, bindingKey, dataNodeId, dataViewId));
                    RegisterUsage(newTask, bindingKey, binding.Reads(dataViewId), binding.Writes(dataViewId));
                    lowering.OnBindingLowered(oldTask, newTask, logicalBindingKey, bindingKey);
                }
            }

            var newTaskNodeId = graph.AddTask(newTask, oldTaskNode.Name);
            m_TaskReplacements.Add(oldTaskId, newTaskNodeId);
            m_LoweredTasks.Add(oldTaskId);

            using (ListPool<DataNodeId>.Get(out var parentNodes))
            {
                foreach (var (view, key, logicalDataNode, logicalView) in concreteBindings)
                {
                    m_DataConcretizationResult.CollectConcreteParents(
                        graph.DataNodes[logicalDataNode], logicalView, parentNodes, m_LoweredTasks);

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
