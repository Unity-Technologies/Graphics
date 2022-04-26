using System;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.MathBook
{
    [Serializable]
    public class ContainerMathBookAsset : MathBookAsset
    {
        [MenuItem("Assets/Create/GTF Samples/Math Book/Container Math Book")]
        public new static void CreateGraph(MenuCommand menuCommand)
        {
            const string path = "Assets";
            var template = new GraphTemplate<MathBookStencil>("Container " + MathBookStencil.GraphName);
            ICommandTarget target = null;
            var window = GraphViewEditorWindow.FindOrCreateGraphWindow<SimpleGraphViewWindow>();
            if (window != null)
                target = window.GraphTool;

            GraphAssetCreationHelpers.CreateInProjectWindow<ContainerMathBookAsset>(template, target, path);
        }
    }
}
