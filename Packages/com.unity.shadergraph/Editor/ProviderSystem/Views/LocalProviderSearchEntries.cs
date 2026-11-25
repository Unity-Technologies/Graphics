using System.Collections.Generic;

namespace UnityEditor.ShaderGraph.ProviderSystem
{
    static class LocalProviderSearchEntries
    {
        // Listed first, with only this header in italics.
        internal const string kCategory = "Locals";

        internal static List<(string[] path, AbstractMaterialNode node)> For(GraphData graph)
        {
            List<(string[], AbstractMaterialNode)> entries = new();
            HashSet<string> paths = new();
            foreach (var provider in graph.loadedVirtualFunctions)
            {
                var context = AbstractVirtualFunctionNode.ContextLabel(AbstractVirtualFunctionNode.ContextOf(provider.Definition));
                foreach (var node in AbstractVirtualFunctionNode.NodesFor(provider.Definition))
                {
                    node.InitializeFromLocalProvider(provider);
                    entries.Add((Unique(paths, new[] { kCategory, "Virtual Functions", context, node.name }), node));
                }
            }
            return entries;
        }

        // The searcher merges entries with the same path, so later ones are numbered like the library's.
        static string[] Unique(HashSet<string> paths, string[] path)
        {
            var name = path[path.Length - 1];
            for (int i = 1; !paths.Add(string.Join("/", path)); ++i)
                path[path.Length - 1] = $"{name}, ({i})";
            return path;
        }
    }
}
