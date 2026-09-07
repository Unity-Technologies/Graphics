using System.Collections.Generic;
using System.Text;
using UnityEditor.VFX;
using UnityEngine;

namespace Unity.GraphCommon.LowLevel.Editor
{
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
                                GraphVisualizer.AddNode(sb, nodeIndex, GraphVisualizerPassesHelpers.DataViewLabel(dataView));
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
                                if (parentDataNode.IsWritten(dataView.Id))
                                {
                                    int parentNodeIndex = parentDataNode.Id.Index | dataView.Id.Index << 16;
                                    GraphVisualizer.AddLink(sb, parentNodeIndex, nodeIndex);
                                    return false;
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
            GraphVisualizer.AddNode(sb, dataView.Id.Index, GraphVisualizerPassesHelpers.DataViewTreeLabel(dataView));
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
            WriteGraph(context.graph, Path);
            return true;
        }

        public static void WriteGraph(IReadOnlyGraph graph, string path)
        {
            StringBuilder sb = new StringBuilder();
            using (new GraphVisualizer.GraphScope(sb))
            {
                foreach (var taskNode in graph.TaskNodes)
                {
                    var taskLabel = GraphVisualizerPassesHelpers.TaskLabel(taskNode);
                    var (clusterFill, clusterBorder) = GraphVisualizerPassesHelpers.TaskCategoryColors(taskNode);
                    using (new GraphVisualizer.ClusterScope(taskNode.Id.Index, taskLabel, sb, clusterFill, clusterBorder))
                    {
                        foreach (var dataNode in taskNode.DataNodes)
                        {
                            var (htmlLabel, fill) = GraphVisualizerPassesHelpers.DataInTaskNodeLabel(dataNode);
                            GraphVisualizer.AddNode(sb, dataNode.Id.Index, htmlLabel, fillColorStr: fill);
                        }
                    }
                }

                foreach (var dataNode in graph.DataNodes)
                {
                    foreach (var parent in dataNode.Parents)
                    {
                        GraphVisualizer.AddLink(sb, parent.Id.Index, dataNode.Id.Index);
                    }
                }
            }
            GraphVisualizer.SaveFile(sb, path);
        }
    }

    static class GraphVisualizerPassesHelpers
    {
        public static string TaskLabel(TaskNode taskNode)
        {
            string taskName = taskNode.Name;
            if (string.IsNullOrEmpty(taskNode.Name))
            {
                taskName = taskNode.Task switch
                {
                    LegacyExpressionTask legacyExpressionTask =>
                        $"{legacyExpressionTask.Expression.GetType().Name}",
                    TemplatedTask templatedTask => $"TemplatedTask-{templatedTask.TemplateName}",
                    _ => taskNode.Task.GetType().Name,

                };
            }

            var taskLabel = $"{taskName}({taskNode.Id})";
            return taskLabel;
        }

        public static string DataViewLabel(DataView dataView)
        {
            return $"{dataView.DataContainer.Name}/{dataView.SubDataKey} ({dataView.Id})";
        }

        public static string DataViewTreeLabel(DataView dataView)
        {
            string prefix = dataView.IsRoot ? $"{dataView.DataContainer.Name} " : "";
            return $"{prefix}{dataView.Id} ({dataView.SubDataKey})";
        }

        public static (string htmlLabel, string fill) DataInTaskNodeLabel(DataNode dataNode)
        {
            bool anyRead = false;
            bool anyWritten = false;
            if (dataNode.UsedDataViewsRoot.Valid)
            {
                foreach (var dataView in dataNode.UsedDataViews)
                {
                    anyRead |= dataNode.IsRead(dataView.Id);
                    anyWritten |= dataNode.IsWritten(dataView.Id);
                }
            }

            var bindingsList = new List<string>(dataNode.DataBindings.Count);
            foreach (var binding in dataNode.DataBindings)
                bindingsList.Add(binding.BindingDataKey.ToString());
            string bindingsStr = bindingsList.Count > 0 ? HtmlEscape(string.Join(", ", bindingsList)) : "(no binding)";

            var html = new StringBuilder();
            html.Append('<');
            html.Append("<B>").Append(bindingsStr).Append("</B>");
            // The data views used by this node, laid out as a tree: each parent view sits above its
            // (indented) children, colour- and tag-coded by whether this node reads and/or writes it.
            html.Append("<BR ALIGN=\"LEFT\"/>");
            if (dataNode.UsedDataViewsRoot.Valid)
                AppendDataViewHierarchy(html, dataNode, dataNode.UsedDataViewsRoot, 0);
            html.Append("<FONT POINT-SIZE=\"8\" COLOR=\"#888888\">#")
                .Append(dataNode.Id.Index)
                .Append("</FONT>");
            html.Append('>');

            string fill = (anyRead, anyWritten) switch
            {
                (true,  true)  => "#e6ccff", // RW — purple
                (true,  false) => "#cce5ff", // R  — blue
                (false, true)  => "#ffe0b3", // W  — orange
                _              => "#f5f5f5", // none
            };

            return (html.ToString(), fill);
        }

        // Appends one indented line per used data view (pre-order, so each parent appears above its
        // children) to the node's HTML label, colour- and tag-coded by this node's read/write usage.
        static void AppendDataViewHierarchy(StringBuilder html, DataNode dataNode, DataView dataView, int depth)
        {
            bool read = dataNode.IsRead(dataView.Id);
            bool written = dataNode.IsWritten(dataView.Id);
            (string color, string tag) = (read, written) switch
            {
                (true,  true)  => ("#7a3fa0", " [RW]"), // read + written — purple
                (true,  false) => ("#0a4d99", " [R]"),  // read — blue
                (false, true)  => ("#a05500", " [W]"),  // written — orange
                _              => ("#888888", ""),      // structural parent — gray
            };
            string subKey = dataView.SubDataKey != null ? " " + HtmlEscape(dataView.SubDataKey.ToString()) : "";

            for (int i = 0; i < depth; i++)
                html.Append("&nbsp;&nbsp;");
            html.Append("<FONT POINT-SIZE=\"9\" COLOR=\"").Append(color).Append("\">")
                .Append(dataView.Id).Append(subKey).Append(tag)
                .Append("</FONT><BR ALIGN=\"LEFT\"/>");

            foreach (var child in dataView.Children)
                AppendDataViewHierarchy(html, dataNode, child, depth + 1);
        }

        // Pick (fill, border) colors for a task cluster based on a coarse category.
        public static (string fill, string border) TaskCategoryColors(TaskNode taskNode)
        {
            if ( taskNode.Task is VfxTemplatedTask or GpuKernelTask or RenderingTask)
                return ("#dce7f2", "#5b7a99"); // lifecycle — cool blue
            if (taskNode.Task is ParticleSystemTask)
                return ("#dceadc", "#6a8a6a"); // system — soft green
            if (taskNode.Task is LegacyExpressionTask)
                return ("#f4ecd0", "#a08c50"); // expression/value — yellow
            return ("#eef3f8", "#5b7a99");     // default — neutral blue
        }

        // Minimal HTML escaping for GraphViz HTML labels.
        public static string HtmlEscape(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            return s.Replace("&", "&amp;")
                    .Replace("<", "&lt;")
                    .Replace(">", "&gt;")
                    .Replace("\"", "&quot;");
        }
    }
}
