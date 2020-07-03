using System;
using System.Collections.Generic;
using UnityEditor.Graphing;

namespace UnityEditor.ShaderGraph
{
    static class TargetUtils
    {
        public static List<SubTarget> GetSubTargetsOfType<T>() where T : Target
        {
            // Get Variants
            var subTargets = ListPool<SubTarget>.Get();
            var typeCollection = TypeCache.GetTypesDerivedFrom<SubTarget>();
            foreach (var type in typeCollection)
            {
                if(type.IsAbstract || !type.IsClass)
                    continue;

                var subTarget = (SubTarget)Activator.CreateInstance(type);
                if(subTarget.targetType.Equals(typeof(T)))
                {
                    subTargets.Add(subTarget);
                }
            }

            return subTargets;
        }
    }
}
