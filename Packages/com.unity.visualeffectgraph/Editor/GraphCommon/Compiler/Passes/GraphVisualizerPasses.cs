using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Unity.GraphCommon.LowLevel.Editor
{
    class GraphVisualizerTaskPass : CompilationPass
    {
        private string Path { get; }

        public GraphVisualizerTaskPass(string path)
        {
            Debug.Assert(path != null);
            Path = path;
        }

        public bool Execute(ref CompilationContext context)
        {
            StringBuilder sb = new StringBuilder();
            using (new GraphVisualizer.GraphScope(sb))
            {
                foreach (var taskNode in context.graph.TaskNodes)
                {
                    var taskLabel = GraphVisualizerPassesHelpers.TaskLabel(taskNode);
                    GraphVisualizer.AddNode(sb, taskNode.Id.Index, taskLabel);
                    foreach (var child in taskNode.Children)
                    {
                        GraphVisualizer.AddLink(sb, taskNode.Id.Index, child.Id.Index);
                    }
                }
            }
            GraphVisualizer.SaveFile(sb, Path);
            return true;
        }
    }

    class GraphVisualizerDataPass : CompilationPass
    {
        private string Path { get; }

        public GraphVisualizerDataPass(string path)
        {
            Debug.Assert(path != null);
            Path = path;
        }

        public bool Execute(ref CompilationContext context)
        {
            StringBuilder sb = new StringBuilder();
            using (new GraphVisualizer.GraphScope(sb))
            {
                foreach (var dataNode in context.graph.DataNodes)
                {
                    var taskLabel = $"{(dataNode.TaskNode.Task is Task task ? task.DebugName : dataNode.TaskNode.Task.GetType().Name)} - {dataNode.TaskNode.Id}";
                    var dataLabel = $"{dataNode.Id} ({taskLabel})";
                    GraphVisualizer.AddNode(sb, dataNode.Id.Index, dataLabel);
                    foreach (var child in dataNode.Children)
                    {
                        GraphVisualizer.AddLink(sb, dataNode.Id.Index, child.Id.Index);
                    }
                }
            }
            GraphVisualizer.SaveFile(sb, Path);
            return true;
        }
    }

    class GraphVisualizerDataViewAccessesPass : CompilationPass
    {
        private string Path { get; }
        public GraphVisualizerDataViewAccessesPass(string path)
        {
            Debug.Assert(path != null);
            Path = path;
        }

        //TODO: this is a temporary solution, should be replaced with a more generic way to check if a data view is used
        static bool Contains(IEnumerable<DataView> dataViews, DataViewId dataViewId)
        {
            foreach (var dataView in dataViews)
            {
                if(dataView.Id.Equals(dataViewId)) return true;
            }
            return false;
        }
        public bool Execute(ref CompilationContext context)
        {
            StringBuilder sb = new StringBuilder();

            Dictionary<DataViewId, LinearGraph<DataNodeId>> accessGraphs = new Dictionary<DataViewId, LinearGraph<DataNodeId>>();
            Dictionary<DataViewId, Dictionary<DataNodeId, int>> idToIndex = new ();

            var traverser = context.graph.CreateTraverser();

            foreach (var rootDataNode in traverser.TraverseDataRoots())
            {
                foreach (var dataNode in traverser.TraverseDataDownwards(rootDataNode))
                {
                    foreach (var dataView in dataNode.UsedDataViews)
                    {
                        if (!accessGraphs.ContainsKey(dataView.Id))
                        {
                            accessGraphs.Add(dataView.Id, new LinearGraph<DataNodeId>());
                            idToIndex.Add(dataView.Id, new Dictionary<DataNodeId, int>());
                        }

                        int index = accessGraphs[dataView.Id].AddItem(dataNode.Id);
                        idToIndex[dataView.Id].Add(dataNode.Id, index);

                        // TODO: probably should unify "used" data view + an enum for read/write
                        if (Contains(dataNode.ReadDataViews, dataView.Id))
                        {
                            foreach (var parentDataNode in traverser.TraverseDataUpwards(dataNode))
                            {
                                if(dataNode.Id.Equals(parentDataNode.Id))
                                    continue;
                                if (Contains(parentDataNode.WrittenDataViews, dataView.Id))
                                {
                                    accessGraphs[dataView.Id].Connect(idToIndex[dataView.Id][parentDataNode.Id],
                                        idToIndex[dataView.Id][dataNode.Id]);
                                    break;
                                }
                            }
                        }
                    }
                }
            }


            using (new GraphVisualizer.GraphScope(sb))
            {
                foreach (var (dataViewId, graph) in accessGraphs)
                {
                    string nodeLabel = $"{context.graph.DataViews[dataViewId].DataDescription.Name} ({dataViewId})";
                    using (new GraphVisualizer.ClusterScope(dataViewId.Index, nodeLabel, sb))
                    {
                        foreach (var node in graph)
                        {
                            int nodeIndex = node.Data.Index | dataViewId.Index << 16;

                            GraphVisualizer.AddNode(sb, nodeIndex, node.Data.ToString());
                            foreach (var parentNode in node.Parents)
                            {
                                int parentNodeIndex = parentNode.Data.Index | dataViewId.Index << 16;
                                GraphVisualizer.AddLink(sb, parentNodeIndex, nodeIndex);
                            }
                        }
                    }
                }
            }

            GraphVisualizer.SaveFile(sb, Path);
            return true;
        }
    }
    class GraphVisualizerDataViewInTaskPass : CompilationPass
    {
        private string Path { get; }
        public GraphVisualizerDataViewInTaskPass(string path)
        {
            Debug.Assert(path != null);
            Path = path;
        }
        public bool Execute(ref CompilationContext context)
        {
            var traverser = context.graph.CreateTraverser();

            StringBuilder sb = new StringBuilder();
            using (new GraphVisualizer.GraphScope(sb))
            {
                foreach (var taskNode in context.graph.TaskNodes)
                {
                    var taskLabel = GraphVisualizerPassesHelpers.TaskLabel(taskNode);

                    using (new GraphVisualizer.ClusterScope(taskNode.Id.Index, taskLabel, sb))
                    {
                        foreach (var dataNode in taskNode.DataNodes)
                        {
                            foreach (var dataView in dataNode.UsedDataViews)
                            {
                                int nodeIndex = dataNode.Id.Index | dataView.Id.Index << 16;
                                var dataLabel = $"{dataView.DataContainer.Name}/{dataView.SubDataKey} ({dataView.Id})";
                                GraphVisualizer.AddNode(sb, nodeIndex, dataLabel);
                            }
                        }
                    }
                }

                foreach (var dataNode in context.graph.DataNodes)
                {
                    foreach (var dataView in dataNode.UsedDataViews)
                    {
                        int nodeIndex = dataNode.Id.Index | dataView.Id.Index << 16;
                        traverser.TraverseDataUpwards(dataNode,
                            parentDataNode =>
                            {
                                if (parentDataNode.Id.Equals(dataNode.Id))
                                    return true;
                                foreach (var parentDataView in parentDataNode.WrittenDataViews)
                                {
                                    if (dataView.Id.Equals(parentDataView.Id))
                                    {
                                        int parentNodeIndex = parentDataNode.Id.Index | parentDataView.Id.Index << 16;
                                        GraphVisualizer.AddLink(sb, parentNodeIndex, nodeIndex);
                                        return false;
                                    }
                                }
                                return true;
                                 }).Execute();
                    }
                }
            }
            GraphVisualizer.SaveFile(sb, Path);
            return true;
        }
    }
    class GraphVisualizerDataViewTreePass : CompilationPass
    {
        private string Path { get; }

        public GraphVisualizerDataViewTreePass(string path)
        {
            Debug.Assert(path != null);
            Path = path;
        }

        private static void AddNodeRecursive(StringBuilder sb, DataView dataView)
        {
            var dataLabel = $"{dataView.Id} ({dataView.SubDataKey})";
            GraphVisualizer.AddNode(sb, dataView.Id.Index, dataLabel);
            foreach (var child in dataView.Children)
            {
                AddNodeRecursive(sb, child);
                GraphVisualizer.AddLink(sb, dataView.Id.Index, child.Id.Index);
            }
        }
        public bool Execute(ref CompilationContext context)
        {
            StringBuilder sb = new StringBuilder();
            using (new GraphVisualizer.GraphScope(sb))
            {
                foreach (var dataView in context.graph.DataViews)
                {
                    if (dataView.Parent == null)
                    {
                        AddNodeRecursive(sb, dataView);
                    }
                }
            }
            GraphVisualizer.SaveFile(sb, Path);
            return true;
        }
    }

    class GraphVisualizerDataInTaskPass : CompilationPass
    {
        private string Path { get; }

        public GraphVisualizerDataInTaskPass(string path)
        {
            Debug.Assert(path != null);
            Path = path;
        }

        public bool Execute(ref CompilationContext context)
        {
            StringBuilder sb = new StringBuilder();
            using (new GraphVisualizer.GraphScope(sb))
            {
                foreach (var taskNode in context.graph.TaskNodes)
                {
                    var taskLabel = GraphVisualizerPassesHelpers.TaskLabel(taskNode);

                    using (new GraphVisualizer.ClusterScope(taskNode.Id.Index, taskLabel, sb))
                    {
                        foreach (var dataNode in taskNode.DataNodes)
                        {
                            var dataLabel = $"{dataNode.Id} (R:";
                            foreach (var dataView in dataNode.ReadDataViews)
                            {
                                dataLabel += $"{dataView.Id} ";
                            }
                            dataLabel += "|W:";
                            foreach (var dataView in dataNode.WrittenDataViews)
                            {
                                dataLabel += $"{dataView.Id} ";
                            }
                            dataLabel += ")";
                            GraphVisualizer.AddNode(sb, dataNode.Id.Index, dataLabel);
                        }
                    }
                }

                foreach (var dataNode in context.graph.DataNodes)
                {
                    foreach (var parent in dataNode.Parents)
                    {
                        GraphVisualizer.AddLink(sb, parent.Id.Index, dataNode.Id.Index);
                    }
                }
            }
            GraphVisualizer.SaveFile(sb, Path);
            return true;
        }


    }

    static class GraphVisualizerPassesHelpers
    {
        public static string TaskLabel(TaskNode taskNode)
        {
            string taskName;
            if(taskNode.Task is Task task)
                taskName = task.DebugName;
            else if (taskNode.Task is ExpressionTask expressionTask)
                taskName = $"{expressionTask.GetType().Name}-{expressionTask.Expression.ResultType.Name}";
            else if (taskNode.Task is TemplatedTask templatedTask)
                taskName = $"TemplatedTask-{templatedTask.TemplateName}";
            else
                taskName = taskNode.Task.GetType().Name;
            var taskLabel =
                $"{taskName}({taskNode.Id})";
            return taskLabel;
        }
    }
}
