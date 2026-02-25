using System;
using System.IO;
using System.Text;

namespace Unity.GraphCommon.LowLevel.Editor
{
    static class GraphVisualizer
    {
        /// <summary>
        /// Generates a safe identifier for GraphViz by replacing invalid characters.
        /// </summary>
        /// <param name="id">The graph element id.</param>
        /// <returns>A string that can be used as a GraphViz node identifier.</returns>
        private static string SafeId(object id)
        {
            return $"node_{id.ToString().Replace("-", "_")}";
        }

        public static void AddNode(StringBuilder sb, int id, string label, string colorStr = "black")
        {
            sb.AppendLine($"        {SafeId(id)} [shape=ellipse, label=\"{label}\", color={colorStr}];");
        }

        public static void AddLink(StringBuilder sb, int from, int to)
        {
            sb.AppendLine($"    {SafeId(from)} -> {SafeId(to)} [label=\"\", color=green];");
        }

        public static void SaveFile(StringBuilder sb, string filePath)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            File.WriteAllText(filePath, sb.ToString());
        }
        public abstract class GraphVizScope : IDisposable
        {
            protected StringBuilder sb;
            protected GraphVizScope(StringBuilder sb)
            {
                this.sb = sb;
            }
            public void Dispose()
            {
                CloseScope();
            }
            protected virtual void CloseScope()
            {
            }
        }

        public class GraphScope : GraphVizScope
        {
            public GraphScope(StringBuilder sb) : base(sb)
            {
                sb.AppendLine("digraph G {");
                sb.AppendLine("    rankdir=TB;"); // Specify top-to-bottom layout
                sb.AppendLine("    compound=true;"); // Allow edges between clusters
            }

            protected override void CloseScope()
            {
                sb.AppendLine("}"); // End graph
            }
        }

        public class ClusterScope : GraphVizScope
        {
            public ClusterScope(int id, string label, StringBuilder sb) : base(sb)
            {
                sb.AppendLine($"    subgraph cluster_{SafeId(id)} {{");
                sb.AppendLine($"        rankdir=LR;"); // Specify left-to-right layout
                sb.AppendLine($"        label=\"{label}\";");
                sb.AppendLine($"        style=rounded;"); // Rounded cluster border
                sb.AppendLine($"        color=black;"); // Cluster border color
            }
            protected override void CloseScope()
            {
                sb.AppendLine("    }"); // Close TaskNode cluster
            }
        }

        }
}
