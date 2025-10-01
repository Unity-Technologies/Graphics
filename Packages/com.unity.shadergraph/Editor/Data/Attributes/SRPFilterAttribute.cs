using System;
using System.Collections.Generic;

namespace UnityEditor.ShaderGraph
{
    [GenerationAPI]
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    internal class SRPFilterAttribute : ContextFilterableAttribute
    {
        public Type[] srpTypes = null;
        public SRPFilterAttribute(params Type[] WorksWithSRP)
        {
            srpTypes = WorksWithSRP;
        }
    }

    internal static class SRPFilterUtil
    {
        internal static bool IsCompatibleWithSRPs(this AbstractMaterialNode node, HashSet<Type> srpTypes, out HashSet<Type> incompatibleSet)
        {
            var srpFilter = NodeClassCache.GetAttributeOnNodeType<SRPFilterAttribute>(node.GetType());
            incompatibleSet = null;

            if (srpFilter == null || srpTypes.IsSubsetOf(srpFilter.srpTypes))
                return true;

            incompatibleSet = new(srpTypes);
            incompatibleSet.ExceptWith(srpFilter.srpTypes);
            return false;
        }

        internal static void GatherSRPCompatibility(this AbstractMaterialNode node, ref HashSet<Type> srpTypes)
        {
            var srpFilter = NodeClassCache.GetAttributeOnNodeType<SRPFilterAttribute>(node.GetType());
            if (srpFilter == null)
                return;

            HashSet<Type> supportedTypes = new HashSet<Type>(srpFilter.srpTypes);
            srpTypes.UnionWith(supportedTypes);
        }
    }
}
