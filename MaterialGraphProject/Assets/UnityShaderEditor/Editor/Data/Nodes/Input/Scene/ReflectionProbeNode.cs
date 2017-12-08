using System.Reflection;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("Input", "Scene", "Reflection Probe")]
    public class ReflectionProbeNode : CodeFunctionNode
    {
        public ReflectionProbeNode()
        {
            name = "Reflection Probe";
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
            [Slot(0, Binding.ObjectSpaceViewDirection)] Vector3 ViewDir,
            [Slot(1, Binding.ObjectSpaceNormal)] Vector3 Normal,
            [Slot(2, Binding.None)] Vector1 LOD,
            [Slot(3, Binding.None)] out Vector3 Out)
        {
            Out = Vector3.one;
            return
                @"
{
    {precision}3 reflectVec = reflect(-ViewDir, Normal);
    Out = DecodeHDR(UNITY_SAMPLE_TEXCUBE_LOD(unity_SpecCube0, reflectVec, LOD), unity_SpecCube0_HDR);
}
";
        }
    }
}
