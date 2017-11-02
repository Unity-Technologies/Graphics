using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEditor.Graphing;

namespace UnityEditor.ShaderGraph
{
    [Title("Input/Scene Data/Reflection Probe")]
    public class ReflectionProbeNode : CodeFunctionNode
    {
        public ReflectionProbeNode()
        {
            name = "ReflectionProbe";
        }

        public override PreviewMode previewMode
        {
            get
            {
                return PreviewMode.Preview3D;
            }
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_ReflectionProbe", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_ReflectionProbe(
            [Slot(0, Binding.ObjectSpaceNormal)] Vector3 viewDirection,
            [Slot(1, Binding.ObjectSpaceViewDirection)] Vector3 worldSpaceNormal,
            [Slot(2, Binding.None)] Vector1 lod,
            [Slot(3, Binding.None)] out Vector4 color)
        {
            color = Vector4.one;
            return
                @"
{
    {precision}3 reflect = reflect(-viewDirection, worldSpaceNormal);
    color = DecodeHDR(UNITY_SAMPLE_TEXCUBE_LOD(unity_SpecCube0, reflect, lod), unity_SpecCube0_HDR);
}
";
        }
    }
}
