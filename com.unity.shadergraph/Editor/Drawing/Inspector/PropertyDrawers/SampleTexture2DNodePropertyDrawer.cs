using System;
using System.Reflection;
using UnityEngine;
using UnityEditor.UIElements;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing;
using UnityEditor.Graphing.Util;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.Drawing.Inspector.PropertyDrawers
{
    [SGPropertyDrawer(typeof(SampleTexture2DNode))]
    class SampleTexture2DNodePropertyDrawer : AbstractMaterialNodePropertyDrawer
    {
        internal override void AddCustomNodeProperties(VisualElement parentElement, AbstractMaterialNode nodeBase, Action setNodesAsDirtyCallback, Action updateNodeViewsCallback)
        {
            var node = nodeBase as SampleTexture2DNode;
            PropertyDrawerUtils.AddCustomCheckboxProperty(
                parentElement, nodeBase, setNodesAsDirtyCallback, updateNodeViewsCallback,
                "Use Global Mip Bias", "Change Enable Global Mip Bias",
                () => node.enableGlobalMipBias, (val) => node.enableGlobalMipBias = val);
            PropertyDrawerUtils.AddCustomEnumProperty<Texture2DMipSamplingMode>(
                parentElement, nodeBase, setNodesAsDirtyCallback, updateNodeViewsCallback,
                "Mip Sampling Mode", "Change Mip Sampling Mode",
                () => node.mipSamplingMode, (val) => node.mipSamplingMode = val);
        }
    }
}
