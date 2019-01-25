using System.Reflection;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing.Controls;

namespace UnityEditor.ShaderGraph
{
    [FormerName("UnityEditor.ShaderGraph.LightProbeNode")]
    [Title("Input", "Lighting", "Baked GI")]
    class BakedGAbstractMaterialNode : CodeFunctionNode
    {
        public override bool hasPreview { get { return false; } }

        public BakedGAbstractMaterialNode()
        {
            name = "Baked GI";
        }

        public override string documentationURL
        {
            get { return "https://github.com/Unity-Technologies/ShaderGraph/wiki/Baked-GI-Node"; }
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            if(applyScaling.isOn)
                return GetType().GetMethod("Unity_BakedGIScale", BindingFlags.Static | BindingFlags.NonPublic);
            else
                return GetType().GetMethod("Unity_BakedGI", BindingFlags.Static | BindingFlags.NonPublic);
        }

        [SerializeField]
        private bool m_ApplyScaling = true;

        [ToggleControl("Apply Lightmap Scaling")]
        public ToggleData applyScaling
        {
            get { return new ToggleData(m_ApplyScaling); }
            set
            {
                if (m_ApplyScaling == value.isOn)
                    return;
                m_ApplyScaling = value.isOn;
                Dirty(ModificationScope.Node);
            }
        }

        static string Unity_BakedGI(
           [Slot(2, Binding.WorldSpacePosition)] Vector3 Position,
           [Slot(0, Binding.WorldSpaceNormal)] Vector3 Normal,
           [Slot(3, Binding.MeshUV1)] Vector2 StaticUV,
           [Slot(4, Binding.MeshUV2)] Vector2 DynamicUV,
           [Slot(1, Binding.None)] out Vector3 Out)
        {
            Out = Vector3.one;
            return
                @"
{
    Out = SHADERGRAPH_BAKED_GI(Position, Normal, StaticUV, DynamicUV, false);
}
";
        }

        static string Unity_BakedGIScale(
           [Slot(2, Binding.WorldSpacePosition)] Vector3 Position,
           [Slot(0, Binding.WorldSpaceNormal)] Vector3 Normal,
           [Slot(3, Binding.MeshUV1)] Vector2 StaticUV,
           [Slot(4, Binding.MeshUV2)] Vector2 DynamicUV,
           [Slot(1, Binding.None)] out Vector3 Out)
        {
            Out = Vector3.one;
            return
                @"
{
    Out = SHADERGRAPH_BAKED_GI(Position, Normal, StaticUV, DynamicUV, true);
}
";
        }
    }
}
