using System.Reflection;

namespace UnityEditor.ShaderGraph
{
    [Title("Input", "Geometry", "Instance ID")]
    class InstanceIDNode : CodeFunctionNode
    {
        public override bool hasPreview { get { return false; } }

        public InstanceIDNode()
        {
            name = "Instance ID";
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("UnityGetInstanceID", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string UnityGetInstanceID([Slot(0, Binding.None)] out Vector1 Out)
        {
            return
@"
{
#if UNITY_ANY_INSTANCING_ENABLED
    Out = unity_InstanceID;
#else
    Out = 0;
#endif
}
";
        }
    }
}
