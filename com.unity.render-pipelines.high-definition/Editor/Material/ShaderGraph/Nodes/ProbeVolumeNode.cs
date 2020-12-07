using System.Reflection;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("Input", "Probe Volume")]
    class ProbeVolumeNode : CodeFunctionNode
    {
        public override bool hasPreview { get { return false; } }

        public ProbeVolumeNode()
        {
            name = "Probe Volume";
        }


        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_ProbeVolume", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_ProbeVolume(
           [Slot(2, Binding.WorldSpacePosition)] Vector3 Position,
           [Slot(0, Binding.WorldSpaceNormal)] Vector3 Normal,
           [Slot(1, Binding.None)] out Vector3 Out)
        {
            Out = Vector3.one;
            return
                @"
{
    Out = SHADERGRAPH_PROBE_VOLUME(Position, Normal);
}
";
        }
    }
}
