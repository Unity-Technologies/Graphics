using System;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.Drawing.Inspector.PropertyDrawers
{
    [SGPropertyDrawer(typeof(SampleElementTextureNode))]
    class SampleElementTextureNodePropertyDrawer : AbstractMaterialNodePropertyDrawer
    {
        internal override void AddCustomNodeProperties(VisualElement parentElement, AbstractMaterialNode nodeBase, Action setNodesAsDirtyCallback, Action updateNodeViewsCallback)
        {
            var node = nodeBase as SampleElementTextureNode;
            PropertyDrawerUtils.AddCustomIntegerProperty(
                parentElement, nodeBase, setNodesAsDirtyCallback, updateNodeViewsCallback,
                "Sample Count", "Change Sample Count", 1, 32,
                () => node.sampleCount, (val) => node.sampleCount = val);
            PropertyDrawerUtils.AddCustomEnumProperty<ElementTextureMipSamplingMode>(
                parentElement, nodeBase, setNodesAsDirtyCallback, updateNodeViewsCallback,
                "Mip Sampling Mode", "Change Mip Sampling Mode",
                () => node.mipSamplingMode, (val) => node.mipSamplingMode = val);
        }
    }
}
