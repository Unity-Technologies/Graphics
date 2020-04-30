using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Serialization;

namespace UnityEditor.ShaderGraph
{
    static class TargetUtils
    {
        public static void ProcessSubTargetList(ref JsonData<SubTarget> activeSubTarget, ref List<SubTarget> subTargets)
        {
            if(subTargets == null || subTargets.Count == 0)
                return;

            if(activeSubTarget.value == null)
            {
                activeSubTarget = subTargets[0];
                return;
            }

            // Update SubTarget list with active SubTarget
            var activeSubTargetType = activeSubTarget.value.GetType();
            var activeSubTargetCurrent = subTargets.FirstOrDefault(x => x.GetType() == activeSubTargetType);
            var index = subTargets.IndexOf(activeSubTargetCurrent);
            subTargets[index] = activeSubTarget;
        }

        public static List<SubTarget> GetSubTargets<T>(T target) where T : Target
        {
            // Get Variants
            var subTargets = ListPool<SubTarget>.Get();
            var typeCollection = TypeCache.GetTypesDerivedFrom<SubTarget>();
            foreach (var type in typeCollection)
            {
                if(type.IsAbstract || !type.IsClass)
                    continue;

                var subTarget = (SubTarget)Activator.CreateInstance(type);
                if(!subTarget.isHidden && subTarget.targetType.Equals(typeof(T)))
                {
                    subTarget.target = target;
                    subTargets.Add(subTarget);
                }
            }

            return subTargets;
        }
    }
}
