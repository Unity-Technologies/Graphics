using System.Reflection;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("Utility", "Logic", "Nand")]
    class NandNode : CodeFunctionNode
    {
        public NandNode()
        {
            name = "Nand";
        }

        public override bool hasPreview
        {
            get { return false; }
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_Nand", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_Nand(
            [Slot(0, Binding.None)] Boolean A,
            [Slot(1, Binding.None)] Boolean B,
            [Slot(2, Binding.None)] out Boolean Out)
        {
            return
@"
{
    Out = !A && !B;
}
";
        }
    }
}
