using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Serialization;
using UnityEngine.Pool;

namespace UnityEditor.ShaderGraph
{
    static class TargetUtils
    {
        public static void ProcessSubTargetList(ref JsonData<SubTarget> activeSubTarget, ref List<SubTarget> subTargets)
        {
            if (subTargets == null || subTargets.Count == 0)
                return;

            // assign the initial sub-target, if none is assigned yet
            if (activeSubTarget.value == null)
            {
                // this is a bit of a hack: prefer subtargets named "Lit" if they exist, otherwise default to the first one
                // in the future, we should make the default sub-target user configurable
                var litSubTarget = subTargets.FirstOrDefault(x => x.displayName == "Lit");
                if (litSubTarget != null)
                    activeSubTarget = litSubTarget;
                else
                    activeSubTarget = subTargets[0];
                return;
            }

            // Update SubTarget list with active SubTarget
            var activeSubTargetType = activeSubTarget.value.GetType();
            var activeSubTargetCurrent = subTargets.FirstOrDefault(x => x.GetType() == activeSubTargetType);
            var index = subTargets.IndexOf(activeSubTargetCurrent);
            if (index == -1)
            {
                ShaderGraphImporter.subtargetNotFoundError = true;
                index = 0;
            }
            else ShaderGraphImporter.subtargetNotFoundError = false;
            subTargets[index] = activeSubTarget;
        }

        public static List<SubTarget> GetSubTargets<T>(T target) where T : Target
        {
            // Get Variants
            var subTargets = ListPool<SubTarget>.Get();
            var typeCollection = TypeCache.GetTypesDerivedFrom<SubTarget>();
            foreach (var type in typeCollection)
            {
                if (type.IsAbstract || !type.IsClass)
                    continue;

                var subTarget = (SubTarget)Activator.CreateInstance(type);
                if (!subTarget.isHidden && subTarget.targetType.Equals(typeof(T)))
                {
                    subTarget.target = target;
                    subTargets.Add(subTarget);
                }
            }

            return subTargets;
        }
    }
}
