using System;
using System.Reflection;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    [Title("Utility", "Logic", "TestInputConnection")]
    class TestInputConnectionNode : CodeFunctionNode
    {
        public TestInputConnectionNode()
        {
            name = "Test Input Connection";
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_InputConnectionBranch", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_InputConnectionBranch(
            [Slot(0, Binding.None)] PropertyConnectionState Input,
            [Slot(1, Binding.None, 1, 1, 1, 1)] DynamicDimensionVector Connected,
            [Slot(2, Binding.None, 0, 0, 0, 0)] DynamicDimensionVector NotConnected,
            [Slot(3, Binding.None, ShaderStageCapability.Fragment)] out DynamicDimensionVector Out)
        {

            return
@"
{
    Out = Input ? Connected : NotConnected;
}
";
        }
    }
}
