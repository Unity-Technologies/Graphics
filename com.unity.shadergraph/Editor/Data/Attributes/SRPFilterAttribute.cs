using System;

namespace UnityEditor.ShaderGraph
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    internal class SRPFilterAttribute : ContextFilterableAttribute
    {
        public Type[] srpTypes = null;
        public SRPFilterAttribute(params Type[] WorksWithSRP)
        {
            srpTypes = WorksWithSRP;
        }
    }
}
