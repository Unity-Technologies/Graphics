#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;

namespace UnityEngine.Rendering
{
    static partial class RenderPipelineGraphicsSettingsStripper
    {
        static class Fetcher
        {
            static readonly Type k_InterfaceType = typeof(IRenderPipelineGraphicsSettingsStripper<>);

            public static Dictionary<Type, List<IStripper>> ComputeStrippersMap()
            {
                var validStrippers = new Dictionary<Type, List<IStripper>>();

                foreach (var (stripperType, settingsType) in GetStrippersFromAssemblies())
                {
                    var stripperInstance = Activator.CreateInstance(stripperType) as IStripper;
                    if (stripperInstance is not { active: true })
                        continue;

                    if (!validStrippers.TryGetValue(settingsType, out var instances))
                    {
                        instances = new List<IStripper>();
                        validStrippers[settingsType] = instances;
                    }

                    instances.Add(stripperInstance);
                }

                return validStrippers;
            }

            private static IEnumerable<(Type, Type)> GetStrippersFromAssemblies()
            {
                foreach (var stripperType in TypeCache.GetTypesDerivedFrom(typeof(IRenderPipelineGraphicsSettingsStripper<>)))
                {
                    if (stripperType.IsAbstract)
                        continue;

                    // The ctor is private?
                    if (stripperType.GetConstructor(BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes,
                            null) == null)
                    {
                        Debug.LogWarning($"{stripperType} has no public constructor, it will not be used to strip {nameof(IRenderPipelineGraphicsSettings)}.");
                        continue;
                    }

                    foreach (var i in stripperType.GetInterfaces())
                    {
                        if (i.IsGenericType && i.GetGenericTypeDefinition() == k_InterfaceType)
                        {
                            yield return (stripperType, i.GetGenericArguments()[0]);
                        }
                    }
                }
            }
        }
    }
}
#endif
