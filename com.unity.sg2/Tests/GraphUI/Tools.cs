using System;
using System.Linq;
using Unity.GraphToolsFoundation.Editor;
using UnityEngine;

namespace UnityEditor.ShaderGraph.GraphUI.UnitTests
{
    /*
     * A place to store any tools
     */
    public class Tools
    {
        string testAssetPath => $"Assets\\AllNodes{ShaderGraphStencil.GraphExtension}";

        [MenuItem("Window/Shaders/Tools/Generate All Nodes Graph", false)]
        void GenerateAllNodesGraph()
        {
            var graphWindowTest = new BaseGraphWindowTest();
            var window = graphWindowTest.CreateWindow();

            var newGraphAction = ScriptableObject.CreateInstance<GraphAssetUtils.CreateGraphAssetAction>();
            newGraphAction.Action(0, testAssetPath, "");
            var graphAsset = AssetDatabase.LoadAssetAtPath<ShaderGraphAsset>(testAssetPath);

            if (graphAsset != null)
            {
                window.GraphTool.Dispatch(new LoadGraphCommand(graphAsset.GraphModel));
                window.GraphTool.Update();
            }

            static int RoundOff(int i)
            {
                return ((int)Math.Round(i / 10.0)) * 10;
            }

            int Mod(int a, int n) => (a % n + n) % n;

            // TODO: Move this into a menuitem and save the asset off
            if (window.GraphView.GraphModel is ShaderGraphModel shaderGraphModel)
            {
                var shaderGraphStencil = shaderGraphModel.Stencil as ShaderGraphStencil;
                var searcherDatabaseProvider = new ShaderGraphSearcherDatabaseProvider(shaderGraphStencil);
                var searcherDatabases = searcherDatabaseProvider.GetGraphElementsDatabases(shaderGraphModel);
                foreach (var database in searcherDatabases.Skip(1))
                {
                    var allNodes = database.Search("");
                    var roundedSize = RoundOff(allNodes.Count);

                    var gridWidth = roundedSize / 10;
                    var yIndex = 0;
                    for (var i = 0; i < allNodes.Count; ++i)
                    {
                        var searcherItem = allNodes[i];
                        var xIndex = Mod(i, gridWidth);
                        var position = new Vector2(500 * xIndex, yIndex * 500);
                        if (xIndex == gridWidth - 1)
                            yIndex++;
                        var createNodeCommand = new CreateNodeCommand().WithNodeOnGraph(searcherItem as GraphNodeModelLibraryItem, position);
                        window.GraphView.Dispatch(createNodeCommand);

                        window.GraphView.DispatchFrameAllCommand();
                    }
                }
            }
        }
    }
}
