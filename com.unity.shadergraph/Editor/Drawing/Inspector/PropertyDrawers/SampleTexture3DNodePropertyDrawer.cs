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
    [SGPropertyDrawer(typeof(SampleTexture3DNode))]
    class SampleTexture3DNodePropertyDrawer : AbstractMaterialNodePropertyDrawer
    {
        internal override void AddCustomNodeProperties(VisualElement parentElement, AbstractMaterialNode nodeBase, Action setNodesAsDirtyCallback, Action updateNodeViewsCallback)
        {
            var node = nodeBase as SampleTexture3DNode;
            PropertyDrawerUtils.AddCustomEnumProperty<Texture3DMipSamplingMode>(
                parentElement, nodeBase, setNodesAsDirtyCallback, updateNodeViewsCallback,
                "Mip Sampling Mode", "Change Mip Sampling Mode",
                () => node.mipSamplingMode, (val) => node.mipSamplingMode = val);
        }
    }
}
