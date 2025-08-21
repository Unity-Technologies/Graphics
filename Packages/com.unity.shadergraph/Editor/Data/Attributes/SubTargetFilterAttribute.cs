using System;
using System.Collections.Generic;

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

    internal static class SubTargetFilterUtil
    {
        // This is to check actual compatibility with a subtarget.
        internal static bool IsSubTargetCompatible(this AbstractMaterialNode node, Type subTarget)
        {
            var subTargetFilter = NodeClassCache.GetAttributeOnNodeType<SubTargetFilterAttribute>(node.GetType());
            if (subTargetFilter == null)
                return true;

            foreach (var type in subTargetFilter.subTargetTypes)
                if (type.IsAssignableFrom(subTarget))
                    return true;

            return false;
        }

        // This does not check for inheritance, it's mainly for comparing between filters of two or more nodes.
        internal static bool IsCompatibleWithSubTargetFilters(this AbstractMaterialNode node, HashSet<Type> subTargetTypes, out HashSet<Type> incompatibleSet)
        {
            var subTargetFilter = NodeClassCache.GetAttributeOnNodeType<SubTargetFilterAttribute>(node.GetType());
            incompatibleSet = null;

            if (subTargetFilter == null || subTargetTypes.IsSubsetOf(subTargetFilter.subTargetTypes))
                return true;

            incompatibleSet = new(subTargetTypes);
            incompatibleSet.ExceptWith(subTargetFilter.subTargetTypes);
            return false;
        }

        // This does not gather inherited types, it's for populating the declared subtarget filters.
        // There is not currently a case where inherited types would need to be dealt with here, but this won't work
        // correctly if that's needed.
        internal static void GatherSubTargetCompatibility(this AbstractMaterialNode node, ref HashSet<Type> subTargetTypes)
        {
            var subTargetFilter = NodeClassCache.GetAttributeOnNodeType<SubTargetFilterAttribute>(node.GetType());
            if (subTargetFilter == null)
                return;
            HashSet<Type> supportedTypes = new HashSet<Type>(subTargetFilter.subTargetTypes);
            subTargetTypes.UnionWith(supportedTypes);
        }
    }
}
