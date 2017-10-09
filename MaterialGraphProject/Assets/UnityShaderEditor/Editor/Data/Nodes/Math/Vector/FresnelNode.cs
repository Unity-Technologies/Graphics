using System.Reflection;

namespace UnityEngine.MaterialGraph
{
    [Title("Math/Vector/Fresnel")]
    class FresnelNode : CodeFunctionNode
    {
        public FresnelNode()
        {
            name = "Fresnel";
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_Fresnel", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_Fresnel(
            [Slot(0, Binding.None)] Vector3 first,
            [Slot(1, Binding.None)] Vector3 second,
            [Slot(2, Binding.None)] out Vector1 result)
        {
            return
                @"
{
    result = (1.0 - dot (normalize (first), normalize (second)));
}
";
        }
    }
}
