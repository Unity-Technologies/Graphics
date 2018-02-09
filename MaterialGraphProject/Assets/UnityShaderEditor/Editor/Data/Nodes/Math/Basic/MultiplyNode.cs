using System.Reflection;

namespace UnityEditor.ShaderGraph
{
    [Title("Math", "Basic", "Multiply")]
    public class MultiplyNode : CodeFunctionNode
    {
        public MultiplyNode()
        {
            name = "Multiply";
        }

        public override string documentationURL
        {
            get { return "https://github.com/Unity-Technologies/ShaderGraph/wiki/Multiply-Node"; }
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_Multiply", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_Multiply(
            [Slot(0, Binding.None, 0, 0, 0, 0)] DynamicDimensionVector A,
            [Slot(1, Binding.None, 2, 2, 2, 2)] DynamicDimensionVector B,
            [Slot(2, Binding.None)] out DynamicDimensionVector Out)
        {
            return
                @"
{
    Out = A * B;
}
";
        }
    }
}
