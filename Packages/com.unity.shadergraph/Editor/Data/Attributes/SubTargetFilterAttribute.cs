using System;

namespace UnityEditor.ShaderGraph
{
    [GenerationAPI]
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    internal class SubTargetFilterAttribute : ContextFilterableAttribute
    {
        public Type[] subTargetTypes = null;
        public SubTargetFilterAttribute(params Type[] WorksWithSubTargets)
        {
            subTargetTypes = WorksWithSubTargets;
        }
    }
}
