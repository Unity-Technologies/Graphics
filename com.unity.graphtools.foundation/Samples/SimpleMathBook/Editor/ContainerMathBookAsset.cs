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
        public new static void CreateGraph()
        {
            const string path = "Assets";
            var template = new GraphTemplate<MathBookStencil>("Container " + MathBookStencil.GraphName);

            GraphAssetCreationHelpers.CreateInProjectWindow<ContainerMathBookAsset>(template, null, path,
                () => GraphViewEditorWindow.FindOrCreateGraphWindow<SimpleGraphViewWindow>());
        }
    }
}
