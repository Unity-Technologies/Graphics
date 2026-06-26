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

        public static void AddNode(StringBuilder sb, int id, string label,
                                   string colorStr = null, string fillColorStr = null)
        {
            // GraphViz: a label that starts with '<' is interpreted as HTML (no quotes).
            bool isHtml = label.Length > 0 && label[0] == '<';
            var labelAttr = isHtml ? $"label={label}" : $"label=\"{label}\"";
            var attrs = labelAttr;
            if (colorStr != null)     attrs += $", color={colorStr}";
            if (fillColorStr != null) attrs += $", fillcolor=\"{fillColorStr}\"";
            sb.AppendLine($"        {SafeId(id)} [{attrs}];");
        }

        public static void AddLink(StringBuilder sb, int from, int to)
        {
            sb.AppendLine($"    {SafeId(from)} -> {SafeId(to)};");
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
                sb.AppendLine("    graph [fontname=\"Helvetica\", fontsize=11, nodesep=0.18, ranksep=1.2, splines=spline];");
                sb.AppendLine("    node  [shape=box, style=\"rounded,filled\", fillcolor=\"#fafafa\", color=\"#888888\", fontname=\"Helvetica\", fontsize=10, margin=\"0.10,0.04\"];");
                sb.AppendLine("    edge  [color=\"#888888\", arrowsize=0.6, penwidth=0.8, arrowhead=open];");
            }

            protected override void CloseScope()
            {
                sb.AppendLine("}"); // End graph
            }
        }

        public class ClusterScope : GraphVizScope
        {
            public ClusterScope(int id, string label, StringBuilder sb,
                                string fillColor = "#eef3f8", string borderColor = "#5b7a99") : base(sb)
            {
                sb.AppendLine($"    subgraph cluster_{SafeId(id)} {{");
                sb.AppendLine($"        rankdir=LR;"); // Specify left-to-right layout
                sb.AppendLine($"        label=\"{label}\";");
                sb.AppendLine($"        style=\"rounded,filled\";");
                sb.AppendLine($"        fillcolor=\"{fillColor}\";");
                sb.AppendLine($"        color=\"{borderColor}\";");
                sb.AppendLine($"        penwidth=1.2;");
                sb.AppendLine($"        fontname=\"Helvetica-Bold\";");
                sb.AppendLine($"        fontsize=12;");
            }
            protected override void CloseScope()
            {
                sb.AppendLine("    }"); // Close TaskNode cluster
            }
        }

        }
}
