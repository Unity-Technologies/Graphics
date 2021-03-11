using System.Reflection;

namespace UnityEditor.ShaderGraph
{
    [Title("Math", "Wave", "Sawtooth Wave")]
    class SawtoothWaveNode : CodeFunctionNode
    {
        public SawtoothWaveNode()
        {
            name = "Sawtooth Wave";
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("SawtoothWave", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string SawtoothWave(
            [Slot(0, Binding.None)] DynamicDimensionVector In,
            [Slot(1, Binding.None)] out DynamicDimensionVector Out)
        {
            return
@"
{
    Out = 2 * (In - floor(0.5 + In));
}
";
        }
    }
}
