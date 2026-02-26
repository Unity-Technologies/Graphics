using System.Collections.Generic;
using Unity.GraphCommon.LowLevel.Editor;
using UnityEngine;

namespace UnityEditor.VFX
{
    class VfxGraphLegacyTemplatedTaskPass : CompilationPass
    {
        struct TemplatedTaskNodeCache
        {
            public TaskNodeId taskNodeId;
            public TemplatedTask task;
        }
        struct SystemTaskNodeCache
        {
            public TaskNodeId taskNodeId;
            public PlaceholderSystemTask task;
        }
        public bool Execute(ref CompilationContext context)
        {
            Dictionary<ParticleData, List<TemplatedTaskNodeCache>> taskNodesGroups = new();
            Dictionary<ParticleData, SystemTaskNodeCache> systemTasks = new();
            foreach (var taskNode in context.graph.TaskNodes)
            {
                if (taskNode.Task is TemplatedTask or PlaceholderSystemTask)
                {
                    foreach (var dataNode in taskNode.DataNodes)
                    {
                        if (dataNode.DataContainer.RootDataView.DataDescription is ParticleData particleData)
                        {
                            if (taskNode.Task is TemplatedTask templatedTask)
                            {
                                if (!taskNodesGroups.ContainsKey(particleData))
                                {
                                    taskNodesGroups.Add(particleData, new());
                                }

                                taskNodesGroups[particleData].Add(new() { taskNodeId = taskNode.Id, task = templatedTask });
                            }
                            else if (taskNode.Task is PlaceholderSystemTask systemTask)
                            {
                                systemTasks.Add(particleData, new() { taskNodeId = taskNode.Id, task = systemTask });
                            }
                        }
                    }
                }
            }

            foreach (var particleData in systemTasks.Keys)
            {
                var systemTask = systemTasks[particleData];

                StructuredData graphValuesBuffer = new StructuredData();
                var graphValuesId = context.graph.AddData("GraphValuesBuffer", graphValuesBuffer);
                context.graph.BindData(systemTask.taskNodeId, TemplatedTask.GraphValuesKey, graphValuesId, BindingUsage.Write);

                // Add a default subdata for ContextData, which is expected from the C++ runtime.
                graphValuesBuffer.AddSubdata(TemplatedTask.ContextDataKey, ValueData.Create(typeof(Vector4)));

                foreach (var taskNode in taskNodesGroups[particleData])
                {
                    foreach (var (subDatakey, expression) in taskNode.task.Expressions)
                    {
                        if (graphValuesBuffer.GetSubdata(subDatakey) != null) continue; //Duplicate subdata, skip

                        var inputDataViewId = BuildExpressionGraph(context.graph, expression);

                        if (expression.IsConstant || typeof(Texture).IsAssignableFrom(expression.ResultType)) // Do not go through graphValues
                        {
                            context.graph.BindData(taskNode.taskNodeId, subDatakey, inputDataViewId, BindingUsage.Read);
                        }
                        else
                        {
                            if (graphValuesBuffer.AddSubdata(subDatakey, ValueData.Create(expression.ResultType)))
                            {
                                context.graph.BindData(systemTask.taskNodeId, subDatakey, inputDataViewId, BindingUsage.Read);
                            }
                            var subdataViewId = context.graph.GetSubdata(graphValuesId, subDatakey);
                            context.graph.BindData(taskNode.taskNodeId, subDatakey, subdataViewId, BindingUsage.Read);
                        }
                    }
                    var contextDataViewId = context.graph.GetSubdata(graphValuesId, TemplatedTask.ContextDataKey);
                    context.graph.BindData(taskNode.taskNodeId, TemplatedTask.ContextDataKey,  contextDataViewId, BindingUsage.Read);
                }
                foreach (var (subDatakey, expression) in systemTask.task.Expressions)
                {
                    var inputDataViewId = BuildExpressionGraph(context.graph, expression);
                    context.graph.BindData(systemTask.taskNodeId, subDatakey, inputDataViewId, BindingUsage.Read);
                }
            }

            return true;
        }

        private DataViewId BuildExpressionGraph(IMutableGraph graph, IExpression expression)
        {
            // TODO: This is duplicated code (see BlockBuilder). Find a proper place for expression graph
            // TODO: Cache expressions per-task?
            var taskId = graph.AddTask(new ExpressionTask(expression));
            var parents = expression.Parents;
            for (int i = 0; i < parents.Count; ++i)
            {
                graph.BindData(taskId, new IndexDataKey(i),BuildExpressionGraph(graph, parents[i]), BindingUsage.Read);
            }
            var dataDescription = expression is IValueExpression valueExpression && expression.IsConstant ? ConstantValueData.Create(valueExpression.Value, expression.ResultType) : ValueData.Create(expression.ResultType);
            var dataId = graph.AddData(expression.GetType().Name, dataDescription);
            graph.BindData(taskId, ExpressionTask.Value, dataId, BindingUsage.Write);
            return dataId;
        }
    }
}
