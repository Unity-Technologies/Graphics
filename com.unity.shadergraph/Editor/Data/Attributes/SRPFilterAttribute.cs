using System;

namespace UnityEditor.ShaderGraph
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class SRPFilterAttribute : ContextFilterableAttribute
    {
        public Type[] srpTypes = null;
        public SRPFilterAttribute(params Type[] WorksWithSRP)
        {
            srpTypes = WorksWithSRP;
        }
    }
}
