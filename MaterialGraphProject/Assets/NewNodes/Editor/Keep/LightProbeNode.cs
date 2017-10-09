using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
    [Title("Input/Scene Data/Light Probe")]
    public class LightProbeNode : CodeFunctionNode
    {
        public override bool hasPreview { get { return false; } }

        public LightProbeNode()
        {
            name = "LightProbe";
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_LightProbe", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_LightProbe(
            [Slot(0, Binding.WorldSpaceNormal)] Vector3 worldSpaceNormal,
            [Slot(1, Binding.None)] out Vector4 color)
        {
            color = Vector4.one;
            return
                @"
{
    color = ShadeSH9(float4(worlsSpaceNormal , 1));
}
";
        }

        public override PreviewMode previewMode
        {
            get
            {
                return PreviewMode.Preview3D;
            }
        }
    }
}
