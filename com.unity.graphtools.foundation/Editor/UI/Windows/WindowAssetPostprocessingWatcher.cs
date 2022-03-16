using System.Linq;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    class WindowAssetPostprocessingWatcher : AssetPostprocessor
    {
        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            if (!deletedAssets.Any() && !importedAssets.Any())
                return;

            var windows = Resources.FindObjectsOfTypeAll<GraphViewEditorWindow>();

            foreach (var deletedAssetPath in deletedAssets)
            {
                var deletedAssetGuid = AssetDatabase.AssetPathToGUID(deletedAssetPath);

                foreach (var window in windows)
                {
                    if (IsParentGraphOfSubgraph(window, deletedAssetGuid))
                        ReloadGraph(window.GraphView);
                }
            }

            foreach (var importedAssetPath in importedAssets)
            {
                var importedAssetGuid = AssetDatabase.AssetPathToGUID(importedAssetPath);

                foreach (var window in windows)
                {
                    if (IsParentGraphOfSubgraph(window, importedAssetGuid))
                        ReloadGraph(window.GraphView);
                }
            }
        }

        static bool IsParentGraphOfSubgraph(GraphViewEditorWindow window, string deletedSubgraphGuid)
        {
            return window.GraphView?.GraphModel?.NodeModels != null && window.GraphView.GraphModel.NodeModels.OfType<ISubgraphNodeModel>().Any(n => n.SubgraphGuid == deletedSubgraphGuid);
        }

        static void ReloadGraph(GraphView view)
        {
            view.Dispatch(new LoadGraphAssetCommand(view.GraphModel?.AssetModel, loadStrategy: LoadGraphAssetCommand.LoadStrategies.KeepHistory));
        }
    }
}
