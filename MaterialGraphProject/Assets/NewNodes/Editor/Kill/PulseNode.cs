using System.Reflection;

/*namespace UnityEditor.ShaderGraph
{
    [Title("OLD", "Pulse")]
    public class PulseNode : CodeFunctionNode
    {
        public PulseNode()
        {
            name = "Pulse";
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_Pulse", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_Pulse(
            [Slot(0, Binding.None)] Vector1 x,
            [Slot(1, Binding.None)] Vector1 xMinAndMax,
            [Slot(2, Binding.None)] out Vector1 result)
        {
            return @"
{{
    result = step( xMinAndMax.x, xValue ) - step( xMinAndMax.y, xValue );
}}
";
        }
    }
}*/
